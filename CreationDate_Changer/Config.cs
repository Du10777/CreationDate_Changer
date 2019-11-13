using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace CreationDate_Changer
{
    class Config
    {
        //Проверка есть ли файл конфига
        //Если нет - создать шаблонный пример и сообщить про это
        //попытаться его считать
        //Если были ошибки во время чтения конфига - вывести сообщения и закрыть прогу
        //Проверить не стандартный ли это конфиг
        //Если стандартный - вывести сообщения и закрыть прогу

        public static bool SilentMode = false;

        public static void Open()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("Config.Open");

            string exeFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string CfgFileName = exeFileName.Remove(exeFileName.Length - 4) + ".cfg";

            if (!File.Exists(CfgFileName))
            {
                CreateDefaultConfig(CfgFileName);
                Log.Add("Не обнаружен файл конфига. Создан стандартный конфиг");
                Program.Close(1);
            }

            ReadConfig(CfgFileName);
            CheckConfig();
        }

        static void CreateDefaultConfig(string CfgFileName)
        {
            List<string> lines = new List<string>();
            lines.Add("# Эта программа ищет новые (ранее не виденные) файлы в указанном каталоге, и меняет в них дату создания на текущую дату");
            lines.Add("");
            lines.Add("# Запуск с ключом -silent отключает вывод выскакивающих сообщений об ошибке, которые не получилось записать в лог");
            lines.Add("# Если такие сообщения и будут - они не куда не выведутся и программа продолжит работу (либо закроется, если ошибка была фатальной)");
            lines.Add("");
            lines.Add("# Каталог, в котором надо обновлять дату создания Новых файлов, которые программа еще не видела");
            lines.Add("Folder: " + defaultFolder);
            lines.Add("");
            lines.Add("# Путь к файлу со списком уже виденных файлов.");
            lines.Add("# Если его не указать - список будет храниться рядом с файлом программы.");
            lines.Add("# В этом случае убедитесь, что у пользователя (от имени которого запускается программа) есть право на запись в этот каталог");
            lines.Add("DBLocation: " + defaultDBLocation);
            lines.Add("");
            lines.Add("# Путь к файлу с логами. Не обязательный параметр.");
            lines.Add("# Если его не указать - логи будут храниться рядом с файлом программы.");
            lines.Add("# В этом случае убедитесь, что у пользователя (от имени которого запускается программа) есть право на запись в этот каталог");
            lines.Add("Logs: " + defaultLogsFileName);
            lines.Add("");
            lines.Add("# Максимальный размер лога (в байтах).");
            lines.Add("# при его превышении будет удален кусок файла из начала, что бы общий размер не превышал заданного максимального размера.");
            lines.Add("# Выполняется во время завершения работы программы");
            lines.Add("MaxLogSize: " + defaultMaxLogSize);

            try
            {
                File.WriteAllLines(CfgFileName, lines.ToArray());
            }
            catch (Exception ex)
            {
                Log.Add("Не могу сохранить шаблонный конфиг. " + ex.Message);
            }


            string Message = "Файл конфигурации не обнаружен.\r\n";
            Message += "Создан шаблонный файл конфигурации.\r\n";
            Message += "Ознакомтесь с ним и отредактируйте под свои нужды.\r\n";
            Message += "Путь к файлу:\r\n";
            Message += CfgFileName;
            MessageBox.Show(Message);
        }




        static void ReadConfig(string CfgFileName)
        {
            string[] lines = File.ReadAllLines(CfgFileName);

            foreach (string line in lines)
                ReadConfigLine(line);
        }
        static void ReadConfigLine(string line)
        {
            if (line.Length == 0)//Пропуск пустых строк
                return;
            if (line.StartsWith("#"))//Пропуск комментария
                return;

            string ValueName = line.Split(':')[0];
            switch (ValueName)
            {
                case "Folder":
                    Folder = GetValue(line);
                    break;
                case "DBLocation":
                    DBLocation = GetValue(line);
                    break;
                case "Logs":
                    LogsFileName = GetValue(line);
                    break;
                case "MaxLogSize":
                    MaxLogSize = StringToULong(GetValue(line));
                    break;
                default:
                    break;
            }
        }

        static string GetValue(string line)
        {
            int ValueNameLength = line.Split(':')[0].Length;//Узнать длинну имени параметра
            string result = line.Remove(0, ValueNameLength + 1);//Удалить из строки имя параметра + символ двоеточия

            //Если вначале строки остались пробелы - удалять их пока они не исчезнут
            while (true)
            {
                if (result.StartsWith(" "))
                    result = result.Remove(0, 1);
                else
                    break;
            }

            return result;
        }

        static long StringToULong(string Value)
        {
            long result = 0;

            try
            {
                result = Convert.ToInt64(Value);
            }
            catch (Exception)
            {
                string Message = "Не могу сконвертировать максимальный размер лога. Пришел параметр:\r\n";
                Message += Value + "\r\n";
                Log.Add(Message);

                Program.Close(-4);
            }

            return result;
        }



        static void CheckConfig()
        {
            if (Folder == defaultFolder)
            {
                Log.Add("Вы используете папку по умолчанию. Её использовать нельзя. Задайте другое имя папки");
                Program.Close(-1);
            }
            if (!Directory.Exists(Folder))
            {
                Log.Add("Не найден указанный каталог: " + Folder);
                Program.Close(-3);
            }
            if (!Folder.EndsWith("\\"))
            {//добавить последний слеш, если его нет
                Folder += '\\';
            }

            if (DBLocation.Length == 0)
            {
                string exeFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DBLocation = exeFileName.Remove(exeFileName.Length - 4) + ".list";
            }
            if (LogsFileName.Length == 0)
            {
                string exeFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                LogsFileName = exeFileName.Remove(exeFileName.Length - 4) + ".log";
            }
            if (MaxLogSize < 0)
            {
                Log.Add("Не задан максимальный размер лога. Будет использоваться размер лога по умолчанию: 10 Мб");
                MaxLogSize = defaultMaxLogSize;
            }
        }


        public static string Folder;
        public static string DBLocation;
        public static string LogsFileName;
        public static long MaxLogSize = -1;
        public static bool DebugMode = false;

        static string defaultFolder = @"D:\ExampleFolder";
        static string defaultDBLocation = @"D:\CreationDate_Changer_DB.txt";
        static string defaultLogsFileName = @"D:\ExampleLogs.txt";
        static long defaultMaxLogSize = 10 * 1024 * 1024;
    }
}
