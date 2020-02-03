using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LitJson;
using PublicKeyConvert;
using Viveport.Core;
using System.Collections;

namespace Viveport
{
    namespace Core
    {
        public class Logger
        {
            private const string LoggerTypeNameUnity = "UnityEngine.Debug";

            private static bool _hasDetected;
            private static bool _usingUnityLog = true;
            private static Type _unityLogType;

            public static void Log(string message)
            {
                if (!_hasDetected || _usingUnityLog)
                {
                    UnityLog(message);
                }
                else
                {
                    ConsoleLog(message);
                }
            }

            private static void ConsoleLog(string message)
            {
                Console.WriteLine(message);
                _hasDetected = true;
            }

            private static void UnityLog(string message)
            {
                try
                {
                    if (_unityLogType == null)
                    {
                        _unityLogType = GetType(LoggerTypeNameUnity);
                    }
                    var methodInfo = _unityLogType.GetMethod("Log", new[] { typeof(string) });
                    methodInfo.Invoke(null, new object[] { message });
                    _usingUnityLog = true;
                }
                catch (Exception)
                {
                    ConsoleLog(message);
                    _usingUnityLog = false;
                }
                _hasDetected = true;
            }

            private static Type GetType(string typeName)
            {
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                return null;
            }
        }
    }

    public delegate void StatusCallback(int nResult);
    public delegate void StatusCallback2(int nResult, string message);
    public delegate void QueryRuntimeModeCallback(int nResult, int emu);

    public class Leaderboard
    {
        public int Rank { get; set; }
        public int Score { get; set; }
        public string UserName { get; set; }
    }

    public class SubscriptionStatus
    {
        public enum Platform
        {
            Windows,
            Android
        }

        public enum TransactionType
        {
            Unknown,
            Paid,
            Redeem,
            FreeTrial
        }

        public List<Platform> Platforms { get; set; } = new List<Platform>();
        public TransactionType Type { get; set; } = TransactionType.Unknown;
    }

    public partial class Api
    {
        internal static readonly List<Internal.GetLicenseCallback> InternalGetLicenseCallbacks = new List<Internal.GetLicenseCallback>();
        internal static readonly List<Internal.StatusCallback> InternalStatusCallbacks = new List<Internal.StatusCallback>();
        internal static readonly List<Internal.QueryRuntimeModeCallback> InternalQueryRunTimeCallbacks = new List<Internal.QueryRuntimeModeCallback>();
        internal static readonly List<LicenseChecker> InternalLicenseCheckers = new List<LicenseChecker>();

#if !UNITY_ANDROID
        private static readonly Internal.GetLicenseCallback sGetLicenseHandler = GetLicenseHandler;
#endif
        private static readonly string VERSION = "9.99.999.9999";

        private static string _appId = "";
        private static string _appKey = "";

        public static void GetLicense(
                LicenseChecker checker,
                string appId,
                string appKey)
        {
            if (checker == null || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
            {
                throw new InvalidOperationException("checker == null || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey)");
            }

            _appId = appId;
            _appKey = appKey;

#if !UNITY_ANDROID
            InternalLicenseCheckers.Add(checker);
            if (Environment.Is64BitProcess)
            {
                Internal.Api.GetLicense_64(sGetLicenseHandler, _appId, _appKey);
            }
            else
            {
                Internal.Api.GetLicense(sGetLicenseHandler, _appId, _appKey);
            }
#elif UNITY_ANDROID
            Internal.Api.GetLicense(checker, _appId, _appKey);
#endif
        }

        public static int Init(
                StatusCallback callback,
                string appId)
        {
            if (callback == null || string.IsNullOrEmpty(appId))
            {
                throw new InvalidOperationException("callback == null || string.IsNullOrEmpty(appId)");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            InternalStatusCallbacks.Add(internalCallback);
            if (Environment.Is64BitProcess)
            {
                return Internal.Api.Init_64(internalCallback, appId);
            }
            else
            {
                return Internal.Api.Init(internalCallback, appId);
            }
        }

        public static int Shutdown(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            InternalStatusCallbacks.Add(internalCallback);
            if (Environment.Is64BitProcess)
            {
                return Internal.Api.Shutdown_64(internalCallback);
            }
            else
            {
                return Internal.Api.Shutdown(internalCallback);
            }
        }

        public static string Version()
        {
            var nativeVersion = "";
#if !UNITY_ANDROID
            try
            {
                if (Environment.Is64BitProcess)
                {
                    nativeVersion += Marshal.PtrToStringAnsi(Internal.Api.Version_64());
                }
                else
                {
                    nativeVersion += Marshal.PtrToStringAnsi(Internal.Api.Version());
                }
            }
            catch (Exception)
            {
                Logger.Log("Can not load version from native library");
            }
#else
            nativeVersion = Internal.Api.Version();
#endif
            return "C# version: " + VERSION + ", Native version: " + nativeVersion;
        }

#if !UNITY_ANDROID
        public static void QueryRuntimeMode(QueryRuntimeModeCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.QueryRuntimeModeCallback(callback);
            InternalQueryRunTimeCallbacks.Add(internalCallback);
            if (Environment.Is64BitProcess)
            {
                Internal.Api.QueryRuntimeMode_64(internalCallback);
            }
            else
            {
                Internal.Api.QueryRuntimeMode(internalCallback);
            }
        }

        /*
         * Responsed license JSON format:
         * {
         *   "issueTime": 1442301893123, // epoch time in milliseconds, Long
         *   "expirationTime": 1442451893123, // epoch time in milliseconds, Long
         *   "latestVersion": 1001, // versionId, Integer
         *   "updateRequired": true // Boolean
         * }
         */
        private static void GetLicenseHandler(
                [MarshalAs(UnmanagedType.LPStr)] string message,
                [MarshalAs(UnmanagedType.LPStr)] string signature)
        {
            // Logger.Log("Raw Message: " + message);
            // Logger.Log("Raw Signature: " + signature);

            var isVerified = !string.IsNullOrEmpty(message);
            if (!isVerified)
            {
                for (var i = InternalLicenseCheckers.Count - 1; i >= 0; i--)
                {
                    var checker = InternalLicenseCheckers[i];
                    checker.OnFailure(90003, "License message is empty");
                    InternalLicenseCheckers.Remove(checker);
                }
                return;
            }

            isVerified = !string.IsNullOrEmpty(signature);
            if (!isVerified) // signature is empty - error code mode
            {
                var jsonData = JsonMapper.ToObject(message);
                var errorCode = 99999;
                var errorMessage = "";

                try
                {
                    errorCode = int.Parse((string)jsonData["code"]);
                }
                catch
                {
                    // ignored
                }
                try
                {
                    errorMessage = (string)jsonData["message"];
                }
                catch
                {
                    // ignored
                }

                for (var i = InternalLicenseCheckers.Count - 1; i >= 0; i--)
                {
                    var checker = InternalLicenseCheckers[i];
                    checker.OnFailure(errorCode, errorMessage);
                    InternalLicenseCheckers.Remove(checker);
                }
                return;
            }

            isVerified = VerifyMessage(_appId, _appKey, message, signature);
            if (!isVerified)
            {
                for (var i = InternalLicenseCheckers.Count - 1; i >= 0; i--)
                {
                    var checker = InternalLicenseCheckers[i];
                    checker.OnFailure(90001, "License verification failed");
                    InternalLicenseCheckers.Remove(checker);
                }
                return;
            }

            var decodedLicense = Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                            message.Substring(message.IndexOf("\n", StringComparison.Ordinal) + 1)
                    )
            );
            var jsonData2 = JsonMapper.ToObject(decodedLicense);
            Logger.Log("License: " + decodedLicense);

            var issueTime = -1L;
            var expirationTime = -1L;
            var latestVersion = -1;
            var updateRequired = false;

            try
            {
                issueTime = (long)jsonData2["issueTime"];
            }
            catch
            {
                // ignored
            }
            try
            {
                expirationTime = (long)jsonData2["expirationTime"];
            }
            catch
            {
                // ignored
            }
            try
            {
                latestVersion = (int)jsonData2["latestVersion"];
            }
            catch
            {
                // ignored
            }
            try
            {
                updateRequired = (bool)jsonData2["updateRequired"];
            }
            catch
            {
                // ignored
            }

            for (var i = InternalLicenseCheckers.Count - 1; i >= 0; i--)
            {
                var checker = InternalLicenseCheckers[i];
                checker.OnSuccess(issueTime, expirationTime, latestVersion, updateRequired);
                InternalLicenseCheckers.Remove(checker);
            }
        }

