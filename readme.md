# Gameboy Advanced Emulator - C#

This is a WIP Gameboy Advanced emulator which is nowhere near something you'd want to play actual games on.

- [Compatibility](./compatibility)
- [Notes](./Notes.md)

## Projects

Within this repository there are a number of projects:

1. [GameboyAdvanced.Arm.SourceGenerators](./GameboyAdvanced.Arm.SourceGenerators) - Roslyn source generators to template our various common operations in the ARM core
2. [GameboyAdvanced.CompatibilityChecker](./GameboyAdvanced.CompatibilityChecker) - A console application which runs the emulator against all roms in a directory and outputs 1 image per rom along with a github flavour markdown doc showing compatibility
3. [GameboyAdvanced.Core.Benchmarks](./GameboyAdvanced.Core.Benchmarks) - A (mostly unused) project to generate benchmarks of low level parts of the emulator
4. [GameboyAdvanced.Core.Tests](./GameboyAdvanced.Core.Tests) - A small handful of unit tests written whilst validating the CPU. Not really used any more as test roms are more useful
5. [GameboyAdvanced.Core](./GameboyAdvanced.Core) - The fundamental core of the emulator which provides a `Device` class that takes a gamepak and allows single/frame stepping
6. [GameboyAdvanced.Headless](./GameboyAdvanced.Headless) - A dotnet console application which allows running the core without a UI but has debugging commands
7. [GameboyAdvanced.Sdl2](./GameboyAdvanced.Sdl2) - An SDL2 based UI for emulator which provides no features beyond running the application
8. [GameboyAdvanced.Web](./GameboyAdvanced.Web) - A [SignalR]() based websockets implementation of the emulator which runs the emulator on the server and passes frames to a web ui which includes some debugging information

## Architecture

The core question for any low level emulator is what granularity it steps the various components. This can be done in a number of ways but the most common are:
- 1 step per instruction
- 1 step per memory access
- 1 step per master clock cycle

Typically good 8 bit system (NES/GB) emulators will act at the master clock cycle level but this can be prohitibitively expensive to once working on more modern
devices.

*This* emulator aims to step all of the components of a GBA at the master clock cycle speed, specifically it aims to accurately emulate the various signals in/out of
the arm7tdmi core. _Some_ effort has been taken to ensure that the correct signals appear on the rising/falling edge of each cycle but since the core is stepped once 
per master clock cycle rather than once for each of the rising/falling edge this is not reliably accurate. I don't believe that it impacts accuracy of the overarching 
system emulation.

The various components are then listed below with links to the code handling their step function:

- [CPU - ARM7TDMI](./GameboyAdvanced.Core/Cpu/Core.cs)
- [PPU](./GameboyAdvanced.Core/Ppu/Ppu.cs)
- [Timer](./GameboyAdvanced.Core/Timer/TimerController.cs)
- [Dma](./GameboyAdvanced.Core/Dma/DmaController.cs)
- [APU](./GameboyAdvanced.Core/Apu/Apu.cs) - TODO, register implemented but no cycle step function

### Scheduler or not?

All good GBA emulators make use of a scheduler instead of single stepping components which don't need running each cycle (e.g. timers). This emulator does
not have a scheduler although it's likely that I'll need to add one at some point to resolve the performance issues tracked here: https://github.com/DaveTCode/GBADotnet/issues/48.

### Low level coding decisions

