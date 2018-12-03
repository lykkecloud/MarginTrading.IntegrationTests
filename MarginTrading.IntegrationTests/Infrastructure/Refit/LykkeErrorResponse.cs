using JetBrains.Annotations;

namespace MarginTrading.IntegrationTests.Infrastructure.Refit
{
    [UsedImplicitly]
    public class LykkeErrorResponse
    {
        public string ErrorMessage { get; set; }

        public override string ToString() => ErrorMessage;
    }
}
