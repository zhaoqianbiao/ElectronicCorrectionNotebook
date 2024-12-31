using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;


namespace ElectronicCorrectionNotebook.Services
{
    public static class DataService
    {
        private static readonly string filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "errors.json");

        // 加载数据 反序列化
        public static async Task<List<ErrorItem>> LoadDataAsync()
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonSerializer.Deserialize<List<ErrorItem>>(json);
                }
            }
            return new List<ErrorItem>();
        }


        // 保存数据 序列化
        public static async Task SaveDataAsync(List<ErrorItem> errorItems)
        {
            var json = JsonSerializer.Serialize(errorItems);
            await File.WriteAllTextAsync(filePath, json);
        }

    }
}
