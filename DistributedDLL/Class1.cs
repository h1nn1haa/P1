namespace DistributedDLL
{
    public class DistributedClass
    {
        /// <summary>
        /// A private class vairable that an application can only access via API commands
        /// </summary>
        private int _setting;

        /// <summary>
        /// Constructs a DistributedClass with _setting set to some initial value.
        /// </summary>
        /// <param name="initSetting">
        /// The initial value of _setting
        /// </param>
        public DistributedClass(int initSetting)
        {

            this._setting = initSetting;
#if DEBUG
            Console.WriteLine("DEBUG DLL - initial state: " + this._setting.ToString());
#endif
        }

        public int getSetting()
        {
            return this._setting;
        }

        public void incSetting()
        {
            this._setting++;
        }
    }
}