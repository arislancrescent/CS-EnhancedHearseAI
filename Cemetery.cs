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
        private enum SearchDirection
        {
            None,
            Left,
            Right,
            Both,
            Error
        }

        private readonly ushort _id;

        private Dictionary<ushort, Claimant> _master;
        private HashSet<ushort> _primary;
        private HashSet<ushort> _secondary;
        private List<ushort> _checkups;

        public Cemetery(ushort id, ref Dictionary<ushort, Claimant> master)
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
                _master.Add(id, new Claimant(_id, id));

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

        public ushort AssignTarget(ushort hearseID)
        {
            Vehicle hearse = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[hearseID];
            ushort target = 0;

            if (hearse.m_sourceBuilding != _id)
                return target;

            ushort current = hearse.m_targetBuilding;

            if (!SkylinesOverwatch.Data.Instance.IsBuildingWithDead(current))
            {
                _master.Remove(current);
                _primary.Remove(current);
                _secondary.Remove(current);

                current = 0;
            }
            else if (_master.ContainsKey(current))
            {
                if (_master[current].IsValid && _master[current].Hearse != hearseID)
                    current = 0;
            }

            bool immediateOnly = _primary.Contains(current) || _secondary.Contains(current);
            SearchDirection immediateDirection = GetImmediateSearchDirection(hearseID);

            if (immediateOnly && (immediateDirection == SearchDirection.None || immediateDirection == SearchDirection.Error))
                target = current;
            else
            {
                target = GetClosestTarget(hearseID, ref _primary, immediateOnly, immediateDirection);

                if (target == 0)
                    target = GetClosestTarget(hearseID, ref _secondary, immediateOnly, immediateDirection);
            }

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
            else if (_master.ContainsKey(target))
            {
                if (_master[target].Hearse != hearseID)
                    _master[target] = new Claimant(hearseID, target);
            }
            else
                _master.Add(target, new Claimant(hearseID, target));

            return target;
        }

        private SearchDirection GetImmediateSearchDirection(ushort hearseID)
        {
            Vehicle hearse = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[hearseID];

            PathManager pm = Singleton<PathManager>.instance;

            PathUnit pu = pm.m_pathUnits.m_buffer[hearse.m_path];

            byte pi = hearse.m_pathPositionIndex;
            if (pi == 255) pi = 0;

            PathUnit.Position position = pu.GetPosition(pi >> 1);

            NetManager nm = Singleton<NetManager>.instance;

            NetSegment segment = nm.m_segments.m_buffer[position.m_segment];

            int laneCount = 0;

            int leftLane = -1;
            float leftPosition = float.PositiveInfinity;

            int rightLane = -1;
            float rightPosition = float.NegativeInfinity;

            for (int i = 0; i < segment.Info.m_lanes.Length; i++)
            {
                NetInfo.Lane l = segment.Info.m_lanes[i];

                if (l.m_laneType != NetInfo.LaneType.Vehicle || l.m_vehicleType != VehicleInfo.VehicleType.Car)
                    continue;

                laneCount++;

                if (l.m_position < leftPosition)
                {
                    leftLane = i;
                    leftPosition = l.m_position;
                }

                if (l.m_position > rightPosition)
                {
                    rightLane = i;
                    rightPosition = l.m_position;
                }
            }

            SearchDirection dir = SearchDirection.Error;

            if (laneCount == 0)
            {
                dir = SearchDirection.None;
            }
            else if (position.m_lane != leftLane && position.m_lane != rightLane)
            {
                dir = SearchDirection.None;
            }
            else if (leftLane == rightLane)
            {
                dir = SearchDirection.Both;
            }
            else if (laneCount == 2 && segment.Info.m_lanes[leftLane].m_direction != segment.Info.m_lanes[rightLane].m_direction)
                dir = SearchDirection.Both;
            else 
            {   
                if (position.m_lane == leftLane)
                    dir = SearchDirection.Left;
                else
                    dir = SearchDirection.Right;
            }

            return dir;
        }

        private ushort GetClosestTarget(ushort hearseID, ref HashSet<ushort> targets, bool immediateOnly, SearchDirection immediateDirection)
        {
            Vehicle hearse = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[hearseID];

            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            List<ushort> removals = new List<ushort>();

            ushort target = hearse.m_targetBuilding;
            if (_master.ContainsKey(target) && _master[target].IsValid && _master[target].Hearse != hearseID)
                target = 0;
            
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
            else if (!immediateOnly)
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

                if (_master.ContainsKey(id) && _master[id].IsValid && !_master[id].IsChallengable)
                    continue;
                
                Vector3 p = buildings[id].m_position;
                float d = (p - position).sqrMagnitude;

                bool candidateProblematic = (buildings[id].m_problems & Notification.Problem.Death) != Notification.Problem.None;

                double angle = Helper.GetAngleDifference(facing, Math.Atan2(p.z - position.z, p.x - position.x));

                bool isImmediate = IsImmediate(d, angle, immediateDirection);

                #region debug

                string bname = Singleton<BuildingManager>.instance.GetBuildingName(id, new InstanceID { Building = id });
                string vname = Singleton<VehicleManager>.instance.GetVehicleName(hearseID);

                if (bname.Contains("##") && vname.Contains("##"))
                {
                    Helper.Instance.NotifyPlayer(String.Format("{0} :: {1} :: {2} :: {3}", d, angle, immediateDirection, isImmediate));
                }

                #endregion

                if (_master.ContainsKey(id) && _master[id].IsValid && _master[id].IsChallengable)
                {
                    if (d > distance)
                        continue;

                    if (d > _master[id].Distance)
                        continue;

                    if (!isImmediate)
                        continue;
                }
                else
                {
                    if (immediateOnly && !isImmediate)
                        continue;
                    
                    if (targetProblematic && !candidateProblematic)
                        continue;

                    if (!targetProblematic && candidateProblematic)
                    {
                        // No additonal conditions at the moment. Problematic buildings always have priority over nonproblematic buildings
                    }
                    else
                    {
                        if (d > distance)
                            continue;

                        if (isImmediate)
                        {
                            // If it's that close, no need to further qualify its priority
                        }
                        else if (IsAlongTheWay(d, angle))
                        {
                            // If it's in the general direction the vehicle is facing, it's good enough
                        }
                        else if (!double.IsPositiveInfinity(bearing))
                        {
                            angle = Helper.GetAngleDifference(bearing, Math.Atan2(p.z - position.z, p.x - position.x));

                            if (IsAlongTheWay(d, angle))
                            {
                                // If it's in the general direction along the vehicle's target path, we will have to settle for it at this point
                            }
                            else 
                                continue;
                        }
                        else
                        {
                            // If it's not closeby and not in the direction the vehicle is facing, but our vehicle also has no bearing, we will take whatever is out there
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

        private bool IsImmediate(float distance, double angle, SearchDirection immediateDirection)
        {
            // -90 degrees to 90 degrees. This is the default search angle
            double l = -1.5707963267948966;
            double r = 1.5707963267948966;

            if (distance < Settings.Instance.ImmediateRange1)
            {
                // Restrict to 90 degrees of the side the vehicle is allowed to search on
                if (immediateDirection == SearchDirection.Left)
                    r = 0;
                else if (immediateDirection == SearchDirection.Right)
                    l = 0;
            }
            else if (distance < Settings.Instance.ImmediateRange2)
            {
                // Restrict the search on the opposite side to 60 degrees to give enough space for merging
                if (immediateDirection == SearchDirection.Left)
                    r = 1.0471975512;
                else if (immediateDirection == SearchDirection.Right)
                    l = -1.0471975512;
            }
            else 
                return false;

            return angle >= l && angle <= r;
        }

        private bool IsAlongTheWay(float distance, double angle)
        {
            if (distance < Settings.Instance.ImmediateRange2) // This is within the immediate range. Use IsImmediate() instead
                return false;

            // -90 degrees to 90 degrees. This is the default search angle
            double l = -1.5707963267948966;
            double r = 1.5707963267948966;

            return angle >= l && angle <= r;
        }
    }
}

