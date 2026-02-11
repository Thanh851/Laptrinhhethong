using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lab5_IPC_RPC
{
    // Class dùng cho JSON-RPC (Câu 4 & 5)
    public class RpcRequest
    {
        public string Id { get; set; }
        public string Method { get; set; }
        public object[] Params { get; set; }
    }

    public class RpcResponse
    {
        public string Id { get; set; }
        public object Result { get; set; }
        public string Error { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== MODULE 5: IPC & RPC DEMO ===");
            Console.WriteLine("1. Named Pipes (Local IPC - Cau 2)");
            Console.WriteLine("2. TCP Sockets (Network IPC - Cau 3)");
            Console.WriteLine("3. JSON-RPC (Remote Procedure Call - Cau 4 & 5)");
            Console.Write("Chon bai tap (1-3): ");
            string mode = Console.ReadLine();

            Console.WriteLine("\nBan muon chay voi vai tro gi?");
            Console.WriteLine("S. Server (Nguoi phuc vu/Lang nghe)");
            Console.WriteLine("C. Client (Khach hang/Gui yeu cau)");
            Console.Write("Chon (S/C): ");
            string role = Console.ReadLine().ToUpper();

            if (mode == "1")
            {
                if (role == "S") RunPipeServer();
                else RunPipeClient();
            }
            else if (mode == "2")
            {
                if (role == "S") RunTcpServer();
                else RunTcpClient();
            }
            else if (mode == "3")
            {
                // RPC chạy trên nền TCP
                if (role == "S") RunRpcServer();
                else RunRpcClient();
            }
            else
            {
                Console.WriteLine("Lua chon khong hop le.");
            }
        }

        // --- CÂU 2: NAMED PIPES ---
        static void RunPipeServer()
        {
            Console.WriteLine("--- PIPE SERVER DANG CHO KET NOI ---");
            using (var server = new NamedPipeServerStream("testpipe"))
            {
                server.WaitForConnection(); // Chờ Client kết nối
                Console.WriteLine("[Server] Client da ket noi!");

                using (var reader = new StreamReader(server))
                using (var writer = new StreamWriter(server) { AutoFlush = true })
                {
                    // Đọc tin nhắn từ Client
                    string msg = reader.ReadLine();
                    Console.WriteLine($"[Server] Nhan duoc: {msg}");

                    // Phản hồi lại
                    string response = "Server da nhan: " + msg + " (Xu ly boi Pipe)";
                    writer.WriteLine(response);
                }
            }
            Console.WriteLine("[Server] Ket thuc phien.");
        }

        static void RunPipeClient()
        {
            Console.WriteLine("--- PIPE CLIENT DANG KET NOI ---");
            using (var client = new NamedPipeClientStream(".", "testpipe", PipeDirection.InOut))
            {
                try
                {
                    client.Connect(2000); // Thử kết nối trong 2s
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    using (var reader = new StreamReader(client))
                    {
                        Console.Write("Nhap tin nhan gui Server: ");
                        string msg = Console.ReadLine();
                        writer.WriteLine(msg); // Gửi đi

                        string response = reader.ReadLine(); // Đọc phản hồi
                        Console.WriteLine($"[Client] Server tra loi: {response}");
                    }
                }
                catch (Exception) { Console.WriteLine("[Loi] Khong tim thay Server! Hay chay Server truoc."); }
            }
        }

        // --- CÂU 3: TCP SOCKETS ---
        static void RunTcpServer()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("--- TCP SERVER DANG LANG NGHE TAI PORT 5000 ---");

            while (true)
            {
                using (TcpClient client = server.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    Console.WriteLine("[Server] Client Connected!");
                    string msg = reader.ReadLine();
                    Console.WriteLine($"[Server] Nhan: {msg}");

                    writer.WriteLine("TCP Response: " + msg.ToUpper());
                }
                break; // Demo chạy 1 lần rồi thoát cho đơn giản
            }
        }

        static void RunTcpClient()
        {
            try
            {
                using (TcpClient client = new TcpClient("127.0.0.1", 5000))
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                using (StreamReader reader = new StreamReader(stream))
                {
                    Console.Write("Nhap tin nhan TCP: ");
                    string msg = Console.ReadLine();
                    writer.WriteLine(msg);

                    string response = reader.ReadLine();
                    Console.WriteLine($"[Client] Server phan hoi: {response}");
                }
            }
            catch { Console.WriteLine("[Loi] Khong ket noi duoc TCP Server (Port 5000)."); }
        }

        // --- CÂU 4 & 5: SIMPLE JSON-RPC ---
        static void RunRpcServer()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 6000);
            server.Start();
            Console.WriteLine("--- RPC SERVER (MoneyExchange) READY AT PORT 6000 ---");

            while (true)
            {
                using (TcpClient client = server.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // 1. Đọc JSON Request
                    string jsonRequest = reader.ReadLine();
                    Console.WriteLine($"[Log] Raw Request: {jsonRequest}");
                    
                    RpcResponse response = new RpcResponse();
                    try
                    {
                        var req = JsonSerializer.Deserialize<RpcRequest>(jsonRequest);
                        response.Id = req.Id;

                        // 2. Xử lý hàm (Router)
                        if (req.Method == "MoneyExchange")
                        {
                            // Params: [Currency, Amount] -> VD: ["USD", 100]
                            string currency = req.Params[0].ToString();
                            double amount = double.Parse(req.Params[1].ToString());
                            
                            // Giả lập logic đổi tiền
                            double rate = (currency == "USD") ? 25000 : 1;
                            double resultVND = amount * rate;
                            
                            response.Result = $"{amount} {currency} = {resultVND} VND";
                        }
                        else
                        {
                            response.Error = "Method not found";
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Error = "Internal Error: " + ex.Message;
                    }

                    // 3. Gửi JSON Response
                    string jsonResponse = JsonSerializer.Serialize(response);
                    writer.WriteLine(jsonResponse);
                }
                break; // Chạy 1 request rồi thoát để demo
            }
        }

        static void RunRpcClient()
        {
            try
            {
                using (TcpClient client = new TcpClient("127.0.0.1", 6000))
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                using (StreamReader reader = new StreamReader(stream))
                {
                    // 1. Tạo Request giả lập hàm MoneyExchange("USD", 50)
                    var request = new RpcRequest
                    {
                        Id = Guid.NewGuid().ToString(),
                        Method = "MoneyExchange",
                        Params = new object[] { "USD", 50 }
                    };

                    string jsonString = JsonSerializer.Serialize(request);
                    Console.WriteLine($"[Client] Sending RPC: {jsonString}");
                    writer.WriteLine(jsonString);

                    // 2. Nhận kết quả
                    string jsonResponse = reader.ReadLine();
                    var response = JsonSerializer.Deserialize<RpcResponse>(jsonResponse);

                    Console.WriteLine($"[Client] Ket qua RPC: {response.Result}");
                    if (response.Error != null) Console.WriteLine($"[Loi RPC]: {response.Error}");
                }
            }
            catch { Console.WriteLine("[Loi] Khong ket noi duoc RPC Server (Port 6000)."); }
        }
    }
}