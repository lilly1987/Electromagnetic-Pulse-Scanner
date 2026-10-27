using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ElectromagneticPulseGenerator
{
    public static class EPGAutoMineUtility
    {
        public static void DesignateDepositAndMineShaft(Map map, List<IntVec3> deposit)
        {
            HashSet<IntVec3> depositCells = new HashSet<IntVec3>(deposit);
            foreach (IntVec3 cell in deposit)
            {
                Mineable mineable = map.thingGrid.ThingAt<Mineable>(cell);
                if (mineable != null &&
                    mineable.def.building != null &&
                    mineable.def.building.isResourceRock &&
                    map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) == null)
                {
                    map.designationManager.AddDesignation(new Designation(mineable, DesignationDefOf.Mine));
                }
            }

            IntVec3? bestStart = null;
            IntVec3 bestEnd = IntVec3.Invalid;
            int bestNewMineables = int.MaxValue;
            int bestDistance = int.MaxValue;
            bool bestUsesExistingMineDesignation = false;

            IntVec3? closestGround = FindClosestUnfoggedCell(map, deposit);
            if (closestGround.HasValue)
            {
                ConsiderRoutes(
                    map,
                    closestGround.Value,
                    false,
                    deposit,
                    depositCells,
                    ref bestStart,
                    ref bestEnd,
                    ref bestNewMineables,
                    ref bestDistance,
                    ref bestUsesExistingMineDesignation);
            }

            foreach (IntVec3 cell in map.AllCells)
            {
                Mineable mineable = map.thingGrid.ThingAt<Mineable>(cell);
                if (mineable == null ||
                    depositCells.Contains(cell) ||
                    map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) == null)
                {
                    continue;
                }

                ConsiderRoutes(
                    map,
                    cell,
                    true,
                    deposit,
                    depositCells,
                    ref bestStart,
                    ref bestEnd,
                    ref bestNewMineables,
                    ref bestDistance,
                    ref bestUsesExistingMineDesignation);
            }

            if (!bestStart.HasValue)
            {
                return;
            }

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(bestStart.Value, bestEnd))
            {
                if (depositCells.Contains(cell))
                {
                    continue;
                }

                Mineable mineable = map.thingGrid.ThingAt<Mineable>(cell);
                if (mineable != null &&
                    map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) == null)
                {
                    map.designationManager.AddDesignation(new Designation(mineable, DesignationDefOf.Mine));
                }
            }
        }

        private static void ConsiderRoutes(
            Map map,
            IntVec3 start,
            bool usesExistingMineDesignation,
            List<IntVec3> deposit,
            HashSet<IntVec3> depositCells,
            ref IntVec3? bestStart,
            ref IntVec3 bestEnd,
            ref int bestNewMineables,
            ref int bestDistance,
            ref bool bestUsesExistingMineDesignation)
        {
            foreach (IntVec3 end in deposit)
            {
                int newMineables = 0;
                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start, end))
                {
                    if (depositCells.Contains(cell))
                    {
                        continue;
                    }

                    if (map.thingGrid.ThingAt<Mineable>(cell) != null &&
                        map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) == null)
                    {
                        newMineables++;
                    }
                }

                int distance = Mathf.RoundToInt(start.DistanceTo(end));
                if (newMineables < bestNewMineables ||
                    (newMineables == bestNewMineables &&
                     usesExistingMineDesignation &&
                     !bestUsesExistingMineDesignation) ||
                    (newMineables == bestNewMineables &&
                     usesExistingMineDesignation == bestUsesExistingMineDesignation &&
                     distance < bestDistance))
                {
                    bestStart = start;
                    bestEnd = end;
                    bestNewMineables = newMineables;
                    bestDistance = distance;
                    bestUsesExistingMineDesignation = usesExistingMineDesignation;
                }
            }
        }

        private static IntVec3? FindClosestUnfoggedCell(Map map, List<IntVec3> deposit)
        {
            int minDist = int.MaxValue;
            IntVec3? best = null;
            bool bestIsStraight = false;

            foreach (IntVec3 cell in map.AllCells)
            {
                if (!map.fogGrid.IsFogged(cell) && cell.Standable(map))
                {
                    int dist = Mathf.RoundToInt(deposit.Min(d => d.DistanceTo(cell)));
                    bool isStraight = deposit.Any(d => d.x == cell.x || d.z == cell.z);
                    if (dist < minDist || (dist == minDist && isStraight && !bestIsStraight))
                    {
                        minDist = dist;
                        best = cell;
                        bestIsStraight = isStraight;
                    }
                }
            }

            return best;
        }
    }
}
