﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using SOURCE.Builders.Abstract;
using SOURCE.Helpers;
using SOURCE.Models;
using SOURCE.Workers;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SOURCE.Builders;

// ReSharper disable once InconsistentNaming
// ReSharper disable once UnusedType.Global
public class IUnitOfWorkBuilder : ISourceBuilder
{
    private readonly string ProjectPath;
    private readonly string RootNamespace = "DAL.EntityFramework.UnitOfWork";
    private readonly string DefaultDocumentBody = @"using DAL.EntityFramework.Abstract;

namespace DAL.EntityFramework.UnitOfWork;
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    public Task CommitAsync();    
}";

    public IUnitOfWorkBuilder()
    {
        ProjectPath = Path.Combine(
            Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.ToString(),
            @"DAL\DAL.csproj");
    }

    public void BuildSourceFile(List<Entity> entities)
    {
        var result = GenerateSource(entities.Where(w =>
                w.Options.BuildUnitOfWork
                && w.Options.BuildRepository)
            .ToList()).Result;
    }

    private async Task<string> GenerateSource(List<Entity> entities)
    {
        using MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (source, args) =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                Console.WriteLine(args.Diagnostic.Message);
            }
        };

        Project project = await workspace.OpenProjectAsync(ProjectPath);
        Document? document = project.Documents
            .Where(w => w.Name == "IUnitOfWork.cs")
            .FirstOrDefault();

        if (document != null && !entities.Any())
        {
            return string.Empty;
        }

        if (document is null)
        {
            document = project
                .AddDocument("IUnitOfWork.cs", DefaultDocumentBody, ["EntityFramework", "UnitOfWork"]);
        }

        SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
        SyntaxNode rootNode = await syntaxTree.GetRootAsync();
        InterfaceDeclarationSyntax interfaceDeclaration = rootNode
            .DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .FirstOrDefault();

        foreach (Entity entity in entities)
        {
            var existEntity = interfaceDeclaration
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Any(w => w.Identifier.Text == $"{entity.Name}Repository");
            if (existEntity) continue;
            interfaceDeclaration = interfaceDeclaration
                .AddMembers(GetProperty(interfaceDeclaration, entity));

        }

        rootNode = rootNode.ReplaceNode(
            rootNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault(),
            interfaceDeclaration);


        Document newDocument = document.WithSyntaxRoot(rootNode.NormalizeWhitespace());

        workspace.TryApplyChanges(newDocument.Project.Solution);


        return string.Empty;
    }

    private static PropertyDeclarationSyntax GetProperty(InterfaceDeclarationSyntax? classDeclaration, Entity entity)
    {
        var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName($"I{entity.Name}Repository"), $"{entity.Name}Repository")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        )
                        .WithTrailingTrivia(SyntaxFactory.Space);
        return property.NormalizeWhitespace();
    }

    public string BuildSourceText(Entity? entity, List<Entity>? entities)
    {
        var properties = new StringBuilder();
        entities?.ForEach(e =>
            properties.AppendLine($"    public I{e.Name}Repository {e.Name}Repository {{ get; set; }}"));


        var text = $$"""
                     using DAL.EntityFramework.Abstract;

                     namespace DAL.EntityFramework.UnitOfWork;

                     public interface IUnitOfWork : IAsyncDisposable, IDisposable
                     {
                     {{properties}}
                         public Task CommitAsync();
                     }

                     """;

        return text;
    }
}