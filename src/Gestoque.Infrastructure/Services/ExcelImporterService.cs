using ClosedXML.Excel;
using Gestoque.Application.Common.Interfaces;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Gestoque.Infrastructure.Services;

public class ExcelImporterService
{
    private readonly IGestoqueDbContext _context;

    public ExcelImporterService(IGestoqueDbContext context)
    {
        _context = context;
    }

    public async Task<(int products, int suppliers, int movements)> ImportFromExcelAsync(string filePath, Guid tenantId)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo Excel não encontrado.", filePath);

        using var workbook = new XLWorkbook(filePath);

        int productsCount = 0;
        int suppliersCount = 0;
        int movementsCount = 0;

        // 1. IMPORTAR FORNECEDORES da aba "ENTRADA ENTREGA"
        var suppliersDict = new Dictionary<string, Supplier>(StringComparer.OrdinalIgnoreCase);
        if (workbook.TryGetWorksheet("ENTRADA ENTREGA", out var wsEntrega))
        {
            var lastRow = wsEntrega.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 4; r <= lastRow; r++)
            {
                var supName = wsEntrega.Cell(r, 1).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(supName) && !suppliersDict.ContainsKey(supName))
                {
                    var existingSup = await _context.Suppliers
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Name.ToLower() == supName.ToLower());

                    if (existingSup == null)
                    {
                        existingSup = new Supplier
                        {
                            TenantId = tenantId,
                            Name = supName,
                            IsActive = true
                        };
                        _context.Suppliers.Add(existingSup);
                        suppliersCount++;
                    }
                    suppliersDict[supName] = existingSup;
                }
            }
            await _context.SaveChangesAsync();
        }

        // 2. IMPORTAR PRODUTOS & SALDOS da aba "ESTOQUE"
        if (workbook.TryGetWorksheet("ESTOQUE", out var wsEstoque))
        {
            var lastRow = wsEstoque.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 8; r <= lastRow; r++)
            {
                var name = wsEstoque.Cell(r, 1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var stockVal = wsEstoque.Cell(r, 2).GetString().Trim();
                decimal currentStock = 0;
                if (decimal.TryParse(stockVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedStock))
                    currentStock = parsedStock;
                else if (decimal.TryParse(stockVal, NumberStyles.Any, new CultureInfo("pt-BR"), out parsedStock))
                    currentStock = parsedStock;

                // Validade
                DateTime? expiryDate = null;
                var cellVal = wsEstoque.Cell(r, 3);
                if (cellVal.DataType == XLDataType.DateTime)
                {
                    expiryDate = cellVal.GetDateTime();
                }
                else
                {
                    var valStr = cellVal.GetString().Trim();
                    if (DateTime.TryParse(valStr, out var parsedDate))
                        expiryDate = parsedDate;
                    else if (int.TryParse(valStr, out var year) && year >= 2024 && year <= 2035)
                        expiryDate = new DateTime(year, 12, 31);
                }

                var brand = wsEstoque.Cell(r, 4).GetString().Trim();
                var minStockStr = wsEstoque.Cell(r, 5).GetString().Trim();
                decimal minStock = 0;
                if (decimal.TryParse(minStockStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMin))
                    minStock = parsedMin;
                else if (decimal.TryParse(minStockStr, NumberStyles.Any, new CultureInfo("pt-BR"), out parsedMin))
                    minStock = parsedMin;

                var notes = wsEstoque.Cell(r, 6).GetString().Trim();

                // Unit determination
                var unit = UnitOfMeasure.UND;
                var upperName = name.ToUpper();
                if (upperName.Contains(" KG") || upperName.Contains("QUILO"))
                    unit = UnitOfMeasure.KG;
                else if (upperName.Contains(" ML") || upperName.Contains("LITRO") || upperName.Contains(" 1L") || upperName.Contains(" 2L") || upperName.Contains(" 900 ML"))
                    unit = UnitOfMeasure.L;
                else if (upperName.Contains("PACOTE"))
                    unit = UnitOfMeasure.PACOTE;
                else if (upperName.Contains("LATA"))
                    unit = UnitOfMeasure.LATA;
                else if (upperName.Contains("CAIXA"))
                    unit = UnitOfMeasure.CX;

                var product = await _context.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name.ToLower() == name.ToLower());

                if (product == null)
                {
                    product = new Product
                    {
                        TenantId = tenantId,
                        Name = name,
                        Brand = string.IsNullOrEmpty(brand) ? null : brand,
                        Unit = unit,
                        MinimumStock = minStock,
                        CurrentStock = currentStock,
                        Notes = string.IsNullOrEmpty(notes) ? null : notes,
                        IsActive = true
                    };
                    _context.Products.Add(product);
                    productsCount++;
                }
                else
                {
                    product.CurrentStock = currentStock;
                    product.MinimumStock = minStock;
                    if (!string.IsNullOrEmpty(brand)) product.Brand = brand;
                }

                // Batch for existing stock
                if (currentStock > 0)
                {
                    var batch = new Batch
                    {
                        TenantId = tenantId,
                        Product = product,
                        BatchNumber = $"LOTE-MIGRAÇÃO-{r}",
                        ExpiryDate = expiryDate,
                        InitialQuantity = currentStock,
                        CurrentQuantity = currentStock,
                        ReceivedDate = DateTime.UtcNow,
                        Brand = brand,
                        Notes = "Importado da planilha original"
                    };
                    _context.Batches.Add(batch);

                    var movement = new StockMovement
                    {
                        TenantId = tenantId,
                        Product = product,
                        Batch = batch,
                        MovementType = MovementType.Entrada,
                        MovementReason = MovementReason.AjusteInventario,
                        Quantity = currentStock,
                        MovementDate = DateTime.UtcNow,
                        Notes = "Saldo Inicial importado do Excel"
                    };
                    _context.StockMovements.Add(movement);
                    movementsCount++;
                }
            }
            await _context.SaveChangesAsync();
        }

        return (productsCount, suppliersCount, movementsCount);
    }
}