        private static bool VerifyMessage(
                string appId,
                string appKey,
                string message,
                string signature)
        {
            try
            {
                var provider = PEMKeyLoader.CryptoServiceProviderFromPublicKeyInfo(appKey);
                var decodedSignature = Convert.FromBase64String(signature);
                var sha = new SHA1Managed();
                var data = Encoding.UTF8.GetBytes(appId + "\n" + message);

                return provider.VerifyData(data, sha, decodedSignature);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
            return false;
        }
#endif

        public abstract class LicenseChecker
        {
            public abstract void OnSuccess(
                    long issueTime,
                    long expirationTime,
                    int latestVersion,
                    bool updateRequired
            );
            public abstract void OnFailure(
                    int errorCode,
                    string errorMessage
            );
        }
    }

    public partial class User
    {
#if !UNITY_ANDROID
        private const int MaxIdLength = 256;
        private const int MaxNameLength = 256;
        private const int MaxUrlLength = 512;
#endif

        public static string GetUserId()
        {
#if !UNITY_ANDROID
            var userId = new StringBuilder(MaxIdLength);
            if (Environment.Is64BitProcess)
            {
                Internal.User.GetUserID_64(userId, MaxIdLength);
            }
            else
            {
                Internal.User.GetUserID(userId, MaxIdLength);
            }
            return userId.ToString();
#else
            return Internal.User.GetUserId().ToString();
#endif
        }

        public static string GetUserName()
        {
#if !UNITY_ANDROID
            var userName = new StringBuilder(MaxNameLength);
            if (Environment.Is64BitProcess)
            {
                Internal.User.GetUserName_64(userName, MaxNameLength);
            }
            else
            {
                Internal.User.GetUserName(userName, MaxNameLength);
            }
            return userName.ToString();
#else
            return Internal.User.GetUserName().ToString();
#endif
        }

        public static string GetUserAvatarUrl()
        {
#if !UNITY_ANDROID
            var userAvatarUrl = new StringBuilder(MaxUrlLength);
            if (Environment.Is64BitProcess)
            {
                Internal.User.GetUserAvatarUrl_64(userAvatarUrl, MaxUrlLength);
            }
            else
            {
                Internal.User.GetUserAvatarUrl(userAvatarUrl, MaxUrlLength);
            }
            return userAvatarUrl.ToString();
#else
            return Internal.User.GetUserAvatarUrl().ToString();
#endif
        }

    }

    public partial class UserStats
    {

        public enum LeaderBoardRequestType
        {
            GlobalData = Internal.ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal /* 0 */,
            GlobalDataAroundUser = Internal.ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser /* 1 */,
            LocalData = Internal.ELeaderboardDataRequest.k_ELeaderboardDataRequestLocal /* 2 */,
            LocalDataAroundUser = Internal.ELeaderboardDataRequest.k_ELeaderboardDataRequestLocaleAroundUser /* 3 */,
        }

        public enum LeaderBoardTimeRange
        {
            AllTime = Internal.ELeaderboardDataTimeRange.k_ELeaderboardDataScropeAllTime /* 0 */,
            Daily = Internal.ELeaderboardDataTimeRange.k_ELeaderboardDataScropeDaily /* 1 */,
            Weekly = Internal.ELeaderboardDataTimeRange.k_ELeaderboardDataScropeWeekly /* 2 */,
            Monthly = Internal.ELeaderboardDataTimeRange.k_ELeaderboardDataScropeMonthly /* 3 */,
        }

        public enum LeaderBoardSortMethod
        {
            None = Internal.ELeaderboardSortMethod.k_ELeaderboardSortMethodNone /* 0 */,
            Ascending = Internal.ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending /* 1 */,
            Descending = Internal.ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending /* 2 */,
        }

        public enum LeaderBoardDiaplayType
        {
            None = Internal.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNone /* 0 */,
            Numeric = Internal.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric /* 1 */,
            TimeSeconds = Internal.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds /* 2 */,
            TimeMilliSeconds = Internal.ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds /* 3 */,
        }

        public enum LeaderBoardScoreMethod
        {
            None = Internal.ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodNone /* 0 */,
            KeepBest = Internal.ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest /* 1 */,
            ForceUpdate = Internal.ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate /* 2 */,
        }

        public static int IsReady(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.IsReady_64(internalCallback);
            }
            else
            {
                return Internal.UserStats.IsReady(internalCallback);
            }
        }

        public static int DownloadStats(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.DownloadStats_64(internalCallback);
            }
            else
            {
                return Internal.UserStats.DownloadStats(internalCallback);
            }
        }

        public static int GetStat(string name, int defaultValue)
        {
#if !UNITY_ANDROID
            var result = defaultValue;
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.GetStat_64(name, ref result);
            }
            else
            {
                Internal.UserStats.GetStat(name, ref result);
            }
            return result;
#else
            return Internal.UserStats.GetStat(name, defaultValue);
#endif
        }

        public static float GetStat(string name, float defaultValue)
        {
#if !UNITY_ANDROID
            var result = defaultValue;
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.GetStat_64(name, ref result);
            }
            else
            {
                Internal.UserStats.GetStat(name, ref result);
            }
            return result;
#else
            return Internal.UserStats.GetStat(name, defaultValue);
#endif
        }

        public static void SetStat(string name, int value)
        {
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.SetStat_64(name, value);
            }
            else
            {
                Internal.UserStats.SetStat(name, value);
            }
        }

        public static void SetStat(string name, float value)
        {
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.SetStat_64(name, value);
            }
            else
            {
                Internal.UserStats.SetStat(name, value);
            }
        }

        public static int UploadStats(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.UploadStats_64(internalCallback);
            }
            else
            {
                return Internal.UserStats.UploadStats(internalCallback);
            }            
        }

        // for Achievements
        public static bool GetAchievement(string pchName)
        {
#if !UNITY_ANDROID
            var nAchieved = 0;
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.GetAchievement_64(pchName, ref nAchieved);
            }
            else
            {
                Internal.UserStats.GetAchievement(pchName, ref nAchieved);
            }
            return nAchieved == 1;
#else
            return Internal.UserStats.GetAchievement(pchName);
#endif
        }

        public static int GetAchievementUnlockTime(string pchName)
        {
#if !UNITY_ANDROID
            var nUnlockTime = 0;
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.GetAchievementUnlockTime_64(pchName, ref nUnlockTime);
            }
            else
            {
                Internal.UserStats.GetAchievementUnlockTime(pchName, ref nUnlockTime);
            }            
            return nUnlockTime;
#else
            return Internal.UserStats.GetAchievementUnlockTime(pchName);
