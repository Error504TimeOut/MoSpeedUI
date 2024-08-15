using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace MoSpeedUI;

public class FilePathCompare : IEqualityComparer<IStorageFile>
{
    public bool Equals(IStorageFile x, IStorageFile y)
    {
        return x.Path == y.Path;
    }

    public int GetHashCode(IStorageFile obj)
    {
        return obj.Path.GetHashCode();
    }
}