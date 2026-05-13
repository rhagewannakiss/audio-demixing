using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AudioStemPlayer.UI.ViewModels;

namespace AudioStemPlayer.UI.Views;

public partial class PlayerPanelView : UserControl
{
    private bool _draggingStart;
    private bool _draggingEnd;
    private bool _draggingPosition;
    private bool _draggingVolume;
    private PlayerPanelViewModel? _vm;

    public PlayerPanelView()
    {
        InitializeComponent();

        this.PropertyChanged += (s, e) =>
        {
            if (e.Property == DataContextProperty)
            {
                if (_vm != null)
                    _vm.PropertyChanged -= OnViewModelPropertyChanged;

                _vm = DataContext as PlayerPanelViewModel;

                if (_vm != null)
                {
                    _vm.PropertyChanged += OnViewModelPropertyChanged;
                    UpdateAllVisuals();
                }
            }
        };

        LoopStartMarker.PointerPressed += OnLoopStartPressed;
        LoopEndMarker.PointerPressed += OnLoopEndPressed;
        PositionThumb.PointerPressed += OnPositionPressed;

        ProgressCanvas.PointerMoved += OnCanvasPointerMoved;
        ProgressCanvas.PointerReleased += OnCanvasPointerReleased;
        ProgressCanvas.LayoutUpdated += OnCanvasLayoutUpdated;
        ProgressCanvas.PointerPressed += OnCanvasPointerPressed;

        VolumeThumb.PointerPressed += OnVolumeThumbPressed;
        VolumeCanvas.PointerMoved += OnVolumeCanvasPointerMoved;
        VolumeCanvas.PointerReleased += OnVolumeCanvasPointerReleased;
        VolumeCanvas.LayoutUpdated += OnVolumeCanvasLayoutUpdated;
        VolumeCanvas.PointerPressed += OnVolumeCanvasPointerPressed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "Position" or "LoopStart" or "LoopEnd" or "IsLoopEnabled" or "Volume")
            UpdateAllVisuals();
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_draggingPosition || _draggingStart || _draggingEnd) return;

        double canvasWidth = ProgressCanvas.Bounds.Width;
        if (canvasWidth <= 0) return;

        _draggingPosition = true;
        e.Pointer.Capture(ProgressCanvas);
        e.Handled = true;

        var pos = e.GetPosition(ProgressCanvas);
        double fraction = Math.Clamp(pos.X / canvasWidth, 0, 1);
        if (_vm != null)
        {
            _vm.Position = fraction;
        }
    }

    private void UpdateAllVisuals()
    {
        if (_vm == null) return;
        UpdateProgressBarVisuals();
        UpdateVolumeVisuals();
    }

    private void UpdateProgressBarVisuals()
    {
        double canvasWidth = ProgressCanvas.Bounds.Width;
        if (canvasWidth <= 0) return;

        TrackBackground.Width = canvasWidth;
        ProgressFill.Width = _vm.Position * canvasWidth;

        if (_vm.IsLoopEnabled)
        {
            InactiveLeft.Width = _vm.LoopStart * canvasWidth;
            InactiveRight.Width = (1 - _vm.LoopEnd) * canvasWidth;
            Canvas.SetLeft(InactiveRight, _vm.LoopEnd * canvasWidth);
        }
        else
        {
            InactiveLeft.Width = 0;
            InactiveRight.Width = 0;
        }

        double thumbX = Math.Clamp(
            _vm.Position * canvasWidth - PositionThumb.Width / 2,
            0,
            canvasWidth - PositionThumb.Width);
        Canvas.SetLeft(PositionThumb, thumbX);

        if (_vm.IsLoopEnabled)
        {
            double startX = Math.Clamp(
                _vm.LoopStart * canvasWidth - LoopStartMarker.Width / 2,
                0,
                canvasWidth - LoopStartMarker.Width);
            double endX = Math.Clamp(
                _vm.LoopEnd * canvasWidth - LoopEndMarker.Width / 2,
                0,
                canvasWidth - LoopEndMarker.Width);
            Canvas.SetLeft(LoopStartMarker, startX);
            Canvas.SetLeft(LoopEndMarker, endX);
        }
    }

    private void OnCanvasLayoutUpdated(object? sender, EventArgs e) => UpdateProgressBarVisuals();

    private void OnLoopStartPressed(object? sender, PointerPressedEventArgs e)
    {
        _draggingStart = true;
        e.Pointer.Capture(LoopStartMarker);
        e.Handled = true;
    }

    private void OnLoopEndPressed(object? sender, PointerPressedEventArgs e)
    {
        _draggingEnd = true;
        e.Pointer.Capture(LoopEndMarker);
        e.Handled = true;
    }

    private void OnPositionPressed(object? sender, PointerPressedEventArgs e)
    {
        _draggingPosition = true;
        e.Pointer.Capture(PositionThumb);
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_vm == null) return;
        double canvasWidth = ProgressCanvas.Bounds.Width;
        if (canvasWidth <= 0) return;
        var pos = e.GetPosition(ProgressCanvas);
        double fraction = Math.Clamp(pos.X / canvasWidth, 0, 1);

        if (_draggingStart)
        {
            if (fraction < _vm.LoopEnd) _vm.LoopStart = fraction;
            else _vm.LoopStart = _vm.LoopEnd - 0.01;
        }
        else if (_draggingEnd)
        {
            if (fraction > _vm.LoopStart) _vm.LoopEnd = fraction;
            else _vm.LoopEnd = _vm.LoopStart + 0.01;
        }
        else if (_draggingPosition)
        {
            _vm.Position = fraction;
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _draggingStart = false;
        _draggingEnd = false;
        _draggingPosition = false;
        e.Pointer.Capture(null);
    }
    
    
    


    
    private void OnVolumeCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_draggingVolume) return;
        double canvasWidth = VolumeCanvas.Bounds.Width;
        if (canvasWidth <= 0) return;
        var pos = e.GetPosition(VolumeCanvas);
        double fraction = Math.Clamp(pos.X / canvasWidth, 0, 1);
        if (_vm != null)
            _vm.Volume = fraction * 100;
        e.Handled = true;
    }

    private void OnVolumeThumbPressed(object? sender, PointerPressedEventArgs e)
    {
        _draggingVolume = true;
        e.Pointer.Capture(VolumeThumb);
        e.Handled = true;
    }

    private void OnVolumeCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingVolume || _vm == null) return;
        double canvasWidth = VolumeCanvas.Bounds.Width;
        if (canvasWidth <= 0) return;
        var pos = e.GetPosition(VolumeCanvas);
        double fraction = Math.Clamp(pos.X / canvasWidth, 0, 1);
        _vm.Volume = fraction * 100;
    }

    private void OnVolumeCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _draggingVolume = false;
        e.Pointer.Capture(null);
    }

    private void UpdateVolumeVisuals()
    {
        double canvasWidth = VolumeCanvas.Bounds.Width;
        if (canvasWidth <= 0 || _vm == null) return;
        VolumeTrackBackground.Width = canvasWidth;
        VolumeProgressFill.Width = (_vm.Volume / 100.0) * canvasWidth;
        double thumbX = Math.Clamp(
            (_vm.Volume / 100.0) * canvasWidth - VolumeThumb.Width / 2,
            0,
            canvasWidth - VolumeThumb.Width);
        Canvas.SetLeft(VolumeThumb, thumbX);
    }

    private void OnVolumeCanvasLayoutUpdated(object? sender, EventArgs e) => UpdateVolumeVisuals();
}
