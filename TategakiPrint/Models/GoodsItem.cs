namespace TategakiPrint.Models
{
    public class GoodsItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public string Unit { get; set; } = "";
        public int SortKey { get; set; } // J列 (*)
    }
}