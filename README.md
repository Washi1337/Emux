Emux
====
Emux is a free and open source GameBoy emulator written in C# that allows you to play the original GameBoy games on your computer. Emux is released under the GPLv3 license.

Features
========
- Play GameBoy Games and relive your nostalgic memories!
    - Open ROM dumps and execute them like the original GameBoy does.
        - Support for cartridges that contain memory bank controllers v1, v2 and v3.
        - Support for cartridges that contain external memory (for e.g. save files).
    - Disable the frame limit for the boring parts of the game that take forever and you'd rather skip (such as training your Pokémon, I won't judge).
    - Listen to the good ol' tunes and sound effects that the original GameBoy produced.
- Debugging capabilities
    - Break and continue execution whenever you want
    - View disassembly of the GameBoy memory.
    - Step through the code one instruction at a time.
    - Set breakpoints on specific memory addresses.
    - View the values of general CPU registers.
    - Virtual keypad for easier emulation of keypresses when paused or stepping through the code.
- Oh and probably a lot of funny glitches because the emulation is far from completed at this time.

Default keybindings
===================

| GameBoy Key | Keyboard binidng
|-------------|:-------------|
| Up          | Up           |
| Down        | Down         |
| Left        | Left         |
| Right       | Right        |
| A           | X            |
| B           | Z            |
| Start       | Enter        |
| Select      | Left Shift   |


Want to contribute?
===========
There is still much to be done so any help is welcome! Here is a couple of things you can do to contribute to the Emux project:
- Be a follower! Star the project and get the nice fuzzles of contributing without having to do much.
- Be a developer! Fork the project, make your changes and make a pull request. 
- Be a tester! Try out games and open issues in the GitHub issue tracker about games that cause glitches or completely fail to work.
    -  Try to provide as much information as you can. If possible, find out where and when the error occurs. The more details, the easier it is to reproduce and fix.
    -  IMPORTANT: Please do not upload the ROM itself if the cartridge is licensed. This project is not meant to spread (stolen) copies of games or whatsoever.

References
==========
This project is based on the specifications of the following papers (Therefore a big shoutout to the authors!):
- http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf
- http://bgb.bircd.org/pandocs.htm
- http://pastraiser.com/cpu/gameboy/gameboy_opcodes.html

External libraries
=======================
- [NAudio](https://github.com/naudio/NAudio) for the sound rendering.