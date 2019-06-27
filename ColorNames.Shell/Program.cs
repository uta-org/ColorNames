using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using ColorNames.Lib;
using CsvHelper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using UnidecodeSharpCore;
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

            string generatedFile = Path.Combine(folder, "ColorNames.cs");
            string generatedJSONFile = Path.Combine(folder, "ColorNames.json");

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
            File.WriteAllText(generatedFile, declaration.ToFullString());

            Console.WriteLine("Converting json!");
            File.WriteAllText(generatedJSONFile, JsonConvert.SerializeObject(entities, Formatting.Indented));

            sw.Stop();
            Console.WriteLine($"Converted successfully in {sw.ElapsedMilliseconds / 1000.0:F2} s!");
            Console.ReadLine();
        }
    }
}