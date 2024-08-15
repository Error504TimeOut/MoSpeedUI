using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace MoSpeedUI;

public class CompileConfig
{
    public List<IStorageFile> Files { get; set; } = new();
}