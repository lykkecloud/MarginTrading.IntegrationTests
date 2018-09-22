using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    [NonParallelizable]
    public class AccountManagementTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var sett = SettingsUtil.Settings.IntegrationTestSettings;
            var connectionString = sett.Cqrs.ConnectionString;
            var eventsExchange = $"{sett.Cqrs.EnvironmentName}.{sett.Cqrs.ContextNames.AccountsManagement}.events.exchange";
            
            //cqrs messages subscription
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<DepositSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalFailedEvent>(connectionString, eventsExchange);

            //other messages subscription
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(sett.MessagingDelay);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            //todo dispose listeners
        }
        
        #region Charge manually
        
        [Test]
        public async Task EnsureAccountState_ShouldFixAccount()
        {
            // arrange

            // act
            var result = await AccountHelpers.EnsureAccountState(neededBalance: 13);

            // assert
            var account = await AccountHelpers.GetAccount();
            account.Should().BeEquivalentTo(new
            {
                ClientId = AccountHelpers.ClientId,
                Id = AccountHelpers.AccountId,
                Balance = 13,
                IsDisabled = false,
            }, o => o.ExcludingMissingMembers());
            
            result.Should().BeEquivalentTo(account, o => o.Excluding(x => x.ModificationTimestamp));
        }

        [TestCase(-10000)]
        [TestCase(10000)]
        public async Task ChargeManually_ShouldUpdateBalance(decimal delta)
        {
            // arrange
            await AccountHelpers.EnsureAccountState();

            // act
            await AccountHelpers.ChargeManually(delta);

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(0 + delta);
        }
        
        #endregion Charge manually
        
        #region Position close
        
        [TestCase(-10000)]
        [TestCase(10000)]
        public async Task PositionClose_ShouldUpdateBalance(decimal delta)
        {
            // arrange
            await AccountHelpers.EnsureAccountState();
            var operationId = Guid.NewGuid().ToString();
            
            // act
            //todo use specific command
            CqrsUtil.SendEventToAccountManagement(new Backend.Contracts.Events.PositionClosedEvent(
                accountId: AccountHelpers.AccountId, 
                clientId: AccountHelpers.ClientId, 
                positionId: operationId,
                assetPairId: "testAssetPair",
                balanceDelta: delta));

            await RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                m.BalanceChange.Id == operationId + "-update-balance");

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(0 + delta);
        }
        
        #endregion Position close
        
        #region Deposit
        
        [Test]
        public async Task Deposit_Success()
        {
            // arrange
            await AccountHelpers.EnsureAccountState();

            // act
            var operationId = await ClientUtil.AccountsApi.BeginDeposit(AccountHelpers.AccountId,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 123,
                    Reason = "integration tests: deposit",
                });
            
            var messagesReceivedTask = Task.WhenAll(
                RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId),
                RabbitUtil.WaitForMessage<DepositSucceededEvent>(m => m.OperationId == operationId));

            await messagesReceivedTask;

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(123);
        }
        
        #endregion Deposit
        
        #region Withdrawal
        
        [Test]
        public async Task IfEnoughBalance_ShouldWithdraw()
        {
            // arrange
            await AccountHelpers.EnsureAccountState(neededBalance: 123M);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(AccountHelpers.AccountId,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 123M,
                    Reason = "integration tests: withdraw",
                });

            await Task.WhenAll(
                RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId),
                RabbitUtil.WaitForMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId));

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(0);
        }
        
        [Test]
        public async Task IfNotEnoughBalance_ShouldFailWithdraw()
        {
            // arrange
            await AccountHelpers.EnsureAccountState(neededBalance: 123);
            (await AccountHelpers.GetAccount()).Balance.Should().Be(123);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(AccountHelpers.AccountId,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 124,
                    Reason = "integration tests: withdraw",
                });
            
            var eventTask = await Task.WhenAny(
                RabbitUtil.WaitForMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId),
                RabbitUtil.WaitForMessage<WithdrawalFailedEvent>(m => m.OperationId == operationId));

            eventTask.GetType().GetGenericArguments().First().Should().Be<WithdrawalFailedEvent>();
            //eventTask.Should().BeOfType<Task<WithdrawalFailedEvent>>();

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(123);
        }
        
        #endregion Withdrawal
    }
}
