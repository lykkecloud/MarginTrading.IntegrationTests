using System;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    public class CloseOrderTests
    {
        [TestCase(-10000)]
        [TestCase(10000)]
        public async Task Always_ShouldUpdateBalance(decimal delta)
        {
            // arrange
            await TestsHelpers.EnsureAccountState();
            var operationId = Guid.NewGuid().ToString();
            
            // act
            //todo use specific command
            CqrsUtil.SendEventToAccountManagement(new Backend.Contracts.Events.PositionClosedEvent(
                accountId: TestsHelpers.AccountId, 
                clientId: TestsHelpers.ClientId, 
                positionId: operationId,
                assetPairId: "testAssetPair",
                balanceDelta: delta));

            await RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m =>
                m.BalanceChange.Id == operationId + "-update-balance");

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(0 + delta);
        }
    }
}
