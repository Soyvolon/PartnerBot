using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PartnerBot.Core.Entities
{
    public static class DevelopmentStressTestDataManager
    {
        public static string path = Path.Join("Config", "partner_sender_stress_test_config.json");
        
        /// <summary>
        /// Gets the data for a stress test.
        /// </summary>
        /// <returns>A list of Development Sterss Test Channels.</returns>
        public static async Task<List<DevelopmentStressTestChannel>?> GetDataAsync()
        {
            try
            {
                await using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                List<DevelopmentStressTestChannel>? res = await JsonSerializer.DeserializeAsync<List<DevelopmentStressTestChannel>>(fs);

                return res;
            }
            catch { return null; }
        }

        /// <summary>
        /// Saves a list of stress test channels for later use.
        /// </summary>
        /// <param name="data">A list of stress test channels.</param>
        /// <returns>The save Task</returns>
        public static async Task SaveDataAsync(List<DevelopmentStressTestChannel> data)
        {
            string? json = JsonSerializer.Serialize(data);

            await File.WriteAllTextAsync(path, json);
        }
    }
}
