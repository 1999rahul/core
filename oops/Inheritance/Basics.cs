namespace oops.Inheritance
{
    /// <summary>
    /// Inheritance is a mechanism where one class (child/derived) acquires the members — fields, properties, methods — of another class (parent/base). The child gets everything the parent has, and can add new members or change existing ones.
    ///  The golden rule: inheritance models an "is-a" relationship.A Dog IS - A Animal.A Manager IS - A Employee.If the sentence doesn't hold naturally, inheritance is the wrong tool.
    /// </summary>

    public class Employee
    {
        // Properties inherited by every child class
        public string Name { get; set; }
        public decimal Salary { get; protected set; }

        public Employee(string name, decimal salary)
        {
            Name = name;
            Salary = salary;
        }

        // virtual = child classes CAN override this
        public virtual void Work()
        {
            Console.WriteLine($"{Name} is working.");
        }
    }

    public class Manager : Employee   // ":" means "inherits from"
    {
        public int TeamSize { get; set; }

        // "base(...)" calls the parent constructor
        public Manager(string name, decimal salary, int teamSize)
            : base(name, salary)
        {
            TeamSize = teamSize;
        }

        // override = replacing the parent's virtual method
        public override void Work()
        {
            Console.WriteLine($"{Name} is managing a team of {TeamSize} people.");
        }
    }


    // SeniorManager inherits from Manager, which inherits from Employee
    // It gets ALL members from BOTH parents
    public class SeniorManager : Manager
    {
        public int MentorCount { get; set; }

        public SeniorManager(string name, decimal salary, int teamSize, int mentorCount)
            : base(name, salary, teamSize)   // calls Developer's constructor
        {
            MentorCount = mentorCount;
        }

        public override void Work()
        {
            base.Work();   // prints Developer's message first
            Console.WriteLine($"  ...and mentoring {MentorCount} juniors.");
        }
    }

    // ==================================================================== SEALED CLASSES AND METHODS =========================================================
    /// sealed — stopping inheritance, sealed on a class means nobody can inherit from it. sealed on a method means no further override is allowed.

    // sealed CLASS — cannot be subclassed at all
    public sealed class UtilityHelper
    {
        public static void Log(string msg) => Console.WriteLine(msg);
    }

    // class Derived : UtilityHelper { }  // compile error


    // sealed METHOD — stops the chain of overrides at this level
    public class Vehicle
    {
        public virtual void StartEngine() => Console.WriteLine("Generic engine start");
    }


    public class Car : Vehicle
    {
        public sealed override void StartEngine()  // sealed here
        {
            Console.WriteLine("Car engine: vroom");
        }
    }

    public class SportsCar : Car
    {
        // public override void StartEngine() { }  // compile error — Car sealed it and cannot be overridden further
    }

    // ============================================================================== END ===============================================================================


    // =========================================================================== ABSTRACT CLASSES AND METHODS ============================================================

    // abstract — forcing the child to implement
    // An abstract class cannot be instantiated. An abstract method has no body in the parent — the child MUST provide one.

    public abstract class PaymentGateway
    {
        public string MerchantId { get; }

        public PaymentGateway(string merchantId)
        {
            MerchantId = merchantId;
        }

        // abstract = no body here — child MUST override
        public abstract bool ProcessPayment(decimal amount); // If a class has abstract members, the class itself must be declared abstract. An abstract class can have both abstract and concrete members.
        public abstract void Refund(decimal amount);

        // concrete method — shared by all children, not overridable here
        public void PrintReceipt(decimal amount)
        {
            Console.WriteLine($"Receipt: {amount:C} via merchant {MerchantId}");
        }
    }

    public class StripeGateway : PaymentGateway
    {
        public StripeGateway(string merchantId) : base(merchantId) { }

        public override bool ProcessPayment(decimal amount)  // must override abstract methods
        {
            Console.WriteLine($"Stripe: charging {amount:C}");
            return true;
        }

        public override void Refund(decimal amount)
        {
            Console.WriteLine($"Stripe: refunding {amount:C}");
        }
    }

    // ============================================================================== END ===============================================================================

    // =========================================================================== Constructor execution order ===========================================================================

    public class A
    {
        public A() => Console.WriteLine("A constructor");
    }

    public class B : A
    {
        public B() => Console.WriteLine("B constructor");
    }

    public class C : B
    {
        public C() => Console.WriteLine("C constructor");
    }

    // new C();
    // Output:
    // A constructor   ← grandparent first
    // B constructor   ← then parent
    // C constructor   ← child last
    // Always: top-down on construction, bottom-up on garbage collection

    // ============================================================================== END ===============================================================================

    // =========================================================================== Implementing multiple inheritance ===========================================================================

    // C# does NOT support multiple inheritance of classes (a class cannot inherit from more than one class), but it supports multiple inheritance of interfaces (a class can implement multiple interfaces).
    // This avoids the "diamond problem" where two parent classes have a method with the same signature, causing ambiguity for the child class.

    // This is ILLEGAL in C#
    // public class HybridVehicle : Car, Boat { }

    // Why Multiple Inheritance of Classes is not allowed in C#:

    // Problem 1 — Method Ambiguity (No common parent needed)
    // Even if Car and Boat are fully independent classes, same method name = compiler confusion.

    class Car1 { public string Move() => "Car driving"; }
    class Boat1 { public string Move() => "Boat sailing"; }

    // class AmphibiousCar : Car, Boat1{ }  // COMPILE ERROR
    // Which Move() does AmphibiousCar inherit? Compiler gives up


    // Problem 2 — Diamond Problem (With common parent)

    class Vehicle1 { public int Speed = 0; }  // one field
    class Car2 : Vehicle { }  // gets its OWN copy of Speed
    class Boat2 : Vehicle { }  // gets its OWN copy of Speed

    // If allowed:
    //class AmphibiousCar : Car2, Boat2
    //{
    //    // TWO copies of Speed — Car's or Boat's? 
    //    // Which constructor runs first? Vehicle runs TWICE? 
    //}

    // Why Interfaces Don't Have This Problem
    // Classes  →  carry IMPLEMENTATION  →  two versions clash
    // Interfaces →  carry only CONTRACT  →  child writes its own version

    // Compiler Limit or Runtime Limit?
    // Key interview point: The .NET CLR (runtime) can actually handle multiple inheritance at the IL level. It is the C# compiler that blocks it — a deliberate language design decision.

    // Solutions in C#

    // Multiple Interfaces (Primary)
    interface IDriveable { void Move(); }  // no body = no conflict
    interface ISailable { void Move(); }  // no body = no conflict

    class AmphibiousCar : IDriveable, ISailable
    {
        // Explicit Interface Implementation — removes all ambiguity
        void IDriveable.Move() => Console.WriteLine("Driving on road!");
        void ISailable.Move() => Console.WriteLine("Sailing on water!");
    }

    //   AmphibiousCar a = new AmphibiousCar();
    //   ((IDriveable) a).Move();  // Driving on road!
    //   ((ISailable) a).Move();   // Sailing on water!
    // Casting works here because AmphibiousCar successfully implements both interfaces — the IS-A relationship exists. Unlike classes, where the inheritance never compiles.

    // Solution 2 — Abstract Class + Multiple Interfaces

    abstract class AbstractVehicle
    {
        public string Name { get; set; }
        public int Speed { get; set; }
        public abstract void ShowDetails();
    }

    interface IDriveableVehicle { void Drive(); }
    interface ISailableVehicle { void Sail(); void Drive(); }

    // One base class + multiple interfaces
    class AmphibiousVehicle : AbstractVehicle, IDriveableVehicle, ISailableVehicle
    {
        public override void ShowDetails() =>
            Console.WriteLine($"{Name} going at {Speed} km/h");

        public void Drive() => Console.WriteLine("Driving!");
        public void Sail() => Console.WriteLine("Sailing!");
    }

    // Solution 3 — Composition (Has-A over Is-A)

    class DriveBehavior { public void Drive() => Console.WriteLine("Driving!"); }
    class SailBehavior { public void Sail() => Console.WriteLine("Sailing!"); }

    // Most flexible and testable. Preferred in modern OOP.
    class AmphibiousCarVehicle
    {
        private DriveBehavior _driver = new DriveBehavior();
        private SailBehavior _sailor = new SailBehavior();

        public void Drive() => _driver.Drive();  // delegates to object
        public void Sail() => _sailor.Sail();   // delegates to object
    }


    // ============================================================================= END ===============================================================================

    // ========================================================================== Sealed classes and methods ========================================================================

    // A sealed class in C# is a class that cannot be inherited. If another class tries to derive from a sealed class, the compiler will throw an error
    // If inheritance is a family tree, a sealed class is the end of the bloodline. It can have parents, but it cannot have children.

    public sealed class SecuritySystem
    {
        public void Authenticate() { /* ... */ }
    }

    // You can also use the sealed keyword on methods and properties, but only if they are overriding a virtual member from a base class. You cannot use sealed on a standard method. It is used to stop further overriding down the inheritance chain

    public class BaseClass
    {
        public virtual void DoWork() { }
    }

    public class DerivedClass : BaseClass
    {
        // Overrides the base method, but SEALS it so classes deriving 
        // from DerivedClass cannot override it further.
        public sealed override void DoWork() { }
    }

    // Golden Rules to Memorize
    // sealed vs. abstract: A class cannot be both abstract and sealed. They are exact opposites. abstract forces inheritance; sealed prevents it.
    // sealed vs. static: A static class cannot be instantiated or inherited. A sealed class can be instantiated (created with the new keyword), it just can't be inherited.
    // Structs are implicitly sealed: You cannot inherit from a struct in C#. Every struct in C# is effectively sealed by default.

    // THE TRICK QUESTION: Sealing without the 'sealed' keyword
    // Q: How do you prevent a class from being inherited without using the sealed keyword?
    // A: Make all of its constructors PRIVATE. Since derived classes must call a base 
    //    constructor, hiding it prevents inheritance.

    public class PseudoSealedClass
    {
        // 1. Private constructor prevents external inheritance
        private PseudoSealedClass() { }

        // 2. Provide a static factory method so the class can still be instantiated
        public static PseudoSealedClass CreateInstance()
        {
            return new PseudoSealedClass();
        }

        // 3. THE LOOPHOLE (Mention this to impress the interviewer):
        // A nested class CAN still inherit from it, because nested classes 
        // have access to the private members of their parent class.
        public class NestedDerivedClass : PseudoSealedClass
        {
            public NestedDerivedClass() : base()
            {
                // This works perfectly fine!
            }
        }
    }

    // ========================================================================
    // REAL-WORLD ENTERPRISE SCENARIOS FOR SEALED CLASSES
    // ========================================================================

    // ------------------------------------------------------------------------
    // 1. SECURITY (Defense in Depth)
    // ------------------------------------------------------------------------
    // Scenario: You are building code that handles passwords, tokens, or encryption.
    // Reason: Prevents "Method Hijacking." If unsealed, a malicious or careless 
    // developer could inherit, override the hashing method to log plain-text 
    // passwords to a file, and then call base.Hash().

    public sealed class PasswordHasher
    {
        public string Hash(string plainTextPassword)
        {
            // Complex cryptographic hashing logic goes here
            return "hashed_string";
        }
    }


    // ------------------------------------------------------------------------
    // 2. THE SINGLETON PATTERN
    // ------------------------------------------------------------------------
    // Scenario: Application-wide services like a database connection pool or config manager.
    // Reason: A Singleton MUST have exactly one instance. If it's unsealed, another 
    // class could inherit from it, expose a new public constructor, and create 
    // multiple instances, entirely breaking the architectural pattern.

    public sealed class DatabaseConnectionPool
    {
        // Static instance created once
        private static readonly DatabaseConnectionPool _instance = new DatabaseConnectionPool();

        // Private constructor prevents external instantiation
        private DatabaseConnectionPool()
        {
            // Initialize heavy database connections
        }

        public static DatabaseConnectionPool Instance => _instance;

        public void ExecuteQuery(string query) { /* Execution logic */ }
    }

    // ========================================================================== END ===================================================================================

}
