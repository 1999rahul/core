using oops.Inheritance;
using System.Collections;
using System.Net;
using System.Numerics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
}
