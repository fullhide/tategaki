using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using MiniExcelLibs;
using TategakiPrint.Models;

namespace TategakiPrint.Services
{
    public class DonationMoneyState
    {
        private const string LocalStorageKey = "TategakiPrint_DonationMoney_Settings";
        public const string SortByAmount = "amount";
        public const string SortByKana = "kana";
        public const string SortByCustomKey = "customKey";
        public const string SortByName = "name";
        private static readonly List<string> DefaultSortOrder = new() { SortByAmount, SortByKana, SortByCustomKey, SortByName };
        private static readonly HashSet<string> AllowedSortKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            SortByAmount,
            SortByKana,
            SortByCustomKey,
            SortByName
        };

        // 既存のプロパティ
        public List<DonationMoneyItem> Items { get; set; } = new();

        // 画面切り替え状態（初期表示を Print に設定）
        public ViewMode CurrentView { get; private set; } = ViewMode.Print;

        // --- ファイル共有用状態 ---
        public byte[]? FileBytes { get; private set; }
        public List<string> SheetNames { get; private set; } = new();
        public string SelectedSheetName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public bool IsLoading { get; set; }
        public string LastLoadedWorkbookName { get; private set; } = string.Empty;
        public string LastLoadedSheetName { get; private set; } = string.Empty;
        public DateTime? LastLoadedAt { get; private set; }
        public IReadOnlyList<string> SortOrder => _sortOrder;
        private string CurrentWorkbookName { get; set; } = string.Empty;
        private List<string> _sortOrder = new(DefaultSortOrder);

        public event Action? OnChange;

        // 画面切り替えメソッド
        public void SetView(ViewMode view)
        {
            CurrentView = view;
            NotifyStateChanged();
        }

        public void ShowPrint()
        {
            SortItems();
            SetView(ViewMode.Print);
        }
        public void ShowEdit() => SetView(ViewMode.Edit);

        public void SortItems()
        {
            IOrderedEnumerable<DonationMoneyItem>? ordered = null;

            foreach (var sortKey in _sortOrder)
            {
                switch (sortKey)
                {
                    case SortByAmount:
                        ordered = ordered == null
                            ? Items.OrderByDescending(x => x.Amount)
                            : ordered.ThenByDescending(x => x.Amount);
                        break;
                    case SortByKana:
                        ordered = ordered == null
                            ? Items.OrderBy(x => x.Kana)
                            : ordered.ThenBy(x => x.Kana);
                        break;
                    case SortByCustomKey:
                        ordered = ordered == null
                            ? Items.OrderBy(x => x.SortKey1)
                            : ordered.ThenBy(x => x.SortKey1);
                        break;
                    case SortByName:
                        ordered = ordered == null
                            ? Items.OrderBy(x => x.Name)
                            : ordered.ThenBy(x => x.Name);
                        break;
                }
            }

            Items = (ordered ?? Items.OrderByDescending(x => x.Amount)).ToList();
            NotifyStateChanged();
        }

        public async Task LoadSortOrderAsync(IJSRuntime js)
        {
            try
            {
                var json = await js.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var loaded = JsonSerializer.Deserialize<DonationMoneyPrintSettings>(json);
                    _sortOrder = NormalizeSortOrder(loaded?.SortOrder);
                    return;
                }
            }
            catch { }

