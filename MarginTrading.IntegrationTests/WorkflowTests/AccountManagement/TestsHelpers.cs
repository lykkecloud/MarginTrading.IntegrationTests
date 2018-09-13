﻿using System;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.IntegrationTests.Infrastructure;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    public static class TestsHelpers
    {
        public const string ClientId = "IntergationalTestsClient";
        public const string AccountId = "IntergationalTestsAccount-1";

        public static async Task<AccountContract> EnsureAccountState(decimal needBalance = 0)
        {
            var account = await ClientUtil.AccountsApi.GetByClientAndId(ClientId, AccountId);
            if (account == null)
            {
                account = await ClientUtil.AccountsApi.Create(ClientId, new CreateAccountRequest
                {
                    AccountId = AccountId,
                    BaseAssetId = "EUR",
                });
            }

            if (account.Balance != needBalance)
            {
                await ChargeManually(needBalance - account.Balance);
                account = new AccountContract(account.Id, account.ClientId, account.TradingConditionId, 
                    account.BaseAssetId, needBalance, account.WithdrawTransferLimit, account.LegalEntity,
                    account.IsDisabled, account.ModificationTimestamp);
            }

            if (account.IsDisabled)
            {
                account = await ClientUtil.AccountsApi.Change(ClientId, AccountId, new ChangeAccountRequest
                {
                    IsDisabled = false,
                });
            }

            return account;
        }

        public static async Task ChargeManually(decimal delta)
        {
            var operationId = await ClientUtil.AccountsApi.BeginChargeManually(ClientId, AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = delta,
                    Reason = "integration tests",
                    ReasonType = AccountBalanceChangeReasonTypeContract.Manual,
                    EventSourceId = Guid.NewGuid().ToString(),
                });

            await RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId);
        }

        public static Task<AccountContract> GetAccount()
        {
            return ClientUtil.AccountsApi.GetByClientAndId(ClientId, AccountId);
        }
    }
}