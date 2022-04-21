namespace DtoWindow.Logic.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Models;

    public interface ISingleFileProcessor
  {
    Task<ClassInfo> AnalyzeAll(string selectedEntityName,
        IEnumerable<string> allProjectSources,
        Action<int> onProgressChanged);
  }
}