#endif
        }

        public static int SetAchievement(string pchName)
        {
            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.SetAchievement_64(pchName);
            }
            else
            {
                return Internal.UserStats.SetAchievement(pchName);
            }            
        }

        public static int ClearAchievement(string pchName)
        {
            if(Environment.Is64BitProcess)
            {
                return Internal.UserStats.ClearAchievement_64(pchName);
            }
            else
            {
                return Internal.UserStats.ClearAchievement(pchName);
            }
        }

        // for Leaderboards
        public static int DownloadLeaderboardScores(
                StatusCallback callback,
                string pchLeaderboardName,
                LeaderBoardRequestType eLeaderboardDataRequest,
                LeaderBoardTimeRange eLeaderboardDataTimeRange,
                int nRangeStart,
                int nRangeEnd)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.DownloadLeaderboardScores_64(
                    internalCallback,
                    pchLeaderboardName,
                    (Internal.ELeaderboardDataRequest)eLeaderboardDataRequest,
                    (Internal.ELeaderboardDataTimeRange)eLeaderboardDataTimeRange,
                    nRangeStart,
                    nRangeEnd
                );
            }
            else
            {
                return Internal.UserStats.DownloadLeaderboardScores(
                    internalCallback,
                    pchLeaderboardName,
                    (Internal.ELeaderboardDataRequest)eLeaderboardDataRequest,
                    (Internal.ELeaderboardDataTimeRange)eLeaderboardDataTimeRange,
                    nRangeStart,
                    nRangeEnd
                );
            }
        }

        public static int UploadLeaderboardScore(
                StatusCallback callback,
                string pchLeaderboardName,
                int nScore)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.UploadLeaderboardScore_64(internalCallback, pchLeaderboardName, nScore);
            }
            else
            {
                return Internal.UserStats.UploadLeaderboardScore(internalCallback, pchLeaderboardName, nScore);
            }
        }

        public static Leaderboard GetLeaderboardScore(int index)
        {
#if !UNITY_ANDROID
            Internal.LeaderboardEntry_t pLeaderboardEntry;
            pLeaderboardEntry.m_nGlobalRank = 0;
            pLeaderboardEntry.m_nScore = 0;
            pLeaderboardEntry.m_pUserName = "";
            if (Environment.Is64BitProcess)
            {
                Internal.UserStats.GetLeaderboardScore_64(index, ref pLeaderboardEntry);
            }
            else
            {
                Internal.UserStats.GetLeaderboardScore(index, ref pLeaderboardEntry);
            }
 
            return new Leaderboard
            {
                Rank = pLeaderboardEntry.m_nGlobalRank,
                Score = pLeaderboardEntry.m_nScore,
                UserName = pLeaderboardEntry.m_pUserName
            };

#else
            Leaderboard pLeaderboardEntry;
            pLeaderboardEntry = Internal.UserStats.GetLeaderboardScore(index);
            return pLeaderboardEntry;
#endif
        }

        public static int GetLeaderboardScoreCount()
        {
            if (Environment.Is64BitProcess)
            {
                return Internal.UserStats.GetLeaderboardScoreCount_64();
            }
            else
            {
                return Internal.UserStats.GetLeaderboardScoreCount();
            }
        }

        public static LeaderBoardSortMethod GetLeaderboardSortMethod()
        {
            if (Environment.Is64BitProcess)
            {
                return (LeaderBoardSortMethod)Internal.UserStats.GetLeaderboardSortMethod_64();
            }
            else
            {
                return (LeaderBoardSortMethod)Internal.UserStats.GetLeaderboardSortMethod();
            }
        }

        public static LeaderBoardDiaplayType GetLeaderboardDisplayType()
        {
            if (Environment.Is64BitProcess)
            {
                return (LeaderBoardDiaplayType)Internal.UserStats.GetLeaderboardDisplayType_64();
            }
            else
            {
                return (LeaderBoardDiaplayType)Internal.UserStats.GetLeaderboardDisplayType();
            }            
        }
    }

    public partial class ArcadeLeaderboard
    {
#if !UNITY_ANDROID
        public enum LeaderboardTimeRange
        {
            AllTime = Internal.ELeaderboardDataTimeRange.k_ELeaderboardDataScropeAllTime /* 0 */,
        }

        public static void IsReady(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                Internal.ArcadeLeaderboard.IsReady_64(internalCallback);
            }
            else
            {
                Internal.ArcadeLeaderboard.IsReady(internalCallback);
            }
        }

        public static void DownloadLeaderboardScores(
                StatusCallback callback,
                string pchLeaderboardName,
                LeaderboardTimeRange eLeaderboardDataTimeRange,
                int nCount)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            eLeaderboardDataTimeRange = LeaderboardTimeRange.AllTime;

            if (Environment.Is64BitProcess)
            {
                Internal.ArcadeLeaderboard.DownloadLeaderboardScores_64(
                    internalCallback,
                    pchLeaderboardName,
                    (Internal.ELeaderboardDataTimeRange)eLeaderboardDataTimeRange,
                    nCount
                );
            }
            else
            {
                Internal.ArcadeLeaderboard.DownloadLeaderboardScores(
                    internalCallback,
                    pchLeaderboardName,
                    (Internal.ELeaderboardDataTimeRange)eLeaderboardDataTimeRange,
                    nCount
                );
            }
        }

        public static void UploadLeaderboardScore(
                StatusCallback callback,
                string pchLeaderboardName,
                string pchUserName,
                int nScore)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            Api.InternalStatusCallbacks.Add(internalCallback);

            if (Environment.Is64BitProcess)
            {
                Internal.ArcadeLeaderboard.UploadLeaderboardScore_64(
                    internalCallback,
                    pchLeaderboardName,
                    pchUserName,
                    nScore
                );
            }
            else
            {
                Internal.ArcadeLeaderboard.UploadLeaderboardScore(
                    internalCallback,
                    pchLeaderboardName,
                    pchUserName,
                    nScore
                );
            }
        }

        public static Leaderboard GetLeaderboardScore(int index)
        {
            Internal.LeaderboardEntry_t pLeaderboardEntry;
            pLeaderboardEntry.m_nGlobalRank = 0;
            pLeaderboardEntry.m_nScore = 0;
            pLeaderboardEntry.m_pUserName = "";
            if (Environment.Is64BitProcess)
            {
                Internal.ArcadeLeaderboard.GetLeaderboardScore_64(index, ref pLeaderboardEntry);
            }
            else
            {
                Internal.ArcadeLeaderboard.GetLeaderboardScore(index, ref pLeaderboardEntry);
            }
            return new Leaderboard
            {
                Rank = pLeaderboardEntry.m_nGlobalRank,
                Score = pLeaderboardEntry.m_nScore,
                UserName = pLeaderboardEntry.m_pUserName
            };
        }

        public static int GetLeaderboardScoreCount()
        {
            if (Environment.Is64BitProcess)
            {
                return Internal.ArcadeLeaderboard.GetLeaderboardScoreCount_64();
            }
            else
            {
                return Internal.ArcadeLeaderboard.GetLeaderboardScoreCount();
            }
        }

        public static int GetLeaderboardUserRank()
        {
            if (Environment.Is64BitProcess)
            {
                return Internal.ArcadeLeaderboard.GetLeaderboardUserRank_64();
            }
            else
            {
                return Internal.ArcadeLeaderboard.GetLeaderboardUserRank();
            }
        }

        public static int GetLeaderboardUserScore()
        {
            if (Environment.Is64BitProcess)
            {
                return Internal.ArcadeLeaderboard.GetLeaderboardUserScore_64();
            }
            else
            {
                return Internal.ArcadeLeaderboard.GetLeaderboardUserScore();
            }
        }
