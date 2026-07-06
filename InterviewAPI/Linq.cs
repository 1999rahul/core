// ============================================================
//  LINQ COMPLETE REFERENCE — C# Interview Preparation Guide
// ============================================================
//  Topics covered:
//    1.  Data models used throughout
//    2.  Query syntax vs method syntax
//    3.  Filtering          — Where
//    4.  Projection         — Select, SelectMany
//    5.  Sorting            — OrderBy, ThenBy, Reverse
//    6.  Grouping           — GroupBy
//    7.  Joining            — Join, GroupJoin, left outer join
//    8.  Aggregation        — Count, Sum, Average, Min, Max, Aggregate
//    9.  Element operators  — First, Single, Last, ElementAt (and OrDefault)
//   10.  Set operations     — Distinct, Union, Intersect, Except
//   11.  Quantifiers        — Any, All, Contains
//   12.  Partitioning       — Skip, Take, Chunk, SkipWhile, TakeWhile
//   13.  Deferred vs immediate execution
//   14.  IEnumerable<T> vs IQueryable<T>
//   15.  LINQ providers
//          a. LINQ to Objects
//          b. LINQ to DataSet
//          c. LINQ to Entities  (EF Core — conceptual; needs a real DB)
//          d. LINQ to XML
//   16.  Expression trees
//   17.  Interview Q&A quick reference
// ============================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

// ── Entry point ──────────────────────────────────────────────────────────────

class LinqReference
{
    static void Main()
    {
        // Uncomment any section to run it
        Section02_QueryVsMethodSyntax();
        Section03_Filtering();
        Section04_Projection();
        Section05_Sorting();
        Section06_Grouping();
        Section07_Joining();
        Section08_Aggregation();
        Section09_ElementOperators();
        Section10_SetOperations();
        Section11_Quantifiers();
        Section12_Partitioning();
        Section13_DeferredExecution();
        Section14_IEnumerableVsIQueryable();
        Section15b_LinqToDataSet();
        Section15d_LinqToXml();
        Section16_ExpressionTrees();
    }
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 1 — DATA MODELS USED THROUGHOUT THIS FILE
// ═══════════════════════════════════════════════════════════════════════════

record Employee(string Name, string Dept, decimal Salary, int YearsExp = 3);

record Department(int Id, string Name, string Location);

record Order(int Id, decimal Total, int CustomerId, string Status);

record Customer(int Id, string Name, string Country);

// ── Sample data factory ──────────────────────────────────────────────────────
static class SampleData
{
    public static List<Employee> Employees() => new()
    {
        new("Asha",  "IT",  75000m, 5),
        new("Raj",   "IT",  45000m, 2),
        new("Maya",  "HR",  60000m, 4),
        new("Karan", "HR",  55000m, 3),
        new("Priya", "IT",  80000m, 7),
        new("Dev",   "Finance", 70000m, 6),
        new("Neha",  "Finance", 65000m, 4),
    };

    public static List<Department> Departments() => new()
    {
        new(1, "IT",      "Bangalore"),
        new(2, "HR",      "Mumbai"),
        new(3, "Finance", "Delhi"),
        new(4, "Legal",   "Chennai"),    // no employees → tests left join
    };

    public static List<Order> Orders() => new()
    {
        new(101, 1500m, 1, "Shipped"),
        new(102,  800m, 2, "Pending"),
        new(103, 2200m, 1, "Delivered"),
        new(104,  300m, 3, "Cancelled"),
        new(105, 5000m, 2, "Shipped"),
    };

