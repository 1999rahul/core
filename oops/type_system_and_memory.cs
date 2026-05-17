using System.Collections;

namespace oops
{
    // =========================================== Value Types vs Reference Types — Stack vs Heap ===========================================

    // The One-Line Difference
    // Value Type     →  stores the ACTUAL DATA directly
    // Reference Type →  stores the ADDRESS (pointer) to where data lives

    // Understanding Memory (Stack vs Heap).
    // Before types, you need to understand where data lives in memory.

    //  -- Stack -- 
    // Fast — works like a pile of plates (LIFO — Last In First Out)
    // Automatically managed — data removed when method ends
    // Fixed size — must know size at compile time
    // Thread safe — each thread has its own stack
    // Limited size — typically 1MB to 8MB
    // No flexibility — cannot resize or store large data

    // -- Heap --
    // Large — limited only by system RAM
    // Flexible — objects can be any size 
    // Shared — all threads share one heap
    // Slower — needs Garbage Collector to clean up
    // Overhead — GC causes occasional pauses
    // Fragmentation — memory can get scattered


    //    STACK                             HEAP
    //──────────────────           ──────────────────────────────
    //│  int x = 10      │         │  [Person object]           │
    //│  value: 10       │         │  Name = "Ravi"             │
    //├──────────────────┤         │  Age  = 25                 │
    //│  bool flag = true|         │  [Address: 0x1A2B]         │
    //│  value: true     │         │                            │
    //├──────────────────┤         │  [Order object]            │
    //│  Person p        │         │  Amount = 5000             │
    //│  ref: 0x1A2B     │────────>│  [Address: 0x3C4D]         │
    //──────────────────--          ──────────────────────────────
    //Cleaned automatically        Cleaned by Garbage Collector
    //when method ends             when no references remain


    // Value Types

    // All of these are VALUE TYPES
    //int age = 25;
    //double price = 99.99;
    //bool active = true;
    //char grade = 'A';
    //float rate = 3.14f;
    //decimal salary = 50000.00m;

    // Structs are value types
    struct Point
    {
        public int X;
        public int Y;
    }

    // Enums are value types
    enum Status { Pending, Active, Closed }

    // Where do they live? -> They live in the stack

    // Reference Types
    // What are they?

    // All of these are REFERENCE TYPES
    //    class Person { }       // classes
    //    string name = "Ravi";    // string (special case — discussed below)
    //    int[] nums = new int[5]; // arrays
    //    object obj = new object();
    //    List<int> list = new List<int>();

    // Where do they live? -> They live in the heap memory

    // Method Call Behavior

    //void AddTen(int number)
    //{
    //    number = number + 10;  // modifying the LOCAL COPY
    //    Console.WriteLine($"Inside method: {number}");  // 110
    //}

    //int x = 100;
    //AddTen(x);
    //Console.WriteLine($"Outside method: {x}"); 

    //    void ChangeName(Person p)
    //    {
    //        p.Name = "Mohan";  // modifying the ACTUAL OBJECT on heap
    //    }

    //    Person person = new Person();
    //    person.Name = "Ravi";

    //    ChangeName(person);
    //    Console.WriteLine(person.Name);  // Mohan — changed!




    // ----- The ref Keyword — Force Value Type to Behave Like Reference ----
    //void AddTen(ref int number)
    //{
    //    number = number + 10;  // modifies the ORIGINAL — not a copy
    //}

    //int x = 100;
    //AddTen(ref x);
    //Console.WriteLine(x); 

    // The out Keyword — Must Assign Inside Method

    //    void GetValues(out int min, out int max)
    //    {
    //        min = 10;   // MUST assign — out guarantees a value comes back
    //        max = 100;
    //    }

    //    int minimum, maximum;            // not initialized — that's fine with out
    //    GetValues(out minimum, out maximum);
    //    Console.WriteLine(minimum);  // 10
    //    Console.WriteLine(maximum);  // 100

    // When should you use which?

    // Use out when your method needs to return more than one piece of data, or when the method's primary job is to generate a new result (like int.TryParse).
    // Use ref when the method needs to alter an existing variable that already holds meaningful data(like a Swap(ref int a, ref int b) method or a game loop updating a player's position




    // --------String — The Special Reference Type------------
    // String is a reference type that behaves like a value type because it is immutable

    //    string a = "Hello";
    //    string b = a;       // b points to SAME object on heap

    //    b = "World";        // does NOT modify existing string
    //                    // creates a NEW string object on heap
    //                    // b now points to new object

    //Console.WriteLine(a);  // Hello — unchanged
    //Console.WriteLine(b);  // World


    // String Immutability Performance Impact

    // BAD — creates 10,000 new string objects on heap
    //      string result = "";
    //      for (int i = 0; i< 10000; i++)
    //      {
    //          result = result + i.ToString();  // new object every iteration!
    //      }

    //// GOOD — StringBuilder modifies in place — one object
    //  StringBuilder sb = new StringBuilder();
    //  for (int i = 0; i < 10000; i++)
    //  {
    //      sb.Append(i);  // modifies same object — no new heap allocation
    //  }
    //  string result = sb.ToString();


