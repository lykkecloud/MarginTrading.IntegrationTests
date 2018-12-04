using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using MarginTrading.IntegrationTests.Infrastructure;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class SqlHelper
    {
        public static async Task ClearHistoryTables()
        {
            using (var conn = new SqlConnection(SettingsUtil.Settings.IntegrationTestSettings.Db.ConnectionString))
            {
                var accountClause = $"WHERE AccountId like '{AccountHelpers.GetAccountIdPrefix}%'";
                
                await conn.ExecuteAsync("DELETE AccountHistory WHERE ClientId = @clientId",
                    new {clientId = AccountHelpers.GetClientId});
                await conn.ExecuteAsync($"DELETE OrdersHistory {accountClause}");
                await conn.ExecuteAsync($"DELETE PositionsHistory {accountClause}");
                await conn.ExecuteAsync($"DELETE Trades {accountClause}");
                await conn.ExecuteAsync($"DELETE Deals {accountClause}");
                await conn.ExecuteAsync($"DELETE OvernightSwapHistory {accountClause}");
            }
        }
    }
}
