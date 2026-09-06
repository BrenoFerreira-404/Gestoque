using ClosedXML.Excel;
using Gestoque.Application.Common.Interfaces;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

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
        if (!TryGetWorksheetCaseInsensitive(workbook, "ENTRADA ENTREGA", out var wsEntrega))
        {
            throw new InvalidOperationException("Aba 'ENTRADA ENTREGA' não encontrada na planilha.");
        }

        var suppliersDict = new Dictionary<string, Supplier>(StringComparer.OrdinalIgnoreCase);
        var entregaLastRow = wsEntrega.LastRowUsed()?.RowNumber() ?? 0;
        if (entregaLastRow >= 4)
        {
            for (int r = 4; r <= entregaLastRow; r++)
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
        if (!TryGetWorksheetCaseInsensitive(workbook, "ESTOQUE", out var wsEstoque))
        {
            throw new InvalidOperationException("Aba 'ESTOQUE' não encontrada na planilha.");
        }

        var estoqueHeaderRow = FindHeaderRow(wsEstoque, "ALIMENTO")
            ?? throw new InvalidOperationException("Não foi possível localizar o cabeçalho 'ALIMENTO' na aba ESTOQUE.");

        var productsCache = await LoadProductsCacheAsync(tenantId);
        var productsByRowId = new Dictionary<int, Product>();

        var lastRow = wsEstoque.LastRowUsed()?.RowNumber() ?? 0;
        var spreadsheetRowId = 0;
        for (int r = estoqueHeaderRow + 1; r <= lastRow; r++)
        {
            var name = wsEstoque.Cell(r, 1).GetString().Trim();
            if (IsFoodSectionBreak(name))
                break;

            if (string.IsNullOrWhiteSpace(name))
                continue;

            spreadsheetRowId++;

            var currentStock = ParseDecimal(wsEstoque.Cell(r, 2).GetString().Trim());
            var expiryDate = ParseExpiryDate(wsEstoque.Cell(r, 3));
            var brand = wsEstoque.Cell(r, 4).GetString().Trim();
            var minStock = ParseDecimal(wsEstoque.Cell(r, 5).GetString().Trim());
            var notes = wsEstoque.Cell(r, 6).GetString().Trim();
            var unit = ResolveUnit(name);

            var (product, created) = await GetOrCreateProductAsync(
                tenantId, productsCache, name, brand, unit, minStock, currentStock, notes);

            if (created) productsCount++;
            productsByRowId[spreadsheetRowId] = product;

            if (currentStock > 0)
            {
                AddInitialStockMovement(tenantId, product, currentStock, expiryDate, brand,
                    $"LOTE-MIGRAÇÃO-{r}", "Importado da planilha original", "Saldo Inicial importado do Excel");
                movementsCount++;
            }
        }

        // 2b. IMPORTAR PRODUTOS & SALDOS da seção PROTEINAS (se existir)
        var proteinHeaderRow = FindHeaderRowAfter(wsEstoque, "PRODUTO", FindHeaderRow(wsEstoque, "PROTEINAS") ?? 0);
        if (proteinHeaderRow.HasValue)
        {
            for (int r = proteinHeaderRow.Value + 1; r <= wsEstoque.LastRowUsed()?.RowNumber(); r++)
            {
                var name = wsEstoque.Cell(r, 1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name) || name.Equals("TOTAL DE ITENS", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Seção PROTEINAS: col1=PRODUTO, col2=QUANT.(kg), col3=VALIDADE
                var currentStock = ParseDecimal(wsEstoque.Cell(r, 2).GetString().Trim());
                var expiryDate = ParseExpiryDate(wsEstoque.Cell(r, 3));

                var (product, created) = await GetOrCreateProductAsync(
                    tenantId, productsCache, name, brand: null, UnitOfMeasure.KG, minStock: 0, currentStock, notes: null);

                if (created) productsCount++;

                if (currentStock > 0)
                {
                    AddInitialStockMovement(tenantId, product, currentStock, expiryDate, brand: null,
                        $"LOTE-MIGRAÇÃO-PROT-{r}", "Importado da planilha original - proteínas",
                        "Saldo Inicial importado do Excel - proteínas");
                    movementsCount++;
                }
            }
        }

        await _context.SaveChangesAsync();

        // 3. IMPORTAR HISTÓRICO DE ENTREGAS (sem alterar saldo — já refletido no ESTOQUE)
        if (entregaLastRow >= 4)
        {
            for (int r = 4; r <= entregaLastRow; r++)
            {
                var supName = wsEntrega.Cell(r, 1).GetString().Trim();
                var itemName = wsEntrega.Cell(r, 2).GetString().Trim();
                var quantity = ParseDecimal(wsEntrega.Cell(r, 3).GetString().Trim());
                var movementDate = ParseMovementDate(wsEntrega.Cell(r, 5));

                if (string.IsNullOrWhiteSpace(supName) || string.IsNullOrWhiteSpace(itemName) || quantity <= 0)
                    continue;

                var product = FindProductMatch(productsCache, itemName);
                if (product == null)
                    continue;

                suppliersDict.TryGetValue(supName, out var supplier);

                var movement = new StockMovement
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    MovementType = MovementType.Entrada,
                    MovementReason = MovementReason.CompraFornecedor,
                    Quantity = quantity,
                    MovementDate = movementDate,
                    Notes = $"Histórico importado - Fornecedor: {supName}"
                };
                _context.StockMovements.Add(movement);
                movementsCount++;
            }
            await _context.SaveChangesAsync();
        }

        // 4. IMPORTAR DOAÇÕES (rastreabilidade — saldo já está no ESTOQUE)
        if (TryGetWorksheetCaseInsensitive(workbook, "DOAÇÃO", out var wsDoacao)
            || TryGetWorksheetCaseInsensitive(workbook, "DOACAO", out wsDoacao))
        {
            var doacaoLastRow = wsDoacao.LastRowUsed()?.RowNumber() ?? 0;
            var doacaoHeaderRow = wsDoacao.Cell(1, 1).GetString().Trim()
                .Equals("ALIMENTO", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            for (int r = doacaoHeaderRow + 1; r <= doacaoLastRow; r++)
            {
                var name = wsDoacao.Cell(r, 1).GetString().Trim();
                var quantity = ParseDecimal(wsDoacao.Cell(r, 2).GetString().Trim());
                var unitStr = wsDoacao.Cell(r, 3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(name) || quantity <= 0)
                    continue;

                var product = FindProductMatch(productsCache, name);
                if (product == null)
                    continue;

                var donation = new Donation
                {
                    TenantId = tenantId,
                    DonorName = "Doação (planilha original)",
                    DonationDate = DateTime.UtcNow,
                    Notes = "Importado da aba DOAÇÃO da planilha Excel"
                };
                _context.Donations.Add(donation);

                _context.DonationItems.Add(new DonationItem
                {
                    TenantId = tenantId,
                    Donation = donation,
                    ProductId = product.Id,
                    Quantity = quantity,
                    Brand = product.Brand,
                    Notes = string.IsNullOrWhiteSpace(unitStr) ? null : $"Unidade: {unitStr}"
                });

                _context.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    MovementType = MovementType.Entrada,
                    MovementReason = MovementReason.DoacaoRecebida,
                    Quantity = quantity,
                    MovementDate = DateTime.UtcNow,
                    Notes = $"Doação importada da planilha: {name}"
                });
                movementsCount++;
            }
            await _context.SaveChangesAsync();
        }

        // 5. IMPORTAR SAÍDAS DIÁRIAS (histórico — saldo atual já está no ESTOQUE)
        if (TryGetWorksheetCaseInsensitive(workbook, "SAÍDA", out var wsSaida)
            || TryGetWorksheetCaseInsensitive(workbook, "SAIDA", out wsSaida))
        {
            movementsCount += ImportDailyMatrixMovements(
                wsSaida,
                productsByRowId,
                tenantId,
                MovementType.Saida,
                MovementReason.ConsumoCozinha,
                "Saída importada da planilha");
            await _context.SaveChangesAsync();
        }

        return (productsCount, suppliersCount, movementsCount);
    }

    public async Task<int> ImportStockOutflowsAsync(string filePath, Guid tenantId)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo Excel não encontrado.", filePath);

        using var workbook = new XLWorkbook(filePath);

        if (!TryGetWorksheetCaseInsensitive(workbook, "ESTOQUE", out var wsEstoque))
            throw new InvalidOperationException("Aba 'ESTOQUE' não encontrada na planilha.");

        if (!TryGetWorksheetCaseInsensitive(workbook, "SAÍDA", out var wsSaida)
            && !TryGetWorksheetCaseInsensitive(workbook, "SAIDA", out wsSaida))
        {
            throw new InvalidOperationException("Aba 'SAÍDA' não encontrada na planilha.");
        }

        var productsByRowId = await LoadProductsBySpreadsheetRowIdAsync(wsEstoque, tenantId);
        var movementsCount = ImportDailyMatrixMovements(
            wsSaida,
            productsByRowId,
            tenantId,
            MovementType.Saida,
            MovementReason.ConsumoCozinha,
            "Saída importada da planilha");

        await _context.SaveChangesAsync();
        return movementsCount;
    }

    private async Task<Dictionary<string, Product>> LoadProductsCacheAsync(Guid tenantId)
    {
        var products = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();

        return products.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<int, Product>> LoadProductsBySpreadsheetRowIdAsync(
        IXLWorksheet worksheet,
        Guid tenantId)
    {
        var headerRow = FindHeaderRow(worksheet, "ALIMENTO")
            ?? throw new InvalidOperationException("Não foi possível localizar o cabeçalho 'ALIMENTO' na aba ESTOQUE.");

        var productsCache = await LoadProductsCacheAsync(tenantId);
        var productsByRowId = new Dictionary<int, Product>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var spreadsheetRowId = 0;

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var name = worksheet.Cell(r, 1).GetString().Trim();
            if (IsFoodSectionBreak(name))
                break;

            if (string.IsNullOrWhiteSpace(name))
                continue;

            spreadsheetRowId++;
            var product = FindProductMatch(productsCache, name);
            if (product != null)
                productsByRowId[spreadsheetRowId] = product;
        }

        return productsByRowId;
    }

    private int ImportDailyMatrixMovements(
        IXLWorksheet worksheet,
        Dictionary<int, Product> productsByRowId,
        Guid tenantId,
        MovementType movementType,
        MovementReason movementReason,
        string notesPrefix)
    {
        var count = 0;
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int headerRow = 1; headerRow <= lastRow; headerRow++)
        {
            if (!worksheet.Cell(headerRow, 1).GetString().Trim()
                    .Equals("ID", StringComparison.OrdinalIgnoreCase))
                continue;

            var firstDayCol = FindFirstDayColumn(worksheet, headerRow);
            if (firstDayCol < 0)
                continue;

            var (month, year) = ParseMonthYearFromSheet(worksheet, headerRow);

            var dataRow = headerRow + 1;
            while (dataRow <= lastRow && string.IsNullOrWhiteSpace(worksheet.Cell(dataRow, 1).GetString().Trim()))
                dataRow++;

            for (; dataRow <= lastRow; dataRow++)
            {
                var idText = worksheet.Cell(dataRow, 1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(idText))
                    break;

                if (idText.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    break;

                if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowId))
                    continue;

                if (!productsByRowId.TryGetValue(rowId, out var product))
                    continue;

                for (int day = 1; day <= 31; day++)
                {
                    var col = firstDayCol + day - 1;
                    var quantity = ParseDecimal(worksheet.Cell(dataRow, col).GetString().Trim());
                    if (quantity <= 0)
                        continue;

                    if (day > DateTime.DaysInMonth(year, month))
                        continue;

                    var movementDate = ToUtc(new DateTime(year, month, day));

                    _context.StockMovements.Add(new StockMovement
                    {
                        TenantId = tenantId,
                        ProductId = product.Id,
                        MovementType = movementType,
                        MovementReason = movementReason,
                        Quantity = quantity,
                        MovementDate = movementDate,
                        Notes = $"{notesPrefix} - {GetPortugueseMonthName(month)}/{year} dia {day}"
                    });
                    count++;
                }
            }
        }

        return count;
    }

    private static int FindFirstDayColumn(IXLWorksheet worksheet, int headerRow)
    {
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 40;
        for (int col = 2; col <= lastCol; col++)
        {
            if (worksheet.Cell(headerRow, col).GetString().Trim() == "1")
                return col;
        }
        return -1;
    }

    private static (int month, int year) ParseMonthYearFromSheet(IXLWorksheet worksheet, int headerRow)
    {
        for (int r = Math.Max(1, headerRow - 5); r < headerRow; r++)
        {
            var text = worksheet.Cell(r, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var titleMatch = Regex.Match(text,
                @"SA[ÍI]DA DE ESTOQUE\s+(\w+)\s+(\d{4})",
                RegexOptions.IgnoreCase);
            if (titleMatch.Success
                && TryParsePortugueseMonth(titleMatch.Groups[1].Value, out var titleMonth)
                && int.TryParse(titleMatch.Groups[2].Value, out var titleYear))
            {
                return (titleMonth, titleYear);
            }

            var entradaMatch = Regex.Match(text,
                @"ENTRADA DE ESTOQUE\s+(\w+)\s+(\d{4})",
                RegexOptions.IgnoreCase);
            if (entradaMatch.Success
                && TryParsePortugueseMonth(entradaMatch.Groups[1].Value, out var entradaMonth)
                && int.TryParse(entradaMatch.Groups[2].Value, out var entradaYear))
            {
                return (entradaMonth, entradaYear);
            }

            if (TryParsePortugueseMonth(text, out var monthOnly))
            {
                for (int scanRow = r; scanRow <= headerRow; scanRow++)
                {
                    var scanLastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 10;
                    for (int c = 1; c <= scanLastCol; c++)
                    {
                        var cellText = worksheet.Cell(scanRow, c).GetString();
                        var yearMatch = Regex.Match(cellText, @"\b(20\d{2})\b");
                        if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var year))
                            return (monthOnly, year);
                    }
                }
            }
        }

        return (1, DateTime.UtcNow.Year);
    }

    private static bool TryParsePortugueseMonth(string text, out int month)
    {
        month = text.Trim().ToUpperInvariant() switch
        {
            "JANEIRO" => 1,
            "FEVEREIRO" => 2,
            "MARÇO" or "MARCO" => 3,
            "ABRIL" => 4,
            "MAIO" => 5,
            "JUNHO" => 6,
            "JULHO" => 7,
            "AGOSTO" => 8,
            "SETEMBRO" => 9,
            "OUTUBRO" => 10,
            "NOVEMBRO" => 11,
            "DEZEMBRO" => 12,
            _ => 0
        };
        return month > 0;
    }

    private static string GetPortugueseMonthName(int month) => month switch
    {
        1 => "Janeiro",
        2 => "Fevereiro",
        3 => "Março",
        4 => "Abril",
        5 => "Maio",
        6 => "Junho",
        7 => "Julho",
        8 => "Agosto",
        9 => "Setembro",
        10 => "Outubro",
        11 => "Novembro",
        12 => "Dezembro",
        _ => month.ToString(CultureInfo.InvariantCulture)
    };

    private async Task<(Product product, bool created)> GetOrCreateProductAsync(
        Guid tenantId,
        Dictionary<string, Product> cache,
        string name,
        string? brand,
        UnitOfMeasure unit,
        decimal minStock,
        decimal currentStock,
        string? notes)
    {
        if (cache.TryGetValue(name, out var product))
        {
            product.CurrentStock = currentStock;
            product.MinimumStock = minStock;
            if (!string.IsNullOrEmpty(brand)) product.Brand = brand;
            if (!string.IsNullOrEmpty(notes)) product.Notes = notes;
            return (product, false);
        }

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
        cache[name] = product;
        return (product, true);
    }

    private void AddInitialStockMovement(
        Guid tenantId,
        Product product,
        decimal quantity,
        DateTime? expiryDate,
        string? brand,
        string batchNumber,
        string batchNotes,
        string movementNotes)
    {
        var batch = new Batch
        {
            TenantId = tenantId,
            Product = product,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            InitialQuantity = quantity,
            CurrentQuantity = quantity,
            ReceivedDate = DateTime.UtcNow,
            Brand = string.IsNullOrEmpty(brand) ? null : brand,
            Notes = batchNotes
        };
        _context.Batches.Add(batch);

        _context.StockMovements.Add(new StockMovement
        {
            TenantId = tenantId,
            Product = product,
            Batch = batch,
            MovementType = MovementType.Entrada,
            MovementReason = MovementReason.AjusteInventario,
            Quantity = quantity,
            MovementDate = DateTime.UtcNow,
            Notes = movementNotes
        });
    }

    private static Product? FindProductMatch(Dictionary<string, Product> cache, string searchName)
    {
        if (cache.TryGetValue(searchName, out var exact))
            return exact;

        var normalized = NormalizeName(searchName);

        // Match exato normalizado
        foreach (var (name, product) in cache)
        {
            if (NormalizeName(name) == normalized)
                return product;
        }

        // Match parcial: nome da planilha contido no produto ou vice-versa
        Product? bestMatch = null;
        var bestScore = 0;

        foreach (var (name, product) in cache)
        {
            var normalizedProduct = NormalizeName(name);
            if (normalizedProduct.Contains(normalized) || normalized.Contains(normalizedProduct))
            {
                var score = Math.Min(normalized.Length, normalizedProduct.Length);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = product;
                }
            }
        }

        return bestMatch;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.ToUpperInvariant().Trim();
        normalized = normalized.Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
            .Replace("Ó", "O").Replace("Ú", "U").Replace("Ç", "C").Replace("Ã", "A");
        normalized = Regex.Replace(normalized, @"[^A-Z0-9\s]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    private static bool IsFoodSectionBreak(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var upper = value.Trim().ToUpperInvariant();
        return upper.Contains("PROTEINAS") || upper.Contains("TOTAL DE ITENS");
    }

    private static int? FindHeaderRow(IXLWorksheet worksheet, string headerText)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int r = 1; r <= lastRow; r++)
        {
            var cellValue = worksheet.Cell(r, 1).GetString().Trim();
            if (cellValue.Equals(headerText, StringComparison.OrdinalIgnoreCase))
                return r;
        }
        return null;
    }

    private static int? FindHeaderRowAfter(IXLWorksheet worksheet, string headerText, int afterRow)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int r = afterRow + 1; r <= lastRow; r++)
        {
            var cellValue = worksheet.Cell(r, 1).GetString().Trim();
            if (cellValue.Equals(headerText, StringComparison.OrdinalIgnoreCase))
                return r;
        }
        return null;
    }

    private static bool TryGetWorksheetCaseInsensitive(XLWorkbook workbook, string worksheetName, out IXLWorksheet worksheet)
    {
        if (workbook.TryGetWorksheet(worksheetName, out worksheet))
            return true;

        foreach (var ws in workbook.Worksheets)
        {
            if (string.Equals(ws.Name?.Trim(), worksheetName, StringComparison.OrdinalIgnoreCase))
            {
                worksheet = ws;
                return true;
            }
        }

        worksheet = null!;
        return false;
    }

    private static decimal ParseDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0m;

        raw = raw.Trim();

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        if (decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("pt-BR"), out result))
            return result;

        return 0m;
    }

    private static DateTime ToUtc(DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }

    private static DateTime? ParseExpiryDate(IXLCell cell)
    {
        try
        {
            if (cell.DataType == XLDataType.DateTime)
                return ToUtc(cell.GetDateTime());

            var valStr = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(valStr))
                return null;

            // Formato MM/yyyy (ex: 5/2027, 12/2026)
            var monthYearMatch = Regex.Match(valStr, @"^(\d{1,2})\s*/\s*(\d{4})$");
            if (monthYearMatch.Success
                && int.TryParse(monthYearMatch.Groups[1].Value, out var month)
                && int.TryParse(monthYearMatch.Groups[2].Value, out var year)
                && month is >= 1 and <= 12)
            {
                var lastDay = DateTime.DaysInMonth(year, month);
                return ToUtc(new DateTime(year, month, lastDay));
            }

            // Formato dd/MM/yyyy
            if (DateTime.TryParse(valStr, new CultureInfo("pt-BR"), DateTimeStyles.None, out var parsedDate))
                return ToUtc(parsedDate);

            if (DateTime.TryParse(valStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                return ToUtc(parsedDate);

            // Apenas ano (ex: 2027, 2028)
            if (int.TryParse(valStr, out var yearOnly) && yearOnly >= 2024 && yearOnly <= 2035)
                return ToUtc(new DateTime(yearOnly, 12, 31));

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime ParseMovementDate(IXLCell cell)
    {
        try
        {
            if (cell.DataType == XLDataType.DateTime)
                return ToUtc(cell.GetDateTime());

            var valStr = cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(valStr))
            {
                if (DateTime.TryParse(valStr, new CultureInfo("pt-BR"), DateTimeStyles.None, out var parsed))
                    return ToUtc(parsed);

                if (DateTime.TryParse(valStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    return ToUtc(parsed);
            }
        }
        catch { /* fallback below */ }

        return DateTime.UtcNow;
    }

    private static UnitOfMeasure ResolveUnit(string productName)
    {
        var upperName = productName.ToUpperInvariant();
        if (upperName.Contains("KG") || upperName.Contains("QUILO"))
            return UnitOfMeasure.KG;
        if (upperName.Contains("ML") || upperName.Contains("LITRO") || upperName.Contains("1L") || upperName.Contains("2L") || upperName.Contains("900 ML"))
            return UnitOfMeasure.L;
        if (upperName.Contains("PACOTE"))
            return UnitOfMeasure.PACOTE;
        if (upperName.Contains("LATA"))
            return UnitOfMeasure.LATA;
        if (upperName.Contains("CAIXA"))
            return UnitOfMeasure.CX;

        return UnitOfMeasure.UND;
    }
}
