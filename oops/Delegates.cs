

namespace oops
{
    // ============================================================
    //  D E L E G A T E S   I N   C #  —  Complete Interview Guide
    // ============================================================
    //
    //  Topics covered:
    //  1.  What is a delegate?
    //  2.  Type-safe function pointer explained
    //  3.  Delegates as variables (first-class functions)
    //  4.  Custom delegate declaration
    //  5.  Multicast delegates
    //  6.  Built-in delegates: Func, Action, Predicate
    //  7.  Why Predicate<T> exists despite Func<T, bool>
    //  8.  Nominal typing — why they are incompatible types
    //  9.  Lambda expressions with delegates
    //  10. Delegates as parameters (callback pattern)
    //  11. Returning delegates from methods
    //  12. Storing delegates in collections (chaining)
    //  13. Delegates and Events (publisher-subscriber)
    //  14. Closures — capturing variables
    // ============================================================

    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace DelegatesGuide
    {

        // ─────────────────────────────────────────────────────────
        // SECTION 1 — CUSTOM DELEGATE DECLARATION
        // ─────────────────────────────────────────────────────────
        //
        // Syntax:  delegate <ReturnType> <Name>(<Parameters>);
        //
        // The compiler generates a class behind the scenes that
        // inherits from System.MulticastDelegate. Writing this:
        //
        //   delegate int MathOperation(int a, int b);
        //
        // is roughly equivalent to the compiler generating:
        //
        //   class MathOperation : MulticastDelegate
        //   {
        //       public int Invoke(int a, int b) { ... }
        //   }
        //
        // You rarely need custom delegates in modern C# because
        // Func / Action / Predicate cover almost every case.
        // ─────────────────────────────────────────────────────────

        delegate int MathOperation(int a, int b);   // returns int
        delegate void Logger(string message);         // returns void
        delegate bool Validator(string input);        // returns bool


        // ─────────────────────────────────────────────────────────
        // SECTION 2 — WHAT IS "TYPE-SAFE FUNCTION POINTER"?
        // ─────────────────────────────────────────────────────────
        //
        // In C/C++ a raw function pointer is just a memory address.
        // The compiler does NOT check whether the method you point
        // to has the right signature — crashes happen at runtime.
        //
        //   // C — no type safety:
        //   void* fp = &someFunction;
        //   ((int(*)(char*,float))fp)("wrong", 3.14f);  // compiles! crashes!
        //
        // In C# a delegate BAKES the signature into the type.
        // If the method signature doesn't match, you get a
        // COMPILE-TIME error — the bug is caught before the
        // program ever runs.
        //
        //   delegate int MathOperation(int a, int b);
        //
        //   static int  Add(int a, int b)    => a + b;       // ✅ int,int → int
        //   static string Greet(string name) => "Hi " + name; // ❌ wrong signature
        //
        //   MathOperation op = Add;    // ✅ compiles
        //   MathOperation op = Greet;  // ❌ compile-time error — type safe!
        //
        // "Type-safe" = parameter types AND return type must match
        // exactly, enforced by the compiler, not the runtime.
        // ─────────────────────────────────────────────────────────

        class TypeSafeDemo
        {
            static int Add(int a, int b) => a + b;
            static int Multiply(int a, int b) => a * b;
            // static string Greet(string s)   => s;  // ← would cause compile error

            public static void Run()
            {
                Console.WriteLine("\n── Type-Safe Function Pointer ──");

                MathOperation op = Add;               // assign method like a variable
                Console.WriteLine($"Add(3,4)      = {op(3, 4)}");   // 7

                op = Multiply;                        // reassign — still type-safe
                Console.WriteLine($"Multiply(3,4) = {op(3, 4)}");   // 12

                // The delegate guarantees: whatever method 'op' holds,
                // it will always accept (int, int) and return int.
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 3 — DELEGATES AS VARIABLES (FIRST-CLASS FUNCTIONS)
        // ─────────────────────────────────────────────────────────
        //
        // A delegate variable stores a METHOD the same way an int
        // variable stores a number. This means you can:
        //
        //   ✅ Assign a method to a variable
        //   ✅ Reas as an argument to another method
        //   ✅ Return a methodsign it to a different method
        //   ✅ Pass a method from a method
        //   ✅ Store methods in a List / Dictionary / array
        //
        // This is called "first-class functions" in other languages
        // (JavaScript, Python, etc.) — functions treated as values.
        // ─────────────────────────────────────────────────────────

        class DelegateAsVariable
        {
            // ── Pass a method as an argument (callback pattern) ──
            static void RunTwice(Action task)
            {
                task();  // first call
                task();  // second call
            }

            // ── Return a method from a method ──
            static Func<int, int> GetMultiplier(int factor)
            {
                // Returns a method — not a value!
                return x => x * factor;
            }

            // ── Store methods in a collection ──
            static void ChainOperations()
            {
                var pipeline = new List<Func<int, int>>
            {
                x => x + 1,    // step 1: add 1
                x => x * 2,    // step 2: double
                x => x * x     // step 3: square
            };

                int value = 3;
                foreach (var step in pipeline)
                    value = step(value);
                // 3 → (3+1=4) → (4*2=8) → (8*8=64)

                Console.WriteLine($"Pipeline result: {value}"); // 64
            }

            public static void Run()
            {
                Console.WriteLine("\n── Delegate as Variable ──");

                // Assign and reassign
                Func<int, int> op;
                op = x => x * x;
                Console.WriteLine($"Square(5)  = {op(5)}");  // 25
                op = x => x + 100;
                Console.WriteLine($"Add100(5)  = {op(5)}");  // 105

                // Pass as argument
                RunTwice(() => Console.WriteLine("Hello from delegate!"));

                // Return a method
                var triple = GetMultiplier(3);
                Console.WriteLine($"Triple(10) = {triple(10)}");  // 30

                // Store in collection
                ChainOperations();
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 4 — MULTICAST DELEGATES
        // ─────────────────────────────────────────────────────────
        //
        // A single delegate variable can hold references to
        // MULTIPLE methods using += and -=.
        // When invoked, ALL methods run in the order they were added.
        //
        // ⚠️  Key limitation:
        //     If the delegate has a non-void return type, only the
        //     LAST method's return value is captured. Intermediate
        //     return values are silently discarded.
        //     → This is why multicast delegates typically use void.
        // ─────────────────────────────────────────────────────────

        class MulticastDemo
        {
            static void LogToConsole(string msg) => Console.WriteLine($"[Console] {msg}");
            static void LogToFile(string msg) => Console.WriteLine($"[File]    {msg}");
            static void LogToCloud(string msg) => Console.WriteLine($"[Cloud]   {msg}");

            public static void Run()
            {
                Console.WriteLine("\n── Multicast Delegate ──");

                Logger logger = LogToConsole;
                logger += LogToFile;    // add second method
                logger += LogToCloud;   // add third method

                logger("App started");
                // [Console] App started
                // [File]    App started
                // [Cloud]   App started

                Console.WriteLine("-- After removing File logger --");
                logger -= LogToFile;    // remove one method

                logger("App stopped");
                // [Console] App stopped
                // [Cloud]   App stopped

                // ── Non-void multicast: only last return value survives ──
                MathOperation multiMath = (a, b) => { Console.WriteLine($"Result A: {a + b}"); return a + b; };
                multiMath += (a, b) => { Console.WriteLine($"Result B: {a * b}"); return a * b; };

                int finalResult = multiMath(3, 4);
                // Result A: 7   ← ran, but return value 7 is DISCARDED
                // Result B: 12  ← ran, return value 12 is KEPT
                Console.WriteLine($"Final result (only last): {finalResult}"); // 12
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 5 — BUILT-IN DELEGATES: Func, Action, Predicate
        // ─────────────────────────────────────────────────────────
        //
        //  ┌─────────────────────┬────────────────┬───────────────┐
        //  │ Type                │ Parameters     │ Return type   │
        //  ├─────────────────────┼────────────────┼───────────────┤
        //  │ Action              │ 0              │ void          │
        //  │ Action<T>           │ 1              │ void          │
        //  │ Action<T1,T2,...>   │ up to 16       │ void          │
        //  ├─────────────────────┼────────────────┼───────────────┤
        //  │ Func<TResult>       │ 0              │ TResult       │
        //  │ Func<T,TResult>     │ 1              │ TResult       │
        //  │ Func<T1,T2,TResult> │ up to 16 + ret │ TResult       │
        //  │  (last type = return type)                           │
        //  ├─────────────────────┼────────────────┼───────────────┤
        //  │ Predicate<T>        │ exactly 1      │ always bool   │
        //  └─────────────────────┴────────────────┴───────────────┘
        // ─────────────────────────────────────────────────────────

        class BuiltInDelegatesDemo
        {
            public static void Run()
            {
                Console.WriteLine("\n── Built-in Delegates ──");

                // ── Action — takes params, returns void ──
                Action greet = () => Console.WriteLine("Hello!");
                greet(); // Hello!

                Action<string> greetUser = name => Console.WriteLine($"Hello, {name}!");
                greetUser("Alice"); // Hello, Alice!

                Action<string, int> repeat = (msg, n) =>
                {
                    for (int i = 0; i < n; i++)
                        Console.WriteLine(msg);
                };
                repeat("Hi", 2); // Hi \n Hi

                // ── Func — takes params, returns a value ──
                // Last type argument is ALWAYS the return type
                Func<int, int, int> add = (a, b) => a + b;
                Console.WriteLine($"add(5,3)     = {add(5, 3)}");   // 8

                Func<string, int> length = s => s.Length;
                Console.WriteLine($"length(Hello)= {length("Hello")}"); // 5

                Func<int, bool> isPositive = n => n > 0;
                Console.WriteLine($"isPositive(5)= {isPositive(5)}");   // True

                // ── Predicate — exactly 1 param, always returns bool ──
                Predicate<int> isEven = n => n % 2 == 0;
                Predicate<string> isEmpty = s => s.Length == 0;

                Console.WriteLine($"isEven(4)    = {isEven(4)}");    // True
                Console.WriteLine($"isEven(7)    = {isEven(7)}");    // False
                Console.WriteLine($"isEmpty(\"\") = {isEmpty("")}");  // True

                // ── Func used in LINQ ──
                var numbers = new List<int> { 1, 2, 3, 4, 5, 6 };
                var evens = numbers.Where(n => n % 2 == 0);    // Func<int,bool> inside
                Console.WriteLine($"Evens: {string.Join(", ", evens)}"); // 2, 4, 6
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 6 — WHY DOES Predicate<T> EXIST?
        //             (if Func<T, bool> does the same thing)
        // ─────────────────────────────────────────────────────────
        //
        // SHORT ANSWER: Historical reasons + backward compatibility.
        //
        // TIMELINE:
        //   .NET 2.0 (2005) → Predicate<T> introduced for List<T>
        //   .NET 3.5 (2007) → Func<T> / Action<T> introduced with LINQ
        //
        // Predicate<T> was created BEFORE generic Func delegates
        // existed. It was built into List<T> methods:
        //
        //   list.Find(Predicate<T>)       → finds first match
        //   list.FindAll(Predicate<T>)    → finds all matches
        //   list.FindIndex(Predicate<T>)  → finds index of match
        //   list.RemoveAll(Predicate<T>)  → removes all matches
        //   list.Exists(Predicate<T>)     → checks if any match
        //
        // When Func arrived, Microsoft kept Predicate<T> because
        // removing it would have broken existing code everywhere.
        //
        // ─────────────────────────────────────────────────────────
        // NOMINAL TYPING — Why Predicate<T> and Func<T,bool>
        //                  are INCOMPATIBLE even though identical
        // ─────────────────────────────────────────────────────────
        //
        // C# uses NOMINAL typing for delegates (name-based),
        // not STRUCTURAL typing (shape-based).
        //
        // Even though both resolve to bool(int) for T = int,
        // C# treats them as DIFFERENT types — just like:
        //
        //   class Point { int X; int Y; }
        //   class Coord { int X; int Y; }
        //   Point p = new Coord();  // ❌ same shape, different name
        //
        // So:
        //   Predicate<int>  p = n => n > 0;
        //   Func<int, bool> f = p;           // ❌ COMPILE ERROR
        //   Func<int, bool> f = p.Invoke;    // ✅ workaround: wrap in Invoke
        //
        // This is why List<T>.FindAll(Func<int,bool>) fails:
        //
        //   Func<int, bool> isEven = n => n % 2 == 0;
        //   list.FindAll(isEven);            // ❌ expects Predicate<T>, not Func
        //   list.FindAll(n => n % 2 == 0);  // ✅ lambda inferred as Predicate<T>
        // ─────────────────────────────────────────────────────────

        class PredicateVsFuncDemo
        {
            public static void Run()
            {
                Console.WriteLine("\n── Predicate<T> vs Func<T, bool> ──");

                var numbers = new List<int> { -3, -1, 0, 2, 4, 7, 9 };

                // ── Predicate<T> — used by old List<T> API ──
                Predicate<int> isPositive = n => n > 0;

                var found = numbers.Find(isPositive);
                Console.WriteLine($"First positive (Find):    {found}");          // 2

                var allPositive = numbers.FindAll(isPositive);
                Console.WriteLine($"All positives (FindAll):  {string.Join(", ", allPositive)}"); // 2,4,7,9

                bool anyPositive = numbers.Exists(isPositive);
                Console.WriteLine($"Any positive (Exists):    {anyPositive}");    // True

                var copy = new List<int>(numbers);
                copy.RemoveAll(n => n <= 0);
                Console.WriteLine($"After RemoveAll(<=0):     {string.Join(", ", copy)}"); // 2,4,7,9

                // ── Func<T,bool> — used by LINQ ──
                Func<int, bool> isEven = n => n % 2 == 0;
                var evens = numbers.Where(isEven); // LINQ uses Func, not Predicate
                Console.WriteLine($"Even numbers (LINQ):      {string.Join(", ", evens)}"); // -2? no: 0,2,4

                // ── Nominal typing in action ──
                // These have IDENTICAL signatures but are DIFFERENT types:
                Predicate<int> p = n => n > 5;
                // Func<int,bool> f = p;   ← COMPILE ERROR (uncomment to verify)
                Func<int, bool> f = p.Invoke; // ✅ workaround via Invoke
                Console.WriteLine($"Predicate wrapped as Func: {f(10)}"); // True

                // ── Modern recommendation ──
                // Use Func<T, bool> everywhere in new code.
                // Only use Predicate<T> when List<T> methods demand it
                // (or just use lambdas directly — the compiler infers the type).
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 7 — LAMBDA EXPRESSIONS
        // ─────────────────────────────────────────────────────────
        //
        // Lambdas are inline anonymous methods — syntactic sugar
        // that the compiler converts into a delegate instance.
        //
        // Syntax:
        //   (parameters) => expression          ← expression lambda
        //   (parameters) => { statements; }     ← statement lambda
        // ─────────────────────────────────────────────────────────

        class LambdaDemo
        {
            public static void Run()
            {
                Console.WriteLine("\n── Lambda Expressions ──");

                // Old style: anonymous method (C# 2.0)
                Func<int, int> squareAnon = delegate (int x) { return x * x; };

                // Modern style: expression lambda (C# 3.0+)
                Func<int, int> squareLambda = x => x * x;

                // Statement lambda (multiple lines)
                Func<int, string> classify = n =>
                {
                    if (n < 0) return "negative";
                    if (n == 0) return "zero";
                    return "positive";
                };

                Console.WriteLine($"squareAnon(5)   = {squareAnon(5)}");     // 25
                Console.WriteLine($"squareLambda(5) = {squareLambda(5)}");   // 25
                Console.WriteLine($"classify(-3)    = {classify(-3)}");      // negative
                Console.WriteLine($"classify(0)     = {classify(0)}");       // zero
                Console.WriteLine($"classify(7)     = {classify(7)}");       // positive
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 8 — DELEGATE AS PARAMETER (CALLBACK PATTERN)
        // ─────────────────────────────────────────────────────────
        //
        // The most common real-world use of delegates is passing
        // behavior as an argument — letting the caller decide
        // what the method does with each item.
        // This is the foundation of LINQ, sorting, filtering, etc.
        // ─────────────────────────────────────────────────────────

        class CallbackDemo
        {
            static void ProcessNumbers(List<int> numbers, Func<int, int> transform)
            {
                for (int i = 0; i < numbers.Count; i++)
                    numbers[i] = transform(numbers[i]);
            }

            static void FilterAndPrint(List<int> numbers, Func<int, bool> predicate)
            {
                var result = numbers.Where(predicate);
                Console.WriteLine(string.Join(", ", result));
            }

            public static void Run()
            {
                Console.WriteLine("\n── Callback Pattern ──");

                var nums = new List<int> { 1, 2, 3, 4, 5 };

                ProcessNumbers(nums, x => x * x);
                Console.WriteLine($"After squaring:   {string.Join(", ", nums)}"); // 1,4,9,16,25

                ProcessNumbers(nums, x => x + 100);
                Console.WriteLine($"After +100:       {string.Join(", ", nums)}"); // 101,104,109,116,125

                var data = new List<int> { 10, 23, 4, 67, 8, 55, 3 };
                Console.Write("Greater than 10:  ");
                FilterAndPrint(data, n => n > 10);   // 23, 67, 55
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 9 — DELEGATES AND EVENTS
        // ─────────────────────────────────────────────────────────
        //
        // An event is a DELEGATE with restrictions added.
        // The 'event' keyword ensures:
        //   ✅ External code can subscribe   (+= )
        //   ✅ External code can unsubscribe (-= )
        //   ❌ External code CANNOT invoke the delegate directly
        //   ❌ External code CANNOT reassign (= ) the delegate
        //
        // This enforces the publisher-subscriber (observer) pattern.
        // The publisher (Button) fires the event.
        // Subscribers (UI, Logger) react to it.
        //
        // ?.Invoke() safely calls only if there are subscribers.
        // ─────────────────────────────────────────────────────────

        class Button
        {
            // event — restricted delegate
            public event Action<string> OnClick;

            public void Click(string label)
            {
                Console.WriteLine($"[Button] '{label}' was clicked");
                OnClick?.Invoke(label);   // null-safe: no crash if no subscribers
            }
        }

        class EventDemo
        {
            static void HandleClick(string label) =>
                Console.WriteLine($"[UI Handler] Reacting to click: {label}");

            public static void Run()
            {
                Console.WriteLine("\n── Events (Delegate + Restrictions) ──");

                var btn = new Button();

                // Subscribe
                btn.OnClick += HandleClick;
                btn.OnClick += label => Console.WriteLine($"[Logger] Click logged: {label}");

                btn.Click("Submit");
                // [Button] 'Submit' was clicked
                // [UI Handler] Reacting to click: Submit
                // [Logger] Click logged: Submit

                Console.WriteLine("-- After unsubscribing UI Handler --");
                btn.OnClick -= HandleClick;
                btn.Click("Cancel");
                // [Button] 'Cancel' was clicked
                // [Logger] Click logged: Cancel

                // btn.OnClick("direct");   ← COMPILE ERROR: only owner can invoke
                // btn.OnClick = null;      ← COMPILE ERROR: only owner can assign
            }
        }


        // ─────────────────────────────────────────────────────────
        // SECTION 10 — CLOSURES (CAPTURING VARIABLES)
        // ─────────────────────────────────────────────────────────
        //
        // A delegate (lambda) can "capture" variables from its
        // enclosing scope. The captured variable stays alive as long
        // as the delegate is alive — even after the original method
        // has returned.
        //
        // ⚠️  Memory leak risk:
        //     If a long-lived object subscribes to an event on
        //     another long-lived object, the subscriber is kept
        //     alive by the delegate reference, preventing GC.
        //     Always unsubscribe with -= when done.
        // ─────────────────────────────────────────────────────────

        class ClosureDemo
        {
            static Func<int> MakeCounter()
            {
                int count = 0;          // local variable — lives on the heap
                return () => ++count;   // lambda captures 'count'
            }                           // method returns, but 'count' survives

            static Func<int, int> MakeAdder(int addBy)
            {
                return x => x + addBy;  // captures 'addBy' parameter
            }

            public static void Run()
            {
                Console.WriteLine("\n── Closures ──");

                var counter = MakeCounter();
                Console.WriteLine(counter()); // 1
                Console.WriteLine(counter()); // 2
                Console.WriteLine(counter()); // 3
                                              // 'count' persists across calls because the delegate holds a reference

                var add5 = MakeAdder(5);
                var add10 = MakeAdder(10);
                Console.WriteLine(add5(3));   // 8   (3 + 5)
                Console.WriteLine(add10(3));  // 13  (3 + 10)
                                              // Each closure captures its own separate 'addBy'
            }
        }


        // ─────────────────────────────────────────────────────────
        // MAIN — Run all sections
        // ─────────────────────────────────────────────────────────

        class Program
        {
            static void Main(string[] args)
            {
                Console.WriteLine("===========================================");
                Console.WriteLine("  C# DELEGATES — Complete Interview Guide ");
                Console.WriteLine("===========================================");

                TypeSafeDemo.Run();          // Section 2
                DelegateAsVariable.Run();    // Section 3
                MulticastDemo.Run();         // Section 4
                BuiltInDelegatesDemo.Run();  // Section 5
                PredicateVsFuncDemo.Run();   // Section 6
                LambdaDemo.Run();            // Section 7
                CallbackDemo.Run();          // Section 8
                EventDemo.Run();             // Section 9
                ClosureDemo.Run();           // Section 10

                Console.WriteLine("\n===========================================");
                Console.WriteLine("  All sections complete.");
                Console.WriteLine("===========================================");
            }
        }

    }
}
