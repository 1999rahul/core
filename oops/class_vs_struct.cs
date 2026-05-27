using System.Collections;
using System.Drawing;
using System.Runtime.Intrinsics.X86;

namespace oops
{
    class PointClass
    {
        public int X;
    }

    /// Classes vs Structs — Notes
    /// ============================

    /// 1. Core Difference
    /// Class → Reference type → stored on Heap
    /// Struct → Value type → behaves like a primitive(int, bool, double are structs too)

    /// 2. Copy Behavior
    /// Struct → copying = copy of data (independent)
    /// Class → copying = copy of pointer(both point to same object)
    
    class TestStruct1
    {
        public void Test()
        {
            // Struct — independent copy
            var p = new Point { X = 1 , };
            var q = p;
            q.X = 99;
            Console.WriteLine(p.X); // 1 — unchanged

            // Class — shared reference
            var a = new PointClass { X = 1 };
            var b = a;
            b.X = 99;
            Console.WriteLine(a.X); // 99 — affected!
        }
    }

    /// 3. Null Behavior
    /// Class → can be null
    /// Struct → cannot be null
    /// To allow null on struct → use T?
    class TestStruct2
    {
        public void Test()
        {
            PointClass obj = null;
            // Point s = null;    //  compile error
            Point? s = null;   // nullable struct
        }
    }

    /// 4. Inheritance
    /// Class → supports full inheritance 
    /// Struct → NO inheritance, but can implement interfaces
    /// Why structs can't inherit — 2 reasons:
    ///  Fixed size → compiler must know exact bytes to copy at compile time. Inheritance makes size unpredictable

    /// 5. Interface + Struct = Boxing
    /// IShape shape = new Circle { Radius = 5 }; // struct boxed to heap!
    /// Using a struct as an interface type causes boxing
    /// Boxing = copying value type from stack to heap, wrapped in object
    /// 

}
