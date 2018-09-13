using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    /// <summary>
    /// Runs before and after all tests in this namespace
    /// </summary>
    [SetUpFixture]
    public class Setup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // ...
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            // ...
        }
    }
}