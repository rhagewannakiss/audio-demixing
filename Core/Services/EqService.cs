using System;
using System.Collections.ObjectModel;
using System.Linq;
using ManagedBass;

namespace AudioStemPlayer.Core.Services
{
    public class EqService : IEqService, IDisposable
    {
        private IAudioPlayerService? _audioPlayer;
        private int[]? _fxHandles;
        private int _stream;

        public bool IsEnabled { get; set; }

        public ObservableCollection<EqualizerBand> Bands { get; } = new ObservableCollection<EqualizerBand>
        {
            new EqualizerBand { Frequency = "32", Gain = 0 },
            new EqualizerBand { Frequency = "64", Gain = 0 },
            new EqualizerBand { Frequency = "125", Gain = 0 },
            new EqualizerBand { Frequency = "250", Gain = 0 },
            new EqualizerBand { Frequency = "500", Gain = 0 },
            new EqualizerBand { Frequency = "1k", Gain = 0 },
            new EqualizerBand { Frequency = "2k", Gain = 0 },
            new EqualizerBand { Frequency = "4k", Gain = 0 },
            new EqualizerBand { Frequency = "8k", Gain = 0 },
            new EqualizerBand { Frequency = "16k", Gain = 0 }
        };

        public string[] Presets { get; } = { "Flat", "Rock", "Pop", "Jazz", "Classical" };
        public string SelectedPreset { get; set; } = "Flat";

        public void Connect(IAudioPlayerService audioPlayer)
        {
            _audioPlayer = audioPlayer;
            if (_audioPlayer is AudioPlayerService ap)
                ap.StreamCreated += OnStreamCreated;
        }

        public void Disconnect()
        {
            if (_audioPlayer is AudioPlayerService ap)
                ap.StreamCreated -= OnStreamCreated;
            ClearEffects();
            _audioPlayer = null;
        }

        private void OnStreamCreated(object? sender, int stream)
        {
            ClearEffects();
            _stream = stream;
            CreateEffects();
            ApplyAll();
        }

        private void CreateEffects()
        {
            if (_stream == 0) return;
            _fxHandles = new int[10];
            float[] centerFreqs = { 32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };

            for (int i = 0; i < 10; i++)
            {
                int fxHandle = BassFx.ChannelSetFX(_stream, BassFx.BASS_FX_BFX_PEAKEQ, 0);
                if (fxHandle != 0)
                {
                    var param = new BassFx.PeakingEqParameters
                    {
                        lBand      = 0,
                        fBandwidth = 1.0f,
                        fQ         = 0f,
                        fCenter    = centerFreqs[i],
                        fGain      = (float)Bands[i].Gain,
                        lChannel   = -1
                    };
                    BassFx.FXSetParameters(fxHandle, param);
                    _fxHandles[i] = fxHandle;
                }
            }
        }

        public void ApplyBand(int bandIndex)
        {
            if (_fxHandles == null || bandIndex < 0 || bandIndex >= _fxHandles.Length) return;
            int fx = _fxHandles[bandIndex];
            if (fx != 0)
            {
                var param = BassFx.FXGetParameters<BassFx.PeakingEqParameters>(fx);
                param.fGain = IsEnabled ? (float)Bands[bandIndex].Gain : 0f;
                BassFx.FXSetParameters(fx, param);
            }
        }

        

        private void ClearEffects()
        {
            if (_fxHandles == null) return;
            for (int i = 0; i < _fxHandles.Length; i++)
            {
                if (_fxHandles[i] != 0)
                    BassFx.ChannelRemoveFX(_stream, _fxHandles[i]);
            }
            _fxHandles = null;
        }

        public void ApplyPreset(string preset)
        {
            double[] gains = preset switch
            {
                "Rock" => new double[] { 4, 2, 0, -2, -3, 2, 5, 8, 8, 7 },
                "Pop" => new double[] { -1, -1, 0, 3, 5, 4, 2, -1, -2, -4 },
                "Jazz" => new double[] { 3, 2, 1, 1, -2, -3, -2, 0, 2, 4 },
                "Classical" => new double[] { 5, 4, 3, 2, -1, -1, 0, 2, 3, 4 },
                _ => new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            };

            for (int i = 0; i < 10; i++)
                Bands[i].Gain = gains[i];

            SelectedPreset = preset;
            ApplyAll();
        }

        

        public void ApplyAll()
        {
            for (int i = 0; i < 10; i++)
                ApplyBand(i);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}