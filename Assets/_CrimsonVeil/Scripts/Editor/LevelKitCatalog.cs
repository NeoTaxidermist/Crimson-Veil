using System;
using System.Collections.Generic;
using System.IO;
using CrimsonVeil.World;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.EditorTools
{
    /// <summary>Everything the Level Editor can place, with Czech labels, and the prefabs behind them.</summary>
    public static class LevelKitCatalog
    {
        public const string PrefabDir = LevelKitMaterials.Root + "/LevelKit/Prefabs";

        public enum Category { Teren, Hra, Dekorace, Scena }

        public sealed class Entry
        {
            public string id;
            public string label;
            public string hint;
            public Category category;
            /// <summary>Decorations can be put on any depth layer; gameplay pieces always live on the playfield.</summary>
            public bool layered;
            public Vector2 previewSize;
            public Func<GameObject> build;
            public string PrefabPath => $"{PrefabDir}/{id}.prefab";
        }

        public static readonly List<Entry> Entries = new List<Entry>
        {
            // ---- terrain
            new Entry { id = "Platform", label = "Plošina", hint = "Pochozí skála s trávou. Táhni úchyty pro šířku a výšku.", category = Category.Teren, previewSize = new Vector2(6f, 2f),
                build = () => LevelKitFactory.Terrain(Vector3.zero, 6f, 2f) },
            new Entry { id = "Ledge", label = "Římsa", hint = "Tenká visící římsa – na chytání hran.", category = Category.Teren, previewSize = new Vector2(3f, 0.8f),
                build = () => LevelKitFactory.Terrain(Vector3.zero, 3f, 0.8f, TerrainStyle.Rock, 3) },
            new Entry { id = "Wall", label = "Zeď", hint = "Vysoká zeď na klouzání a odrazy od zdi.", category = Category.Teren, previewSize = new Vector2(1.4f, 8f),
                build = () => LevelKitFactory.Terrain(Vector3.zero, 1.4f, 8f, TerrainStyle.Ruin, 5, false) },
            new Entry { id = "Cliff", label = "Útes", hint = "Masivní útes / podlaha úrovně.", category = Category.Teren, previewSize = new Vector2(12f, 12f),
                build = () => LevelKitFactory.Terrain(Vector3.zero, 12f, 12f, TerrainStyle.Rock, 7) },

            // ---- gameplay
            new Entry { id = "PlayerSpawn", label = "Start hráče", hint = "Kde hrdina začíná (a první respawn).", category = Category.Hra, previewSize = new Vector2(0.6f, 1.8f),
                build = () => LevelKitFactory.Spawn(Vector3.zero) },
            new Entry { id = "Player", label = "Hrdina", hint = "Hrdina se šálou. V úrovni má být právě jeden.", category = Category.Hra, previewSize = new Vector2(0.6f, 1.8f),
                build = () => LevelKitFactory.Player(Vector3.zero) },
            new Entry { id = "Checkpoint", label = "Checkpoint", hint = "Po průchodu se tady hrdina oživí.", category = Category.Hra, previewSize = new Vector2(2.4f, 3f),
                build = () => LevelKitFactory.Checkpoint(Vector3.zero) },
            new Entry { id = "Wraith", label = "Přízrak", hint = "Nepřítel: hlídkuje, pronásleduje, telegrafuje útok.", category = Category.Hra, previewSize = new Vector2(0.8f, 2f),
                build = () => LevelKitFactory.Wraith(Vector3.zero) },
            new Entry { id = "ShieldMonk", label = "Mnich se štítem", hint = "Zepředu nezranitelný. Obejdi ho, nebo mu bičem vytrhni štít.", category = Category.Hra, previewSize = new Vector2(1.2f, 2f),
                build = () => LevelKitFactory.ShieldMonk(Vector3.zero) },
            new Entry { id = "RootCrawler", label = "Kořenolez", hint = "Visí ze stropu (polož ho pod převis) a seskočí. Kopí ho přišpendlí.", category = Category.Hra, previewSize = new Vector2(1f, 1f),
                build = () => LevelKitFactory.RootCrawler(Vector3.zero) },
            new Entry { id = "HangedMan", label = "Oběšenec", hint = "Houpe se na laně (klikni, kde mu visí nohy). Přestřel lano – tělo zůstane jako závaží.", category = Category.Hra, previewSize = new Vector2(0.8f, 2f),
                build = () => LevelKitFactory.HangedMan(Vector3.zero) },
            new Entry { id = "ShadowDouble", label = "Stínový dvojník", hint = "Opakuje hrdinovy pohyby se zpožděním, ale neskáče. Vlákej ho do propasti.", category = Category.Hra, previewSize = new Vector2(0.6f, 1.8f),
                build = () => LevelKitFactory.ShadowDouble(Vector3.zero) },
            new Entry { id = "CrowSwarm", label = "Havraní roj", hint = "Létající hejno, krouží nad hrdinou a střemhlav jím prolétá. Bič jich srazí víc najednou.", category = Category.Hra, previewSize = new Vector2(3f, 2f),
                build = () => LevelKitFactory.CrowSwarm(Vector3.zero) },
            new Entry { id = "AshMoth", label = "Popelavá můra", hint = "Létá vysoko (na luk). Sype popel, který ztlumí šálu.", category = Category.Hra, previewSize = new Vector2(1.4f, 1.4f),
                build = () => LevelKitFactory.AshMoth(Vector3.zero) },
            new Entry { id = "LanternWisp", label = "Bludička", hint = "Neškodná, ale láká k propastem. Zásah ji chytí – zůstane svítit na místě.", category = Category.Hra, previewSize = new Vector2(0.6f, 0.8f),
                build = () => LevelKitFactory.LanternWisp(Vector3.zero) },
            new Entry { id = "PaperKite", label = "Papírový drak", hint = "Klikni na kolík v zemi. Létá na niti; přestřel ji – z jeho látky se dá plachtit (drž skok).", category = Category.Hra, previewSize = new Vector2(1.2f, 6f),
                build = () => LevelKitFactory.PaperKite(Vector3.zero) },
            new Entry { id = "Urn", label = "Urna", hint = "Rozbitný předmět.", category = Category.Hra, previewSize = new Vector2(0.6f, 0.8f),
                build = () => LevelKitFactory.Urn(Vector3.zero, 2) },
            new Entry { id = "RopeBridge", label = "Most s provazem", hint = "Prkno na hraně propasti; přestřel rudý provaz a spadne doprava jako most (11 m).", category = Category.Hra, previewSize = new Vector2(11f, 11f),
                build = () => LevelKitFactory.RopeBridge(Vector3.zero) },
            new Entry { id = "CameraZone", label = "Zóna kamery", hint = "V této oblasti kamera oddálí a zvedne pohled.", category = Category.Hra, previewSize = new Vector2(20f, 6f),
                build = () => LevelKitFactory.CameraZone(Vector3.zero) },

            // ---- decoration
            new Entry { id = "Arch", label = "Oblouk", hint = "Ruina oblouku.", category = Category.Dekorace, layered = true, previewSize = new Vector2(5.6f, 7.4f),
                build = () => LevelKitFactory.Arch(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "BrokenArch", label = "Rozbitý oblouk", hint = "Oblouk s ulomeným rohem.", category = Category.Dekorace, layered = true, previewSize = new Vector2(5.6f, 7.4f),
                build = () => LevelKitFactory.Arch(Vector3.zero, LevelLayer.Near, 4f, 4.5f, 0.5f, 2) },
            new Entry { id = "Column", label = "Sloup", hint = "Antický sloup s hlavicí.", category = Category.Dekorace, layered = true, previewSize = new Vector2(1.2f, 6f),
                build = () => LevelKitFactory.Column(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "BrokenColumn", label = "Zlomený sloup", hint = "Torzo sloupu.", category = Category.Dekorace, layered = true, previewSize = new Vector2(1.2f, 4f),
                build = () => LevelKitFactory.Column(Vector3.zero, LevelLayer.Near, 6f, true, 3) },
            new Entry { id = "RuinWall", label = "Zeď s okny", hint = "Rozpadlá zeď s klenutými okny (model z Blenderu).", category = Category.Dekorace, layered = true, previewSize = new Vector2(9f, 7f),
                build = () => LevelKitFactory.Ruin("RuinWall", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Tower", label = "Věž", hint = "Osmiboká věž s okny a ulomenou korunou.", category = Category.Dekorace, layered = true, previewSize = new Vector2(5.4f, 16f),
                build = () => LevelKitFactory.Ruin("Tower", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "RuinStairs", label = "Schody", hint = "Rozpadlé kamenné schodiště.", category = Category.Dekorace, layered = true, previewSize = new Vector2(5f, 3.2f),
                build = () => LevelKitFactory.Ruin("RuinStairs", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Fern", label = "Kapradí", hint = "Keř kapradí – skvělý do popředí (vlaje).", category = Category.Dekorace, layered = true, previewSize = new Vector2(3f, 1.6f),
                build = () => LevelKitFactory.Fern(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Rubble", label = "Sutiny", hint = "Hromada kamenů.", category = Category.Dekorace, layered = true, previewSize = new Vector2(2.4f, 0.8f),
                build = () => LevelKitFactory.Rubble(Vector3.zero, LevelLayer.Near, 4) },
            new Entry { id = "Banner", label = "Prapor", hint = "Rudý potrhaný prapor (vlaje).", category = Category.Dekorace, layered = true, previewSize = new Vector2(0.9f, 2.8f),
                build = () => LevelKitFactory.Banner(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Gallows", label = "Šibenice", hint = "Šibenice se dvěma prapory.", category = Category.Dekorace, layered = true, previewSize = new Vector2(3.2f, 5.8f),
                build = () => LevelKitFactory.Gallows(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "CrimsonTree", label = "Rudý strom", hint = "Javor s rudými listy.", category = Category.Dekorace, layered = true, previewSize = new Vector2(8f, 11f),
                build = () => LevelKitFactory.CrimsonTree(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Grass", label = "Tráva", hint = "Trs trávy (vlaje).", category = Category.Dekorace, layered = true, previewSize = new Vector2(3f, 1.4f),
                build = () => LevelKitFactory.Grass(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Waterfall", label = "Vodopád", hint = "Vodopád s tříští dole.", category = Category.Dekorace, layered = true, previewSize = new Vector2(3.2f, 22f),
                build = () => LevelKitFactory.Waterfall(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "MistBand", label = "Pás mlhy", hint = "Driftující mlha mezi vrstvami.", category = Category.Dekorace, layered = true, previewSize = new Vector2(40f, 6f),
                build = () => LevelKitFactory.MistBand(Vector3.zero, LevelLayer.Near, 40f) },
            new Entry { id = "LightShaft", label = "Paprsek světla", hint = "Šikmý měsíční paprsek.", category = Category.Dekorace, layered = true, previewSize = new Vector2(6f, 34f),
                build = () => LevelKitFactory.LightShaft(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "FallingLeaves", label = "Padající listí", hint = "Rudé listí padající v oblasti 60 m.", category = Category.Dekorace, previewSize = new Vector2(60f, 1f),
                build = () => LevelKitFactory.FallingLeaves(Vector3.zero) },

            // ---- scene
            new Entry { id = "LevelEssentials", label = "Základ úrovně", hint = "Kamera, světlo, post-processing, mlha, efekty, měsíc. Jednou na scénu.", category = Category.Scena, previewSize = new Vector2(1f, 1f),
                build = LevelKitFactory.Essentials },
        };

        public static Entry Find(string id) => Entries.Find(e => e.id == id);

        [MenuItem("Crimson Veil/Level Kit/Rebuild Prefabs")]
        public static void RebuildPrefabs()
        {
            LevelKitMaterials.Get(refresh: true);
            Directory.CreateDirectory(PrefabDir);
            foreach (var e in Entries)
            {
                var go = e.build();
                go.name = e.id;
                PrefabUtility.SaveAsPrefabAsset(go, e.PrefabPath);
                UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[CrimsonVeil] Level kit: {Entries.Count} prefabs rebuilt in {PrefabDir}");
        }

        public static GameObject Prefab(Entry e)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(e.PrefabPath);
            if (p == null)
            {
                RebuildPrefabs();
                p = AssetDatabase.LoadAssetAtPath<GameObject>(e.PrefabPath);
            }
            return p;
        }

        /// <summary>Instantiates the entry's prefab at <paramref name="pos"/> on <paramref name="layer"/>.</summary>
        public static GameObject Place(Entry e, Vector3 pos, LevelLayer layer, Transform parent)
        {
            var prefab = Prefab(e);
            var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : e.build();
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(pos.x, pos.y, e.layered ? DepthLayer.Z(layer) : 0f);
            if (e.layered && layer != LevelLayer.Near) LevelKitFactory.ApplyLayer(go, layer);
            return go;
        }
    }
}
