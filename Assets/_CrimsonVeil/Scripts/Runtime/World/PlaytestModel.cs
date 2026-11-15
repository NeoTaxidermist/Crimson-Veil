using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// What a playtest measures, engine independent: when the hero first gets into each section of the level (by
    /// how far along it he is), how long each one took, how many times he died in it, and the whole run's time.
    /// </summary>
    public sealed class PlaytestModel
    {
        public struct Section
        {
            public string name;
            public float fromX;
            public Section(string name, float fromX) { this.name = name; this.fromX = fromX; }
        }

        public sealed class Record
        {
            public string name;
            public float enteredAt = -1f;   // s since the start, -1 = not reached
            public float leftAt = -1f;
            public int deaths;
            public float Duration(float now) => enteredAt < 0f ? 0f : (leftAt >= 0f ? leftAt : now) - enteredAt;
        }

        /// <summary>The prototype level's sections (left to right; the x where each begins, see PrototypeSceneBuilder).</summary>
        public static readonly Section[] PrototypeSections =
        {
            new Section("Start a šibenice", -100f),
            new Section("Římsa a šachta", 11f),
            new Section("Horní cesta", 31f),
            new Section("Karmínový strom", 58f),
            new Section("Most přes propast a druhý břeh", 80f),
            new Section("Věž", 115f),
            new Section("Hradby", 133f),
            new Section("Studna", 176f),
            new Section("Výstup na plošinu", 195f),
            new Section("Zvonice", 222f),
            new Section("Hřeben", 240f),
            new Section("Propast", 282f),
            new Section("Zahrada", 310f),
        };

        public readonly List<Record> Records = new List<Record>();
        readonly Section[] sections;
        readonly float endX;
        int current = -1;

        public float Elapsed { get; private set; }
        public int Deaths { get; private set; }
        public bool Finished { get; private set; }
        public float FinishedAt { get; private set; } = -1f;
        public int Current => current;

        /// <summary>Raised when the hero first gets into a section (its index) - and when he reaches the end (-1).</summary>
        public event System.Action<int> Entered;

        public PlaytestModel(Section[] sections, float endX)
        {
            this.sections = sections;
            this.endX = endX;
            foreach (var s in sections) Records.Add(new Record { name = s.name });
        }

        public int SectionAt(float x)
        {
            int i = 0;
            for (int k = 0; k < sections.Length; k++) if (x >= sections[k].fromX) i = k;
            return i;
        }

        /// <summary>The hero is at <paramref name="x"/> at <paramref name="time"/> s into the run. Sections count once,
        /// when first reached going forward (walking back doesn't re-enter them).</summary>
        public void Tick(float time, float x)
        {
            Elapsed = time;
            if (Finished) return;
            int s = SectionAt(x);
            if (s > current)
            {
                for (int k = Mathf.Max(0, current); k <= s; k++)
                {
                    if (k > current && Records[k].enteredAt < 0f) Records[k].enteredAt = time;
                    if (k < s && Records[k].leftAt < 0f && Records[k].enteredAt >= 0f) Records[k].leftAt = time;
                }
                current = s;
                Entered?.Invoke(s);
            }
            if (x >= endX)
            {
                Finished = true;
                FinishedAt = time;
                if (current >= 0 && Records[current].leftAt < 0f) Records[current].leftAt = time;
                Entered?.Invoke(-1);
            }
        }

        /// <summary>The hero died (or fell) at <paramref name="x"/>.</summary>
        public void Died(float x)
        {
            Deaths++;
            Records[Mathf.Max(0, current >= 0 ? current : SectionAt(x))].deaths++;
        }

        public static string Clock(float s)
        {
            int t = Mathf.Max(0, Mathf.FloorToInt(s));
            return (t / 60).ToString("00") + ":" + (t % 60).ToString("00");
        }

        /// <summary>The run as a table to read.</summary>
        public string Report(string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine(new string('=', title.Length));
            sb.AppendLine(Finished ? "Dohráno za " + Clock(FinishedAt) : "Nedohráno, hraje se " + Clock(Elapsed) + " (naposled: " + (current >= 0 ? Records[current].name : "-") + ")");
            sb.AppendLine("Smrti celkem: " + Deaths);
            sb.AppendLine();
            sb.AppendLine(string.Format("{0,-34} {1,8} {2,8} {3,6}", "Úsek", "začátek", "trvání", "smrti"));
            float now = Finished ? FinishedAt : Elapsed;
            foreach (var r in Records)
            {
                if (r.enteredAt < 0f) { sb.AppendLine(string.Format("{0,-34} {1,8} {2,8} {3,6}", r.name, "-", "-", "-")); continue; }
                sb.AppendLine(string.Format("{0,-34} {1,8} {2,8} {3,6}", r.name, Clock(r.enteredAt), Clock(r.Duration(now)), r.deaths));
            }
            return sb.ToString();
        }
    }
}
