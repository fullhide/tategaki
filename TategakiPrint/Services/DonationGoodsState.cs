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
    public class DonationGoodsState
    {
        private const string LocalStorageKey = "TategakiPrint_DonationGoods_Settings";

        public List<DonationGoodsItem> Items { get; set; } = new();

        // ビュー状態（初期値は印刷表示）
        public ViewMode CurrentView { get; private set; } = ViewMode.Print;

        // ファイル・シート状態（寄付品専用で独立保持）
        public byte[]? FileBytes { get; private set; }
        public List<string> SheetNames { get; private set; } = new();
        public string SelectedSheetName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public bool IsLoading { get; set; }
        public string LastLoadedWorkbookName { get; private set; } = string.Empty;
        public string LastLoadedSheetName { get; private set; } = string.Empty;
        public DateTime? LastLoadedAt { get; private set; }
        private string CurrentWorkbookName { get; set; } = string.Empty;

        public event Action? OnChange;

        // --- ビュー切り替え処理 ---
        public void SetView(ViewMode view)
        {
            CurrentView = view;
            NotifyStateChanged();
        }

        public void ShowPrint() => SetView(ViewMode.Print);
        public void ShowEdit() => SetView(ViewMode.Edit);

        // --- データソート ---
        public void SortItems()
        {
            Items = Items
                .OrderBy(x => x.SortKey)
                .ThenBy(x => x.Name)
                .ToList();
            NotifyStateChanged();
        }

        // --- Excel読み込み処理 ---
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

                if (rows == null || !rows.Any())
                {
                    ErrorMessage = $"シート「{SelectedSheetName}」のA3セル以降にデータが見つかりませんでした。\nファイルの内容およびフォーマットをご確認ください。";
                    return;
                }

                // --- 厳密なヘッダー検証（インデックスフォールバック廃止に伴うチェック） ---
                var firstRowKeys = rows.First().Keys.Where(k => k != null).Select(k => k.Trim()).ToList();

                bool hasName = firstRowKeys.Any(k => k.Equals("名前", StringComparison.OrdinalIgnoreCase) || k.Equals("氏名", StringComparison.OrdinalIgnoreCase) || k.Equals("芳名", StringComparison.OrdinalIgnoreCase) || k.Equals("Name", StringComparison.OrdinalIgnoreCase));
                bool hasItem = firstRowKeys.Any(k => k.Equals("品物", StringComparison.OrdinalIgnoreCase) || k.Equals("品名", StringComparison.OrdinalIgnoreCase) || k.Equals("物品", StringComparison.OrdinalIgnoreCase) || k.Equals("Item", StringComparison.OrdinalIgnoreCase));

                if (!hasName || !hasItem)
                {
                    ErrorMessage = $"エラー: 選択されたシート「{SelectedSheetName}」のフォーマットが正しくありません。\n（「名前」または「品物」の列が見つかりません。正しいシートを選択してください）";
                    return;
                }

                var validGoods = new List<DonationGoodsItem>();
                int skippedRowsCount = 0;

                foreach (var row in rows)
                {
                    if (row == null) continue;

                    // キー名の一致のみで値を取得（インデックスフォールバックなし）
                    string name = GetValStrict(row, "名前", "氏名", "芳名", "Name");
                    string itemName = GetValStrict(row, "品物", "品名", "物品", "Item");
                    string qtyStr = GetValStrict(row, "数量", "数", "Quantity");
                    string unit = GetValStrict(row, "単位", "Unit");
                    string sortKeyStr = GetValStrict(row, "*", "SortKey");

                    int.TryParse(qtyStr, out int qty);
                    int.TryParse(sortKeyStr, out int sortKey);

                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(itemName))
                    {
                        skippedRowsCount++;
                        continue;
                    }

                    validGoods.Add(new DonationGoodsItem
                    {
                        Name = name.Trim(),
                        ItemName = itemName.Trim(),
                        Quantity = qty,
                        Unit = unit.Trim(),
                        SortKey = sortKey
                    });
                }

                if (validGoods.Any())
                {
                    Items = validGoods;
                    SortItems();
                    await SaveLastSelectedSheetNameAsync(js, SelectedSheetName);
                    LastLoadedWorkbookName = CurrentWorkbookName;
                    LastLoadedSheetName = SelectedSheetName;
                    LastLoadedAt = DateTime.Now;
                }
                else
                {
                    ErrorMessage = $"シート「{SelectedSheetName}」から有効な寄付品データを取り込めませんでした。\n（読み取り行数: {rows.Count}件 / スキップ行数: {skippedRowsCount}件）\nA3セルからの列配置が正しいかご確認ください。";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Excelデータの抽出中にエラーが発生しました。\n詳細:\n{ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        // --- キー名による厳密な値取得（位置に依存しない） ---
        private string GetValStrict(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var k in keys)
            {
                var match = row.FirstOrDefault(x => x.Key != null && x.Key.Trim().Equals(k, StringComparison.OrdinalIgnoreCase));
                if (match.Key != null && match.Value != null)
                {
                    return match.Value.ToString() ?? "";
                }
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
                    var loaded = JsonSerializer.Deserialize<DonationGoodsPrintSettings>(json);
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
                DonationGoodsPrintSettings settings = !string.IsNullOrWhiteSpace(json)
                    ? JsonSerializer.Deserialize<DonationGoodsPrintSettings>(json) ?? new DonationGoodsPrintSettings()
                    : new DonationGoodsPrintSettings();

                settings.LastSelectedSheetName = sheetName;
                var updatedJson = JsonSerializer.Serialize(settings);
                await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, updatedJson);
            }
            catch { }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // --- ファイル受信とシート名取得 (Stream引数版) ---
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
                await SetFileAsync(ms.ToArray(), js, notify: false);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"ファイル読み込みエラー:\n{ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        // --- バイト配列版の既存メソッド ---
        public async Task SetFileAsync(byte[] bytes, IJSRuntime js, bool notify = true)
        {
            FileBytes = bytes;
            SheetNames.Clear();
            SelectedSheetName = string.Empty;
            ErrorMessage = null;

            try
            {
                using var stream = new MemoryStream(FileBytes);
                SheetNames = stream.GetSheetNames().ToList();

                if (SheetNames.Any())
                {
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
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Excel解析エラー:\n{ex.Message}";
            }
            
            if (notify)
            {
                NotifyStateChanged();
            }
        }

        // --- シート名変更メソッド ---
        public async Task ChangeSheetNameAsync(string sheetName, IJSRuntime js)
        {
            SelectedSheetName = sheetName;
            await SaveLastSelectedSheetNameAsync(js, sheetName);
            NotifyStateChanged();
        } 
    }
}