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
- All DMA channels take 3 cycles to start up, the first two these do _not_ use the bus and so the CPU will continue as normal, the third _does_ use the bus and so the CPU will pause on it's next bus cycle
- DMA internal/latch cycles happen when the DMA starts for the first time and not when the channel is enabled. e.g. for an HDMA channel the 3 cycle delay happens at the start of HDMA regardless of when it was configured (provde this during AGS cart tests)
- There is a single cycle at the end of DMA during which the bus is still in use but which does not otherwise pause the system
- The bus is only made active to the CPU during the read cycles of the DMA unit, that is it will always be 1 extra cycle after writing the last hw/w before the cpu unstalls.
- Internal DMA cycles which do not use the bus are not affected by wait states, that is the first two internal cycles will happen regardless of whether the bus is waiting.

## Known unknowns

There are still lots of things I don't fully understand about timing of DMA, some of which are probably already known by more experience GBA developers.

1. What happens when a channel is stopped during it's internal startup cycles, does it matter which of the 3 cycles it's stopped during?
2. What happens if HALTFLG is pulled high during the DMA (presumably only possible by DMA to HALTFLG), does it resume after an interrupt? Can an interrupt even fire given that the cpu was paused for DMA? Lock up?
3. What happens if a low prio channel is interrupted in the middle of it's startup cycles? Do they start again (probably) or does it continue from where it got to?

## Timing Example

The following table illustrates the state of each relevant component during a "Start DMA, read Timer" pair of instructions. 
This assumes the application is running from a portion of memory with no wait states (e.g. IWRAM).

*Cycle 0:*
The CPU is performing a write operation so the bus is used by the CPU with nRW high (write) and is not doing instruction fetch.

DMA is inactive at this point

The pipeline is full with the next two instructions to execute

*Cycle 1:*
The CPU is doing two things:
1. Fetching an instruction
2. Starting the instruction immediately after the DMA register write

The DMA unit is in it's internal cycle where it latches values but has not yet taken over the bus (or the CPU wouldn't be able to do the instruction fetch above and would stall)

*Cycle 2:*
The CPU is in the second cycle of the LDR instruction where the bus has put the timer value into an internal latch before loading it into a register with no wait states.

The DMA unit is in the second of it's internal cycles pre bus usage so CPU is still active.

*Cycle 3:*
The CPU is now in an internal cycle (no bus accesses) putting the latched value into the register.

The DMA unit has taken over the bus at this point but has not yet read the first value. Since the CPU isn't using the bus both continue for this cycle.


*Cycle 4:*
The CPU attempts to access the bus to fetch the next instruction but gets paused because the bus is active for the DMA unit

The DMA unit starts to read/write pair on whatever memory addresses were configured.

Timing from here out is fairly straightforward, it's simply read/write pairs with wait states depending on memory region.

So how does this differ if the code was executed from a region with wait states?

The key difference is that DMA internal cycles happen whether there are wait states or not. So cycle 1 & 2 are identical in both cases for DMA but for the CPU it will block between cycles 1 & 2
based on the number of wait states in the region where the code is. This therefore demonstrates why the mgba timing suite for DMA
shows 2 cycles when code accesses take ~0 wait states but normal timings when they take more.

