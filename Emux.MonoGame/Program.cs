using System;
using System.IO;
using Emux.GameBoy.Cartridge;
using Emux.NAudio;
using NAudio.Wave;

namespace Emux.MonoGame
{
    public static class Program
    {
        private static void PrintAbout()
        {
            Console.WriteLine("Emux.MonoGame: v{0}, Core: v{1}", 
                typeof(Program).Assembly.GetName().Version,
                typeof(GameBoy.GameBoy).Assembly.GetName().Version);
            Console.WriteLine("Copyright © Washi 2017-2018");
            Console.WriteLine("Repository and issue tracker: https://www.github.com/Washi1337/Emux");
        }
        
        [STAThread]
        private static void Main(string[] args)
        {
            PrintAbout();

            string romFile;
            string saveFile;
            
            switch (args.Length)
            {
                default:
                    Console.WriteLine("Usage: Emux.MonoGame.exe romfile [savefile]");
                    return;
                case 1:
                    romFile = args[0].Replace("\"", "");
                    saveFile = Path.ChangeExtension(romFile, ".sav");
                    break;
                case 2:
                    romFile = args[0].Replace("\"", "");
                    saveFile = args[1].Replace("\"", "");
                    break;
            }

            if (!File.Exists(romFile))
            {
                Console.WriteLine("ROM could not be found!");
                return;
            }

            
            using (var game = new EmuxHost())
            using (var saveFs = File.Open(saveFile, FileMode.OpenOrCreate))
            {
                var cartridge = new EmulatedCartridge(File.ReadAllBytes(romFile), new StreamedExternalMemory(saveFs));
                var device = new GameBoy.GameBoy(cartridge, game, true);
                game.GameBoy = device;
                
                device.Gpu.VideoOutput = game;
                
                var mixer = new GameBoyNAudioMixer();
                mixer.Connect(device.Spu);
                var player = new DirectSoundOut();
                player.Init(mixer);
                player.Play();
                
                game.Run();
            }
        }
    }
}