# Notes

This document contains random notes either about TODOs in the code, or reasons for implementation choices, or anything else that might help produce a blog article.

## Missing implementation

- Open bus behaviour, I've deliberately architected the CPU so that core.A & core.D should be 
correct at any given cycle which should allow for open bus behaviour but I don't know 
exactly how it should work yet so haven't actually implemented anything
- Word/HW aligned addresses. As I understand it there's various places where mis-aligned read/writes either get aligned automatically or cause other odd behaviour. 
I've not been very consistent about implementing that behaviour as I've gone which may cause me trouble later. (e.g. what happens during thumb -> arm BX when PC is not aligned to word boundary?)


## WIP

* Register accurate up to when arm wrestler (thumb) switches to thumb mode at `0x08031544`
* Cycle counting seems all off
	* At least LDR as I've set it up seems to take 2 cycles (one to set up A and one to write it back, cycle 3 is missing) but all cycle counting seems off what mgba is reporting
* Looks like I've forgotte to not set flags when S=0 on ALU ops in Arm mode