using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ControlThreads3105
{
    delegate void FileObserver(FileCreator file);
    class FileWatcher
    {
        private FileCreator file;
        public event FileObserver FileChanged;

        public FileWatcher(FileCreator file)
        {
            this.file= file;
        }
        private readonly object lockObject = new object();
        public void Start()
        {
            Thread t = new Thread(() =>
            {
                DateTime currentModTime = file.TextFileObject.LastWriteTime;
                while (true)
                {
                    lock (lockObject)
                    {
                        using (StreamReader reader = new StreamReader(file.TextFileObject.FullName))
                        {
                            if (currentModTime != file.TextFileObject.LastWriteTime)
                            {
                                currentModTime = file.TextFileObject.LastWriteTime;
                                FileChanged?.Invoke(file);
                            }
                        }
                    }
                }
            });
            t.Start();
        }
    }

    class FileCreator
    {
        public FileInfo TextFileObject { get; set; }
        public FileStream TextFileStream { get; set; }
        public FileCreator()
        {
            TextFileObject = new FileInfo("test.txt");
            TextFileStream = TextFileObject.Create();
            TextFileStream.Close();

            using (StreamWriter writer = new StreamWriter(TextFileObject.FullName))
            {
                writer.Write("0");
            }
        }

        private static readonly object lockObject = new object();
        public void WriteFile(FileInfo file, string data)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                lock (lockObject)
                {
                    using (StreamWriter writer = new StreamWriter(file.FullName))
                    {
                        writer.Write(data);
                    }
                }
            });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Threads control");
            
            FileCreator fileObject = new FileCreator();
            FileWatcher fw = new FileWatcher(fileObject);
            fw.FileChanged += OnFileChanged;
            fw.Start();

            while (true)
            {
                Console.WriteLine("Do you want to change file content to 1");
            }

            Console.ReadLine();
        }


        private static void OnFileChanged(FileCreator file)
        {
            string content;

            using (StreamReader reader = new StreamReader(file.TextFileObject.FullName))
            {
                content = reader.Read().ToString();
            }
            Console.WriteLine($"Content: {content}\nLast changed: {file.TextFileObject.LastWriteTime.ToString()}");
            if (content == "1")
            {
                file.WriteFile(file.TextFileObject, "0");
                Thread.Sleep(10000);
                Console.WriteLine("File content changed to 0 ten seconds ago");
            }
        }
    }

}
