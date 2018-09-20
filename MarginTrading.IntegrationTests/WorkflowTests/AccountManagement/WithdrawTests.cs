using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    public class WithdrawTests
    {
        [Test]
        public async Task IfEnoughBalance_ShouldWithdraw()
        {
            // arrange
            await TestsHelpers.EnsureAccountState(neededBalance: 123M);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(TestsHelpers.AccountId,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 123M,
                    Reason = "integration tests: withdraw",
                });

            await Task.WhenAll(
                RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId),
                RabbitUtil.WaitForCqrsMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId));

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(0);
        }
        
        [Test]
        public async Task IfNotEnoughBalance_ShouldFailWithdraw()
        {
            // arrange
            await TestsHelpers.EnsureAccountState(neededBalance: 123);
            (await TestsHelpers.GetAccount()).Balance.Should().Be(123);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(TestsHelpers.AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 124,
                    Reason = "integration tests: withdraw",
                    ReasonType = AccountBalanceChangeReasonTypeContract.Manual,
                });
            
            var eventTask = await Task.WhenAny(
                RabbitUtil.WaitForCqrsMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId),
                RabbitUtil.WaitForCqrsMessage<WithdrawalFailedEvent>(m => m.OperationId == operationId));

            eventTask.GetType().GetGenericArguments().First().Should().Be<WithdrawalFailedEvent>();
            //eventTask.Should().BeOfType<Task<WithdrawalFailedEvent>>();

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(123);
        }
    }
}
