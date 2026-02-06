namespace Lua;

public static partial class LuaConstants
{
    public const string VersionMajor = "5";
    public const string VersionMinor = "4";
    public const string VersionRelease = "8";

    public const int VersionNum = 504;
    public const int VersionReleaseNum = VersionNum * 100 + 8;

    public const string Version = "Lua " + VersionMajor + "." + VersionMinor;
    public const string Release = Version + "." + VersionRelease;
    public const string Copyright = Release + "  Copyright (C) 1994-2025 Lua.org, PUC-Rio";
    public const string Authors = "R. Ierusalimschy, L. H. de Figueiredo, W. Celes";

    public const byte LuacVersion = ((VersionNum / 100) * 16) + (VersionNum % 100);
    public const byte LuacFormat = 0; // this is the official format
    public const int LuacInt = 0x5678;
    public const double LuacNum = 370.5;

    public const int InitStackSize = 256;

    public const int LuaMinStack = 20;
}
