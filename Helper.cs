using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

using ICities;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Math;
using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedHearseAI
{
    internal sealed class Helper
    {
        private static Helper _Instance = null;
        public static Helper Instance {
			get {
				if (Helper._Instance == null)
					Helper._Instance = new Helper (); // TODO: Why do we need a instance at all?
				return Helper._Instance;
			}
		}

        internal bool GameLoaded = false;

        public void Log(string message)
        {
            Debug.Log(String.Format("{0}: {1}", Settings.Instance.Tag, message));
        }

        public void NotifyPlayer(string message)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, String.Format("{0}: {1}", Settings.Instance.Tag, message));
            Log(message);
        }

        public static double GetAngleDifference(double a, double b)
        {
            if (a < 0) a = Math.PI * 2 + a;
            if (b < 0) b = Math.PI * 2 + b;

            double diff = a - b;

            if (diff > Math.PI)
                diff -= Math.PI * 2;
            else if (diff < -Math.PI)
                diff += Math.PI * 2;

            return diff;
        }
    }
}