using System;
using System.Collections.Generic;
using System.Threading;

using ICities;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedHearseAI
{
    public class Cemetery
    {
        private readonly ushort _id;

        private Dictionary<ushort, float> _pickups;
        private List<ushort> _checkups;

        public Cemetery(ushort id)
        {
            _id = id;

            _pickups = new Dictionary<ushort, float>();
            _checkups = new List<ushort>();
        }

        public void AddPickup(ushort id)
        {
            if (_pickups.ContainsKey(id))
                return;

            if (WithinRange(id))
                _pickups.Add(id, float.PositiveInfinity);
        }

        public void AddCheckup(ushort id)
        {
            if (_checkups.Count >= 20)
                return;

            if (WithinRange(id))
                _checkups.Add(id);
        }

        public ushort GetClosestPickup(Vehicle hearse)
        {
            ushort pickup = 0;

            if (hearse.m_sourceBuilding != _id)
                return pickup;

            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            List<ushort> removals = new List<ushort>();
            float distance = float.PositiveInfinity;

            foreach (ushort id in _pickups.Keys)
            {
                if (!SkylinesOverwatch.Data.Instance.IsBuildingsWithDead(id))
                {
                    removals.Add(id);
                    continue;
                }

                float d = (hearse.GetLastFramePosition() - buildings[id].m_position).sqrMagnitude;

                if (d >= distance)
                    continue;
                
                if (d > (_pickups[id] + 100))
                    continue;

                distance = d;
                pickup = id;
            }

            foreach (ushort id in removals)
                _pickups.Remove(id);

            if (pickup != 0)
            {
                if (_pickups.ContainsKey(hearse.m_targetBuilding))
                    _pickups[hearse.m_targetBuilding] = float.PositiveInfinity;
                
                _pickups[pickup] = distance;
            }
            else if (!WithinRange(hearse.m_targetBuilding) && _checkups.Count > 0)
            {
                pickup = _checkups[0];
                _checkups.RemoveAt(0);
            }

            return pickup;
        }

        private bool WithinRange(ushort id)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            Building cemetery = buildings[(int)_id];
            Building pickup = buildings[(int)id];

            DistrictManager dm = Singleton<DistrictManager>.instance;
            byte district = dm.GetDistrict(cemetery.m_position);

            if (district != dm.GetDistrict(pickup.m_position))
                return false;
            
            if (district == 0)
            {
                float range = cemetery.Info.m_buildingAI.GetCurrentRange(_id, ref cemetery);
                range = range * range;

                float distance = (cemetery.m_position - pickup.m_position).sqrMagnitude;

                if (distance > range)
                    return false;
            }

            return true;
        }
    }
}

