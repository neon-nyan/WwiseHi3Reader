using BnkExtractor.Revorb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WEMSharp;

namespace WwiseHi3Reader
{
    public class WwiseHi3ReaderMain
    {
        static string[] suppExt = new string[] { "pck" };

        public static void LoadAndCompareNewer(string[] args, out WwiseHi3Reader FileReaderOld, out WwiseHi3Reader FileReaderNew)
        {
            FileReaderOld = new();
            FileReaderNew = new();
            IEnumerable<string> FileListOld;
            IEnumerable<string> FileListNew;

            FileListOld = Directory.EnumerateFiles(args[1].Replace("\n", "\\n"), "*.pck", SearchOption.AllDirectories);
            FileListNew = Directory.EnumerateFiles(args[2].Replace("\n", "\\n"), "*.pck", SearchOption.AllDirectories);

            FileReaderOld.ReadEnumerate(FileListOld);
            FileReaderNew.ReadEnumerate(FileListNew);

            FileReaderNew.CompareAndEliminateOldFile(FileReaderOld);
        }

        public static void StartConversion(WwiseHi3Reader InputReader, string OutputDir, bool isRAW = false)
        {
            string outputPath, filename;
            OutputDir = OutputDir.Replace("\n", "\\n");
            FileInfo outputInfo, outputInfoRevorb;

            int i = 0;

            Console.WriteLine($"Conversion Output: {OutputDir}");

            foreach (SoundFileProp file in InputReader.soundFileList)
            {
                i++;
                outputPath = Path.Combine(OutputDir, file.relativePath);

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                if (InputReader.IsTitleByIDExist(file))
                {
                    SongLibraryMetadata trackMetadata = InputReader.GetTrackInfo(file);
                    filename = $"{trackMetadata.artistName} - {trackMetadata.titleName}";
                }
                else
                {
                    filename = $"{file.id}";
                }

                Console.Write($"Converting Track: {i}/{InputReader.soundFileList.Count} {Path.Combine(file.relativePath, $"{filename}.ogg")}...");

                using (Stream rawStream = new MemoryStream())
                {
                    outputInfo = new FileInfo(Path.Combine(outputPath, $"{filename}.ogg"));
                    outputInfoRevorb = new FileInfo(Path.Combine(outputPath, $"{filename}.oggRevorb"));

                    using (Stream outputStream = outputInfo.Create())
                    {
                        try
                        {
                            InputReader.GetStream(file, rawStream);

                            if (isRAW)
                            {
                                rawStream.Seek(0, SeekOrigin.Begin);
                                rawStream.CopyTo(outputStream);
                            }
                            else
                            {
                                using (Stream outputRevorbedStream = outputInfoRevorb.Create())
                                {
                                    new WEMFile(rawStream).GenerateOGG(outputStream, false, false);
                                    outputStream.Position = 0;
                                    RevorbSharp.Convert(outputStream, outputRevorbedStream);
                                }
                            }

                            Console.WriteLine($"\b\b\b Done!");
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            outputStream.Dispose();
                            outputInfo.Delete();
                            Console.WriteLine($"\b\b\b Error while converting this one. This might not be an OGG Vorbis format file.\r\n{ex}");
                        }
                    }

                    if (!isRAW)
                    {
                        outputInfo.Delete();
                        outputInfoRevorb.MoveTo(outputInfo.FullName);
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            if (!(args.Length == 0))
            {
                int startTrack = 1;
                string[] FileList;
                WwiseHi3Reader reader;

                reader = new WwiseHi3Reader();

                switch (args[0].ToLowerInvariant())
                {
                    case "convertraw":
                        {
                            if (!string.IsNullOrEmpty(Path.GetExtension(args[1])))
                                FileList = new string[] { args[1] };
                            else
                                FileList = Directory.GetFiles(args[1], "*.*", SearchOption.AllDirectories)
                                           .Where(file =>
                                                suppExt
                                                .Any(x => file
                                                          .EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToArray();

                            reader.Read(FileList, true
#if DEBUG
                                ,false
#else
                                , true
#endif
                                );

                            // reader.FilterFileByFolder("sfx");
                            reader.FilterHighFreqOnly();

                            StartConversion(reader, args[2], true);
                            break;
                        }
                    case "playnewer":
                        {
                            WwiseHi3Reader FileReaderOld = null;
                            WwiseHi3Reader FileReaderNew = null;

                            LoadAndCompareNewer(args, out FileReaderOld, out FileReaderNew);

                            FileReaderNew.PlayAllStream(1, false);
                        }
                        break;
                    case "convertnewer":
                        {
                            WwiseHi3Reader FileReaderOld = null;
                            WwiseHi3Reader FileReaderNew = null;

                            LoadAndCompareNewer(args, out FileReaderOld, out FileReaderNew);

                            StartConversion(FileReaderNew, args[3]);
                        }
                        break;
                    case "view":
                        {
                            if (!string.IsNullOrEmpty(Path.GetExtension(args[1])))
                                FileList = new string[] { args[1] };
                            else
                                FileList = Directory.GetFiles(args[1], "*.*", SearchOption.AllDirectories)
                                           .Where(file =>
                                                suppExt
                                                .Any(x => file
                                                          .EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToArray();

                            reader.Read(FileList, true, true);

                            // reader.FilterFileByFolder("sfx");
                            // reader.FilterHighFreqOnly();

                            reader.ListTracks();
                        }
                        break;
                    case "convert":
                        {
                            if (!string.IsNullOrEmpty(Path.GetExtension(args[1])))
                                FileList = new string[] { args[1] };
                            else
                                FileList = Directory.GetFiles(args[1], "*.*", SearchOption.AllDirectories)
                                           .Where(file =>
                                                suppExt
                                                .Any(x => file
                                                          .EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToArray();

                            reader.Read(FileList, true, true);

                            // reader.FilterFileByFolder("sfx");
                            reader.FilterHighFreqOnly();

                            StartConversion(reader, args[2]);
                            break;
                        }
                    default:
                        if (!string.IsNullOrEmpty(Path.GetExtension(args[0])))
                            FileList = new string[] { args[0] };
                        else
                            FileList = Directory.GetFiles(args[0], "*.*", SearchOption.AllDirectories)
                                       .Where(file =>
                                            suppExt
                                            .Any(x => file
                                                      .EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToArray();

                        try
                        {
                            if (args[1].ToLowerInvariant() == "loop")
                            {
                                reader.PlaySetLoop();
                                startTrack = int.Parse(args[2]);
                            }
                            else
                            {
                                startTrack = int.Parse(args[1]);
                            }
                        }
                        catch (IndexOutOfRangeException) { }

                        reader.Read(FileList, false);

                        // reader.FilterFileByFolder("sx");
                        // reader.FilterHighFreqOnly();

                        reader.PlayAllStream(startTrack, false);
                        break;
                }
            }
            else
            {
                Console.WriteLine(@"Honkai Impact 3's Wwise Audio Player by neon-nyan
Usage:
    WwiseHi3Reader <path-of-the-game> <start-number-of-track>
Or:
    WwiseHi3Reader <path-of-the-game>
Or With Loop for One Track
    WwiseHi3Reader <path-of-the-game> loop 
Or With Loop for One Track and start for specific Track Number
    WwiseHi3Reader <path-of-the-game> loop <number-of-track>
    
For Listing the Available Tracks Only:
    WwiseHi3Reader view <path-of-the-game>
    
For Converting Tracks to OGG Vorbis:
    WwiseHi3Reader convert <path-of-the-game> <path-of-the-output>

This program is using couple of Projects below:
 - ManagedBass (Un4Seen BASS Library Wrapper for .NET) by Mathew Sachin - For BASS Library Wrapper
 - BASS (Audio Library) by Un4Seen - For Audio Player
 - WEMSharp by Crauzer - For Audiokinetic Wwise WEM Stream to OGG Vorbis Conversion.

Player Shortcut:
 Arrow Left Key     : Go to the Previous Track
 Arrow Right Key    : Go to the Next Track
 q                  : Exit from Program
 Space              : Play/Pause

Press enter to quit...");
                Console.ReadLine();
            }
        }
    }
}