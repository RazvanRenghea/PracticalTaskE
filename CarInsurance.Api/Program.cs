using CarInsurance.Api.Data;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddScoped<CarService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<PolicyExpirationLogger>();

var app = builder.Build();

// Ensure DB and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.EnsureSeeded(db);
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS InsuranceClaim (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CarId INTEGER NOT NULL,
            ClaimDate TEXT NOT NULL,
            Description TEXT NOT NULL,
            Amount REAL NOT NULL,
            FOREIGN KEY (CarId) REFERENCES Cars(Id)
        );
    ");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ProcessedExpirations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PolicyId INTEGER NOT NULL,
            LoggedAt TEXT NOT NULL,
            FOREIGN KEY (PolicyId) REFERENCES Policies(Id)
        );
    ");

}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
