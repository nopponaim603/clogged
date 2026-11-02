# Simulator Source — Gather Balance Simulation

> ซอร์สโค้ดของแบบจำลองที่ใช้สร้าง [gather-balance-basedsv-matrix.md](gather-balance-basedsv-matrix.md) (V2)
> และ [gather-balance-simulation.md](gather-balance-simulation.md) (V1)
> เป็นโปรเจกต์ .NET console แยกต่างหาก (**ไม่ใช่ส่วนหนึ่งของโปรเจกต์ Unity** — ไม่ถูกคอมไพล์โดย Unity และไม่กระทบ build)

---

## 1. วิธีรันซ้ำ

```powershell
# 1) สร้างโปรเจกต์
mkdir clogged-sim
cd clogged-sim

# 2) บันทึก sim.csproj (ด้านล่าง) เป็น sim.csproj
# 3) บันทึก Program.cs (ด้านล่าง) เป็น Program.cs

# 4) รัน: <outputDir> <repeats> <runsPerTest>
dotnet run -c Release -- .\out 10 10000
```

ผลลัพธ์ที่ได้ใน `.\out`:

| ไฟล์ | เนื้อหา |
|---|---|
| `results-raw.csv` | 480 แถว × 36 คอลัมน์ (ข้อมูลดิบต่อเทส — ใช้เป็น `results-basedsv-raw.csv` ในรายงาน V2) |
| `part-detail.md` | ตาราง §4 ของรายงาน |
| `part-summary.md` | ตาราง §5 ของรายงาน (5.1–5.7) |
| `part-sv.md` | ตาราง §6.1 (SV คงที่ทุกทีม × ทุกถ้ำ × 2 baseSV) |
| `part-floor.md` | ตาราง §6.2 (sensitivity เพดานความเสี่ยง 40 vs 25) |

---

## 2. `sim.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <InvariantGlobalization>true</InvariantGlobalization>
    <AssemblyName>sim</AssemblyName>
    <RootNamespace>sim</RootNamespace>
  </PropertyGroup>
</Project>
```

> ใช้ `net8.0` หรือ `net9.0` ก็ได้ ถ้าเครื่องไม่มี .NET 10

---

## 3. โครงสร้างแบบจำลอง

```
Main()
 ├─ scenarios[16]          ← 3 มิติ: baseSV {80/55/30, 70/45/25, 65/40/20, 50/30/10} × Div {2,3} × Core {25/50, 5/15}
 ├─ seeds[10]              ← seed ชุดเดียวใช้ร่วมทุก scenario (paired)
 ├─ foreach scenario × repeat × policy:
 │    └─ RunCell(seed, runs=10000)
 │         └─ foreach game: PlayRun()
 │              ├─ NewRun()            ← roster + ทรัพยากรเริ่มต้น (ค่าจาก §8.1)
 │              ├─ while day ≤ 7 and not ended:
 │              │    ├─ HealPhase()    ← รักษาคน Hurt (ยา 1 AP / ไม่มี ยา 3 AP)
 │              │    ├─ FoodPhase()    ← Food < aliveCount → Cook → Easy trip
 │              │    ├─ DispatchPhase()← เลือก (ทีม, ถ้ำ) ที่ expected Core Item สูงสุด
 │              │    └─ EndDay()       ← กินอาหาร / starvation / เช็คแพ้ชนะ
 │              └─ รวมสถิติลง Cell
 └─ sensitivity pass: Solo × 16 scenario อีกชุด ด้วย SurvivalFloor = 25
```

| มิติ | ค่าที่รัน | จำนวน |
|---|---|---|
| baseSV (Easy/Normal/Hard) | 80/55/30, 70/45/25, 65/40/20, 50/30/10 | 4 |
| Hard Success divisor | 2, 3 | 2 |
| Core Item chance (Normal/Hard) | 25/50, 5/15 | 2 |
| **รวม scenario** | | **16** |
| ซ้ำต่อ scenario | | 10 |
| policy | Solo, Duo, Adaptive | 3 |
| เกมต่อเซลล์ | | 10,000 |
| **เกมรวม** | | **4,800,000** (+1,600,000 ในส่วน sensitivity) |

---

## 4. โค้ดเต็ม — `Program.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

enum Talent { None, Heal, Crafting, Cooking, Exploring }
enum Policy { Solo, Duo, Adaptive }

sealed class UnitDef
{
    public readonly string Name; public readonly int Apt; public readonly Talent Talent;
    public UnitDef(string n, int a, Talent t) { Name = n; Apt = a; Talent = t; }
}

sealed class ZoneDef
{
    public string Name; public int Apt; public int Ap; public int Base;
    public int Core; public int Pity; public int MainType; public int MainAmount;
    public int[][] Bonus;
}

sealed class Scenario
{
    public int Id; public string Name; public string Label;
    public int Div; public int NormalCore; public int HardCore;
    public int PityN; public int PityH;
    public int BaseE, BaseN, BaseH;
    public string BaseLabel { get { return BaseE + "/" + BaseN + "/" + BaseH; } }
}

sealed class U
{
    public UnitDef Def; public bool Alive = true; public bool Hurt; public bool Gathered;
    public int Ap; public bool Blessed; public int Pos; public int Neg; public int Starve;
}

sealed class RunState
{
    public List<U> Units = new List<U>();
    public int Food, CookMat, Med, Mat;
    public bool CoreA, CoreB;
    public int PityN, PityH;
    public int Day = 1;
    public bool Ended; public string EndReason;
    public int Trips, Hs, Heals, Cooks;
    public int[] TripsByZone = new int[3];
    public int[] HsByZone = new int[3];
    public int FirstPieceDay, SecondPieceDay;
    public int UnitsLost;
    public int Pieces { get { return (CoreA ? 1 : 0) + (CoreB ? 1 : 0); } }
    public bool HasBoth { get { return CoreA && CoreB; } }
    public int AliveCount { get { int n = 0; foreach (var u in Units) if (u.Alive) n++; return n; } }
}

sealed class Move { public List<U> Team; public ZoneDef Zone; public double Score; }

