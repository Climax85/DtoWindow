namespace DtoWindow.Logic.Models;

using System.Collections.Generic;
using System.Linq;

public class ClassInfo : TreeViewItem
{
    public string OriginalNamespace { get; set; }
    public string EntityName { get; set; }
    public string FeatureName { get; set; }

    public ICollection<PropertyInfo> Properties { get; set; }

    public bool IsEnabled { get; set; } = true;
    public override string DisplayTypeName => Name;
    public new ICollection<TreeViewItem> Children => Properties.Cast<TreeViewItem>().ToList();
}

public abstract class TreeViewItem
{
    public string Name { get; set; }
    public abstract string DisplayTypeName { get; }
    public bool EnableForCreate { get; set; }
    public bool EnableForList { get; set; }
    public bool EnableForUpdate { get; set; } = true;
    public ICollection<TreeViewItem> Children { get; }
}