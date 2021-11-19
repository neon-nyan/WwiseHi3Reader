using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using WEMSharp;

using ManagedBass;


namespace WwiseHi3Reader
{
    public partial class WwiseHi3Reader
    {
        MemoryStream rawStream;
        MemoryStream outputStream;

        SongLibrary songLibrary = new SongLibrary();

        public void GetStream(SoundFileProp fileProp, Stream output)
        {
            byte[] fileBytes = new byte[fileProp.fileSize];
            binaryReader = new BinaryReader(new FileStream((string)fileHashTable[fileProp.pckIndex], FileMode.Open, FileAccess.Read));
            binaryReader.BaseStream.Seek(fileProp.fileOffset, 0);

            fileBytes = binaryReader.ReadBytes((int)fileProp.fileSize);

            output.Write(fileBytes, 0, fileBytes.Length);

            output.Position = 0;
        }

        int playerPtr;
        bool playerPaused;
        bool playerLoop;
        bool playerLoopAll;
        int playerCurTrack;
        int playerTotalTrack;

        public void PlayStream(in SoundFileProp fileProp, bool loop = false)
        {
            playerLoop = loop;
            playerCurTrack = 1;
            playerTotalTrack = 1;
            InitializeConvertedStream(fileProp);

            PlayWithBass(fileProp);
        }

        public void PlaySetLoop() => playerLoop = true;

        void PlayStream(in SoundFileProp fileProp)
        {
            try
            {
                InitializeConvertedStream(fileProp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured while loading file with ID: {fileProp.id} from {fileProp.relativePath}.\r\nTraceback: {ex}");
                return;
            }

            PlayWithBass(fileProp);
        }

        public void PlayAllStreamStartFromID(int searchID, bool loopAll = false)
        {
            bool searchFinished = false;
            playerTotalTrack = FileList.Count;
            Console.Write($"Finding Track with ID: {searchID}...");
            for (; playerCurTrack < playerTotalTrack + 1; playerCurTrack++)
            {
                if (searchFinished)
                {
                    PlayStream(FileList[playerCurTrack - 1]);
                }
                else
                {
                    if (FileList[playerCurTrack - 1].id == (uint)searchID)
                    {
                        Console.WriteLine($"\b\b\b Found at {FileList[playerCurTrack - 1].relativePath} on Offset: {FileList[playerCurTrack - 1].fileOffset}, Size: {FileList[playerCurTrack - 1].fileSize} bytes");
                        searchFinished = true;
                        PlayStream(FileList[playerCurTrack - 1]);
                    }
                }
            }
        }

        public void PlayAllStream(int startOnTrack = 1, bool loopAll = false)
        {
            playerLoopAll = loopAll;
            if (playerLoopAll)
            {
                while (true)
                {
                    PlayAllStream(startOnTrack);
                }
            }
            else
            {
                PlayAllStream(startOnTrack);
            }
        }

        void PlayAllStream(int startOnTrack)
        {
            playerTotalTrack = FileList.Count;
            for (playerCurTrack = startOnTrack; playerCurTrack < playerTotalTrack + 1; playerCurTrack++)
            {
                PlayStream(FileList[playerCurTrack - 1]);
            }
        }

        private void PlayWithBass(in SoundFileProp fileProp)
        {
            DeviceInitFlags deviceFlags;

            switch (fileProp.channel)
            {
                case 0:
                    throw new Exception($"This sample doesn't have channel info.");
                case 1:
                    deviceFlags = DeviceInitFlags.Mono;
                    break;
                case 2:
                    deviceFlags = DeviceInitFlags.Stereo;
                    break;
                default:
                    deviceFlags = DeviceInitFlags.Device3D;
                    break;
            }

            Bass.Init(-1, (int)fileProp.sampleRate);

            using (outputStream)
            {
                playerPtr = Bass.CreateStream(outputStream.ToArray(), 0, outputStream.Length, BassFlags.Default);
                playerPaused = false;
                Bass.ChannelPlay(playerPtr);

                SetPlayerState();
            }

            Task.Run(() => { KeyWatcher(); });

            TimeSpan currentPos;
            TimeSpan fileLength = TimeSpan.FromSeconds(Bass.ChannelBytes2Seconds(playerPtr, Bass.ChannelGetLength(playerPtr)));

            Console.WriteLine($"Playing Track [{playerCurTrack}/{playerTotalTrack}] with ID: {fileProp.id} from {fileProp.relativePath}");
            Console.WriteLine($"    \u2514\u2500 channel: {deviceFlags}, sampleRate: {fileProp.sampleRate} Hz, fileOffset: {fileProp.fileOffset}, fileSize: {fileProp.fileSize} bytes");

            PlayerFindTitleByID(fileProp);

            while (Bass.ChannelIsActive(playerPtr) == PlaybackState.Playing || playerPaused)
            {
                currentPos = TimeSpan.FromSeconds(Bass.ChannelBytes2Seconds(playerPtr, Bass.ChannelGetPosition(playerPtr)));
                Console.Write($"\r{currentPos} - {fileLength} | CPU Usage: {Math.Round(Bass.CPUUsage, 3)}%");
                Thread.Sleep(100);
            }

            Console.WriteLine();
            Bass.ChannelStop(playerPtr);
            Bass.StreamFree(playerPtr);

            outputStream.Dispose();
        }

        void PlayerFindTitleByID(in SoundFileProp fileProp)
        {
            try
            {
                SongLibraryMetadata metadata = new SongLibraryMetadata();
                metadata = songLibrary.SongLibraryDictionary[fileProp.id];
                Console.WriteLine($"    \u2514\u2500 Title: {metadata.titleName} by {metadata.artistName}");
            }
            catch (KeyNotFoundException) { return; }
        }

        void PlayerJumpDialog()
        {
            playerPaused = true;
            string input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                playerCurTrack--;
            }
            else
            {
                playerCurTrack = UInt16.Parse(input) - 1;
            }
            playerPaused = false;
        }

