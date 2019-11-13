using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace CreationDate_Changer
{
    class Log
    {
        static FileStream fs;
        static StreamWriter file;

        public static void Open(string fileName)
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("Log.Open");

            try
            {
                string LogsFolder = Path.GetDirectoryName(fileName);
                Directory.CreateDirectory(LogsFolder);

                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                file = new StreamWriter(fs);
            }
            catch (Exception ex)
            {
                string Message = "Can not open log file\r\n";
                Message += "FileName: " + fileName + "\r\n";
                Message += "FileMode: Append\r\n";
                Message += "FileAcces: Write\r\n";
                Message += "FileShare: ReadWrite\r\n";
                Message += "\r\n";
                Message += "Error message: " + ex.Message;

                Log.Add(Message);
                Program.Close(-2);
            }
        }

        public static void Add(string Message)
        {
            if (Config.DebugMode)
            {
                System.Windows.Forms.MessageBox.Show("Log.Add");
                System.Windows.Forms.MessageBox.Show(Message);
            }


            string Time = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffffff");
            Message = Time + "# " + Message;

            try
            {//Попытка записать сообщение в файл
                file.WriteLine(Message);
                file.Flush();
            }
            catch (Exception)
            {//Если попытка не удалась
                //if (Config.SilentMode)
                //    return;
                //И если у нас не тихий режим
                MessageBox.Show(Message);
            }
        }

        public static void Close()
        {
            if (Config.DebugMode)
                System.Windows.Forms.MessageBox.Show("LogClose");

            try
            {
                file.Dispose();
                file.Close();

                fs.Dispose();
                fs.Close();

                //Обрезка лога под максимальный размер
                CutLogFile();
            }
            catch (Exception ex)
            {
                string Message = "Can not close log file. Error message: " + ex.Message;

                Log.Add(Message);
            }

        }

        static void CutLogFile()
        {
            long LogSize = new FileInfo(Config.LogsFileName).Length;
            if (LogSize <= Config.MaxLogSize)
                return;


            string tmpFileName = Config.LogsFileName + ".tmp";

            fs = new FileStream(Config.LogsFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            FileStream tmpFile = new FileStream(tmpFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);



            fs.Position = fs.Length - Config.MaxLogSize;
            fs.CopyTo(tmpFile);



            tmpFile.Dispose();
            tmpFile.Close();

            fs.Dispose();
            fs.Close();

            File.Delete(Config.LogsFileName);
            File.Move(tmpFileName, Config.LogsFileName);
        }
    }
}
