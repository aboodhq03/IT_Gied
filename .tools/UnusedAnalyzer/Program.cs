using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FindSymbols;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var root = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var projectPath = args.Length > 1 ? args[1] : Path.Combine(root, "IT_Gied.csproj");
        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Project not found: {projectPath}");
            return 1;
        }

        var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
        if (instances.Length == 0)
        {
            Console.Error.WriteLine("No MSBuild instances found.");
            return 1;
        }
        var instance = instances[0];
        MSBuildLocator.RegisterInstance(instance);

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (s, e) => Console.Error.WriteLine($"Workspace: {e.Diagnostic.Message}");

        Console.WriteLine($"Loading project {projectPath}");
        var project = await workspace.OpenProjectAsync(projectPath);
        var compilation = await project.GetCompilationAsync();
        if (compilation == null)
        {
            Console.Error.WriteLine("Failed to compile project.");
            return 1;
        }

        var solution = workspace.CurrentSolution;
        var unusedMembers = new List<string>();
        foreach (var document in project.Documents)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            if (semanticModel == null) continue;
            var rootNode = await document.GetSyntaxRootAsync();
            if (rootNode == null) continue;
            var declaredSymbols = rootNode.DescendantNodes()
                .Select(node => semanticModel.GetDeclaredSymbol(node))
                .Where(symbol => symbol != null)
                .Distinct(SymbolEqualityComparer.Default)
                .ToList();

            foreach (var symbol in declaredSymbols)
            {
                if (symbol.Kind == SymbolKind.Namespace) continue;
                if (symbol.DeclaredAccessibility == Accessibility.Private || symbol.DeclaredAccessibility == Accessibility.NotApplicable || symbol.DeclaredAccessibility == Accessibility.Internal)
                {
                    if (symbol is INamespaceOrTypeSymbol nsType && nsType.TypeKind == TypeKind.Class)
                    {
                        // skip controller classes and MVC related classes.
                        if (IsControllerClass(nsType)) continue;
                    }
                    if (symbol.IsImplicitlyDeclared) continue;
                    if (symbol is IMethodSymbol method && method.MethodKind == MethodKind.Constructor) continue;
                    if (symbol is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Delegate) continue;
                    if (symbol is IPropertySymbol property && property.IsOverride) continue;
                    if (symbol is IMethodSymbol m && m.IsOverride) continue;

                    var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
                    var referenceCount = references.SelectMany(r => r.Locations).Count();
                    if (referenceCount == 0)
                    {
                        unusedMembers.Add($"{symbol.Kind} {symbol.ToDisplayString()} in {document.FilePath}");
                    }
                }
            }
        }

        Console.WriteLine($"Found {unusedMembers.Count} unused private/internal declarations.");
        foreach (var item in unusedMembers.OrderBy(x => x))
            Console.WriteLine(item);

        return 0;
    }

    static bool IsControllerClass(INamespaceOrTypeSymbol symbol)
    {
        var current = symbol;
        while (current != null)
        {
            if (current.Name == "Controller" || current.Name == "ControllerBase") return true;
            current = current.BaseType;
        }
        return false;
    }
}
