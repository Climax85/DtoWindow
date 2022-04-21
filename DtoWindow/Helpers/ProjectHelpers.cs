namespace DtoWindow.Helpers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class ProjectHelpers
{
    public static string GetProjectPath(string projectName)
    {
        var projects = VS.Solutions.GetAllProjectsAsync().GetAwaiter().GetResult();
        var project = projects.FirstOrDefault(project => project.Name == projectName);
        var projectPath = project.FullPath.Remove(project.FullPath.LastIndexOf('\\'));
        return projectPath;
    }

    public static SyntaxTree ParseToSyntaxTree(string code)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: new[] { "RELEASE" });
        //There are many other configuration items, the simplest of which is enough
        return CSharpSyntaxTree.ParseText(code, parseOptions);
    }
    public static ImmutableList<SolutionItem> GetEntities(string projectName)
    {
        var projects = VS.Solutions.GetAllProjectsAsync().GetAwaiter().GetResult();
        var project = projects.FirstOrDefault(project1 => project1.Name == projectName);

        return project?.Children
            .SelectMany(item => item.GetSolutionItems())
            .ToImmutableList();
    }

    private static IEnumerable<SolutionItem> GetSolutionItems(this SolutionItem item)
    {
        if (item.Children.Any()) return item.Children.SelectMany(solutionItem => solutionItem.GetSolutionItems());

        return new[] { item };
    }

    public static async Task ImplementsInterfaceAsync(ImmutableList<SolutionItem> solutionItems)
    {
        try
        {
            foreach (var item in solutionItems.Where(item => item.FullPath != null)) await GetSyntaxTreeAsync(item);
        }
        catch (Exception e)
        {
            await e.LogAsync();
            Console.WriteLine(e);
        }
    }

    private static async Task GetSyntaxTreeAsync(SolutionItem item)
    {
        var code = await ReadFile(item.FullPath);
        var syntaxTree = ParseToSyntaxTree(code);
        await VS.MessageBox.ShowAsync(syntaxTree.ToString());
    }

    public static Task<string> ReadFile(string path)
    {
        return Task.Run(() =>
        {
            using var reader = new StreamReader(path);

            return reader.ReadToEnd();
        });
    }
}