    // ------------- Nullable Value Types ------------------------
    // Value types cannot be null by default — but you can allow it with ?:
    // int age = null;   //  COMPILE ERROR — int cannot be null
    // int? age = null;   //  Nullable int — allowed

    // Common use case — database values that might be missing
    //    int? dbValue = GetFromDatabase();  // might return null if no record

    //if (dbValue.HasValue)
    //    Console.WriteLine(dbValue.Value);  // safe access
    //else
    //    Console.WriteLine("No value found");

    //// Shorthand with null coalescing
    //int result = dbValue ?? 0;  // use 0 if null

    // Why Value Types Cannot Be Null By Default
    // First — Understand What Null Actually Means -> null = "I am pointing to NOTHING — no object exists in memory"

    // Null is a concept that means "no reference" — it only makes sense when you have something that points to a memory address.

    //    Reference Type  →  stores a MEMORY ADDRESS  →  address can be empty(null)  
    //    Value Type      →  stores ACTUAL DATA        →  data cannot be "nothing" 

    //int x = 0;     // 4 bytes in memory — stores binary 00000000
    //               // even "empty" int = 0 — never truly nothing

    //int x = null;  // what binary pattern represents null for an int?
    //               // there is NO such pattern — int has no null state
    //               // every possible bit pattern = a valid integer

    // Since value types cannot be null, the compiler assigns a default value instead:
    //int x;      // default = 0
    //double d;      // default = 0.0
    //bool b;      // default = false
    //char c;      // default = '\0'  (empty character)
    //struct s;      // default = all fields set to their defaults

    //// Reference types default to null — they CAN be nothing
    //Person p;      // default = null  (points to nothing)
    //string s;      // default = null
    //int[] arr;    // default = null

    //Value types ALWAYS have a value — compiler guarantees it
    //Reference types default to null — no object until you create one

    // So How Does ? Work — Nullable Value Types
    // When you write int? the compiler secretly wraps your value type inside a special struct called Nullable<T>:

    // int?  age = null;
    // is exactly the same as:
    // Nullable<int> age = null;

    // Revision and Interview Questions are left -> 30 minutes
    // =========================================== END ===============================================================



    // ========================================== STRING IMUTABLITY ===============================
    // What Does Immutable Mean?
    // string name = "Ravi";
    // name = "Mohan";        // looks like we changed it — but we did NOT

    // What actually happened:
    // 1. "Ravi"  → created on heap — stays there
    // 2. "Mohan" → NEW object created on heap
    // 3. name variable now points to "Mohan"
    // 4. "Ravi"  → still sitting on heap — waiting for GC

    // -------------------------------------------------------------
    // Reason 1 — String Interning (Memory Optimization)
    // C# maintains a special place in memory called the String Intern Pool.
    // When you create the same string literal twice, C# does NOT create two objects — it reuses the same one.
    // string a = "Hello";
    // string b = "Hello";  // does NOT create new object — reuses "Hello" from pool
    // Console.WriteLine(object.ReferenceEquals(a, b));  // True — same object!

    // Why Immutability is Critical Here?

    //    string a = "Hello";
    //    string b = "Hello";  // same object as a

    //    // If strings were MUTABLE:
    //    a[0] = 'J';  // imaginary — changes "Hello" to "Jello"

    //// Now b is also "Jello" — we never touched b!
    //   Console.WriteLine(b);  // "Jello" — DISASTER ❌

    //// Because a and b point to SAME object
    //// Mutating via a would silently corrupt b
    //---------------------------------------------------------------------

    // --------------------------- Reason 2 — Thread Safety ----------------
    // Multiple threads can read the same string simultaneously with zero locks — because nobody can modify it.

    //    string config = "DatabaseConnection=server01";

    //    // Thread 1 reads config
    //    Task.Run(() => {
    //    Console.WriteLine(config);  // safe — nobody can change this 
    //});

    //// Thread 2 reads config simultaneously
    //Task.Run(() => {
    //    Console.WriteLine(config);  // safe — same guarantee 
    //});

    //// If strings were mutable:
    //// Thread 1 reads  "DatabaseConnection=server01"
    //// Thread 2 changes it to "DatabaseConnection=HACKED" mid-read
    //// Thread 1 now has corrupted data ❌ — race condition
    ///

    //    IMMUTABLE string — Thread Safety:
    // ──────────────────────────────────────────
    //Thread 1 ──→ reads "server01"  safe
    //Thread 2 ──→ reads "server01"  safe
    //Thread 3 ──→ reads "server01"  safe
    //Nobody can modify — no locks needed — no race conditions
    //──────────────────────────────────────────

    //MUTABLE string — Thread Disaster:
    //──────────────────────────────────────────
    //Thread 1 ──→ reads  "server01"
    //Thread 2 ──→ writes "HACKED"    ← corrupts mid-read 
    //Thread 1 ──→ gets   "HACKEDr01" ← garbage data 
    //──────────────────────────────────────────

    // ------------------------------------------------------------------------------------------

    // -------------------------------------Reason 4 — Hashcode Stability------------------------
    // Strings are used as Dictionary keys everywhere. Hashcode must never change for a key.

    // Dictionary<string, int> scores = new Dictionary<string, int>();
    // scores["Ravi"] = 100;
    // Dictionary stores "Ravi" at position based on its hashcode
    // hashcode("Ravi") = 12345  →  stored at bucket 12345

