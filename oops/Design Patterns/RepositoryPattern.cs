// ============================================================================
// UNIT OF WORK PATTERN — INTERVIEW STUDY REFERENCE (C#)
// Covers BOTH an EF Core-style implementation and a Dapper-style implementation
// ============================================================================
//
// WHAT PROBLEM DOES IT SOLVE?
// Once you're using the Repository Pattern, a single business operation often
// needs to touch more than one entity type — e.g. placing an order means
// inserting a new Order AND updating the User's last-order date. If each
// repository committed independently, a failure partway through could leave
// the database with the order saved but the user update lost (or vice versa).
// Unit of Work groups those operations into a single atomic transaction: they
// all succeed together, or none of them are persisted at all.
//
// THE CORE IDEA
// Unit of Work wraps one shared database context/connection and hands out
// repositories that all operate against that same context. The repositories
// only TRACK changes (Add/Update/Remove) — they never call SaveChanges/Commit
// themselves. The Unit of Work exposes a single Commit() method, and that is
// the only place anything is actually written to the database.
//
// EF CORE vs DAPPER — WHY THE IMPLEMENTATION LOOKS DIFFERENT
// - EF Core's DbContext has a CHANGE TRACKER. Add/Update/Remove just stage
//   intent in memory; nothing touches the database until SaveChanges() runs,
//   and SaveChanges() wraps everything it's about to write in one transaction
//   automatically. Because of this, DbContext is ALREADY a Unit of Work —
//   wrapping IUnitOfWork around it mostly buys you testability (mocking an
//   interface is easier than mocking DbContext) and decoupling business logic
//   from EF Core specifically, not atomicity itself (SaveChanges already
//   gives you that).
// - Dapper has NO change tracker. Every connection.Execute(...) call hits the
//   database immediately, as its own standalone statement. To make several
//   Dapper calls atomic, you must explicitly open an IDbTransaction yourself
//   and pass that SAME transaction object into every call you want included.
//   Forget to pass it on one call, and that statement silently auto-commits
//   outside the transaction. With Dapper, Unit of Work is NOT redundant —
//   there's no built-in equivalent, so this pattern (or TransactionScope) is
//   the standard way to get atomicity across multiple repository calls.
//
// COMMON INTERVIEW QUESTIONS
// - "Repository vs Unit of Work?" Repository abstracts access to ONE entity
//   type. Unit of Work coordinates a transaction across SEVERAL repositories
//   so they commit together. Usually paired, but they solve different things.
// - "Is this redundant with EF Core?" Mostly, for the atomicity part —
//   SaveChanges() already provides it. It still earns its place for
//   testability and for enforcing an architectural boundary (e.g. keeping EF
//   Core types out of the domain/application layer).
// - "What happens if Commit() throws partway through?" With EF Core: nothing
//   was ever written to the DB — changes were only staged — so there's
//   nothing to undo. With Dapper: writes already happened against the live
//   connection, so you need an explicit Rollback() to undo them.
//
// HOW TO RUN: drop into any .NET console app's Program.cs, or
//   dotnet run --project . / dotnet script UnitOfWorkPattern.cs
// This file is fully self-contained — no EF Core or Dapper NuGet packages
// required to compile and run it. The REAL production code using the actual
// EF Core / Dapper APIs is included as comment blocks in each section.
// ============================================================================


// ---------- Domain entities ----------
using System.Data;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
}

// ---------- Repositories: track changes, never commit ----------
public interface IUserRepository
{
    User GetById(int id);
    void Update(User user);
}

public interface IOrderRepository
{
    void Add(Order order);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context) => _context = context;

    public User GetById(int id) => _context.Users.Find(id);
    public void Update(User user) => _context.Users.Update(user); // no SaveChanges
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;
    public OrderRepository(AppDbContext context) => _context = context;

    public void Add(Order order) => _context.Orders.Add(order); // no SaveChanges
}

// ---------- Unit of Work: the ONLY place that commits ----------
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IOrderRepository Orders { get; }
    int Commit();
}

// Unit Of works using EF Core's DbContext as the shared context. The repositories it hands out
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository _users;
    private IOrderRepository _orders;

    // _context is injected once and reused by every repository this UoW hands out
    public UnitOfWork(AppDbContext context) => _context = context;

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);

    public int Commit() => _context.SaveChanges();

    public void Dispose() => _context.Dispose();
}

// ---------- Service layer: orchestrates the transaction ----------
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;
    public OrderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public void PlaceOrder(int userId, decimal total)
    {
        var user = _unitOfWork.Users.GetById(userId);
        if (user == null) throw new InvalidOperationException("User not found.");

        _unitOfWork.Orders.Add(new Order { UserId = userId, Total = total });
        user.LastOrderDate = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        // Both writes commit together here, or neither does
        _unitOfWork.Commit();
    }
}

// ---------- DI registration ----------
//services.AddDbContext<AppDbContext>();      // Scoped by default
//services.AddScoped<IUnitOfWork, UnitOfWork>();





// ===================  Unit Of works using Dapper (no change tracking, so we have to manage transactions ourselves) ===================    

public class DapUnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private IDbConnection _connection;
    private IDbTransaction _transaction;
    private IUserRepository _users;
    private IOrderRepository _orders;

    public UnitOfWork(string connectionString) => _connectionString = connectionString;
    // nothing opened yet

    // Lazily open the connection and transaction only when we first need to use them. This way, if a service only reads from one repository and never calls Commit(), we won't unnecessarily open a transaction and can be registered as  scoped or even transient without worrying about connection pooling issues.
    private void EnsureTransaction()
    {
        if (_connection != null) return;
        _connection = new SqlConnection(_connectionString);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public IUserRepository Users
    {
        get { EnsureTransaction(); return _users ??= new UserRepository(_connection, _transaction); }
    }

    public IOrderRepository Orders
    {
        get { EnsureTransaction(); return _orders ??= new OrderRepository(_connection, _transaction); }
    }

    public void Commit() => _transaction?.Commit();
    public void Rollback() => _transaction?.Rollback();
    public void Dispose() { _transaction?.Dispose(); _connection?.Dispose(); }
}

//services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString)); // Can be registered scoped
//services.AddScoped<OrderService>();