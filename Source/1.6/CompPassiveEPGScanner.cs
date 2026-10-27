using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ElectromagneticPulseGenerator
{
    public class CompPassiveEPGScanner : ThingComp
    {
        private const int TickInterval = 250;

        private static FieldInfo cachedUpgradesField;
        private static PropertyInfo cachedUpgradesProperty;

        private int scanTicks;
        private bool autoMineEnabled;

        public CompProperties_PassiveEPGScanner Props => (CompProperties_PassiveEPGScanner)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref scanTicks, "scanTicks", 0);
            Scribe_Values.Look(ref autoMineEnabled, "autoMineEnabled", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!parent.Spawned || parent.Map == null)
            {
                return;
            }

            if (!parent.IsHashIntervalTick(TickInterval))
            {
                return;
            }

            if (!HasRequiredUpgrade())
            {
                return;
            }

            AddProgress(TickInterval);
        }

        private void AddProgress(int amount)
        {
            scanTicks += amount;
            float mtb = Props.meanTimeBetweenFindsDays * 60000f;

            if (Rand.MTBEventOccurs(mtb, 60000f, amount) || scanTicks >= Props.maxScanTimeTicks)
            {
                scanTicks = 0;
                RevealRandomOreDeposit(parent.Map);
            }
        }

        private bool RevealRandomOreDeposit(Map map)
        {
            List<List<IntVec3>> unrevealedDeposits = FindUnrevealedOreDeposits(map);
            if (unrevealedDeposits.Count == 0)
            {
                return false;
            }

            List<IntVec3> deposit = unrevealedDeposits[Rand.Range(0, unrevealedDeposits.Count)];
            foreach (IntVec3 cell in deposit)
            {
                if (map.fogGrid.IsFogged(cell))
                {
                    map.fogGrid.Unfog(cell);
                }
            }

            IntVec3 centerCell = deposit[0];
            Mineable oreThing = map.thingGrid.ThingAt<Mineable>(centerCell);
            string oreLabel = oreThing?.def?.label ?? "ore";
            string shuttleLabel = parent.LabelCap;

            Messages.Message(
                $"{shuttleLabel} passively scanned and revealed a new {oreLabel} deposit.",
                new TargetInfo(centerCell, map),
                MessageTypeDefOf.PositiveEvent);

            if (autoMineEnabled)
            {
                DesignateDepositAndMineShaft(map, deposit);
            }

            return true;
        }

        private void DesignateDepositAndMineShaft(Map map, List<IntVec3> deposit)
        {
            foreach (IntVec3 cell in deposit)
            {
                Mineable mineable = map.thingGrid.ThingAt<Mineable>(cell);
                if (mineable != null && mineable.def.building != null && mineable.def.building.isResourceRock)
                {
                    if (map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) == null)
                    {
                        map.designationManager.AddDesignation(new Designation(mineable, DesignationDefOf.Mine));
                    }
                }
            }

            IntVec3? start = FindClosestUnfoggedCell(map, deposit);
            if (start == null)
            {
                return;
            }

            IntVec3 end = deposit.OrderBy(c => c.DistanceTo(start.Value)).First();
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start.Value, end))
            {
                if (deposit.Contains(cell))
                {
                    continue;
                }

                Mineable mineable = map.thingGrid.ThingAt<Mineable>(cell);
                if (mineable != null && map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) == null)
                {
                    map.designationManager.AddDesignation(new Designation(mineable, DesignationDefOf.Mine));
                }
            }
        }

        private IntVec3? FindClosestUnfoggedCell(Map map, List<IntVec3> deposit)
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

        private List<List<IntVec3>> FindUnrevealedOreDeposits(Map map)
        {
            List<List<IntVec3>> deposits = new List<List<IntVec3>>();
            HashSet<IntVec3> visited = new HashSet<IntVec3>();

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (!(thing is Mineable mineable) || !IsOre(mineable.def))
                {
                    continue;
                }

                IntVec3 start = mineable.Position;
                if (visited.Contains(start))
                {
                    continue;
                }

                List<IntVec3> deposit = new List<IntVec3>();
                bool anyFogged = FloodFillOre(map, start, mineable.def, visited, deposit);
                if (deposit.Count > 0 && anyFogged)
                {
                    deposits.Add(deposit);
                }
            }

            return deposits;
        }

        private bool IsOre(ThingDef def)
        {
            return def.building != null && def.building.isResourceRock;
        }

        private bool FloodFillOre(Map map, IntVec3 start, ThingDef oreDef, HashSet<IntVec3> visited, List<IntVec3> deposit)
        {
            bool anyFogged = false;
            Queue<IntVec3> queue = new Queue<IntVec3>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                deposit.Add(cell);

                if (map.fogGrid.IsFogged(cell))
                {
                    anyFogged = true;
                }

                foreach (IntVec3 adj in GenAdj.AdjacentCellsAndInside)
                {
                    IntVec3 next = cell + adj;
                    if (!next.InBounds(map) || visited.Contains(next))
                    {
                        continue;
                    }

                    Thing thing = map.thingGrid.ThingAt(next, oreDef);
                    if (thing != null && thing.def == oreDef)
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            return anyFogged;
        }

        private bool HasRequiredUpgrade()
        {
            if (string.IsNullOrEmpty(Props.requiredUpgradeKey) || parent.AllComps == null)
            {
                return false;
            }

            ThingComp upgradeTreeComp = null;
            foreach (ThingComp comp in parent.AllComps)
            {
                if (comp != null && comp.GetType().Name == "CompUpgradeTree")
                {
                    upgradeTreeComp = comp;
                    break;
                }
            }

            if (upgradeTreeComp == null)
            {
                return false;
            }

            if (cachedUpgradesField == null && cachedUpgradesProperty == null)
            {
                System.Type compType = upgradeTreeComp.GetType();
                cachedUpgradesField = compType.GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                cachedUpgradesProperty = compType.GetProperty("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            object upgrades = cachedUpgradesField != null
                ? cachedUpgradesField.GetValue(upgradeTreeComp)
                : cachedUpgradesProperty?.GetValue(upgradeTreeComp, null);

            if (!(upgrades is IEnumerable upgradeEnumerable))
            {
                return false;
            }

            foreach (object value in upgradeEnumerable)
            {
                if (value is string key && key == Props.requiredUpgradeKey)
                {
                    return true;
                }
            }

            return false;
        }

        public override string CompInspectStringExtra()
        {
            if (!HasRequiredUpgrade())
            {
                return null;
            }

            float progress = Mathf.Clamp01((float)scanTicks / Mathf.Max(Props.maxScanTimeTicks, 1));
            int remainingTicks = Mathf.Max(Props.maxScanTimeTicks - scanTicks, 0);
            float remainingDays = remainingTicks / 60000f;

            return $"Passive EPG scan: {progress:P0} ({remainingDays:0.0} days)";
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (!HasRequiredUpgrade())
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "Auto-mine revealed deposits",
                defaultDesc = "When enabled, deposits revealed by the passive shuttle scanner and a mine shaft corridor will be designated for mining.",
                icon = ContentFinder<Texture2D>.Get("UI/Auto_Mine_Icon", true),
                isActive = () => autoMineEnabled,
                toggleAction = () => autoMineEnabled = !autoMineEnabled
            };
        }
    }
}
