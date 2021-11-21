using System;
using System.IO;
using WEMSharp;

namespace WwiseHi3Reader
{
    public class WwiseHi3ReaderMain
    {
        public static void Main(string[] args)
        {
            if (!(args.Length == 0))
            {
                string[] FileList;
                WwiseHi3Reader reader;

                FileInfo outputStream;
                MemoryStream rawStream;

                reader = new WwiseHi3Reader();

                if (args[0].ToLowerInvariant() == "view")
                {
                    FileList = Directory.GetFiles(args[1], "*.pck", SearchOption.AllDirectories);
                    reader.Read(FileList, true, true);

                    reader.FilterFileByFolder("sfx");
                    reader.FilterHighFreqOnly();

                    reader.ListTracks();

                    return;
                }
                else if (args[0].ToLowerInvariant() == "convert")
                {
                    FileList = Directory.GetFiles(args[1], "*.pck", SearchOption.AllDirectories);
                    reader.Read(FileList, true, true);

                    reader.FilterFileByFolder("sfx");
                    reader.FilterHighFreqOnly();

                    string outputFolder = args[2], outputPath, filename;

                    int i = 0;

                    Console.WriteLine($"Conversion Output: {outputFolder}");

                    foreach (SoundFileProp file in reader.FileList)
                    {
                        i++;
                        outputPath = Path.Combine(outputFolder, file.relativePath);

                        if (!Directory.Exists(outputPath))
                            Directory.CreateDirectory(outputPath);

                        if (reader.IsTitleByIDExist(file))
                        {
                            SongLibraryMetadata trackMetadata = reader.GetTrackInfo(file);
                            filename = $"{trackMetadata.artistName} - {trackMetadata.titleName}";
                        }
                        else
                        {
                            filename = $"{file.id}";
                        }

                        Console.Write($"Converting Track: {i}/{reader.FileList.Count} {Path.Combine(file.relativePath, $"{filename}.ogg")}...");
                        rawStream = new MemoryStream();

                        outputStream = new FileInfo(Path.Combine(outputPath, $"{filename}.ogg"));

                        if (!outputStream.Exists)
                        {
                            reader.GetStream(file, rawStream);
                            try
                            {
                                new WEMFile(rawStream).GenerateOGG(outputStream.Create(), false, false);

                                Console.WriteLine($"\b\b\b Done!");
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                outputStream.Delete();
                                Console.WriteLine($"\b\b\b Error while converting this one. This might not be an OGG Vorbis format file.\r\n{ex}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"\b\b\b Skipping!");
                        }

                        rawStream.Dispose();
                    }

                    return;
                }
                else
                {
                    if (!string.IsNullOrEmpty(Path.GetExtension(args[0])))
                        FileList = new string[] { args[0] };
                    else
                        FileList = Directory.GetFiles(args[0], "*.pck", SearchOption.AllDirectories);

                    int startTrack = 1;

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

                    reader.Read(FileList, true);

                    reader.FilterFileByFolder("sfx");
                    reader.FilterHighFreqOnly();

                    reader.PlayAllStream(startTrack, false);
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