#endif
    }

    public partial class IAPurchase
    {
        public static void IsReady(IAPurchaseListener listener, string pchAppKey)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.IsReady_64(handler.getIsReadyHandler(), pchAppKey);
            }
            else
            {
                Internal.IAPurchase.IsReady(handler.getIsReadyHandler(), pchAppKey);
            }            
        }

        public static void Request(IAPurchaseListener listener, string pchPrice)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.Request_64(handler.getRequestHandler(), pchPrice);
            }
            else
            {
                Internal.IAPurchase.Request(handler.getRequestHandler(), pchPrice);
            }
        }

        public static void Request(IAPurchaseListener listener, string pchPrice, string pchUserData)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.Request_64(handler.getRequestHandler(), pchPrice, pchUserData);
            }
            else
            {
                Internal.IAPurchase.Request(handler.getRequestHandler(), pchPrice, pchUserData);
            }            
        }

        public static void Purchase(IAPurchaseListener listener, string pchPurchaseId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.Purchase_64(handler.getPurchaseHandler(), pchPurchaseId);
            }
            else
            {
                Internal.IAPurchase.Purchase(handler.getPurchaseHandler(), pchPurchaseId);
            }
        }

        public static void Query(IAPurchaseListener listener, string pchPurchaseId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.Query_64(handler.getQueryHandler(), pchPurchaseId);
            }
            else
            {
                Internal.IAPurchase.Query(handler.getQueryHandler(), pchPurchaseId);
            }
        }

        public static void Query(IAPurchaseListener listener)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.Query_64(handler.getQueryListHandler());
            }
            else
            {
                Internal.IAPurchase.Query(handler.getQueryListHandler());
            }
        }

        public static void GetBalance(IAPurchaseListener listener)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.GetBalance_64(handler.getBalanceHandler());
            }
            else
            {
                Internal.IAPurchase.GetBalance(handler.getBalanceHandler());
            }
        }

        public static void RequestSubscription(
                IAPurchaseListener listener,
                string pchPrice,
                string pchFreeTrialType,
                int nFreeTrialValue,
                string pchChargePeriodType,
                int nChargePeriodValue,
                int nNumberOfChargePeriod,
                string pchPlanId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.RequestSubscription_64(
                    handler.getRequestSubscriptionHandler(),
                    pchPrice,
                    pchFreeTrialType,
                    nFreeTrialValue,
                    pchChargePeriodType,
                    nChargePeriodValue,
                    nNumberOfChargePeriod,
                    pchPlanId
                );
            }
            else
            {
                Internal.IAPurchase.RequestSubscription(
                    handler.getRequestSubscriptionHandler(),
                    pchPrice,
                    pchFreeTrialType,
                    nFreeTrialValue,
                    pchChargePeriodType,
                    nChargePeriodValue,
                    nNumberOfChargePeriod,
                    pchPlanId
                );
            }
            
        }

        public static void RequestSubscriptionWithPlanID(IAPurchaseListener listener, string pchPlanId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.RequestSubscriptionWithPlanID_64(handler.getRequestSubscriptionWithPlanIDHandler(), pchPlanId);
            }
            else
            {
                Internal.IAPurchase.RequestSubscriptionWithPlanID(handler.getRequestSubscriptionWithPlanIDHandler(), pchPlanId);
            }
        }

        public static void Subscribe(IAPurchaseListener listener, string pchSubscriptionId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.Subscribe_64(handler.getSubscribeHandler(), pchSubscriptionId);
            }
            else
            {
                Internal.IAPurchase.Subscribe(handler.getSubscribeHandler(), pchSubscriptionId);
            }
        }

        public static void QuerySubscription(IAPurchaseListener listener, string pchSubscriptionId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.QuerySubscription_64(handler.getQuerySubscriptionHandler(), pchSubscriptionId);
            }
            else
            {
                Internal.IAPurchase.QuerySubscription(handler.getQuerySubscriptionHandler(), pchSubscriptionId);
            }
        }

        public static void QuerySubscriptionList(IAPurchaseListener listener)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.QuerySubscriptionList_64(handler.getQuerySubscriptionListHandler());
            }
            else
            {
                Internal.IAPurchase.QuerySubscriptionList(handler.getQuerySubscriptionListHandler());
            }
        }

        public static void CancelSubscription(IAPurchaseListener listener, string pchSubscriptionId)
        {
            var handler = new IAPHandler(listener);
            if (Environment.Is64BitProcess)
            {
                Internal.IAPurchase.CancelSubscription_64(handler.getCancelSubscriptionHandler(), pchSubscriptionId);
            }
            else
            {
                Internal.IAPurchase.CancelSubscription(handler.getCancelSubscriptionHandler(), pchSubscriptionId);
            }
        }

        private partial class IAPHandler : BaseHandler
        {
            static IAPurchaseListener listener;

            public IAPHandler(IAPurchaseListener cb)
            {
                listener = cb;
            }

            #region IsReady

            public Internal.IAPurchaseCallback getIsReadyHandler()
            {
                return IsReadyHandler;
            }

            /*
             * TODO
             * 
             * Responsed JSON format:
             * {
             *   "statusCode": 500,     // status code, Integer
             *   "currencyName": "",     // user's setting currencyName
             *   "message": "",         // error message information, String
             * }
             * 
             */
            protected override void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[IsReadyHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string currencyName = "";
                string errMessage = "";
                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[IsReadyHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[IsReadyHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            currencyName = (string)jsonData["currencyName"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[IsReadyHandler] currencyName ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[IsReadyHandler] currencyName=" + currencyName);
                    }
                }

                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnSuccess(currencyName);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion IsReady
            #region Request

            public Internal.IAPurchaseCallback getRequestHandler()
            {
                return RequestHandler;
            }

            /*
             * TODO
             * 
             * Responsed JSON format:
             * {
             *   "statusCode": 500,     // status code, Integer
             *   "purchase_id": "",     // specific purchase id, String
             *   "message": "",         // error message information, String
             * }
             * 
             */
            protected override void RequestHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[RequestHandler] message=" + message);

                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string purchaseId = "";
                string errMessage = "";

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[RequestHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[RequestHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            purchaseId = (string)jsonData["purchase_id"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[RequestHandler] purchase_id ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[RequestHandler] purchaseId =" + purchaseId);
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnRequestSuccess(purchaseId);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion Request

            #region Purchase

            public Internal.IAPurchaseCallback getPurchaseHandler()
            {
                return PurchaseHandler;
            }

            /*
             * TODO
             * 
             * Responsed JSON format:
             * {
             *   "statusCode": 500,     // status code, Integer
             *   "purchase_id": "",     // specific purchase id, String
             *   "paid_timestamp": 0,   // paid_timestamp in milli seconds, Long,
             *   "message": "",         // error message information, String
             * }
             * 
             */
            protected override void PurchaseHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[PurchaseHandler] message=" + message);

                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string purchaseId = "";
                string errMessage = "";
                long paid_timestamp = 0L;

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[PurchaseHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[PurchaseHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            purchaseId = (string)jsonData["purchase_id"];
                            paid_timestamp = (long)jsonData["paid_timestamp"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[PurchaseHandler] purchase_id,paid_timestamp ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[PurchaseHandler] purchaseId =" + purchaseId + ",paid_timestamp=" + paid_timestamp);
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnPurchaseSuccess(purchaseId);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion Purchase

            #region Query

            public Internal.IAPurchaseCallback getQueryHandler()
            {
                return QueryHandler;
            }


            /*
             * TODO
             * 
             * Responsed JSON format:
             * 
             * {
             *   "order_id": "",                // , String
             *   "purchase_id": "string",       // , String
             *   "status": "string",            // , String
             *   "app_id": "string",            // , String
             *   "price": "string",             // , String
             *   "item_list": [
             *     {
             *       "item_id": "string",           // , String
             *       "quantity": 0,                 // , Integer
             *       "subtotal_price": "string",    // , String
             *       "category": "string",          // , String
             *       "description": "string"        // , String
             *     }
             *   ],
             *   "currency": "string",          // , String
             *   "paid_timestamp": 0,           // epoch time in milliseconds, Long
             *   "user_data": "string"          // , String
             * }
             * 
             */
            protected override void QueryHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[QueryHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string purchaseId = "";
                string errMessage = "";
                string order_id = "";
                string status = "";
                string price = "";
                string currency = "";
                long paid_timestamp = 0L;

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[QueryHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[QueryHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            purchaseId = (string)jsonData["purchase_id"];
                            order_id = (string)jsonData["order_id"];
                            status = (string)jsonData["status"];
                            price = (string)jsonData["price"];
                            currency = (string)jsonData["currency"];
                            paid_timestamp = (long)jsonData["paid_timestamp"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[QueryHandler] purchase_id, order_id ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[QueryHandler] status =" + status + ",price=" + price + ",currency=" + currency);
                        Viveport.Core.Logger.Log("[QueryHandler] purchaseId =" + purchaseId + ",order_id=" + order_id + ",paid_timestamp=" + paid_timestamp);
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            QueryResponse response = new QueryResponse();
                            response.purchase_id = purchaseId;
                            response.order_id = order_id;
                            response.price = price;
                            response.currency = currency;
                            response.paid_timestamp = paid_timestamp;
                            response.status = status;
                            listener.OnQuerySuccess(response);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }

                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }

                /*
                if (listener != null)
                {
                    if (code == 0)
                    {
                        //string sampleText = "{\"order_id\":\"response_order_id_000\",\"purchase_id\":null,\"status\":\"response_status_000\",\"price\":null,\"item_list\":null,\"currency\":null,\"paid_timestamp\":0,\"user_data\":null}";
                        QueryResponse response = JsonMapper.ToObject<QueryResponse>(message);
                        if (response != null && string.IsNullOrEmpty(response.message))
                        {
                            listener.OnQuerySuccess(response);
                        }
                        else
                        {
                            int statusCode = 999;
                            if (response != null && !string.IsNullOrEmpty(response.code))
                            {
                                // TODO code shoud be Integer
                                string[] codes = response.code.Split('.');
                                if (codes != null && codes.Length > 0)
                                    statusCode = Int32.Parse(codes[0]);
                            }

                            string errMessage = (response != null) ? response.message : "";
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
                */
            }

            #endregion Query
            #region QueryList

            public Internal.IAPurchaseCallback getQueryListHandler()
            {
                return QueryListHandler;
            }

            protected override void QueryListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[QueryListHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                int total = 0;
                int from = 0;
                int to = 0;
                List<QueryResponse2> purchaseList = new List<QueryResponse2>();
                string errMessage = "";

                if (code == 0)
                {

                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[QueryListHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[QueryListHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            JsonData purchaseData = JsonMapper.ToObject(errMessage);
                            total = (int)purchaseData["total"];
                            from = (int)purchaseData["from"];
                            to = (int)purchaseData["to"];
                            JsonData purchases = (JsonData)purchaseData["purchases"];
                            bool isArray = purchases.IsArray;
                            foreach (JsonData jd in purchases)
                            {
                                QueryResponse2 q = new QueryResponse2();
                                var dic = (jd as IDictionary);
                                q.app_id = dic.Contains("app_id") ? (string)jd["app_id"] : "";
                                q.currency = dic.Contains("currency") ? (string)jd["currency"] : "";
                                q.purchase_id = dic.Contains("purchase_id") ? (string)jd["purchase_id"] : "";
                                q.order_id = dic.Contains("order_id") ? (string)jd["order_id"] : "";
                                q.price = dic.Contains("price") ? (string)jd["price"] : "";
                                q.user_data = dic.Contains("user_data") ? (string)jd["user_data"] : "";
                                if (dic.Contains("paid_timestamp"))
                                {
                                    if (jd["paid_timestamp"].IsLong)
                                    {
                                        q.paid_timestamp = (long)jd["paid_timestamp"];
                                    }
                                    else if (jd["paid_timestamp"].IsInt)
                                    {
                                        q.paid_timestamp = (int)jd["paid_timestamp"];
                                    }
                                }
                                purchaseList.Add(q);
                            }
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[QueryListHandler] purchase_id, order_id ex=" + ex);
                        }
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            QueryListResponse response = new QueryListResponse();
                            response.total = total;
                            response.from = from;
                            response.to = to;
                            response.purchaseList = purchaseList;
                            listener.OnQuerySuccess(response);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }

                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion QueryList
            #region GetBalance

            public Internal.IAPurchaseCallback getBalanceHandler()
            {
                return BalanceHandler;
            }
            /*
             * TODO
             * 
             * Responsed JSON format:
             * {
             *   "statusCode": 500,     // status code, Integer
             *   "currencyName": "USD",     // currency name, String
             *   "balance": "100",     // balance, String
             *   "message": "",         // error message information, String
             * }
             * 
             */
            protected override void BalanceHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[BalanceHandler] code=" + code + ",message= " + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string currencyName = "";
                string balance = "";
                string errMessage = "";

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[BalanceHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[BalanceHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            currencyName = (string)jsonData["currencyName"];
                            balance = (string)jsonData["balance"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[BalanceHandler] currencyName, balance ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[BalanceHandler] currencyName=" + currencyName + ",balance=" + balance);
                    }
                }

                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnBalanceSuccess(balance);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }
            #endregion GetBalance
            #region RequestSubscription

            public Internal.IAPurchaseCallback getRequestSubscriptionHandler()
            {
                return RequestSubscriptionHandler;
            }

            /*
             * 
             * Responsed JSON format:
             * 
             *  {
             *      "statusCode",  0,
             *      "subscription_id": "subscription_id_string",
             *      "message", "success"
             *  }
             * 
             */
            protected override void RequestSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[RequestSubscriptionHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string subscription_id = "";
                string errMessage = "";

                try
                {
                    statusCode = (int)jsonData["statusCode"];
                    errMessage = (string)jsonData["message"];
                }
                catch (Exception ex)
                {
                    Viveport.Core.Logger.Log("[RequestSubscriptionHandler] statusCode, message ex=" + ex);
                }
                Viveport.Core.Logger.Log("[RequestSubscriptionHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                if (statusCode == 0)
                {
                    try
                    {
                        subscription_id = (string)jsonData["subscription_id"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[RequestSubscriptionHandler] subscription_id ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[RequestSubscriptionHandler] subscription_id =" + subscription_id);
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnRequestSubscriptionSuccess(subscription_id);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion RequestSubscription
            #region RequestSubscriptionWithPlanID

            public Internal.IAPurchaseCallback getRequestSubscriptionWithPlanIDHandler()
            {
                return RequestSubscriptionWithPlanIDHandler;
            }

            /*
             * 
             * Responsed JSON format:
             * 
             *  {
             *      "statusCode",  0,
             *      "subscription_id": "subscription_id_string",
             *      "message", "success"
             *  }
             * 
             */
            protected override void RequestSubscriptionWithPlanIDHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[RequestSubscriptionWithPlanIDHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string subscription_id = "";
                string errMessage = "";

                try
                {
                    statusCode = (int)jsonData["statusCode"];
                    errMessage = (string)jsonData["message"];
                }
                catch (Exception ex)
                {
                    Viveport.Core.Logger.Log("[RequestSubscriptionWithPlanIDHandler] statusCode, message ex=" + ex);
                }
                Viveport.Core.Logger.Log("[RequestSubscriptionWithPlanIDHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                if (statusCode == 0)
                {
                    try
                    {
                        subscription_id = (string)jsonData["subscription_id"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[RequestSubscriptionWithPlanIDHandler] subscription_id ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[RequestSubscriptionWithPlanIDHandler] subscription_id =" + subscription_id);
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnRequestSubscriptionWithPlanIDSuccess(subscription_id);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion RequestSubscriptionWithPlanID
            #region Subscribe

            public Internal.IAPurchaseCallback getSubscribeHandler()
            {
                return SubscribeHandler;
            }

            /*
             * TODO
             * 
             * Responsed JSON format:
             * 
             *  {
             *      "statusCode",  0,
             *      "subscription_id": "subscription_id_string",
             *      "subscribed_timestamp": 0,
             *      "plan_id": "string",
             *      "message", "success"
             *  }
             * 
             */
            protected override void SubscribeHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[SubscribeHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string subscription_id = "";
                string errMessage = "";
                string plan_id = "";
                long subscribed_timestamp = 0L;

                try
                {
                    statusCode = (int)jsonData["statusCode"];
                    errMessage = (string)jsonData["message"];
                }
                catch (Exception ex)
                {
                    Viveport.Core.Logger.Log("[SubscribeHandler] statusCode, message ex=" + ex);
                }
                Viveport.Core.Logger.Log("[SubscribeHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                if (statusCode == 0)
                {
                    try
                    {
                        subscription_id = (string)jsonData["subscription_id"];
                        plan_id = (string)jsonData["plan_id"];
                        subscribed_timestamp = (long)jsonData["subscribed_timestamp"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[SubscribeHandler] subscription_id, plan_id ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[SubscribeHandler] subscription_id =" + subscription_id + ",plan_id=" + plan_id);
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnSubscribeSuccess(subscription_id);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion Subscribe

            #region QuerySubscription

            public Internal.IAPurchaseCallback getQuerySubscriptionHandler()
            {
                return QuerySubscriptionHandler;
            }

            /*
            {
              "statusCode": 0,
              "message": "success",
              "subscriptions": [
                {
                  "app_id": "app001",
                  "order_id": "dev001",
                  "subscription_id": "s001",
                  "price": "10",
                  "currency": "USD",
                  "subscribed_timestamp": 1486905703000,
                  "free_trial_period": {"time_type":"month", "value":1},
                  "charge_period": {"time_type":"month", "value":1},
                  "number_of_charge_period": 3,
                  "plan_id": "01",
                  "status": "ACTIVE",
                  "status_detail": {"date_next_charge":1486905703000,
                  "transactions":[{"create_time":1486905703000, "payment_method":"wallet", "status":"finish"}],
                  "cancel_reason":"none"}
                }
              ]
            }
            */

            protected override void QuerySubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[QuerySubscriptionHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string errMessage = "";
                List<Subscription> subscriptions = null;

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[QuerySubscriptionHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[QuerySubscriptionHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            QuerySubscritionResponse querySubscritionResponse = JsonMapper.ToObject<QuerySubscritionResponse>(message);
                            subscriptions = querySubscritionResponse.subscriptions;
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[QuerySubscriptionHandler] ex =" + ex);
                        }
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0 && subscriptions != null && subscriptions.Count > 0)
                        {
                            listener.OnQuerySubscriptionSuccess(subscriptions.ToArray());
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion QuerySubscription

            #region QuerySubscriptionList

            public Internal.IAPurchaseCallback getQuerySubscriptionListHandler()
            {
                return QuerySubscriptionListHandler;
            }

            /*
            {
              "statusCode": 0,
              "message": "success",
              "subscriptions": [
                {
                  "app_id": "app001",
                  "order_id": "dev001",
                  "subscription_id": "s001",
                  "price": "10",
                  "currency": "USD",
                  "subscribed_timestamp": 1486905703000,
                  "free_trial_period": {"time_type":"month", "value":1},
                  "charge_period": {"time_type":"month", "value":1},
                  "number_of_charge_period": 3,
                  "plan_id": "01",
                  "status": "ACTIVE",
                  "status_detail": {"date_next_charge":1486905703000,
                  "transactions":[{"create_time":1486905703000, "payment_method":"wallet", "status":"finish"}],
                  "cancel_reason":"none"}
                }
              ]
            }
            */

            protected override void QuerySubscriptionListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[QuerySubscriptionListHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                string errMessage = "";
                QuerySubscritionResponse querySubscritionResponse = null;

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[QuerySubscriptionListHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[QuerySubscriptionListHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        try
                        {
                            querySubscritionResponse = JsonMapper.ToObject<QuerySubscritionResponse>(message);
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[QuerySubscriptionListHandler] ex =" + ex);
                        }
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnQuerySubscriptionListSuccess(querySubscritionResponse);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion QuerySubscriptionList

            #region CancelSubscription

            public Internal.IAPurchaseCallback getCancelSubscriptionHandler()
            {
                return CancelSubscriptionHandler;
            }

            /*
             * Responsed JSON format:
             * {
             *   "statusCode": 500,     // status code, Integer
             *   "message": "",         // error message information, String
             * }
            */

            protected override void CancelSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
            {
                Viveport.Core.Logger.Log("[CancelSubscriptionHandler] message=" + message);
                JsonData jsonData = JsonMapper.ToObject(message);
                int statusCode = -1;
                bool isCanceled = false;
                string errMessage = "";

                if (code == 0)
                {
                    try
                    {
                        statusCode = (int)jsonData["statusCode"];
                        errMessage = (string)jsonData["message"];
                    }
                    catch (Exception ex)
                    {
                        Viveport.Core.Logger.Log("[CancelSubscriptionHandler] statusCode, message ex=" + ex);
                    }
                    Viveport.Core.Logger.Log("[CancelSubscriptionHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                    if (statusCode == 0)
                    {
                        isCanceled = true;
                        Viveport.Core.Logger.Log("[CancelSubscriptionHandler] isCanceled = " + isCanceled);
                    }
                }
                if (listener != null)
                {
                    if (code == 0)
                    {
                        // TODO The actual success judgement.
                        if (statusCode == 0)
                        {
                            listener.OnCancelSubscriptionSuccess(isCanceled);
                        }
                        else
                        {
                            listener.OnFailure(statusCode, errMessage);
                        }
                    }
                    else
                    {
                        listener.OnFailure(code, message);
                    }
                }
            }

            #endregion CancelSubscription
        }

        private abstract partial class BaseHandler
        {
            protected abstract void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void RequestHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void PurchaseHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void QueryHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void QueryListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void BalanceHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void RequestSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void RequestSubscriptionWithPlanIDHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void SubscribeHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void QuerySubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void QuerySubscriptionListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            protected abstract void CancelSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
        }

        public partial class IAPurchaseListener
        {
            public virtual void OnSuccess(string pchCurrencyName) { }
            public virtual void OnRequestSuccess(string pchPurchaseId) { }
            public virtual void OnPurchaseSuccess(string pchPurchaseId) { }
            public virtual void OnQuerySuccess(QueryResponse response) { }
            public virtual void OnQuerySuccess(QueryListResponse response) { }
            public virtual void OnBalanceSuccess(string pchBalance) { }
            public virtual void OnFailure(int nCode, string pchMessage) { }
            public virtual void OnRequestSubscriptionSuccess(string pchSubscriptionId) { }
            public virtual void OnRequestSubscriptionWithPlanIDSuccess(string pchSubscriptionId) { }
            public virtual void OnSubscribeSuccess(string pchSubscriptionId) { }
            public virtual void OnQuerySubscriptionSuccess(Subscription[] subscriptionlist) { }
            public virtual void OnQuerySubscriptionListSuccess(QuerySubscritionResponse querySubscritionResponset) { }
            public virtual void OnCancelSubscriptionSuccess(bool bCanceled) { }
        }

        public class QueryResponse
        {
            public string order_id { get; set; }
            public string purchase_id { get; set; }
            public string status { get; set; }//the value of status is "created" or "processing" or "success" or "failure" or "expired"
            public string price { get; set; }
            public string currency { get; set; }
            public long paid_timestamp { get; set; }
        }

        public class QueryResponse2
        {
            public string order_id { get; set; }
            public string app_id { get; set; }
            public string purchase_id { get; set; }
            public string user_data { get; set; }
            public string price { get; set; }
            public string currency { get; set; }
            public long paid_timestamp { get; set; }
        }

        public class QueryListResponse
        {
            public int total { get; set; }
            public int from { get; set; }
            public int to { get; set; }
            public List<QueryResponse2> purchaseList;
        }

        public class StatusDetailTransaction
        {
            public long create_time { get; set; }
            public string payment_method { get; set; }
            public string status { get; set; }//paymentFailed/pendingWebhook/finish
        }

        public class StatusDetail
        {
            public long date_next_charge { get; set; }
            public StatusDetailTransaction[] transactions { get; set; }
            public string cancel_reason { get; set; }
        }

        public class TimePeriod
        {
            public string time_type { get; set; }
            public int value { get; set; }
        }

        public class Subscription
        {
            public string app_id { get; set; }
            public string order_id { get; set; }
            public string subscription_id { get; set; }
            public string price { get; set; }
            public string currency { get; set; }
            public long subscribed_timestamp { get; set; }
            public TimePeriod free_trial_period { get; set; }
            public TimePeriod charge_period { get; set; }
            public int number_of_charge_period { get; set; }
            public string plan_id { get; set; }
            public string plan_name { get; set; }
            //the value of status is "created" or "processing" or "failure" or "expired" or "ACTIVE" or "NON_RENEWING" or "CANCELED"
            public string status { get; set; }
            public StatusDetail status_detail { get; set; }
        }

        public class QuerySubscritionResponse
        {
            public int statusCode { get; set; }
            public string message { get; set; }
            public List<Subscription> subscriptions { get; set; }
        }
    }

    public partial class Token
    {
#if !UNITY_ANDROID
        public static void IsReady(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            if (Environment.Is64BitProcess)
            {
                Internal.Token.IsReady_64(internalCallback);
            }
            else
            {
                Internal.Token.IsReady(internalCallback);
            }
        }

        public static void GetSessionToken(StatusCallback2 callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback2(callback);
            if (Environment.Is64BitProcess)
            {
                Internal.Token.GetSessionToken_64(internalCallback);
            }
            else
            {
                Internal.Token.GetSessionToken(internalCallback);
            }
        }
#endif
    }

    public partial class Dlc
    {
#if !UNITY_ANDROID
        private const int AppIdLength = 37;

        public static int IsReady(StatusCallback callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback(callback);
            if (Environment.Is64BitProcess)
            {
                return Internal.Dlc.IsReady_64(internalCallback);
            }
            else
            {
                return Internal.Dlc.IsReady(internalCallback);
            }
        }

        public static int GetCount()
        {
            if (Environment.Is64BitProcess)
            {
                return Internal.Dlc.GetCount_64();
            }
            else
            {
                return Internal.Dlc.GetCount();
            }
        }

        public static bool GetIsAvailable(int index, out string appId, out bool isAvailable)
        {
            bool isInRange = false;

            var appIdSB = new StringBuilder(AppIdLength);
            if (Environment.Is64BitProcess)
            {
                isInRange = Internal.Dlc.GetIsAvailable_64(index, appIdSB, out isAvailable);
            }
            else
            {
                isInRange = Internal.Dlc.GetIsAvailable(index, appIdSB, out isAvailable);
            }
            appId = appIdSB.ToString();

            return isInRange;
        }
#endif
    }

    public partial class Subscription
    {
#if !UNITY_ANDROID

        public static void IsReady(StatusCallback2 callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("callback == null");
            }

            var internalCallback = new Internal.StatusCallback2(callback);            
            if (Environment.Is64BitProcess)
            {
                Internal.Subscription.IsReady_64(internalCallback);
            }
            else
            {
                Internal.Subscription.IsReady(internalCallback);
            }
        }

        public static SubscriptionStatus GetUserStatus()
        {
            var status = new SubscriptionStatus();

            if (Environment.Is64BitProcess)
            {
                if (Internal.Subscription.IsWindowsSubscriber_64())
                {
                    status.Platforms.Add(SubscriptionStatus.Platform.Windows);
                }
                if (Internal.Subscription.IsAndroidSubscriber_64())
                {
                    status.Platforms.Add(SubscriptionStatus.Platform.Android);
                }

                switch (Internal.Subscription.GetTransactionType_64())
                {
                    case Internal.ESubscriptionTransactionType.UNKNOWN:
                        status.Type = SubscriptionStatus.TransactionType.Unknown;
                        break;
                    case Internal.ESubscriptionTransactionType.PAID:
                        status.Type = SubscriptionStatus.TransactionType.Paid;
                        break;
                    case Internal.ESubscriptionTransactionType.REDEEM:
                        status.Type = SubscriptionStatus.TransactionType.Redeem;
                        break;
                    case Internal.ESubscriptionTransactionType.FREEE_TRIAL:
                        status.Type = SubscriptionStatus.TransactionType.FreeTrial;
                        break;
                    default:
                        status.Type = SubscriptionStatus.TransactionType.Unknown;
                        break;
                }
            }
            else
            {
                if (Internal.Subscription.IsWindowsSubscriber())
                {
                    status.Platforms.Add(SubscriptionStatus.Platform.Windows);
                }
                if (Internal.Subscription.IsAndroidSubscriber())
                {
                    status.Platforms.Add(SubscriptionStatus.Platform.Android);
                }

                switch (Internal.Subscription.GetTransactionType())
                {
                    case Internal.ESubscriptionTransactionType.UNKNOWN:
                        status.Type = SubscriptionStatus.TransactionType.Unknown;
                        break;
                    case Internal.ESubscriptionTransactionType.PAID:
                        status.Type = SubscriptionStatus.TransactionType.Paid;
                        break;
                    case Internal.ESubscriptionTransactionType.REDEEM:
                        status.Type = SubscriptionStatus.TransactionType.Redeem;
                        break;
                    case Internal.ESubscriptionTransactionType.FREEE_TRIAL:
                        status.Type = SubscriptionStatus.TransactionType.FreeTrial;
                        break;
                    default:
                        status.Type = SubscriptionStatus.TransactionType.Unknown;
                        break;
                }
            }

            return status;
        }
#endif
    }

    namespace Arcade
    {
        partial class Session
        {
#if !UNITY_ANDROID
            public static void IsReady(SessionListener listener)
            {
                SessionHandler handler = new SessionHandler(listener);
                if (Environment.Is64BitProcess)
                {
                    Internal.Arcade.Session.IsReady_64(handler.getIsReadyHandler());
                }
                else
                {
                    Internal.Arcade.Session.IsReady(handler.getIsReadyHandler());
                }
            }
            public static void Start(SessionListener listener)
            {
                SessionHandler handler = new SessionHandler(listener);
                if (Environment.Is64BitProcess)
                {
                    Internal.Arcade.Session.Start_64(handler.getStartHandler());
                }
                else
                {
                    Internal.Arcade.Session.Start(handler.getStartHandler());
                }
            }
            public static void Stop(SessionListener listener)
            {
                SessionHandler handler = new SessionHandler(listener);
                if (Environment.Is64BitProcess)
                {
                    Internal.Arcade.Session.Stop_64(handler.getStopHandler());
                }
                else
                {
                    Internal.Arcade.Session.Stop(handler.getStopHandler());
                }                
            }
            partial class SessionHandler : BaseHandler
            {
                static SessionListener listener;

                public SessionHandler(SessionListener cb)
                {
                    listener = cb;
                }

            #region IsReady
                public Internal.Arcade.SessionCallback getIsReadyHandler()
                {
                    return IsReadyHandler;
                }

                /*
                 * 
                 * Responsed JSON format:
                 * {
                 *   "statusCode": 0, // status code, Integer, 0 is success
                 *   "appID": "",     // app ID that is passed by Api.Init(), String
                 *   "message": "",   // error message information
                 * }
                 * 
                 */
                protected override void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
                {
                    //code is 0(ipc successful) or 20001(Functions are not supported) 
                    Viveport.Core.Logger.Log("[Session IsReadyHandler] message=" + message + ",code=" + code);
                    JsonData jsonData = JsonMapper.ToObject(message);
                    int statusCode = -1;
                    string errMessage = "";
                    string appID = "";

                    if (code == 0)
                    {
                        try
                        {
                            statusCode = (int)jsonData["statusCode"];
                            errMessage = (string)jsonData["message"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[IsReadyHandler] statusCode, message ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[IsReadyHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                        if (statusCode == 0)
                        {
                            try
                            {
                                appID = (string)jsonData["appID"];
                            }
                            catch (Exception ex)
                            {
                                Viveport.Core.Logger.Log("[IsReadyHandler] appID ex=" + ex);
                            }
                            Viveport.Core.Logger.Log("[IsReadyHandler] appID=" + appID);
                        }
                    }

                    if (listener != null)
                    {
                        if (code == 0)
                        {
                            // TODO The actual success judgement.
                            if (statusCode == 0)
                            {
                                listener.OnSuccess(appID);
                            }
                            else
                            {
                                listener.OnFailure(statusCode, errMessage);
                            }
                        }
                        else
                        {
                            listener.OnFailure(code, message);
                        }
                    }
                }
            #endregion IsReady

            #region Start
                public Internal.Arcade.SessionCallback getStartHandler()
                {
                    return StartHandler;
                }

                /*
                 * 
                 * Responsed JSON format:
                 * {
                 *   "statusCode": 0, // status code, Integer, 0 is success
                 *   "appID": "",     // app ID that is passed by Api.Init(), String
                 *   "Guid": "",      // Guid that is generated on every session to represent a unique session, if succes, a valid and unique Guid, if error, an empty Guid 
                 *   "message": "",   // error message information
                 * }
                 * 
                 */
                protected override void StartHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
                {
                    //code is 0(ipc successful) or 20001(Functions are not supported) 
                    Viveport.Core.Logger.Log("[Session StartHandler] message=" + message + ",code=" + code);
                    JsonData jsonData = JsonMapper.ToObject(message);
                    int statusCode = -1;
                    string errMessage = "";
                    string appID = "";
                    string Guid = "";

                    if (code == 0)
                    {
                        try
                        {
                            statusCode = (int)jsonData["statusCode"];
                            errMessage = (string)jsonData["message"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[StartHandler] statusCode, message ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[StartHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                        if (statusCode == 0)
                        {
                            try
                            {
                                appID = (string)jsonData["appID"];
                                Guid = (string)jsonData["Guid"];
                            }
                            catch (Exception ex)
                            {
                                Viveport.Core.Logger.Log("[StartHandler] appID, Guid ex=" + ex);
                            }
                            Viveport.Core.Logger.Log("[StartHandler] appID=" + appID + ",Guid=" + Guid);
                        }
                    }

                    if (listener != null)
                    {
                        if (code == 0)
                        {
                            // TODO The actual success judgement.
                            if (statusCode == 0)
                            {
                                listener.OnStartSuccess(appID, Guid);
                            }
                            else
                            {
                                listener.OnFailure(statusCode, errMessage);
                            }
                        }
                        else
                        {
                            listener.OnFailure(code, message);
                        }
                    }
                }
            #endregion Start

            #region Stop
                public Internal.Arcade.SessionCallback getStopHandler()
                {
                    return StopHandler;
                }

                /*
                 * 
                 * Responsed JSON format:
                 * {
                 *   "statusCode": 0, // status code, Integer, 0 is success
                 *   "appID": "",     // app ID that is passed by Api.Init(), String
                 *   "Guid": "",      // Guid that is the same as the Guid received by the callback of Start(), if succes, a valid and unique Guid, if error, an empty Guid 
                 *   "message": "",   // error message information
                 * }
                 * 
                 */
                protected override void StopHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
                {
                    //code is 0(ipc successful) or 20001(Functions are not supported) 
                    Viveport.Core.Logger.Log("[Session StopHandler] message=" + message + ",code=" + code);
                    JsonData jsonData = JsonMapper.ToObject(message);
                    int statusCode = -1;
                    string errMessage = "";
                    string appID = "";
                    string Guid = "";

                    if (code == 0)
                    {
                        try
                        {
                            statusCode = (int)jsonData["statusCode"];
                            errMessage = (string)jsonData["message"];
                        }
                        catch (Exception ex)
                        {
                            Viveport.Core.Logger.Log("[StopHandler] statusCode, message ex=" + ex);
                        }
                        Viveport.Core.Logger.Log("[StopHandler] statusCode =" + statusCode + ",errMessage=" + errMessage);
                        if (statusCode == 0)
                        {
                            try
                            {
                                appID = (string)jsonData["appID"];
                                Guid = (string)jsonData["Guid"];
                            }
                            catch (Exception ex)
                            {
                                Viveport.Core.Logger.Log("[StopHandler] appID, Guid ex=" + ex);
                            }
                            Viveport.Core.Logger.Log("[StopHandler] appID=" + appID + ",Guid=" + Guid);
                        }
                    }

                    if (listener != null)
                    {
                        if (code == 0)
                        {
                            // TODO The actual success judgement.
                            if (statusCode == 0)
                            {
                                listener.OnStopSuccess(appID, Guid);
                            }
                            else
                            {
                                listener.OnFailure(statusCode, errMessage);
                            }
                        }
                        else
                        {
                            listener.OnFailure(code, message);
                        }
                    }
                }
            #endregion Stop
            }
            abstract partial class BaseHandler
            {
                protected abstract void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
                protected abstract void StartHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
                protected abstract void StopHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
            }

            public partial class SessionListener
            {
                public virtual void OnSuccess(string pchAppID) { }
                public virtual void OnStartSuccess(string pchAppID, string pchGuid) { }
                public virtual void OnStopSuccess(string pchAppID, string pchGuid) { }
                public virtual void OnFailure(int nCode, string pchMessage) { }
            }
#endif
        }
    }
}
