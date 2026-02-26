using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BaiTap6
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private string _inputFolder;
        private string _processedFolder;
        private int _intervalSeconds;
        private FileSystemWatcher _watcher;
        
        // Dictionary dùng để khóa các file đang xử lý, tránh xử lý đúp
        private ConcurrentDictionary<string, bool> _processingFiles = new ConcurrentDictionary<string, bool>();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        // Chạy khi Service bắt đầu
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== Windows Service is STARTING ===");
            ReadConfiguration();
            SetupFileSystemWatcher();
            return base.StartAsync(cancellationToken);
        }

        // Chạy khi Service dừng
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== Windows Service is STOPPING ===");
            _watcher?.Dispose();
            return base.StopAsync(cancellationToken);
        }

        // Đọc cấu hình từ Windows Registry (Câu 2)
        private void ReadConfiguration()
        {
            try
            {
                // Mở key trong Registry (Yêu cầu chạy VS Code quyền Admin nếu muốn tạo mới key này trên máy)
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\TradingService"))
                {
                    if (key != null)
                    {
                        _inputFolder = key.GetValue("InputFolder")?.ToString();
                        _processedFolder = key.GetValue("Processed Folder")?.ToString();
                        _intervalSeconds = (int)key.GetValue("IntervalSeconds", 30);
                        _logger.LogInformation("Doc thanh cong cau hinh tu Registry.");
                    }
                    else
                    {
                        // Xử lý khi Key không tồn tại
                        _logger.LogWarning("Khong tim thay Registry key. Su dung gia tri mac dinh.");
                        UseDefaultConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cấu hình không hợp lệ
                _logger.LogError(ex, "Loi khi doc Registry. Su dung gia tri mac dinh.");
                UseDefaultConfig();
            }

            // Đảm bảo thư mục tồn tại
            Directory.CreateDirectory(_inputFolder);
            Directory.CreateDirectory(_processedFolder);
            _logger.LogInformation($"Config: Input={_inputFolder}, Processed={_processedFolder}, Interval={_intervalSeconds}s");
        }

        private void UseDefaultConfig()
        {
            _inputFolder = Path.Combine(AppContext.BaseDirectory, "InputFolder");
            _processedFolder = Path.Combine(AppContext.BaseDirectory, "ProcessedFolder");
            _intervalSeconds = 30;
        }

        // Cài đặt giám sát thư mục (Câu 3)
        private void SetupFileSystemWatcher()
        {
            _watcher = new FileSystemWatcher(_inputFolder, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            // Bắt sự kiện file được tạo mới
            _watcher.Created += async (sender, e) => await ProcessFileAsync(e.FullPath);
        }

        // Hàm xử lý file JSON đến (Đảm bảo Thread Safety)
        private async Task ProcessFileAsync(string filePath)
        {
            // Cờ lê kiểm tra: Nếu file đang được xử lý rồi thì bỏ qua (Prevent double processing)
            if (!_processingFiles.TryAdd(filePath, true)) return;

            try
            {
                // Chờ một chút để OS copy xong file vào thư mục, tránh lỗi file đang bị khóa
                await Task.Delay(500);

                _logger.LogInformation($"[Process] Dang doc file: {filePath}");
                string content = await File.ReadAllTextAsync(filePath); // Đọc nội dung
                
                // Giả lập thời gian xử lý trade file
                await Task.Delay(2000);

                // Di chuyển sang Processed Folder
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(_processedFolder, fileName);

                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(filePath, destPath);

                _logger.LogInformation($"[Success] Da xu ly va di chuyen den: {destPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Loi xu ly file {filePath}: {ex.Message}");
            }
            finally
            {
                // Xử lý xong thì gỡ khóa
                _processingFiles.TryRemove(filePath, out _);
            }
        }

        // Vòng lặp chạy ngầm định kỳ (Câu 1)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Background Processor dang hoat dong luc: {time}", DateTimeOffset.Now);
                // Lặp lại mỗi 30s hoặc theo cấu hình
                await Task.Delay(_intervalSeconds * 1000, stoppingToken);
            }
        }
    }
}