    // If strings were mutable and someone changed the key "Ravi" to "Mohan":
    // hashcode("Mohan") = 99999  →  different bucket!
    // scores["Ravi"]  →  looks in bucket 12345 — but it will find the changed key .i.e. "mohan" and will return empty
    // scores["Mohan"] →  looks in bucket 99999 — but their might be another object present at that location
    // The entry is LOST — can never be found again



    // How Does a Dictionary Actually Work Internally?
    // Dictionary is a Hash Table — Array of Buckets

    // Dictionary<string, int> scores = new Dictionary<string, int>();

    // INTERNALLY — Dictionary creates an array of BUCKETS:

    // Bucket[0]  →  empty
    // Bucket[1]  →  empty
    // Bucket[2]  →  empty
    // Bucket[3]  →  empty
    // Bucket[4]  →  empty
    // Bucket[5]  →  empty
    // Bucket[6]  →  empty
    // Bucket[7]  →  empty
    //...
    // Bucket[N]  →  empty

    // How INSERT Works — Step by Step
    // scores["Ravi"] = 100;

    //Step 1: Compute hashcode of key
    //    "Ravi".GetHashCode() = 1234567

    //Step 2: Find the bucket
    //        bucketIndex = 1234567 % totalBuckets
    //        bucketIndex = 1234567 % 8 = 7
    //        → goes into Bucket[7]

    //Step 3: Store key + value in that bucket
    //        Bucket[7] → { key="Ravi", value=100 }


    //BUCKETS after insert:
    //─────────────────────────────
    //Bucket[0]  →  empty
    //Bucket[1]  →  empty
    //Bucket[2]  →  empty
    //Bucket[3]  →  empty
    //Bucket[4]  →  empty
    //Bucket[5]  →  empty
    //Bucket[6]  →  empty
    //Bucket[7]  →  { "Ravi" = 100 }   ← stored here


    // How LOOKUP Works — Step by Step
    // int score = scores["Ravi"];

    //    Step 1: Compute hashcode of key
    //        "Ravi".GetHashCode() = 1234567

    //Step 2: Find the bucket — SAME formula
    //        bucketIndex = 1234567 % 8 = 7
    //        → go to Bucket[7]

    //Step 3: Check if key matches — return value
    //        Bucket[7] → { "Ravi" = 100 }  found!
    //        return 100

    //Total comparisons needed = 1  ← lightning fast 
    //No need to scan all buckets

    // The Disaster — What if String Was Mutable?
    // Now imagine strings COULD be mutated — and someone changes the key after insertion.

    // Imagine this was possible (it is NOT in C# — just for understanding)
    //    string key = "Ravi";
    //    scores[key] = 100;

    //// Key "Ravi" stored at Bucket[7]
    //// hashcode("Ravi") = 1234567 → 1234567%8 = 7 → Bucket[7] 

    //// Now imagine mutating the key directly
    //key.MutateInPlace('M', 0);  // imaginary — changes "Ravi" to "Mavi"

    //// What just happened internally?

    //    BEFORE mutation:
    //─────────────────────────────────────────────────
    //key variable  →  points to string object "Ravi"
    //Bucket[7]     →  { key="Ravi", value=100 }
    //hashcode of key = 1234567 → bucket 7 
    //─────────────────────────────────────────────────

    //AFTER mutation (imaginary):
    //─────────────────────────────────────────────────
    //key variable  →  points to SAME string object "Mavi"  ← content changed
    //Bucket[7]     →  { key="Mavi", value=100 }            ← bucket unchanged!
    //hashcode of "Mavi" = 9876543 → 9876543%8 = 3          ← different bucket!
    //─────────────────────────────────────────────────

    // Now Try to Find It

    //    LOOKUP for "Mavi":
    //────────────────────────────────────────────────────
    //Step 1: hashcode("Mavi") = 9876543
    //Step 2: bucketIndex = 9876543 % 8 = 3
    //Step 3: Go to Bucket[3] → EMPTY 

    //Entry is at Bucket[7] — but we are looking at Bucket[3]
    //We will NEVER find it 
    //────────────────────────────────────────────────────

    //LOOKUP for "Ravi" (original name):
    //────────────────────────────────────────────────────
    //Step 1: hashcode("Ravi") = 1234567
    //Step 2: bucketIndex = 1234567 % 8 = 7
    //Step 3: Go to Bucket[7] → { key="Mavi", value=100 }
    //Step 4: Compare "Ravi" == "Mavi" → FALSE 
    //        Key does not match — entry not found 
    //────────────────────────────────────────────────────

    //The entry { "Ravi" = 100 } is PERMANENTLY LOST
    //It exists in memory but can NEVER be retrieved  <---- THE MAIN PROBLEM


    // Question -> Does the stack memory is only available for functions?
    // --------------------------------------END-------------------------------------------------
    // ========================================== END =============================================


