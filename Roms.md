# Rom compatibility/Test status

## Test Roms

| Group      | Test                  | Status             | Notes |
| ---------- | --------------------- | ------------------ | ----- |
| DenSinH    | Arm Data Processing   | :heavy_check_mark: |       |
| DenSinH    | Arm Any               | :heavy_check_mark: |       |
| DenSinH    | Thumb Data Processing | :heavy_check_mark: |       |
| DenSinH    | Thumb Any             | :heavy_check_mark: |       |
| Dead_Body  | cpu_test              | :heavy_check_mark: | Passes after ensuring that the rotates on the data bus are handled for misaligned reads |
| Wrestler   | arm-wrestler-fixed    | :x:                | Mode 0 enabled this to run. Fixed all LDR/STR/LDM/STM operations now that shifter treats RRX properly and register writebacks happen on the correct cycle. Remaining error is MRS/MSR ops which are tested using FIQ that isn't implemented properly yet |
| JSMolka    | arm                   | :x:                | Fails test 223 |
| JSMolka    | thumb                 | :heavy_check_mark: | Passes everything after some shenanigans with LDM/STM special cases |
| Panda      | panda                 | :heavy_check_mark: | |
| PeterLemon | Hello World           | :heavy_check_mark: | Required SWI, DMA channel 3 immediate, transparent palette color 0 and multiple palettes in same BG |
| PeterLemon | BGMode0               | :x:                | Requires "BG Tile Offset = 0, Enable Mosaic, Tiles 8BPP, BG Map Offset = 26624, Map Size = 64x64 Tiles" |
| PeterLemon | Cylinder Map          | :heavy_check_mark: | Ran on first attempt, not clear what it showcases |
| mgba       | suite                 | :x:                | Prints weird tilemap and then crashes - great success |
| TONC       | First                 | :heavy_check_mark: | First passing test case! |
| TONC       | Second                | :heavy_check_mark: |  |
| TONC       | Hello                 | :x:                | Calls an SWI from Thumb and then ends up executing beyond where it should in bios. Haven't checked what's happening precisely. |
| TONC       | Pageflip              | :heavy_check_mark: | Requires LYC to behave vaguely sensibly and page flipping (obviously) so those are vaguely tested |
| TONC       | M3 Demo               | :heavy_check_mark: |  |

## Real Roms

| Rom   | Status | Notes |
| ----- | ------ | ----- |
| Doom  | :x:    | 2 frames in does something with an invalid address, not investigated why yet |