# Rom compatibility/Test status

## Test Roms

| Group      | Test                  | Status             | Notes |
| ---------- | --------------------- | ------------------ | ----- |
| DenSinH    | Arm Data Processing   | :heavy_check_mark: |       |
| DenSinH    | Arm Any               | :heavy_check_mark: |       |
| DenSinH    | Thumb Data Processing | :heavy_check_mark: |       |
| DenSinH    | Thumb Any             | :heavy_check_mark: |       |
| Dead_Body  | cpu_test              | :heavy_check_mark: | Passes after ensuring that the rotates on the data bus are handled for misaligned reads |
| Wrestler   | arm-wrestler-fixed    | :heavy_check_mark: | Mode 0 enabled this to run. Fixed all LDR/STR/LDM/STM operations now that shifter treats RRX properly and register writebacks happen on the correct cycle. FIQ banking support required for MRS/MSR tests |
| JSMolka    | arm                   | :x:                | Fails test 232 |
| JSMolka    | thumb                 | :heavy_check_mark: | Passes everything after some shenanigans with LDM/STM special cases |
| JSMolka    | memory                | :heavy_check_mark: | Passes all memory mirror tests after implementing mirroring of relevant regions |
| JSMolka    | nes                   | :heavy_check_mark: | Passed first time |
| JSMolka    | hello                 | :heavy_check_mark: |  |
| JSMolka    | shades                | :heavy_check_mark: | Amusingly reads beyond the end of the provided ROM (w/ pipelining) so that needed fixing before it passed |
| JSMolka    | shades                | :heavy_check_mark: |  |
| JSMolka    | unsafe                | :x:                | As per readme for this test it doesn't pass on real hardware |
| Panda      | panda                 | :heavy_check_mark: | |
| PeterLemon | Hello World           | :heavy_check_mark: | Required SWI, DMA channel 3 immediate, transparent palette color 0 and multiple palettes in same BG |
| PeterLemon | BGMode0               | :x:                | Requires "BG Tile Offset = 0, Enable Mosaic, Tiles 8BPP, BG Map Offset = 26624, Map Size = 64x64 Tiles" |
| PeterLemon | Fast Line             | :heavy_check_mark: | Ran on first attempt |
| PeterLemon | Fast Line Clip        | :heavy_check_mark: | Ran on first attempt |
| PeterLemon | Cylinder Map          | :heavy_check_mark: | Ran on first attempt, not clear what it showcases |
| PeterLemon | Myst                  | :x:                | Requires BLDCNT |
| PeterLemon | BIOS - ArcTac         | :x:                | Passes check but fails timer as I haven't implemented them! ' |
| mgba       | suite                 | :x:                | Prints title then writes to 04FFF780 (checked that op is same on mgba itself) which is not mapped |
| TONC       | First                 | :heavy_check_mark: | First passing test case! |
| TONC       | Second                | :heavy_check_mark: | fixed with bug fixes around SWI return/MSR/MRS |
| TONC       | Hello                 | :heavy_check_mark: | Calls an SWI from Thumb and then ends up executing beyond where it should in bios. Haven't checked what's happening precisely. |
| TONC       | Pageflip              | :heavy_check_mark: | Requires LYC to behave vaguely sensibly and page flipping (obviously) so those are vaguely tested |
| TONC       | M3 Demo               | :heavy_check_mark: |  |
| TONC       | SWI Demo              | :heavy_check_mark: | Required a really interesting interaction with PC and a load instruction which was bugged for ages |

## Real Roms

| Rom   | Status | Notes |
| ----- | ------ | ----- |
| Doom  | :x:    | 2 frames in does something with an invalid address, not investigated why yet |