namespace Viveport.TestProgram
{
    public class TestLeaderboard : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "af5d39c4-d463-4c16-bc05-63f085dcff2b";
        private const string LEADERBOARD_NAME = "TestLeaderboard";
        private static TestLeaderboard instance;

        public static TestLeaderboard GetInstance()
        {
            if (instance == null)
            {
                instance = new TestLeaderboard();
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
                UserStats.DownloadLeaderboardScores(
                    TestDownloadLeaderboardScoresCallback,
                    LEADERBOARD_NAME,
                    UserStats.LeaderBoardRequestType.GlobalDataAroundUser,
                    UserStats.LeaderBoardTimeRange.AllTime,
                    -5,
                    5);
            }
            else
            {
                TestLogger.Error("IsReady", string.Format("IsReady failure. Error Code: {0}", errorCode));
            }
        }

        void TestDownloadLeaderboardScoresCallback(int errorCode)
        {
            if (errorCode == SUCCESS)
            {
                TestLogger.Success("DownloadLeaderboardScores", "DownloadLeaderboardScores success.");
                var leaderboardScoreCount = UserStats.GetLeaderboardScoreCount();
                TestLogger.Success("GetLeaderboardScoreCount", leaderboardScoreCount.ToString());
                for (var i = 0; i < leaderboardScoreCount; i++)
                {
                    var leaderboard = UserStats.GetLeaderboardScore(leaderboardScoreCount);
                    TestLogger.Success("GetLeaderboardScore", string.Format("Rank: {0}, Score: {1}, User Name: {2}", leaderboard.Rank, leaderboard.Score, leaderboard.UserName));
                }
                TestLogger.Success("GetLeaderboardSortMethod", UserStats.GetLeaderboardSortMethod().ToString());
                TestLogger.Success("GetLeaderboardDisplayType", UserStats.GetLeaderboardDisplayType().ToString());
            }
            else
            {
                TestLogger.Error("DownloadLeaderboardScores", string.Format("DownloadLeaderboardScores failure. Error Code: {0}", errorCode));
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
