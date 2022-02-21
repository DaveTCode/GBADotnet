# Rom compatibility/Test status

## Test Roms

| Group      | Test                  | Status             | Notes |
| ---------- | --------------------- | ------------------ | ----- |
| DenSinH    | Arm Data Processing   | :heavy_check_mark: |       |
| DenSinH    | Arm Any               | :heavy_check_mark: |       |
| DenSinH    | Thumb Data Processing | :heavy_check_mark: |       |
| DenSinH    | Thumb Any             | :heavy_check_mark: |       |
| Dead_Body  | cpu_test              | :heavy_check_mark: | Passes after ensuring that the rotates on the data bus are handled for misaligned reads |
| Wrestler   | arm-wrestler-fixed    | :x:                | Mode 0 enabled this to run, LDR failing for register reads, few others across the board (including MOV somehow!) |
| JSMolka    | arm                   | :x:                | Fails test 223 "ARM 3: PC as destination with S bit" which isn't surprising since I haven't implemented S bit on data ops |
| JSMolka    | thumb                 | :x:                | Never actually writes to VRAM for some reason, presumably goes off some silly dead end |
| PeterLemon | Hello World           | :x:                | Requires SWI into bios function (RamReset) which then fails trying to write to 04000114 which doesn't map to a known register |
| mgba       | suite                 | :x:                | Prints weird tilemap and then crashes - great success |
| TONC       | First                 | :heavy_check_mark: | First passing test case! |
| TONC       | Second                | :heavy_check_mark: |  |
| TONC       | Hello                 | :x:                | Calls an SWI from Thumb and then ends up executing beyond where it should in bios. Haven't checked what's happening precisely. |
| TONC       | Pageflip              | :heavy_check_mark: | Requires LYC to behave vaguely sensibly and page flipping (obviously) so those are vaguely tested |

## Real Roms

| Rom   | Status | Notes |
| ----- | ------ | ----- |
| Doom  | :x:    | Makes call to DMA register read |