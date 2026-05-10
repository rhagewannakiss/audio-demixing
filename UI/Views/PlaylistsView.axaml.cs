using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.VisualTree;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.UI.ViewModels;
using Avalonia.Platform.Storage;
using System.Collections.Generic;

namespace AudioStemPlayer.UI.Views;

public partial class PlaylistsView : UserControl
{
    private bool _isDragging;
    private TrackInfo? _draggedTrack;
    private int _draggedIndex = -1;
    private Point _dragStartPoint;

    public PlaylistsView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnExternalDrop);
        AddHandler(DragDrop.DragOverEvent, OnExternalDragOver);

        TracksItemsControl.PointerPressed += OnPointerPressed;
        TracksItemsControl.PointerMoved += OnPointerMoved;
        TracksItemsControl.PointerReleased += OnPointerReleased;
    }

    private void OnExternalDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private async void OnExternalDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files == null || !files.Any()) return;
        if (DataContext is PlaylistsViewModel vm)
            await vm.ImportDroppedFiles(files.ToList());
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;
        var container = source?.FindAncestorOfType<ContentPresenter>();
        if (container == null) return;
        if (container.Content is not TrackInfo track) return;

        var point = e.GetCurrentPoint(container);
        if (!point.Properties.IsLeftButtonPressed) return;

        _draggedTrack = track;
        _draggedIndex = ((PlaylistsViewModel)DataContext!).PlaylistTracks.IndexOf(track);
        _dragStartPoint = e.GetPosition(TracksItemsControl);
        _isDragging = false;
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTrack == null || DataContext is not PlaylistsViewModel vm) return;

        if (!_isDragging)
        {
            _isDragging = true;

            if (VisualRoot is TopLevel topLevel)
                topLevel.Cursor = new Cursor(StandardCursorType.DragMove);
        }

        var position = e.GetPosition(TracksItemsControl);
        var hitVisual = TracksItemsControl.InputHitTest(position) as Visual;
        var targetContainer = hitVisual?.FindAncestorOfType<ContentPresenter>();
        TrackInfo? targetTrack = targetContainer?.Content as TrackInfo;
        if (targetTrack == null || targetTrack == _draggedTrack) return;

        int targetIndex = vm.PlaylistTracks.IndexOf(targetTrack);
        if (targetIndex == _draggedIndex) return;

        vm.PlaylistTracks.Move(_draggedIndex, targetIndex);
        _draggedIndex = targetIndex;
    }

    private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedTrack == null || DataContext is not PlaylistsViewModel vm) return;

        if (_isDragging)
        {
            await vm.ReorderTrack(_draggedTrack, _draggedIndex);
        }

        if (VisualRoot is TopLevel topLevel)
            topLevel.Cursor = Cursor.Default;

        _draggedTrack = null;
        _draggedIndex = -1;
        _isDragging = false;
    }
}