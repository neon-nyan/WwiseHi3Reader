using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WwiseHi3Reader
{
    public class SongLibrary
    {
        public SongLibrary()
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Library.json")))
            {
                string libraryValue = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Library.json"));
                SongLibraryDictionary = JsonConvert.DeserializeObject<Dictionary<uint, SongLibraryMetadata>>(libraryValue);
            }
            else
                SongLibraryDictionary = new Dictionary<uint, SongLibraryMetadata> { };
        }

        public Dictionary<uint, SongLibraryMetadata> SongLibraryDictionary;
    }
}
