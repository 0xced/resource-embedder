﻿using System;

namespace ResourceEmbedder.Core
{
    /// <summary>
    /// Helpers for debug symbols.
    /// </summary>
    public static class DebugSymbolHelper
    {
        /// <summary>
        /// Convert from msbuild strings.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static DebugSymbolType FromString(string input)
        {
            if (string.IsNullOrEmpty(input) ||
                "none".Equals(input, StringComparison.OrdinalIgnoreCase))
                return DebugSymbolType.None;
            if ("portable".Equals(input, StringComparison.OrdinalIgnoreCase))
                return DebugSymbolType.Portable;
            if ("full".Equals(input, StringComparison.OrdinalIgnoreCase))
                return DebugSymbolType.Full;
            if ("pdb-only".Equals(input, StringComparison.OrdinalIgnoreCase) ||
                "pdbonly".Equals(input, StringComparison.OrdinalIgnoreCase))
                return DebugSymbolType.PdbOnly;

            throw new NotSupportedException("Unsupported " + input);
        }
    }
}
