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

        private Dictionary<ushort, float> _master;
        private HashSet<ushort> _primary;
        private HashSet<ushort> _secondary;
        private List<ushort> _checkups;

        public Cemetery(ushort id, ref Dictionary<ushort, float> master)
        {
            _id = id;

            _master = master;
            _primary = new HashSet<ushort>();
            _secondary = new HashSet<ushort>();
            _checkups = new List<ushort>();
        }

        public void AddPickup(ushort id)
        {
            if (!_master.ContainsKey(id))
                _master.Add(id, float.PositiveInfinity);

            if (_primary.Contains(id) || _secondary.Contains(id))
                return;

            if (WithinPrimaryRange(id))
                _primary.Add(id);
            else if (WithinSecondaryRange(id))
                _secondary.Add(id);
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
                if ((current != 0 && !SkylinesOverwatch.Data.Instance.IsBuildingWithDead(current) && WithinPrimaryRange(current)) || _checkups.Count == 0)
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
                    if (_master.ContainsKey(current))
                        _master[current] = float.PositiveInfinity;
                }

                float distance = (hearse.GetLastFramePosition() - buildings[target].m_position).sqrMagnitude;

                if (_master.ContainsKey(target))
                    _master[target] = distance;
            }

            return target;
        }

        private ushort GetClosestTarget(Vehicle hearse, ref HashSet<ushort> targets)
        {
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            List<ushort> removals = new List<ushort>();

            ushort target = hearse.m_targetBuilding;
            bool targetProblematic = false;
            float distance = float.PositiveInfinity;

            Vector3 velocity = hearse.GetLastFrameVelocity();
            Vector3 position = hearse.GetLastFramePosition();

            double bearing = double.PositiveInfinity;
            double facing = Math.Atan2(velocity.z, velocity.x);

            if (targets.Contains(target))
            {
                if (!SkylinesOverwatch.Data.Instance.IsBuildingWithDead(target))
                {
                    removals.Add(target);
                    target = 0;
                }
                else
                {
                    targetProblematic = (buildings[target].m_problems & Notification.Problem.Death) != Notification.Problem.None;

                    Vector3 a = buildings[target].m_position;

                    distance = (a - position).sqrMagnitude;
                    bearing = Math.Atan2(a.z - position.z, a.x - position.x);
                }
            }
            else
                target = 0;

            foreach (ushort id in targets)
            {
                if (target == id)
                    continue;
                
                if (!SkylinesOverwatch.Data.Instance.IsBuildingWithDead(id))
                {
                    removals.Add(id);
                    continue;
                }

                Vector3 p = buildings[id].m_position;
                float d = (p - position).sqrMagnitude;

                bool candidateProblematic = (buildings[id].m_problems & Notification.Problem.Death) != Notification.Problem.None;

                if (!float.IsPositiveInfinity(_master[id]))
                {
                    if (d > 2500 || d > _master[id])
                        continue;

                    if (d > distance)
                        continue;

                    double angle = Math.Abs(facing - Math.Atan2(p.z - position.z, p.x - position.x));

                    if (angle > 1.5707963267948966)
                        continue;
                }
                else
                {
                    if (targetProblematic && !candidateProblematic)
                        continue;

                    if (!targetProblematic && candidateProblematic)
                    {
                        // No additonal conditions at the moment. Problematic buildings always have priority over nonproblematic buildings
                    }
                    else
                    {
                        if (d > (distance * 0.9))
                            continue;

                        if (!double.IsPositiveInfinity(bearing))
                        {
                            double angle = Math.Abs(bearing - Math.Atan2(p.z - position.z, p.x - position.x));

                            if (angle > 1.5707963267948966)
                                continue;
                        }
                    }
                }

                target = id;
                targetProblematic = candidateProblematic;
                distance = d;
            }

            foreach (ushort id in removals)
            {
                _master.Remove(id);
                targets.Remove(id);
            }

            return target;
        }
    }
}

