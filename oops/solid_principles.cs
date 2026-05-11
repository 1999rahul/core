namespace oops
{
    // SOLID is a set of 5 design principles that make code easier to maintain, extend, and test. Each letter is one principle.

    // ======================================================================= 1.Single Responsibility Principle ====================================================================
    
    // Every class should do exactly one thing and own that thing completely. If you can describe a class's job using "and", it's doing too much.

    // The problem
    // BAD — this class does THREE completely unrelated things
    public class Employee12
    {
        public string Name { get; set; }

        public decimal Salary { get; set; }

        // Responsibility 1: business logic
        public decimal CalculateBonus()
        {
            return Salary * 0.10m;
        }

        // Responsibility 2: database — totally different concern
        public void SaveToDatabase()
        {
            Console.WriteLine($"INSERT INTO Employees VALUES ('{Name}', {Salary})");
        }

        // Responsibility 3: reporting — yet another concern
        public string GeneratePayslip()
        {
            return $"--- Payslip ---\nName: {Name}\nSalary: {Salary:C}\nBonus: {CalculateBonus():C}";
        }
    }

    // If you change the database schema → you touch Employee. If you change the payslip format → you touch Employee. If you change bonus rules → you touch Employee. Three reasons to change = SRP violated.

    // The fix — split into focused classes
    // Responsibility 1: ONLY the data + business logic
    public class Employee123
    {
        public string Name { get; set; }
        public decimal Salary { get; set; }

        public decimal CalculateBonus() => Salary * 0.10m;
    }

    // Responsibility 2: ONLY persistence
    public class EmployeeRepository
    {
        public void Save(Employee emp)
        {
            Console.WriteLine($"INSERT INTO Employees VALUES ('{emp.Name}', {emp.Salary})");
        }

        public Employee GetById(int id)
        {
            return new Employee { Name = "Rahul", Salary = 80000m };
        }
    }

    // Responsibility 3: ONLY report generation
    public class PayslipGenerator
    {
        public string Generate(Employee12 emp)
        {
            return $"--- Payslip ---\n" +
                   $"Name:   {emp.Name}\n" +
                   $"Salary: {emp.Salary:C}\n" +
                   $"Bonus:  {emp.CalculateBonus():C}";
        }
    }

    // Now changing the database touches only EmployeeRepository. Changing the payslip format touches only PayslipGenerator. Zero cross-contamination.

    // ================================================================== END =====================================================================




    // =========================================================== 2.Open/Closed Principle ====================================================================
    
    // Classes should be open for extension, but closed for modification
    // You should be able to add new behavior without editing existing, working, tested code. The classic way: add a new class, not new if/switch blocks.

    // The problem
    // BAD — every new shape means editing this class and adding another if-block
    public class AreaCalculator
    {
        public double Calculate(object shape)
        {
            if (shape is Circle c)
                return Math.PI * c.Radius * c.Radius;

            if (shape is Rectangle r)
                return r.Width * r.Height;

            // Adding a Triangle means editing this method — VIOLATION
            //if (shape is Triangle t)
            //    return 0.5 * t.Base * t.Height;

            throw new ArgumentException("Unknown shape");
        }
    }

    // Every new shape type forces you to modify AreaCalculator. It's never "closed" — it always needs editing.

    // The fix — extend via abstraction

    // AreaCalculator — never needs to change again for new shapes
    public class AreaCalculator1
    {
        public double Calculate(Shape shape) => shape.Area();

        // Add any new shape and no need to change existing code
        public double TotalArea(List<Shape> shapes)
            => shapes.Sum(s => s.Area());
    }

    // ============================================================ END =====================================================================



    // ============================================================ 3.Liskov Substitution Principle =====================================================================

    // Objects of a child class must be usable wherever the parent class is expected, without breaking the program
    // If you swap a parent-type variable for any child, the code must still work correctly. This is violated when a child class throws exceptions the parent doesn't, returns unexpected values, or ignores method contracts.


    // The classic violation — the Square/Rectangle trap

    // BAD — Square inherits Rectangle but breaks its contract
    public class Rectangle123
    {
        public virtual double Width { get; set; }
        public virtual double Height { get; set; }

        public double Area() => Width * Height;
    }

    public class Square123 : Rectangle123
    {
        // Square forces both sides equal — but this breaks Rectangle's contract
        public override double Width
        {
            set { base.Width = value; base.Height = value; }  // ← side effect!
        }
        public override double Height
        {
            set { base.Width = value; base.Height = value; }  // ← side effect!
        }

        // This works fine with Rectangle
        void ResizeAndCheck(Rectangle123 r)
        {
            r.Width = 4;
            r.Height = 5;
            Console.WriteLine(r.Area()); // Expected: 20

            ResizeAndCheck(new Rectangle123()); // prints 20
            ResizeAndCheck(new Square123());    // prints 25
        }
    }

    // The fix — shared abstraction, not forced inheritance

    // Correct model: both shapes share an abstraction, neither inherits the other
    public abstract class Shape21
    {
        public abstract double Area();
    }

    public class Rectangle21 : Shape21
    {
        public double Width { get; set; }
        public double Height { get; set; }

        public Rectangle21(double w, double h) { Width = w; Height = h; }

        public override double Area() => Width * Height;
    }

    public class Square21 : Shape21
    {
        public double Side { get; set; }

        public Square21(double side) => Side = side;

        public override double Area() => Side * Side;

        // Now substitution works — both ARE shapes, neither pretends to be the other
        void PrintArea(Shape s) => Console.WriteLine($"Area: {s.Area()}");

        //PrintArea(new Rectangle(4, 5)); // Area: 20 
        //PrintArea(new Square(4));       // Area: 16 
    }

    // ============================================================ END =====================================================================


    // ============================================================ 4.Interface Segregation Principle =====================================================================

    // No class should be forced to implement methods it doesn't use.
    // Fat interfaces that bundle unrelated methods force implementing classes to provide fake or empty implementations. Split them into focused, role-specific interfaces.

    // BAD — one giant interface forces all implementers to deal with everything
    public interface IWorker
    {
        void Work();
        void TakeBreak();
        void AttendMeeting();
        void SubmitTimeshee();
        void DriveForklift();    // not every worker drives a forklift!
        void ManageTeam();       // not every worker manages!
    }

    // A Cleaner is forced to fake unneeded methods
    public class Cleaner : IWorker
    {
        public void Work() => Console.WriteLine("Cleaning...");
        public void TakeBreak() => Console.WriteLine("On break.");
        public void AttendMeeting() => throw new NotImplementedException(); // ❌ forced
        public void SubmitTimeshee() => Console.WriteLine("Submitting timesheet.");
        public void DriveForklift() => throw new NotImplementedException(); // ❌ forced
        public void ManageTeam() => throw new NotImplementedException(); // ❌ forced
    }

    // The fix — small, focused interfaces

    // Each interface = one capability
    public interface IWorkable { void Work(); }
    public interface IBreakable { void TakeBreak(); }
    public interface IMeetingGoer { void AttendMeeting(); }
    public interface ITimesheetFiler { void SubmitTimesheet(); }
    public interface IForkliftDriver { void DriveForklift(); }
    public interface ITeamManager { void ManageTeam(); }

    // Each class implements only what it actually does
    public class Cleaner1 : IWorkable, IBreakable, ITimesheetFiler
    {
        public void Work() => Console.WriteLine("Cleaning floors.");
        public void TakeBreak() => Console.WriteLine("Coffee break.");
        public void SubmitTimesheet() => Console.WriteLine("Timesheet submitted.");
    }

    public class WarehouseWorker : IWorkable, IBreakable, IForkliftDriver, ITimesheetFiler
    {
        public void Work() => Console.WriteLine("Moving inventory.");
        public void TakeBreak() => Console.WriteLine("Lunch break.");
        public void DriveForklift() => Console.WriteLine("Operating forklift.");
        public void SubmitTimesheet() => Console.WriteLine("Timesheet submitted.");
    }

    public class Manager : IWorkable, IBreakable, IMeetingGoer, ITimesheetFiler, ITeamManager
    {
        public void Work() => Console.WriteLine("Planning sprint.");
        public void TakeBreak() => Console.WriteLine("Break.");
        public void AttendMeeting() => Console.WriteLine("In standup.");
        public void SubmitTimesheet() => Console.WriteLine("Timesheet submitted.");
        public void ManageTeam() => Console.WriteLine("Reviewing PRs.");
    }

    // ============================================================== END ====================================================================

    // ============================================================ 5.Dependency Inversion Principle =====================================================================
    // High-level modules should not depend on low-level modules. Both should depend on abstractions. Abstractions should not depend on details. Details should depend on abstractions.
    // Think like services should depend on interfaces, not concrete implementations. This allows you to swap out implementations without changing the high-level code.

    // ============================================================== END =====================================================================
}
