using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SemanticamenteElocuente
{
    internal static class NativeAsm
    {
        [DllImport("AsmNative.dll", EntryPoint = "suma_asm", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SumaAsm(int a, int b);
    }
}