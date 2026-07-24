using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold)
    {
        var products = await _productRepository.GetLowStockAsync(threshold);
        var unitsSold = await _orderRepository.GetUnitsSoldByProductAsync(DateTime.UtcNow.AddDays(-30));

        return products
            .Select(p => new LowStockProduct(
                p.Id, p.Sku, p.Name, p.StockQuantity,
                unitsSold.TryGetValue(p.Id, out var qty) ? qty : 0))
            .ToList();
    }
}
