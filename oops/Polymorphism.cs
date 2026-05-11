namespace oops
{
    // The word Polymorphism means "many forms". One interface, one method name, one reference — but different behavior depending on the actual object at runtime.

    // ============================================= Type 1 — Compile-time Polymorphism ==============================================

    // The compiler decides which method to call based on the number and types of arguments, Also called static polymorphism. Compiler known at compile time which method to call based on the signature (parameters).
    // method overloading
    public class Calculator
    {
        // Same method name — different signatures
        public int Add(int a, int b)
            => a + b;

        public double Add(double a, double b)
            => a + b;

        public int Add(int a, int b, int c)
            => a + b + c;

        public string Add(string a, string b)
            => a + b;   // concatenation
    }

    // Usage
    // var calc = new Calculator();
    // Console.WriteLine(calc.Add(2, 3));           // int    → 5
    // Console.WriteLine(calc.Add(2.5, 3.1));       // double → 5.6
    // Console.WriteLine(calc.Add(1, 2, 3));        // 3-arg  → 6
    // Console.WriteLine(calc.Add("Hello ", "C#")); // string → "Hello C#"

    // Operator Overloading

    public class Vector2D
    {
        public double X { get; }
        public double Y { get; }

        public Vector2D(double x, double y) { X = x; Y = y; }

        // Overload + operator
        public static Vector2D operator +(Vector2D a, Vector2D b)
            => new Vector2D(a.X + b.X, a.Y + b.Y);

        // Overload == operator
        public static bool operator ==(Vector2D a, Vector2D b)
            => a.X == b.X && a.Y == b.Y;

        public static bool operator !=(Vector2D a, Vector2D b) => !(a == b);

        public override string ToString() => $"({X}, {Y})";
    }

    // Usage
    // var v1 = new Vector2D(1, 2);
    // var v2 = new Vector2D(3, 4);
    // Console.WriteLine(v1 + v2);    // (4, 6) — calls our operator +
    // Console.WriteLine(v1 == v2);   // False

    // ============================================= END ===============================================================

    // ============================================= Type 2 — Runtime Polymorphism ==============================================
    // The decision of which method to call is made at runtime based on the actual type of the object, not the reference type. Also called dynamic polymorphism. Requires inheritance and virtual/override keywords.

    public abstract class Shape
    {
        public string Color { get; set; }

        public Shape(string color) => Color = color;

        // virtual — child classes MAY override
        public virtual void Draw()
        {
            Console.WriteLine($"Drawing a shape in {Color}");
        }

        // abstract — child classes MUST override (no body here)
        public abstract double Area();
    }

    public class Circle : Shape
    {
        public double Radius { get; }

        public Circle(string color, double radius) : base(color)
            => Radius = radius;

        public override void Draw()
            => Console.WriteLine($"Drawing Circle — radius {Radius}, color {Color}");

        public override double Area()
            => Math.PI * Radius * Radius;
    }

    public class Rectangle : Shape
    {
        public double Width { get; }
        public double Height { get; }

        public Rectangle(string color, double w, double h) : base(color)
        { Width = w; Height = h; }

        public override void Draw()
            => Console.WriteLine($"Drawing Rectangle {Width}x{Height}, color {Color}");

        public override double Area()
            => Width * Height;

        public static void ShouPloymorphic()
        {
            // Now the polymorphic usage — this is the part interviewers want to see:
            // All stored as base type Shape
            List<Shape> shapes = new List<Shape>
                                {
                                    new Circle("Red", 5),
                                    new Rectangle("Blue", 4, 6)
                                };

            // One loop, one method call — 3 different behaviors
            foreach (Shape shape in shapes)
            {
                shape.Draw();
                Console.WriteLine($"Area: {shape.Area():F2}");
                Console.WriteLine();
            }
        }
    }

    // ============================================= END ===============================================================


    // ============================================================================== The new keyword trap — hiding vs overriding ===============================================================================
    // This is a very popular interview trick. The difference between override and new is critical for understanding polymorphism:

    public class Animal
    {
        public virtual void Speak() => Console.WriteLine("Animal speaks");
        public void Breathe() => Console.WriteLine("Animal breathes");
    }

    public class Dog : Animal
    {
        public override void Speak() => Console.WriteLine("Dog barks");   // participates in polymorphism
        public new void Breathe() => Console.WriteLine("Dog breathes"); // this method gets hidden, NOT polymorphic

        public static void ShowNewVsOverride()
        {
            Animal animal = new Dog();
            animal.Speak();   // Dog barks — override works
            animal.Breathe(); // Animal breathes — new is hidden, calls base method

            Dog dog = new Dog();
            dog.Speak();      // Dog barks
            dog.Breathe();    // Dog breathes — new is visible when using Dog reference
        }
    }

    // ================================================================================== END =================================================================================================================

    // ======================================================== Abstract classes vs Interfaces — key differences and when to choose ===========================================================================

    // ======================================================== END ===========================================================================================================================================
}
