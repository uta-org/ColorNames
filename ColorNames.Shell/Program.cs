// #define TEST

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using ColorNames.Lib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Syntax = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ColorNames.Shell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string colorNamesUrl =
                "https://raw.githubusercontent.com/meodai/color-names/master/src/colornames.csv";

            var sw = Stopwatch.StartNew();

            string csvContents;
            using (var wc = new WebClient())
                csvContents = wc.DownloadString(colorNamesUrl);

            List<ColorEntity> entities = csvContents.GetLines().Skip(1).Select(line =>
                {
                    var split = line.Split(',');
                    return new ColorEntity() { Name = split[0], Hex = split[1] };
                })
                .ToList();

            string folder = Path.Combine(Environment.CurrentDirectory, "Generated");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string generatedFile_ColorNames = Path.Combine(folder, "ColorNames.cs");
            string generatedFile_NamedColor = Path.Combine(folder, "NamedColor.cs");
            string generatedJSONFile = Path.Combine(folder, "ColorNames.json");

#if !TEST
            Console.WriteLine("Saving ColorNames class!");
            string contents_colorNames = CreateClass_ColorNames(entities);
            File.WriteAllText(generatedFile_ColorNames, contents_colorNames);

            Console.WriteLine("Saving NamedColor class!");
            string contents_namedColor = CreateClass_NamedColor(entities);
            File.WriteAllText(generatedFile_NamedColor, contents_namedColor);

            Console.WriteLine("Generating dictionary!");
            var dict = entities.ToDictionary(t => (Lib.ColorNames)Enum.Parse(typeof(Lib.ColorNames), t.Name.SanitizeEnum()),
                t => t);
            string jsonContents = JsonConvert.SerializeObject(dict, Formatting.Indented);

            Console.WriteLine("Converting json!");
            File.WriteAllText(generatedJSONFile, jsonContents);

            sw.Stop();
            Console.WriteLine($"Converted successfully in {sw.ElapsedMilliseconds / 1000.0:F2} s!");
#else
            // Console.WriteLine("Saving NamedColor class!");
            string contents_namedColor = CreateClass_NamedColor(entities);
            Console.WriteLine(contents_namedColor);
#endif
            Console.ReadLine();
        }

        private static string CreateClass_NamedColor(List<ColorEntity> entities)
        {
            // Create a namespace: (namespace UnityEngine.Utils)
            var @namespace = Syntax.NamespaceDeclaration(Syntax.ParseName("UnityEngine.Utils")).NormalizeWhitespace();

            //// Add System using statement: (using System)
            //@namespace = @namespace.AddUsings(Syntax.UsingDirective(Syntax.ParseName("System")));

            //  Create a class: (class Order)
            var classDeclaration = Syntax.ClassDeclaration("NamedColor");

            // Add the public modifier: (public class Order)
            classDeclaration = classDeclaration.AddModifiers(
                Syntax.Token(SyntaxKind.PublicKeyword),
                Syntax.Token(SyntaxKind.PartialKeyword));

            // Create a string variable: (bool canceled;)
            var variableDeclarations = entities.Select(entity =>
            {
                string varName = entity.Name.SanitizeEnum();
                return Syntax.VariableDeclaration(Syntax.ParseTypeName("NamedColor"))
                    // .AddVariables(Syntax.VariableDeclarator("canceled"))
                    .WithVariables(Syntax.SingletonSeparatedList(
                        Syntax.VariableDeclarator(
                                Syntax.Identifier(varName))
                            .WithInitializer(
                                Syntax.EqualsValueClause(
                                    Syntax.ParseExpression($"new NamedColor(ColorNames.{varName}, " +
                                                           $"{entity.Hex.ToUpperInvariant().Replace("#", "0x")})")
                                ))));
            });

            // Create a field declaration: (private bool canceled;)
            var fieldDeclarations = variableDeclarations.Select(variableDeclaration => Syntax.FieldDeclaration(variableDeclaration)
                .AddModifiers(
                    Syntax.Token(SyntaxKind.PrivateKeyword),
                    Syntax.Token(SyntaxKind.StaticKeyword)));

            // Add the field, the property and method to the class.
            classDeclaration = classDeclaration.AddMembers(fieldDeclarations.ToArray());

            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = @namespace
                .NormalizeWhitespace()
                .ToFullString();

            // Output new code to the console.
            return code;
        }

        private static string CreateClass_ColorNames(List<ColorEntity> entities)
        {
            var members = entities.Select((entity, index) =>
            {
                // Console.WriteLine($"Converted {index} of {entities.Count} entities!");
                return Syntax.EnumMemberDeclaration(identifier:
                    Syntax.Identifier(entity.Name.SanitizeEnum()));
            });

            Console.WriteLine("Declaring enum!");
            var declaration = Syntax.EnumDeclaration(
                    new SyntaxList<AttributeListSyntax>(),
                    identifier: Syntax.Identifier("ColorNames"),
                    modifiers: Syntax.TokenList(Syntax.Token(SyntaxKind.PublicKeyword)),
                    baseList: null,
                    members: Syntax.SeparatedList(members))
                .NormalizeWhitespace();

            Console.WriteLine("Converting to string!");
            string classDeclaration = declaration.ToFullString();

            return classDeclaration;
        }
    }
}