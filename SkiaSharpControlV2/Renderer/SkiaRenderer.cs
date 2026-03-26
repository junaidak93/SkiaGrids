
using SkiaSharp;
using SkiaSharp.Views.WPF;
using SkiaSharpControlV2.Data.Enum;
using SkiaSharpControlV2.Helpers;
using SkiaSharpControlV2.Model;
using System.Collections;
using System.Collections.ObjectModel;
using System.Text;

using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace SkiaSharpControlV2.Renderer
{
    public class SkiaRenderer : IDisposable
    {
        public SkiaRenderer(SkiaGridViewV2 CurrentContext, ReflectionHelper reflectionHelper)
        {
            this.reflectionHelper = reflectionHelper;
            this.CurrentContext = CurrentContext;

        }
        private SkiaGridViewV2 CurrentContext;
        public ReflectionHelper reflectionHelper;
        private SKFont SymbolFont { get; set; } = new() { Size = 12, Typeface = SKTypeface.FromFamilyName("Arial") };
        private SKFont UniCodeFont { get; set; } = new() { Size = 12, Typeface = SKTypeface.FromFamilyName("Segoe UI Symbol") };
        private string FontFamily { get; set; } = "Arial";
        private string FontStyle { get; set; } = "Normal";
        private float FontSize { get; set; } = 12;
        private ICustomCollectionView? Items { get; set; }
        private IEnumerable? SelectedItems { get; set; }
        private IEnumerable<SKGridViewColumn>? Columns { get; set; } = [];
        private SKGroupDefinition? Group { get; set; }
        private ScrollBar? HorizontalScrollViewer { get; set; }
        private ScrollBar? VerticalScrollViewer { get; set; }
        private bool ShowGridLines { get; set; }

        private bool IsWindowActive = true;

        private readonly List<SKGridViewColumn> _visibleColumnsCache = new();
        private SKPaint SelectedRowBackgroundHighlighting = new SKPaint() { Color = SKColor.Parse("#0072C6"), IsAntialias = true };
        private SKPaint SelectedRowTextColor = new SKPaint { Color = SKColors.White, StrokeWidth = 1, IsAntialias = true };
        private SKPaint GridLineColor = new SKPaint { Color = SKColors.Black, StrokeWidth = 1, IsAntialias = true };
        private SKPaint FontColor = new SKPaint { Color = SKColors.Black, StrokeWidth = 1, IsAntialias = true };
        private SKPaint RowBackgroundColor = new SKPaint { Color = SKColors.White, StrokeWidth = 1, IsAntialias = true };
        private SKPaint GroupRowBackgroundColor = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1, IsAntialias = true };
        private SKPaint GroupFontColor = new SKPaint { Color = SKColors.White, StrokeWidth = 1, IsAntialias = true };
        private SKPaint AlternatingRowBackground = null;

        private SKPaint ButtonBackgroundcolor = new SKPaint { Color = SKColors.Transparent, StrokeWidth = 1, IsAntialias = true };
        private readonly SKPaint ButtonForegroundcolor = new SKPaint { Color = SKColors.Black, StrokeWidth = 1, IsAntialias = true };
        private readonly SKPaint ButtonBordercolor = new SKPaint { Color = SKColors.Transparent, StrokeWidth = 1, IsAntialias = true };


        private SKPaint CellBackgroundColor = new SKPaint { Color = SKColors.White, StrokeWidth = 1, IsAntialias = true };
        private SKPaint CellBorderColor = new SKPaint { Color = SKColors.Green, StrokeWidth = 1, IsAntialias = true };
        public List<GroupModel> GroupItemSource { get; set; } = new();
        public void UpdateItems(ICustomCollectionView items)
        {
            Items = items;
        }
        public void SetWindowActive(bool isWindowActive)
        {
            IsWindowActive = isWindowActive;
        }
        public void UpdateSelectedItems(IEnumerable selectedItems)
        {
            SelectedItems = selectedItems;
        }
        public void SetScrollBars(ScrollBar horizontalScrollViewer, ScrollBar verticalScrollViewer)
        {
            HorizontalScrollViewer = horizontalScrollViewer;
            VerticalScrollViewer = verticalScrollViewer;
        }
        public void SetColumns(IEnumerable<SKGridViewColumn> columns)
        {
            Columns = columns;
        }
        public void SetGroup(SKGroupDefinition? group)
        {
            Group = group;
        }
        public void SetFontSize(float size)
        {
            FontSize = size;
            UpdateFont();
        }

        public void SetFontFamily(string fontFamily)
        {
            FontFamily = fontFamily;
            UpdateFont();
        }
        public void SetFontStyle(string fontStyle)
        {
            FontStyle = fontStyle;
            UpdateFont();
        }
        private void UpdateFont()
        {
            if (Helper.IsFontInstalled(FontFamily))
            {
                SymbolFont = SkFontFactory.CreateSkFont(FontFamily, FontStyle, FontSize);
                UniCodeFont.Size = FontSize;
            }
            else
                throw new ArgumentException($" \"{FontFamily}\" font not installed");
        }

        public void SetGridLinesVisibility(bool showGridLines)
        {
            ShowGridLines = showGridLines;
        }
        public void SetGridLinesColor(string color)
        {
            if (!string.IsNullOrEmpty(color))
                GridLineColor.Color = SKColor.Parse(color);
        }
        public void SetGroupRowBackgroundColor(string? color)
        {
            if (color != null)
                GroupRowBackgroundColor.Color = SKColor.Parse(color);
        }
        public void SetGroupFontColor(string? color)
        {
            if (color != null)
                GroupFontColor.Color = SKColor.Parse(color);
        }
        public void SetForeground(string color)
        {
            if (!string.IsNullOrEmpty(color))
                FontColor.Color = SKColor.Parse(color);
        }
        public void SetRowBackgroundColor(string color)
        {
            if (!string.IsNullOrEmpty(color))
                RowBackgroundColor.Color = SKColor.Parse(color);
        }
        public void SetAlternatingRowBackground(string? color)
        {
            if (color == null)
                AlternatingRowBackground = null;
            else
                AlternatingRowBackground = new SKPaint { Color = SKColor.Parse(color), StrokeWidth = 1, IsAntialias = true };
        }
        public void Draw(SKCanvas canvas, float scrollOffsetX, float scrollOffsetY, float rowHeight, int totalRows)
        {
            try
            {


                int firstVisibleRow = Math.Max(0, (int)(scrollOffsetY / rowHeight));

                int firstVisibleCol = 0;
                int visibleRowCount = Math.Min((int?)(VerticalScrollViewer?.ViewportSize / rowHeight) ?? 0, totalRows - firstVisibleRow);
                int visibleColCount = 0;

                double columnSum = scrollOffsetX;
                int columnCounter = 0;

                var visibleColumns = _visibleColumnsCache;//(?.Where(x => x.Width > 0) ?? []).ToList();

                foreach (var item in visibleColumns)
                {
                    columnSum -= item.Width;
                    if (columnSum <= 0)
                    {
                        firstVisibleCol = columnCounter;
                        break;
                    }
                    columnCounter++;
                }

                columnSum = 0;

                for (int i = firstVisibleCol; i < visibleColumns.Count; i++)
                {
                    columnSum += visibleColumns[i].Width;
                    visibleColCount = i + 1;
                    if (columnSum >= HorizontalScrollViewer?.ViewportSize)
                        break;
                }
                if ((firstVisibleRow + visibleRowCount) > totalRows)
                    return;

                float currentY = firstVisibleRow * rowHeight;
                List<GroupModel>? GroupItems = null;

                IEnumerator<object> items = Items!.Cast<object>().Skip(firstVisibleRow).Take(visibleRowCount).GetEnumerator();
                items?.MoveNext();
                if (Group != null)
                {
                    GroupItems = GroupItemSource.Where(x => x.IsGroupHeader || x.IsExpanded).ToList();
                }

                for (int row = firstVisibleRow; row < firstVisibleRow + visibleRowCount; row++)
                {

                    var item = items?.Current;
                    float currentX = 0;
                    float currentX1 = 0;

                    var columnList = visibleColumns;

                    for (int i = 0; i < firstVisibleCol && i < columnList?.Count; i++)
                    {
                        currentX += (float)columnList[i].Width;
                    }
                    currentX1 += currentX;
                    for (int colIndex = firstVisibleCol; colIndex < visibleColCount; colIndex++)
                    {
                        float GVColumnWidth = (float)visibleColumns[colIndex].Width;
                        var rowcolor = row % 2 == 0 ? RowBackgroundColor : AlternatingRowBackground ?? RowBackgroundColor;
                        if (GroupItems != null)
                        {
                            if (GroupItems[row].IsGroupHeader)
                            {
                                string value = Convert.ToString(GroupItems[row]?.GroupName!);
                                if (Group?.Target!= null && Group?.Target == visibleColumns[colIndex].Name)
                                {
                                    CellContentAlignment cellContentAlignment = visibleColumns[colIndex].ContentAlignment;

                                    var defaultRowtemplate = GetSetterValues(reflectionHelper, Group?.GroupCellTemplate?.Setters, GroupItems[row].Item);
                                    SKPaint BackgroundColor = defaultRowtemplate.BackgroundColor ?? GroupRowBackgroundColor;
                                    SKPaint Foregroundcolor = defaultRowtemplate.Foregroundcolor ?? GroupFontColor;
                                    SKPaint BorderColor = null;

                                    if (Group?.GroupCellTemplate?.Triggers?.Count > 0)
                                    {
                                        var ValuesForTotal = GroupItemSource.Where(x => x.GroupName == GroupItems[row].GroupName && x.IsGroupHeader == false);
                                        var groupRowTemplate = GetGroupTriggerTemplate(reflectionHelper, ValuesForTotal, Group?.GroupCellTemplate?.Triggers);
                                        BackgroundColor = groupRowTemplate.BackgroundColor ?? BackgroundColor;
                                        Foregroundcolor = groupRowTemplate.ForegroundColor ?? Foregroundcolor;
                                        BorderColor = null;
                                    }

                                    Draw(canvas!, colIndex, row, value!, Foregroundcolor, SymbolFont, BackgroundColor, null, GVColumnWidth, currentX, currentY, cellContentAlignment, rowHeight, HighlightSelected(GroupItems[row].Item));
                                }
                                else if (Group?.ToggleSymbol?.TargetColumns == visibleColumns[colIndex].Name)
                                {
                                    if (CurrentContext.GroupToggleDetails.ContainsKey(value!))
                                    {
                                        var values = CurrentContext.GroupToggleDetails[value];
                                        values.x = currentX;
                                        values.y = currentY;
                                        values.width = GVColumnWidth;
                                        values.height = rowHeight;
                                        CurrentContext.GroupToggleDetails[value] = values;
                                    }
                                    var toggleButtonItem = "";
                                    if (GroupItems[row]!.IsExpanded)
                                        toggleButtonItem = Group?.ToggleSymbol?.Expand ?? "";
                                    else
                                        toggleButtonItem = Group?.ToggleSymbol?.Collapse ?? "";

                                    if (Group?.ToggleSymbol != null && Group.ToggleSymbol.ShowGroupDetail.HasValue && Group.ToggleSymbol.ShowGroupDetail.Value)
                                    {
                                        Draw(canvas!, colIndex, row, toggleButtonItem, GroupFontColor, UniCodeFont, GroupRowBackgroundColor, null, 20, currentX, currentY, CellContentAlignment.Left, rowHeight, HighlightSelected(GroupItems[row].Item));
                                        Draw(canvas!, colIndex, row, GroupItems[row].GroupName, GroupFontColor, SymbolFont, GroupRowBackgroundColor, null, GVColumnWidth, currentX + 20, currentY, CellContentAlignment.Left, rowHeight, HighlightSelected(GroupItems[row].Item));
                                    }
                                    else
                                        Draw(canvas!, colIndex, row, toggleButtonItem, GroupFontColor, UniCodeFont, GroupRowBackgroundColor, null, GVColumnWidth, currentX, currentY, CellContentAlignment.Center, rowHeight, HighlightSelected(GroupItems[row].Item));
                                }
                                else if (Group?.HeaderFields != null && Group?.HeaderFields.Count > 0)
                                {
                                    var groupHeader = Group.HeaderFields.FirstOrDefault(x => x.TargetColumns != null && x.TargetColumns == visibleColumns[colIndex].Name);;
                                    if (groupHeader != null)
                                    {
                                        var ValuesForTotal = GroupItemSource.Where(x => x.GroupName == GroupItems[row].GroupName && x.IsGroupHeader == false);
                                        object? aggreateValue = CalculateGroupAggregation(ValuesForTotal, reflectionHelper, groupHeader.BindingPath, groupHeader.Aggregation);


                                        var val = Helper.ApplyFormat(typeof(double), aggreateValue.ToString(), visibleColumns[colIndex].Format, visibleColumns[colIndex].ShowBracketOnNegative, visibleColumns[colIndex].FormatWithAcronym);
                                        var defaultRowtemplate = GetSetterValues(reflectionHelper, groupHeader?.GroupCellTemplate?.Setters, GroupItems[row].Item);
                                        SKPaint BackgroundColor = defaultRowtemplate.BackgroundColor ?? GroupRowBackgroundColor;
                                        SKPaint Foregroundcolor = defaultRowtemplate.Foregroundcolor ?? GroupFontColor;
                                        SKPaint BorderColor = null;

                                        if (groupHeader?.GroupCellTemplate?.Triggers?.Count > 0)
                                        {
                                            var groupRowTemplate = GetGroupTriggerTemplate(reflectionHelper, ValuesForTotal, groupHeader?.GroupCellTemplate?.Triggers);
                                            BackgroundColor = groupRowTemplate.BackgroundColor ?? BackgroundColor;
                                            Foregroundcolor = groupRowTemplate.ForegroundColor ?? Foregroundcolor;
                                            BorderColor = null;
                                        }

                                        Draw(canvas, colIndex, row, val, Foregroundcolor, SymbolFont, BackgroundColor, BorderColor, GVColumnWidth, currentX, currentY, visibleColumns[colIndex].ContentAlignment, rowHeight, HighlightSelected(GroupItems[row].Item));
                                    }
                                    else
                                        Draw(canvas, colIndex, row, "", GroupFontColor, SymbolFont, GroupRowBackgroundColor, null, GVColumnWidth, currentX, currentY, CellContentAlignment.Center, rowHeight, HighlightSelected(GroupItems[row].Item));
                                }
                                else
                                    Draw(canvas, colIndex, row, "", GroupFontColor, SymbolFont, GroupRowBackgroundColor, null, GVColumnWidth, currentX, currentY, CellContentAlignment.Center, rowHeight, HighlightSelected(GroupItems[row].Item));
                            }
                            else
                            {
                                var value = reflectionHelper.ReadCurrentItemWithTypes(GroupItems[row].Item, visibleColumns[colIndex].BindingPath);
                                var val = Helper.ApplyFormat(value.Type, value.Value, visibleColumns[colIndex].Format, visibleColumns[colIndex].ShowBracketOnNegative, visibleColumns[colIndex].FormatWithAcronym);

                                var defaultRowtemplate = GetSetterValues(reflectionHelper, CurrentContext?.RowTemplate?.Setters, GroupItems[row].Item);
                                SKPaint BackgroundColor = defaultRowtemplate.BackgroundColor ?? rowcolor;
                                SKPaint Foregroundcolor = defaultRowtemplate.Foregroundcolor ?? FontColor;
                                SKPaint BorderColor = null;

                                var defaultrowtriggerTemplate = GetTriggerTemplate(GroupItems[row].Item, reflectionHelper, CurrentContext?.RowTemplate?.Triggers);
                                BackgroundColor = defaultrowtriggerTemplate.BackgroundColor ?? BackgroundColor;
                                Foregroundcolor = defaultrowtriggerTemplate.Foregroundcolor ?? Foregroundcolor;
                                BorderColor = null;

                                var defaultcelltemplate = GetSetterValues(reflectionHelper, CurrentContext?.CellTemplate?.Setters, GroupItems[row].Item);
                                BackgroundColor = defaultcelltemplate.BackgroundColor ?? BackgroundColor;
                                Foregroundcolor = defaultcelltemplate.Foregroundcolor ?? Foregroundcolor;
                                BorderColor = defaultcelltemplate.BorderColor ?? BorderColor;

                                var defaulttriggerTemplate = GetTriggerTemplate(GroupItems[row].Item, reflectionHelper, CurrentContext?.CellTemplate?.Triggers);
                                BackgroundColor = defaulttriggerTemplate.BackgroundColor ?? BackgroundColor;
                                Foregroundcolor = defaulttriggerTemplate.Foregroundcolor ?? Foregroundcolor;
                                BorderColor = defaulttriggerTemplate.BorderColor ?? BorderColor;

                                var celltemplate = GetSetterValues(reflectionHelper, visibleColumns[colIndex]?.CellTemplate?.Setters, GroupItems[row].Item);
                                BackgroundColor = celltemplate.BackgroundColor ?? BackgroundColor;
                                Foregroundcolor = celltemplate.Foregroundcolor ?? Foregroundcolor;
                                BorderColor = celltemplate.BorderColor ?? BorderColor;

                                var triggerTemplate = GetTriggerTemplate(GroupItems[row].Item, reflectionHelper, visibleColumns[colIndex].CellTemplate?.Triggers);
                                BackgroundColor = triggerTemplate.BackgroundColor ?? BackgroundColor;
                                Foregroundcolor = triggerTemplate.Foregroundcolor ?? Foregroundcolor;
                                BorderColor = triggerTemplate.BorderColor ?? BorderColor;

                                CellContentAlignment cellContentAlignment = visibleColumns[colIndex].ContentAlignment;
                                Draw(canvas, colIndex, row, visibleColumns[colIndex].DataVisible ? val : "", Foregroundcolor, SymbolFont, BackgroundColor, BorderColor, GVColumnWidth, currentX, currentY, cellContentAlignment, rowHeight, HighlightSelected(GroupItems[row].Item), visibleColumns[colIndex].CellTemplate, GroupItems[row].Item);
                            }
                        }
                        else
                        {
                            var CurrentColumns = visibleColumns[colIndex];

                            var value = reflectionHelper.ReadCurrentItemWithTypes(item, CurrentColumns.BindingPath);
                            var val = Helper.ApplyFormat(value.Type, value.Value, CurrentColumns.Format, CurrentColumns.ShowBracketOnNegative, CurrentColumns.FormatWithAcronym);

                            var defaultRowtemplate = GetSetterValues(reflectionHelper, CurrentContext?.RowTemplate?.Setters, item);
                            SKPaint BackgroundColor = defaultRowtemplate.BackgroundColor ?? rowcolor;
                            SKPaint Foregroundcolor = defaultRowtemplate.Foregroundcolor ?? FontColor;
                            SKPaint BorderColor = null;

                            var defaultrowtriggerTemplate = GetTriggerTemplate(item, reflectionHelper, CurrentContext?.RowTemplate?.Triggers);
                            BackgroundColor = defaultrowtriggerTemplate.BackgroundColor ?? BackgroundColor;
                            Foregroundcolor = defaultrowtriggerTemplate.Foregroundcolor ?? Foregroundcolor;
                            BorderColor = null;

                            var defaultcelltemplate = GetSetterValues(reflectionHelper, CurrentContext?.CellTemplate?.Setters, item);
                            BackgroundColor = defaultcelltemplate.BackgroundColor ?? BackgroundColor;
                            Foregroundcolor = defaultcelltemplate.Foregroundcolor ?? Foregroundcolor;
                            BorderColor = defaultcelltemplate.BorderColor ?? BorderColor;

                            var defaulttriggerTemplate = GetTriggerTemplate(item, reflectionHelper, CurrentContext?.CellTemplate?.Triggers);
                            BackgroundColor = defaulttriggerTemplate.BackgroundColor ?? BackgroundColor;
                            Foregroundcolor = defaulttriggerTemplate.Foregroundcolor ?? Foregroundcolor;
                            BorderColor = defaulttriggerTemplate.BorderColor ?? BorderColor;

                            var celltemplate = GetSetterValues(reflectionHelper, CurrentColumns?.CellTemplate?.Setters, item);
                            BackgroundColor = celltemplate.BackgroundColor ?? BackgroundColor;
                            Foregroundcolor = celltemplate.Foregroundcolor ?? Foregroundcolor;
                            BorderColor = celltemplate.BorderColor ?? BorderColor;

                            var triggerTemplate = GetTriggerTemplate(item, reflectionHelper, CurrentColumns?.CellTemplate?.Triggers);
                            BackgroundColor = triggerTemplate.BackgroundColor ?? BackgroundColor;
                            Foregroundcolor = triggerTemplate.Foregroundcolor ?? Foregroundcolor;
                            BorderColor = triggerTemplate.BorderColor ?? BorderColor;

                            CellContentAlignment cellContentAlignment = visibleColumns[colIndex].ContentAlignment;
                            Draw(canvas, colIndex, row, visibleColumns[colIndex].DataVisible ? val : "", Foregroundcolor, SymbolFont, BackgroundColor, BorderColor, GVColumnWidth, currentX, currentY, cellContentAlignment, rowHeight, HighlightSelected(item), CurrentColumns.CellTemplate, item);

                            if (defaultRowtemplate.BorderColor != null || defaultrowtriggerTemplate.BorderColor != null)
                            {
                                DrawBorder(canvas, defaultrowtriggerTemplate.BorderColor ?? defaultRowtemplate.BorderColor, (float)columnSum, currentX1, currentY, rowHeight);
                            }

                        }

                        if (ShowGridLines)
                        {
                            canvas?.DrawLine(currentX + GVColumnWidth, currentY, currentX + GVColumnWidth, currentY + rowHeight, GridLineColor);
                            canvas?.DrawLine(currentX, currentY + rowHeight, currentX + GVColumnWidth, currentY + rowHeight, GridLineColor);
                        }
                        currentX += GVColumnWidth;
                    }
                    items?.MoveNext();
                    currentY += rowHeight;
                }
            }
            catch (Exception ex)
            {


            }
        }



        public void UpdateVisibleColumns()
        {
            _visibleColumnsCache.Clear();

            if (Columns == null) return;

            foreach (var col in Columns.OrderBy(x => x.DisplayIndex).ToList())
            {
                if (col.IsVisible)
                    _visibleColumnsCache.Add(col);
            }
        }
        private void Draw(SKCanvas canvas, int columnsIndex, int rowIndex, string value, SKPaint fontcolor, SKFont textFont, SKPaint backColor, SKPaint? borderColor, float width, float x, float y, CellContentAlignment cellContentAlignment, float rowHeight, bool isselectedrow, SKCellTemplate? sKCellTemplate = null, object? data = null)
        {

            var rowBackColor = (isselectedrow && IsWindowActive) ? SelectedRowBackgroundHighlighting : backColor;
            var rowTextColor = (isselectedrow && IsWindowActive) ? SelectedRowTextColor : fontcolor;

            var TextX = x;
            var TextTotalX = 0.0f;
            var TextY = y;

            DrawRect(canvas, rowIndex, x, y, rowBackColor, width, rowHeight);


            if (borderColor != null && !(isselectedrow && IsWindowActive))
            {
                DrawBorder2(canvas, borderColor, width, x, y, rowHeight);
            }

            //drawing button

            if (sKCellTemplate != null)
            {
                var dynamicButtons = sKCellTemplate?.DrawButton?.Invoke(data);
                if (dynamicButtons != null && dynamicButtons.Count > 0)
                {
                    foreach (var item in dynamicButtons)
                    {
                        //var btn = new SkButton { Name = item.Name, Width = item.Width, BackgroundColor = item.BackgroundColor, BorderColor = item.BorderColor, ContentAlignment = item.ContentAlignment, ForegroundColor = item.ForegroundColor, MarginLeft = item.MarginLeft, MarginRight = item.MarginRight, Text = item.Text };
                        DrawButton(canvas, item, data, TextX, y, width, rowHeight, rowBackColor, columnsIndex, rowIndex, borderColor != null && !(isselectedrow && IsWindowActive));
                        TextX += (item.Width.HasValue ? (float)(item.Width + item.MarginLeft + item.MarginRight) : 0);
                        TextTotalX += (item.Width.HasValue ? (float)(item.Width + item.MarginLeft + item.MarginRight) : 0);
                    }
                }
                else
                {
                    if (sKCellTemplate?.SkButton != null && sKCellTemplate.SkButtons.Count == 0)
                    {
                        DrawButton(canvas, sKCellTemplate.SkButton, data, TextX, y, width, rowHeight, rowBackColor, columnsIndex, rowIndex, borderColor != null && !(isselectedrow && IsWindowActive));
                        TextX += (sKCellTemplate.SkButton.Width.HasValue ? (float)sKCellTemplate.SkButton.Width : width) + (float)sKCellTemplate.SkButton.MarginLeft + (float)sKCellTemplate.SkButton.MarginRight;
                        TextTotalX += (sKCellTemplate.SkButton.Width.HasValue ? (float)sKCellTemplate.SkButton.Width : width) + (float)sKCellTemplate.SkButton.MarginLeft + (float)sKCellTemplate.SkButton.MarginRight;
                    }
                    else if (sKCellTemplate?.SkButtons != null)
                    {
                        foreach (var item in sKCellTemplate.SkButtons)
                        {
                            DrawButton(canvas, item, data, TextX, y + 0.5f, width, rowHeight - 2, rowBackColor, columnsIndex, rowIndex, borderColor != null && !(isselectedrow && IsWindowActive));
                            TextX += (item.Width.HasValue ? (float)(item.Width + item.MarginLeft + item.MarginRight) : 0);
                            TextTotalX += (item.Width.HasValue ? (float)(item.Width + item.MarginLeft + item.MarginRight) : 0);
                        }
                    }
                }
            }
            DrawText(canvas, columnsIndex, rowIndex, value, rowTextColor, textFont, width - TextTotalX, TextX, TextY, cellContentAlignment);

        }


        private void DrawButton(SKCanvas canvas, SkButton skButton, object data, float CurrentX, float y, double width, float rowHeight, SKPaint rowBackColor, int columnsIndex, int rowIndex, bool isBorderDrawed)
        {

            if (skButton != null)
            {
                if (CurrentContext.ButtonDetails.ContainsKey((data, skButton.Name)))
                {
                    var values = CurrentContext.ButtonDetails[(data, skButton.Name)];
                    values.x = (float)(CurrentX + skButton.MarginLeft);
                    values.y = y;
                    values.width = !skButton.Width.HasValue ? 0 : (float)skButton.Width;
                    values.height = rowHeight;
                    CurrentContext.ButtonDetails[(data, skButton.Name)] = values;
                }
                else
                {
                    CurrentContext.ButtonDetails.Add((data, skButton.Name), (CurrentX, y, rowHeight, !skButton.Width.HasValue ? 0 : (float)skButton.Width, skButton));
                }

                var btnBackColor = skButton.BackgroundColor != null ? new SKPaint() { Color = SKColor.Parse(skButton.BackgroundColor) } : rowBackColor;
                var btnForeColor = skButton.ForegroundColor != null ? new SKPaint() { Color = SKColor.Parse(skButton.ForegroundColor) } : ButtonForegroundcolor;
                var btnBorderColor = skButton.BorderColor != null ? new SKPaint() { Color = SKColor.Parse(skButton.BorderColor) } : ButtonBordercolor;


                DrawButton(
                        canvas,
                        columnsIndex,
                        rowIndex,
                        skButton.Text ?? "",
                        btnForeColor,
                        SymbolFont,
                        btnBackColor,
                        btnBorderColor,
                        !skButton.Width.HasValue
                            ? 0
                            : (float)skButton.Width,
                        CurrentX,
                        y,
                        skButton.ContentAlignment,
                        rowHeight,
                        (float)skButton.MarginRight,
                        (float)skButton.MarginLeft,
                        isBorderDrawed,
                        skButton
                    );

            }

        }

        private void DrawButton(
    SKCanvas canvas,
    int columnsIndex,
    int rowIndex,
    string value,
    SKPaint fontcolor,
    SKFont textFont,
    SKPaint backColor,
    SKPaint? borderColor,
    float width,
    float x,
    float y,
    CellContentAlignment cellContentAlignment,
    float rowHeight,
    float marginRight,
    float marginLeft, bool isBorderDrawed,
    SkButton? skButton = null)
        {
            var rowBackColor = backColor;
            var rowTextColor = fontcolor;
            float contentX = x + marginLeft;
            float contentY = y - 3;
            float contentWidth = width;
            float contentHeight = rowHeight;
            if (isBorderDrawed)
            {
                y += 2;
                rowHeight -= 5;
            }
            // background rect
            DrawRect(canvas, rowIndex, x + marginLeft, y, rowBackColor, width, rowHeight);

            // border
            if (borderColor != null)
            {
                DrawBorder2(canvas, borderColor, width + 1, x + marginLeft, y - 1, rowHeight + 2);
            }

            if (skButton?.ImageSource != null)
            {
                var skBitmap = GetOrCreateBitmap(skButton.ImageSource);
                if (skBitmap != null)
                {

                    var imageSize = Math.Min(contentHeight - 4, contentWidth - 2); // adjust size
                    var imageRect = new SKRect(contentX, contentY + 5, contentX + imageSize - 4, contentY + 4 + imageSize);

                    canvas.DrawBitmap(skBitmap, imageRect);

                    contentX += imageSize + 6;
                    contentWidth -= (imageSize + 6);
                }
            }
            else
                DrawText(canvas, columnsIndex, rowIndex, value, rowTextColor, UniCodeFont, contentWidth, contentX, contentY, cellContentAlignment);
        }
        private readonly Dictionary<ImageSource, SKBitmap> _imageCache = new();

        private SKBitmap? GetOrCreateBitmap(ImageSource? source)
        {
            if (source == null) return null;

            if (_imageCache.TryGetValue(source, out var cached))
            {
                return cached;
            }

            if (source is BitmapSource bitmapSource)
            {
                var bmp = bitmapSource.ToSKBitmap();
                _imageCache[source] = bmp;
                return bmp;
            }

            return null;
        }




        private static void DrawBorder(SKCanvas canvas, SKPaint? borderColor, float width, float x, float y, float rowHeight)
        {
            canvas.DrawLine(x + 1f, y, x + 1f, y + rowHeight, borderColor);
            canvas.DrawLine(x + width - 1f, y, x + width - 1f, y + rowHeight, borderColor);
            canvas.DrawLine(x, y + rowHeight - 2f, x + width, y + rowHeight - 2f, borderColor);//bottom
            canvas.DrawLine(x, y + 0.5f, x + width, y + 0.5f, borderColor); // top
        }
        private static void DrawBorder2(SKCanvas canvas, SKPaint? borderColor, float width, float x, float y, float rowHeight)
        {
            canvas.DrawLine(x + 1f, y + 1f, x + 1f, y + rowHeight - 1f, borderColor); //left
            canvas.DrawLine(x + width - 2f, y + 1f, x + width - 2f, y + rowHeight - 2f, borderColor);//right
            canvas.DrawLine(x + 1f, y + rowHeight - 2f, x + width - 2f, y + rowHeight - 2f, borderColor);//bottom
            canvas.DrawLine(x + 1f, y + 1f, x + width - 2f, y + 1f, borderColor); // top
        }
        private void DrawText(SKCanvas canvas, int columnsIndex, int rowIndex, string value, SKPaint fontColor, SKFont textFont, float width, float x, float y, CellContentAlignment cellContentAlignment)
        {
            if (width < 10 || string.IsNullOrEmpty(value))
                return;

            float maxTextWidth = width - 10;
            ReadOnlySpan<char> span = value;
            ReadOnlySpan<char> ellipsis = "...";
            float ellipsisWidth = textFont.MeasureText(ellipsis, out _);

            int left = 0;
            int right = span.Length;
            int fitLength = span.Length;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var testSpan = span.Slice(0, mid);
                float testWidth = textFont.MeasureText(testSpan, out _);

                if (testWidth + (mid < span.Length ? ellipsisWidth : 0) <= maxTextWidth)
                {
                    fitLength = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            string finalText = span.Slice(0, fitLength).ToString();
            bool wasTrimmed = fitLength < span.Length;
            if (wasTrimmed)
                finalText += "...";

            float finalWidth = textFont.MeasureText(finalText, out _);

            float textX = x + 5;
            if (cellContentAlignment == CellContentAlignment.Right)
                textX = x + width - finalWidth - 5;
            else if (cellContentAlignment == CellContentAlignment.Center)
                textX = x + (width - finalWidth) / 2;

            canvas.DrawText(finalText, textX, y + textFont.Size, textFont, fontColor);
        }
        private void DrawRect(SKCanvas canvas, int rowIndex, float x, float y, SKPaint backColor, float width, float rowHeight)
        {
            float left = ShowGridLines ? x : x;
            float top = ShowGridLines ? y : y - 1;
            float right = x + width;
            float bottom = y + rowHeight;

            canvas.DrawRect(SKRect.Create(left, top + (0.25f), right - left, bottom - top), backColor);
        }

        private bool HighlightSelected(object? item)
        {
            if (SelectedItems == null)
                return false;

            foreach (var selectedItem in SelectedItems)
            {
                if (item == selectedItem)
                {
                    return true;
                }
            }

            return false;
        }

        private object? CalculateGroupAggregation(
    IEnumerable<GroupModel> groupItems,
    ReflectionHelper reflectionHelper,
    string bindingPath,
    SkAggregation aggregation)
        {
            var rawValues = groupItems
                .Select(x => x.Item)
                .Select(item =>
                {
                    var (strVal, _) = reflectionHelper.ReadCurrentItemWithTypes(item, bindingPath);
                    return strVal;
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!rawValues.Any())
                return "";

            switch (aggregation)
            {
                case SkAggregation.Sum:
                case SkAggregation.Count:
                case SkAggregation.Avg:
                case SkAggregation.Min:
                case SkAggregation.Max:
                    var numbers = rawValues
                        .Select(v => double.TryParse(v, out var num) ? (double?)num : null)
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                        .ToList();

                    if (!numbers.Any())
                        return null;

                    return aggregation switch
                    {
                        SkAggregation.Sum => numbers.Sum(),
                        SkAggregation.Count => numbers.Count,
                        SkAggregation.Avg => numbers.Average(),
                        SkAggregation.Min => numbers.Min(),
                        SkAggregation.Max => numbers.Max(),
                        _ => null
                    };

                case SkAggregation.Distinct:
                    return string.Join('/', (rawValues != null && rawValues.Count > 0) ? rawValues
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x).ToArray() : []);


                default:
                    return null;
            }
        }


        //private static (SKPaint BackgroundColor, SKPaint Foregroundcolor, SKPaint BorderColor) GetTriggerTemplate(object item, ReflectionHelper reflection, IEnumerable<SKTrigger> triggers)
        //{
        //    SKPaint backgroundColor = null;
        //    SKPaint foregroundcolor = null;
        //    SKPaint borderColor = null;
        //    if (triggers != null && triggers.Count() > 0)
        //    {
        //        foreach (var trigger in triggers)
        //        {
        //            var res = trigger.Evaluate(item, reflection);
        //            if (res)
        //            {
        //                if (trigger.IsTimerBased)
        //                {
        //                    if (trigger.Duration >= (DateTime.Now.AddSeconds(3) - DateTime.Now).TotalSeconds)
        //                    {
        //                        return GetSetterValues(reflection, trigger.Setters, item);
        //                    }
        //                }
        //                else
        //                {
        //                    return GetSetterValues(reflection, trigger.Setters, item);
        //                }
        //            }
        //        }
        //    }
        //    return (backgroundColor, foregroundcolor, borderColor);
        //}



        private static (SKPaint BackgroundColor, SKPaint Foregroundcolor, SKPaint BorderColor) GetTriggerTemplate(
    object item, ReflectionHelper reflection, IEnumerable<SKTrigger> triggers)
        {
            SKPaint backgroundColor = null;
            SKPaint foregroundcolor = null;
            SKPaint borderColor = null;

            if (triggers == null || !triggers.Any())
                return (backgroundColor, foregroundcolor, borderColor);

            foreach (var trigger in triggers)
            {
                if (trigger.Evaluate(item, reflection))
                {
                    string bindingPath = (trigger as SKDataTrigger)?.BindingPath
                                       ?? (trigger as SKMultiTrigger)?.Conditions?.Last()?.BindingPath!;

                    object? currentValue = null;
                    if (!string.IsNullOrEmpty(bindingPath))
                        (currentValue, _) = reflection.ReadCurrentItemWithTypes(item, bindingPath);

                    if (SKTriggerStateCache.ShouldApply(trigger, item, bindingPath, currentValue, true))
                    {
                        return GetSetterValues(reflection, trigger.Setters, item);
                    }
                }

            }

            return (backgroundColor, foregroundcolor, borderColor);
        }

        private (SKPaint BackgroundColor, SKPaint ForegroundColor, SKPaint BorderColor) GetGroupTriggerTemplate(
        ReflectionHelper reflectionHelper,
        IEnumerable<GroupModel> groupItems,
        IEnumerable<SKGroupTrigger>? triggers)
        {
            SKPaint backgroundColor = null;
            SKPaint foregroundColor = null;
            SKPaint borderColor = null;

            if (triggers == null || !triggers.Any())
                return (backgroundColor, foregroundColor, borderColor);

            foreach (var trigger in triggers)
            {
                bool isMatch = false;

                switch (trigger)
                {
                    case SkGroupDataTrigger dataTrigger:
                        isMatch = EvaluateGroupDataTriggerInternal(dataTrigger, groupItems, reflectionHelper);
                        break;

                    case SKGroupMultiTrigger multiTrigger:
                        isMatch = EvaluateGroupMultiTriggerInternal(multiTrigger, groupItems, reflectionHelper);
                        break;
                }

                if (isMatch && trigger.Setters != null && trigger.Setters.Any())
                {
                    // Yahan tumhare existing GetSetterValues function ko call karenge
                    return GetSetterValues(reflectionHelper, trigger.Setters, groupItems);
                }
            }

            return (backgroundColor, foregroundColor, borderColor);
        }

        private (SKPaint? BackgroundColor, SKPaint? Foregroundcolor, SKPaint? BorderColor) GetGroupTriggerValues(ReflectionHelper reflectionHelper, IEnumerable<GroupModel> groupItems, SKGroupTrigger trigger)
        {
            SKPaint? backgroundColor = null;
            SKPaint? foregroundcolor = null;
            SKPaint? borderColor = null;

            if (trigger != null)
            {

                return GetSetterValues(reflectionHelper, EvaluateGroupTrigger(trigger, groupItems, reflectionHelper), null);

            }

            return (backgroundColor, foregroundcolor, borderColor);
        }

        private IEnumerable<SKSetter> EvaluateGroupTrigger(
            SKGroupTrigger trigger,
            IEnumerable<GroupModel> groupItems,
            ReflectionHelper reflectionHelper)
        {
            bool isMatch = false;

            switch (trigger)
            {
                case SkGroupDataTrigger dataTrigger:
                    isMatch = EvaluateGroupDataTriggerInternal(dataTrigger, groupItems, reflectionHelper);
                    break;

                case SKGroupMultiTrigger multiTrigger:
                    isMatch = EvaluateGroupMultiTriggerInternal(multiTrigger, groupItems, reflectionHelper);
                    break;
            }

            if (isMatch)
                return trigger.Setters ?? Enumerable.Empty<SKSetter>();

            return Enumerable.Empty<SKSetter>();
        }

        //    private bool EvaluateGroupDataTriggerInternal(
        //SkGroupDataTrigger trigger,
        //IEnumerable<GroupModel> groupItems,
        //ReflectionHelper reflectionHelper)
        //    {
        //        var aggValue = (double?)CalculateGroupAggregation(
        //            groupItems,
        //            reflectionHelper,
        //            trigger.BindingPath!,
        //            trigger.Aggregation
        //        );

        //        if (!aggValue.HasValue)
        //            return false;

        //        if (!double.TryParse(trigger.Value?.ToString(), out var compareValue))
        //            return false;

        //        return CompareValues(aggValue.Value, compareValue, trigger.Operator);
        //    }
        private bool EvaluateGroupDataTriggerInternal(
            SkGroupDataTrigger trigger,
            IEnumerable<GroupModel> groupItems,
            ReflectionHelper reflectionHelper)
        {
            object? leftValue = null;

            if (trigger.Binding != null) // 🔥 direct binding mode
            {
                leftValue = trigger.Binding;
            }
            else if (!string.IsNullOrEmpty(trigger.BindingPath)) // 🔥 aggregation mode
            {
                leftValue = CalculateGroupAggregation(
                    groupItems,
                    reflectionHelper,
                    trigger.BindingPath!,
                    trigger.Aggregation
                );
            }

            if (leftValue == null)
                return false;

            return CompareValues(leftValue, trigger.Value, trigger.Operator);
        }



        private bool EvaluateGroupMultiTriggerInternal(
            SKGroupMultiTrigger multiTrigger,
            IEnumerable<GroupModel> groupItems,
            ReflectionHelper reflectionHelper)
        {
            if (multiTrigger.Conditions == null || multiTrigger.Conditions.Count == 0)
                return false;

            return multiTrigger.Conditions.All(c => EvaluateGroupConditionInternal(c, groupItems, reflectionHelper));
        }

        //private bool EvaluateGroupConditionInternal(
        //    SKGroupCondition condition,
        //    IEnumerable<GroupModel> groupItems,
        //    ReflectionHelper reflectionHelper)
        //{
        //    var aggValue = (double?)CalculateGroupAggregation(
        //        groupItems,
        //        reflectionHelper,
        //        condition.BindingPath!,
        //        condition.Aggregation
        //    );

        //    if (!aggValue.HasValue)
        //        return false;

        //    if (!double.TryParse(condition.Value?.ToString(), out var compareValue))
        //        return false;

        //    return CompareValues(aggValue.Value, compareValue, condition.Operator);
        //}

        private bool EvaluateGroupConditionInternal(
    SKGroupCondition condition,
    IEnumerable<GroupModel> groupItems,
    ReflectionHelper reflectionHelper)
        {
            object? leftValue = null;

            if (condition.Binding != null) // ✅ Direct Binding
            {
                leftValue = condition.Binding;
            }
            else if (!string.IsNullOrEmpty(condition.BindingPath)) // ✅ Aggregation mode
            {

                leftValue = CalculateGroupAggregation(
                    groupItems,
                    reflectionHelper,
                    condition.BindingPath!,
                    condition.Aggregation
                );
            }

            if (leftValue == null)
                return false;

            return CompareValues(leftValue, condition.Value, condition.Operator);


        }



        //private bool CompareValues(double left, double right, SKOperation op)
        //{
        //    return op switch
        //    {
        //        SKOperation.Equals => left == right,
        //        SKOperation.NotEquals => left != right,
        //        SKOperation.GreaterThan => left > right,
        //        SKOperation.GreaterThanOrEqual => left >= right,
        //        SKOperation.LessThan => left < right,
        //        SKOperation.LessThanOrEqual => left <= right,
        //        _ => false
        //    };
        //}

        private bool CompareValues(object left, object right, SKOperation op)
        {
            if (left == null || right == null)
                return false;

            // Try to normalize types
            if (left is IConvertible && right is IConvertible)
            {
                // ✅ Try number compare
                if (double.TryParse(left.ToString(), out var leftNum) &&
                    double.TryParse(right.ToString(), out var rightNum))
                {
                    return op switch
                    {
                        SKOperation.Equals => leftNum == rightNum,
                        SKOperation.NotEquals => leftNum != rightNum,
                        SKOperation.GreaterThan => leftNum > rightNum,
                        SKOperation.GreaterThanOrEqual => leftNum >= rightNum,
                        SKOperation.LessThan => leftNum < rightNum,
                        SKOperation.LessThanOrEqual => leftNum <= rightNum,
                        _ => false
                    };
                }

                // ✅ Try bool compare
                if (bool.TryParse(left.ToString(), out var leftBool) &&
                    bool.TryParse(right.ToString(), out var rightBool))
                {
                    return op switch
                    {
                        SKOperation.Equals => leftBool == rightBool,
                        SKOperation.NotEquals => leftBool != rightBool,
                        _ => false // For bool, only equality/inequality make sense
                    };
                }
            }

            // ✅ String compare (case-insensitive)
            var leftStr = left.ToString();
            var rightStr = right.ToString();

            return op switch
            {
                SKOperation.Equals => string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
                SKOperation.NotEquals => !string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
                SKOperation.GreaterThan => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) > 0,
                SKOperation.GreaterThanOrEqual => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) >= 0,
                SKOperation.LessThan => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) < 0,
                SKOperation.LessThanOrEqual => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) <= 0,
                _ => false
            };
        }


        private static (SKPaint BackgroundColor, SKPaint Foregroundcolor, SKPaint BorderColor) GetSetterValues(ReflectionHelper reflection, IEnumerable<SKSetter>? setters, object Item)
        {
            SKPaint backgroundColor = null;
            SKPaint foregroundcolor = null;
            SKPaint borderColor = null;
            if (setters != null)
            {
                foreach (var item1 in setters)
                {
                    var value = "";
                    if (string.IsNullOrEmpty(item1.ValuePath) || Item == null)
                        value = item1.Value?.ToString() ?? "";
                    else
                    {
                        var (strVal, _) = reflection.ReadCurrentItemWithTypes(Item, item1.ValuePath);
                        value = strVal ?? "";
                    }


                    switch (item1.Property)
                    {
                        case SkStyleProperty.Background:
                            backgroundColor = new SKPaint { Color = SKColor.Parse(value), StrokeWidth = 1, IsAntialias = true };
                            break;
                        case SkStyleProperty.Foreground:
                            foregroundcolor = new SKPaint { Color = SKColor.Parse(value), StrokeWidth = 1, IsAntialias = true };
                            break;
                        case SkStyleProperty.BorderColor:
                            borderColor = new SKPaint { Color = SKColor.Parse(value), StrokeWidth = 1, IsAntialias = true };
                            break;
                        default:
                            break;
                    }
                }
            }
            return (backgroundColor, foregroundcolor, borderColor);
        }
        //public string ExportData(SKExportType exportType)
        //{
        //    if (_visibleColumnsCache == null || Items == null)
        //        return "";

        //    var columns = _visibleColumnsCache
        //                    .Where(c => c.IsVisible)
        //                    .OrderBy(c => c.DisplayIndex)
        //                    .ToList();

        //    StringBuilder sb = new();

        //    // Header
        //    sb.AppendLine(string.Join("\t", columns.Select(c => c.Header)));

        //    var items = exportType == SKExportType.Selected ? SelectedItems.Cast<object>() : Items.Cast<object>();
        //    // Rows
        //    foreach (var item in items)
        //    {
        //        List<string> row = new();

        //        foreach (var col in columns)
        //        {
        //            var val = string.IsNullOrEmpty(col.BindingPath) ? ("", null) : reflectionHelper.ReadCurrentItemWithTypes(item, col.BindingPath);
        //            var formatted = Helper.ApplyFormat(val.Type, val.Value, col.Format, col.ShowBracketOnNegative, col.FormatWithAcronym);
        //            row.Add(formatted);
        //        }

        //        sb.AppendLine(string.Join("\t", row));
        //    }
        //    return sb.ToString();
        //}
        public string ExportData(SKExportType exportType)
        {
            if (_visibleColumnsCache == null || (Items == null && GroupItemSource == null))
                return "";

            var columns = _visibleColumnsCache
                            .Where(c => c.IsVisible)
                            .OrderBy(c => c.DisplayIndex)
                            .ToList();

            StringBuilder sb = new();

            // 🔹 Header Row
            sb.AppendLine(string.Join("\t", columns.Select(c => c.Header)));

            // 🔹 Decide source: grouped or flat
            IEnumerable<object> exportSource;
            if (GroupItemSource != null && GroupItemSource.Count > 0)
            {
                if (exportType == SKExportType.Selected)
                    exportSource = GroupItemSource.Where(x => SelectedItems!.Cast<object>().Contains(x.Item));
                else
                    exportSource = GroupItemSource;
            }
            else
            {
                exportSource = exportType == SKExportType.Selected
                    ? SelectedItems!.Cast<object>()
                    : Items!.Cast<object>();
            }

            // 🔹 Iterate rows
            foreach (var row in exportSource)
            {
                if (row is GroupModel gi)
                {
                    if (gi.IsGroupHeader)
                    {
                        // Export group header row
                        List<string> rowData = new();
                        foreach (var col in columns)
                        {
                            if (Group?.Target == col.Name)
                            {
                                rowData.Add(gi.GroupName ?? "");
                            }
                            else if (Group?.HeaderFields?.Any(x => x.TargetColumns == col.Name) == true)
                            {
                                var header = Group.HeaderFields.First(x => x.TargetColumns == col.Name);
                                var groupValues = GroupItemSource!
                                    .Where(x => x.GroupName == gi.GroupName && x.IsGroupHeader == false);

                                object? agg = CalculateGroupAggregation(groupValues, reflectionHelper, header.BindingPath, header.Aggregation);
                                var formatted = Helper.ApplyFormat(typeof(double), agg?.ToString(), col.Format!, col.ShowBracketOnNegative, col.FormatWithAcronym);
                                rowData.Add(formatted);
                            }
                            else
                            {
                                rowData.Add(""); // blank for non-group columns
                            }
                        }
                        sb.AppendLine(string.Join("\t", rowData));
                    }
                    else
                    {
                        // Normal item row
                        List<string> rowData = new();
                        foreach (var col in columns)
                        {
                            var val = reflectionHelper.ReadCurrentItemWithTypes(gi.Item, col.BindingPath);
                            var formatted = Helper.ApplyFormat(val.Type!, val.Value, col.Format!, col.ShowBracketOnNegative, col.FormatWithAcronym);
                            if (col.DataVisible)
                                rowData.Add(formatted);
                            else
                                rowData.Add("");
                        }
                        sb.AppendLine(string.Join("\t", rowData));
                    }
                }
                else
                {
                    // Fallback for flat (non-grouped) rows
                    List<string> rowData = new();
                    foreach (var col in columns)
                    {
                        var val = reflectionHelper.ReadCurrentItemWithTypes(row, col.BindingPath);
                        var formatted = Helper.ApplyFormat(val.Type!, val.Value, col.Format!, col.ShowBracketOnNegative, col.FormatWithAcronym);
                        rowData.Add(formatted);
                    }
                    sb.AppendLine(string.Join("\t", rowData));
                }
            }

            return sb.ToString();
        }

        public void Dispose()
        {

        }
    }
    public static class SKTriggerStateCache
    {
        private static readonly Dictionary<string, (object LastValue, DateTime ExpireAt)> _cache = new();

        public static bool ShouldApply(SKTrigger trigger, object item, string bindingPath, object? currentValue, bool conditionResult)
        {
            string key = $"{item.GetHashCode()}_{trigger.GetHashCode()}_{bindingPath}";
            if (!_cache.TryGetValue(key, out var entry))
                entry = (null, DateTime.MinValue);

            // case: timer based
            if (trigger.IsTimerBased)
            {
                if (conditionResult)
                {
                    // value changed => reset timer
                    if (!Equals(entry.LastValue, currentValue))
                    {
                        _cache[key] = (currentValue, DateTime.Now.AddSeconds(trigger.Duration));
                        return true;
                    }

                    // same value but still in active duration
                    if (entry.ExpireAt > DateTime.Now)
                        return true;

                    return false; // expired => no highlight
                }
                else
                {
                    // condition false => NO highlight 
                    return false;
                }
            }
            else
            {
                // non-timer triggers => continuous highlight if condition true
                if (conditionResult)
                {
                    _cache[key] = (currentValue, DateTime.MinValue);
                    return true;
                }
                return false;
            }
        }
    }
}
