// #define TEST

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ColorNames.Lib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using ShellProgressBar;
using Syntax = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ColorNames.Shell
{
    internal class Program
    {
        private const int STEPS = 5;

        private const double PROGRESS_STEP = .2;

        private static ProgressBarOptions childOptions;

        private static void Main(string[] args)
        {
            const string colorNamesUrl =
                "https://raw.githubusercontent.com/meodai/color-names/master/src/colornames.csv";

            var sw = Stopwatch.StartNew();
            // int curStep = 0;

            var options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─'
            };

            childOptions = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Green,
                BackgroundColor = ConsoleColor.DarkGreen,
                ProgressCharacter = '─',
                CollapseWhenFinished = false
            };

            using (var progress = new ProgressBar(STEPS, string.Empty, options))
            {
                progress.Message = "Downloading content!";
                string csvContents;

                using (var wc = new WebClient())
                using (var child = progress.Spawn((int)wc.GetBytes(colorNamesUrl), "bytes downloaded", childOptions))
                {
                    wc.DownloadProgressChanged += (sender, e) => WcOnDownloadProgressChanged(sender, e, child);
                    csvContents = wc.DownloadString(colorNamesUrl);
                }

                var lines = csvContents.GetLines();
                progress.MaxTicks = lines.Length + STEPS - 2;

                progress.Message = "Parsing entities!";
                List<ColorEntity> entities;

                using (var child = progress.Spawn(lines.Length, "lines parsed", childOptions))
                    entities = lines.Skip(1).Select(line =>
                    {
                        var split = line.Split(',');
                        var entity = new ColorEntity { Name = split[0], Hex = split[1] };

                        child.Tick();

                        return entity;
                    })
                    .ToList();

                string folder = Path.Combine(Environment.CurrentDirectory, "Generated");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string generatedFile_ColorNames = Path.Combine(folder, "ColorNames.cs");
                string generatedFile_NamedColor = Path.Combine(folder, "NamedColor.cs");
                string generatedJSONFile = Path.Combine(folder, "ColorNames.json");

#if !TEST
                progress.Message = "Saving ColorNames class!";

                Task.Factory.StartNew(() =>
                {
                    string contents_colorNames = CreateClass_ColorNames(entities, progress);
                    File.WriteAllText(generatedFile_ColorNames, contents_colorNames);
                });

                progress.Message = "Saving NamedColor class!";

                Task.Factory.StartNew(() =>
                {
                    string contents_namedColor = CreateClass_NamedColor(entities, progress);
                    File.WriteAllText(generatedFile_NamedColor, contents_namedColor);
                });

                progress.Message = "Generating dictionary!";

                Dictionary<Lib.ColorNames, ColorEntity> dict;
                Dictionary<string, Lib.ColorNames> enumDict;
                Array enumArr = Enum.GetValues(typeof(Lib.ColorNames));

                using (var child = progress.Spawn(enumArr.Length, "enum items added to parse", childOptions))
                {
                    enumDict = enumArr
                    .Cast<Lib.ColorNames>()
                    .Select((x, i) => new { Item = x, Index = i })
                    .ToDictionary(t =>
                    {
                        child.Tick();

                        return t.Item.ToString();
                    }, t => t.Item);
                }

                using (var child = progress.Spawn(enumArr.Length, "entities parsed", childOptions))
                {
                    dict = entities
                    .Select((x, i) => new { Item = x, Index = i })
                    .ToDictionary(t =>
                    {
                        child.Tick();

                        return enumDict[t.Item.Name.SanitizeEnum()];
                    }, t => t.Item);
                }

                string jsonContents = JsonConvert.SerializeObject(dict, Formatting.Indented);

                progress.Message = "Converting json!";
                File.WriteAllText(generatedJSONFile, jsonContents);

                sw.Stop();
                progress.Message = $"Converted successfully in {sw.ElapsedMilliseconds / 1000.0:F2} s!";
#else
            // Console.WriteLine("Saving NamedColor class!");
            string contents_namedColor = CreateClass_NamedColor(entities);
            Console.WriteLine(contents_namedColor);
#endif
            }

            Console.ReadLine();
        }

        private static void WcOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e, ChildProgressBar childProgressBar)
        {
            childProgressBar.Tick((int)e.BytesReceived);
        }

        private static string CreateClass_NamedColor(List<ColorEntity> entities, ProgressBar progress)
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

            string code;
            using (var child = progress.Spawn(entities.Count, "fields added", childOptions))
            {
                // Create a string variable: (bool canceled;)
                var variableDeclarations = entities.Select((entity, index) =>
                {
                    //double prog = (double)index / entities.Count / STEPS;
                    //progress.Report(PROGRESS_STEP * lastStep + prog);

                    child.Tick();

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
                code = @namespace
                    .NormalizeWhitespace()
                    .ToFullString();
            }

            // Output new code to the console.
            return code;
        }

        private static string CreateClass_ColorNames(List<ColorEntity> entities, ProgressBar progress)
        {
            string classDeclaration;
            using (var child = progress.Spawn(entities.Count, "enum items added", childOptions))
            {
                var members = entities.Select((entity, index) =>
                {
                    //double prog = (double)index / entities.Count / STEPS;
                    //progress.Report(PROGRESS_STEP * lastStep + prog);

                    child.Tick();

                    // Console.WriteLine($"Converted {index} of {entities.Count} entities!");
                    return Syntax.EnumMemberDeclaration(identifier:
                        Syntax.Identifier(entity.Name.SanitizeEnum()));
                });

                // TODO
                // Console.WriteLine("Declaring enum!");
                var declaration = Syntax.EnumDeclaration(
                        new SyntaxList<AttributeListSyntax>(),
                        identifier: Syntax.Identifier("ColorNames"),
                        modifiers: Syntax.TokenList(Syntax.Token(SyntaxKind.PublicKeyword)),
                        baseList: null,
                        members: Syntax.SeparatedList(members))
                    .NormalizeWhitespace();

                classDeclaration = declaration.ToFullString();
            }

            return classDeclaration;
        }
    }
}