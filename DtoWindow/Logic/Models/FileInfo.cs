﻿namespace DtoWindow.Logic.Models;

using System.Collections.Generic;

public class FileInfo
{
    public string ModelNamespace { get; set; }

    public string Namespace { get; set; }

    public List<ClassInfo> Classes { get; set; }
}