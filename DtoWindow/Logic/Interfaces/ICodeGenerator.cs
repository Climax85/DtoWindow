namespace DtoWindow.Logic.Interfaces;

using System.Collections.Generic;
using Models;

public interface ICodeGenerator
{
    IDictionary<string, string> GenerateSourcecodes(ClassInfo classInfo);
}