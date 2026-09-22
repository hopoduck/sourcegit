using System;
using Avalonia;
using Avalonia.Controls;

namespace SourceGit.Views
{
    /// <summary>
    ///     Lays out three children as [left][center][right]. The center child sits on the window's
    ///     horizontal center as long as both side children still fit; the right child fills the rest.
    /// </summary>
    public class WindowCenteredPanel : Panel
    {
        public WindowCenteredPanel()
        {
            // Ancestors' bounds are not final while this panel is being arranged, so re-check once layout settles.
            LayoutUpdated += OnLayoutUpdated;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var width = 0.0;
            var height = 0.0;
            var childAvailable = new Size(double.PositiveInfinity, availableSize.Height);
            foreach (var child in Children)
            {
                child.Measure(childAvailable);
                width += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count != 3)
                return base.ArrangeOverride(finalSize);

            var left = Children[0];
            var center = Children[1];
            var right = Children[2];
            var centerWidth = center.DesiredSize.Width;

            var (originX, windowWidth) = GetPlacement();
            _arrangedOriginX = originX;
            _arrangedWindowWidth = windowWidth;

            var x = windowWidth > 0 ? windowWidth * 0.5 - originX - centerWidth * 0.5 : (finalSize.Width - centerWidth) * 0.5;
            x = Math.Min(x, finalSize.Width - right.DesiredSize.Width - centerWidth);
            x = Math.Max(x, left.DesiredSize.Width);

            var rightX = x + centerWidth;
            left.Arrange(new Rect(0, 0, left.DesiredSize.Width, finalSize.Height));
            center.Arrange(new Rect(x, 0, centerWidth, finalSize.Height));
            right.Arrange(new Rect(rightX, 0, Math.Max(0, finalSize.Width - rightX), finalSize.Height));
            return finalSize;
        }

        private (double, double) GetPlacement()
        {
            if (TopLevel.GetTopLevel(this) is not { } top)
                return (0, 0);

            var origin = this.TranslatePoint(default, top);
            return (origin?.X ?? 0, top.ClientSize.Width);
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            var (originX, windowWidth) = GetPlacement();
            if (Math.Abs(originX - _arrangedOriginX) > 0.5 || Math.Abs(windowWidth - _arrangedWindowWidth) > 0.5)
                InvalidateArrange();
        }

        private double _arrangedOriginX = 0;
        private double _arrangedWindowWidth = 0;
    }
}
