using System.Collections.Generic;
using MarginTrading.SettingsService.Contracts.Asset;

namespace MarginTrading.IntegrationTests
{
    public static class MtSettingsData
    {
        public static IEnumerable<AssetContract> Assets()
        {
            yield return new AssetContract {Id = "IT1", Name = "IT1", Accuracy = 5};
            yield return new AssetContract {Id = "IT2", Name = "IT2", Accuracy = 6};
            yield return new AssetContract {Id = "IT3", Name = "IT3", Accuracy = 7};
        }
        
        
    }
}
