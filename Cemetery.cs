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

        private Dictionary<ushort, float> _primary;
        private Dictionary<ushort, float> _secondary;
        private List<ushort> _checkups;

        public Cemetery(ushort id)
        {
            _id = id;

            _primary = new Dictionary<ushort, float>();
            _secondary = new Dictionary<ushort, float>();
            _checkups = new List<ushort>();
        }

        public void AddPickup(ushort id)
        {
            if (_primary.ContainsKey(id) || _secondary.ContainsKey(id))
                return;

            if (WithinPrimaryRange(id))
                _primary.Add(id, float.PositiveInfinity);
            else if (WithinSecondaryRange(id))
                _secondary.Add(id, float.PositiveInfinity);
        }

        public void AddCheckup(ushort id)
        {
            if (_checkups.Count >= 20)
                return;

            if (WithinPrimaryRange(id))
                _checkups.Add(id);
        }

        private bool WithinPrimaryRange(ushort id)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            Building cemetery = buildings[(int)_id];
            Building target = buildings[(int)id];

            DistrictManager dm = Singleton<DistrictManager>.instance;
            byte district = dm.GetDistrict(cemetery.m_position);

            if (district != dm.GetDistrict(target.m_position))
                return false;

            if (district == 0)
                return WithinSecondaryRange(id);

            return true;
        }

        private bool WithinSecondaryRange(ushort id)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            Building cemetery = buildings[(int)_id];
            Building target = buildings[(int)id];

            float range = cemetery.Info.m_buildingAI.GetCurrentRange(_id, ref cemetery);
            range = range * range;

            float distance = (cemetery.m_position - target.m_position).sqrMagnitude;

            return distance <= range;
        }

        public ushort AssignTarget(Vehicle hearse)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            ushort target = 0;
            ushort current = hearse.m_targetBuilding;

            if (hearse.m_sourceBuilding != _id)
                return target;

            target = GetClosestTarget(hearse, ref _primary);

            if (target == 0)
                target = GetClosestTarget(hearse, ref _secondary);

            if (target == 0)
            {
                if ((current != 0 && WithinPrimaryRange(current)) || _checkups.Count == 0)
                    target = current;
                else
                {
                    target = _checkups[0];
                    _checkups.RemoveAt(0);
                }
            }
            else
            {
                if (target != current)
                {
                    if (_primary.ContainsKey(current))
                        _primary[current] = float.PositiveInfinity;
                    else if (_secondary.ContainsKey(current))
                        _secondary[current] = float.PositiveInfinity;
                }

                float distance = (hearse.GetLastFramePosition() - buildings[target].m_position).sqrMagnitude;

                if (_primary.ContainsKey(target))
                    _primary[target] = distance;
                else if (_secondary.ContainsKey(target))
                    _secondary[target] = distance;
            }

            return target;
        }

        private ushort GetClosestTarget(Vehicle hearse, ref Dictionary<ushort, float> targets)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            List<ushort> removals = new List<ushort>();

            ushort target = 0;
            Vector3 targetP;
            float distance = float.PositiveInfinity;
            double atan2 = double.PositiveInfinity;

            Vector3 position = hearse.GetLastFramePosition();

            if (targets.ContainsKey(hearse.m_targetBuilding))
            {
                if (!SkylinesOverwatch.Data.Instance.IsBuildingWithDead(hearse.m_targetBuilding))
                    removals.Add(hearse.m_targetBuilding);
                else
                {
                    target = hearse.m_targetBuilding;
                    targetP = buildings[target].m_position;
                    distance = (targetP - position).sqrMagnitude;
                    atan2 = Math.Atan2(targetP.z - position.z, targetP.x - position.x);
                }
            }
            
            foreach (ushort id in targets.Keys)
            {
                if (distance <= 200)
                    break;
                
                if (!SkylinesOverwatch.Data.Instance.IsBuildingWithDead(id))
                {
                    removals.Add(id);
                    continue;
                }

                targetP = buildings[id].m_position;

                float d = (targetP - position).sqrMagnitude;

                if (d > (distance - 100))
                    continue;

                if (!float.IsPositiveInfinity(targets[id]) && d > 200)
                    continue;

                if (!double.IsPositiveInfinity(atan2))
                {
                    double angle = Math.Abs(atan2 - Math.Atan2(targetP.z - position.z, targetP.x - position.x));

                    if (angle > 1.5707963267948966)
                        continue;
                }

                target = id;
                distance = d;
            }

            foreach (ushort id in removals)
                targets.Remove(id);

            return target;
        }
    }
}

