namespace Viveport.TestProgram
{
    public class TestStatAchievement : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "af5d39c4-d463-4c16-bc05-63f085dcff2b";
        private const string STAT_NAME = "TestStat";
        private const string ACHIEVEMENT_NAME = "TestAchievement";
        private static TestStatAchievement instance;

        public static TestStatAchievement GetInstance()
        {
            if (instance == null)
            {
                instance = new TestStatAchievement();
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
                UserStats.IsReady(TestIsReadyCallback);
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
                UserStats.DownloadStats(TestDownloadStatsCallback);
            }
            else
            {
                TestLogger.Error("IsReady", string.Format("IsReady failure. Error Code: {0}", errorCode));
            }
        }

        void TestDownloadStatsCallback(int errorCode)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("DownloadStats", "DownloadStats success.");
                TestLogger.Success("GetStat", string.Format("GestStat success. Stat: {0}", UserStats.GetStat(STAT_NAME, -1)));
                TestLogger.Success("GetAchievement", string.Format("GetAchievement success. Achieved: {0}", UserStats.GetAchievement(ACHIEVEMENT_NAME)));
                TestLogger.Success("GetAchievementUnlockTime", string.Format("GetAchievementUnlockTime", UserStats.GetAchievementUnlockTime(ACHIEVEMENT_NAME)));
            }
            else
            {
                TestLogger.Error("DownloadStats", string.Format("DownloadStats failure. Error Code: {0}", errorCode));
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
