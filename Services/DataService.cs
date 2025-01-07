using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Windows.Storage;
using System.Threading;
using ElectronicCorrectionNotebook.DataStructure;

namespace ElectronicCorrectionNotebook.Services
{
    public static class DataService
    {
        // Data文件夹
        private static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        // json文本文件路径
        private static readonly string JsonFilePath = Path.Combine(appDataPath, "errors.json");
        // rtf富文本路径
        private static readonly string rtfPath = Path.Combine(appDataPath, "rtfFiles");

        static DataService()
        {
            // 确保应用目录存在
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            else if (!Directory.Exists(rtfPath))
            {
                Directory.CreateDirectory(rtfPath);
            }
        }

        // 加载数据 反序列化
        public static async Task<List<ErrorItem>> LoadDataAsync(CancellationToken token)
        {
            if (File.Exists(JsonFilePath))
            {
                var json = await File.ReadAllTextAsync(JsonFilePath, token);
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
            await File.WriteAllTextAsync(JsonFilePath, json, token);
        }
    }
}
