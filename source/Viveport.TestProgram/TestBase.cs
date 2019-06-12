namespace Viveport.TestProgram
{
    public abstract class TestBase
    {
        public const int SUCCESS = 0;

        public delegate void Callback();
        private Callback callback = null;

        public void SetCallback(Callback callback)
        {
            this.callback = callback;
        }

        protected void OnTestFinished()
        {
            callback?.Invoke();
        }
    }
}