            _sortOrder = NormalizeSortOrder(null);
        }

        public async Task SetSortOrderAsync(IJSRuntime js, IEnumerable<string>? sortOrder, bool applySort = true)
        {
            _sortOrder = NormalizeSortOrder(sortOrder);

            try
            {
                var json = await js.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
                DonationMoneyPrintSettings settings = !string.IsNullOrWhiteSpace(json)
                    ? JsonSerializer.Deserialize<DonationMoneyPrintSettings>(json) ?? new DonationMoneyPrintSettings()
                    : new DonationMoneyPrintSettings();

                settings.SortOrder = _sortOrder.ToList();
                var updatedJson = JsonSerializer.Serialize(settings);
                await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, updatedJson);
            }
            catch { }

            if (applySort)
            {
                SortItems();
            }
        }

        // --- Excelファイルセット・解析ロジック ---
        public async Task SetFileAsync(Stream stream, IJSRuntime js, string workbookName = "")
        {
            IsLoading = true;
            ErrorMessage = null;
            SheetNames.Clear();
            FileBytes = null;
            CurrentWorkbookName = workbookName;
            NotifyStateChanged();

            try
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                FileBytes = ms.ToArray();

                using var readStream = new MemoryStream(FileBytes);
                SheetNames = MiniExcel.GetSheetNames(readStream).ToList();

                if (SheetNames.Any())
                {
                    // 前回選択のシート名をlocalStorageから復元を試みる
                    string? lastSheet = await GetLastSelectedSheetNameAsync(js);
                    if (!string.IsNullOrEmpty(lastSheet) && SheetNames.Contains(lastSheet))
                    {
                        SelectedSheetName = lastSheet;
                    }
                    else
                    {
                        SelectedSheetName = SheetNames.First();
                        await SaveLastSelectedSheetNameAsync(js, SelectedSheetName);
                    }
                }
                else
                {
                    ErrorMessage = "ファイル内にシートが見つかりませんでした。";
                }
            }
            catch (Exception ex)
            {
                FileBytes = null;
                ErrorMessage = $"ファイル読み込みエラー:\n{ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task ChangeSheetNameAsync(string sheetName, IJSRuntime js)
        {
            SelectedSheetName = sheetName;
            await SaveLastSelectedSheetNameAsync(js, sheetName);
            NotifyStateChanged();
        }

        public async Task LoadSelectedSheetAsync(IJSRuntime js)
        {
            if (FileBytes == null || string.IsNullOrEmpty(SelectedSheetName)) return;

            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            try
            {
                using var stream = new MemoryStream(FileBytes);
                var rows = stream.Query(sheetName: SelectedSheetName, useHeaderRow: true, startCell: "A3")
                                 .Cast<IDictionary<string, object>>()
                                 .ToList();

                var validItems = new List<DonationMoneyItem>();

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row == null) continue;

                    string name = GetVal(row, 1, "名前", "氏名", "芳名", "Name", "B");
                    string kana = GetVal(row, 2, "かな", "ふりがな", "フリガナ", "Kana", "C");
                    string amountStr = GetVal(row, 9, "合計金額", "金額", "寄付額", "Amount", "J");

                    string k1 = GetVal(row, 13, "*", "N");

                    string cleanedAmount = amountStr.Replace(",", "").Replace("￥", "").Replace("円", "").Trim();

                    if (decimal.TryParse(cleanedAmount, out decimal amount) && amount > 0 && !string.IsNullOrWhiteSpace(name))
                    {
                        validItems.Add(new DonationMoneyItem
                        {
                            Name = name.Trim(),
                            Kana = kana.Trim(),
                            Amount = amount,
                            SortKey1 = k1.Trim()
                        });
                    }
                }

                if (validItems.Any())
                {
                    Items = validItems;
                    SortItems();
                    await SaveLastSelectedSheetNameAsync(js, SelectedSheetName);
                    LastLoadedWorkbookName = CurrentWorkbookName;
                    LastLoadedSheetName = SelectedSheetName;
                    LastLoadedAt = DateTime.Now;
                }
                else
                {
                    ErrorMessage = $"シート「{SelectedSheetName}」から有効なデータを取り込めませんでした。";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"データ抽出エラー:\n{ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private string GetVal(IDictionary<string, object> row, int index0Based, params string[] keys)
        {
            foreach (var k in keys)
            {
                var match = row.FirstOrDefault(x => x.Key != null && x.Key.Trim().Equals(k, StringComparison.OrdinalIgnoreCase));
                if (match.Key != null && match.Value != null)
                {
                    return match.Value.ToString() ?? "";
                }
            }

            if (row.Count > index0Based)
            {
                var val = row.ElementAtOrDefault(index0Based).Value;
                return val?.ToString() ?? "";
            }

            return "";
        }

        private async Task<string?> GetLastSelectedSheetNameAsync(IJSRuntime js)
        {
            try
            {
                var json = await js.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var loaded = JsonSerializer.Deserialize<DonationMoneyPrintSettings>(json);
                    return loaded?.LastSelectedSheetName;
                }
            }
            catch { }
            return null;
        }

        private async Task SaveLastSelectedSheetNameAsync(IJSRuntime js, string sheetName)
        {
            try
            {
                var json = await js.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
                DonationMoneyPrintSettings settings = !string.IsNullOrWhiteSpace(json) 
                    ? JsonSerializer.Deserialize<DonationMoneyPrintSettings>(json) ?? new DonationMoneyPrintSettings() 
                    : new DonationMoneyPrintSettings();

                settings.LastSelectedSheetName = sheetName;
                settings.SortOrder = _sortOrder.ToList();
                var updatedJson = JsonSerializer.Serialize(settings);
                await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, updatedJson);
            }
            catch { }
        }

        private static List<string> NormalizeSortOrder(IEnumerable<string>? source)
        {
            var normalized = new List<string>();

            if (source != null)
            {
                foreach (var key in source)
                {
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!AllowedSortKeys.Contains(key)) continue;
                    if (normalized.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;

                    normalized.Add(key);
                }
            }

            foreach (var defaultKey in DefaultSortOrder)
            {
                if (!normalized.Contains(defaultKey, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(defaultKey);
                }
            }

            return normalized;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}