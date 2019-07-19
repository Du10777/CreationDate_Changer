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
            Config.SilentMode = IsSilent(args);
            
            Config.Open();
            Log.Open(Config.LogsFileName);
            Log.Add("--------------------Запуск программы------------------------");
            CreateFlugFile();

            OpenDB();
            CheckOldFiles();
            CheckNewFiles();

            SaveDB();

            DeleteFlugFile();
            Log.Add("--------------------Завершение программы--------------------");
            Log.Close();
        }

        static bool IsSilent(string[] args)
        {
            if (args.Length < 1)
                return false;
            if (args[0].ToLower() == "-silent")
                return true;

            return false;
        }

        static List<string> DBList;

        static void OpenDB()
        {
            if (!File.Exists(Config.DBLocation))
                DBList = new List<string>();
            else
                DBList = new List<string>(File.ReadAllLines(Config.DBLocation));
        }

        static void CheckOldFiles()
        {
            for(int i = DBList.Count - 1; i >= 0; i--)
            {
                string FullFileName = Config.Folder + DBList[i];
                if (!File.Exists(FullFileName))//Если старый (уже виденный) файл не существует - убрать его из списка
                    DBList.RemoveAt(i);
            }
        }

        static void CheckNewFiles()
        {
            string[] list = Directory.GetFiles(Config.Folder, "*", SearchOption.AllDirectories);

            foreach (string file in list)
            {
                string ShortFileName = file.Remove(0, Config.Folder.Length);

                string SearchResult = DBList.Find(NameInList => NameInList == ShortFileName);
                if (SearchResult != null)//Файл уже есть в списке. Дату создания менять не надо
                    continue;//Идем дальше

                File.SetCreationTime(file, DateTime.Now);
                DBList.Add(ShortFileName);
                Log.Add("Найден новый файл: " + file);
            }
        }

        static void SaveDB()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Config.DBLocation));

            File.WriteAllLines(Config.DBLocation, DBList.ToArray());
        }

        static void CreateFlugFile()
        {
            string FlugFileName = Path.Combine(Config.Folder, "CreationDate_Changer_FlugFile");

            if (File.Exists(FlugFileName))
            {
                Log.Add("Найден файл CreationDate_Changer_FlugFile. Его наличие означает, что другой процесс программы CreationDate_Changer еще не закончил свою работу. Прекращаю работу текущего процесса!");
                Environment.Exit(-7);
            }

            File.Create(FlugFileName).Close();
        }

        static void DeleteFlugFile()
        {
            string FlugFileName = Path.Combine(Config.Folder, "CreationDate_Changer_FlugFile");
            File.Delete(FlugFileName);
        }
    }
}