    // ============================================== BOXING AND UNBOXING ====================================
    class BoxingUnboxing
    {
        public BoxingUnboxing()
        {
            // ─────────────────────────────────────────────────────────────
            // 1. WHAT IS BOXING?
            // ─────────────────────────────────────────────────────────────

            // Boxing = converting a VALUE TYPE into a REFERENCE TYPE
            //          value copied from STACK to a new object on HEAP

            int x = 42;       // value type → STACK
            object obj = x;        // BOXING → int wrapped in heap object

            // MEMORY:
            // STACK              HEAP
            // x   = 42           [Box Object]
            // obj = 0x1A2B ────→   type  = int
            //                       value = 42   

            // ─────────────────────────────────────────────────────────────
            // 2. WHAT IS UNBOXING?
            // ─────────────────────────────────────────────────────────────

            // Unboxing = extracting a VALUE TYPE back from a heap object
            //            value copied from HEAP back to STACK
            //            requires EXPLICIT CAST

            object obj2 = 42;       // boxing   → int goes to heap
            int y = (int)obj2; // unboxing → int extracted back to stack

            // MEMORY:
            // HEAP                        STACK
            // [Box Object]                y = 42 (fresh copy)
            //   type  = int    ─copy──→
            //   value = 42
            // (box stays on heap until GC cleans it)


            // ─────────────────────────────────────────────────────────────
            // 3. WHAT HAPPENS INTERNALLY
            // ─────────────────────────────────────────────────────────────

            // BOXING — 4 steps:
            // Step 1: CLR allocates new object on HEAP (~16-20 bytes for int)
            // Step 2: Value COPIED from stack into heap object
            // Step 3: Type info stored with heap object ("this contains int")
            // Step 4: Address of heap object stored in reference variable

            // UNBOXING — 3 steps:
            // Step 1: CLR checks type of boxed object — does it match cast?
            //         if NO  → InvalidCastException thrown
            //         if YES → continue
            // Step 2: Value COPIED from heap object to stack variable
            // Step 3: Heap object remains unchanged — GC cleans it later

            // ─────────────────────────────────────────────────────────────
            // 4. WHY BOXING IS EXPENSIVE
            // ─────────────────────────────────────────────────────────────

            // Cost 1 — HEAP ALLOCATION
            //   new object created on heap every single time
            //   heap allocation much slower than stack

            // Cost 2 — MEMORY COPY
            //   value copied from stack to heap object

            // Cost 3 — GC PRESSURE
            //   every boxed object = one more object GC must manage
            //   more objects → GC runs more often → app pauses

            // ─────────────────────────────────────────────────────────────
            // 5. OBVIOUS BOXING
            // ─────────────────────────────────────────────────────────────

            int a = 42;
            object ob1 = a;          // ✅ obvious boxing — you can see it
            int b = (int)ob1;   // ✅ obvious unboxing


            // ─────────────────────────────────────────────────────────────
            // 6. HIDDEN BOXING — INTERVIEW TRAPS
            // ─────────────────────────────────────────────────────────────

            // TRAP 1 — ArrayList
            // every Add() boxes the value type
            ArrayList list = new ArrayList();
            list.Add(1);                 // int → boxed to object
            list.Add(2);                 // int → boxed to object
            list.Add(3);                 // int → boxed to object
            int val = (int)list[0];      // object → unboxed to int


            // TRAP 2 — String.Format
            int age = 25;
            double sal = 50000.5;
            // both value types boxed — Format takes object params
            string s1 = string.Format("Age: {0}, Salary: {1}", age, sal);
            // no boxing — use interpolation instead
            string s2 = $"Age: {age}, Salary: {sal}";

            // TRAP 3 — Non-Generic Interface
            //interface IShowable { void Show(); }
            //struct MyStruct : IShowable
            //{
            //    public int Value;
            //    public void Show() => Console.WriteLine(Value);
            //}
            //// assigning struct to interface variable → boxing
            //IShowable s = new MyStruct { Value = 42 };
            //// interface is reference type → struct boxed into heap object

            // TRAP 4 — object Array
            // every element boxes the struct
            object[] items = new object[3];
            items[0] = new Point { X = 1, Y = 2 };  // boxed
            items[1] = new Point { X = 3, Y = 4 };  // boxed
            items[2] = new Point { X = 5, Y = 6 };  // boxed


            // ─────────────────────────────────────────────────────────────
            // PERFORMANCE COMPARISON
            // ─────────────────────────────────────────────────────────────

            // WITH boxing — 10 million iterations
            var boxedList = new ArrayList();
            for (int i = 0; i < 10_000_000; i++)
                boxedList.Add(i);            // boxing every iteration
                                             // Result: ~800ms — 10 million heap allocations!

            // WITHOUT boxing — 10 million iterations
            var typedList = new List<int>();
            for (int i = 0; i < 10_000_000; i++)
                typedList.Add(i);            // no boxing
                                             // Result: ~40ms — 20x faster!


            // ─────────────────────────────────────────────────────────────
            // 11. SOLUTIONS — HOW TO AVOID BOXING
            // ─────────────────────────────────────────────────────────────

            // SOLUTION 1 — Generics (Primary Fix)
            // Non-generic — causes boxing
            ArrayList nonGeneric = new ArrayList();
            nonGeneric.Add(42);             // boxed
            int v1 = (int)nonGeneric[0];   // unboxed
                                           // Generic — no boxing
            List<int> generic = new List<int>();
            generic.Add(42);                // no boxing — stored as int directly
            int v2 = generic[0];            // no unboxing — already int

            // SOLUTION 2 — Always Use Generic Collections
            // OLD — avoid (all cause boxing with value types)
            // ArrayList, Hashtable, Stack, Queue
            // NEW — use these (no boxing)
            List<int> safeList = new List<int>();
            Dictionary<string, int> safeDict = new Dictionary<string, int>();
            Stack<int> safeStack = new Stack<int>();
            Queue<int> safeQueue = new Queue<int>();

            // SOLUTION 3 — String Interpolation Over String.Format
            int num = 25;
            // boxes value types
            string formatted = string.Format("Value: {0}", num);
            // no boxing
            string interpolated = $"Value: {num}";

            // SOLUTION 4 — Generic Methods
            // causes boxing
            void PrintObject(object value) { Console.WriteLine(value); }
            PrintObject(42);                // int boxed to object 
                                            // no boxing
            void PrintGeneric<T>(T value) { Console.WriteLine(value); }
            PrintGeneric(42);
        }
    }
    // ============================================== END ====================================================



