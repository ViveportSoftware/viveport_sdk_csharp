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

        void TestIsReadyCallback(int errorCode)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("IsReady", "IsReady success.");

                var isViveportClientNeedToUpdate = false;
                var isSubscribed = Subscription.IsSubscribed(out isViveportClientNeedToUpdate);
                if (isViveportClientNeedToUpdate)
                {
                    TestLogger.Warnning("System", "Your VIVEPORT CLIENT needs to update...");
                }
                else
                {
                    TestLogger.Success("IsSubscribed", isSubscribed ? "Content subscribed by user." : "This user didn't subscribe this content.");
                }
            }
            else
            {
                TestLogger.Error("IsReady", string.Format("IsReady failure. Error Code: {0}", errorCode));
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
