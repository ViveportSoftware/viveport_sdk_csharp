namespace Viveport.TestProgram
{
    public class TestDlc : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "efc1113a-3041-45e5-9ace-9bb68e3b0890";
        private static TestDlc instance;

        public static TestDlc GetInstance()
        {
            if (instance == null)
            {
                instance = new TestDlc();
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
                Dlc.IsReady(TestIsReady);
            }
            else
            {
                TestLogger.Error("Init", string.Format("Init failure. Error Code: {0}", errorCode));
            }
        }

        void TestIsReady(int errorCode)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("IsReady", "IsReady success.");
                var dlcCount = Dlc.GetCount();
                TestLogger.Success("GetCount", dlcCount.ToString());
                for (var i = 0; i < dlcCount; i++)
                {
                    var viveporId = string.Empty;
                    var isAvailable = false;
                    bool isNotOverRange = Dlc.GetIsAvailable(i, out viveporId, out isAvailable);
                    if (isNotOverRange)
                    {
                        TestLogger.Success("GetIsAvailable", string.Format("VIVEPORT ID: {0}, Is Available: {1}", viveporId, isAvailable));
                    }
                    else
                    {
                        TestLogger.Error("GetIsAvailable", "Index is out of range.");
                        break;
                    }
                }
            }
            else
            {
                TestLogger.Error("IsReady", string.Format("IsReady failure. Error Code: {0}", errorCode));
            }

            Api.Shutdown(TestShutdownCallback);
        }

        public void TestShutdownCallback(int errorCode)
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
