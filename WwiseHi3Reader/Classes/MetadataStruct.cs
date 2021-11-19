namespace WwiseHi3Reader
{
    public struct HeaderProp
    {
        public uint headerSize, folderListSize, bankTableSize, soundTableSize, pckIndex;
    }
    public struct FolderProp
    {
        public uint offset, id;
        public string name;
    }
    public struct SoundBankProp
    {
        public uint id, headerOffset, headerSize, hircOffset, hircSize, pckIndex;
        public string relativePath;
    }
    public struct SoundFileProp
    {
        public uint id, fileOffset, fileSize, sampleRate, channel, pckIndex;
        public string relativePath;
    }
    public struct SongLibraryMetadata
    {
        public string artistName, titleName;
    }
}
