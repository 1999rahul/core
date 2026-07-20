
using UnitOfWorks.Database;
using UnitOfWorks.Services;

namespace UnitOfWorks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

            // SINGLETON: SqlConnectionFactory holds nothing but a connection string and knows how
            // to create connections. It has no per-request or mutable state, so one instance for
            // the entire app lifetime is safe and avoids pointless re-allocation.
            builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

            // SCOPED: one UnitOfWork per HTTP request.
            // UnitOfWork opens exactly ONE physical connection in its constructor and keeps it
            // open for the life of that instance. Getting this lifetime right matters:
            //   - Transient would open a brand-new connection every time something injects
            //     IUnitOfWork, defeating the "share one connection/transaction" purpose entirely.
            //   - Singleton would keep ONE connection open for the whole application's lifetime
            //     and share it (and any transaction on it) across concurrent, unrelated requests -
            //     which is unsafe and will corrupt data under load.
            //   - Scoped matches "one unit of work per request," which is exactly the semantic
            //     we want: everything that happens during this request shares one connection,
            //     and can share one transaction if BeginTransaction() is called.
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // NOTE: unlike the EF Core version, IRepository<T> is deliberately NOT registered
            // in the container (no `AddScoped(typeof(IRepository<>), typeof(Repository<>))` here).
            // In the EF Core version that worked because every Repository<T> could independently
            // resolve the same scoped DbContext from DI and still end up sharing state correctly.
            // Here, a Repository<T> needs the UnitOfWork's specific IDbConnection AND its current
            // IDbTransaction (which changes when BeginTransaction()/Commit() are called). DI has
            // no way to inject "whatever transaction happens to be active on this UnitOfWork right
            // now" - so repositories are created by UnitOfWork.Repository<T>() as a factory method
            // instead of being resolved directly. Consumers inject IUnitOfWork, not IRepository<T>.
            builder.Services.AddScoped<ProductService>();

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