sealed class Cell
{
    public Scenario S; public Policy P; public int Seed; public int Runs; public int Rep;
    public int Wins, LostAll, LostDeadline;
    public long WinDaySum;
    public long Trips, Hs, TripsN, TripsH, HsN, HsH, Heals, Cooks;
    public long Pieces, UnitsLost, FirstPieceDaySum, PityDrops, ChanceDrops;
    public int[] BothByDay = new int[9];
    public int[] FirstByDay = new int[9];
    public List<int> WinDays = new List<int>();

    public double WinPct { get { return 100.0 * Wins / Runs; } }
    public double AllLostPct { get { return 100.0 * LostAll / Runs; } }
    public double DeadlinePct { get { return 100.0 * LostDeadline / Runs; } }
    public double AvgWinDay { get { return Wins > 0 ? (double)WinDaySum / Wins : double.NaN; } }
    public double MedianWinDay
    {
        get
        {
            if (WinDays.Count == 0) return double.NaN;
            var s = WinDays.OrderBy(x => x).ToList();
            return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2.0;
        }
    }
    public double TripsPerRun { get { return (double)Trips / Runs; } }
    public double HsPerRun { get { return (double)Hs / Runs; } }
    public double HsRate { get { return Trips > 0 ? 100.0 * Hs / Trips : 0; } }
    public double HsNormalPct { get { return TripsN > 0 ? 100.0 * HsN / TripsN : 0; } }
    public double HsHardPct { get { return TripsH > 0 ? 100.0 * HsH / TripsH : 0; } }
    public double HardTripPct { get { return Trips > 0 ? 100.0 * TripsH / Trips : 0; } }
    public double PiecesPerRun { get { return (double)Pieces / Runs; } }
    public double PiecesPerHs { get { return Hs > 0 ? (double)Pieces / Hs : 0; } }
    public double FirstPieceDay
    {
        get
        {
            long n = 0;
            for (int d = 1; d <= 7; d++) n += FirstByDay[d];
            return n > 0 ? (double)FirstPieceDaySum / n : double.NaN;
        }
    }
    public double PityShare { get { long t = PityDrops + ChanceDrops; return t > 0 ? 100.0 * PityDrops / t : 0; } }
    public double UnitsLostPerRun { get { return (double)UnitsLost / Runs; } }
    public double HealsPerRun { get { return (double)Heals / Runs; } }
    public double CooksPerRun { get { return (double)Cooks / Runs; } }
    public double WinByPct(int d) { return 100.0 * BothByDay[d] / Runs; }
    public double FirstByPct(int d)
    {
        long n = 0;
        for (int i = 1; i <= d; i++) n += FirstByDay[i];
        return 100.0 * n / Runs;
    }
}

static class Sim
{
    static readonly UnitDef[] Defs = {
        new UnitDef("Ann", 0, Talent.Heal),
        new UnitDef("Boon", 2, Talent.Crafting),
        new UnitDef("Cia", 1, Talent.Cooking),
        new UnitDef("Dan", 2, Talent.Exploring),
    };

    const int DeadlineDays = 7;
    const int ApPerDay = 3;
    const int StartFood = 8, StartCookMat = 2, StartMed = 1, StartMat = 0;

    /// <summary>Teams refuse a trip whose success value is below this. Raised/lowered to model risk appetite.</summary>
    static int SurvivalFloor = 40;

    static Scenario sc;
    static ZoneDef[] zones;
    static Random rng;
    static int dropsFromPity, dropsFromChance;

    static ZoneDef[] BuildZones()
    {
        return new[]
        {
            new ZoneDef { Name = "Easy", Apt = 0, Ap = 1, Base = sc.BaseE, Core = 0, Pity = 0,
                MainType = 0, MainAmount = 2,
                Bonus = new[] { new[] { 0, 2 }, new[] { 1, 1 } } },
            new ZoneDef { Name = "Normal", Apt = 1, Ap = 2, Base = sc.BaseN, Core = sc.NormalCore, Pity = sc.PityN,
                MainType = 1, MainAmount = 1,
                Bonus = new[] { new[] { 1, 1 }, new[] { 0, 1 } } },
            new ZoneDef { Name = "Hard", Apt = 2, Ap = 3, Base = sc.BaseH, Core = sc.HardCore, Pity = sc.PityH,
                MainType = 2, MainAmount = 2,
                Bonus = new[] { new[] { 2, 2 }, new[] { 1, 1 } } },
        };
    }

    static bool Fumble(int sv, int roll) { return sv < 50 ? roll >= 96 : roll == 100; }

    static void TierCounts(int sv, int div, out int hs, out int reg, out int fail, out int fum)
    {
        hs = reg = fail = fum = 0;
        int thr = sv / div;
        for (int roll = 1; roll <= 100; roll++)
        {
            if (Fumble(sv, roll)) { fum++; continue; }
            if (roll <= thr) hs++;
            else if (roll <= sv) reg++;
            else fail++;
        }
    }

    static double PHs(int sv) { int a, b, c, d; TierCounts(sv, sc.Div, out a, out b, out c, out d); return a / 100.0; }

    static RunState NewRun()
    {
        var r = new RunState();
        foreach (var d in Defs) r.Units.Add(new U { Def = d, Ap = ApPerDay });
        r.Food = StartFood; r.CookMat = StartCookMat; r.Med = StartMed; r.Mat = StartMat;
        return r;
    }

    static int Sv(List<U> team, ZoneDef z)
    {
        int v = z.Base;
        bool apt = false, pos = false, neg = false;
        int blessed = 0;
        foreach (var u in team)
        {
            if (u.Def.Apt == z.Apt) apt = true;
            v -= 10 * Math.Abs(u.Def.Apt - z.Apt);
            if (u.Blessed) blessed++;
            if (u.Pos > 0) pos = true;
            if (u.Neg > 0) neg = true;
        }
        if (apt) v += 25;
        if (team.Count == 2) v += 25;
        if (pos) v += 10;
        if (neg) v -= 10;
        v += blessed * 10;
        return v;
    }

