using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SystemLogin.Core;

namespace SystemLogin;

public class AppDbContext(string? dbPath = null) : DbContext
{
    private readonly string _dbPath =
        dbPath ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "database.sqlite"));

    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }
    // Execute raw SQL commands to ensure the database schema is created and correct
    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.executesqlrawasync?view=efcore-10.0
    public async Task EnsureSchemaAsync()
    {
        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                saltedPasswordHash BLOB NOT NULL,
                salt BLOB NOT NULL,
                isAdmin INTEGER NOT NULL
            );
            """);
        await Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username
            ON users(username);
            """);
        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                address TEXT NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users(id)
            );
            """);
        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                color INTEGER NOT NULL
            );
            """);
        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER NOT NULL,
                status INTEGER NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id)
            );
            """);
        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS order_lines (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                FOREIGN KEY (order_id) REFERENCES orders(id),
                FOREIGN KEY (product_id) REFERENCES products(id)
            );
            """);
    }
}

// Entity Definitions - Bridge between C# classes and SQL Tables
// https://learn.microsoft.com/en-us/ef/core/modeling/
[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Column("user_id")]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("salt")]
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    [Required]
    [Column("saltedPasswordHash")]
    public byte[] SaltedPasswordHash { get; set; } = Array.Empty<byte>();

    [Column("isAdmin")]
    public bool IsAdmin { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("color")]
    public BlockColor Color { get; set; }

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    [Column("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}

[Table("order_lines")]
public class OrderLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    [Column("product_id")]
    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }
}
