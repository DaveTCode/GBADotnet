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
	- Currently assuming 7 wait states for all word reads to 0x0800_0000 region so a normal instruciton takes 1 cycle + 7 wait states. mgba thinks it should be 6 (48 -> 54) which would imply that the wait states should add to 5
	- `WAITCNT` isn't currently implemented but should default to 0x0 which I think means wait state 0 first access is 4 and second is 2, which is 6 but not the 5 I would expect.
	- If we assume that _both_ memory reads are sequential then we get 2 * 2 = 4 which is _still_ not 5
- I'm going to need to plumb through SEQ to the memory unit to achieve correct wait states I think
- Writing a word to a 16 bit address bus (or reading) will cause two writes across the bus, one will be an extended N (or S) cycle and the other will be an extended S cycle
	- The question is whether or not we need to emulate that the two halves of that read/write are happening on different cycles
	- If so then I think the best way to achieve it will be to set the memory unit up as a clocked unit and have it act as yet another state machine

## Memory bus musings

I don't really like lots of the code relating to the memory bus. The CPU part is quite nice, we set up A/D/MAS/nRW etc and then the next cycle the memory unit knows what to do.
The trouble is with the way that read/writes are farmed out to downstream subsystems and particularly how that relates to the bus width.

e.g. PPU bus is 16 bit, if a byte is written then that byte appears on both halves of the data bus and a half word is actually written. That feels like something which could
be implemented at a more fundamental level. Is it true for all 16 bit buses? If so then can we be clever with how A/D/MAS interact to avoid calling multiple different functions?

The other big issue is around wait states and cycles. If a word is written to a 16 bit bus then what happens? Presumably that takes 1 N(or S) and 1 S cycle to complete. 

## Thoughts on source generators

I'm (mis)using C# source generators as a filthy macro preprocessor system in order to auto generate the logic for the various incantations of str/ldr/stm/ldm & alu ops.
They _do_ work and it's not that bad debugging code that's been generated this way but quite often a VS restart is required for the generated source files to appear in
the solution explorer.

## WIP

* Register accurate up to when arm wrestler (thumb) switches to thumb mode at `0x08031544`
* Cycle counting seems all off
	* At least LDR as I've set it up seems to take 2 cycles (one to set up A and one to write it back, cycle 3 is missing) but all cycle counting seems off what mgba is reporting
* Looks like I've forgotten to not set flags when S=0 on ALU ops in Arm mode