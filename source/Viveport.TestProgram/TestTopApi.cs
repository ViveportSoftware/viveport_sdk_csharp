namespace Viveport.TestProgram
{
    public class TestTopApi : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "8effed82-b89b-4429-b0ee-46fb85548207";
        private const string VIVEPORT_KEY = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCHgmRicTobMkOfFUTeWLXuGXeSLL9g8JJ7WcDpdJY2nfsK5QxCa+6GHDdSrWquzg3az/qg45MtoqylntJWVEUTobhzImhNFpDzj0ATLjuUcfb+Lnj5wzaR50Doayyi+G5IfKO9yXkG6s/oN+qig4/V3CGlV74u/NqwZee6lMAtVwIDAQAB";
        private static TestTopApi instance;

        public static TestTopApi GetInstance()
        {
            if (instance == null)
            {
                instance = new TestTopApi();
            }

            return instance;
        }

        public void StartTest()
        {
            Api.Init(TestInitCallback, VIVEPORT_ID);
        }

        void TestInitCallback(int errorCode)
        {
            if (errorCode == 0)
            {
                TestLogger.Success("Init", "Init success.");
                TestLogger.Success("Version", Api.Version());
                Api.QueryRuntimeMode(TestQueryRuntimeModeCallback);
            }
            else
            {
                TestLogger.Error("Init", string.Format("Init failure. Error Code: {0}", errorCode));
            }
        }

        void TestQueryRuntimeModeCallback(int errorCode, int mode)
        {
            if (errorCode == 0)
            {
                TestLogger.Success("QueryRuntimeMode", mode.ToString());
                Api.GetLicense(new TestLicenseChecker(), VIVEPORT_ID, VIVEPORT_KEY);
            }
            else
            {
                TestLogger.Error("QueryRuntimeMode", string.Format("QueryRuntimeMode failure. Error Code: {0}", errorCode));
            }
        }

        class TestLicenseChecker : Api.LicenseChecker
        {
            public override void OnSuccess(long issueTime, long expirationTime, int latestVersion, bool updateRequired)
            {
                TestLogger.Success(
                    "GetLicense",
                    string.Format(
                        "GetLicense success. Issue Time: {0}, Expiration Time: {1}, Latest Version: {2}, Update Required: {3}",
                        issueTime,
                        expirationTime,
                        latestVersion,
                        updateRequired));
                Api.Shutdown(GetInstance().TestShutdownCallback);
            }

            public override void OnFailure(int errorCode, string errorMessage)
            {
                TestLogger.Error(
                    "GetLicense",
                    string.Format(
                        "GetLicense failure. Error Code: {0}, Error Message: {1}",
                        errorCode,
                        errorMessage));
                Api.Shutdown(GetInstance().TestShutdownCallback);
            }
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
