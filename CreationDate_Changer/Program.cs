using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CreationDate_Changer
{
    class Program
    {
        //Программа нужна для поддержания работы самоочищающейся папки, файлы в которой удаляются через N дней после их помещения туда
        //Программа проверяет наличие новых файлов (ранее не виденных) в указанном каталоге
        //Если такие файлы найдены - меняет их время создания на "Сейчас"
        //Проверяет наличие старых файлов, и если их нет - удаляет их из "Базы"
        //Записывает в свою "Базу" найденные новые файлы

        //Алгоритм работы
        //Открыть Базу
        //Проверить наличие старых файлов из Базы. Если их нет - удалить записи Базы
        //Найти все файлы в каталоге
        //Для каждого найденного файла проверить наличие в Базе
        //Если в Базе нет - обновить время создания
        //Добавить в Базу новые файлы
        //--Отсортировать Базу -- пока не применяется. Возможно, раз в час искать перебором будет не так уж и долго
        //Сохранить Базу
        static void Main(string[] args)
        {
            if(Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("Start");

            Config.SilentMode = IsSilent(args);
            
            Config.Open();
            Log.Open(Config.LogsFileName);
            Log.Add("--------------------Запуск программы------------------------");
            CreateFlugFile();

            OpenDB();
            CheckOldFiles();
            CheckNewFiles();

            Program.Close(0);
        }

        static bool IsSilent(string[] args)
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("Check if silent launch");

            if (args.Length < 1)
                return false;
            if (args[0].ToLower() == "-silent")
                return true;

            return false;
        }

        static List<string> DBList;

        static void OpenDB()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("OpenDB");

            if (!File.Exists(Config.DBLocation))
                DBList = new List<string>();
            else
                DBList = new List<string>(File.ReadAllLines(Config.DBLocation));
        }

        static void CheckOldFiles()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("CheckOldFiles");

            for(int i = DBList.Count - 1; i >= 0; i--)
            {
                string FullFileName = Config.Folder + DBList[i];
                if (!File.Exists(FullFileName))//Если старый (уже виденный) файл не существует - убрать его из списка
                    DBList.RemoveAt(i);
            }
        }

        static void CheckNewFiles()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("CheckNewFiles");

            string[] list = Directory.GetFiles(Config.Folder, "*", SearchOption.AllDirectories);

            foreach (string file in list)
            {
                string ShortFileName = file.Remove(0, Config.Folder.Length);

                string SearchResult = DBList.Find(NameInList => NameInList == ShortFileName);
                if (SearchResult != null)//Файл уже есть в списке. Дату создания менять не надо
                    continue;//Идем дальше

                SetNewCreationTime(file, ShortFileName);
            }
        }
        static void SetNewCreationTime(string fileName, string ShortFileName)
        {
            try
            {
                RemoveFlug_ReadOnly(fileName);
                File.SetCreationTime(fileName, DateTime.Now);
                DBList.Add(ShortFileName);
                Log.Add("Найден новый файл: " + fileName);
            }
            catch (Exception ex)
            {
                Log.Add("################### Файл: " + fileName);
                Log.Add("################### Ошибка: " + ex.Message);
                Log.Add("################### Сообщение: Дальнейшая работа программы остановлена до вмешетельства оператора и нахождения причин остановки программы.");
                Log.Add("################### Сообщение: Для предотвращения падений на этой ошибке, найденные причины необходимо сообщить на почту du10 (собака) bk (точка) ru");
                Log.Add("################### Сообщение: Для продолжения работы программы устраните причины возникновения ошибки, и удалите файл !CreationDate_Changer_FlugFile");

                Program.Close(-7);
            }
        }
        

        static void RemoveFlug_ReadOnly(string fileName)
        {
            FileAttributes attributes = File.GetAttributes(fileName);

            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {//Найден флаг "Только чтение"
                //Надо его убрать
                attributes = attributes & ~FileAttributes.ReadOnly;//RemoveAttribute
                File.SetAttributes(fileName, attributes);

                Log.Add("Удален флаг -Только чтение- у файла: " + fileName);
            }
        }

        static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        static void SaveDB()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("SaveDB");

            Directory.CreateDirectory(Path.GetDirectoryName(Config.DBLocation));

            if (DBList == null ||
                DBList.Count == 0)
                return;

            File.WriteAllLines(Config.DBLocation, DBList.ToArray());
        }

        static void CreateFlugFile()
        {
            string FlugFileName = Path.Combine(Config.Folder, "!CreationDate_Changer_FlugFile");

            if (File.Exists(FlugFileName))
            {
                Log.Add("Найден файл !CreationDate_Changer_FlugFile. Его наличие означает, что другой процесс программы CreationDate_Changer еще не закончил свою работу. Прекращаю работу текущего процесса!");
                Close(-6);
            }

            File.Create(FlugFileName).Close();
        }

        static void DeleteFlugFile()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("DeleteFlugFile");

            string FlugFileName = Path.Combine(Config.Folder, "!CreationDate_Changer_FlugFile");
            File.Delete(FlugFileName);
        }

        public static void Close(int ExitCode)
        {
            //-7 = Ошибка при изменении даты создания у одного из файлов
            //-6 = FlugFile уже существует
            //-4 = Не могу сконвертировать максимальный размер лога
            //-3 = Не найден указанный каталог
            //-2 = Не получилось открыть лог
            //-1 = Вы используете папку по умолчанию. Её использовать нельзя. Задайте другое имя папки
            // 1 = Не обнаружен файл конфига. Создан стандартный конфиг

            SaveDB();

            if (ExitCode != -6 &&//-6 - был найден этот файл. Если был найден, не я его создал не мне его и удалять
                ExitCode != -7)  //-7 - Ошибка при изменении даты создания у одного из файлов. Продолжать работу нельзя. Остановить работу до вмешательства оператора и выяснения причин остановки
                DeleteFlugFile();


            if (ExitCode == 0)
                Log.Add("--------------------Завершение программы--------------------");
            else
                Log.Add("--------------------АВАРИЙНОЕ Завершение программы. Код выхода: " + ExitCode + " --------------------");
            
            if (ExitCode != -2)//-2 - не получилось открыть лог. А значит, и закрывать нечего
                Log.Close();

            Environment.Exit(ExitCode);
        }
    }
}
