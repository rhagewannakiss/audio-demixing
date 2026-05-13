using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AudioStemPlayer.Core.Services
{
    public partial class EqualizerBand : ObservableObject
    {
        public string Frequency { get; set; } = string.Empty;

        [ObservableProperty]
        private double _gain;
    }

    public interface IEqService
    {
        bool IsEnabled { get; set; }
        ObservableCollection<EqualizerBand> Bands { get; }
        string[] Presets { get; }
        string SelectedPreset { get; set; }
        void ApplyPreset(string preset);
        void ApplyBand(int bandIndex);
        void ApplyAll();
        void Connect(IAudioPlayerService audioPlayer);
        void Disconnect();
    }
}