# WwiseHi3Reader
Honkai Impact 3rd's .pck (Wwise Audio) reader

# Usage
Play Audio Files Directly

    WwiseHi3Reader <path-of-the-game/single-pck-file>
Or Play Audio Files Directly but Start from certain Track

    WwiseHi3Reader <path-of-the-game/single-pck-file> <start-number-of-track>
Or With Loop for One Track

    WwiseHi3Reader <path-of-the-game/single-pck-file> loop
Or With Loop for One Track and start from specific Track Number

    WwiseHi3Reader <path-of-the-game/single-pck-file> loop <number-of-track>
For Listing Available Tracks Only:

    WwiseHi3Reader view <path-of-the-game/single-pck-file>
For Converting Tracks to OGG Vorbis:

    WwiseHi3Reader convert <path-of-the-game/single-pck-file> <path-of-the-output>

This program is using couple of Projects below:
 - [**ManagedBass (Un4Seen BASS Library Wrapper for .NET)**](https://github.com/ManagedBass/ManagedBass) by Mathew Sachin - For BASS Library Wrapper
 - [**BASS (Audio Library)**](https://www.un4seen.com) by Un4Seen - For Audio Player
 - [**WEMSharp**](https://github.com/Crauzer/WEMSharp) by Crauzer - For Audiokinetic Wwise WEM Stream to OGG Vorbis Conversion.

Player Shortcut:
- Arrow Left Key     : Go to Previous Track
- Arrow Right Key    : Go to Next Track
- q                  : Exit from Program
- Space              : Play/Pause
