using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Lab2_SystemProgramming
{
    // --- Định nghĩa cho CÂU 1 & 2 ---
    // Struct (Value Type)
    struct PointStruct
    {
        public int X, Y;
    }

    // Class (Reference Type)
    class PointClass
    {
        public int X, Y;
    }

    class Program
    {
        // Import hàm API của Windows để cài Font (Dùng cho Câu 5)
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        public static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        static void Main(string[] args)
        {
            // Chạy lần lượt các bài
            RunQuestion1();
            Console.WriteLine(); 
            
            RunQuestion2();
            Console.WriteLine();
            
            RunQuestion5();
            
            // Dừng màn hình để xem kết quả
            Console.WriteLine("\nNhan phim bat ky de ket thuc...");
            Console.ReadLine();
        }

        // --- CÂU 1: SO SÁNH STRUCT VÀ CLASS ---
        static void RunQuestion1()
        {
            Console.WriteLine("=== QUESTION 1: VALUE TYPE VS REFERENCE TYPE ===");
            
            // 1. Test Struct (Copy giá trị)
            PointStruct s1 = new PointStruct { X = 10, Y = 10 };
            PointStruct s2 = s1; // Copy toàn bộ dữ liệu ra vùng nhớ mới
            s2.X = 999; // Sửa s2 không ảnh hưởng s1

            Console.WriteLine("[Struct] s1.X: " + s1.X + " (Giu nguyen)");
            Console.WriteLine("[Struct] s2.X: " + s2.X + " (Da thay doi)");

            // 2. Test Class (Copy tham chiếu/địa chỉ)
            PointClass c1 = new PointClass { X = 10, Y = 10 };
            PointClass c2 = c1; // Trỏ cùng vào một vùng nhớ
            c2.X = 999; // Sửa c2 làm thay đổi cả c1

            Console.WriteLine("[Class ] c1.X: " + c1.X + " (Bi thay doi theo)");
            Console.WriteLine("[Class ] c2.X: " + c2.X + " (Da thay doi)");
        }

        // --- CÂU 2: ĐO HIỆU NĂNG VÀ BỘ NHỚ ---
        static void RunQuestion2()
        {
            Console.WriteLine("=== QUESTION 2: MEMORY & PERFORMANCE ===");
            int size = 1000000; // 1 triệu phần tử
            
            // 1. Đo Struct
            long startMem = GC.GetTotalMemory(true);
            PointStruct[] structArray = new PointStruct[size];
            // Struct được cấp phát liền mạch, không cần khởi tạo từng phần tử
            long endMem = GC.GetTotalMemory(true);
            Console.WriteLine($"[Struct Array] Memory used: {(endMem - startMem) / 1024 / 1024} MB");

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < size; i++) { var temp = structArray[i].X; }
            sw.Stop();
            Console.WriteLine($"[Struct Array] Access Time: {sw.ElapsedMilliseconds} ms");

            // Dọn dẹp bộ nhớ
            structArray = null;
            GC.Collect();

            // 2. Đo Class
            startMem = GC.GetTotalMemory(true);
            PointClass[] classArray = new PointClass[size];
            for (int i = 0; i < size; i++) { classArray[i] = new PointClass(); } // Phải cấp phát từng object
            endMem = GC.GetTotalMemory(true);
            Console.WriteLine($"[Class Array ] Memory used: {(endMem - startMem) / 1024 / 1024} MB");

            sw.Restart();
            for (int i = 0; i < size; i++) { var temp = classArray[i].X; }
            sw.Stop();
            Console.WriteLine($"[Class Array ] Access Time: {sw.ElapsedMilliseconds} ms");
        }

        // --- CÂU 5: IN THẺ THÀNH VIÊN ---
        static void RunQuestion5()
        {
            Console.WriteLine("=== QUESTION 5: MEMBERSHIP CARD PRINTER ===");
            
            // Logic cài đặt Font (Giả lập)
            string fontName = "Arial.ttf"; // Dùng tạm font có sẵn để demo không lỗi
            string sysFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontName);

            try
            {
                if (File.Exists(sysFontPath))
                {
                    Console.WriteLine("[System] Font da ton tai hoac duoc he dieu hanh ho tro.");
                }
                else
                {
                    // Code cài font thực tế cần quyền Admin để chạy lệnh File.Copy vào thư mục Fonts
                    Console.WriteLine("[System] Font chua duoc cai dat. Can quyen Admin de cai dat.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] Loi kiem tra font: " + ex.Message);
            }

            // In thẻ
            Console.WriteLine("\n--------------------------------");
            Console.WriteLine("|           UEH CARD           |");
            Console.WriteLine("|                              |");
            Console.WriteLine($"|  ID: {"40181 700982",-16}|");
            Console.WriteLine($"|  NAME: {"John Smith",-14}|");
            Console.WriteLine($"|  LEVEL: {"VIP MEMBER",-13}|");
            Console.WriteLine("|                              |");
            Console.WriteLine("--------------------------------");
        }
    }
}