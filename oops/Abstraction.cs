using oops.Inheritance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using System.Text;

namespace oops
{
    // Abstraction means showing what an object does, hiding how it does it. You expose a clean interface — the "what" — and keep the complex implementation details — the "how" — locked away inside.
    // Think of a TV remote. You press the volume button (public interface). You don't know or care about the infrared signals, receiver circuits, and firmware processing happening inside (private implementation). That's abstraction.

    // Two tools in C# to achieve it:
    // abstract class — partial abstraction(mix of defined + undefined)
    // interface — pure abstraction(only the contract)

    // Abstract class 
    // An abstract class is a half-finished blueprint.It defines what child classes must do (abstract methods) and can also provide shared code that's already done (concrete methods).

    // Interface 
    // An interface is a pure contract. No state, no implementation — just a list of methods the implementer promises to provide.

    // Abstract Classes vs Interfaces

    // Use ABSTRACT CLASS when:
    // ─────────────────────────────────────────────────────
    // Classes share a common identity("IS-A" relationship)
    // You want to share fields / state across children
    // You have common logic that all children reuse
    // You want a constructor to enforce initialization
    // You want protected methods only subclasses can access
    // Example: Animal, Vehicle, BankAccount, Shape

    // Use INTERFACE when:
    // ─────────────────────────────────────────────────────
    // Unrelated classes need the same capability
    // You need multiple inheritance-like behavior
    // You want to define a contract with no shared state
    // You are building for plug-and-play / dependency injection
    // You want loose coupling(great for testing with mocks)
    // Example: IFlyable, ISaveable, IPrintable, INotificationService


    //   FoodDeliveryApp
    //│
    //├── Abstract Classes(shared identity)
    //│     ├── User          — base for all users
    //│     ├── MenuItem      — base for all food items
    //│     └── DeliveryAgent — base for all delivery agents
    //│
    //├── Interfaces(capabilities)
    //│     ├── IPayable         — can make payments
    //│     ├── ITrackable       — can be tracked live
    //│     ├── INotifiable      — can receive notifications
    //│     ├── IDiscountable    — can apply discounts
    //│     └── IRatable         — can be rated
    //│
    //└── Concrete Classes(actual objects)
    //      ├── Customer         — User + IPayable + INotifiable + IRatable
    //      ├── RestaurantOwner  — User + INotifiable
    //      ├── Burger           — MenuItem + IDiscountable
    //      ├── Pizza            — MenuItem + IDiscountable
    //      ├── BikeAgent        — DeliveryAgent + ITrackable + INotifiable
    //      └── Order            — ties everything together

    // Every single table in the database has these columns
    // Perfect for abstract class — shared STATE across all entities
    abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Every entity must be able to describe itself — but differently
        public abstract string GetEntityName();
        public abstract string GetTableName();

        // Shared behavior — same for ALL entities
        public void MarkUpdated()
        {
            UpdatedAt = DateTime.Now;
            Console.WriteLine($"[DB]    {GetEntityName()} #{Id} timestamp updated.");
        }

        public void Deactivate()
        {
            IsActive = false;
            MarkUpdated();
            Console.WriteLine($"[DB]    {GetEntityName()} #{Id} deactivated.");
        }
    }

    // Basic CRUD — every repository must support this
    interface IRepository<T> where T : BaseEntity
    {
        void Insert(T entity);
        T GetById(int id);
        void Update(T entity);
        void Delete(int id);
        List<T> GetAll();
    }

    // Repositories that support search/filter
    interface ISearchable<T>
    {
        List<T> Search(string keyword);
        List<T> Filter(Func<T, bool> predicate);
    }

    interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string DeletedBy { get; set; }

        void SoftDelete(string deletedBy);
        void Restore();
    }

    // Tables that track who created and last modified a record
    interface IAuditable
    {
        string CreatedBy { get; set; }
        string ModifiedBy { get; set; }

        void SetCreatedBy(string user);
        void SetModifiedBy(string user);
    }

    class User : BaseEntity, IAuditable
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }  // Admin, Customer, Agent
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        public User(int id, string username, string email, string role)
        {
            Id = id;
            Username = username;
            Email = email;
            Role = role;
        }

        // Abstract methods from BaseEntity
        public override string GetEntityName() => "User";
        public override string GetTableName() => "tbl_users";

        // IAuditable
        public void SetCreatedBy(string user)
        {
            CreatedBy = user;
            Console.WriteLine($"[AUDIT] User #{Id} created by: {user}");
        }

        public void SetModifiedBy(string user)
        {
            ModifiedBy = user;
            MarkUpdated();
            Console.WriteLine($"[AUDIT] User #{Id} last modified by: {user}");
        }
    }

    //    DatabaseApp
    //│
    //├── Abstract Classes(shared identity)
    //│     ├── BaseRepository<T>   — common DB operations for ALL entities
    //│     └── BaseEntity          — common fields every DB table has
    //│
    //├── Interfaces(capabilities)
    //│     ├── IRepository<T>      — CRUD contract
    //│     ├── ISoftDeletable      — can be soft deleted(not physically removed)
    //│     ├── IAuditable          — tracks who created / modified a record
    //│     ├── ICacheable          — can be cached in memory
    //│     └── ISearchable<T>      — can be searched / filtered
    //│
    //└── Concrete Classes(actual tables/repos)
    //      ├── User                — BaseEntity + IAuditable
    //      ├── Product             — BaseEntity + IAuditable + ISoftDeletable + ICacheable
    //      ├── Order               — BaseEntity + IAuditable + ISoftDeletable
    //      ├── UserRepository      — BaseRepository + ISearchable
    //      ├── ProductRepository   — BaseRepository + ISearchable + ICacheable
    //      └── OrderRepository     — BaseRepository
}

