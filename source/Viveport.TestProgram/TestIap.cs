namespace Viveport.TestProgram
{
    public class TestIap : TestBase, IViveportTest
    {
        private const string VIVEPORT_ID = "2840d776-3bca-4ef7-997f-b25c0c6ae2f3";
        private const string API_KEY = "2vg38ZwKQx-UDotry2RkTCd31yHpz4QG";
        private static TestIap instance;

        public static TestIap GetInstance()
        {
            if (instance == null)
            {
                instance = new TestIap();
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
                IAPurchase.IsReady(new TestIapListener(), API_KEY);
            }
            else
            {
                TestLogger.Error("Init", string.Format("Init failure. Error Code: {0}", errorCode));
            }
        }

        class TestIapListener : IAPurchase.IAPurchaseListener
        {
            public override void OnSuccess(string pchCurrencyName)
            {
                TestLogger.Success("IsReady", string.Format("IsReady success. Currency: {0}", pchCurrencyName));
                IAPurchase.Query(this);
            }

            public override void OnQuerySuccess(IAPurchase.QueryListResponse response)
            {
                TestLogger.Success("Query", "Query success.");
                for (var i = 0; i < response.purchaseList.Count && i < 10; i++)
                {
                    TestLogger.Success("Query", string.Format(
                        "VIVEPORT ID: {0}, Purchase ID: {1}, User Data: {2}, Price: {3}, Currency: {4}, Paid Timestamp: {5}",
                        response.purchaseList[i].app_id,
                        response.purchaseList[i].purchase_id,
                        response.purchaseList[i].user_data,
                        response.purchaseList[i].price,
                        response.purchaseList[i].currency,
                        response.purchaseList[i].paid_timestamp));
                }
                IAPurchase.GetBalance(this);
            }

            public override void OnBalanceSuccess(string pchBalance)
            {
                TestLogger.Success("GetBalance", string.Format("GetBalance success. Balance: {0}", pchBalance));
                IAPurchase.Request(this, "1");
            }

            public override void OnRequestSuccess(string pchPurchaseId)
            {
                TestLogger.Success("Request", string.Format("Request success. Purchase ID: {0}", pchPurchaseId));
                IAPurchase.QuerySubscriptionList(this);
            }

            public override void OnQuerySubscriptionListSuccess(IAPurchase.QuerySubscritionResponse querySubscritionResponset)
            {
                TestLogger.Success("QuerySubscriptionList",
                    string.Format(
                        "QuerySubscriptionList success. Error Code: {0}, Message: {1}",
                        querySubscritionResponset.statusCode,
                        querySubscritionResponset.message));
                for (var i = 0; i < querySubscritionResponset.subscriptions.Count && i < 10; i++)
                {
                    TestLogger.Success(
                        "QuerySubscriptionList",
                        string.Format(
                            "VIVEPORT ID: {0}, Subscription ID: {1}, Price: {2}, Currency: {3}, Subscribed Timestamp: {4}, Free Trial Period: {5} {6}, Charge Period {7} {8}, Number of Charge Period: {9}, Plan ID: {10}, Plan Name: {11}, Status: {12}, Status Detail: {13} {14}",
                            querySubscritionResponset.subscriptions[i].app_id,
                            querySubscritionResponset.subscriptions[i].subscription_id,
                            querySubscritionResponset.subscriptions[i].price,
                            querySubscritionResponset.subscriptions[i].currency,
                            querySubscritionResponset.subscriptions[i].subscribed_timestamp,
                            querySubscritionResponset.subscriptions[i].free_trial_period?.time_type ?? "None",
                            querySubscritionResponset.subscriptions[i].free_trial_period?.value ?? 0,
                            querySubscritionResponset.subscriptions[i].charge_period.time_type,
                            querySubscritionResponset.subscriptions[i].charge_period.value,
                            querySubscritionResponset.subscriptions[i].number_of_charge_period,
                            querySubscritionResponset.subscriptions[i].plan_id,
                            querySubscritionResponset.subscriptions[i].plan_name,
                            querySubscritionResponset.subscriptions[i].status,
                            querySubscritionResponset.subscriptions[i].status_detail?.date_next_charge ?? 0,
                            querySubscritionResponset.subscriptions[i].status_detail?.cancel_reason ?? "None"));
                }

                IAPurchase.RequestSubscription(this, "1", "month", 1, "day", 2, 3, "pID");
            }

            public override void OnRequestSubscriptionSuccess(string pchSubscriptionId)
            {
                TestLogger.Success("RequestSubscription", string.Format("RequestSubscription success. Subscription ID: {0}", pchSubscriptionId));
                Api.Shutdown(GetInstance().TestShutdownCallback);
            }

            public override void OnFailure(int nCode, string pchMessage)
            {
                TestLogger.Error("IAP", string.Format("IAP failure. Error Code: {0}, Message: {1}", nCode, pchMessage));
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