    // ============================================================
    //    NULLABLE TYPES — COMPLETE INTERVIEW NOTES
    //    Covers: int?, ??, ?., ??=, null checks, patterns
    // ============================================================


    class NullableTypes
    {


        // ─────────────────────────────────────────────────────────────
        // 1. WHY NULLABLE TYPES EXIST
        // ─────────────────────────────────────────────────────────────

        // Value types (int, bool, double) CANNOT be null by default
        // int x = null;   // COMPILE ERROR

        // BUT real world scenarios NEED null for value types:
        // → Database column "Age" might be empty (no data entered)
        // → API response field might be missing
        // → Optional method parameter not provided
        // → Unknown / pending state (not 0, not false — truly absent)

        // Without Nullable — forced to use fake "no value" markers
        int age = -1;       // -1 means "no age" — confusing, error prone
        int salary = 0;     // 0 means "no salary" or actually zero? ambiguous

        // With Nullable — clean, explicit, meaningful
        int? age2 = null; // null means "age not provided" — clear and safe
        int? salary2 = 0;   // 0 means actually zero salary — unambiguous


        // ─────────────────────────────────────────────────────────────
        // 2. WHAT IS int? — NULLABLE VALUE TYPE
        // ─────────────────────────────────────────────────────────────

        // int? is SYNTACTIC SUGAR for Nullable<int>
        int? x = 42;
        // is exactly the same as:
        Nullable<int> x2 = 42;

        // Works for ALL value types:
        int? nullableInt = null;
        double? nullableDouble = null;
        bool? nullableBool = null;
        decimal? nullableDecimal = null;
        float? nullableFloat = null;
        char? nullableChar = null;
        DateTime? nullableDate = null;

        // Enum? also works
        enum Status { Pending, Active, Closed }
        Status? nullableStatus = null;

        // Struct? also works
        struct Point { public int X, Y; }
        Point? nullablePoint = null;

        // ─────────────────────────────────────────────────────────────
        // 3. HOW Nullable<T> WORKS INTERNALLY
        // ─────────────────────────────────────────────────────────────

        // Compiler generates something like this behind the scenes:
        // struct Nullable<T> where T : struct
        // {
        //     private T    _value;      // actual int/bool/double etc
        //     private bool _hasValue;   // flag — do I have a value or am I null?
        //
        //     public bool HasValue => _hasValue;
        //     public T    Value    => _hasValue ? _value
        //                                      : throw new InvalidOperationException();
        // }

        // int? age = null:
        // Nullable<int>:
        //   _value    = 0      (irrelevant)
        //   _hasValue = false  ← THIS is what "null" means

        // KEY POINT:
        // int? is still a STRUCT — still lives on STACK
        // it is NOT a reference type — no heap allocation for null
        // null here means _hasValue = false — nothing more


        // ─────────────────────────────────────────────────────────────
        // 4. NULL COALESCING OPERATOR — ??
        // ─────────────────────────────────────────────────────────────

        // ?? means "if left side is null, use right side instead"
        // syntax: value ?? fallback

        //int? points = null;
        //int result2 = points ?? 0;      // points is null → use 0
        //Console.WriteLine(result2);      // 0

        // ─────────────────────────────────────────────────────────────
        // 5. NULL COALESCING ASSIGNMENT — ??=
        // ─────────────────────────────────────────────────────────────

        // ??= means "assign right side to left side ONLY if left is null"
        // syntax: variable ??= value
        // Added in C# 8

        //  int? count = null;
        //  count ??= 0;                    // count is null → assign 0
        //  Console.WriteLine(count);       // 0

        // ─────────────────────────────────────────────────────────────
        // 6. NULL CONDITIONAL OPERATOR — ?.
        // ─────────────────────────────────────────────────────────────

        // ?. means "if left side is null, return null instead of crashing"
        //          "if left side is NOT null, call the member"
        // syntax: object?.Member

        // WITHOUT ?. — crashes if person is null
        Person? p = null;
        // Console.WriteLine(p.Name);   // NullReferenceException!

        // WITH ?. — safely returns null if p is null
        //string? name3 = p?.Name;       // p is null → name3 = null, no crash
        //Console.WriteLine(name3);      // (nothing — null)
    }

