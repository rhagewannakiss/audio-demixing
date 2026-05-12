using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.UI.ViewModels;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using System.Linq;

namespace AudioStemPlayer.UI.Views
{
    public partial class EqualizerWindow : Window
    {
        private EqualizerViewModel? _vm;
        private readonly Dictionary<Canvas, (Rectangle track, Rectangle fill, Ellipse thumb)> _sliderParts = new();

        public EqualizerWindow()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                _vm = DataContext as EqualizerViewModel;
            };
        }

        public static EqualizerWindow Create(IEqService eqService)
        {
            var window = new EqualizerWindow
            {
                DataContext = new EqualizerViewModel(eqService)
            };
            return window;
        }

        

        private void OnSliderLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is not Canvas canvas) return;

            var track = canvas.Children.OfType<Rectangle>().FirstOrDefault(r => r.Name == "TrackBg");
            var fill  = canvas.Children.OfType<Rectangle>().FirstOrDefault(r => r.Name == "FillRect");
            var thumb = canvas.Children.OfType<Ellipse>().FirstOrDefault(el => el.Name == "Thumb");
            if (track == null || fill == null || thumb == null) return;

            _sliderParts[canvas] = (track, fill, thumb);

            canvas.LayoutUpdated += (_, _) =>
            {
                if (canvas.Tag is EqualizerBand band)
                    SetSliderVisual(band, canvas);
            };

            if (canvas.Tag is EqualizerBand initialBand)
                SetSliderVisual(initialBand, canvas);
        }

        private void SetSliderVisual(EqualizerBand band, Canvas canvas)
        {
            if (!_sliderParts.TryGetValue(canvas, out var parts)) return;

            double h = canvas.Bounds.Height;
            if (h <= 0) return;


            parts.track.Height = h;


            
            
            double rel = (band.Gain + 15) / 30.0;
            double fillH = rel * h;
            parts.fill.Height = fillH;
            Canvas.SetTop(parts.fill, h - fillH);


            double thumbY = Math.Clamp(h - fillH - parts.thumb.Height / 2, 0, h - parts.thumb.Height);
            Canvas.SetTop(parts.thumb, thumbY);
        }

        private void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Canvas canvas) return;
            if (canvas.Tag is not EqualizerBand band) return;

            var p = e.GetCurrentPoint(canvas);
            if (!p.Properties.IsLeftButtonPressed) return;

            e.Pointer.Capture(canvas);
            UpdateGainFromPosition(canvas, band, p.Position.Y);
            e.Handled = true;
        }

        private void OnSliderPointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is not Canvas canvas) return;
            if (canvas.Tag is not EqualizerBand band) return;

            if (!ReferenceEquals(e.Pointer.Captured, canvas)) return;

            UpdateGainFromPosition(canvas, band, e.GetPosition(canvas).Y);
        }

        private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Pointer.Capture(null);
        }

        private void UpdateGainFromPosition(Canvas canvas, EqualizerBand band, double y)
        {
            double h = canvas.Bounds.Height;
            if (h <= 0) return;

            double fraction = 1.0 - Math.Clamp(y / h, 0, 1);
            double gain = (fraction - 0.5) * 30.0;
            gain = Math.Round(gain * 2, MidpointRounding.AwayFromZero) / 2;
            band.Gain = Math.Clamp(gain, -15, 15);
        }
    }
}