using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WwiseHi3Reader
{
    public partial class WwiseHi3Reader
    {
        HeaderProp _header;
        Hashtable folderHashTable = new Hashtable();
        Hashtable fileHashTable = new Hashtable();
        List<FolderProp> _folders;
        List<SoundFileProp> soundFileList = new List<SoundFileProp>();
        List<SoundBankProp> soundBankList = new List<SoundBankProp>();
        public List<SoundFileProp> FileList;

        BinaryReader binaryReader;
        private string magicWord = "AKPK";

        private bool quiet = false;

        private string filename;
        public WwiseHi3Reader()
        {
            // Initial list of Folders
            _folders = new List<FolderProp>();
            _header = new HeaderProp();
            _header.pckIndex = 0;

            // Init FileList to soundFileList first;
            FileList = soundFileList;
        }

        uint soundFilePerFileCount;

        public void Read(string pckPath, bool skipSoundBank = true, bool quiet = false)
        {
            this.quiet = quiet;
            folderHashTable = new Hashtable();
            filename = Path.GetFileNameWithoutExtension(pckPath);

            if (fileHashTable.ContainsValue(pckPath))
            {
                Console.WriteLine($"File {filename} is already exist in entry. Skipping!");
                return;
            }

            fileHashTable.Add(_header.pckIndex, pckPath);
            ReadHeader(skipSoundBank, pckPath);
        }

        public void ListTracks()
        {
            SoundFileProp list;
            Console.WriteLine($"Total Track: {FileList.Count}");
            for (int i = 1; i < FileList.Count + 1; i++)
            {
                list = FileList[i - 1];
                Console.WriteLine($"Track: {i} ID: {list.id} ({list.relativePath}) Offset: {list.fileOffset} Size: {list.fileSize} FreqHz: {list.sampleRate} Ch: {list.channel}");
                FindTitleByID(list);
            }
        }

        void FindTitleByID(in SoundFileProp fileProp)
        {
            try
            {
                SongLibraryMetadata metadata = GetTrackInfo(fileProp);
                Console.WriteLine($"\u2514\u2500 Title: {metadata.titleName} by {metadata.artistName}");
            }
            catch (KeyNotFoundException) { return; }
        }

        public bool IsTitleByIDExist(in SoundFileProp fileProp) => songLibrary.SongLibraryDictionary.ContainsKey(fileProp.id);

        public SongLibraryMetadata GetTrackInfo(in SoundFileProp fileProp) => songLibrary.SongLibraryDictionary[fileProp.id];

        public void Read(string[] pckPaths, bool skipSoundBank = true, bool quiet = false)
        {
            for (uint i = 0; i < pckPaths.Length; i++)
            {
                Read(pckPaths[i], skipSoundBank, quiet);
            }
        }

        void ReadHeader(bool skipSoundBank, string pckPath)
        {
            if (!quiet) Console.Write($"Reading {filename}...");
            binaryReader = new BinaryReader(new FileStream(pckPath, FileMode.Open, FileAccess.Read));

            if (new string(binaryReader.ReadChars(4)) != magicWord)
                throw new FormatException($"This file is not a Wwise Audio Pack format!");

            binaryReader.BaseStream.Seek(0x8, 0); // Seek 0x8

            if (binaryReader.ReadUInt32() != 1)
                throw new FormatException($"PCK must be in Little-Endian format. Big-Endian format isn't supported.");

            binaryReader.BaseStream.Seek(0x4, 0); // Seek again with 0x4

            soundFilePerFileCount = 0;
            _header.headerSize = binaryReader.ReadUInt32();
            binaryReader.ReadUInt32(); // Skip
            _header.folderListSize = binaryReader.ReadUInt32();
            _header.bankTableSize = binaryReader.ReadUInt32();
            _header.soundTableSize = binaryReader.ReadUInt32();
            binaryReader.ReadUInt32(); // Skip

            ReadFolderMetadata(); // Read list of Folder Metadata

            if (skipSoundBank)
                SkipSoundBankMetadata(); // Skip Sound Bank Metadata
            else
                ReadSoundBankMetadata(); // Read list of Sound Bank Metadata

            ReadSoundFileMetadata(); // Read list of Sound File Metadata

            _header.pckIndex++;

            if (!quiet) Console.WriteLine(
                string.Format(" Done!\r\n    \u2514\u2500 folderList: {0}, bankTable: {1}, soundTable: {2}, soundFile: {3}",
                _header.folderListSize,
                _header.bankTableSize,
                _header.soundTableSize,
                soundFilePerFileCount
                ));
        }

        void ReadFolderMetadata()
        {
            uint folderListStartPos = (uint)binaryReader.BaseStream.Position;
            uint foldersCount = binaryReader.ReadUInt32();

            FolderProp folderProp;

            for (int i = 0; i < foldersCount; i++)
            {
                folderProp = new FolderProp();

                folderProp.offset = binaryReader.ReadUInt32() + folderListStartPos;
                folderProp.id = binaryReader.ReadUInt32();

                uint folderListTempPos = (uint)binaryReader.BaseStream.Position;

                // Go grab Folder name
                binaryReader.BaseStream.Seek(folderProp.offset, 0);

                StringBuilder sb = new StringBuilder();
                while (binaryReader.PeekChar() != '\0')
                {
                    sb.Append(binaryReader.ReadChar());
                    binaryReader.ReadChar(); // Skip \0 because Hi3's PCK format is Little-Endian
                }
                folderProp.name = sb.ToString();

                _folders.Add(folderProp);

                try
                {
                    folderHashTable.Add(_folders[i].id, _folders[i].name);
                }
                catch (ArgumentException)
                {
                    if (!quiet) Console.Write($"\r\n    folderKey {_folders[i].name} with ID: {_folders[i].id} is duplicate.");
                }

                // Return to where we were in the List
                binaryReader.BaseStream.Seek(folderListTempPos, 0);
            }

            // Jump to past the Folder section
            binaryReader.BaseStream.Seek(folderListStartPos + _header.folderListSize, 0);
        }

        void SkipSoundBankMetadata()
        {
            for (uint i = binaryReader.ReadUInt32(); i > 0; i--)
                binaryReader.BaseStream.Seek(binaryReader.BaseStream.Position += 20, 0);
        }

        void ReadSoundBankMetadata()
        {
            SoundBankProp soundBankProp;
            SoundFileProp soundFileProp;
            long lastFileOffset;
            uint soundBankCount = binaryReader.ReadUInt32();

            while (soundBankCount > 0)
            {
                soundBankProp = new SoundBankProp();

                // Sound Bank info
                soundBankProp.pckIndex = _header.pckIndex;
                soundBankProp.id = binaryReader.ReadUInt32();
                uint soundBankOffsetMult = binaryReader.ReadUInt32();
                uint soundBankSize = binaryReader.ReadUInt32();
                soundBankProp.headerOffset = binaryReader.ReadUInt32()*soundBankOffsetMult;
                uint soundBankFolder = binaryReader.ReadUInt32();
                string soundBankFolderName = (string)folderHashTable[soundBankFolder];
                string pckFileName = filename;
                soundBankProp.relativePath = Path.Combine(pckFileName, soundBankFolderName, $"Bank_{soundBankProp.id}");

                uint soundBankTempPos = (uint)binaryReader.BaseStream.Position;

                // Actual Sound Bank, header
                binaryReader.BaseStream.Seek(soundBankProp.headerOffset, 0);
                binaryReader.ReadUInt32(); // Bank head Identifier
                soundBankProp.headerSize = binaryReader.ReadUInt32() + 8; // Include 0x8 head
                uint firstBankSectionPos = soundBankProp.headerOffset + soundBankProp.headerSize;
                binaryReader.BaseStream.Seek(firstBankSectionPos, 0);

                // Check if we have a DIDX section (contains embedded *.wem files) to deal with
                string firstSectionIdent = new string(binaryReader.ReadChars(4));
                if (firstSectionIdent == @"DIDX")
                {
                    uint didxSize = binaryReader.ReadUInt32();
                    uint didxFilesCount = didxSize / 12; // Each file description is 0xC bytes
                    uint dataFilesOffset = firstBankSectionPos + didxSize + 16; // 16 is for the DIDX+DATA headers

                    for (int j = 0; j < didxFilesCount; j++)
                    {
                        soundFileProp = new SoundFileProp();

                        soundFileProp.pckIndex = _header.pckIndex;
                        soundFileProp.id = binaryReader.ReadUInt32();
                        soundFileProp.fileOffset = binaryReader.ReadUInt32() + dataFilesOffset;
                        soundFileProp.fileSize = binaryReader.ReadUInt32();

                        lastFileOffset = binaryReader.BaseStream.Position;
                        soundFileProp = ReadSoundFileRIFFMetadata(soundFileProp);
                        binaryReader.BaseStream.Position = lastFileOffset;

                        soundFileProp.relativePath = soundBankProp.relativePath;

                        soundFileList.Add(soundFileProp);
                        soundFilePerFileCount++;
                    }

                    binaryReader.BaseStream.Seek(firstBankSectionPos + didxSize + 12, 0); // Get us to the DATA section size
                    uint dataSectionSize = binaryReader.ReadUInt32();
                    binaryReader.ReadBytes((int)dataSectionSize + 4); // Skip to the end of DATA + 4, so we're back after HIRC Identifier
                }

                soundBankProp.hircOffset = (uint)binaryReader.BaseStream.Position - 4; // We already hit the HIRC head Identifier
                soundBankProp.hircSize = binaryReader.ReadUInt32() + 8; // Include 0x8 head

                // Go back to SoundBank list
                binaryReader.BaseStream.Seek(soundBankTempPos, 0);

                soundBankCount--;
                soundBankList.Add(soundBankProp);
            }
        }

        void ReadSoundFileMetadata()
        {
            uint soundFileCount = binaryReader.ReadUInt32();
            long lastFileOffset;

            SoundFileProp soundFileProp;

            for (int i = 0; i < soundFileCount; i++)
            {
                soundFileProp = new SoundFileProp();

                soundFileProp.pckIndex = _header.pckIndex;
                soundFileProp.id = binaryReader.ReadUInt32();
                uint soundFileOffsetMult = binaryReader.ReadUInt32();
                soundFileProp.fileSize = binaryReader.ReadUInt32();
                soundFileProp.fileOffset = binaryReader.ReadUInt32() * soundFileOffsetMult;
                uint soundFileFolder = binaryReader.ReadUInt32();
                string soundFileFolderName = (string)folderHashTable[soundFileFolder];
                string pckFileName = filename;
                soundFileProp.relativePath = Path.Combine(pckFileName, soundFileFolderName);

                lastFileOffset = binaryReader.BaseStream.Position;
                soundFileProp = ReadSoundFileRIFFMetadata(soundFileProp);
                binaryReader.BaseStream.Position = lastFileOffset;

                soundFileList.Add(soundFileProp);
                soundFilePerFileCount++;
            }
        }

#if (NETCOREAPP)
        public void FilterFileByFolder(string searchValue, StringComparison compareMethod = StringComparison.CurrentCultureIgnoreCase) => FileList = FileList.Where(x => x.relativePath.Contains(searchValue, compareMethod)).ToList();
#else
        public void FilterFileByFolder(string searchValue) => FileList = FileList.Where(x => x.relativePath.Contains(searchValue)).ToList();
#endif
        public SoundFileProp SelectFileByID(uint id) => soundFileList.FirstOrDefault(x => x.id == id);
        public void FilterHighFreqOnly() => FileList = FileList.Where(x => x.sampleRate >= 44100 && x.channel >= 2).ToList();
        public void ResetFilter() => FileList = soundFileList;
    }
}
