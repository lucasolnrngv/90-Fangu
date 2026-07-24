using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_FiltersByThreshold_AndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        var high = TestSetup.AddProduct(db, stock: 15, sku: "SKU-HIGH");
        var low = TestSetup.AddProduct(db, stock: 8, sku: "SKU-LOW");
        var lowest = TestSetup.AddProduct(db, stock: 3, sku: "SKU-LOWEST");

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal(lowest.Id, result[0].ProductId);
        Assert.Equal(low.Id, result[1].ProductId);
        Assert.DoesNotContain(result, p => p.ProductId == high.Id);
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        TestSetup.AddProduct(db, stock: 2, isActive: false, sku: "SKU-INACTIVE");
        var active = TestSetup.AddProduct(db, stock: 2, isActive: true, sku: "SKU-ACTIVE");

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].ProductId);
    }

    [Fact]
    public async Task GetLowStock_UnitsSoldLast30Days_ExcludesCancelledOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2, sku: "SKU-CANCEL");

        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow,
            Items = { new OrderItem { ProductId = product.Id, Quantity = 5 } }
        });
        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = { new OrderItem { ProductId = product.Id, Quantity = 3 } }
        });
        db.SaveChanges();

        var result = await service.GetLowStockAsync(10);

        var row = Assert.Single(result);
        Assert.Equal(3, row.UnitsSoldLast30Days);
    }

    [Fact]
    public async Task GetLowStock_UnitsSoldLast30Days_ExcludesOrdersOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2, sku: "SKU-OLD");

        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            Items = { new OrderItem { ProductId = product.Id, Quantity = 7 } }
        });
        db.SaveChanges();

        var result = await service.GetLowStockAsync(10);

        var row = Assert.Single(result);
        Assert.Equal(0, row.UnitsSoldLast30Days);
    }
}
