namespace OrderHub.Core.Services;

public record LowStockProduct(int ProductId, string Sku, string Name, int StockQuantity, int UnitsSoldLast30Days);
