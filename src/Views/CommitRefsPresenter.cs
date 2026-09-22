using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace SourceGit.Views
{
    public class CommitRefsIconCache
    {
        public static CommitRefsIconCache Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CommitRefsIconCache();
                return _instance;
            }
        }

        public CommitRefsIconCache()
        {
            _head = LoadIcon("Icons.Head");
            _branch = LoadIcon("Icons.Branch");
            _remote = LoadIcon("Icons.Remote");
            _tag = LoadIcon("Icons.Tag");
        }

        public Geometry GetIcon(Models.DecoratorType type)
        {
            return type switch
            {
                Models.DecoratorType.CurrentBranchHead => _head,
                Models.DecoratorType.CurrentCommitHead => _head,
                Models.DecoratorType.LocalBranchHead => _branch,
                Models.DecoratorType.RemoteBranchHead => _remote,
                Models.DecoratorType.Tag => _tag,
                _ => null,
            };
        }

        private Geometry LoadIcon(string resourceKey)
        {
            var geo = App.Current.FindResource(resourceKey) as StreamGeometry;
            var drawGeo = geo!.Clone();
            var iconBounds = drawGeo.Bounds;
            var translation = Matrix.CreateTranslation(-(Vector)iconBounds.Position);
            var scale = Math.Min(10.0 / iconBounds.Width, 10.0 / iconBounds.Height);
            var center = Matrix.CreateTranslation((10.0 - iconBounds.Width * scale) * 0.5, (10.0 - iconBounds.Height * scale) * 0.5);
            var transform = translation * Matrix.CreateScale(scale, scale) * center;
            if (drawGeo.Transform == null || drawGeo.Transform.Value == Matrix.Identity)
                drawGeo.Transform = new MatrixTransform(transform);
            else
                drawGeo.Transform = new MatrixTransform(drawGeo.Transform.Value * transform);

            return drawGeo;
        }

        private static CommitRefsIconCache _instance = null;
        private Geometry _head = null;
        private Geometry _branch = null;
        private Geometry _remote = null;
        private Geometry _tag = null;
    }

    public class CommitRefsPresenter : Control
    {
        public class RenderItem
        {
            public Models.Decorator Decorator { get; set; } = null;
            public FormattedText Label { get; set; } = null;
            public IBrush Brush { get; set; } = null;
            public bool IsHead { get; set; } = false;
            public List<Geometry> Icons { get; set; } = [];
            public double IconWidth { get; set; } = 0.0;
            public double Width { get; set; } = 0.0;
            public List<FormattedText> Remotes { get; set; } = [];
        }

        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<CommitRefsPresenter>();

        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
           TextBlock.FontSizeProperty.AddOwner<CommitRefsPresenter>();

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, IBrush>(nameof(Background), Brushes.Transparent);

        public IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, IBrush>(nameof(Foreground), Brushes.White);

        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<bool> UseCompactBranchNamesProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, bool>(nameof(UseCompactBranchNames));

        public bool UseCompactBranchNames
        {
            get => GetValue(UseCompactBranchNamesProperty);
            set => SetValue(UseCompactBranchNamesProperty, value);
        }

        public static readonly StyledProperty<bool> HasSingleRemoteProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, bool>(nameof(HasSingleRemote));

        public bool HasSingleRemote
        {
            get => GetValue(HasSingleRemoteProperty);
            set => SetValue(HasSingleRemoteProperty, value);
        }

        public static readonly StyledProperty<bool> UseGraphColorProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, bool>(nameof(UseGraphColor));

        public bool UseGraphColor
        {
            get => GetValue(UseGraphColorProperty);
            set => SetValue(UseGraphColorProperty, value);
        }

        public static readonly StyledProperty<bool> AllowWrapProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, bool>(nameof(AllowWrap));

        public bool AllowWrap
        {
            get => GetValue(AllowWrapProperty);
            set => SetValue(AllowWrapProperty, value);
        }

        public static readonly StyledProperty<bool> ShowTagsProperty =
            AvaloniaProperty.Register<CommitRefsPresenter, bool>(nameof(ShowTags), true);

        public bool ShowTags
        {
            get => GetValue(ShowTagsProperty);
            set => SetValue(ShowTagsProperty, value);
        }

        public Models.Decorator DecoratorAt(Point point)
        {
            var x = 1.5;
            foreach (var item in _items)
            {
                x += item.Width + BadgeGap;
                if (point.X < x)
                    return item.Decorator;
            }

            return null;
        }

        public override void Render(DrawingContext context)
        {
            if (_items.Count == 0)
                return;

            var fg = Foreground;
            var bg = Background;
            var allowWrap = AllowWrap;
            var x = 1.5;
            var y = 0.5;

            context.FillRectangle(Brushes.Transparent, Bounds);

            foreach (var item in _items)
            {
                if (allowWrap && x > 1.5 && x + item.Width > Bounds.Width)
                {
                    x = 1.5;
                    y += BadgeHeight + BadgeGap;
                }

                var pen = new Pen(item.Brush);
                var entireRect = new RoundedRect(new Rect(x, y, item.Width, BadgeHeight), new CornerRadius(BadgeRadius));
                if (bg != null)
                    context.DrawRectangle(bg, null, entireRect);

                var labelX = x + item.IconWidth;
                var labelCorner = item.IconWidth > 0 ? new CornerRadius(0, BadgeRadius, BadgeRadius, 0) : new CornerRadius(BadgeRadius);
                var labelRect = new RoundedRect(new Rect(labelX, y, item.Width - item.IconWidth, BadgeHeight), labelCorner);
                using (context.PushOpacity(item.IsHead ? .32 : .2))
                    context.DrawRectangle(item.Brush, null, labelRect);

                if (item.IconWidth > 0)
                {
                    // The current branch gets a solid icon cell; other icons stay muted.
                    var iconBrush = fg;
                    var iconOpacity = .55;
                    if (item.IsHead)
                    {
                        var iconRect = new RoundedRect(new Rect(x, y, item.IconWidth, BadgeHeight), new CornerRadius(BadgeRadius, 0, 0, BadgeRadius));
                        context.DrawRectangle(item.Brush, null, iconRect);
                        iconBrush = s_headIconBrush;
                        iconOpacity = 1;
                    }

                    context.DrawLine(pen, new Point(labelX, y), new Point(labelX, y + BadgeHeight));

                    using (context.PushOpacity(iconOpacity))
                    {
                        var iconX = x + IconPadding;
                        foreach (var icon in item.Icons)
                        {
                            using (context.PushTransform(Matrix.CreateTranslation(iconX, y + (BadgeHeight - IconSize) * 0.5)))
                                context.DrawGeometry(iconBrush, null, icon);
                            iconX += IconSize + IconSpacing;
                        }
                    }
                }

                var textX = labelX + LabelPaddingLeft;
                context.DrawText(item.Label, new Point(textX, y + (BadgeHeight - item.Label.Height) * 0.5));

                if (item.Remotes.Count > 0)
                {
                    var rx = textX + item.Label.Width + RemoteSpacing;
                    using (context.PushOpacity(.6))
                    {
                        foreach (var remote in item.Remotes)
                        {
                            context.DrawText(remote, new Point(rx, y + (BadgeHeight - remote.Height) * 0.5));
                            rx += remote.Width + RemoteSpacing;
                        }
                    }
                }

                context.DrawRectangle(null, pen, entireRect);
                x += item.Width + BadgeGap;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == FontFamilyProperty ||
                change.Property == FontSizeProperty ||
                change.Property == ForegroundProperty ||
                change.Property == UseGraphColorProperty ||
                change.Property == UseCompactBranchNamesProperty ||
                change.Property == HasSingleRemoteProperty ||
                change.Property == BackgroundProperty ||
                change.Property == ShowTagsProperty)
                InvalidateMeasure();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _items.Clear();

            if (DataContext is not Models.Commit commit)
                return new Size(0, 0);

            var refs = commit.Decorators;
            var count = refs.Count;
            if (count == 0)
            {
                InvalidateVisual();
                return new Size(0, 0);
            }

            var useCompactBranchNames = UseCompactBranchNames;
            var hasSingleRemote = HasSingleRemote;
            var typeface = new Typeface(FontFamily);
            var typefaceHead = new Typeface(FontFamily, FontStyle.Normal, FontWeight.Bold);
            var typefaceRemote = new Typeface(FontFamily, FontStyle.Italic, FontWeight.Normal);
            var fg = Foreground;
            var normalBG = UseGraphColor ? Models.CommitGraph.Pens[commit.Color].Brush : Brushes.Gray;
            var tagBG = UseGraphColor ? s_tagBrush : Brushes.Gray;
            var icons = CommitRefsIconCache.Instance;
            var labelSize = FontSize;
            var requiredHeight = BadgeHeight;
            var x = 0.0;
            var allowWrap = AllowWrap;
            var showTags = ShowTags;
            var skippedIdx = new HashSet<int>();

            for (var i = 0; i < count; i++)
            {
                if (skippedIdx.Contains(i))
                    continue;

                var decorator = refs[i];
                if (!showTags && decorator.Type == Models.DecoratorType.Tag)
                    continue;

                var item = new RenderItem()
                {
                    Decorator = decorator,
                    Brush = decorator.Type == Models.DecoratorType.Tag ? tagBG : normalBG,
                    IsHead = decorator.Type is Models.DecoratorType.CurrentBranchHead or Models.DecoratorType.CurrentCommitHead,
                };
                _items.Add(item);

                item.Label = new FormattedText(
                    decorator.Name,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    item.IsHead ? typefaceHead : typeface,
                    labelSize,
                    fg);

                var onRemote = decorator.Type == Models.DecoratorType.RemoteBranchHead;
                var findRemotes = useCompactBranchNames && (decorator.Type == Models.DecoratorType.CurrentBranchHead || decorator.Type == Models.DecoratorType.LocalBranchHead);
                if (findRemotes)
                {
                    for (var j = i + 1; j < count; j++)
                    {
                        var test = refs[j];
                        if (test.Type != Models.DecoratorType.RemoteBranchHead)
                            continue;

                        var idxOfSlash = test.Name.IndexOf('/');
                        if (idxOfSlash < 1 || idxOfSlash == test.Name.Length - 1)
                            continue;

                        var name = test.Name.Substring(idxOfSlash + 1);
                        if (decorator.Name.Equals(name, StringComparison.Ordinal))
                        {
                            onRemote = true;

                            if (!hasSingleRemote)
                            {
                                item.Remotes.Add(new FormattedText(
                                    test.Name.Substring(0, idxOfSlash),
                                    CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    typefaceRemote,
                                    labelSize,
                                    fg));
                            }

                            skippedIdx.Add(j);
                        }
                    }
                }

                // Icons only where they carry meaning: HEAD gets a check, tags a tag,
                // and branches a cloud only when they exist on a remote.
                switch (decorator.Type)
                {
                    case Models.DecoratorType.CurrentBranchHead:
                    case Models.DecoratorType.CurrentCommitHead:
                        item.Icons.Add(icons.GetIcon(decorator.Type));
                        if (onRemote)
                            item.Icons.Add(icons.GetIcon(Models.DecoratorType.RemoteBranchHead));
                        break;
                    case Models.DecoratorType.Tag:
                        item.Icons.Add(icons.GetIcon(decorator.Type));
                        break;
                    default:
                        if (onRemote)
                            item.Icons.Add(icons.GetIcon(Models.DecoratorType.RemoteBranchHead));
                        break;
                }

                var iconCount = item.Icons.Count;
                item.IconWidth = iconCount > 0 ? IconPadding * 2 + IconSize * iconCount + IconSpacing * (iconCount - 1) : 0;
                item.Width = item.IconWidth + LabelPaddingLeft + item.Label.Width + LabelPaddingRight;
                foreach (var remote in item.Remotes)
                    item.Width += RemoteSpacing + remote.Width;

                x += item.Width + BadgeGap;
                if (allowWrap)
                {
                    if (x > availableSize.Width)
                    {
                        requiredHeight += BadgeHeight + BadgeGap;
                        x = item.Width;
                    }
                }
            }

            double requiredWidth = 0;
            if (_items.Count > 0)
            {
                if (allowWrap && requiredHeight > BadgeHeight)
                    requiredWidth = double.IsInfinity(availableSize.Width) ? x + 2 : availableSize.Width;
                else
                    requiredWidth = x + 2;
            }

            InvalidateVisual();
            return new Size(requiredWidth, requiredHeight);
        }

        private const double BadgeHeight = 18.0;
        private const double BadgeRadius = 3.0;
        private const double BadgeGap = 4.0;
        private const double IconSize = 10.0;
        private const double IconPadding = 4.0;
        private const double IconSpacing = 3.0;
        private const double LabelPaddingLeft = 6.0;
        private const double LabelPaddingRight = 7.0;
        private const double RemoteSpacing = 5.0;

        private static readonly IBrush s_tagBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x9D, 0x8F, 0xE8));
        private static readonly IBrush s_headIconBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xE0, 0x1C, 0x1C, 0x1C));

        private List<RenderItem> _items = new List<RenderItem>();
    }
}
