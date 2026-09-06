using ClosedXML.Excel;

var path = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "planilha_inspect.xlsx");
using var wb = new XLWorkbook(path);

foreach (var name in new[] { "SAÍDA", "SAIDA", "ENTRADA" })
{
    if (!wb.TryGetWorksheet(name, out var ws)) continue;
    Console.WriteLine($"\n=== {ws.Name} ===");
    for (int r = 1; r <= Math.Min(25, ws.LastRowUsed()?.RowNumber() ?? 0); r++)
    {
        var cells = new List<string>();
        for (int c = 1; c <= Math.Min(35, ws.LastColumnUsed()?.ColumnNumber() ?? 0); c++)
        {
            if (c == 1 || c == 2 || c == 3 || c == 4 || c == 5 || c == 6 || c == 7 || c == 24 || c == 25 || c == 26 || c == 33 || c == 34)
                cells.Add($"C{c}={ws.Cell(r,c).GetFormattedString().Trim()}");
        }
        if (cells.Any(x => !x.EndsWith("=")))
            Console.WriteLine($"R{r}: {string.Join(" | ", cells)}");
    }
    Console.WriteLine($"Last row: {ws.LastRowUsed()?.RowNumber()}, Last col: {ws.LastColumnUsed()?.ColumnNumber()}");
}

// ESTOQUE - check if there's row ID mapping
if (wb.TryGetWorksheet("ESTOQUE", out var estoque))
{
    Console.WriteLine("\n=== ESTOQUE first col check for IDs ===");
    var headerRow = 7;
    for (int r = headerRow; r <= Math.Min(20, estoque.LastRowUsed()?.RowNumber() ?? 0); r++)
        Console.WriteLine($"R{r}: A={estoque.Cell(r,1).GetString()[..Math.Min(40, estoque.Cell(r,1).GetString().Length)]}");
}
