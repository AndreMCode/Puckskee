using UnityEngine;
using UnityEngine.UIElements;

namespace Puckskee.UI
{
    [UxmlElement]
    public partial class PendulumMeter : VisualElement
    {
        private float _value = 0.5f;
        [UxmlAttribute] public float Value { get => _value; set { _value = value; MarkDirtyRepaint(); } }

        public PendulumMeter()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1 || h < 1) return;

            var p = ctx.painter2D;
            Vector2 center = new(w / 2, h);
            float radius = Mathf.Min(w / 2, h) * 0.9f;

            // Draw Arc
            p.strokeColor = new Color(1, 0.5f, 0, 0.3f);
            p.lineWidth = 4;
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(180), Angle.Degrees(360), ArcDirection.Clockwise);
            p.Stroke();

            // Draw Sweet Spot
            p.strokeColor = new Color(1, 0.5f, 0, 1);
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(260), Angle.Degrees(280), ArcDirection.Clockwise);
            p.Stroke();

            // Draw Needle
            // float angle = 180 + (_value * 180);
            float needleAngle = 180 + (_value * 180);
            Vector2 end = center + new Vector2(Mathf.Cos(needleAngle * Mathf.Deg2Rad), Mathf.Sin(needleAngle * Mathf.Deg2Rad)) * radius;

            p.strokeColor = Color.white;
            p.lineWidth = 6;
            p.BeginPath();
            p.MoveTo(center);
            p.LineTo(end);
            p.Stroke();
        }
    }

    [UxmlElement]
    public partial class FillMeter : VisualElement
    {
        private float _value = 0f;
        [UxmlAttribute] public float Value { get => _value; set { _value = value; MarkDirtyRepaint(); } }

        public FillMeter()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1 || h < 1) return;

            var p = ctx.painter2D;
            float padding = 10;
            float barWidth = w - (padding * 2);
            float barHeight = h - (padding * 2);

            // Background
            p.fillColor = new Color(1, 0.5f, 0, 0.2f);
            p.BeginPath();
            p.MoveTo(new Vector2(padding, padding));
            p.LineTo(new Vector2(padding + barWidth, padding));
            p.LineTo(new Vector2(padding + barWidth, padding + barHeight));
            p.LineTo(new Vector2(padding, padding + barHeight));
            p.ClosePath();
            p.Fill();

            // Fill
            p.fillColor = new Color(1, 0.5f, 0, 1);
            float currentHeight = barHeight * _value;
            p.BeginPath();
            p.MoveTo(new Vector2(padding, padding + barHeight));
            p.LineTo(new Vector2(padding + barWidth, padding + barHeight));
            p.LineTo(new Vector2(padding + barWidth, padding + barHeight - currentHeight));
            p.LineTo(new Vector2(padding, padding + barHeight - currentHeight));
            p.ClosePath();
            p.Fill();
        }
    }

    [UxmlElement]
    public partial class OrbitLoop : VisualElement
    {
        private float _progress = 0f; // 0 to 1
        [UxmlAttribute] public float Progress { get => _progress; set { _progress = value; MarkDirtyRepaint(); } }

        public OrbitLoop()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1 || h < 1) return;

            var p = ctx.painter2D;
            Vector2 viewCenter = new(w / 2, h / 2);
            float radius = Mathf.Min(w, h) * 0.25f;

            Vector2 offset = new(radius * 0.866f, radius * -0.5f);
            Vector2 c1 = viewCenter - offset; // Upper-left
            Vector2 c2 = viewCenter + offset; // Lower-right

            // Paths
            p.strokeColor = new Color(1, 0.5f, 0, 0.3f);
            p.lineWidth = 3;
            p.BeginPath(); p.Arc(c1, radius, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise); p.Stroke();
            p.BeginPath(); p.Arc(c2, radius, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise); p.Stroke();

            // Intersection Point
            p.fillColor = new Color(1, 0.5f, 0, 1);
            p.BeginPath(); p.Arc(viewCenter, 5, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise); p.Fill();

            // Crosshairs move clockwise
            float angle = _progress * 360f;

            // Circle 1 starts at -30 deg (4 o'clock)
            float a1 = -30 - angle;
            Vector2 p1 = c1 + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * radius;

            // Circle 2 starts at 150 deg (10 o'clock)
            float a2 = 150 - angle;
            Vector2 p2 = c2 + new Vector2(Mathf.Cos(a2 * Mathf.Deg2Rad), Mathf.Sin(a2 * Mathf.Deg2Rad)) * radius;

            p.fillColor = Color.white;
            p.BeginPath(); p.Arc(p1, 8, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise); p.Fill();
            p.BeginPath(); p.Arc(p2, 8, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise); p.Fill();
        }
    }
}
