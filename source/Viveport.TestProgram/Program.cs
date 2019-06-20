using System;

namespace Viveport.TestProgram
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TestLogger.Debug(null, "Start to test top API...");
            var testTopApi = TestTopApi.GetInstance();
            testTopApi.SetCallback(OnTopApiFinished);
            testTopApi.StartTest();

            Console.ReadKey();
        }

        static void OnTopApiFinished()
        {
            TestLogger.Debug(null, "Top API testing finished...");
            TestLogger.Debug(null, "Start to test token...");
            var testToken = TestToken.GetInstance();
            testToken.SetCallback(OnTokenFinished);
            testToken.StartTest();
        }

        static void OnTokenFinished()
        {
            TestLogger.Debug(null, "Token testing finished...");
            TestLogger.Debug(null, "Start to test user...");
            var testUser = TestUser.GetInstance();
            testUser.SetCallback(OnUserFinished);
            testUser.StartTest();
        }

        static void OnUserFinished()
        {
            TestLogger.Debug(null, "User testing finished...");
            TestLogger.Debug(null, "Start to test stat and achievement...");
            var testStatAchievement = TestStatAchievement.GetInstance();
            testStatAchievement.SetCallback(OnStatAchievementFinished);
            testStatAchievement.StartTest();
        }

        static void OnStatAchievementFinished()
        {
            TestLogger.Debug(null, "Stat and achievement testing finished...");
            TestLogger.Debug(null, "Start to test leaderboard...");
            var testLeaderboard = TestLeaderboard.GetInstance();
            testLeaderboard.SetCallback(OnLeaderboardFinished);
            testLeaderboard.StartTest();
        }

        static void OnLeaderboardFinished()
        {
            TestLogger.Debug(null, "Leaderboard testing finished...");
            TestLogger.Debug(null, "Start to test IAP...");
            var testIap = TestIap.GetInstance();
            testIap.SetCallback(OnIapFinished);
            testIap.StartTest();
        }

        static void OnIapFinished()
        {
            TestLogger.Debug(null, "IAP testing finished...");
            TestLogger.Debug(null, "Start to test DLC...");
            var testDlc = TestDlc.GetInstance();
            testDlc.SetCallback(OnDlcFinished);
            testDlc.StartTest();
        }

        static void OnDlcFinished()
        {
            TestLogger.Debug(null, "DLC testing finished...");
            var testSubscription = TestSubscription.GetInstance();
            testSubscription.SetCallback(OnSubscriptionFinished);
            testSubscription.StartTest();
        }

        static void OnSubscriptionFinished()
        {
            TestLogger.Debug(null, "Subscription testing finished...");
            TestLogger.Debug(null, "Please press enter key to leave...");
        }
    }
}
