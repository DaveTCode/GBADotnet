# Notes

This document contains random notes either about TODOs in the code, or reasons for implementation choices, or anything else that might help produce a blog article.

## Missing implementation

- Open bus behaviour, I've deliberately architected the CPU so that core.A & core.D should be 
correct at any given cycle which should allow for open bus behaviour but I don't know 
exactly how it should work yet so haven't actually implemented anything
- Word/HW aligned addresses. As I understand it there's various places where mis-aligned read/writes either get aligned automatically or cause other odd behaviour. 
I've not been very consistent about implementing that behaviour as I've gone which may cause me trouble later. (e.g. what happens during thumb -> arm BX when PC is not aligned to word boundary?)


## Cycle timings

- I think I've mostly implemented the cpu core so that it behaves correctly w.r.t the number of N/S/I cycles taken per instruction
- One exception where sometimes I cycles are played out as wait states (see ArmDataOpGenerator) which will need fixing
- Unfortunately the wait states retured by the gamepak need resolving 
	- Currently assuming 7 wait states for all word reads to 0x0800_0000 region so a normal instruction takes 1 cycle + 7 wait states. mgba thinks it should be 6 (48 -> 54) which would imply that the wait states should add to 5
	- `WAITCNT` isn't currently implemented but should default to 0x0 which I think means wait state 0 first access is 4 and second is 2, which is 6 but not the 5 I would expect.
	- If we assume that _both_ memory reads are sequential then we get 2 * 2 = 4 which is _still_ not 5
- I'm going to need to plumb through SEQ to the memory unit to achieve correct wait states I think
- Writing a word to a 16 bit address bus (or reading) will cause two writes across the bus, one will be an extended N (or S) cycle and the other will be an extended S cycle
	- The question is whether or not we need to emulate that the two halves of that read/write are happening on different cycles
	- If so then I think the best way to achieve it will be to set the memory unit up as a clocked unit and have it act as yet another state machine

## Memory bus musings

I don't really like lots of the code relating to the memory bus. The CPU part is quite nice barring deliberate duplication, we set up A/D/MAS/nRW etc and then the next cycle the memory unit knows what to do.
The trouble is with the way that read/writes are farmed out to downstream subsystems and particularly how that relates to the bus width.

e.g. PPU bus is 16 bit, if a byte is written then that byte appears on both halves of the data bus and a half word is actually written. That feels like something which could
be implemented at a more fundamental level. Is it true for all 16 bit buses? If so then can we be clever with how A/D/MAS interact to avoid calling multiple different functions?

The other big issue is around wait states and cycles. If a word is written to a 16 bit bus then what happens? Presumably that takes 1 N(or S) and 1 S cycle to complete. 

### How long from clearing the pipeline to executing op?

Problem statement: MOV R0,R15 in Arm mode takes 2 cycles, an S cycle and an I cycle.
| Cycle | Address | MAS      | nRW   | Data    | nMREQ | SEQ   | nOPC  |
| ----- | ------- | -------- | ----- | ------- | ----- | ----- | ----- |
| 1     | pc+8    | word     | false | [pc+2L] | true  | false | false |
| 2     | *pc+12* | word     | false | -       | false | true  | true  |

How do we ensure that the address bus points to PC+12 during cycle 2 when we've got to drive nMREQ high for
the operation?

Related question (probably) is how many cycles after a pipeline is cleared do we start executing.

Cycle 1 - A=PC, D=[PC], no execution, no decode
Cycle 2 - A=PC+4, D=[PC+4], no execution, decoding [PC]
Cycle 3 - A=PC+8, D=[PC+8], executing [PC], decoding [PC+4]

Given those two cycle set ups how do we order operations in our linear `cycle` function such that the
executing unit sees the correct value of R15.

1. Step memory unit (read A into D, don't touch A/R15)
2. Step execution unit if there's something to execute
3. Step decode unit if there's something to decode
4. Step pipeline and increment A/R15 as appropriate

Does that work? At time of writing I was stepping memory unit, then pipeline (and incremementing A/R15), the execution unit which meant that
execution happened on cycle 2 after a branch instead of cycle 3.

## Thoughts on source generators

I'm (mis)using C# source generators as a filthy macro preprocessor system in order to auto generate the logic for the various incantations of str/ldr/stm/ldm & alu ops.
They _do_ work and it's not that bad debugging code that's been generated this way but quite often a VS restart is required for the generated source files to appear in
the solution explorer.

Actually developing them is a bit like how I imagine punchcard devs felt in the 70s, lots of typing with no syntax validation and eventually running to get a cryptic error 
message which you can't click through to because VS has decided it doesn't want to load the code without a restart.

## WIP

* Cycle counting seems all off
	* At least LDR as I've set it up seems to take 2 cycles (one to set up A and one to write it back, cycle 3 is missing) but all cycle counting seems off what mgba is reporting
* Vast majority of simple roms are working now, PPU is whole frame only so lots of PPU specific tests don't work
* jsmolka arm suite _almost_ passes but fails on test 530 stmdb on empty rlist. Doubt that's affecting anything else so deprioritised
* mgba suite is the next serious target but current the main tests (DMA, Timing, Memory) all crash out after a while hitting illegal ops so _something_ is going wrong
* No games work yet, almost all need BLDCNT which I haven't implemented

## Status

| Component     | Status | Notes                         |
| ------------- | ------ | ----------------------------- |
| CPU - General | 95%    | All instructions implemented and DenSinH tests passed, definitely still bugs in some instructions |
| CPU - Timing  | 30%    | Implementation considers timing properly but it's totally untested until mgba test suite can run |
| Dma           | 30%    | Shown working for DMA3 in a Peter Lemon test rom (hello world), all DMAs theoretically implemented. Added vblank, hblank dmas but not special yet |
| Input         | 80%    | Register read/write working and input from SDL app appears fine, no IRQs implemented, tested working on DenSinH tests. Dedicated keypad test working. |
| Ppu           | 10%    | BG1-2 not implemented at all, BG0 only insofar as required for tests, 3-5 implemented as single end of frame render. No FIFO pixel pipeline, no IRQs, none of the required bits for tile maps etc |
| Gamepak       | ?      | Not sure what will even be needed here although I can load a basic rom |
| Serial        | 5%     | Just enough register read/writes mocked out to get roms which check them to pass |
| Timers        | 60%    | Timer are tested and working (including a dedicated test rom), count down timers are _not_ working though |
| APU           | 0%     | No registers or anything handled here yet |
| OpenBus       | 0%     | Noting as a separate area since there's a bunch of work to be done |
| IRQs          | 50%    | Interrupts are there from timers, ppu, dma, keypad and tested working to at least a basic extent. No serial or gamepad interrupts implemented |

## Surprising things

### Writes to odd registers during BIOS
