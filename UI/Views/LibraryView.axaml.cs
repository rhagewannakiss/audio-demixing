using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AudioStemPlayer.UI.ViewModels;
using System.Linq;


namespace AudioStemPlayer.UI.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files == null || !files.Any()) return;
        if (DataContext is LibraryViewModel vm)
            await vm.LoadFromDrop(files.ToList());
    }
}