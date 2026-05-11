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

        [DllImport("AsmNative.dll", EntryPoint = "max_asm", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int MaxAsm(int a, int b);

        [DllImport("AsmNative.dll", EntryPoint = "factorial_asm", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int FactorialAsm(int n);

        [DllImport("AsmNative.dll", EntryPoint = "es_par_asm", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EsParAsm(int n);
    }
}