    ////// IDisposable and using — resource cleanup pattern
    ///// Garbage Collector — generations (Gen0, Gen1, Gen2), GC.Collect pitfalls



    class GarbageCollector
    {
        public GarbageCollector()
        {
            // ============================================================
            //    IDisposable AND using — COMPLETE INTERVIEW NOTES
            //    Resource Cleanup, Dispose Pattern, using statement/declaration
            // ============================================================

            // ─────────────────────────────────────────────────────────────
            // 1. WHY IDisposable EXISTS — THE PROBLEM FIRST
            // ─────────────────────────────────────────────────────────────

            // Garbage Collector (GC) cleans up MANAGED memory automatically
            // BUT some resources are UNMANAGED — GC does NOT clean these up:

            // UNMANAGED RESOURCES:
            // → File handles          (open files on disk)
            // → Database connections  (connections to SQL Server, MySQL etc)
            // → Network sockets       (HTTP connections, TCP sockets)
            // → Stream handles        (FileStream, NetworkStream)
            // → Window handles        (UI resources)
            // → Mutex / locks         (OS-level synchronization)
            // → Unmanaged memory      (Marshal.AllocHGlobal)
            // → COM objects           (interop resources)

            // PROBLEM — if you forget to close these:
            void BadCode()
            {
                FileStream fs = new FileStream("data.txt", FileMode.Open);
                // read file...
                // forgot to close — file handle stays open FOREVER
                // other code cannot access this file
                // after 1000 calls — 1000 open file handles → OS limit hit → crash
            }

            // GC will eventually finalize fs — but GC runs on ITS schedule
            // could be seconds, minutes, or never in some scenarios
            // UNMANAGED resources need DETERMINISTIC cleanup — right when done

            // ─────────────────────────────────────────────────────────────
            // 2. WHAT IS IDisposable?
            // ─────────────────────────────────────────────────────────────

            // IDisposable is a built-in interface with ONE method:
            // public interface IDisposable
            // {
            //     void Dispose();
            // }

            // Dispose() = "I am done with this resource — clean it up NOW"
            // Deterministic — YOU decide when cleanup happens
            // Not GC — not random — right when YOU say so

            // ─────────────────────────────────────────────────────────────
            // 3. WITHOUT IDisposable — THE WRONG WAY
            // ─────────────────────────────────────────────────────────────

            // Manually calling close — easy to forget, not safe on exception
            void ReadFileWrong(string path)
            {
                FileStream fs = new FileStream(path, FileMode.Open);
                StreamReader reader = new StreamReader(fs);

                string content = reader.ReadToEnd();
                // if ReadToEnd() throws an exception — Close() never called!
                Console.WriteLine(content);

                reader.Close();  // might never reach here if exception thrown
                fs.Close();      // might never reach here
            }

            // try-finally — correct but VERY verbose
            void ReadFileTryFinally(string path)
            {
                FileStream fs = null;
                StreamReader reader = null;
                try
                {
                    fs = new FileStream(path, FileMode.Open);
                    reader = new StreamReader(fs);
                    string content = reader.ReadToEnd();
                    Console.WriteLine(content);
                }
                finally
                {
                    reader?.Close();   // always runs — even on exception
                    fs?.Close();       // always runs — even on exception
                }
                // works but ugly — this is exactly what using does for us
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 4. IMPLEMENTING IDisposable — BASIC PATTERN
        // ─────────────────────────────────────────────────────────────

        class FileManager : IDisposable
        {
            private FileStream _fileStream;
            private StreamReader _reader;
            private bool _disposed = false;  // guard against double dispose

            public FileManager(string path)
            {
                _fileStream = new FileStream(path, FileMode.OpenOrCreate);
                _reader = new StreamReader(_fileStream);
                Console.WriteLine($"[FileManager] Opened: {path}");
            }

            public string ReadAll()
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(FileManager));
                return _reader.ReadToEnd();
            }

            // IDisposable implementation
            public void Dispose()
            {
                if (_disposed) return;     // guard — safe to call multiple times

                _reader?.Close();          // close reader first
                _fileStream?.Close();      // then close stream
                _disposed = true;          // mark as disposed

                Console.WriteLine("[FileManager] Resources released.");
            }
        }

        //// Usage
        //FileManager fm = new FileManager("data.txt");
        //string text = fm.ReadAll();
        //fm.Dispose();                     // manually called — works but risky


        // ─────────────────────────────────────────────────────────────
        // 5. using STATEMENT — AUTOMATIC Dispose()
        // ─────────────────────────────────────────────────────────────

        // using statement automatically calls Dispose() when block ends
        // even if an exception is thrown — ALWAYS disposes

        // SYNTAX:
        // using (var resource = new SomeDisposable())
        // {
        //     // use resource here
        // }  // ← Dispose() called HERE automatically

        // WHAT COMPILER GENERATES BEHIND THE SCENES:
        // FileManager fm = new FileManager("data.txt");
        // try
        // {
        //     string content = fm.ReadAll();
        // }
        // finally
        // {
        //     if (fm != null)
        //         ((IDisposable)fm).Dispose();   // always called
        // }

