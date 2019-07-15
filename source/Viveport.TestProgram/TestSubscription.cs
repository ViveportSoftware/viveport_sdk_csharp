namespace Viveport.TestProgram
{
    public class TestSubscription : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "35e7aabb-e4e6-46cb-b365-e80ed6fd4cea";
        private static TestSubscription instance;

        public static TestSubscription GetInstance()
        {
            if (instance == null)
            {
                instance = new TestSubscription();
            }

            return instance;
        }

        public void StartTest()
        {
            Api.Init(TestInitCallback, VIVEPORT_ID);
        }

        void TestInitCallback(int errorCode)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("Init", "Init success.");
                Subscription.IsReady(TestIsReadyCallback);
            }
            else
            {
                TestLogger.Error("Init", string.Format("Init failure. Error Code: {0}", errorCode));
            }
        }

        void TestIsReadyCallback(int errorCode, string message)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("IsReady", "IsReady success.");

                var userStatus = Subscription.GetUserStatus();
                var isWindowsSubscriber = userStatus.Platforms.Contains(SubscriptionStatus.Platform.Windows) ? "true" : "false";
                var isAndroidSubscriber = userStatus.Platforms.Contains(SubscriptionStatus.Platform.Android) ? "true" : "false";
                var transactionType = "";
                switch (userStatus.Type)
                {
                    case SubscriptionStatus.TransactionType.Unknown:
                        transactionType = "Unknown";
                        break;
                    case SubscriptionStatus.TransactionType.Paid:
                        transactionType = "Paid";
                        break;
                    case SubscriptionStatus.TransactionType.Redeem:
                        transactionType = "Redeem";
                        break;
                    case SubscriptionStatus.TransactionType.FreeTrial:
                        transactionType = "FreeTrial";
                        break;
                    default:
                        transactionType = "Unknown";
                        break;
                }

                TestLogger.Success("GetUserStatus", string.Format("User is a Windows subscriber: {0}", isWindowsSubscriber));
                TestLogger.Success("GetUserStatus", string.Format("User is a Android subscriber: {0}", isAndroidSubscriber));
                TestLogger.Success("GetUserStatus", string.Format("Transaction Type: {0}", transactionType));
            }
            else
            {
                TestLogger.Error("IsReady", string.Format("IsReady failure. Error Code: {0}, Error Message: {1}", errorCode, message));
            }

            Api.Shutdown(TestShutdownCallback);
        }

        void TestShutdownCallback(int errorCode)
        {
            if (errorCode == 0)
            {
                TestLogger.Success("Shutdown", "Shutdown success.");
            }
            else
            {
                TestLogger.Error("Shutdown", string.Format("Shutdown failure. Error Code: {0}", errorCode));
            }

            OnTestFinished();
        }
    }
}
