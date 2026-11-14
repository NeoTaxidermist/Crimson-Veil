using System;
using System.Collections.Generic;
using System.IO;
using CrimsonVeil.Veil;
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
            /// <summary>A ground plant: it gets a <see cref="GroundAnchor"/> and always sits on the terrain under it.</summary>
            public bool ground;
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
            new Entry { id = "GrapplePoint", label = "Úchyt pro bič", hint = "Železný kruh s rudou stuhou. S bičem v ruce zaútoč v dosahu (7,5 m, kruh musí být nad tebou): bič se omotá a hrdina se houpe. ←/→ rozhoupat, ↑/↓ šplhat po biči, skok = pustit se a vyletět. Pokládej nad propasti nebo pod vysoké římsy.", category = Category.Hra, previewSize = new Vector2(2.6f, 4f),
                build = () => LevelKitFactory.GrapplePoint(Vector3.zero) },
            new Entry { id = "RelicWhip", label = "Relikvie: Bič", hint = "Kamenný podstavec s rudým obrysem biče. Hrdina se ho dotkne a naučí se bič (L / RT). Relikvie za hrdinou na začátku hry se počítají jako nalezené.", category = Category.Hra, previewSize = new Vector2(1.2f, 2.6f),
                build = () => LevelKitFactory.Relic(VeilForm.Whip, Vector3.zero) },
            new Entry { id = "RelicSpear", label = "Relikvie: Kopí", hint = "Naučí kopí (K / Y): výpad, probodne štít, ve vzduchu s ↓ bodne dolů a odrazí se.", category = Category.Hra, previewSize = new Vector2(1.2f, 2.6f),
                build = () => LevelKitFactory.Relic(VeilForm.Spear, Vector3.zero) },
            new Entry { id = "RelicShield", label = "Relikvie: Štít", hint = "Naučí štít (U / LT): ťuk = úder štítem, držet = kryt, zvednout těsně před úderem = odražení; ve vzduchu zpomalí pád.", category = Category.Hra, previewSize = new Vector2(1.2f, 2.6f),
                build = () => LevelKitFactory.Relic(VeilForm.Shield, Vector3.zero) },
            new Entry { id = "RelicBow", label = "Relikvie: Luk", hint = "Naučí luk (I / LB): ťuk = výstřel, držet = napnout a mířit ↑/↓, pustit = vystřelit.", category = Category.Hra, previewSize = new Vector2(1.2f, 2.6f),
                build = () => LevelKitFactory.Relic(VeilForm.Bow, Vector3.zero) },
            new Entry { id = "Secret", label = "Tajemství", hint = "Neviditelná značka: v okruhu 9 m se volná šála nakloní k ní. Dej ji k ukryté urně, vysoké římse nebo tajné cestě.", category = Category.Hra, previewSize = new Vector2(1f, 1f),
                build = () => LevelKitFactory.Hint(Vector3.zero) },
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
            new Entry { id = "Aqueduct", label = "Akvadukt", hint = "Dvoupatrový rozpadlý akvadukt s popínavými rostlinami – dominanta pozadí.", category = Category.Dekorace, layered = true, previewSize = new Vector2(31f, 13.5f),
                build = () => LevelKitFactory.Ruin("Aqueduct", Vector3.zero, LevelLayer.Mid) },
            new Entry { id = "Castle", label = "Hrad", hint = "Hradní palác se dvěma věžemi, zbořeným rohem a cimbuřím.", category = Category.Dekorace, layered = true, previewSize = new Vector2(26f, 27f),
                build = () => LevelKitFactory.Ruin("Castle", Vector3.zero, LevelLayer.Far) },
            new Entry { id = "CliffRuin", label = "Útes s vesnicí", hint = "Vrstevnatý skalní útes s domky, zvonicí a suchými stromy na vrcholu.", category = Category.Dekorace, layered = true, previewSize = new Vector2(30f, 25f),
                build = () => LevelKitFactory.Ruin("CliffRuin", Vector3.zero, LevelLayer.Mid) },
            new Entry { id = "DeadTree", label = "Suchý strom", hint = "Pokroucený mrtvý strom s kořeny a kůrou – do střední i blízké vrstvy.", category = Category.Dekorace, layered = true, previewSize = new Vector2(6f, 7f),
                build = () => LevelKitFactory.Ruin("DeadTree", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Statue", label = "Socha", hint = "Zahalená socha se zlomeným mečem a rudým hadrem na podstavci.", category = Category.Dekorace, layered = true, previewSize = new Vector2(2.6f, 6.5f),
                build = () => LevelKitFactory.Ruin("Statue", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Cage", label = "Visací klec", hint = "Šibeniční klec s kostmi na řetězu; visí z bodu, kam ji položíš.", category = Category.Dekorace, layered = true, previewSize = new Vector2(1.4f, 4f),
                build = () => LevelKitFactory.Ruin("Cage", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Spires", label = "Věže na obzoru", hint = "Silueta gotického města – katedrála, věže, opěrné oblouky. Do dálky nebo oblohy.", category = Category.Dekorace, layered = true, previewSize = new Vector2(34f, 30f),
                build = () => LevelKitFactory.Ruin("Spires", Vector3.zero, LevelLayer.Far) },
            new Entry { id = "Bramble", ground = true, label = "Ostružiní", hint = "Trnité šlahouny – do popředí i na okraje plošin.", category = Category.Dekorace, layered = true, previewSize = new Vector2(3f, 1.6f),
                build = () => LevelKitFactory.Ruin("Bramble", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Weeds", ground = true, label = "Suché byliny", hint = "Stébla se semeníky a okolíky.", category = Category.Dekorace, layered = true, previewSize = new Vector2(2f, 1.8f),
                build = () => LevelKitFactory.Ruin("Weeds", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Chain", label = "Řetěz", hint = "Visící řetěz s hákem (2,4 m).", category = Category.Dekorace, layered = true, previewSize = new Vector2(0.4f, 3.4f),
                build = () => LevelKitFactory.Ruin("Chain", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "LeafPile", ground = true, label = "Spadané listí", hint = "Hromádka rudých javorových listů.", category = Category.Dekorace, layered = true, previewSize = new Vector2(3.5f, 0.6f),
                build = () => LevelKitFactory.Ruin("LeafPile", Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Fern", ground = true, label = "Kapradí", hint = "Keř kapradí – skvělý do popředí (vlaje).", category = Category.Dekorace, layered = true, previewSize = new Vector2(3f, 1.6f),
                build = () => LevelKitFactory.Fern(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Rubble", label = "Sutiny", hint = "Hromada kamenů.", category = Category.Dekorace, layered = true, previewSize = new Vector2(2.4f, 0.8f),
                build = () => LevelKitFactory.Rubble(Vector3.zero, LevelLayer.Near, 4) },
            new Entry { id = "Banner", label = "Prapor", hint = "Rudý potrhaný prapor (vlaje).", category = Category.Dekorace, layered = true, previewSize = new Vector2(0.9f, 2.8f),
                build = () => LevelKitFactory.Banner(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Gallows", label = "Šibenice", hint = "Šibenice se dvěma prapory.", category = Category.Dekorace, layered = true, previewSize = new Vector2(3.2f, 5.8f),
                build = () => LevelKitFactory.Gallows(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "CrimsonTree", label = "Rudý strom", hint = "Javor s rudými listy.", category = Category.Dekorace, layered = true, previewSize = new Vector2(8f, 11f),
                build = () => LevelKitFactory.CrimsonTree(Vector3.zero, LevelLayer.Near) },
            new Entry { id = "Grass", ground = true, label = "Tráva", hint = "Trs trávy (vlaje).", category = Category.Dekorace, layered = true, previewSize = new Vector2(3f, 1.4f),
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
                if (e.ground && go.GetComponent<GroundAnchor>() == null) go.AddComponent<GroundAnchor>();
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
            if (e.ground)
            {
                var anchor = go.GetComponent<GroundAnchor>();
                if (anchor == null) anchor = go.AddComponent<GroundAnchor>();
                anchor.Snap();
            }
            return go;
        }
    }
}