    static void GainLoot(RunState r, int type, int amount)
    {
        if (amount <= 0) return;
        if (type == 0) r.CookMat += amount;
        else if (type == 1) r.Med += amount;
        else r.Mat += amount;
    }

    static void GainTrait(List<U> team, bool positive)
    {
        var u = team[rng.Next(team.Count)];
        if (positive) u.Pos++; else u.Neg++;
    }

    static void HurtRandom(List<U> team) { team[rng.Next(team.Count)].Hurt = true; }

    static void LoseRandom(List<U> team)
    {
        var u = team[rng.Next(team.Count)];
        if (u.Alive) u.Alive = false;
    }

    static void Obtain(RunState r, int kind)
    {
        if (kind == 0) r.CoreA = true; else r.CoreB = true;
        if (r.Pieces == 1) r.FirstPieceDay = r.Day;
        if (r.Pieces == 2)
        {
            r.SecondPieceDay = r.Day;
            r.Ended = true;
            r.EndReason = "win";
        }
    }

    static void CoreRoll(RunState r, ZoneDef z, List<U> team)
    {
        int counter = z.Apt == 1 ? r.PityN : z.Apt == 2 ? r.PityH : 0;
        bool pityReady = z.Pity > 0 && counter + 1 >= z.Pity;

        int chance = z.Core + (team.Any(u => u.Def.Talent == Talent.Exploring) ? 25 : 0);
        if (chance > 100) chance = 100;

        var missing = new List<int>();
        if (!r.CoreA) missing.Add(0);
        if (!r.CoreB) missing.Add(1);

        int got = 0;
        if (missing.Count > 0)
        {
            if (pityReady)
            {
                Obtain(r, missing[rng.Next(missing.Count)]);
                got++;
                dropsFromPity++;
            }
            else
            {
                foreach (var kind in missing)
                {
                    if (rng.Next(0, 100) >= chance) continue;
                    Obtain(r, kind);
                    got++;
                    dropsFromChance++;
                }
            }
        }

        int delta = got > 0 ? -counter : 1;
        if (z.Apt == 1) r.PityN = Math.Max(0, r.PityN + delta);
        else if (z.Apt == 2) r.PityH = Math.Max(0, r.PityH + delta);
    }

    static void Dispatch(RunState r, List<U> team, ZoneDef z)
    {
        foreach (var u in team) { u.Ap -= z.Ap + u.Starve; u.Gathered = true; }

        int sv = Sv(team, z);
        int roll = rng.Next(1, 101);
        int tier;
        if (Fumble(sv, roll)) tier = 3;
        else if (roll <= sv / sc.Div) tier = 0;
        else if (roll <= sv) tier = 1;
        else tier = 2;

        r.Trips++;
        r.TripsByZone[z.Apt]++;
        if (tier == 0) { r.Hs++; r.HsByZone[z.Apt]++; }

        int mult = team.Any(u => u.Def.Talent == Talent.Exploring) ? 2 : 1;

        switch (tier)
        {
            case 0:
                GainLoot(r, z.MainType, z.MainAmount * mult);
                if (z.Bonus.Length > 0)
                {
                    var b = z.Bonus[rng.Next(z.Bonus.Length)];
                    GainLoot(r, b[0], b[1] * mult);
                }
                if (rng.NextDouble() < 0.5) GainTrait(team, true);
                CoreRoll(r, z, team);
                break;
            case 1:
                GainLoot(r, z.MainType, z.MainAmount * mult);
                break;
            case 2:
                GainLoot(r, z.MainType, (int)Math.Floor(z.MainAmount * 0.5) * mult);
                HurtRandom(team);
                if (rng.NextDouble() < 0.5) GainTrait(team, false);
                break;
            case 3:
                if (z.Apt == 2) LoseRandom(team);
                GainTrait(team, false);
                break;
        }

        foreach (var u in team) u.Blessed = false;
    }

    static bool Affordable(List<U> team, ZoneDef z)
    {
        foreach (var u in team) if (u.Ap < z.Ap + u.Starve) return false;
        return true;
    }

    static List<U> BestTeam(List<U> avail, ZoneDef z, int size)
    {
        List<U> best = null;
        int bestSv = int.MinValue;

        if (size == 1)
        {
            foreach (var u in avail)
            {
                var t = new List<U> { u };
                if (!Affordable(t, z)) continue;
                int sv = Sv(t, z);
                if (sv > bestSv) { bestSv = sv; best = t; }
            }
            return best;
        }

        for (int i = 0; i < avail.Count; i++)
            for (int j = i + 1; j < avail.Count; j++)
            {
                var t = new List<U> { avail[i], avail[j] };
                if (!Affordable(t, z)) continue;
                int sv = Sv(t, z);
                if (sv > bestSv) { bestSv = sv; best = t; }
            }
        return best;
    }

    static U PickWorker(RunState r, Talent preferred, int apCost)
    {
        U pick = null;
        foreach (var u in r.Units)
        {
            if (!u.Alive || u.Hurt) continue;
            if (u.Ap < apCost + u.Starve) continue;
            if (pick == null) { pick = u; continue; }
            bool uGood = u.Def.Talent == preferred;
            bool pGood = pick.Def.Talent == preferred;
            if (uGood && !pGood) pick = u;
            else if (uGood == pGood && u.Ap > pick.Ap) pick = u;
        }
        return pick;
    }

    static void HealPhase(RunState r)
    {
        int guard = 0;
        while (guard++ < 8)
        {
            U patient = null;
            foreach (var u in r.Units) if (u.Alive && u.Hurt) { patient = u; break; }
            if (patient == null) return;

            bool useMed = r.Med >= 1;
            int cost = useMed ? 1 : 3;
            U healer = PickWorker(r, Talent.Heal, cost);
            if (healer == null) return;

            healer.Ap -= cost + healer.Starve;
            if (useMed) r.Med--;
            patient.Hurt = false;
            if (healer.Def.Talent == Talent.Heal) patient.Blessed = true;
            r.Heals++;
        }
    }

