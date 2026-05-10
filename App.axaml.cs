using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.UI.Views;
using AudioStemPlayer.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStemPlayer;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var services = new ServiceCollection();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
            services.AddSingleton<IMetadataReader, MetadataReader>();
            services.AddSingleton<ILibraryService, SqLiteLibraryService>();
            services.AddSingleton<IDemixingService, DemixingService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IProcessingHistoryService, ProcessingHistoryService>();
            services.AddSingleton<IPlaylistService, PlaylistService>();  
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<PlayerPanelViewModel>();
            services.AddSingleton<LibraryViewModel>();
            services.AddSingleton<DemixingViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<PlaylistsViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<ConfirmationDialogViewModel>();
            services.AddSingleton<IServiceProvider>(sp => sp);

            _services = services.BuildServiceProvider();

            var mainWindowViewModel = _services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            desktop.MainWindow.Closed += (sender, args) =>
            {
                _services?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}