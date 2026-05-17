namespace oops
{
    class G_C
    {
        // ============================================================
        //    GARBAGE COLLECTOR (GC) — COMPLETE INTERVIEW NOTES
        //    How GC Works, Generations, Finalization, Best Practices
        // ============================================================

        public G_C()
        {
            // ─────────────────────────────────────────────────────────────
            // 1. WHAT IS GARBAGE COLLECTOR?
            // ─────────────────────────────────────────────────────────────

            // GC is an AUTOMATIC MEMORY MANAGER built into .NET CLR
            // It tracks all objects on managed heap
            // It automatically frees memory of objects no longer reachable
            // You do NOT manually allocate or free managed memory in C#

            // WITHOUT GC (like in C/C++):
            // malloc(sizeof(Person));   // manually allocate
            // free(person);             // manually free — forget this = memory leak
            // use after free            // crash — undefined behavior

            // WITH GC (C#):
            var person = new Person();   // GC allocates on managed heap ✅
                                         // use person...
                                         // go out of scope — GC automatically frees when unreachable ✅
                                         // NO manual free needed ✅

            // GC RESPONSIBILITIES:
            // ✅ Allocate memory for new objects on managed heap
            // ✅ Track which objects are still reachable (in use)
            // ✅ Identify unreachable objects (garbage)
            // ✅ Free memory of unreachable objects
            // ✅ Compact heap to reduce fragmentation
            // ❌ Does NOT manage unmanaged resources (files, sockets, DB)

            // ─────────────────────────────────────────────────────────────
            // 2. MANAGED HEAP — WHERE GC WORKS
            // ─────────────────────────────────────────────────────────────

            // Managed heap = a large block of memory managed entirely by CLR
            // All reference type objects live here
            // GC has complete visibility and control of this memory

            // HEAP LAYOUT:
            // ──────────────────────────────────────────────────────────────
            // │  Small Object Heap (SOH)  │  Large Object Heap (LOH)       │
            // │  objects < 85,000 bytes   │  objects >= 85,000 bytes        │
            // │  Gen0 │ Gen1 │ Gen2       │  always Gen2                    │
            // ──────────────────────────────────────────────────────────────

            // SMALL OBJECT HEAP (SOH):
            // → most objects live here
            // → divided into generations (Gen0, Gen1, Gen2)
            // → compacted after collection (objects moved together)

            // LARGE OBJECT HEAP (LOH):
            // → objects >= 85,000 bytes (large arrays, large strings)
            // → always treated as Gen2
            // → NOT compacted by default (too expensive to move large objects)
            // → can cause fragmentation over time

            // ─────────────────────────────────────────────────────────────
            // 3. HOW ALLOCATION WORKS — THE MANAGED HEAP POINTER
            // ─────────────────────────────────────────────────────────────

            // Managed heap allocation is EXTREMELY FAST
            // CLR maintains a NEXT OBJECT POINTER
            // Allocating = just moving the pointer forward

            // BEFORE allocation:
            // ─────────────────────────────────────────
            // │ Obj1 │ Obj2 │ Obj3 │ FREE SPACE ....  │
            //                         ↑
            //                    Next Object Pointer

            // AFTER new Person() allocation:
            // ─────────────────────────────────────────
            // │ Obj1 │ Obj2 │ Obj3 │ Person │ FREE ... │
            //                                ↑
            //                           Next Object Pointer moved forward

            // Allocation = pointer bump = extremely fast (few nanoseconds) ✅
            // Much faster than C malloc() which must find free block ✅

            void AllocationDemo()
            {
                // All of these are fast pointer-bump allocations
                var p1 = new Person { Name = "Ravi" };   // bump pointer
                var p2 = new Person { Name = "Mohan" };   // bump pointer
                var p3 = new Person { Name = "Priya" };   // bump pointer
                                                          // all placed consecutively on heap — great cache locality ✅
            }

            // ─────────────────────────────────────────────────────────────
            // 4. GENERATIONS — THE CORE GC CONCEPT
            // ─────────────────────────────────────────────────────────────

            // GC uses GENERATIONAL COLLECTION based on one key observation:
            //
            // "Most objects die young"
            //
            // Short-lived objects:  local variables, temp buffers, request objects
            // Long-lived objects:   application config, caches, singletons


            // GENERATIONAL HYPOTHESIS:
            // → collecting only young objects (Gen0) is fast and efficient
            // → most garbage is found in Gen0 — where new objects live
            // → older objects (Gen1, Gen2) are checked less frequently


            // THREE GENERATIONS:
            //
            // GEN 0 — youngest generation
            // → brand new objects go here
            // → collected MOST frequently (every few MB of allocation)
            // → collection is FASTEST (small size ~256KB budget)
            // → most objects die here — never promoted
            //
            // GEN 1 — middle generation
            // → objects that survived ONE Gen0 collection
            // → buffer between Gen0 and Gen2
            // → collected less frequently than Gen0
            // → budget ~2MB
            //
            // GEN 2 — oldest generation
            // → objects that survived Gen1 collection
            // → long-lived objects — singletons, caches, statics
            // → collected LEAST frequently
            // → most expensive collection (entire heap scanned)
            // → LOH objects always here

            // VISUAL — GENERATION LAYOUT:
            //
            // ┌─────────────────────────────────────────────────────────┐
            // │ MANAGED HEAP                                            │
            // │                                                         │
            // │  GEN 0        │  GEN 1      │  GEN 2                   │
            // │  ~256KB        │  ~2MB       │  rest of heap            │
            // │  newest objs   │  survivors  │  long-lived objs         │
            // │  collected     │  collected  │  collected               │
            // │  most often    │  sometimes  │  rarely (full GC)        │
            // └─────────────────────────────────────────────────────────┘


            // ─────────────────────────────────────────────────────────────
            // 5. GC COLLECTION PROCESS — STEP BY STEP
            // ─────────────────────────────────────────────────────────────

            // TRIGGER:
            // GC runs when Gen0 budget is exhausted (filled up)
            // Can also run when system memory is low
            // Can be requested (but not guaranteed) via GC.Collect()

            // STEP 1 — SUSPENSION (Stop The World):
            // GC suspends ALL managed threads
            // Must pause threads to get consistent view of heap
            // Threads paused at SAFE POINTS (between IL instructions)
            // This pause is called "Stop The World" — STW pause


            // STEP 2 — MARKING (Find Live Objects):
            // GC starts from ROOTS and marks all reachable objects
            //
            // ROOTS are:
            // → Local variables on stack (method frames)
            // → Static fields (live for app lifetime)
            // → CPU registers holding object references
            // → GC handles (pinned objects, interop)
            //
            // GC walks from every root following all references
            // Every object reachable from a root = LIVE (marked)
            // Every object NOT reachable = GARBAGE (unmarked)

            void MarkingExample()
            {
                // root → Person → string
                var person2 = new Person { Name = "Ravi" };
                //  ↑ local var = ROOT
                //  Person object → MARKED LIVE ✅
                //  "Ravi" string → MARKED LIVE ✅ (reachable via person2.Name)

                Person orphan = new Person { Name = "Ghost" };
                orphan = null;  // no reference → NOT reachable from any root
                                //  Person object → NOT MARKED → GARBAGE ❌
                                //  "Ghost" string → NOT MARKED → GARBAGE ❌
            }

            // STEP 3 — SWEEPING (Free Garbage):
            // All UNMARKED objects are considered dead
            // Their memory is reclaimed
            // Available for future allocations

            // STEP 4 — COMPACTION (Reduce Fragmentation):
            // LIVE objects moved together (compacted)
            // Eliminates gaps left by collected objects
            // All references updated to new addresses
            // Next object pointer reset to after last live object
            //
            // BEFORE compaction:
            // │ Live │ DEAD │ Live │ DEAD │ DEAD │ Live │ FREE │
            //
            // AFTER compaction:
            // │ Live │ Live │ Live │ FREE SPACE .................│
            //                       ↑ Next object pointer here

            // STEP 5 — PROMOTION:
            // Objects that SURVIVED Gen0 collection → promoted to Gen1
            // Objects that SURVIVED Gen1 collection → promoted to Gen2
            // Long-lived objects eventually settle in Gen2

            void GenerationDemo()
            {
                // All new objects start in Gen0
                var obj1 = new Person { Name = "Ravi" };
                var obj2 = new Person { Name = "Mohan" };
                var obj3 = new Person { Name = "Priya" };

                Console.WriteLine(GC.GetGeneration(obj1)); // 0 — just created ✅

                // Trigger Gen0 collection
                GC.Collect(0);  // collect Gen0 only

                // obj1, obj2, obj3 still referenced → survived → promoted to Gen1
                Console.WriteLine(GC.GetGeneration(obj1)); // 1 — promoted ✅

                // Trigger Gen0 again
                GC.Collect(0);

                // Still referenced → survived Gen1 → promoted to Gen2
                Console.WriteLine(GC.GetGeneration(obj1)); // 2 — long lived ✅

                // Unreachable objects:
                var temp = new Person { Name = "Temp" };
                // temp goes out of scope → Gen0 collected → GONE ✅
                // never reaches Gen1 — "died young" as expected
            }

            // ─────────────────────────────────────────────────────────────
            // 12. GC.COLLECT() — WHEN AND WHY
            // ─────────────────────────────────────────────────────────────

            // GC.Collect() requests GC to run
            // NOT guaranteed to run immediately
            // Generally — DO NOT call in production code

            // WHY YOU SHOULD NOT CALL GC.Collect():
            // → GC already runs optimally on its own schedule
            // → Forcing GC at wrong time disrupts generational efficiency
            // → Forces objects into higher generations prematurely
            // → Causes unnecessary pauses in your application
            // → GC knows better than you when to run

            // WHEN IT IS ACCEPTABLE:
            // → after loading large data you know is now garbage
            // → before memory-sensitive benchmarks (testing only)
            // → after disposing large object graphs in batch processes

            void AcceptableGCCollect()
            {
                // loaded massive dataset — processed — now done with it
                ProcessMassiveDataset();

                // explicitly collect — we KNOW there is a lot of garbage now
                GC.Collect();                      // request collection
                GC.WaitForPendingFinalizers();     // wait for Finalizers to run
                GC.Collect();                      // collect objects freed by Finalizers
                                                   // second Collect needed because Finalizers can release more objects
            }

            void ProcessMassiveDataset() { /* ... */ }

            // NEVER DO THIS in a loop or hot path:
            void NeverDoThis()
            {
                for (int i = 0; i < 10000; i++)
                {
                    var data = new byte[1024];
                    // do work...
                    GC.Collect(); // ❌ TERRIBLE — forcing GC every iteration!
                                  // 10000 forced GCs — destroys performance
                }
            }
        }
    }
}
