using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Emux.GameBoy.Cartridge;
using Emux.NAudio;
using NAudio.Wave;

namespace Emux.MonoGame
{
    public static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            PrintAbout();
            (string romFile, string saveFile) = ParseArguments(args);
            var settings = ReadSettings();

            using (var host = new EmuxHost(settings))
            using (var saveFs = File.Open(saveFile, FileMode.OpenOrCreate))
            {
                var cartridge = new EmulatedCartridge(File.ReadAllBytes(romFile), new StreamedExternalMemory(saveFs));
                var device = new GameBoy.GameBoy(cartridge, host, true);
                host.GameBoy = device;
                
                device.Gpu.VideoOutput = host;
                
                var mixer = new GameBoyNAudioMixer();
                mixer.Connect(device.Spu);
                var player = new DirectSoundOut();
                player.Init(mixer);
                player.Play();
                
                host.Run();
            }
        }

        private static void PrintAbout()
        {
            Console.WriteLine("Emux.MonoGame: v{0}, Core: v{1}", 
                typeof(Program).Assembly.GetName().Version,
                typeof(GameBoy.GameBoy).Assembly.GetName().Version);
            Console.WriteLine("Copyright © Washi 2017-2018");
            Console.WriteLine("Repository and issue tracker: https://www.github.com/Washi1337/Emux");
        }

        private static Settings ReadSettings()
        {
            Settings settings = null;
            var serializer = new DataContractJsonSerializer(typeof(Settings));

            string settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "settings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    using (var fs = File.OpenRead(settingsFile))
                    {
                        settings = (Settings) serializer.ReadObject(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error reading settings file.");
                    Console.Error.WriteLine(ex.Message);
                }
            }

            if (settings == null)
            {
                Console.WriteLine("No valid settings file found. Reverting to default settings.");
                settings = new Settings();
                
                using (var fs = File.Create(settingsFile))
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.Default, false, true))
                {
                    serializer.WriteObject(writer, settings);
                }
            }

            return settings;
        }

        private static (string romFile, string saveFile) ParseArguments(string[] args)
        {
            string romFile = null;
            string saveFile = null;

            switch (args.Length)
            {
                default:
                    Console.WriteLine("Usage: Emux.MonoGame.exe romfile [savefile]");
                    Environment.Exit(0);
                    break;
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
                Console.Error.WriteLine("ROM could not be found!");
                Environment.Exit(1);
            }

            return (romFile, saveFile);
        }
    }
}