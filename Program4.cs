using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lab4_SystemsProgramming
{
    class Program
    {
        // Biến dùng chung cho Câu 1
        static int sharedCounter = 0;
        static object lockObj = new object();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== MODULE 4: THREAD SAFETY & FILE PROCESSING ===");
                Console.WriteLine("1. Question 1: Race Conditions (Lock vs Interlocked)");
                Console.WriteLine("2. Question 2: Task Synchronization (Task.WhenAll)");
                Console.WriteLine("3. Question 3: Thread-Safe File Access");
                Console.WriteLine("4. Question 4: File Watcher (Monitor & Compress)");
                Console.WriteLine("5. Question 5: Encryption & Compression");
                Console.WriteLine("0. Exit");
                Console.Write("Chon bai tap (0-5): ");
                
                string choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1": RunQuestion1(); break;
                    case "2": await RunQuestion2Async(); break;
                    case "3": RunQuestion3(); break;
                    case "4": RunQuestion4(); break;
                    case "5": RunQuestion5(); break;
                    case "0": return;
                    default: Console.WriteLine("Chon sai, thu lai!"); break;
                }
                
                Console.WriteLine("\nNhan Enter de quay lai menu...");
                Console.ReadLine();
            }
        }

        // --- CÂU 1: RACE CONDITIONS ---
        static void RunQuestion1()
        {
            Console.WriteLine("--- DEMO RACE CONDITION ---");
            sharedCounter = 0;
            List<Task> tasks = new List<Task>();

            // Chạy 5 luồng, mỗi luồng tăng biến đếm 1000 lần
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10000; j++)
                    {
                        // CÁCH 1: KHÔNG AN TOÀN (Sẽ ra kết quả sai)
                        // sharedCounter++; 

                        // CÁCH 2: DÙNG LOCK (An toàn)
                        // lock (lockObj) { sharedCounter++; }

                        // CÁCH 3: DÙNG INTERLOCKED (Tối ưu nhất cho biến số)
                        Interlocked.Increment(ref sharedCounter);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine($"Ket qua mong doi: 50000");
            Console.WriteLine($"Ket qua thuc te : {sharedCounter}");
            Console.WriteLine("(Neu dung Interlocked/Lock thi ket qua phai la 50000)");
        }

        // --- CÂU 2: TASK COORDINATION ---
        static async Task RunQuestion2Async()
        {
            Console.WriteLine("--- DEMO TASK.WHENALL ---");
            var t1 = SimulateWork("Task 1", 2000);
            var t2 = SimulateWork("Task 2", 1000);
            var t3 = SimulateWork("Task 3", 3000);

            Console.WriteLine("Dang cho ca 3 tac vu hoan thanh...");
            await Task.WhenAll(t1, t2, t3);
            
            Console.WriteLine(">>> TAT CA TAC VU DA XONG! CHUONG TRINH TIEP TUC.");
        }

        static async Task SimulateWork(string name, int delay)
        {
            Console.WriteLine($"{name} bat dau chay...");
            await Task.Delay(delay);
            Console.WriteLine($"{name} da xong sau {delay}ms");
        }

        // --- CÂU 3: THREAD-SAFE FILE WRITING ---
        static void RunQuestion3()
        {
            string filePath = "log.txt";
            Console.WriteLine($"--- GHI FILE DA LUONG ({filePath}) ---");
            
            Parallel.For(0, 10, i =>
            {
                string logMessage = $"Thread {Task.CurrentId} viet luc {DateTime.Now:HH:mm:ss.fff}\n";
                
                // Dùng lock để tránh lỗi "File is being used by another process"
                lock (lockObj)
                {
                    File.AppendAllText(filePath, logMessage);
                    Console.WriteLine($"Thread {Task.CurrentId} da ghi.");
                }
            });
            Console.WriteLine("Ghi file hoan tat!");
        }

        // --- CÂU 4: FILE MONITORING ---
        static void RunQuestion4()
        {
            string watchFolder = Path.Combine(Directory.GetCurrentDirectory(), "InputFolder");
            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "OutputFolder");
            Directory.CreateDirectory(watchFolder);
            Directory.CreateDirectory(outputFolder);

            Console.WriteLine($"--- GIAM SAT THU MUC: {watchFolder} ---");
            Console.WriteLine("Hay tao/copy mot file text vao thu muc InputFolder de test.");
            
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = watchFolder;
                watcher.Filter = "*.txt"; // Chỉ theo dõi file txt
                watcher.EnableRaisingEvents = true;

                watcher.Created += (sender, e) =>
                {
                    Console.WriteLine($"[Phat hien] File moi: {e.Name}");
                    ProcessFile(e.FullPath, outputFolder);
                };

                Console.WriteLine("Nhan Enter de dung giam sat...");
                Console.ReadLine();
            }
        }

        static void ProcessFile(string inputPath, string outputFolder)
        {
            try
            {
                // Đợi 1 chút để file copy xong hẳn (tránh lỗi file lock)
                Thread.Sleep(500); 

                string content = File.ReadAllText(inputPath);
                string zipPath = Path.Combine(outputFolder, Path.GetFileName(inputPath) + ".gz");

                using (FileStream originalFileStream = File.OpenRead(inputPath))
                using (FileStream compressedFileStream = File.Create(zipPath))
                using (GZipStream compressor = new GZipStream(compressedFileStream, CompressionMode.Compress))
                {
                    originalFileStream.CopyTo(compressor);
                }
                Console.WriteLine($"[Thanh cong] Da nen file sang: {zipPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Loi] {ex.Message}");
            }
        }

        // --- CÂU 5: ENCRYPTION & COMPRESSION ---
        static void RunQuestion5()
        {
            Console.WriteLine("--- MA HOA & NEN DU LIEU ---");
            string originalText = "Day la du lieu bi mat can duoc bao ve!!!";
            string filePath = "secure_data.bin";

            // Tạo khóa ngẫu nhiên cho AES
            using (Aes aes = Aes.Create())
            {
                // 1. Mã hóa -> Nén -> Lưu file
                EncryptAndCompress(originalText, filePath, aes.Key, aes.IV);
                Console.WriteLine($"Da ma hoa va luu vao {filePath}");

                // 2. Đọc file -> Giải nén -> Giải mã
                string decryptedText = DecompressAndDecrypt(filePath, aes.Key, aes.IV);
                Console.WriteLine($"Giai ma duoc: {decryptedText}");
            }
        }

        static void EncryptAndCompress(string text, string filePath, byte[] key, byte[] iv)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress)) // Nén
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                // Mã hóa
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (CryptoStream cryptoStream = new CryptoStream(gzipStream, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cryptoStream))
                {
                    sw.Write(text);
                }
            }
        }

        static string DecompressAndDecrypt(string filePath, byte[] key, byte[] iv)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress)) // Giải nén
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                // Giải mã
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (CryptoStream cryptoStream = new CryptoStream(gzipStream, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cryptoStream))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}