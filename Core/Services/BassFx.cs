using System;
using System.Runtime.InteropServices;

namespace AudioStemPlayer.Core.Services
{
    internal static class BassFx
    {
        public const int BASS_FX_BFX_PEAKEQ = 0x10004;

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

        [DllImport("bass_fx", EntryPoint = "BASS_ChannelSetFX")]
        public static extern int ChannelSetFX(int handle, int type, int priority);

        [DllImport("bass_fx", EntryPoint = "BASS_ChannelRemoveFX")]
        public static extern bool ChannelRemoveFX(int handle, int fxHandle);

        [DllImport("bass_fx", EntryPoint = "BASS_FXSetParameters")]
        private static extern bool FXSetParametersInternal(int fxHandle, IntPtr parameters);

        [DllImport("bass_fx", EntryPoint = "BASS_FXGetParameters")]
        private static extern bool FXGetParametersInternal(int fxHandle, IntPtr parameters);

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
    }
}