using System;
using System.Collections.Generic;
using System.Text;

namespace oops
{
    /// Encapsulation means two things working together:
    /// Bundling — keeping data (fields) and the methods that operate on that data together inside a class.
    /// Data hiding — restricting direct access to that data from outside the class, forcing all interaction through controlled methods.

    /// The problem without encapsulation, First, see what goes wrong when there's no protection:
    // BAD — no encapsulation
    public class BankAccount
    {
        public decimal Balance;   // anyone can touch this directly
        public string Pin;
    }

    /// This is a classic interview example that shows encapsulation elegantly:
    public class Temperature
    {
        private double _celsius;  // single source of truth

        public Temperature(double celsius)
        {
            Celsius = celsius;  // use property so validation runs
        }

        public double Celsius
        {
            get => _celsius;
            set
            {
                if (value < -273.15)
                    throw new ArgumentException("Below absolute zero!");
                _celsius = value;
            }
        }

        // Derived properties — computed, no backing field needed
        public double Fahrenheit
        {
            get => (_celsius * 9 / 5) + 32;
            set => Celsius = (value - 32) * 5 / 9;  // converts and stores
        }

        public double Kelvin
        {
            get => _celsius + 273.15;
            set => Celsius = value - 273.15;
        }

        public override string ToString() =>
            $"{_celsius:F1}°C / {Fahrenheit:F1}°F / {Kelvin:F1}K";
    }


    // Real problems when there's no encapsulation

    // Problem 1 — Invalid state (no validation possible)

    // NO encapsulation — all fields public
    public class Employee
    {
        public string Name;
        public int Age;
        public decimal Salary;
    }

    // Somewhere in the codebase...
    //Employee emp = new Employee();
    //emp.Age    = -5;         // nonsense — nobody stops it
    //emp.Salary = -50000;     // negative salary — corrupt data
    //emp.Name   = "";         // empty name — no check possible

    // There's nowhere to put validation. The field is just a memory slot — it accepts any value blindly. With encapsulation:
    public class Employee1
    {
        private int _age;
        public int Age
        {
            get => _age;
            set
            {
                if (value < 18 || value > 65)
                    throw new ArgumentException("Age must be 18–65.");
                _age = value;
            }
        }
    }
    // emp.Age = -5; → throws immediately, bug caught at the source

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    /// Problem 2 — Broken invariants (two fields that must stay in sync)
    /// -----------------------------------------------------------------
    
    // NO encapsulation
    public class Order
    {
        public List<string> Items;
        public int ItemCount;   // supposed to always equal Items.Count
    }

    /// Order order = new Order();
    /// order.Items = new List<string> { "Phone", "Charger" };
    /// order.ItemCount = 99;   // someone sets it wrong — now data is a lie
    /// Items.Count = 2, ItemCount = 99 — the object is in a broken state

    /// Two related fields are now out of sync. Any code that trusts ItemCount will get wrong results. With encapsulation, ItemCount becomes a computed property that can never lie:
    public class Order1
    {
        private List<string> _items = new List<string>();

        public IReadOnlyList<string> Items => _items.AsReadOnly();
        public int ItemCount => _items.Count;  // always correct, always in sync

        public void AddItem(string item) => _items.Add(item);
    }

    //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    /// Problem 3 — No way to change internal implementation later, This is the most important long-term problem
    
    // NO encapsulation — callers directly touch the field
    public class UserProfile
    {
        public string firstName;  // used directly by 50 other classes
    }

    // Six months later, you want to rename it to FullName...
    // Every single file that ever wrote `user.firstName` now has a compile error.
    // You have to find and fix them all.

    // WITH encapsulation — only the property name matters to callers
    public class UserProfile1
    {
        private string _firstName;   // rename this to anything — no one cares

        public string FirstName      // callers use THIS — you never break them
        {
            get => _firstName;
            set => _firstName = value;
        }
    }


    /// ========================================================================= ACCESS MODIFIERS — controlling who can see what ============================================

    // When a child class inherits from a parent, it inherits all members — but access modifiers control which ones the child can actually see and use.

    // The base class — all modifiers in one place
    public class Person
    {
        private string _ssn;             // Social security — only Person itself
        protected string _name;            // Person + any child class
        internal string _employeeCode;    // Anywhere in THIS project/assembly
        public string Email;            // The whole world

        protected internal string _region; // Children OR same project (either)
        private protected string _secret;  // Children AND same project (both required)

        public string Name { get => _name; set => _name = value; }

        private void ValidateSSN() { }     // Only Person
        protected void Log(string msg) { } // Person + children
        public string GetEmail() => Email; // Everyone
    }


    public class Employee11 : Person
    {
        public void ShowAccess()
        {
            // private — INVISIBLE to child
            // Console.WriteLine(_ssn);        // compile error
            // ValidateSSN();                  // compile error

            // protected — child CAN see it
            Console.WriteLine(_name);          // inherited, visible
            Log("Hello from child");           // inherited, visible

            // internal — visible IF same project
            Console.WriteLine(_employeeCode);  // (same project)

            // protected internal — visible (child OR same project — either is enough)
            Console.WriteLine(_region);        //

            // private protected — visible (child AND same project — both needed)
            Console.WriteLine(_secret);        // (we ARE in same project AND a child)

            // public — always visible
            Console.WriteLine(Email);          
            Console.WriteLine(GetEmail());     
        }
    }

    /// ========================================================================== END ============================================================
}
