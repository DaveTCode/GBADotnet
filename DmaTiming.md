# DMA Timing Notes

There are quite a few ROMs which test bits of DMA timing and some very obvious things to say about it. However there are also a couple of things that
I feel like need clearing up around how many cycles it takes to _start_ a DMA and whether those cycles are required for non-immediate dma.

## Known Good Tests

- [mgba suite](https://github.com/mgba-emu/suite/blob/master/src/timing.c) has tests which validate timing of DMA with both prefetch enabled/disabled
- [AGS](https://github.com/DenSinH/AGSTests) is a commercial test rom that uses DMA of timer counter to test various things and therefore also tests DMA timing. Specifically the DMA priority test seems to heavily rely on exact cycle timings of starting/stopping HDMA
- [NBA HW Tests](https://github.com/nba-emu/hw-test) is a set of simple tests by Fleroviux which uses dma of timer values and dma of other IO registers to test various things

## Obvious timing statement

- DMA performs read/write pairs and so at best must take exactly 2 cycles for each value transferred.
- The first of those pairs during DMA is obviously non-sequential, all following ones (unless DMA is interrupted) are sequential
- Bus wait states affect DMA, that is, if you DMA write to 0x0200_0000 you will get on board ram wait states which stretch the 
  read/write cycle from the point of view of DMA

## Non-obvious timing statements

- A high priority DMA (e.g. HDMA) will only trigger on a _read_ cycle of a lower priority DMA. That is, for each DMA read/write there are two cycles, only the read cycle polls DMA channels for which should be active (proved this during solving AGS priority issues)
- All DMA channels take 3 cycles to start up, the first of these does _not_ use the bus and so the CPU will continue as normal, the second and third _do_ use the bus and so the CPU will pause on it's next bus cycle
- DMA internal/latch cycles happen when the DMA starts for the first time and not when the channel is enabled. e.g. for an HDMA channel the 3 cycle delay happens at the start of HDMA regardless of when it was configured (provde this during AGS cart tests)
- Common understanding on Discord is that there is a cycle after stopping before the CPU becomes active, I have been unable to observe this and in fact having it would break various tests in my implementation.

## Known unknowns

There are still lots of things I don't fully understand about timing of DMA, some of which are probably already known by more experience GBA developers.

1. What happens when a channel is stopped during it's internal startup cycles, does it matter which of the 3 cycles it's stopped during?
2. What happens if HALTFLG is pulled high during the DMA (presumably only possible by DMA to HALTFLG), does it resume after an interrupt? Can an interrupt even fire given that the cpu was paused for DMA? Lock up?
3. What happens if a low prio channel is interrupted in the middle of it's startup cycles? Do they start again (probably) or does it continue from where it got to?
