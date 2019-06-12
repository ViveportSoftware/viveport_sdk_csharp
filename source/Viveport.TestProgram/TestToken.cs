namespace Viveport.TestProgram
{
    public class TestToken : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "8effed82-b89b-4429-b0ee-46fb85548207";
        private static TestToken instance;

        public static TestToken GetInstance()
        {
            if (instance == null)
            {
                instance = new TestToken();
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
                Token.IsReady(TestIsReadyCallback);
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
                Token.GetSessionToken(TestGetSessionTokenCallback);
            }
            else
            {
                TestLogger.Error("IsReady", string.Format("IsReady failure. Error Code: {0}", errorCode));
            }
        }

        void TestGetSessionTokenCallback(int errorCode, string message)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("GetSessionToken", string.Format("GetSessionToken success. Code: {0}, Message: {1}", errorCode, message));
            }
            else
            {
                TestLogger.Error("GetSessionToken", string.Format("GetSessionToken failure. Code: {0}, Message: {1}", errorCode, message));
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
