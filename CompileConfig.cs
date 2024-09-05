using System;
using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace MoSpeedUI;

public class CompileConfig
{
    public string MoSpeedPath { get; set; }
    public IStorageFile? OutputFile { get; set; }
    public IStorageFile? CurrentFile { get; set; }
    public List<IStorageFile> Files { get; set; } = new();
    // /platform=
    public Func<string>? TargetPlatform { get; set; }
    // /memconfig=
    public Func<int>? Vc20Conf { get; set; }
    // /bigram=
    public bool C64Conf { get; set; }
    // /progstart=
    public string? ProgramStartAdd { get; set; }
    // /varstart=
    public string? VariableStartAdd { get; set; }
    // /varend=
    public string? StringMemEndAdd { get; set; }
    // /runtimestart=
    public string? RuntimeStartAdd { get; set; }
    // /memhole=
    public string? MemHoles { get; set; }
    // /compactlevel=
    public int CompactLevel { get; set; }
    // /tolower=
    public string LowerSrc { get; set; }
    // /flipcase=
    public string FlipSrcCase { get; set; }
    // /loopopt=
    public string RemLoops { get; set; }
    // /multipart=
    public string SplitOutput { get; set; }
    public string ArgumentList { get; set; }
    public string ExtendedArguments { get; set; }
}