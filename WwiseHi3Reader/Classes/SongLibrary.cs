using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WwiseHi3Reader
{
    public class SongLibrary
    {
        public SongLibrary()
        {
            if (File.Exists(@".\Library.json"))
            {
                string libraryValue = File.ReadAllText(@".\Library.json");
                SongLibraryDictionary = JsonConvert.DeserializeObject<Dictionary<uint, SongLibraryMetadata>>(libraryValue);
            }
            else
                SongLibraryDictionary = new Dictionary<uint, SongLibraryMetadata> { };
        }

        public Dictionary<uint, SongLibraryMetadata> SongLibraryDictionary;
    }
}
