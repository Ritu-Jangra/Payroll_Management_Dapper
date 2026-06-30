namespace Payroll.Tests
{
    public abstract class BaseTests
    {
        public required TestContext TestContext { get; set; }

        #region Test initialise/clean-up

        [TestInitialize()]
        public virtual void InitialiseTest()
        {
            TestContext.WriteLine($"Initialised test {TestContext.TestName}");
        }

        [TestCleanup()]
        public virtual void TerminateTest()
        {
            TestContext.WriteLine($"Cleaning up for test {TestContext.TestName}");
        }

        #endregion Test initialise/clean-up
    }
}