        void KeyWatcher()
        {
            while (true)
            {
                Thread.Sleep(120);
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.G:
                        long playerLastPos = Bass.ChannelGetPosition(playerPtr);
                        Bass.ChannelStop(playerPtr);
                        Bass.StreamFree(playerPtr);
                        PlayerJumpDialog();
                        break;
                    case ConsoleKey.RightArrow:
                        Console.Write(" Next!");
                        Bass.ChannelStop(playerPtr);
                        break;
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        Environment.Exit(0);
                        break;
                    case ConsoleKey.LeftArrow:
                        if (playerCurTrack != 1)
                        {
                            Console.Write(" Previous!");
                            playerCurTrack -= 2;
                        }
                        else if (playerCurTrack == 1)
                        {
                            string warnFirstTrack = " This is already the first track!";
                            Console.Write(warnFirstTrack);
                            Thread.Sleep(3000);
                            Console.Write(new string(' ', warnFirstTrack.Length));
                            break;
                        }
                        else
                        {
                            Console.Write(" Previous!");
                            playerCurTrack--;
                        }
                        Bass.ChannelStop(playerPtr);
                        break;
                    case ConsoleKey.Spacebar:
                        if (!playerPaused)
                        {
                            Console.Write(" Pause!");
                            Bass.ChannelPause(playerPtr);
                            playerPaused = true;
                        }
                        else
                        {
                            Console.Write("       ");
                            Console.Write("\b\b\b\b\b\b\b");
                            Bass.ChannelPlay(playerPtr);
                            playerPaused = false;
                        }
                        break;

                }
            }
        }

        void SetPlayerState()
        {
            if (playerLoop) Bass.ChannelFlags(playerPtr, BassFlags.Loop, BassFlags.Loop);
        }

        void InitializeConvertedStream(in SoundFileProp fileProp)
        {
            rawStream = new MemoryStream();
            outputStream = new MemoryStream();

            GetStream(fileProp, rawStream);

            WEMFile _wemReader = new WEMFile(rawStream);
            _wemReader.GenerateOGG(outputStream, false, false);

            rawStream.Dispose();
            _wemReader = null;

            outputStream.Position = 0;
        }

        SoundFileProp ReadSoundFileRIFFMetadata(SoundFileProp data)
        {
            binaryReader.BaseStream.Position = data.fileOffset + 0x16; // Skip 16 bytes of the RIFF header
            data.channel = binaryReader.ReadUInt16(); // Read sound channel
            data.sampleRate = binaryReader.ReadUInt32(); // Read sample rate

            return data;
        }
    }
}
