using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Windows.Storage;
using System.Threading;




namespace ElectronicCorrectionNotebook.Services
{
    public static class DataService
    {
        private static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private static readonly string filePath = Path.Combine(appDataPath, "errors.json");

        static DataService()
        {
            // 确保应用目录存在
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }

        // 加载数据 反序列化
        public static async Task<List<ErrorItem>> LoadDataAsync(CancellationToken token)
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath, token);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonSerializer.Deserialize<List<ErrorItem>>(json);
                }
            }
            return new List<ErrorItem>();
        }

        // 保存数据 序列化
        public static async Task SaveDataAsync(List<ErrorItem> errorItems, CancellationToken token)
        {
            var json = JsonSerializer.Serialize(errorItems);
            await File.WriteAllTextAsync(filePath, json, token);
        }
    }
}