    static bool CookOnce(RunState r)
    {
        if (r.CookMat < 1) return false;
        U cook = PickWorker(r, Talent.Cooking, 1);
        if (cook == null) return false;

        cook.Ap -= 1 + cook.Starve;
        r.CookMat--;
        r.Food += cook.Def.Talent == Talent.Cooking ? 2 : 1;
        r.Cooks++;
        return true;
    }

    static bool EasyTrip(RunState r, Policy p)
    {
        var avail = r.Units.Where(u => u.Alive && !u.Hurt && !u.Gathered).ToList();
        if (avail.Count == 0) return false;

        var z = zones[0];
        int size = (p == Policy.Duo && avail.Count >= 2) ? 2 : 1;
        var team = BestTeam(avail, z, size);
        if (team == null) return false;

        Dispatch(r, team, z);
        return true;
    }

    static void FoodPhase(RunState r, Policy p)
    {
        int need = r.AliveCount;
        int guard = 0;
        while (guard++ < 14)
        {
            if (r.Food >= need) return;
            if (r.CookMat >= 1 && CookOnce(r)) continue;
            if (!EasyTrip(r, p)) return;
            if (r.Ended) return;
        }
    }

    static double Score(RunState r, List<U> team, ZoneDef z, int sv)
    {
        int missing = (r.CoreA ? 0 : 1) + (r.CoreB ? 0 : 1);
        if (missing == 0 || z.Core == 0) return 0;

        double pHs = PHs(sv);
        int counter = z.Apt == 1 ? r.PityN : z.Apt == 2 ? r.PityH : 0;
        bool pityReady = z.Pity > 0 && counter + 1 >= z.Pity;

        int chance = z.Core + (team.Any(u => u.Def.Talent == Talent.Exploring) ? 25 : 0);
        if (chance > 100) chance = 100;

        double expected = pityReady ? 1.0 : missing * chance / 100.0;
        return pHs * expected / team.Count;
    }

    static void DispatchPhase(RunState r, Policy p)
    {
        int guard = 0;
        while (guard++ < 12)
        {
            if (r.Ended) return;

            var avail = r.Units.Where(u => u.Alive && !u.Hurt && !u.Gathered).ToList();
            if (avail.Count == 0) return;

            var teams = new List<List<U>>();
            if (p == Policy.Solo)
            {
                foreach (var u in avail) teams.Add(new List<U> { u });
            }
            else
            {
                for (int i = 0; i < avail.Count; i++)
                    for (int j = i + 1; j < avail.Count; j++)
                        teams.Add(new List<U> { avail[i], avail[j] });
                if (p == Policy.Adaptive || teams.Count == 0)
                    foreach (var u in avail) teams.Add(new List<U> { u });
            }

            Move best = null;
            foreach (var team in teams)
                foreach (var z in zones)
                {
                    if (!Affordable(team, z)) continue;
                    int sv = Sv(team, z);
                    if (sv < SurvivalFloor) continue;
                    double score = Score(r, team, z, sv);
                    if (score <= 0) continue;
                    if (best == null || score > best.Score)
                        best = new Move { Team = team, Zone = z, Score = score };
                }

            if (best == null) return;
            Dispatch(r, best.Team, best.Zone);
        }
    }

    static void PlayDay(RunState r, Policy p)
    {
        HealPhase(r);
        FoodPhase(r, p);
        DispatchPhase(r, p);
    }

    static void EndDay(RunState r)
    {
        if (r.Ended) return;

        foreach (var u in r.Units)
        {
            if (!u.Alive) continue;
            if (r.Food > 0) r.Food--;
            else
            {
                u.Starve++;
                if (u.Starve >= 3) { u.Alive = false; r.UnitsLost++; }
            }
        }

        r.Day++;

        if (r.AliveCount == 0) { r.Ended = true; r.EndReason = "alllost"; return; }
        if (r.Day > DeadlineDays && !r.HasBoth) { r.Ended = true; r.EndReason = "deadline"; return; }

        foreach (var u in r.Units) { u.Ap = ApPerDay; u.Gathered = false; }
    }

    static void PlayRun(RunState r, Policy p)
    {
        while (!r.Ended && r.Day <= DeadlineDays)
        {
            PlayDay(r, p);
            if (r.Ended) break;
            EndDay(r);
        }
        if (!r.Ended) { r.Ended = true; r.EndReason = "deadline"; }
    }

    static Cell RunCell(Scenario s, Policy p, int seed, int runs, int rep)
    {
        sc = s;
        zones = BuildZones();
        rng = new Random(seed);

        var c = new Cell { S = s, P = p, Seed = seed, Runs = runs, Rep = rep };

        for (int i = 0; i < runs; i++)
        {
            var r = NewRun();
            dropsFromPity = 0; dropsFromChance = 0;
            PlayRun(r, p);

            if (r.EndReason == "win") { c.Wins++; c.WinDaySum += r.SecondPieceDay; c.WinDays.Add(r.SecondPieceDay); }
            else if (r.EndReason == "alllost") c.LostAll++;
            else c.LostDeadline++;

            c.Trips += r.Trips; c.Hs += r.Hs;
            c.TripsN += r.TripsByZone[1]; c.HsN += r.HsByZone[1];
            c.TripsH += r.TripsByZone[2]; c.HsH += r.HsByZone[2];
            c.Heals += r.Heals; c.Cooks += r.Cooks;
            c.Pieces += r.Pieces; c.UnitsLost += r.UnitsLost;
            c.PityDrops += dropsFromPity; c.ChanceDrops += dropsFromChance;
            if (r.FirstPieceDay > 0) { c.FirstPieceDaySum += r.FirstPieceDay; c.FirstByDay[r.FirstPieceDay]++; }

            for (int d = 1; d <= DeadlineDays; d++)
                if (r.SecondPieceDay > 0 && r.SecondPieceDay <= d) c.BothByDay[d]++;
        }

        return c;
    }

    static string F(double x, int digits)
    {
        return double.IsNaN(x) ? "-" : x.ToString("F" + digits, CultureInfo.InvariantCulture);
    }

