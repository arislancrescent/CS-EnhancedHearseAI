using ICities;

namespace EnhancedHearseAI
{
    public class Identity : IUserMod
    {
        public string Name
        {
            get { return Settings.Instance.Tag; }
        }

        public string Description
        {
            get { return "Oversees death services to ensure that hearses are dispatched in an efficient manner."; }
        }
    }
}