using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SourceGit.Views
{
    public class CommitGraph : Control
    {
        public static readonly DirectProperty<CommitGraph, Models.CommitGraph> GraphProperty =
            AvaloniaProperty.RegisterDirect<CommitGraph, Models.CommitGraph>(
                nameof(Graph),
                static o => o.Graph,
                static (o, v) => o.Graph = v);

        public Models.CommitGraph Graph
        {
            get => _graph;
            set => SetAndRaise(GraphProperty, ref _graph, value);
        }

        public static readonly DirectProperty<CommitGraph, Models.CommitGraphLayout> LayoutProperty =
            AvaloniaProperty.RegisterDirect<CommitGraph, Models.CommitGraphLayout>(
                nameof(Layout),
                static o => o.Layout,
                static (o, v) => o.Layout = v);

        public Models.CommitGraphLayout Layout
        {
            get => _layout;
            set => SetAndRaise(LayoutProperty, ref _layout, value);
        }

        public static readonly StyledProperty<IBrush> DotBrushProperty =
            AvaloniaProperty.Register<CommitGraph, IBrush>(nameof(DotBrush), Brushes.Transparent);

        public IBrush DotBrush
        {
            get => GetValue(DotBrushProperty);
            set => SetValue(DotBrushProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_graph == null || _layout == null)
                return;

            var startY = _layout.StartY;
            var clipWidth = _layout.ClipWidth;
            var clipHeight = Bounds.Height;
            var rowHeight = _layout.RowHeight;
            var endY = startY + clipHeight + 28;

            using (context.PushClip(new Rect(0, 0, clipWidth, clipHeight)))
            using (context.PushTransform(Matrix.CreateTranslation(0, -startY)))
            {
                DrawCurves(context, _graph, startY, endY, rowHeight);
                DrawAnchors(context, _graph, startY, endY, rowHeight);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == GraphProperty ||
                change.Property == LayoutProperty ||
                change.Property == DotBrushProperty)
                InvalidateVisual();
        }

        private void DrawCurves(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight)
        {
            var grayedPen = new Pen(new SolidColorBrush(Colors.Gray, 0.4), Models.CommitGraph.Pens[0].Thickness);

            foreach (var link in graph.Links)
            {
                var startY = link.Start.Y * rowHeight;
                var endY = link.End.Y * rowHeight;

                if (endY < top)
                    continue;
                if (startY > bottom)
                    break;

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    var from = new Point(link.Start.X, startY);
                    ctx.BeginFigure(from, false);
                    CornerHorizontalFirst(ctx, from, new Point(link.End.X, endY));
                }

                var pen = link.IsHighlighted ? Models.CommitGraph.Pens[link.Color] : grayedPen;
                context.DrawGeometry(null, pen, geo);
            }

            foreach (var line in graph.Paths)
            {
                var last = new Point(line.Points[0].X, line.Points[0].Y * rowHeight);
                var size = line.Points.Count;
                var endY = line.Points[size - 1].Y * rowHeight;

                if (endY < top)
                    continue;
                if (last.Y > bottom)
                    break;

                var geo = new StreamGeometry();
                var pen = line.IsHighlighted ? Models.CommitGraph.Pens[line.Color] : grayedPen;

                using (var ctx = geo.Open())
                {
                    var started = false;
                    var ended = false;
                    for (int i = 1; i < size; i++)
                    {
                        var cur = new Point(line.Points[i].X, line.Points[i].Y * rowHeight);
                        if (cur.Y < top)
                        {
                            last = cur;
                            continue;
                        }

                        if (!started)
                        {
                            ctx.BeginFigure(last, false);
                            started = true;
                        }

                        if (cur.Y > bottom)
                        {
                            cur = new Point(cur.X, bottom);
                            ended = true;
                        }

                        if (cur.X > last.X)
                        {
                            CornerHorizontalFirst(ctx, last, cur);
                        }
                        else if (cur.X < last.X)
                        {
                            if (i < size - 1)
                                LaneShift(ctx, last, cur);
                            else
                                CornerVerticalFirst(ctx, last, cur);
                        }
                        else
                        {
                            ctx.LineTo(cur);
                        }

                        if (ended)
                            break;
                        last = cur;
                    }
                }

                context.DrawGeometry(null, pen, geo);
            }
        }

        private void DrawAnchors(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight)
        {
            var dotFill = DotBrush;
            var grayedPen = new Pen(Brushes.Gray, Models.CommitGraph.Pens[0].Thickness);

            foreach (var dot in graph.Dots)
            {
                var center = new Point(dot.Center.X, dot.Center.Y * rowHeight);

                if (center.Y < top)
                    continue;
                if (center.Y > bottom)
                    break;

                var pen = dot.IsHighlighted ? Models.CommitGraph.Pens[dot.Color] : grayedPen;
                switch (dot.Type)
                {
                    case Models.CommitGraph.DotType.Head:
                        context.DrawEllipse(dotFill, pen, center, 5.5, 5.5);
                        context.DrawEllipse(pen.Brush, null, center, 2.5, 2.5);
                        break;
                    case Models.CommitGraph.DotType.Merge:
                        context.DrawEllipse(dotFill, pen, center, 4.5, 4.5);
                        break;
                    default:
                        context.DrawEllipse(pen.Brush, null, center, 3, 3);
                        break;
                }
            }
        }

        // Runs horizontally along `from.Y`, then bends down into `to.X` (branch-off / merge link).
        private static void CornerHorizontalFirst(StreamGeometryContext ctx, Point from, Point to)
        {
            var dy = to.Y - from.Y;
            if (dy <= 0)
            {
                ctx.LineTo(to);
                return;
            }

            var dir = to.X > from.X ? 1 : -1;
            var r = Math.Min(CornerRadius, Math.Min(Math.Abs(to.X - from.X), dy));
            ctx.LineTo(new Point(to.X - dir * r, from.Y));
            ctx.ArcTo(new Point(to.X, from.Y + r), new Size(r, r), 0, false, dir > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            ctx.LineTo(to);
        }

        // Runs down along `from.X`, then bends sideways into `to` (path ending at a commit dot).
        private static void CornerVerticalFirst(StreamGeometryContext ctx, Point from, Point to)
        {
            var dy = to.Y - from.Y;
            if (dy <= 0)
            {
                ctx.LineTo(to);
                return;
            }

            var dir = to.X > from.X ? 1 : -1;
            var r = Math.Min(CornerRadius, Math.Min(Math.Abs(to.X - from.X), dy));
            ctx.LineTo(new Point(from.X, to.Y - r));
            ctx.ArcTo(new Point(from.X + dir * r, to.Y), new Size(r, r), 0, false, dir > 0 ? SweepDirection.CounterClockwise : SweepDirection.Clockwise);
            ctx.LineTo(to);
        }

        // Moves a lane sideways: down, across at mid height, down again.
        private static void LaneShift(StreamGeometryContext ctx, Point from, Point to)
        {
            var dy = to.Y - from.Y;
            if (dy <= 0)
            {
                ctx.LineTo(to);
                return;
            }

            var dir = to.X > from.X ? 1 : -1;
            var r = Math.Min(CornerRadius, Math.Min(Math.Abs(to.X - from.X), dy) / 2);
            var midY = (from.Y + to.Y) / 2;
            ctx.LineTo(new Point(from.X, midY - r));
            ctx.ArcTo(new Point(from.X + dir * r, midY), new Size(r, r), 0, false, dir > 0 ? SweepDirection.CounterClockwise : SweepDirection.Clockwise);
            ctx.LineTo(new Point(to.X - dir * r, midY));
            ctx.ArcTo(new Point(to.X, midY + r), new Size(r, r), 0, false, dir > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            ctx.LineTo(to);
        }

        private const double CornerRadius = 6;

        private Models.CommitGraph _graph = null;
        private Models.CommitGraphLayout _layout = null;
    }
}
