using System;
using System.Collections.Generic;

namespace EnhancedHearseAI
{
    public sealed class Settings
    {
        private Settings()
        {
            Tag = "[ARIS] Enhanced Hearse AI";
        }

        private static Settings _Instance = null;
        public static Settings Instance {
			get {
				if (Settings._Instance == null)
					Settings._Instance = new Settings (); // TODO: For one string?!?
				return Settings._Instance;
			}
		}

        public readonly string Tag;
    }
}