    public static List<Customer> Customers() => new()
    {
        new(1, "Asha Corp",   "India"),
        new(2, "Raj Traders", "India"),
        new(3, "MayaTech",    "USA"),
    };
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 2 — QUERY SYNTAX vs METHOD SYNTAX
// ═══════════════════════════════════════════════════════════════════════════
//
//  Both compile to the same IL.
//  Query syntax is sugar; method syntax is what the compiler generates.
//
//  Key rule: method syntax is the industry standard and supports every
//  LINQ operator. Several operators (Count, Sum, Any, All, Aggregate,
//  Skip, Take, Distinct) have NO query-syntax equivalent.
// ───────────────────────────────────────────────────────────────────────────

static void Section02_QueryVsMethodSyntax()
{
    var employees = SampleData.Employees();

    // ── Query syntax (SQL-like) ──────────────────────────────────────────
    var querySyntax =
        from e in employees
        where e.Dept == "IT"
        orderby e.Salary descending
        select new { e.Name, e.Salary };

    // ── Method syntax (fluent / lambda) — identical result ───────────────
    var methodSyntax = employees
        .Where(e => e.Dept == "IT")
        .OrderByDescending(e => e.Salary)
        .Select(e => new { e.Name, e.Salary });

    // Both yield: Priya 80000 → Asha 75000 → Raj 45000

    // ── Operators ONLY available in method syntax ────────────────────────
    int count = employees.Count(e => e.Dept == "IT");   // no query syntax
    bool any = employees.Any(e => e.Salary > 70000);  // no query syntax
    int prod = new[] { 1, 2, 3, 4, 5 }.Aggregate((a, n) => a * n); // = 120
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 3 — FILTERING  (Where)
// ═══════════════════════════════════════════════════════════════════════════

static void Section03_Filtering()
{
    var employees = SampleData.Employees();

    // ── Single condition ─────────────────────────────────────────────────
    var itTeam = employees.Where(e => e.Dept == "IT");

    // ── Multiple conditions (combine with && — do NOT chain Where calls) ─
    var itSeniors = employees.Where(e => e.Dept == "IT" && e.YearsExp >= 5);

    // ── Index overload — filter by position ─────────────────────────────
    var firstTwo = employees.Where((e, i) => i < 2);

    // ── Chained Where (legal but less readable — prefer &&) ─────────────
    var chained = employees
        .Where(e => e.Dept == "IT")
        .Where(e => e.Salary > 50000); // second Where adds an iterator

    // INTERVIEW NOTE:
    // Chaining two Where() calls is slightly less efficient than a single
    // Where(a && b) because it creates an extra iterator wrapper.
    // In LINQ to Entities (EF Core) both forms generate identical SQL,
    // but for LINQ to Objects prefer the single-condition form.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 4 — PROJECTION  (Select, SelectMany)
// ═══════════════════════════════════════════════════════════════════════════

static void Section04_Projection()
{
    var employees = SampleData.Employees();

    // ── Select: one-to-one transform ────────────────────────────────────
    IEnumerable<string> names = employees.Select(e => e.Name);
    IEnumerable<decimal> salaries = employees.Select(e => e.Salary);

    // Project to anonymous object
    var summaries = employees.Select(e => new
    {
        e.Name,
        Dept = e.Dept.ToUpper(),
        SalaryK = e.Salary / 1000,
        IsSenior = e.YearsExp >= 5
    });

    // Select with index
    var numbered = employees.Select((e, i) => $"{i + 1}. {e.Name}");

    // ── SelectMany: flatten nested collections ────────────────────────────
    //
    //  Select  → IEnumerable<string[]>  (nested — each dept has an array)
    //  SelectMany → IEnumerable<string> (flat — all skills in one sequence)

    var departments = new[]
    {
        new { Name = "IT",      Skills = new[] { "C#", "SQL", "Azure" } },
        new { Name = "HR",      Skills = new[] { "Excel", "SAP" }        },
        new { Name = "Finance", Skills = new[] { "Excel", "Tally", "SQL" }},
    };

    // Without SelectMany — gives IEnumerable<string[]>
    var nested = departments.Select(d => d.Skills);

    // With SelectMany — flattens to IEnumerable<string>
    var allSkills = departments.SelectMany(d => d.Skills);
    // → "C#", "SQL", "Azure", "Excel", "SAP", "Excel", "Tally", "SQL"

    // SelectMany with result selector — combine parent + child in output
    var pairs = departments.SelectMany(
        d => d.Skills,
        (dept, skill) => $"{dept.Name}: {skill}"
    );
    // → "IT: C#", "IT: SQL", "IT: Azure", "HR: Excel", ...

    // INTERVIEW NOTE:
    // Select → one result per source element (mapping).
    // SelectMany → zero-or-more results per source element (flattening).
    // Think of SelectMany as: Select then flatten one level.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 5 — SORTING  (OrderBy, ThenBy, Reverse)
// ═══════════════════════════════════════════════════════════════════════════

static void Section05_Sorting()
{
    var employees = SampleData.Employees();

    // ── Primary sort ────────────────────────────────────────────────────
    var byName = employees.OrderBy(e => e.Name);
    var bySalaryDesc = employees.OrderByDescending(e => e.Salary);

    // ── Multi-column sort — use ThenBy, NOT a second OrderBy ────────────
    var sorted = employees
        .OrderBy(e => e.Dept)
        .ThenByDescending(e => e.Salary);

    // ── WRONG — second OrderBy REPLACES the first, not adds to it ───────
    var wrong = employees
        .OrderBy(e => e.Dept)
        .OrderBy(e => e.Salary);   // ← overwrites the dept sort!

    // ── Reverse order of a sequence ─────────────────────────────────────
    var reversed = employees.Reverse();

    // ── Custom comparator ───────────────────────────────────────────────
    var byNameLength = employees.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

    // INTERVIEW NOTE:
    // ThenBy() / ThenByDescending() adds secondary sort keys to an existing
    // IOrderedEnumerable<T>. It is only callable AFTER OrderBy / OrderByDescending.
    // Chaining two OrderBy() calls gives you only the second sort.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 6 — GROUPING  (GroupBy)
// ═══════════════════════════════════════════════════════════════════════════

static void Section06_Grouping()
{
    var employees = SampleData.Employees();

    // ── Basic GroupBy ────────────────────────────────────────────────────
    IEnumerable<IGrouping<string, Employee>> groups =
        employees.GroupBy(e => e.Dept);

    foreach (var group in groups)
    {
        Console.WriteLine($"Dept: {group.Key} — {group.Count()} employees");
        foreach (var emp in group)
            Console.WriteLine($"  {emp.Name} ({emp.Salary:C})");
    }

    // ── GroupBy + aggregation (classic interview pattern) ────────────────
    var report = employees
        .GroupBy(e => e.Dept)
        .Select(g => new
        {
            Dept = g.Key,
            Count = g.Count(),
            TotalPay = g.Sum(e => e.Salary),
            AvgSalary = Math.Round(g.Average(e => e.Salary), 2),
            MaxSalary = g.Max(e => e.Salary),
            TopEarner = g.OrderByDescending(e => e.Salary).First().Name
        })
        .OrderByDescending(r => r.TotalPay);

    // ── Group by multiple keys (anonymous object as key) ─────────────────
    var multiKey = employees
        .GroupBy(e => new { e.Dept, Senior = e.YearsExp >= 5 })
        .Select(g => new
        {
            g.Key.Dept,
            g.Key.Senior,
            Count = g.Count()
        });

    // ── ToLookup — like GroupBy but immediately evaluated (dictionary-like)
    ILookup<string, Employee> lookup = employees.ToLookup(e => e.Dept);
    IEnumerable<Employee> itOnly = lookup["IT"];    // O(1) access

    // INTERVIEW NOTE:
    // SQL GROUP BY → returns aggregated rows only.
    // LINQ GroupBy → returns IGrouping<TKey, TElement> objects.
    // You can still iterate individual items inside each group — more power.
    //
    // ToLookup() is the immediate-execution sibling of GroupBy().
    // Use it when you need to access groups repeatedly (avoids re-grouping).
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 7 — JOINING  (Join, GroupJoin, left outer join, cross join)
// ═══════════════════════════════════════════════════════════════════════════

static void Section07_Joining()
{
    var employees = SampleData.Employees();
    var departments = SampleData.Departments();
    var orders = SampleData.Orders();
    var customers = SampleData.Customers();

    // ── Inner join ──────────────────────────────────────────────────────
    // Returns only elements with a matching key in BOTH sequences.

    var innerJoin = orders.Join(
        customers,
        o => o.CustomerId,    // outer key selector
        c => c.Id,            // inner key selector
        (o, c) => new         // result selector
        {
            OrderId = o.Id,
            o.Total,
            Customer = c.Name,
            c.Country
        }
    );

    // ── GroupJoin — left outer join (groups matching inner elements) ─────
    // Returns ALL outer elements, with a (possibly empty) group of matches.

    var groupJoin = customers.GroupJoin(
        orders,
        c => c.Id,
        o => o.CustomerId,
        (cust, custOrders) => new
        {
            cust.Name,
            OrderCount = custOrders.Count(),
            TotalSpend = custOrders.Sum(o => o.Total)
        }
    );

    // ── Left outer join using DefaultIfEmpty ────────────────────────────
    // Returns all rows from the LEFT sequence; unmatched right → null.

    var leftOuter =
        from e in employees
        join d in departments on e.Dept equals d.Name into depts
        from dept in depts.DefaultIfEmpty()       // ← left-join magic
        select new
        {
            e.Name,
            DeptName = dept?.Name ?? "No Department",
            Location = dept?.Location ?? "Unknown"
        };

    // ── Cross join (Cartesian product) using SelectMany ──────────────────
    var sizes = new[] { "S", "M", "L" };
    var colors = new[] { "Red", "Blue", "Green" };

    var variants = sizes.SelectMany(
        s => colors,
        (s, c) => $"{s}-{c}"
    );
    // → S-Red, S-Blue, S-Green, M-Red, M-Blue, M-Green, L-Red, L-Blue, L-Green

    // INTERVIEW NOTE:
    // Join       = inner join  (missing on either side → excluded).
    // GroupJoin  = left join   (all left rows included; unmatched right = empty group).
    // DefaultIfEmpty converts the right-side IGrouping into a nullable element.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 8 — AGGREGATION
// ═══════════════════════════════════════════════════════════════════════════

static void Section08_Aggregation()
{
    var employees = SampleData.Employees();

    // ── Standard aggregates ─────────────────────────────────────────────
    int total = employees.Count();
    int itCount = employees.Count(e => e.Dept == "IT");
    decimal sumPay = employees.Sum(e => e.Salary);
    double avgPay = employees.Average(e => e.Salary);
    decimal maxPay = employees.Max(e => e.Salary);
    decimal minPay = employees.Min(e => e.Salary);
    long bigCnt = employees.LongCount();        // for lists > int.MaxValue

    // ── Aggregate — custom fold function ─────────────────────────────────
    int[] nums = { 1, 2, 3, 4, 5 };

    // Product without seed: (((1 * 2) * 3) * 4) * 5 = 120
    int product = nums.Aggregate((acc, n) => acc * n);

    // With seed and result selector
    string csv = nums.Aggregate(
        seed: "",
        func: (acc, n) => acc == "" ? $"{n}" : $"{acc},{n}",
        resultSelector: s => $"[{s}]"
    );
    // → "[1,2,3,4,5]"

    // ── GroupBy + Aggregation (common interview problem) ──────────────────
    var deptStats = employees
        .GroupBy(e => e.Dept)
        .Select(g => new
        {
            Dept = g.Key,
            HeadCount = g.Count(),
            TotalPayroll = g.Sum(e => e.Salary),
            AvgSalary = Math.Round(g.Average(e => e.Salary), 0),
            MaxSalary = g.Max(e => e.Salary),
            MinSalary = g.Min(e => e.Salary)
        })
        .OrderByDescending(x => x.TotalPayroll);

    foreach (var d in deptStats)
        Console.WriteLine($"{d.Dept,-10} Count={d.HeadCount} Avg={d.AvgSalary:C0} Max={d.MaxSalary:C0}");
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 9 — ELEMENT OPERATORS
// ═══════════════════════════════════════════════════════════════════════════
//
//  Method              Returns              Throws when…
//  ──────────────────────────────────────────────────────────────────────
//  First()             first element        sequence is empty
//  FirstOrDefault()    first, or default(T) never
//  Last()              last element         sequence is empty
//  LastOrDefault()     last, or default(T)  never
//  Single()            exactly one element  empty OR more than one match
//  SingleOrDefault()   one, or default(T)   more than one match
//  ElementAt(n)        element at index n   out of range
//  ElementAtOrDefault  element at n, or def never
// ───────────────────────────────────────────────────────────────────────────

static void Section09_ElementOperators()
{
    var employees = SampleData.Employees();

    // ── First / FirstOrDefault ───────────────────────────────────────────
    Employee first = employees.First();                         // safe — list is non-empty
    Employee? maybeZara = employees.FirstOrDefault(e => e.Name == "Zara"); // null

    // C# 10+ overload: supply a fallback value instead of null
    Employee fallback = employees.FirstOrDefault(
        e => e.Name == "Zara",
        new Employee("Unknown", "N/A", 0)
    );

    // ── Single / SingleOrDefault ─────────────────────────────────────────
    // Use for primary-key lookups — throws if the data has duplicates (catches bugs).
    Employee? byDept = employees.SingleOrDefault(e => e.Dept == "Finance" && e.Name == "Dev");

    // Would throw: employees.Single(e => e.Dept == "IT");
    //              → InvalidOperationException: sequence contains more than one element

    // ── Last / LastOrDefault ──────────────────────────────────────────────
    Employee last = employees.Last();
    Employee? lastIt = employees.LastOrDefault(e => e.Dept == "IT");

    // ── ElementAt / ElementAtOrDefault ───────────────────────────────────
    Employee third = employees.ElementAt(2);                    // 0-based
    Employee? outOfRange = employees.ElementAtOrDefault(999);   // null, no exception

    // INTERVIEW NOTE:
    // Use First()  when order matters (top of a sorted list).
    // Use Single() for PK lookups; any duplication should surface as a bug.
    // Always prefer OrDefault variants when absence is a valid scenario.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 10 — SET OPERATIONS
// ═══════════════════════════════════════════════════════════════════════════

static void Section10_SetOperations()
{
    int[] a = { 1, 2, 3, 4, 5 };
    int[] b = { 3, 4, 5, 6, 7 };

    // ── Distinct — remove duplicates from a single sequence ──────────────
    int[] dupes = { 1, 1, 2, 3, 3, 4 };
    var distinct = dupes.Distinct();           // → 1, 2, 3, 4

    // ── Union — all unique items from both sequences ──────────────────────
    var union = a.Union(b);                 // → 1, 2, 3, 4, 5, 6, 7

    // ── Intersect — items present in BOTH ────────────────────────────────
    var common = a.Intersect(b);             // → 3, 4, 5

    // ── Except — items in a but NOT in b ─────────────────────────────────
    var diff = a.Except(b);               // → 1, 2

    // ── .NET 6+ By-key variants ──────────────────────────────────────────
    var employees = SampleData.Employees();

    var distinctDepts = employees.DistinctBy(e => e.Dept);  // one row per dept

    var itEmps = employees.Where(e => e.Dept == "IT");
    var hrEmps = employees.Where(e => e.Dept == "HR");
    var allUnique = itEmps.UnionBy(hrEmps, e => e.Name);
    var sameNames = itEmps.IntersectBy(hrEmps.Select(e => e.Name), e => e.Name);
    var itNotHr = itEmps.ExceptBy(hrEmps.Select(e => e.Name), e => e.Name);

    // INTERVIEW NOTE:
    // Distinct, Union, Intersect, Except use default equality (Equals/GetHashCode).
    // For custom types, either implement IEquatable<T> or pass an IEqualityComparer.
    // The *By variants (.NET 6+) take a key selector — much cleaner for objects.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 11 — QUANTIFIERS  (Any, All, Contains)
// ═══════════════════════════════════════════════════════════════════════════

static void Section11_Quantifiers()
{
    var employees = SampleData.Employees();

    // ── Any — true if AT LEAST ONE element satisfies the predicate ───────
    bool hasIT = employees.Any(e => e.Dept == "IT");     // true
    bool isEmpty = !employees.Any();                        // false

    // ── All — true if EVERY element satisfies the predicate ──────────────
    bool allSenior = employees.All(e => e.YearsExp >= 1);    // true
    bool allIT = employees.All(e => e.Dept == "IT");     // false

    // ── Contains — true if the sequence contains a specific value ─────────
    decimal[] salaries = employees.Select(e => e.Salary).ToArray();
    bool has75k = salaries.Contains(75000m);

    // INTERVIEW NOTE:
    // Prefer Any() over Count() > 0 to check for existence.
    // Any() short-circuits after finding the first match.
    // Count() always iterates the entire sequence — much slower on large data.
    //
    // In EF Core:
    //   Any()   → generates:  SELECT CASE WHEN EXISTS(...)  (more efficient)
    //   Count() → generates:  SELECT COUNT(*)
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 12 — PARTITIONING  (Skip, Take, Chunk, SkipWhile, TakeWhile)
// ═══════════════════════════════════════════════════════════════════════════

static void Section12_Partitioning()
{
    var employees = SampleData.Employees().OrderBy(e => e.Name).ToList();

    // ── Skip & Take — standard paging ────────────────────────────────────
    int pageSize = 3;

    var page1 = employees.Skip((1 - 1) * pageSize).Take(pageSize); // items 1-3
    var page2 = employees.Skip((2 - 1) * pageSize).Take(pageSize); // items 4-6

    // Helper method for paging
    static IEnumerable<T> GetPage<T>(IEnumerable<T> src, int page, int size)
        => src.Skip((page - 1) * size).Take(size);

    // ── .NET 6+ TakeLast / SkipLast ──────────────────────────────────────
    var lastTwo = employees.TakeLast(2);
    var allButLast = employees.SkipLast(1);

    // ── .NET 6+ Chunk — split into fixed-size batches ────────────────────
    // Useful for bulk API calls, batch DB inserts, etc.
    foreach (var batch in employees.Chunk(3))
    {
        Console.WriteLine($"Processing batch of {batch.Length}");
        // ProcessBatch(batch);
    }

    // ── TakeWhile / SkipWhile — predicate-based partitioning ─────────────
    int[] nums = { 2, 4, 6, 7, 8, 10 };

    // TakeWhile: takes elements until the predicate is false, then stops
    var evens = nums.TakeWhile(n => n % 2 == 0);   // → 2, 4, 6

    // SkipWhile: skips elements until the predicate is false, then takes rest
    var rest = nums.SkipWhile(n => n % 2 == 0);   // → 7, 8, 10

    // INTERVIEW NOTE:
    // TakeWhile / SkipWhile check the predicate in order and stop at the
    // first failure — they do NOT filter all elements like Where does.
    // nums = {2, 4, 7, 6}:  TakeWhile(even) → 2, 4  (stops at 7, ignores 6)
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 13 — DEFERRED vs IMMEDIATE EXECUTION
// ═══════════════════════════════════════════════════════════════════════════
//
//  DEFERRED (lazy): Where, Select, OrderBy, GroupBy, Skip, Take, SelectMany
//     → query object is created; nothing runs until the result is consumed.
//
//  IMMEDIATE: ToList, ToArray, ToDictionary, Count, Sum, First, Any, ...
//     → query executes right now and returns a concrete value / collection.
// ───────────────────────────────────────────────────────────────────────────

static void Section13_DeferredExecution()
{
    var numbers = new List<int> { 1, 2, 3 };

    // ❶ Query is DEFINED — not executed yet
    var query = numbers.Where(n =>
    {
        Console.WriteLine($"  Filtering {n}");
        return n > 1;
    });

    // ❷ Mutate the source BEFORE enumeration
    numbers.Add(4);

    Console.WriteLine("Iterating now:");
    // ❸ Query executes HERE — picks up the added 4
    foreach (var n in query)
        Console.WriteLine($"  Result: {n}");
    // Output: Filtering 1, Filtering 2, Filtering 3, Filtering 4
    // Results: 2, 3, 4

    // ── Multiple enumeration bug ──────────────────────────────────────────
    // The query runs TWICE — every foreach or terminal call re-executes it.
    var expensive = numbers.Where(n => n > 1);

    int count1 = expensive.Count();   // ← executes the query (pass 1)
    int count2 = expensive.Count();   // ← executes the query again (pass 2)

    // Fix: materialise once, reuse the list
    var materialised = numbers.Where(n => n > 1).ToList();  // ← ONE pass
    int c1 = materialised.Count;      // free — no query re-execution
    int c2 = materialised.Count;      // free

    // ── Immediate execution operators ────────────────────────────────────
    var employees = SampleData.Employees();

    List<Employee> list = employees.Where(e => e.Salary > 50000).ToList();
    Employee[] arr = employees.OrderBy(e => e.Name).ToArray();
    Dictionary<string, Employee> dict = employees.ToDictionary(e => e.Name);
    HashSet<string> depts = employees.Select(e => e.Dept).ToHashSet();
    int cnt = employees.Count(e => e.Dept == "IT");
    bool any = employees.Any(e => e.Salary > 100000);
    Employee top = employees.OrderByDescending(e => e.Salary).First();
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 14 — IEnumerable<T> vs IQueryable<T>
// ═══════════════════════════════════════════════════════════════════════════
//
//  IEnumerable<T>
//    • In System.Collections.Generic
//    • LINQ operators compile to C# delegates
//    • Entire sequence is processed in C# memory
//    • Use for in-memory collections
//
//  IQueryable<T>  (extends IEnumerable<T>)
//    • In System.Linq
//    • LINQ operators build an Expression<Func<T,bool>> tree
//    • The provider (EF Core, MongoDB Driver, etc.) translates the tree
//      to the native query language (SQL, MQL, etc.) and executes it
//      on the server — only matching rows travel over the wire
//    • Use for remote data sources
// ───────────────────────────────────────────────────────────────────────────

static void Section14_IEnumerableVsIQueryable()
{
    var employees = SampleData.Employees();       // in-memory List<T>

    // ── IEnumerable — delegates execute in C# ─────────────────────────────
    IEnumerable<Employee> query1 = employees
        .Where(e => e.Dept == "IT")               // Func<Employee,bool>
        .OrderByDescending(e => e.Salary);        // runs here in C#

    // ── AsQueryable — wrap IEnumerable to get IQueryable for testing ──────
    IQueryable<Employee> localQueryable = employees.AsQueryable();

    // ── AsEnumerable — pull an IQueryable into C# memory partway ─────────
    // Use when you need a C# function that cannot be translated to SQL.
    //
    //   db.Orders
    //     .Where(o => o.Total > 1000)     ← SQL WHERE (runs on DB)
    //     .AsEnumerable()                 ← switch to C# from here
    //     .Where(o => CustomCSharpLogic(o)); ← runs in C# memory
    //
    // Without AsEnumerable(), EF Core would try to translate CustomCSharpLogic
    // to SQL and throw a NotSupportedException.

    // ── The classic ToList() too early gotcha ─────────────────────────────
    //
    // Assuming db is an EF Core DbContext:
    //
    // ❌ BAD  — loads ALL employees into memory, then filters in C#
    // var bad  = db.Employees.ToList().Where(e => e.Dept == "IT");
    //
    // ✅ GOOD — WHERE clause travels to the database
    // var good = db.Employees.Where(e => e.Dept == "IT").ToList();
    //
    // The difference can be millions of rows vs. a handful.

    Console.WriteLine("IEnumerable vs IQueryable demo complete.");
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 15 — LINQ PROVIDERS
// ═══════════════════════════════════════════════════════════════════════════


// ── 15a. LINQ TO OBJECTS ─────────────────────────────────────────────────────
//
//  • Interface  : IEnumerable<T>
//  • Namespace  : System.Linq  (extension methods on IEnumerable<T>)
//  • Package    : built-in (.NET 3.5+)
//  • Execution  : C# delegates compiled to IL, run in memory
//  • Works on   : List<T>, Array, HashSet<T>, Dictionary<K,V>, any IEnumerable
//
// This is the provider used in all the sections above.
// No additional example needed here.


// ── 15b. LINQ TO DATASET ─────────────────────────────────────────────────────
//
//  • Interface  : IEnumerable<DataRow>
//  • Namespace  : System.Data
//  • Package    : System.Data.DataSetExtensions (built-in)
//  • Bridge     : DataTable.AsEnumerable()
//  • Used in    : legacy ADO.NET applications

static void Section15b_LinqToDataSet()
{
    // Build a DataTable (simulates ADO.NET result from a stored procedure)
    DataTable dt = new DataTable("Employees");
    dt.Columns.Add("Name", typeof(string));
    dt.Columns.Add("Dept", typeof(string));
    dt.Columns.Add("Salary", typeof(decimal));

    dt.Rows.Add("Asha", "IT", 75000m);
    dt.Rows.Add("Raj", "IT", 45000m);
    dt.Rows.Add("Maya", "HR", 60000m);
    dt.Rows.Add("Dev", "Finance", 70000m);

    // ── AsEnumerable() — the bridge ──────────────────────────────────────
    // DataTable does NOT implement IEnumerable<DataRow> by itself.
    // AsEnumerable() is the extension method that makes LINQ work on it.

    IEnumerable<DataRow> rows = dt.AsEnumerable();

    // ── Filter and project ────────────────────────────────────────────────
    var itTeam = dt.AsEnumerable()
        .Where(r => r.Field<string>("Dept") == "IT")
        .OrderByDescending(r => r.Field<decimal>("Salary"))
        .Select(r => new
        {
            Name = r.Field<string>("Name"),
            Salary = r.Field<decimal>("Salary")
        });

    Console.WriteLine("=== LINQ to DataSet — IT Team ===");
    foreach (var e in itTeam)
        Console.WriteLine($"  {e.Name}: {e.Salary:C}");

    // ── GroupBy on DataRows ───────────────────────────────────────────────
    var grouped = dt.AsEnumerable()
        .GroupBy(r => r.Field<string>("Dept"))
        .Select(g => new
        {
            Dept = g.Key,
            Count = g.Count(),
            AvgSalary = g.Average(r => r.Field<decimal>("Salary"))
        });

    Console.WriteLine("=== Department averages ===");
    foreach (var d in grouped)
        Console.WriteLine($"  {d.Dept}: avg={d.AvgSalary:C0}, count={d.Count}");

    // ── Copy result back into a DataTable ────────────────────────────────
    // CopyToDataTable() requires at least one result row; guard accordingly.
    var itRows = dt.AsEnumerable().Where(r => r.Field<string>("Dept") == "IT");
    if (itRows.Any())
    {
        DataTable filteredTable = itRows.CopyToDataTable();
        Console.WriteLine($"Filtered DataTable rows: {filteredTable.Rows.Count}");
    }

    // INTERVIEW NOTE:
    // Field<T>() is the type-safe, null-aware accessor for DataRow columns.
    // Use it instead of (T)row["ColumnName"] to get proper null handling.
}


// ── 15c. LINQ TO ENTITIES (EF CORE) — CONCEPTUAL ─────────────────────────────
//
//  • Interface  : IQueryable<T>  (DbSet<T> implements IQueryable<T>)
//  • Namespace  : Microsoft.EntityFrameworkCore
//  • Package    : Microsoft.EntityFrameworkCore + provider (e.g. Npgsql, SqlServer)
//  • Execution  : DEFERRED — C# LINQ expression tree → SQL via EF Core provider
//  • Works on   : SQL Server, PostgreSQL, MySQL, SQLite, Cosmos DB, and more
//
//  NOTE: The code below is commented out because it needs an actual DbContext
//  and database connection. Add the EF Core NuGet package to run it.

/*
// ── EF Core DbContext setup ───────────────────────────────────────────────────
public class AppDbContext : DbContext
{
    public DbSet<OrderEntity> Orders   { get; set; } = null!;
    public DbSet<CustomerEntity> Customers { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder opts)
        => opts.UseSqlServer("your_connection_string");
}

public class OrderEntity
{
    public int     Id         { get; set; }
    public decimal Total      { get; set; }
    public string  Status     { get; set; } = "";
    public int     CustomerId { get; set; }
    public CustomerEntity Customer { get; set; } = null!;
}

public class CustomerEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
}

static async Task Section15c_LinqToEntities()
{
    using var db = new AppDbContext();

    // ── DbSet<T> is IQueryable<T> — no SQL runs yet ──────────────────────
    IQueryable<OrderEntity> query = db.Orders              // IQueryable
        .Where(o => o.Total > 1000)         // adds WHERE Total > 1000
        .OrderByDescending(o => o.Total)    // adds ORDER BY Total DESC
        .Take(10)                           // adds TOP 10
        .Select(o => new {
            o.Id,
            o.Total,
            o.Status,
            CustomerName = o.Customer.Name  // adds JOIN Customers
        });

    // Generated SQL (SQL Server):
    // SELECT TOP(10) o.Id, o.Total, o.Status, c.Name AS CustomerName
    // FROM Orders AS o
    // INNER JOIN Customers AS c ON o.CustomerId = c.Id
    // WHERE o.Total > 1000
    // ORDER BY o.Total DESC

    var results = await query.ToListAsync();  // ← SQL executes here

    // ── GroupBy in EF Core ────────────────────────────────────────────────
    var stats = await db.Orders
        .GroupBy(o => o.Status)
        .Select(g => new {
            Status = g.Key,
            Count  = g.Count(),
            Total  = g.Sum(o => o.Total)
        })
        .ToListAsync();

    // SQL: SELECT Status, COUNT(*), SUM(Total) FROM Orders GROUP BY Status

    // ── ToList() too early — the classic gotcha ───────────────────────────
    // BAD:  Loads ENTIRE Orders table then filters in C#
    var bad  = await db.Orders.ToListAsync();
    var filtered = bad.Where(o => o.Total > 1000);  // C# delegate on full table

    // GOOD: WHERE runs on the database server — only matching rows fetched
    var good = await db.Orders.Where(o => o.Total > 1000).ToListAsync();
}
*/


// ── 15d. LINQ TO XML ─────────────────────────────────────────────────────────
//
//  • Interface  : IEnumerable<XElement>
//  • Namespace  : System.Xml.Linq
//  • Package    : built-in (.NET 3.5+)
//  • Execution  : in-memory XML tree traversal (deferred for lazy navigation)
//  • Key types  : XDocument, XElement, XAttribute, XText
//  • Replaced   : XmlDocument / XmlNode (older, more verbose API)

static void Section15d_LinqToXml()
{
    // ── Parse XML ─────────────────────────────────────────────────────────
    string xml = """
        <employees>
          <employee dept="IT">
            <name>Asha</name>
            <salary>75000</salary>
          </employee>
          <employee dept="IT">
            <name>Raj</name>
            <salary>45000</salary>
          </employee>
          <employee dept="HR">
            <name>Maya</name>
            <salary>60000</salary>
          </employee>
        </employees>
        """;

    XDocument doc = XDocument.Parse(xml);

    // ── Query with LINQ — Descendants() returns IEnumerable<XElement> ─────
    var itStaff = doc
        .Descendants("employee")
        .Where(e => (string?)e.Attribute("dept") == "IT")
        .Select(e => new
        {
            Name = (string?)e.Element("name"),
            Salary = (int?)e.Element("salary")
        })
        .OrderByDescending(e => e.Salary);

    Console.WriteLine("=== LINQ to XML — IT Staff ===");
    foreach (var e in itStaff)
        Console.WriteLine($"  {e.Name}: {e.Salary:C}");

    // ── Aggregate across XML elements ─────────────────────────────────────
    double avgSalary = doc
        .Descendants("salary")
        .Select(s => (double)s)
        .Average();
    Console.WriteLine($"  Company avg salary: {avgSalary:C0}");

    // ── Create XML with the fluent API ────────────────────────────────────
    XDocument report = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XElement("report",
            new XAttribute("generated", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            itStaff.Select(e =>
                new XElement("entry",
                    new XAttribute("name", e.Name!),
                    new XElement("salary", e.Salary)
                )
            )
        )
    );

    Console.WriteLine("\n=== Generated XML ===");
    Console.WriteLine(report);

    // ── Load from and save to file ────────────────────────────────────────
    // XDocument fromFile = XDocument.Load("employees.xml");
    // report.Save("report.xml");

    // ── XElement navigation helpers ───────────────────────────────────────
    XElement? root = doc.Root;                       // <employees>
    var allNames = doc.Descendants("name");        // all <name> elements
    var depts = doc.Root!.Elements("employee")  // direct child elements
                         .Select(e => e.Attribute("dept")?.Value)
                         .Distinct();

    // INTERVIEW NOTE:
    // Descendants("tag") — searches the ENTIRE subtree for the tag name.
    // Elements("tag")    — searches only DIRECT children.
    // Ancestors("tag")   — navigates UP the tree to find parent elements.
    // All three return IEnumerable<XElement> — fully composable with LINQ.
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 16 — EXPRESSION TREES
// ═══════════════════════════════════════════════════════════════════════════
//
//  An expression tree is a data structure that represents C# code as a
//  tree of objects — it can be inspected, modified, and compiled at runtime.
//
//  IEnumerable uses delegates (compiled code you can CALL).
//  IQueryable  uses expressions (data structures you can ANALYSE).
//
//  This is why EF Core can translate Where(e => e.Salary > 1000) to SQL:
//  it receives an Expression<Func<Employee,bool>>, walks the tree, and
//  emits the equivalent SQL fragment.
// ───────────────────────────────────────────────────────────────────────────

static void Section16_ExpressionTrees()
{
    // ── Delegate vs Expression ────────────────────────────────────────────

    // Delegate — compiled, opaque, you can only CALL it
    Func<int, bool> del = x => x > 5;
    bool result = del(10);            // true — we can call it

    // Expression — an inspectable data structure representing the same logic
    Expression<Func<int, bool>> expr = x => x > 5;

    // Inspect the expression tree
    Console.WriteLine($"Body:     {expr.Body}");         // (x > 5)
    Console.WriteLine($"NodeType: {expr.Body.NodeType}"); // GreaterThan
    Console.WriteLine($"Left:     {((BinaryExpression)expr.Body).Left}");  // x
    Console.WriteLine($"Right:    {((BinaryExpression)expr.Body).Right}"); // 5

    // Compile an expression to a delegate when you need to call it
    Func<int, bool> compiled = expr.Compile();
    bool res2 = compiled(10);         // true

    // ── Building an expression tree manually ──────────────────────────────
    //
    //  Equivalent to:  (int x) => x > 5
    //
    ParameterExpression param = Expression.Parameter(typeof(int), "x");
    ConstantExpression constant = Expression.Constant(5);
    BinaryExpression body = Expression.GreaterThan(param, constant);

    var manual = Expression.Lambda<Func<int, bool>>(body, param);
    Console.WriteLine($"Manual: {manual}");   // x => (x > 5)
    bool m = manual.Compile()(10);            // true

    // ── How IQueryable.Provider uses expression trees ─────────────────────
    //
    //   IQueryable<T> holds two things:
    //     1. Expression  — the accumulated expression tree for this query
    //     2. Provider    — the engine that translates that tree
    //
    //   When you call db.Orders.Where(o => o.Total > 1000):
    //     → The EF Core provider wraps o.Total > 1000 in a MethodCallExpression
    //     → Chains it onto the existing expression tree
    //     → On ToList(), the provider's ExpressionVisitor walks the tree
    //       and emits:  WHERE Total > 1000
    //
    //   Third-party providers (MongoDB, CosmosDB) do the same thing but
    //   emit their own query syntax instead of SQL.

    // ── Inspecting a query's expression tree ──────────────────────────────
    var employees = SampleData.Employees().AsQueryable();

    IQueryable<Employee> q = employees
        .Where(e => e.Dept == "IT")
        .OrderByDescending(e => e.Salary);

    Console.WriteLine($"\nQuery expression tree:\n{q.Expression}");
    // Prints the full MethodCall expression tree EF Core would translate

    // INTERVIEW NOTE:
    // Lambda → Func<T>: compiled code you can only call.
    // Lambda → Expression<Func<T>>: data you can inspect and translate.
    // IQueryable providers (EF Core, MongoDB) require Expression because
    // they cannot call .NET code on a remote server — they need the data
    // to build their own query (SQL / MQL / etc.).
}


// ═══════════════════════════════════════════════════════════════════════════
// SECTION 17 — INTERVIEW Q&A QUICK REFERENCE
// ═══════════════════════════════════════════════════════════════════════════

/*
Q1: What is LINQ?
    Language Integrated Query — a unified C# API for querying any data
    source (in-memory collections, databases, XML) using the same syntax.
    Lives in System.Linq; works via extension methods on IEnumerable<T>
    and IQueryable<T>.

Q2: What is the difference between query syntax and method syntax?
    Both compile to identical IL. Query syntax is syntactic sugar the
    compiler translates into method calls. Method syntax is preferred in
    practice because it supports ALL LINQ operators — several (Count, Sum,
    Any, Aggregate, Skip, Take) have no query-syntax counterpart.

Q3: What is deferred execution?
    Most LINQ operators (Where, Select, OrderBy, GroupBy…) build a query
    object but do NOT execute until the result is iterated (foreach) or
    a terminal operator is called (ToList, Count, First, Sum…).
    Key consequence: enumerating a deferred query twice runs it twice.
    Fix → materialise with .ToList() once and reuse the list.

Q4: What is the difference between First() and Single()?
    First()  — returns the first matching element; doesn't care if more exist.
    Single() — asserts exactly one match; throws InvalidOperationException
               if 0 or 2+ elements match. Use for PK lookups to surface
               data bugs early.
    Both have OrDefault variants that return default(T) instead of throwing
    when no match is found (but Single/OrDefault still throws on 2+).

Q5: What is the difference between IEnumerable<T> and IQueryable<T>?
    IEnumerable<T> — LINQ operators compile to C# delegates; runs in memory.
    IQueryable<T>  — LINQ operators build an expression tree; the provider
                     (EF Core etc.) translates it to SQL and runs it on the DB.
    Mistake: calling .ToList() before .Where() on an IQueryable loads the
    ENTIRE table into memory, then filters in C# — a classic performance bug.

Q6: What are LINQ providers?
    A provider is the engine that translates LINQ expression trees into a
    target query language:
      • LINQ to Objects  — IEnumerable<T> → C# delegates, runs in memory
      • LINQ to Entities — IQueryable<T>  → SQL via EF Core
      • LINQ to XML      — IEnumerable<XElement> → XDocument traversal
      • LINQ to DataSet  — IEnumerable<DataRow>  → ADO.NET DataTable
    Third-party: MongoDB Driver, Azure Cosmos SDK, NHibernate, OData…

Q7: Difference between Select and SelectMany?
    Select    → 1-to-1 mapping, returns IEnumerable<TResult>
    SelectMany → 1-to-many mapping, then flattens one level.
    e.g. departments.Select(d => d.Skills) → IEnumerable<string[]>
         departments.SelectMany(d => d.Skills) → IEnumerable<string>

Q8: Why is Any() preferred over Count() > 0?
    Any() short-circuits after the first match — O(1) for non-empty lists.
    Count() always iterates the full sequence — O(n).
    In EF Core, Any() generates SELECT … WHERE EXISTS (…), which the DB
    optimiser handles more efficiently than SELECT COUNT(*).

Q9: What is an expression tree?
    A data structure representing C# code as a tree of objects that can be
    inspected, transformed, and compiled at runtime. IQueryable providers
    receive Expression<Func<T,bool>> (not compiled Func<T,bool>), walk the
    tree, and emit the equivalent SQL / MQL / etc.

Q10: Write a LINQ query — top 3 departments by average salary.
    employees
        .GroupBy(e => e.Dept)
        .Select(g => new {
            Dept      = g.Key,
            AvgSalary = g.Average(e => e.Salary)
        })
        .OrderByDescending(x => x.AvgSalary)
        .Take(3);
    Pattern: GroupBy → Select (aggregate) → OrderBy → Take.
*/

// ─────────────────────────────────────────────────────────────────────────────
// END OF FILE
// ─────────────────────────────────────────────────────────────────────────────