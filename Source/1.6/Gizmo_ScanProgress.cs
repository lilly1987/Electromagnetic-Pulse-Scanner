using RimWorld;
using UnityEngine;
using Verse;

namespace ElectromagneticPulseGenerator
{
    // Simple gizmo that shows a fillable bar for scan progress to the next guaranteed find
    public class Gizmo_ScanProgress : Gizmo
    {
        private readonly ThingWithComps thing;
        private readonly CompEPGScanner comp;

        public Gizmo_ScanProgress(ThingWithComps thing)
        {
            this.thing = thing;
            comp = thing.GetComp<CompEPGScanner>();
            Order = -100f; // show early
        }

        public override float GetWidth(float maxWidth)
        {
            return 212f;
        }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            float width = GetWidth(maxWidth);
            Rect rect = new Rect(topLeft.x, topLeft.y, width, 75f);
            Widgets.DrawWindowBackground(rect);

            // Title
            Rect titleRect = new Rect(rect.x, rect.y + 5f, rect.width, 22f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "Next guaranteed find");
            Text.Anchor = TextAnchor.UpperLeft;

            // Progress bar
            float progress = comp != null ? Mathf.Clamp01(comp.GetScanProgress()) : 0f;
            Rect barRect = new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, 20f);
            Widgets.FillableBar(barRect, progress);

            // Percent label on the bar
            string percent = progress.ToStringPercent();
            var oldAnchor = Text.Anchor; var oldColor = GUI.color;
            Text.Anchor = TextAnchor.MiddleCenter; GUI.color = Color.white;
            Widgets.Label(barRect, percent);
            Text.Anchor = oldAnchor; GUI.color = oldColor;

            // Tooltip with details
            string tip = comp == null ? "Unavailable" :
                string.Format("Progress: {0}\nRemaining: {1:0.0} days",
                    percent,
                    Mathf.Max((comp.Props.maxScanTimeTicks - GetScanTicksSafe()) / 60000f, 0f));
            TooltipHandler.TipRegion(rect, tip);

            return new GizmoResult(GizmoState.Clear);
        }

        private int GetScanTicksSafe()
        {
            // Access private via progress; approximate based on comp.GetScanProgress
            // scanTicks = progress * maxScanTimeTicks
            float progress = comp != null ? Mathf.Clamp01(comp.GetScanProgress()) : 0f;
            return Mathf.RoundToInt(progress * comp.Props.maxScanTimeTicks);
        }
    }
}