*Q. Why use source generators?*
A. C# sadly doesn't come with a macro/preprocessor/templating system and so our only options here for e.g. ALU operations are either to write 
functions with _loads_ of branches (bad performance), pre-generate a bunch of copy paste functions (even worse than what I've done) or bastardise a
macro system out of source generators.

*Q. Why is the code so bad it makes my eyes hurt?*
A. To be clear, I would slap anyone who submitted this to me for code review. Generally this emulator makes the concious decision to prioritise performance over
readable code. So e.g. there's almost no inheritance or properties. I've also prioritised practicality over good practice. So everything is `public` scoped
in order to make serializing data easy for use by the web based UI and save/load state functionality.

## Test Rom Status

| Group      | Test                     | Status             | Notes                                                                                                                                                                                                     |
|------------|--------------------------|--------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| DenSinH    | Arm Data Processing      | :heavy_check_mark: |                                                                                                                                                                                                           |
| DenSinH    | Arm Any                  | :heavy_check_mark: |                                                                                                                                                                                                           |
| DenSinH    | Thumb Data Processing    | :heavy_check_mark: |                                                                                                                                                                                                           |
| DenSinH    | Thumb Any                | :heavy_check_mark: |                                                                                                                                                                                                           |
| DenSinH    | EEPROM test              | :heavy_check_mark: | EEPROM implemented                                                                                                                                                                                        |
| DenSinH    | Flash test               | :heavy_check_mark: | Fixed banking issues and now works                                                                                                                                                                        |
| Open bus   | Open Bus Bios Misaligned | :heavy_check_mark: | Implemented open bus for bios and passed this and jsmolka bios tests                                                                                                                                      |
| Marie      | Open Bus Bios            | :x:                | Fixed top 4, wrong with THM but so is mgba so not that big a deal                                                                                                                                         |
| Marie      | Retaddr                  | :heavy_check_mark: |                                                                                                                                                                                                           |
| Fleroviux  | Openbuster               | :x:                | 1/144 - smashed it (mostly because I haven't written a proper open bus rotate for LDRH)                                                                                                                   |
| Dead_Body  | cpu_test                 | :heavy_check_mark: | Passes after ensuring that the rotates on the data bus are handled for misaligned reads                                                                                                                   |
| Wrestler   | arm-wrestler-fixed       | :heavy_check_mark: | Mode 0 enabled this to run. Fixed all LDR/STR/LDM/STM operations now that shifter treats RRX properly and register writebacks happen on the correct cycle. FIQ banking support required for MRS/MSR tests |
| JSMolka    | arm                      | :heavy_check_mark: | Passes all tests now                                                                                                                                                                                      |
| JSMolka    | thumb                    | :heavy_check_mark: | Passes everything after some shenanigans with LDM/STM special cases                                                                                                                                       |
| JSMolka    | memory                   | :heavy_check_mark: | Passes all memory mirror tests after implementing mirroring of relevant regions                                                                                                                           |
| JSMolka    | nes                      | :heavy_check_mark: | Passed first time                                                                                                                                                                                         |
| JSMolka    | hello                    | :heavy_check_mark: |                                                                                                                                                                                                           |
| JSMolka    | shades                   | :heavy_check_mark: | Amusingly reads beyond the end of the provided ROM (w/ pipelining) so that needed fixing before it passed                                                                                                 |
| JSMolka    | stripes                  | :heavy_check_mark: |                                                                                                                                                                                                           |
| JSMolka    | bios                     | :heavy_check_mark: |                                                                                                                                                                                                           |
| JSMolka    | unsafe                   | :heavy_check_mark: | According to readme doesn't pass on real hardware so this is not a great test to be passing!                                                                                                              |
| Panda      | panda                    | :heavy_check_mark: |                                                                                                                                                                                                           |
| PeterLemon | 3DEngine                 | :heavy_check_mark: |                                                                                                                                                                                                           |
| PeterLemon | Hello World              | :heavy_check_mark: | Required SWI, DMA channel 3 immediate, transparent palette color 0 and multiple palettes in same BG                                                                                                       |
| PeterLemon | BGMode0                  | :heavy_check_mark: | Requires "BG Tile Offset = 0, Enable Mosaic, Tiles 8BPP, BG Map Offset = 26624, Map Size = 64x64 Tiles"                                                                                                   |
| PeterLemon | BGMode7                  | :heavy_check_mark: | Affine backgrounds                                                                                                                                                                                        |
| PeterLemon | BGRotZoomMode2           | :heavy_check_mark: | Working except unimplemented mosaic                                                                                                                                                                       |
| PeterLemon | BGRotZoomMode3           | :heavy_check_mark: | Works now I have affine bg backgrounds                                                                                                                                                                    |
| PeterLemon | BGRotZoomMode4           | :heavy_check_mark: |                                                                                                                                                                                                           |
| PeterLemon | BGRotZoomMode5           | :heavy_check_mark: |                                                                                                                                                                                                           |
| PeterLemon | OBJRotZoom4BPP           | :heavy_check_mark: | 32*64 sprite is wrong, rest are correct                                                                                                                                                                   |
| PeterLemon | OBJRotZoom8BPP           | :heavy_check_mark: | 8bpp sprites fixes made this work (except mosaic which is tracked elsewhere)                                                                                                                              |
| PeterLemon | Fast Line                | :heavy_check_mark: | Ran on first attempt                                                                                                                                                                                      |
| PeterLemon | Fast Line Clip           | :heavy_check_mark: | Ran on first attempt                                                                                                                                                                                      |
| PeterLemon | Cylinder Map             | :heavy_check_mark: | Ran on first attempt, not clear what it showcases                                                                                                                                                         |
| PeterLemon | Myst                     | :heavy_check_mark: | Looks good to me although only let it play for 30s or so                                                                                                                                                  |
| PeterLemon | BigBuckBunny             | :heavy_check_mark: | Fixing bg affine backgrounds made this display properly in full screen                                                                                                                                    |
| PeterLemon | BIOS - ArcTan            | :heavy_check_mark: | Passes after lots of work on timers                                                                                                                                                                       |
| PeterLemon | BIOS - Div               | :heavy_check_mark: | Passes after lots of work on timers                                                                                                                                                                       |
| PeterLemon | BIOS - Sqrt              | :heavy_check_mark: | Passes after lots of work on timers                                                                                                                                                                       |
| PeterLemon | Timers                   | :heavy_check_mark: | Passes after implementing timer register byte reads                                                                                                                                                       |
| mgba       | suite                    | :x:                | See Notes.md for detailed breakdown of pass/fail                                                                                                                                                          |
| TONC       | Bigmap                   | :heavy_check_mark: | Requires byte wide writes to PPU registers which isn't implemented                                                                                                                                        |
| TONC       | Blddemo                  | :heavy_check_mark: | Blending appears to work compared against mgba                                                                                                                                                            |
| TONC       | BM Modes                 | :heavy_check_mark: | Looks weird but tested against other emulators                                                                                                                                                            |
| TONC       | Brin Demo                | :heavy_check_mark: | Passes with proper support for multi size tilemaps                                                                                                                                                        |
| TONC       | CBB Demo                 | :heavy_check_mark: | mgba was wrong, I'm now right after fixing the default sprite height/width                                                                                                                                |
| TONC       | DMA Demo                 | :heavy_check_mark: | Interesting hackery using HBLANK DMA to create a round window effect                                                                                                                                      |
| TONC       | First                    | :heavy_check_mark: | First passing test case!                                                                                                                                                                                  |
| TONC       | Hello                    | :heavy_check_mark: | Calls an SWI from Thumb and then ends up executing beyond where it should in bios. Haven't checked what's happening precisely.                                                                            |
| TONC       | IRQ Demo                 | :heavy_check_mark: | Looking good, compared completely against mgba and correct                                                                                                                                                |
| TONC       | Key Demo                 | :heavy_check_mark: | Working first time it was tested                                                                                                                                                                          |
| TONC       | M3 Demo                  | :heavy_check_mark: |                                                                                                                                                                                                           |
| TONC       | M7 Demo                  | :heavy_check_mark: | Affine backgrounds working nicely but goes blank if I rotate the screen past half way (fixed by correctly using signed 16 bit ints)                                                                       |
| TONC       | M7 Demo MB               | :heavy_check_mark: | Requires affine backgrounds                                                                                                                                                                               |
| TONC       | M7 Demo Ex               | :heavy_check_mark: | Required affine backgrounds with mid frame changes to Dmx/Dmy                                                                                                                                             |
| TONC       | Mos Demo                 | :x:                | Requires mosaic                                                                                                                                                                                           |
| TONC       | OA Combo                 | :heavy_check_mark: | Sprites don't appear in quite the right place, affine sprites not quite right                                                                                                                             |
| TONC       | Obj Aff                  | :heavy_check_mark: | Good affine sprite test rom                                                                                                                                                                               |
| TONC       | Obj Demo                 | :heavy_check_mark: | I think this is working properly                                                                                                                                                                          |
| TONC       | Octtest                  | :heavy_check_mark: | Required affine backgrounds and sprites but now works                                                                                                                                                     |
| TONC       | Pageflip                 | :heavy_check_mark: | Requires LYC to behave vaguely sensibly and page flipping (obviously) so those are vaguely tested                                                                                                         |
| TONC       | Prio Demo                | :heavy_check_mark: |                                                                                                                                                                                                           |
| TONC       | Sbb Aff                  | :heavy_check_mark: | Requires affine background                                                                                                                                                                                |
| TONC       | Sbb Reg                  | :heavy_check_mark: | Requires large backgrounds                                                                                                                                                                                |
| TONC       | Second                   | :heavy_check_mark: | fixed with bug fixes around SWI return/MSR/MRS                                                                                                                                                            |
| TONC       | SWI Demo                 | :heavy_check_mark: | Required a really interesting interaction with PC and a load instruction which was bugged for ages                                                                                                        |
| TONC       | SWI VSync                | :heavy_check_mark: | Tests sprite rotations based on vsync in some way                                                                                                                                                         |
| TONC       | Tmr Demo                 | :heavy_check_mark: | Tests countdown timers by displaying as a clock, seems to work although I'm sure timer values aren't 100% correct                                                                                         |
| TONC       | TTE Demo                 | :heavy_check_mark: | Full test of all sorts of things                                                                                                                                                                          |
| TONC       | Txt Obj                  | :heavy_check_mark: | Letters bouncing properly, looks like sprite rendering working here well                                                                                                                                  |
| TONC       | Txt SE1                  | :heavy_check_mark: | Looks identical to mgba                                                                                                                                                                                   |
| TONC       | Txt SE2                  | :x:                | Some of these I'm quite close on timings and some I'm out by a chunk, not clear why that is but hopefully nothing serious!                                                                                |
| TONC       | Win demo                 | :heavy_check_mark: | Window implemented and working (but no OBJ window bits)                                                                                                                                                   |
| beeg       | beeg.gba                 | :heavy_check_mark: | With scanline renderer this is now fine                                                                                                                                                                   |
| AGB        | AGB_CHECKER_TCHK10       | :x:                | Passes all tests on first screen but the graphics after that are all over the place                                                                                                                       |
| zayd       | prefetch abuse           | :x:                | All wrong values, still don't have a good handle on the prefetch module                                                                                                                                   |
| zayd       | dmaslow                  | :x:                | All wrong values, still don't have a good handle on the prefetch module                                                                                                                                   |
| zayd       | dmamedium                | :x:                | All wrong values, still don't have a good handle on the prefetch module                                                                                                                                   |
| zayd       | dmamedium2               | :x:                | All wrong values, still don't have a good handle on the prefetch module                                                                                                                                   |
| zayd       | dmafast                  | :x:                | All wrong values, still don't have a good handle on the prefetch module                                                                                                                                   |
| NBA        | IRQ Delay                | :heavy_check_mark: | Passes first but off by 1 cycles on HALT                                                                                                                                                                  |
| NBA        | DMA - Burst into tears   | :x:                |                                                                                                                                                                                                           |
| NBA        | DMA - Latch              | :heavy_check_mark: |                                                                                                                                                                                                           |
| NBA        | DMA - Start Delay        | :heavy_check_mark: | 20 on real GBA and 20 here, fixed by getting write cycles correct                                                                                                                                         |
| NBA        | Haltcnt                  | :heavy_check_mark: | Pass direct and cpuset but fail on iwram and rom by 1 cycle in each case                                                                                                                                  |
| NBA        | PPU - Basic Timing       | :heavy_check_mark: | HDMA doesn't bloody stop when HBlank turns off. FFS. Idiot.                                                                                                                                               |
| NBA        | Timer - Start Stop       | :heavy_check_mark: | Fixed by adding 1 cycle delay from register write to stopping timer                                                                                                                                       |
| NBA        | Timer - Reload           | :heavy_check_mark: | Requires a cycle delay latching more than just start bit (countup etc need the same cycle delay).                                                                                                         |
