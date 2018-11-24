using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.SettingsService.Contracts.Asset;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class MtSettingsHelper
    {
        public static async Task<List<AssetContract>> CreateAssets()
        {
            var result = new List<AssetContract>();
            foreach (var asset in MtSettingsData.Assets())
            {
                result.Add(await ClientUtil.AssetsApi.Insert(asset));
            }

            return result;
        }

        public static async Task RemoveAssets()
        {
            foreach (var asset in MtSettingsData.Assets())
            {
                await ClientUtil.AssetsApi.Delete(asset.Id);
            }
        }
    }
}
