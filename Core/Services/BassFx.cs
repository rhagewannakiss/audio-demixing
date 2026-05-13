using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AudioStemPlayer.Core.Services
{
    internal static class BassFx
    {
        private const int RtldNow = 2;
        private const int RtldGlobal = 0x100;
        public const int BASS_FX_BFX_PEAKEQ = 0x10004;

        static BassFx()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LoadBassWithGlobalSymbols();
                LoadBassFxAddon();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PeakingEqParameters
        {
            public int   lBand;
            public float fBandwidth;
            public float fQ;
            public float fCenter;
            public float fGain;
            public int   lChannel;
        }

        [DllImport("bass", EntryPoint = "BASS_ChannelSetFX")]
        public static extern int ChannelSetFX(int handle, int type, int priority);

        [DllImport("bass", EntryPoint = "BASS_ChannelRemoveFX")]
        public static extern bool ChannelRemoveFX(int handle, int fxHandle);

        [DllImport("bass", EntryPoint = "BASS_FXSetParameters")]
        private static extern bool FXSetParametersInternal(int fxHandle, IntPtr parameters);

        [DllImport("bass", EntryPoint = "BASS_FXGetParameters")]
        private static extern bool FXGetParametersInternal(int fxHandle, IntPtr parameters);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlopen(string fileName, int flags);

        public static void FXSetParameters<T>(int fxHandle, T parameters) where T : struct
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(parameters, ptr, false);
            FXSetParametersInternal(fxHandle, ptr);
            Marshal.FreeHGlobal(ptr);
        }

        public static T FXGetParameters<T>(int fxHandle) where T : struct
        {
            T parameters;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            FXGetParametersInternal(fxHandle, ptr);
            parameters = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return parameters;
        }

        private static void LoadBassWithGlobalSymbols()
        {
            foreach (var candidate in GetBassLibraryCandidates())
            {
                if (dlopen(candidate, RtldNow | RtldGlobal) != IntPtr.Zero)
                    return;
            }
        }

        private static void LoadBassFxAddon()
        {
            foreach (var candidate in GetBassFxLibraryCandidates())
            {
                if (dlopen(candidate, RtldNow | RtldGlobal) != IntPtr.Zero)
                    return;
            }
        }

        private static string[] GetBassLibraryCandidates()
        {
            var baseDirectory = AppContext.BaseDirectory;
            return
            [
                Path.Combine(baseDirectory, "runtimes", "linux-x64", "native", "libbass.so"),
                Path.Combine(baseDirectory, "runtimes", "linux-x86", "native", "libbass.so"),
                Path.Combine(baseDirectory, "libbass.so"),
                "libbass.so"
            ];
        }

        private static string[] GetBassFxLibraryCandidates()
        {
            var baseDirectory = AppContext.BaseDirectory;
            return
            [
                Path.Combine(baseDirectory, "runtimes", "linux-x64", "native", "libbass_fx.so"),
                Path.Combine(baseDirectory, "runtimes", "linux-x86", "native", "libbass_fx.so"),
                Path.Combine(baseDirectory, "libbass_fx.so"),
                "libbass_fx.so"
            ];
        }
    }
}
