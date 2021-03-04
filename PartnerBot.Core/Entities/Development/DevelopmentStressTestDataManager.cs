using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PartnerBot.Core.Entities
{
    public static class DevelopmentStressTestDataManager
    {
        public static string path = Path.Join("Config", "partner_sender_stress_test_config.json");

        public static async Task<List<DevelopmentStressTestChannel>?> GetDataAsync()
        {
            try
            {
                await using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                var res = await JsonSerializer.DeserializeAsync<List<DevelopmentStressTestChannel>>(fs);

                return res;
            }
            catch { return null; }
        }

        public static async Task SaveDataAsync(List<DevelopmentStressTestChannel> data)
        {
            var json = JsonSerializer.Serialize(data);

            await File.WriteAllTextAsync(path, json);
        }
    }
}
