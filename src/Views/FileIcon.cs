using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SourceGit.Views
{
    public class FileIcon : Control
    {
        public static readonly StyledProperty<string> FilePathProperty =
            AvaloniaProperty.Register<FileIcon, string>(nameof(FilePath));

        public FileIcon()
        {
            RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        }

        public string FilePath
        {
            get => GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
            if (_icon != null)
            {
                context.DrawImage(_icon, rect);
                return;
            }

            if (this.FindResource("Icons.File") is StreamGeometry geo && this.FindResource("Brush.FG2") is IBrush brush)
            {
                var bounds = geo.Bounds;
                var scale = Math.Min(rect.Width / bounds.Width, rect.Height / bounds.Height) * 0.8;
                var offsetX = (rect.Width - bounds.Width * scale) * 0.5 - bounds.X * scale;
                var offsetY = (rect.Height - bounds.Height * scale) * 0.5 - bounds.Y * scale;
                using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offsetX, offsetY)))
                    context.DrawGeometry(brush, null, geo);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == FilePathProperty)
            {
                _icon = Native.ShellIcons.Get(FilePath);
                InvalidateVisual();
            }
        }

        private Bitmap _icon = null;
    }
}
