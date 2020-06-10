using System;
using System.IO;
using Emux.GameBoy;
using Emux.GameBoy.Cartridge;
using Emux.Common;
using Emux.GameBoy.Graphics;

namespace Automation
{
    class Program
    {
        private static bool dumpScreen = false;
        private static GameBoy gb;

        static void Main(string[] args)
        {
            var romFilePath = args[0];
            var screenshotPath = args[1];
            var ramFilePath = Path.ChangeExtension(romFilePath, "sav");

            var externalMemory = new BufferedExternalMemory(ramFilePath);
            var cartridge = new EmulatedCartridge(
                Path.GetFileName(romFilePath), 
                File.ReadAllBytes(romFilePath),
                externalMemory
            );
            externalMemory.SetBufferSize(cartridge.ExternalRamSize);
            gb = new GameBoy(cartridge, new WinMmTimer(60), false);
            gb.Gpu.VideoOutput = new FileVideoOutput(screenshotPath);

            gb.Paused += Gb_Paused;

            while (true) {
                gb.Step();
            }
        }

        private static void Gb_Paused(object sender, EventArgs e)
        {
            if (gb.Cpu.LastInstruction?.Disassembly == "ld b, b")
                dumpScreen = true;

            if (dumpScreen && (gb.Gpu.Stat & LcdStatusFlags.ModeMask) == LcdStatusFlags.VBlankMode)
            {
                gb.Gpu.VideoOutput.Blit();
                Environment.Exit(0);
            }
        }
    }
}
