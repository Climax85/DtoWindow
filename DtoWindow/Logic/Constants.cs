namespace DtoWindow.Logic
{
    using System.Collections.Generic;

    public class Constants
  {
    public const string DtoSuffix = "Dto";

    public class Attribute
    {
      public const string JsonProperty = "JsonProperty";

      public const string DataMember = "DataMember";

      public const string DataContract = "DataContract";
    }

    public class Using
    {
      public const string NewtonsoftJson = "Newtonsoft.Json";

      public const string System = "System";

      public const string SystemCollectionsGeneric = "System.Collections.Generic";

      public const string SystemLinq = "System.Linq";

      public const string SystemRuntimeSerialization = "System.Runtime.Serialization";

      public const string IMapFrom = "global::Application.Shared.Common.Mappings";

      public const string MediatR = "MediatR";
    }

    public static List<string> SimpleTypeNames => new List<string>
    {
      "Boolean",
      "Byte",
      "SByte",
      "Char",
      "Decimal",
      "Double",
      "Single",
      "Int32",
      "UInt32",
      "Int64",
      "UInt64",
      "Object",
      "Int16",
      "UInt16",
      "String",

      "bool",
      "byte",
      "sbyte",
      "char",
      "decimal",
      "double",
      "float",
      "int",
      "uint",
      "long",
      "ulong",
      "object",
      "short",
      "ushort",
      "string",

      "DateTime"
    };

    public static List<string> ListTypeNames => new List<string>
    {
      "IEnumerable",
      "ICollection",
      "IList",
      "IReadOnlyCollection",
      "IReadOnlyList",
      "Enumerable",
      "Collection",
      "List"
    };
  }
}
