namespace TategakiPrint.Models;

public class DonationMoneyPrintSettings
{
    public double MaxPageWidthUnits { get; set; } = 25.0;
    public string SelectedFont { get; set; } = "\"Yu Mincho\", \"HG行書体\", \"HGP行書体\", \"Kaiti SC\", \"STKaiti\", serif";
    public double NameFontSize { get; set; } = 2.0;
    public double AmountFontSize { get; set; } = 2.0;
    public double SingleLineWidth { get; set; } = 0.9;
    public double MultiLineWidth { get; set; } = 1.8;
    public double LineGap { get; set; } = 0.0;
    public double NameTopOffset { get; set; } = 0;
    public double KinTopOffset { get; set; } = 0;
    public double AmountTopOffset { get; set; } = 0;

    /// <summary>
    /// 金額の境目に空行を挿入するかどうか
    /// </summary>
    public bool InsertGroupSpacing { get; set; } = true;

    /// <summary>
    /// 空行の幅倍率（1.0 = 標準の短冊幅）
    /// </summary>
    public double GroupSpacingWidth { get; set; } = 0.5;

    // 追加: 空行（スペーサー）の幅倍率（初期値 1.0）
    public double SpacerWidth { get; set; } = 1.0;

    public string LastSelectedSheetName{get;set;} = string.Empty;

    /// <summary>
    /// 各ページのエントリ数を表す一覧
    /// </summary>
    public List<int> PageEntryCounts { get; set; } = new();
}