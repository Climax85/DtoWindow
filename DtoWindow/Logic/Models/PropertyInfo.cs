namespace DtoWindow.Logic.Models;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class PropertyInfo : TreeViewItem
{
    public TypeSyntax Type { get; set; }

    public string TypeName { get; set; }

    public bool HasSetter { get; set; }

    public bool IsEnumerableType { get; set; }

    public bool IsGenericType { get; set; }

    public bool IsEnabled { get; set; } = true;

    public ClassInfo ClassInfo { get; set; }

    public override string DisplayTypeName => TypeName == string.Empty ?  Type.ToString() : TypeName;
    public new ICollection<TreeViewItem> Children => ClassInfo?.Properties.Cast<TreeViewItem>().ToList();
}