        public static void UsingExample()
        {
            // ACTUAL USAGE:
            using (var manager = new FileManager("data.txt"))
            {
                string content = manager.ReadAll();
                Console.WriteLine(content);
            }
            // ← Dispose() called here automatically — even if exception thrown

            // Multiple resources in ONE using:
            using (var fs = new FileStream("in.txt", FileMode.Open))
            using (var writer = new StreamWriter("out.txt"))
            {
                string data3 = new StreamReader(fs).ReadToEnd();
                writer.Write(data3);
            }
            // both fs and writer disposed in reverse order
        }

        // ─────────────────────────────────────────────────────────────
        // 6. using DECLARATION — C# 8+ CLEANER SYNTAX
        // ─────────────────────────────────────────────────────────────

        // C# 8 introduced using DECLARATION — no braces needed
        // Dispose() called when variable goes out of scope (end of method)

        void ProcessFileModern(string path)
        {
            using var fs = new FileStream(path, FileMode.Open);   // no braces!
            using var reader = new StreamReader(fs);                  // no braces!

            string content = reader.ReadToEnd();
            Console.WriteLine(content);

        }  // ← both reader and fs disposed here automatically

        // DIFFERENCE:
        // using STATEMENT → dispose at end of using BLOCK { }
        // using DECLARATION → dispose at end of containing SCOPE (method)

        void Comparison(string path)
        {
            // using STATEMENT — disposed at end of { }
            using (var r1 = new StreamReader(path))
            {
                Console.WriteLine(r1.ReadToEnd());
            }  // ← r1 disposed HERE

            // r1 no longer accessible here

            // using DECLARATION — disposed at end of METHOD
            using var r2 = new StreamReader(path);
            Console.WriteLine(r2.ReadToEnd());

        }  // ← r2 disposed HERE (end of method)

    }

    // ─────────────────────────────────────────────────────────────
    // FULL DISPOSE PATTERN — WITH FINALIZER (SAFE HANDLE PATTERN)
    // ─────────────────────────────────────────────────────────────

    // When your class directly holds UNMANAGED resources
    // (not just wrapping managed IDisposable objects)
    // you need BOTH Dispose() AND a Finalizer as safety net

    class UnmanagedResourceHolder : IDisposable
    {
        // Managed resource (another IDisposable)
        private StreamReader _reader;

        // Unmanaged resource (raw handle — hypothetical)
        private IntPtr _unmanagedHandle;

        private bool _disposed = false;

        public UnmanagedResourceHolder(string path)
        {
            _reader = new StreamReader(path);
            _unmanagedHandle = /* NativeApi.OpenHandle() */ IntPtr.Zero;
        }

        // PUBLIC Dispose — called by consumer via using or manually
        public void Dispose()
        {
            Dispose(disposing: true);

            // Tell GC — do NOT call Finalizer
            // we already cleaned up — no need for GC to do it again
            GC.SuppressFinalize(this);
        }

        // PROTECTED virtual Dispose — actual cleanup logic
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Safe to clean up MANAGED resources here
                // because we were called from Dispose() — not from GC thread
                _reader?.Dispose();
                Console.WriteLine("[Dispose] Managed resources released.");
            }

            // Always clean up UNMANAGED resources
            // whether called from Dispose() or Finalizer
            if (_unmanagedHandle != IntPtr.Zero)
            {
                // NativeApi.CloseHandle(_unmanagedHandle);
                _unmanagedHandle = IntPtr.Zero;
                Console.WriteLine("[Dispose] Unmanaged handle released.");
            }

