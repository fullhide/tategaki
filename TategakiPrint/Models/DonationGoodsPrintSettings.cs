namespace TategakiPrint.Models
{
    public class DonationGoodsPrintSettings
    {
        public string SelectedFont { get; set; } = "\"HG行書体\", \"HGP行書体\", \"Kaiti SC\", \"STKaiti\", \"Yu Mincho\", serif";
        public double MaxPageWidthUnits { get; set; } = 25.0;
        public double NameFontSize { get; set; } = 2.0;
        public double AmountFontSize { get; set; } = 2.0;
        public double SingleLineWidth { get; set; } = 2.0;
        public double MultiLineWidth { get; set; } = 2.2;
        public double LineGap { get; set; } = 0.0;
        public double GoodsItemGap { get; set; } = 0.5; // 品物の行間
        public double SpacerWidth { get; set; } = 1.0;
        public double NameTopOffset { get; set; } = 14.0;
        public double AmountTopOffset { get; set; } = -170.0;
        public string LastSelectedSheetName { get; set; } = string.Empty;
        /// <summary>
        /// 各ページのエントリ数を表す一覧
        /// </summary>
        public List<int> PageEntryCounts { get; set; } = new();
    }
}