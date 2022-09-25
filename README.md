# KMSE

[![KMSE Build & Test](https://github.com/KeyDecoder/KMSE/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/KeyDecoder/KMSE/actions/workflows/build-and-test.yml)

KMSE (KeyDecoder's Master System Emulator) is an emulator for Sega Master System console, written in C#, focused on the Mark II.  This is just a fun research project emulating my favorite console.

As a child of the 80s/90s, I played the Master System so much growing up.  Loved playing Alex Kidd in Miracle World which was built into the console (I can still remember the Janken combinations of each boss).

**Credits**

There is so much great information out there on Master System and Z80 emulation.

http://www.codeslinger.co.uk/pages/projects/mastersystem.html - A great tutorial that helped me understand the basics and get something up and running

https://github.com/maxim-zhao/zexall-sms - Amazing SMS port of ZEXALL for exercising the Z80 emulation

https://www.smspower.org/Development/Documents - SMS Power Development Resources, huge amount of documentation on there (and shoutout to everyone who contributed it, it's amazing)

https://github.com/xdanieldzd/MasterFudgeMk2 / https://github.com/sklivvz/z80 - C# Z80 CPU emulation I used as a reference/basis for my implementation

I also used some other Emulators as a reference
* https://github.com/xdanieldzd/MasterFudgeMk2
* https://github.com/eightlittlebits/elbsms
* https://github.com/ocornut/meka
* https://github.com/ITotalJustice/TotalSMS

**Zexdoc validation**

All tests pass running ZEXDOC from https://github.com/maxim-zhao/zexall-sms

```
[12:28:33 INF] Application started. Press Ctrl+C to shut down.
[12:28:33 INF] Loading ROM from file ..\..\..\Validation\zexdoc.sms
[12:28:33 INF] Hosting environment: Production
Esc to exit console app
TAB to stop/start emulation
F to turn on/off file logging (default is off)
P to pause/unpause emulation (directly)
O to trigger pause button input
R to trigger reset button input
L to enable/disable CPU verbose logging
M to enable/disable Memory verbose logging
I to enable/disable I/O ports verbose logging
S to get CPU current status
Up to trigger controller A up
Down to trigger controller A down
Left to trigger controller A left
Right to trigger controller A right
Z to trigger controller A left button
X to trigger controller A right button
[12:28:33 INF] Z80 Instruction Exerciser 0.18
[12:28:33 INF] Documented flags version
[12:28:33 INF] Outputs:
[12:28:33 INF] * SDSC Debug Console
[12:28:33 INF] * SMS Mode 4
[12:28:33 INF] * SRAM
[12:28:33 INF]
[12:28:33 INF] ld hl, (nnnn) OK
[12:28:33 INF] ld sp, (nnnn) OK
[12:28:33 INF] ld (nnnn), hl OK
[12:28:33 INF] ld (nnnn), sp OK
[12:28:33 INF] ld (<ix|iy>+1), nn OK
[12:28:33 INF] ld <bc|de>, (nnnn) OK
[12:28:33 INF] ld <ix|iy>, (nnnn) OK
[12:28:33 INF] ld <ix|iy>, nnnn OK
[12:28:33 INF] ld a, <(bc)|(de)> OK
[12:28:33 INF] ld a, (nnnn) / ld (nnnn), a OK
[12:28:33 INF] ldd<r> (1) OK
[12:28:34 INF] ldd<r> (2) OK
[12:28:34 INF] ldi<r> (1) OK
[12:28:34 INF] ldi<r> (2) OK
[12:28:34 INF] ld (nnnn), <bc|de> OK
[12:28:34 INF] ld (nnnn), <ix|iy> OK
[12:28:34 INF] ld <bc|de|hl|sp>, nnnn OK
[12:28:34 INF] ld <b|c|d|e|h|l|(hl)|a>, nn OK
[12:28:34 INF] ld (<bc|de>), a OK
[12:28:34 INF] <scf|ccf> OK
[12:28:34 INF] ld (<ix|iy>+1), a OK
[12:28:34 INF] cpl OK
[12:28:35 INF] ld a, (<ix|iy>+1) OK
[12:28:35 INF] shf/rot (<ix|iy>+1) OK
[12:28:35 INF] <set|res> n, (<ix|iy>+1) OK
[12:28:36 INF] ld <h|l>, (<ix|iy>+1) OK
[12:28:36 INF] ld (<ix|iy>+1), <h|l> OK
[12:28:37 INF] ld <ixh|ixl|iyh|iyl>, nn OK
[12:28:38 INF] ld <b|c|d|e>, (<ix|iy>+1) OK
[12:28:39 INF] <inc|dec> bc OK
[12:28:40 INF] <inc|dec> de OK
[12:28:41 INF] <inc|dec> hl OK
[12:28:42 INF] <inc|dec> ix OK
[12:28:43 INF] <inc|dec> iy OK
[12:28:45 INF] <inc|dec> sp OK
[12:28:47 INF] ld (<ix|iy>+1), <b|c|d|e> OK
[12:28:48 INF] bit n, (<ix|iy>+1) OK
[12:28:52 INF] ld <b|c|d|e|h|l|(hl)|a>, <b|c|d|e|h|l|(hl)|a> OK
[12:28:54 INF] <inc|dec> a OK
[12:28:56 INF] <inc|dec> b OK
[12:28:59 INF] <inc|dec> c OK
[12:29:01 INF] <inc|dec> d OK
[12:29:03 INF] <inc|dec> e OK
[12:29:05 INF] <inc|dec> h OK
[12:29:07 INF] <inc|dec> l OK
[12:29:09 INF] <inc|dec> (hl) OK
[12:29:11 INF] <inc|dec> ixh OK
[12:29:13 INF] <inc|dec> ixl OK
[12:29:16 INF] <inc|dec> iyh OK
[12:29:18 INF] <inc|dec> iyl OK
[12:29:26 INF] <rlc|rrc|rl|rr|sla|sra|sll|srl> <b|c|d|e|h|l|(hl)|a> OK
[12:29:35 INF] ld <b|c|d|e|ixh|ixl|(ix+0)|iyh|iyl|(iy+0)|a>, <b|c|d|e|ixh|ixl|(ix+0)|iyh|iyl|(iy+0)|a> OK
[12:29:43 INF] <set|res> n, <b|c|d|e|h|l|(hl)|a> OK
[12:29:48 INF] <inc|dec> (<ix|iy>+1) OK
[12:29:52 INF] <rlca|rrca|rla|rra> OK
[12:29:58 INF] <rrd|rld> OK
[12:30:08 INF] aluop a, a OK
[12:30:17 INF] cpd<r> OK
[12:30:27 INF] cpi<r> OK
[12:31:09 INF] daa OK
[12:31:18 INF] neg OK
[12:31:39 INF] aluop a, nn OK
[12:32:00 INF] aluop a, b OK
[12:32:21 INF] aluop a, c OK
[12:32:43 INF] aluop a, d OK
[12:33:02 INF] aluop a, e OK
[12:33:22 INF] aluop a, h OK
[12:33:42 INF] aluop a, (hl) OK
[12:34:03 INF] aluop a, l OK
[12:34:22 INF] aluop a, ixh OK
[12:34:42 INF] aluop a, ixl OK
[12:35:01 INF] aluop a, iyh OK
[12:35:22 INF] aluop a, iyl OK
[12:36:14 INF] add hl, <bc|de|hl|sp> OK
[12:37:09 INF] add ix, <bc|de|ix|sp> OK
[12:38:02 INF] add iy, <bc|de|iy|sp> OK
[12:39:00 INF] bit n, <b|c|d|e|h|l|(hl)|a> OK
[12:40:46 INF] <adc|sbc> hl, <bc|de|hl|sp> OK
[12:44:13 INF] aluop a, (<ix|iy>+1) OK
[12:44:13 INF] Tests complete
```