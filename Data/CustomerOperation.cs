using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SystemLogin;

public class CustomerOperation(AppDbContext db)
{
    public async Task<List<Customer>> GetCustomersAsync()
    {
        return await db.Customers.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task CreateCustomerAsync(int userId, string name, string address)
    {
        db.Customers.Add(new Customer
        {
            UserId = userId,
            Name = name,
            Address = address
        });
        await db.SaveChangesAsync();
    }
}
