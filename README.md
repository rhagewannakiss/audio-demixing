
# ***AudioStemPlayer***
>*coursework for Computer Technologies at MIPT DREC, 4th semester*

A desktop AI audio player for managing a local music library, playing tracks, storing playback history, and separating songs into stems with Python/Demucs. The application is written in C#/.NET 8 with Avalonia MVVM and uses SQLite for persistent storage.

### Execution pipeline
`Avalonia UI -> ViewModels -> Core services -> SQLite / BASS / Python Demucs`

### Features
- Audio library for `MP3`, `WAV`, `FLAC`, and `OGG`
- Metadata reading with TagLibSharp:
  - title
  - artist
  - album
  - genre
  - year
  - duration
  - file size
- Search by track metadata
- Track duration display in the library
- Duplicate track protection by file path
- Local SQLite persistence
- Audio playback with play, pause, stop, volume, and position controls
- Playback history with search, grouping, and "play again"
- Playlist backend support:
  - create
  - rename
  - delete
  - add/remove tracks
  - preserve track order
- AI demixing through Demucs:
  - `vocals.wav`
  - `drums.wav`
  - `bass.wav`
  - `other.wav`
- Processing history for demixing jobs
- Stem file persistence
- Cancellation and progress reporting for long-running demixing

### Backend architecture
- `Core/Models` contains domain models:
  - `TrackInfo`
  - `PlaylistInfo`
  - `PlaylistTrackInfo`
  - `ProcessingJobInfo`
  - `StemFileInfo`
  - `PlaybackHistoryInfo`
- `Core/Services` contains application services:
  - `JsonLibraryService`
  - `MetadataReader`
  - `FileService`
  - `AudioPlayerService`
  - `DemixingService`
  - `PlaylistService`
  - `ProcessingHistoryService`
  - `PlaybackHistoryService`
- `Core/Storage` contains SQLite infrastructure:
  - `SqliteConnectionFactory`
  - `DatabaseInitializer`

### SQLite storage
The database is created automatically in the user application data directory:

Linux:
```sh
$HOME/.config/AudioStemPlayer/library.db
```

Windows:
```sh
%APPDATA%\AudioStemPlayer\library.db
```

Main tables:
- `Tracks`
- `Playlists`
- `PlaylistTracks`
- `ProcessingJobs`
- `StemFiles`
- `PlaybackHistory`

### Requirements
- .NET SDK 8.0+
- Python 3.10+ for Demucs
- Demucs
- Torch/TorchCodec dependencies required by Demucs
- FFmpeg available in `PATH`
- Native BASS library for the target OS
- Native BASS_FX library for the equalizer (`libbass_fx.so` on Linux, `libbass_fx.dylib` on macOS, `bass_fx.dll` on Windows)

### Installation
```sh
git clone git@github.com:rhagewannakiss/Audio-Demixing.git
cd Audio-Demixing
```

### Build
```sh
dotnet restore
dotnet build
```

### Run
```sh
dotnet run
```

### Demucs setup
Recommended local virtual environment:

```sh
python3 -m venv .venv
.venv/bin/python -m pip install --upgrade pip
.venv/bin/python -m pip install demucs torchcodec
```

Check Demucs:

```sh
.venv/bin/python -m demucs --help
```

On Windows:

```sh
python -m venv .venv
.venv\Scripts\python -m pip install --upgrade pip
.venv\Scripts\python -m pip install demucs torchcodec
.venv\Scripts\python -m demucs --help
```

### BASS_FX setup
The equalizer uses the native BASS_FX add-on. On Linux, `libbass_fx.so` should be placed next to the other native BASS libraries in `runtimes/linux-x86/native/`.

Install the x86-64 Linux library:

```sh
mkdir -p /tmp/audio-stem-bassfx
curl -L https://www.jobnik.net/BASS_FX/bass_fx24-linux.zip \
  -o /tmp/audio-stem-bassfx/bass_fx24-linux.zip
unzip -j -o /tmp/audio-stem-bassfx/bass_fx24-linux.zip \
  x64/libbass_fx.so \
  -d runtimes/linux-x86/native
```

Check the installed library:

```sh
file runtimes/linux-x86/native/libbass_fx.so
```

### Usage
1. Run the application.
2. Open the library page.
3. Click `Load Track` and select an audio file.
4. Use search to filter tracks.
5. Select a track to play it.
6. Open `History` to see previously played tracks.
7. Open `Demixing`, choose a track, and start separation.
8. Use generated stems in the order:
   - vocals
   - drums
   - bass
   - other

### Inspect SQLite manually
```sh
sqlite3 "$HOME/.config/AudioStemPlayer/library.db" ".tables"
sqlite3 "$HOME/.config/AudioStemPlayer/library.db" ".schema"
```

List tracks:
```sh
sqlite3 "$HOME/.config/AudioStemPlayer/library.db" \
  "SELECT Id, Title, Artist, DurationSeconds FROM Tracks;"
```

List playback history:
```sh
sqlite3 "$HOME/.config/AudioStemPlayer/library.db" \
  "SELECT Id, Title, Artist, PlayedAt FROM PlaybackHistory ORDER BY PlayedAt DESC;"
```

List demixing jobs:
```sh
sqlite3 "$HOME/.config/AudioStemPlayer/library.db" \
  "SELECT Id, Status, InputFilePath, OutputDirectory FROM ProcessingJobs ORDER BY CreatedAt DESC;"
```

### Presentation and documentation
- `BACKEND_NOTES.md` - concise backend overview
- `BACKEND_DEEP_DIVE.md` - detailed defense-oriented backend explanation
- `AudioStemPlayer_Presentation.html` - standalone HTML presentation
- `REQUIREMENTS.md` - official coursework requirements
- `BRD_Audio_Demixing.pdf` - business requirements document

### WSL audio note
If the application runs in WSL and no audio device is available, configure ALSA/PulseAudio forwarding. For WSLg, a minimal `~/.asoundrc` can point ALSA to PulseAudio:

```sh
cat > ~/.asoundrc <<'EOF'
pcm.!default {
    type pulse
}

ctl.!default {
    type pulse
}
EOF
```

### Run tests
If test projects are added:

```sh
dotnet test
```

### Authors
- *Makarskaya Alexandra, B01-401 DREC*
- *Pavlenko Ilya, B01-401 DREC*
