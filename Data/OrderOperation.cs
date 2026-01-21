using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SystemLogin.Core;

namespace SystemLogin;

public class OrderOperation(AppDbContext db)
{
    public Task<List<Order>> GetOrdersAsync()
    {
        return db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<int> CreateOrderAsync(int customerId, IReadOnlyDictionary<BlockColor, int> quantities)
    {
        var productsByColor = await db.Products.ToDictionaryAsync(p => p.Color);

        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Pending
        };

        foreach (var entry in quantities)
        {
            if (entry.Value <= 0)
                continue;

            if (!productsByColor.TryGetValue(entry.Key, out var product))
                throw new InvalidOperationException($"Product for color '{entry.Key}' is not configured.");

            order.Lines.Add(new OrderLine
            {
                ProductId = product.Id,
                Quantity = entry.Value
            });
        }

        if (order.Lines.Count == 0)
            throw new InvalidOperationException("Order must contain at least one block.");

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus status)
    {
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        order.Status = status;
        await db.SaveChangesAsync();
    }

    public Task<Order> GetOrderDetailsAsync(int orderId)
    {
        return db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .FirstAsync(o => o.Id == orderId);
    }
}