            _disposed = true;
        }

        // FINALIZER — safety net if consumer forgot to call Dispose()
        // GC calls this on its own thread if Dispose() was never called
        ~UnmanagedResourceHolder()
        {
            Dispose(disposing: false);
            // disposing=false because:
            // → called from GC thread
            // → managed objects might already be collected
            // → only safe to release UNMANAGED resources here
            Console.WriteLine("[Finalizer] Called by GC — Dispose() was forgotten!");
        }

        public static void FinalizerExample()
        {
            // Usage — correct way
            using (var holder = new UnmanagedResourceHolder("data.txt"))
            {
                // use it
            }
            // GC.SuppressFinalize called — Finalizer will NOT run (efficient) ✅

            // Usage — forgot Dispose — finalizer saves us eventually
            var holder2 = new UnmanagedResourceHolder("data.txt");
            // forgot using or Dispose()
            // GC eventually calls ~UnmanagedResourceHolder() → Dispose(false)
            // unmanaged handle released — managed resources may leak though
        }

        class FileHolder
        {
            private FileStream _fs;

            public FileHolder(string path)
            {
                _fs = new FileStream(path, FileMode.Open);
                //    ↑ FileStream object → GC cleans this object 
                //    ↑ but the OS file handle INSIDE FileStream → GC cannot clean 
            }
        }

        void Demo()
        {
            FileHolder holder = new FileHolder("data.txt");
            holder = null;   // no references → GC will clean FileHolder object
                             // GC will clean FileStream object too
                             // BUT the OS file handle stays open ❌
                             // "data.txt" remains locked by OS ❌
                             // other code cannot access the file ❌
        }

        // ============================================================
        //    GC vs UNMANAGED RESOURCES — COMPLETE INTERVIEW NOTES
        //    Why GC Cleans Managed Heap But Not OS Resources
        // ============================================================




        // ─────────────────────────────────────────────────────────────
        // 1. GC'S ONLY JOB — ONE RESPONSIBILITY
        // ─────────────────────────────────────────────────────────────

        // GC has ONE job:
        // "Track objects on the MANAGED HEAP
        //  and reclaim their memory when nobody references them"
        //
        // That is its ENTIRE job — nothing more, nothing less.
        //
        // GC:
        // ✅ Allocated managed heap memory    → tracks it
        // ✅ Knows size of every object       → can free exact bytes
        // ✅ Knows all references             → knows what is reachable
        // ✅ Frees unreachable objects        → reclaims memory
        // ❌ Knows nothing about OS resources → cannot touch them
        // ❌ Knows nothing about file handles → cannot close them
        // ❌ Knows nothing about DB conns     → cannot disconnect them
        // ❌ Knows nothing about sockets      → cannot close them

        // ─────────────────────────────────────────────────────────────
        // 2. WHAT GC CLEANS — MANAGED HEAP OBJECTS ONLY
        // ─────────────────────────────────────────────────────────────

        void WhatGCCleans()
        {
            // ALL of these live on MANAGED HEAP — GC cleans ALL ✅

            //var person = new Person { Name = "Ravi", Age = 25 };  // class object
            var name = "Hello World";                            // string
            var numbers = new int[] { 1, 2, 3, 4, 5 };            // array
            var list = new List<int> { 10, 20, 30 };            // collection
            var dict = new Dictionary<string, int>();            // collection
            // var order = new Order { Amount = 5000 };              // any class
            object boxed = 42;                                       // boxed value type

            // When method ends — all go out of scope
            // GC eventually reclaims ALL of their managed heap memory ✅
            // You do NOTHING — GC handles everything automatically ✅

            // GC TRACKS EVERYTHING ON MANAGED HEAP:
            //
            // Address   │ Object              │ Size     │ GC Tracks
            // ──────────────────────────────────────────────────────
            // 0x1A2B    │ Person              │ 24 bytes │ ✅
            // 0x3C4D    │ string "Ravi"       │ 32 bytes │ ✅
            // 0x5E6F    │ int[] {1,2,3,4,5}  │ 36 bytes │ ✅
            // 0x7A8B    │ List<int>           │ 48 bytes │ ✅
            // ──────────────────────────────────────────────────────
            // GC has COMPLETE visibility of managed heap 

            // ─────────────────────────────────────────────────────────────
            // 3. WHAT GC DOES NOT CLEAN — UNMANAGED RESOURCES
            // ─────────────────────────────────────────────────────────────

            // UNMANAGED RESOURCES — GC has ZERO knowledge of these:
            //
            // ❌ File handles        → OS resource, lives in OS kernel space
            // ❌ Database connections→ network resource, outside managed heap
            // ❌ Network sockets     → OS resource, outside managed heap
            // ❌ Window handles      → UI OS resource (HWND)
            // ❌ Mutex / Semaphore   → OS sync object
            // ❌ Native memory       → Marshal.AllocHGlobal — outside heap
            // ❌ COM objects         → unmanaged COM, outside managed heap
            // ❌ GPU resources       → DirectX/OpenGL, outside managed heap
            // ❌ Thread handles      → OS threads, outside managed heap
            //
            // ALL of these must be manually released by YOU
            // GC will NEVER touch them — it cannot even see them

            // ─────────────────────────────────────────────────────────────
            // 4. THE KEY INSIGHT — OBJECT AND RESOURCE ARE TWO DIFFERENT THINGS
            // ─────────────────────────────────────────────────────────────

            void KeyInsight()
            {
                FileStream fs = new FileStream("data.txt", FileMode.OpenOrCreate);

                // This creates TWO completely separate things:

                // THING 1 — FileStream OBJECT on MANAGED HEAP:
                // ─────────────────────────────────────────────
                // Lives at: 0x5E6F on managed heap
                // Size: 48 bytes
                // GC allocated it   ✅
                // GC tracks it      ✅
                // GC can free it    ✅

                // THING 2 — OS File HANDLE in OS KERNEL:
                // ─────────────────────────────────────────────
                // Lives in: OS kernel space (completely separate from heap)
                // Represented by: a number e.g. 1042
                // OS allocated it   ❌ (GC did not)
                // GC tracks it      ❌ (GC cannot see kernel space)
                // GC can free it    ❌ (GC has no API to close OS handles)

                // FileStream OBJECT has a field that stores the NUMBER 1042
                // GC sees that field — but sees it as just an INTEGER
                // GC does not know 1042 = "an open file in OS kernel"
                // GC does not know HOW to close it even if it wanted to

                // MEMORY PICTURE:
                //
                // STACK              MANAGED HEAP              OS KERNEL
                // ─────────────      ──────────────────────    ──────────────────────
                // fs = 0x5E6F ──────→ FileStream object        Handle #1042
                //                      _handle = 1042    ...→  "data.txt" OPEN
                //                      (GC sees: int)          file is LOCKED
                //                      (GC does not know       consuming OS resources
                //                       what 1042 means)
                // ─────────────      ──────────────────────    ──────────────────────
                // GC tracks fs ✅    GC tracks object ✅       GC blind ❌
            }
        }
    }



}
