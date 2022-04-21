namespace DtoWindow.Logic
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Interfaces;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Models;

    public class CodeGenerator : ICodeGenerator
    {
        public IDictionary<string, string> GenerateSourcecodes(ClassInfo classInfo)
        {
            var sourceCodesCollection = new Dictionary<string, string>();
            GenerateSourcecode(classInfo, sourceCodesCollection);
            GenerateQuerySourcecode(classInfo, sourceCodesCollection);
            return sourceCodesCollection;
        }

        public void GenerateQuerySourcecode(ClassInfo classInfo, IDictionary<string, string> sourceCodesCollection)
        {
            var featureNamespaceName = $"Application.Shared.UseCases.{classInfo.FeatureName}.Queries.Get{classInfo.FeatureName}";
            var dtoNamespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{featureNamespaceName}"));

            var identifier = $"Get{classInfo.FeatureName}Query";

            var dtoDeclaration = SyntaxFactory.ClassDeclaration(identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IRequest<{classInfo.Name}>")));

            var prop = classInfo.Properties.Where(p => p.EnableForUpdate).FirstOrDefault(p => p.Name == "Id");

            var propertyDeclaration = createPropertyDeclaration(prop);
            dtoDeclaration = dtoDeclaration.AddMembers(propertyDeclaration);

            dtoNamespace = dtoNamespace.AddMembers(dtoDeclaration)
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Constants.Using.MediatR)));

            var dtoFile = SyntaxFactory.CompilationUnit()
                .AddMembers(dtoNamespace);

            sourceCodesCollection.Add(identifier, cleanupCodeFormatting(dtoFile.NormalizeWhitespace().ToFullString()));
        }

        public void GenerateSourcecode(ClassInfo classInfo, IDictionary<string, string> sourceCodesCollection)
        {
            var featureNamespaceName = $"Application.Shared.UseCases.{classInfo.FeatureName}.Queries.Get{classInfo.FeatureName}";
            var dtoNamespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{featureNamespaceName}"));

            var dtoDeclaration = SyntaxFactory.ClassDeclaration(classInfo.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IMapFrom<{classInfo.OriginalNamespace}.{classInfo.EntityName}>")));

            foreach (var prop in classInfo.Properties.Where(p => p.EnableForUpdate))
            {
                var propertyDeclaration = createPropertyDeclaration(prop);
                dtoDeclaration = dtoDeclaration.AddMembers(propertyDeclaration);
                if (prop.ClassInfo != null)
                {
                    GenerateSourcecode(prop.ClassInfo, sourceCodesCollection);
                }
            }

            dtoNamespace = dtoNamespace.AddMembers(dtoDeclaration)
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Constants.Using.System)))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Constants.Using.IMapFrom)));

            dtoNamespace = includeUsings(dtoNamespace, classInfo);

            var dtoFile = SyntaxFactory.CompilationUnit()
                .AddMembers(dtoNamespace);

            sourceCodesCollection.Add(classInfo.Name, cleanupCodeFormatting(dtoFile.NormalizeWhitespace().ToFullString()));
        }

        private string cleanupCodeFormatting(string code)
        {
            var propertyRegex = new Regex(@"\s*{\r\n\s*get;\r\n\s*set;\r\n\s*}");
            var privatePropertyRegEx = new Regex(@"\s*{\r\n\s*get;\r\n\s*private\sset;\r\n\s*}");
            return privatePropertyRegEx.Replace(propertyRegex.Replace(code, " { get; set; }"), " { get; private set; }")
                .Replace(" ;", ";")
                .Replace(" ,", ",")
                .Replace("}; \r\n\r\n", "}; \r\n")
                .Replace("};\r\n\r\n", "};\r\n");
        }

        private PropertyDeclarationSyntax createPropertyDeclaration(PropertyInfo prop)
        {
            var setAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (!prop.HasSetter)
                setAccessor = setAccessor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            var propertyDeclaration = SyntaxFactory
                .PropertyDeclaration(
                    string.IsNullOrWhiteSpace(prop.TypeName) ? prop.Type : SyntaxFactory.ParseTypeName(prop.TypeName),
                    prop.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    setAccessor);

            return propertyDeclaration;
        }

        private NamespaceDeclarationSyntax includeUsings(NamespaceDeclarationSyntax dtoNamespace, ClassInfo classInfo)
        {
            var result = dtoNamespace;

            if (classInfo.Properties.Any(p => p.IsGenericType && p.IsEnabled))
                result = result.AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Constants.Using.SystemCollectionsGeneric)));
            if (classInfo.Properties.Any(p => p.IsEnumerableType && p.IsEnabled))
                result = result.AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Constants.Using.SystemLinq)));

            return result;
        }
    }
}