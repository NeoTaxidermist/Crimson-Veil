using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// Pravidlo projektu: každý commit má vlastní verzi a sekci v CHANGELOG.md.
    /// Hlídají to git hooky v .githooks; tady se hlídá, že je CHANGELOG v pořádku a hooky jsou na místě.
    [Category("Tools")]
    public class ChangelogTests
    {
        static readonly Regex Heading = new Regex(@"^## \[(.+?)\](.*)$");
        static readonly Regex Version = new Regex(@"^## \[(\d+)\.(\d+)\.(\d+)\] – \d{4}-\d{2}-\d{2}$");

        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        struct Section { public string head; public List<string> body; }

        static List<Section> Sections()
        {
            var list = new List<Section>();
            foreach (var raw in File.ReadAllLines(Path.Combine(Root, "CHANGELOG.md")))
            {
                string line = raw.TrimEnd();
                if (Heading.IsMatch(line)) list.Add(new Section { head = line, body = new List<string>() });
                else if (list.Count > 0 && line.Length > 0) list[list.Count - 1].body.Add(line);
            }
            return list;
        }

        [Test]
        public void Unreleased_IsFirstAndEmpty_EverythingHasAVersion()
        {
            var s = Sections();
            Assert.AreEqual("## [Unreleased]", s[0].head);
            CollectionAssert.IsEmpty(s[0].body, "pod [Unreleased] nesmí nic být: každá změna patří pod číslo verze");
        }

        [Test]
        public void Versions_AreWellFormed_UniqueAndDescending_WithContent()
        {
            var s = Sections();
            Version prev = null;
            for (int i = 1; i < s.Count; i++)
            {
                var m = Version.Match(s[i].head);
                Assert.IsTrue(m.Success, $"nadpis '{s[i].head}' není ve tvaru '## [X.Y.Z] – RRRR-MM-DD'");
                var v = new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
                if (prev != null) Assert.Less(v, prev, $"verze {v} má být pod vyšší verzí {prev} a jen jednou");
                CollectionAssert.IsNotEmpty(s[i].body, $"sekce {v} je prázdná");
                prev = v;
            }
            Assert.Greater(s.Count, 1);
        }

        [Test]
        public void GitHooks_GuardEveryCommit()
        {
            foreach (var hook in new[] { "pre-commit", "post-commit", "pre-push" })
            {
                string path = Path.Combine(Root, ".githooks", hook);
                Assert.IsTrue(File.Exists(path), $"chybí hook {hook}");
                string text = File.ReadAllText(path);
                StringAssert.StartsWith("#!/bin/sh", text);
                Assert.IsFalse(text.Contains("\r"), $"{hook} musí mít konce řádků LF, jinak ho git nespustí");
            }
            StringAssert.Contains("CHANGELOG.md", File.ReadAllText(Path.Combine(Root, ".githooks", "pre-commit")));
            StringAssert.Contains("git tag -a", File.ReadAllText(Path.Combine(Root, ".githooks", "post-commit")));
        }
    }
}
