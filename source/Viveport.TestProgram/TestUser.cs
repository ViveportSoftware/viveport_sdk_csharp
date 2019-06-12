namespace Viveport.TestProgram
{
    public class TestUser : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "8effed82-b89b-4429-b0ee-46fb85548207";
        private static TestUser instance;

        public static TestUser GetInstance()
        {
            if (instance == null)
            {
                instance = new TestUser();
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
                TestLogger.Success("GetUserId", User.GetUserId());
                TestLogger.Success("GetUserName", User.GetUserName());
                TestLogger.Success("GetUserAvatarUrl", User.GetUserAvatarUrl());
            }
            else
            {
                TestLogger.Error("Init", string.Format("Init failure. Error Code: {0}", errorCode));
            }

            Api.Shutdown(TestShutdownCallback);
        }

        void TestShutdownCallback(int errorCode)
        {
            if (errorCode == SUCCESS)
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
