namespace DtoWindow.ToolWindows;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Helpers;
using Logic;
using Logic.Models;
using TreeViewItem = Logic.Models.TreeViewItem;

public partial class MyToolWindowControl : UserControl
{
    public MyToolWindowControl()
    {
        InitializeComponent();
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var solutionItems = ProjectHelpers.GetEntities("Domain");
        var singleFileProcessor = new SingleFileProcessor();
        var projectSources = solutionItems.Where(item => item.FullPath != null).Select(item => item.FullPath).ToList();
        var classInfo = await singleFileProcessor.AnalyzeAll("TodoList", projectSources, Console.WriteLine);

        var treeViewItems = new List<TreeViewItem> { classInfo };
        trvListClasses.ItemsSource = treeViewItems;
        trvCreateClasses.ItemsSource = treeViewItems;
        trvUpdateClasses.ItemsSource = treeViewItems;

        LoadButton.Visibility = Visibility.Hidden;
        GenerateButton.Visibility = Visibility.Visible;
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var projectPath = ProjectHelpers.GetProjectPath("Application.Shared");

        var codeGenerator = new CodeGenerator();

        foreach (var updateItem in trvUpdateClasses.Items.SourceCollection)
        {
            if (updateItem is ClassInfo classInfo)
            {
                var sourcecodes = codeGenerator.GenerateSourcecodes(classInfo);

                foreach (var sourcecode in sourcecodes)
                {
                    var dtoFilePath =
                        Path.Combine(projectPath, $"UseCases\\{classInfo.FeatureName}\\Queries\\Get{classInfo.FeatureName}\\{sourcecode.Key}.cs");
                    var saveCompleted = false;
                    while (!saveCompleted)
                        try
                        {
                            File.WriteAllText(dtoFilePath, sourcecode.Value);
                            saveCompleted = true;
                        }
                        catch (DirectoryNotFoundException)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(dtoFilePath));
                        }
                        catch (Exception ex)
                        {
                            saveCompleted = true;
                        }
                }
            }
        }

        LoadButton.Visibility = Visibility.Visible;
        GenerateButton.Visibility = Visibility.Hidden;
    }
}