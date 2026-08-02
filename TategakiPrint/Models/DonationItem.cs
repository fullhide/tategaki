using MiniExcelLibs.Attributes;

namespace TategakiPrint.Models;

public class DonationItem
{
    /// <summary>
    /// UI（Blazor）上での一元識別用ID（Excelのマッピング対象外）
    /// </summary>
    [ExcelIgnore]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ExcelColumn(Name = "名前")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn(Index = 2)]
    public string Kana { get; set; } = string.Empty;

    [ExcelColumn(Name = "合計金額")]
    public decimal Amount { get; set; }

    [ExcelColumn(Index = 14)]
    public string SortKey1 { get; set; } = string.Empty;

    [ExcelColumn(Index = 15)]
    public string SortKey2 { get; set; } = string.Empty;

    [ExcelColumn(Index = 16)]
    public string SortKey3 { get; set; } = string.Empty;
}