using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.IntegrationTests.Helpers;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    //[NonParallelizable] //used assembly-wide non-parallelism
    public class AccountManagementTests
    {
        private readonly IntegrationTestSettings _settings = SettingsUtil.Settings.IntegrationTestSettings;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var connectionString = _settings.Cqrs.ConnectionString;
            var eventsExchange = $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.AccountsManagement}.events.exchange";
            
            //cqrs messages subscription
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<DepositSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalFailedEvent>(connectionString, eventsExchange);

            //other messages subscription
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(_settings.MessagingDelay);
        }

        [SetUp]
        public void SetUp()
        {
            Thread.Sleep(2000); //try to wait all the messages to pass
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            //dispose listeners
            RabbitUtil.TearDown();
        }

        #region Create account

        [Test]
        public async Task CreateAccount_Success()
        {
            // arrange
            var accounts = await ClientUtil.AccountsApi.GetByClient(AccountHelpers.GetClientId);
            var currentIndex = accounts.Where(x => x.Id.StartsWith(AccountHelpers.GetAccountIdPrefix))
                .Select(x => (int.TryParse(x.Id.Replace(AccountHelpers.GetAccountIdPrefix, ""), out var value), value))
                .Where(pair => pair.Item1)
                .Select(pair => pair.value)
                .Append(0)
                .Max();
            var newAccountId = $"{AccountHelpers.GetAccountIdPrefix}{++currentIndex}";

            // act
            await MtCoreHelpers.EnsureAccountState(0, newAccountId, false);

            // assert
            await RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.Account.Id == newAccountId
                                                                      && m.EventType == AccountChangedEventTypeContract.Created);
            var resultingAccount = await AccountHelpers.GetAccount(newAccountId);
            resultingAccount.ClientId.Should().Be(AccountHelpers.GetClientId);
            resultingAccount.Id.Should().Be(newAccountId);
            resultingAccount.Balance.Should().Be(0); // may fail if Accounts -> Behaviour -> DefaultBalance != null
            resultingAccount.IsDisabled.Should().Be(false);
        }

        #endregion Create account
        
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
                ClientId = AccountHelpers.GetClientId,
                Id = AccountHelpers.GetDefaultAccount,
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
            await MtCoreHelpers.EnsureAccountState(closeAllPositions: false);

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
            await MtCoreHelpers.EnsureAccountState(closeAllPositions: false);
            var operationId = Guid.NewGuid().ToString();
            
            // act
            //todo use specific command
            CqrsUtil.SendEventToAccountManagement(new PositionClosedEvent(
                accountId: AccountHelpers.GetDefaultAccount, 
                clientId: AccountHelpers.GetClientId, 
                positionId: operationId,
                assetPairId: "testAssetPair",
                balanceDelta: delta));

            await RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                m.BalanceChange?.Id == operationId + "-update-balance");

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(0 + delta);
        }
        
        #endregion Position close
        
        #region Deposit
        
        [Test]
        public async Task Deposit_Success()
        {
            // arrange
            await MtCoreHelpers.EnsureAccountState(closeAllPositions: false);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginDeposit(AccountHelpers.GetDefaultAccount,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 123,
                    Reason = "integration tests: deposit",
                });
            
            var messagesReceivedTask = Task.WhenAll(
                RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.BalanceChange?.Id == operationId),
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
            await MtCoreHelpers.EnsureAccountState(neededBalance: 123M, closeAllPositions: false);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(AccountHelpers.GetDefaultAccount,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 123M,
                    Reason = $"integration tests: {nameof(IfEnoughBalance_ShouldWithdraw)}",
                });

            await Task.WhenAll(
                RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.BalanceChange?.Id == operationId),
                RabbitUtil.WaitForMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId));

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(0);
        }
        
        [Test]
        public async Task IfNotEnoughBalance_ShouldFailWithdraw()
        {
            // arrange
            await MtCoreHelpers.EnsureAccountState(neededBalance: 123, closeAllPositions: false);
            (await AccountHelpers.GetAccount()).Balance.Should().Be(123);

            // act
            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(AccountHelpers.GetDefaultAccount,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = 124,
                    Reason = $"integration tests: {nameof(IfNotEnoughBalance_ShouldFailWithdraw)}",
                });
            
            var eventTask = await Task.WhenAny(
                RabbitUtil.WaitForMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId),
                RabbitUtil.WaitForMessage<WithdrawalFailedEvent>(m => m.OperationId == operationId));

            eventTask.GetType().GetGenericArguments().First().Should().Be<WithdrawalFailedEvent>();

            // assert
            (await AccountHelpers.GetAccount()).Balance.Should().Be(123);
        }
        
        #endregion Withdrawal
    }
}
