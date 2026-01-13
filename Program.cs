using System;
using System.IO;

namespace SystemInfoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // In tieu de
            Console.WriteLine("=== THONG TIN HE THONG ===");

            // 1. In phien ban he dieu hanh
            Console.WriteLine("Phien ban OS: " + Environment.OSVersion);

            // 2. In thu muc hien tai
            Console.WriteLine("Thu muc hien tai: " + Environment.CurrentDirectory);

            // 3. In thoi gian he thong
            Console.WriteLine("Thoi gian hien tai: " + DateTime.Now);

            // Ket thuc chuong trinh
            Console.WriteLine("==========================");
        }
    }
}