    /// <summary>Same maths as <see cref="Sv"/> but with no traits, blessed or starvation in play.</summary>
    static int PlainSv(int baseSv, int zoneApt, IList<UnitDef> team)
    {
        int v = baseSv;
        bool apt = false;
        foreach (var d in team)
        {
            if (d.Apt == zoneApt) apt = true;
            v -= 10 * Math.Abs(d.Apt - zoneApt);
        }
        if (apt) v += 25;
        if (team.Count == 2) v += 25;
        return v;
    }

    static void WriteSvAppendix(string outDir)
    {
        var bases = new[]
        {
            new { Label = "80/55/30", V = new[] { 80, 55, 30 } },
            new { Label = "50/30/10", V = new[] { 50, 30, 10 } },
            new { Label = "65/40/20", V = new[] { 65, 40, 20 } },
            new { Label = "70/45/25", V = new[] { 70, 45, 25 } },
        };
        var zones = new[] { new { Name = "Easy", Apt = 0 }, new { Name = "Normal", Apt = 1 }, new { Name = "Hard", Apt = 2 } };

        var teams = new List<List<UnitDef>>();
        foreach (var d in Defs) teams.Add(new List<UnitDef> { d });
        for (int i = 0; i < Defs.Length; i++)
            for (int j = i + 1; j < Defs.Length; j++)
                teams.Add(new List<UnitDef> { Defs[i], Defs[j] });

        var md = new StringBuilder();
        md.AppendLine("### 6.1 ตาราง SV แบบคงที่ (ไม่มี trait / blessed / starvation)");
        md.AppendLine();
        md.AppendLine("| baseSV | zone | team | SV | HS% (1/2) | HS% (1/3) | Regular% (1/2) | Fail% (1/2) | Fumble% (1/2) |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var b in bases)
            for (int z = 0; z < zones.Length; z++)
                foreach (var team in teams)
                {
                    int sv = PlainSv(b.V[z], zones[z].Apt, team);
                    int h2, r2, f2, fm2, h3, r3, f3, fm3;
                    TierCounts(sv, 2, out h2, out r2, out f2, out fm2);
                    TierCounts(sv, 3, out h3, out r3, out f3, out fm3);
                    string names = string.Join("+", team.Select(u => u.Name));
                    md.AppendLine($"| {b.Label} | {zones[z].Name} | {names} | {sv} | {h2} | {h3} | {r2} | {f2} | {fm2} |");
                }
        File.WriteAllText(Path.Combine(outDir, "part-sv.md"), md.ToString(), new UTF8Encoding(false));
    }

    static void Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : ".";
        int repeats = args.Length > 1 ? int.Parse(args[1]) : 10;
        int runsPerTest = args.Length > 2 ? int.Parse(args[2]) : 10000;

        var scenarios = new List<Scenario>();
        var baseSets = new[]
        {
            new { Label = "80/55/30", E = 80, N = 55, H = 30 },
            new { Label = "50/30/10", E = 50, N = 30, H = 10 },
            new { Label = "65/40/20", E = 65, N = 40, H = 20 },
            new { Label = "70/45/25", E = 70, N = 45, H = 25 },
        };
        var divisors = new[] { 2, 3 };
        var coreSets = new[]
        {
            new { N = 25, H = 50 },
            new { N = 5,  H = 15 },
        };

        int scenarioId = 0;
        foreach (var bs in baseSets)
            foreach (var div in divisors)
                foreach (var cs in coreSets)
                {
                    scenarioId++;
                    scenarios.Add(new Scenario
                    {
                        Id = scenarioId,
                        BaseE = bs.E, BaseN = bs.N, BaseH = bs.H,
                        Div = div,
                        NormalCore = cs.N, HardCore = cs.H,
                        PityN = 3, PityH = 2,
                        Label = $"{bs.Label} · 1/{div} · {cs.N}/{cs.H}",
                        Name = $"baseSV {bs.Label} | HS 1/{div} | Core {cs.N}/{cs.H}",
                    });
                }

        var policies = new[] { Policy.Solo, Policy.Duo, Policy.Adaptive };
        var cells = new List<Cell>();

        // One seed per repeat index, shared by every scenario, so a comparison
        // between scenarios uses the same dice (paired samples).
        var seeds = new int[repeats];
        var seedRng = new Random(424242);
        for (int i = 0; i < repeats; i++) seeds[i] = seedRng.Next(100000, 999999);

        foreach (var s in scenarios)
            for (int rep = 0; rep < repeats; rep++)
                foreach (var p in policies)
                    cells.Add(RunCell(s, p, seeds[rep], runsPerTest, rep + 1));

        // Sensitivity run: a player who will still gamble on Hard even when the
        // expected value is thin. Same seeds, Solo only, so the only difference
        // from the rows above is the risk rule.
        int normalFloor = SurvivalFloor;
        SurvivalFloor = 25;
        var floorCells = new List<Cell>();
        foreach (var s in scenarios)
            for (int rep = 0; rep < repeats; rep++)
                floorCells.Add(RunCell(s, Policy.Solo, seeds[rep], runsPerTest, rep + 1));
        SurvivalFloor = normalFloor;

        Directory.CreateDirectory(outDir);

        // --- results-raw.csv -------------------------------------------------
        var csv = new StringBuilder();
        csv.AppendLine("scenario,scenario_name,base_sv,hard_div,core_normal,core_hard,policy,repeat,seed,runs,win_pct,alllost_pct,deadline_pct,"
            + "avg_win_day,median_win_day,win_by_d1,win_by_d2,win_by_d3,win_by_d4,win_by_d5,win_by_d6,win_by_d7,"
            + "trips_per_run,hs_per_run,hs_rate_pct,hs_normal_pct,hs_hard_pct,hard_trip_pct,"
            + "pieces_per_run,pieces_per_hs,first_piece_day,first_piece_by_d3_pct,pity_share_pct,"
            + "units_lost_per_run,heals_per_run,cooks_per_run");
        foreach (var c in cells)
        {
            csv.Append(c.S.Id).Append(',')
               .Append('"').Append(c.S.Name).Append('"').Append(',')
               .Append(c.S.BaseLabel).Append(',')
               .Append(c.S.Div).Append(',')
               .Append(c.S.NormalCore).Append(',')
               .Append(c.S.HardCore).Append(',')
               .Append(c.P).Append(',')
               .Append(c.Rep).Append(',')
               .Append(c.Seed).Append(',')
               .Append(c.Runs).Append(',')
               .Append(F(c.WinPct, 3)).Append(',')
               .Append(F(c.AllLostPct, 3)).Append(',')
               .Append(F(c.DeadlinePct, 3)).Append(',')
               .Append(F(c.AvgWinDay, 3)).Append(',')
               .Append(F(c.MedianWinDay, 1)).Append(',');
            for (int d = 1; d <= 7; d++) csv.Append(F(c.WinByPct(d), 3)).Append(',');
            csv.Append(F(c.TripsPerRun, 3)).Append(',')
               .Append(F(c.HsPerRun, 3)).Append(',')
               .Append(F(c.HsRate, 3)).Append(',')
               .Append(F(c.HsNormalPct, 3)).Append(',')
               .Append(F(c.HsHardPct, 3)).Append(',')
               .Append(F(c.HardTripPct, 3)).Append(',')
               .Append(F(c.PiecesPerRun, 3)).Append(',')
               .Append(F(c.PiecesPerHs, 3)).Append(',')
               .Append(F(c.FirstPieceDay, 3)).Append(',')
               .Append(F(c.FirstByPct(3), 3)).Append(',')
               .Append(F(c.PityShare, 3)).Append(',')
               .Append(F(c.UnitsLostPerRun, 3)).Append(',')
               .Append(F(c.HealsPerRun, 3)).Append(',')
               .Append(F(c.CooksPerRun, 3));
            csv.AppendLine();
        }
        File.WriteAllText(Path.Combine(outDir, "results-raw.csv"), csv.ToString(), new UTF8Encoding(false));

        // --- part-detail.md (§4) --------------------------------------------
        var md = new StringBuilder();
        md.AppendLine("### 4.1 ตารางข้อมูลดิบรายเทส (per-test detail)");
        md.AppendLine();
        md.AppendLine($"- 1 เทส = 1 seed × 1 policy × {runsPerTest:N0} เกม");
        md.AppendLine("- ทุก scenario ใช้ seed ชุดเดียวกัน (paired) → เทียบผลข้าม scenario ได้ตรง ๆ");
        md.AppendLine($"- ซ้ำ {repeats} รอบต่อ scenario × {scenarios.Count} scenario = **{scenarios.Count * repeats} เทสต่อ policy** · รวม **{cells.Count} เซลล์** = {(long)cells.Count * runsPerTest:N0} เกม");
        md.AppendLine();

        foreach (var s in scenarios)
        {
            md.AppendLine($"#### S{s.Id} — {s.Name}  (baseSV Easy/Normal/Hard = {s.BaseLabel} · Hard Success = roll ≤ SV÷{s.Div} · Core Normal {s.NormalCore}% / Hard {s.HardCore}%)");
            md.AppendLine();
            md.AppendLine("| # | policy | seed | win% | all-lost% | deadline% | avg win day | median | HS/run | HS rate% | HS(N)% | HS(H)% | trips/run | pieces/run | 1st piece day | P(1st≤d3)% | pity% | lost/run |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var p in policies)
            {
                foreach (var c in cells.Where(x => x.S == s && x.P == p).OrderBy(x => x.Rep))
                {
                    md.AppendLine($"| {c.Rep} | {p} | {c.Seed} | {F(c.WinPct, 2)} | {F(c.AllLostPct, 2)} | {F(c.DeadlinePct, 2)} "
                        + $"| {F(c.AvgWinDay, 2)} | {F(c.MedianWinDay, 1)} | {F(c.HsPerRun, 2)} | {F(c.HsRate, 2)} "
                        + $"| {F(c.HsNormalPct, 2)} | {F(c.HsHardPct, 2)} | {F(c.TripsPerRun, 2)} | {F(c.PiecesPerRun, 2)} "
                        + $"| {F(c.FirstPieceDay, 2)} | {F(c.FirstByPct(3), 2)} | {F(c.PityShare, 1)} | {F(c.UnitsLostPerRun, 3)} |");
                }
            }
            md.AppendLine();
        }
        File.WriteAllText(Path.Combine(outDir, "part-detail.md"), md.ToString(), new UTF8Encoding(false));

        // --- part-summary.md (§5) -------------------------------------------
        var sm = new StringBuilder();
        string MeanSd(List<Cell> list, Func<Cell, double> f, int dg)
        {
            var vals = list.Select(f).Where(v => !double.IsNaN(v)).ToList();
            if (vals.Count == 0) return "-";
            double mean = vals.Average();
            double sd = vals.Count > 1 ? Math.Sqrt(vals.Sum(v => (v - mean) * (v - mean)) / (vals.Count - 1)) : 0;
            return mean.ToString("F" + dg, CultureInfo.InvariantCulture) + " ± " + sd.ToString("F" + dg, CultureInfo.InvariantCulture);
        }
        double Mean(List<Cell> list, Func<Cell, double> f)
        {
            var vals = list.Select(f).Where(v => !double.IsNaN(v)).ToList();
            return vals.Count == 0 ? double.NaN : vals.Average();
        }

        sm.AppendLine("### 5.1 ตารางสรุปหลัก — ทุก policy (เฉลี่ย 10 รอบ ± SD)");
        sm.AppendLine();
        sm.AppendLine("| # | กติกา | policy | win% | all-lost% | deadline% | median day | d1 win% | d3 win% | d5 win% | HS rate% | pieces/run | 1st piece day | pity share% |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
            foreach (var p in policies)
            {
                var list = cells.Where(x => x.S == s && x.P == p).ToList();
                sm.AppendLine($"| S{s.Id} | {s.Label} | {p} | {MeanSd(list, c => c.WinPct, 2)} | {MeanSd(list, c => c.AllLostPct, 2)} "
                    + $"| {MeanSd(list, c => c.DeadlinePct, 2)} | {MeanSd(list, c => c.MedianWinDay, 2)} "
                    + $"| {MeanSd(list, c => c.WinByPct(1), 2)} | {MeanSd(list, c => c.WinByPct(3), 2)} | {MeanSd(list, c => c.WinByPct(5), 2)} "
                    + $"| {MeanSd(list, c => c.HsRate, 2)} | {MeanSd(list, c => c.PiecesPerRun, 3)} "
                    + $"| {MeanSd(list, c => c.FirstPieceDay, 2)} | {MeanSd(list, c => c.PityShare, 1)} |");
            }

        sm.AppendLine();
        sm.AppendLine("### 5.2 ตารางสรุป — เทียบทุกกติกา (โหมด Solo)");
        sm.AppendLine();
        sm.AppendLine("| # | baseSV E/N/H | HS divisor | Normal% / Hard% | win% | median day | HS rate% | HS/run | pieces per HS | 1st piece day | P(1st ≤ d3)% | pity share% |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
        {
            var list = cells.Where(x => x.S == s && x.P == Policy.Solo).ToList();
            sm.AppendLine($"| S{s.Id} | {s.BaseLabel} | SV÷{s.Div} | {s.NormalCore} / {s.HardCore} | {MeanSd(list, c => c.WinPct, 2)} "
                + $"| {MeanSd(list, c => c.MedianWinDay, 2)} | {MeanSd(list, c => c.HsRate, 2)} | {MeanSd(list, c => c.HsPerRun, 2)} "
                + $"| {MeanSd(list, c => c.PiecesPerHs, 3)} | {MeanSd(list, c => c.FirstPieceDay, 2)} "
                + $"| {MeanSd(list, c => c.FirstByPct(3), 2)} | {MeanSd(list, c => c.PityShare, 1)} |");
        }

        sm.AppendLine();
        sm.AppendLine("### 5.3 ตารางสรุป — Solo vs Duo vs Adaptive");
        sm.AppendLine();
        sm.AppendLine("| # | Solo win% | Duo win% | ΔDuo | Adaptive win% | Solo all-lost% | Duo all-lost% | Solo HS rate% | Duo HS rate% | Solo HS/run | Duo HS/run | Solo HS(H)% | Duo HS(H)% |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
        {
            var solo = cells.Where(x => x.S == s && x.P == Policy.Solo).ToList();
            var duo = cells.Where(x => x.S == s && x.P == Policy.Duo).ToList();
            var ad = cells.Where(x => x.S == s && x.P == Policy.Adaptive).ToList();
            double sw = Mean(solo, c => c.WinPct), dw = Mean(duo, c => c.WinPct);
            sm.AppendLine($"| S{s.Id} | {sw:F2} | {dw:F2} | {(dw - sw):+0.00;-0.00;0.00} | {Mean(ad, c => c.WinPct):F2} "
                + $"| {Mean(solo, c => c.AllLostPct):F2} | {Mean(duo, c => c.AllLostPct):F2} "
                + $"| {Mean(solo, c => c.HsRate):F2} | {Mean(duo, c => c.HsRate):F2} "
                + $"| {Mean(solo, c => c.HsPerRun):F2} | {Mean(duo, c => c.HsPerRun):F2} "
                + $"| {Mean(solo, c => c.HsHardPct):F2} | {Mean(duo, c => c.HsHardPct):F2} |");
        }

        sm.AppendLine();
        sm.AppendLine("### 5.4 ตารางสรุป — เส้นทางการชนะสะสม (% ของ run ที่ชนะแล้ว, เฉลี่ย 10 รอบ)");
        sm.AppendLine();
        sm.AppendLine("| # | policy | d1 | d2 | d3 | d4 | d5 | d6 | d7 |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
            foreach (var p in policies)
            {
                var list = cells.Where(x => x.S == s && x.P == p).ToList();
                var row = new List<string>();
                for (int d = 1; d <= 7; d++) row.Add(Mean(list, c => c.WinByPct(d)).ToString("F1"));
                sm.AppendLine($"| S{s.Id} | {p} | " + string.Join(" | ", row) + " |");
            }

        sm.AppendLine();
        sm.AppendLine("### 5.5 ตารางสรุป — เศรษฐกิจและการเอาตัวรอด (เฉลี่ย 10 รอบ, ทุก policy)");
        sm.AppendLine();
        sm.AppendLine("| # | policy | trips/run | HS/run | heals/run | cooks/run | units lost/run | pieces/run | pieces per HS |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
            foreach (var p in policies)
            {
                var list = cells.Where(x => x.S == s && x.P == p).ToList();
                sm.AppendLine($"| S{s.Id} | {p} | {Mean(list, c => c.TripsPerRun):F2} | {Mean(list, c => c.HsPerRun):F2} "
                    + $"| {Mean(list, c => c.HealsPerRun):F2} | {Mean(list, c => c.CooksPerRun):F2} "
                    + $"| {Mean(list, c => c.UnitsLostPerRun):F3} | {Mean(list, c => c.PiecesPerRun):F3} "
                    + $"| {Mean(list, c => c.PiecesPerHs):F3} |");
            }

        sm.AppendLine();
        sm.AppendLine("### 5.6 ความเสถียรของการทดสอบ (ใช้ตัดสินว่า 10 รอบพอหรือไม่)");
        sm.AppendLine();
        sm.AppendLine("| # | policy | SD ของ win% | SD ของ HS rate% | SD ของ pieces/run | win% ต่ำสุด | win% สูงสุด | ช่วง (max−min) |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
            foreach (var p in policies)
            {
                var list = cells.Where(x => x.S == s && x.P == p).ToList();
                var wins = list.Select(c => c.WinPct).ToList();
                double mean = wins.Average();
                double sd = Math.Sqrt(wins.Sum(v => (v - mean) * (v - mean)) / (wins.Count - 1));
                var hs = list.Select(c => c.HsRate).ToList();
                double hsm = hs.Average();
                double hssd = Math.Sqrt(hs.Sum(v => (v - hsm) * (v - hsm)) / (hs.Count - 1));
                var pcs = list.Select(c => c.PiecesPerRun).ToList();
                double pcm = pcs.Average();
                double pcsd = Math.Sqrt(pcs.Sum(v => (v - pcm) * (v - pcm)) / (pcs.Count - 1));
                sm.AppendLine($"| S{s.Id} | {p} | {sd:F2} | {hssd:F2} | {pcsd:F3} | {wins.Min():F2} | {wins.Max():F2} | {(wins.Max() - wins.Min()):F2} |");
            }

        sm.AppendLine();
        sm.AppendLine("### 5.7 ตารางสรุป — ทุกชุด baseSV เทียบกับ 80/55/30 (metric เดียวกัน เทียบตรง)");
        sm.AppendLine();
        sm.AppendLine("| HS divisor | Normal% / Hard% | policy | baseSV | win% | Δwin vs 80/55/30 | HS rate% | HS(H)% | hard trip% | median day | pieces/run |");
        sm.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var sRef in scenarios.Where(x => x.BaseE == 80))
        {
            foreach (var p in policies)
            {
                var lr = cells.Where(x => x.S == sRef && x.P == p).ToList();
                double wr = Mean(lr, c => c.WinPct);
                foreach (var sOther in scenarios.Where(x => x.Div == sRef.Div && x.NormalCore == sRef.NormalCore))
                {
                    var lo = cells.Where(x => x.S == sOther && x.P == p).ToList();
                    double wo = Mean(lo, c => c.WinPct);
                    string delta = sOther.BaseE == 80 ? "—" : (wo - wr).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
                    sm.AppendLine($"| SV÷{sRef.Div} | {sRef.NormalCore}/{sRef.HardCore} | {p} | {sOther.BaseLabel} | {wo:F2} | {delta} "
                        + $"| {Mean(lo, c => c.HsRate):F2} | {Mean(lo, c => c.HsHardPct):F2} | {Mean(lo, c => c.HardTripPct):F2} "
                        + $"| {Mean(lo, c => c.MedianWinDay):F2} | {Mean(lo, c => c.PiecesPerRun):F3} |");
                }
            }
        }

        File.WriteAllText(Path.Combine(outDir, "part-summary.md"), sm.ToString(), new UTF8Encoding(false));

        // --- part-floor.md (sensitivity: risk floor 40 vs 25) ---------------
        var fl = new StringBuilder();
        fl.AppendLine("### 6.2 Sensitivity — เพดานความเสี่ยงของทีม (Solo, seed ชุดเดียวกัน)");
        fl.AppendLine();
        fl.AppendLine("`SurvivalFloor` คือค่า SV ขั้นต่ำที่ทีมจะยอมออกทริป (ค่าที่ใช้ในตารางหลัก = 40)");
        fl.AppendLine("ลดลงเป็น 25 เพื่อจำลองผู้เล่นที่ยอมเสี่ยง Hard แม้ค่า SV จะต่ำ — ใช้ seed ชุดเดียวกัน จึงเทียบได้ตรง");
        fl.AppendLine();
        fl.AppendLine("| # | กติกา | floor | win% | median day | trips/run | Hard trip% | HS rate% | pieces/run | pity share% |");
        fl.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scenarios)
        {
            var strict = cells.Where(x => x.S == s && x.P == Policy.Solo).ToList();
            var loose = floorCells.Where(x => x.S == s).ToList();
            var pairs = new[]
            {
                new { Name = "40", Rows = strict },
                new { Name = "25", Rows = loose },
            };
            foreach (var pair in pairs)
                fl.AppendLine($"| S{s.Id} | {s.Label} | {pair.Name} | {Mean(pair.Rows, c => c.WinPct):F2} | {Mean(pair.Rows, c => c.MedianWinDay):F2} "
                    + $"| {Mean(pair.Rows, c => c.TripsPerRun):F2} | {Mean(pair.Rows, c => c.HardTripPct):F2} | {Mean(pair.Rows, c => c.HsRate):F2} "
                    + $"| {Mean(pair.Rows, c => c.PiecesPerRun):F3} | {Mean(pair.Rows, c => c.PityShare):F1} |");
        }
        File.WriteAllText(Path.Combine(outDir, "part-floor.md"), fl.ToString(), new UTF8Encoding(false));

        WriteSvAppendix(outDir);

        Console.WriteLine($"done: {cells.Count} cells = {scenarios.Count} scenarios x {repeats} repeats x {policies.Length} policies");
        Console.WriteLine($"      {runsPerTest} runs per cell, {(long)cells.Count * runsPerTest:N0} games total");
    }
}
```

---

## 5. จุดที่ต้องระวังถ้าจะแก้แบบจำลอง

| จุด | เหตุผล |
|---|---|
| `SurvivalFloor = 40` | AI จะไม่ส่งถ้ำที่ SV ต่ำกว่านี้ — ถ้าลดค่านี้ ผลจะเพี้ยนเพราะ AI จะยอมตายฟรี (รายงาน V2 ใช้ค่านี้เป็นตัวแปร sensitivity ที่ §6.2) |
| `Score()` | ใช้ `P(HS) × expected pieces ÷ team.Count` — เป็นนิยามของ "คุ้ม" ถ้าเปลี่ยนนิยาม ผลจะเปลี่ยน |
| `HealPhase()` ก่อน `DispatchPhase()` | ลำดับตามเกมจริง: รักษาก่อนเพื่อให้คนหาย Hurt กลับมาส่งได้ในวันเดียวกัน |
| `EndDay()` ไม่รีเซ็ต `Starve` | ตรงกับเกมจริง (starvation stack ไม่ลดเอง) |
| `Obtain()` ตั้ง `Ended = true` ทันที | ตรงกับ `CheckImmediateWin()` ของเกม |
| seed ชุดเดียวทุก scenario | ถ้าเปลี่ยนเป็น seed ต่างกันต่อ scenario จะเทียบข้าม scenario ไม่ได้ |

---

*ไฟล์นี้เป็นเอกสารประกอบการทดสอบ ไม่ใช่โค้ดที่รันใน Unity — ปลอดภัยที่จะลบเมื่อจบ sprint*
