using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AudioStemPlayer.UI.ViewModels;

namespace AudioStemPlayer.UI.Views;

public partial class PlaylistsView : UserControl
{
    public PlaylistsView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files == null || !files.Any())
            return;

        if (DataContext is PlaylistsViewModel vm)
        {
            await vm.ImportDroppedFiles(files.ToList());
        }
    }
}