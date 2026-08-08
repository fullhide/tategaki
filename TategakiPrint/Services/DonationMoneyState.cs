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

        public event Action? OnChange;

        // 画面切り替えメソッド
        public void SetView(ViewMode view)
        {
            CurrentView = view;
            NotifyStateChanged();
        }

        public void ShowPrint() => SetView(ViewMode.Print);
        public void ShowEdit() => SetView(ViewMode.Edit);

        public void SortItems()
        {
            // 既存のソート処理
            Items = Items
                .OrderByDescending(x => x.Amount)
                .ThenBy(x => x.Kana)
                .ThenBy(x => x.SortKey1)
                .ThenBy(x => x.SortKey2)
                .ThenBy(x => x.SortKey3)
                .ToList();
            NotifyStateChanged();
        }

        // --- Excelファイルセット・解析ロジック ---
        public async Task SetFileAsync(Stream stream, IJSRuntime js)
        {
            IsLoading = true;
            ErrorMessage = null;
            SheetNames.Clear();
            FileBytes = null;
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
                    string k2 = GetVal(row, 15, "**", "P");
                    string k3 = GetVal(row, 16, "***", "Q");

                    string cleanedAmount = amountStr.Replace(",", "").Replace("￥", "").Replace("円", "").Trim();

                    if (decimal.TryParse(cleanedAmount, out decimal amount) && amount > 0 && !string.IsNullOrWhiteSpace(name))
                    {
                        validItems.Add(new DonationMoneyItem
                        {
                            Name = name.Trim(),
                            Kana = kana.Trim(),
                            Amount = amount,
                            SortKey1 = k1.Trim(),
                            SortKey2 = k2.Trim(),
                            SortKey3 = k3.Trim()
                        });
                    }
                }

                if (validItems.Any())
                {
                    Items = validItems;
                    SortItems();
                    await SaveLastSelectedSheetNameAsync(js, SelectedSheetName);
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
                var updatedJson = JsonSerializer.Serialize(settings);
                await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, updatedJson);
            }
            catch { }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}