#nullable enable
namespace DtoWindow.Logic;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpers;
using Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class SingleFileProcessor : ISingleFileProcessor
{
    private List<string> _visited;

    public SingleFileProcessor()
    {
        _visited = new List<string>();
    }

    public async Task<ClassInfo> AnalyzeAll(string selectedEntityName,
        IEnumerable<string> allProjectSources,
        Action<int> onProgressChanged)
    {
        var progress = 0;
        var projectSourcesParsingTasks = allProjectSources.Select(path => Task.Run(async () =>
        {
            var sourceCode = await ProjectHelpers.ReadFile(path);
            onProgressChanged?.Invoke(++progress);
            var tree = CSharpSyntaxTree.ParseText(sourceCode, null, path);
            onProgressChanged?.Invoke(++progress);
            return tree;
        }));

        var projectSyntaxTrees = await Task.WhenAll(projectSourcesParsingTasks);

        var classes = projectSyntaxTrees
            .Select(tree => tree.GetRoot())
            .SelectMany(node => node.DescendantNodes().OfType<ClassDeclarationSyntax>())
            .ToList();

        var compilation = CSharpCompilation.Create(null)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(projectSyntaxTrees);

        var semanticModels = projectSyntaxTrees.Select(tree => compilation.GetSemanticModel(tree)).ToList();

        var targetTypeSymbol = compilation.GetTypeByMetadataName("CleanArchitecture.Domain.Common.AuditableEntity");
        var entities = GetClassesImplementingTypeSymbol(classes, semanticModels, targetTypeSymbol);

        return GetClassInfo(selectedEntityName, entities, semanticModels, $"{selectedEntityName}{Constants.DtoSuffix}", selectedEntityName);
    }

    private ClassInfo? GetClassInfo(string selectedEntityName, List<ClassDeclarationSyntax> classes,
        List<SemanticModel> semanticModels, string className, string featureName)
    {
        _visited.Add(selectedEntityName);
        var selected = classes.FirstOrDefault(syntax => syntax.Identifier.ValueText == selectedEntityName);

        string namespaceName = null!;

        if (selected.Parent is FileScopedNamespaceDeclarationSyntax namespaceDeclaration)
        {
            namespaceName = namespaceDeclaration.Name.ToString();
        }

        return selected == null ? null : new ClassInfo
        {
            Name = className,
            EntityName = selectedEntityName,
            OriginalNamespace = namespaceName,
            FeatureName = featureName,
            Properties = selected.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Select(prop => GetPropertyInfo(prop, semanticModels, classes, featureName))
                .ToList()
        };
    }

    private PropertyInfo GetPropertyInfo(PropertyDeclarationSyntax node, List<SemanticModel> semanticModel,
        List<ClassDeclarationSyntax> classes, string featureName)
    {
        var isEnumerable = false;
        var isGeneric = false;
        var typeInfo = GetTypeInfo(semanticModel, node.Type);
        var isEntity = false;
        var originalTypeName = string.Empty;

        if (!typeInfo.HasValue)
        {
            return null;
        }

        var typeInfoValue = typeInfo.Value;

        var propertyName = node.Identifier.ValueText;
        var hasSetter = node.AccessorList?.Accessors.FirstOrDefault(
            a => a.IsKind(SyntaxKind.SetAccessorDeclaration) &&
                 !a.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) != null;

        var typeName = "";

        if (typeInfoValue.ConvertedType is INamedTypeSymbol namedType)
        {
            var simpleTypeKinds = new[] { TypeKind.Enum, TypeKind.Struct, TypeKind.Interface };
            var enumerableType = namedType;

            var isNullable = namedType.Name == "Nullable";
            if (isNullable && namedType.TypeArguments.FirstOrDefault() is INamedTypeSymbol)
            {
                namedType = namedType.TypeArguments.First() as INamedTypeSymbol;
            }
            
            isGeneric = namedType.ConstructedFrom.IsGenericType;
            isEnumerable = Constants.ListTypeNames.Any(listType => listType == namedType.Name);

            if (isEnumerable)
            {
                namedType = namedType.TypeArguments.First() as INamedTypeSymbol;
            }

            var isSimple = namedType.IsValueType ||
                           simpleTypeKinds.Any(t => namedType.TypeKind == t) ||
                           Constants.SimpleTypeNames.Contains(namedType.Name) ||
                           namedType.Name.Equals("object", StringComparison.CurrentCultureIgnoreCase) ||
                           namedType.Name.Equals("string", StringComparison.CurrentCultureIgnoreCase);

            if (!isSimple)
            {
                originalTypeName = namedType.Name;
                typeName = $"{namedType.Name}{Constants.DtoSuffix}{(isNullable ? "?" : "")}";
            }

            if (isEnumerable)
            {
                typeName = $"{enumerableType.Name}<{typeName}>";
            }
        }
        else if (typeInfoValue.ConvertedType.Kind == SymbolKind.ArrayType && typeInfoValue.ConvertedType is IArrayTypeSymbol arrayType)
        {
            isEnumerable = true;
            typeName = arrayType.ElementType.Name;

            if (Constants.SimpleTypeNames.All(s => typeName != s))
            {
                originalTypeName = typeName;
                typeName = $"{typeName}{Constants.DtoSuffix}";
                isEntity = true;
            }

            typeName = $"{typeName}[]";
        }

        isEntity = classes.Select(syntax => syntax.Identifier.ValueText).Contains(originalTypeName);

        var classInfo = isEntity && !_visited.Contains(originalTypeName) ? GetClassInfo(originalTypeName, classes, semanticModel, $"{originalTypeName}{Constants.DtoSuffix}", featureName) : null;

        return new PropertyInfo
        {
            Name = propertyName,
            Type = node.Type,
            TypeName = typeName,
            IsEnumerableType = isEnumerable,
            IsGenericType = isGeneric,
            HasSetter = hasSetter,
            ClassInfo = classInfo
        };
    }

    private static List<ClassDeclarationSyntax> GetClassesImplementingTypeSymbol(List<ClassDeclarationSyntax> classes, List<SemanticModel> semanticModels,
        INamedTypeSymbol? targetTypeSymbol)
    {
        var entities =
            classes
                .Where(syntax => syntax.BaseList != null)
                .Where(classDeclarationSyntax => classDeclarationSyntax.BaseList.Types
                    .Select(baseType => GetSymbolInfo(semanticModels, baseType.Type))
                    .Where(info => info.HasValue)
                    .Select(symbolInfo => (INamedTypeSymbol)symbolInfo.Value.Symbol?.OriginalDefinition)
                    .Any(originalSymbolDefinition =>
                        SymbolEqualityComparer.Default.Equals(targetTypeSymbol, originalSymbolDefinition)))
                .ToList();
        return entities;
    }

    private static TypeInfo? GetTypeInfo(List<SemanticModel> semanticModels, ExpressionSyntax syntax)
    {
        TypeInfo? symbolInfo = null;

        foreach (var semanticModel in semanticModels)
        {
            try
            {
                symbolInfo = semanticModel.GetTypeInfo(syntax);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }
        }

        return symbolInfo;
    }
    private static SymbolInfo? GetSymbolInfo(List<SemanticModel> semanticModels, ExpressionSyntax syntax)
    {
        SymbolInfo? symbolInfo = null;

        foreach (var semanticModel in semanticModels)
        {
            try
            {
                symbolInfo = semanticModel.GetSymbolInfo(syntax);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }
        }

        return symbolInfo;
    }
}