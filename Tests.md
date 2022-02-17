| Group    | Test                  | Status             | Notes |
| -------- | --------------------- | ------------------ | ----- |
| DenSinH  | Arm Data Processing   | :heavy_check_mark: |       |
| DenSinH  | Arm Any               | :heavy_check_mark: |       |
| DenSinH  | Thumb Data Processing | :heavy_check_mark: |       |
| DenSinH  | Thumb Any             | :heavy_check_mark: |       |
| Wrestler | arm-wrestler-fixed    | :x:                | Fails doing a byte read from interrupt registers, not sure if it's supposed to and haven't checked |
| JSMolka  | arm                   | :x:                | msr_ic not implemented |
| JSMolka  | thumb                 | :x:                | Hangs with white screen, not checked why |