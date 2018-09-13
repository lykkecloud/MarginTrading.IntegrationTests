using System;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    public class DepositTests
    {
        [Test]
        public async Task Always_ShouldDeposit()
        {
            // arrange
            await TestsHelpers.EnsureAccountState();

            // act

            var operationId = await ClientUtil.AccountsApi.BeginDeposit(TestsHelpers.ClientId,
                TestsHelpers.AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 123,
                    Reason = "integration tests: deposit",
                    ReasonType = AccountBalanceChangeReasonTypeContract.Manual,
                });
            
            var messagesReceivedTask = Task.WhenAll(
                RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId),
                RabbitUtil.WaitForCqrsMessage<DepositSucceededEvent>(m => m.OperationId == operationId));

            await messagesReceivedTask;

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(123);
        }
    }
}