using JetBrains.Annotations;

namespace MarginTrading.IntegrationTests.Infrastructure.Refit
{
    [UsedImplicitly]
    public class MtBackendErrorResponse
    {
        public string ErrorMessage { get; set; }

        public override string ToString() => ErrorMessage;
    }
}
