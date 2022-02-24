﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GameboyAdvanced.Core.Bus;

internal enum PhiOutput
{   
    Disabled = 0,
    Phi4_19MHz = 1,
    Phi8_38MHz = 2,
    Phi16_78MHz = 3,
}

/// <summary>
/// WAITCNT is a special register which controls access timings to various
/// regions of the gamepak.
/// 
/// - Wait State 0 = 0x0800_0000 (normal)
/// - Wait State 1 = 0x0A00_0000
/// - Wait State 2 = 0x0C00_0000
/// 
/// TODO - Not actually using these wait timings anywhere yet, assuming defaults in memory bus
/// </summary>
internal struct WaitControl
{
    internal int SRAMWaitControl;
    internal int WaitState0N;
    internal int WaitState0S;
    internal int WaitState1N;
    internal int WaitState1S;
    internal int WaitState2N;
    internal int WaitState2S;
    internal PhiOutput PhiTerminalOutput;
    internal bool EnableGamepakPrefetch;
    internal bool GamepakIsCGB;

    /// <summary>
    /// WAITCNT is set to 0x0000 on reset
    /// </summary>
    internal void Reset()
    {
        Set(0);
    }

    internal void Set(ushort value)
    {
        SRAMWaitControl = value & 0b11;
        WaitState0N = WaitStatesNFromBitVal((value >> 2) & 0b11);
        WaitState0S = ((value >> 4) & 0b1) == 0 ? 2 : 1;
        WaitState1N = WaitStatesNFromBitVal((value >> 5) & 0b11);
        WaitState1S = ((value >> 7) & 0b1) == 0 ? 4 : 1;
        WaitState2N = WaitStatesNFromBitVal((value >> 8) & 0b11);
        WaitState2S = ((value >> 10) & 0b1) == 0 ? 8 : 1;
        PhiTerminalOutput = (PhiOutput)((value >> 11) & 0b11);
        EnableGamepakPrefetch = ((value >> 14) & 0b1) == 0b1;
        GamepakIsCGB = ((value >> 15) & 0b1) == 0b1;
    }

    internal ushort Get() => (ushort)(SRAMWaitControl |
            (BitValFromWaitStateN(WaitState0N) << 2) |
            (WaitState0S == 2 ? 0 : (1 << 4)) |
            (BitValFromWaitStateN(WaitState1N) << 5) |
            (WaitState1S == 4 ? 0 : (1 << 7)) |
            (BitValFromWaitStateN(WaitState2N) << 8) |
            (WaitState2S == 8 ? 0 : (1 << 10)) |
            ((ushort)PhiTerminalOutput << 11) |
            (EnableGamepakPrefetch ? (1 << 14) : 0) |
            (GamepakIsCGB ? (1 << 15) : 0));

    private static int WaitStatesNFromBitVal(int bitVal) => bitVal switch
    {
        0 => 4,
        1 => 3,
        2 => 2,
        3 => 8,
        _ => throw new Exception("Invalid waitcnt value"),
    };

    private static ushort BitValFromWaitStateN(int waitStateN) => waitStateN switch
    {
        4 => 0,
        3 => 1,
        2 => 2,
        8 => 3,
        _ => throw new Exception("Invalid waitcnt")
    };
}