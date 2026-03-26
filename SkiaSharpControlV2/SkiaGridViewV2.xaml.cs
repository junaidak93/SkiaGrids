
using OpenTK.Compute.OpenCL;
using SkiaSharp;
using SkiaSharpControlV2.Data.Enum;
using SkiaSharpControlV2.Data.Models;
using SkiaSharpControlV2.Helpers;
using SkiaSharpControlV2.Model;
using SkiaSharpControlV2.Renderer;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;


namespace SkiaSharpControlV2
{
    /// <summary>
    /// Interaction logic for SkiaGridViewV2.xaml
    /// </summary>

    //[ContentProperty(nameof(Columns))]
    public partial class SkiaGridViewV2 : UserControl, IDisposable
    {
        private SkiaRenderer SkiaRenderer;
        private ReflectionHelper reflectionHelper;
        private DispatcherTimer SortTimer = new DispatcherTimer();
        #region Properties
        private float ScrollOffsetX = 0, ScrollOffsetY = 0, RowHeight = 12 + 4;
        private int TotalRows, lastSelectedRowIndex = 0;
        private int? _selectionAnchorIndex = null;

         const string TIMERBASED_SORTING_COLOR = "#008040";
         const string NORMAL_GRID_COLUMN_COLOR = "#FF3F3F3F";
         const string FILTER_COLOR = "#0072C6";
        private ScrollViewer DataListViewScroll { get => Helper.FindScrollViewer(DataListView); }
        private ICustomCollectionView? _collectionView;
        private bool IsBusy { get; set; } = false;
        private float Scale { get; set; }
        private bool _isDirty = true;

        internal Dictionary<string, (bool IsExpended, float x, float y, float height, float width)> GroupToggleDetails = new();
        internal Dictionary<(object, string name), (float x, float y, float height, float width, SkButton btn)> ButtonDetails = new();
        #endregion Properties
        public SkiaGridViewV2()
        {
            InitializeComponent();
            DataListView.ItemsSource = new List<string> { "" };
            Columns = new SkGridColumnCollection();
            reflectionHelper = new();
            SkiaRenderer = new(this, reflectionHelper);

            Loaded += (s, e) =>
            {
                SetScale();
                UpdateColumnsInDataGrid();
                UpdateSkiaGrid();
                UpdateScrollValues();
                if (Columns == null || Columns?.Count == 0)
                {
                    TryGenerateColumns(ItemsSource);
                }
                else
                {
                    foreach (var item in Columns!)
                    {
                        if (item?.CellTemplate?.SkButton != null)
                        {
                            if (string.IsNullOrWhiteSpace(item?.CellTemplate?.SkButton.Name))
                            {
                                throw new InvalidOperationException("SkButton.Name is required.");
                            }
                        }
                        if (item?.CellTemplate?.SkButtons.Count > 0)
                        {
                            foreach (var item1 in item.CellTemplate.SkButtons)
                            {
                                if (string.IsNullOrWhiteSpace(item1.Name))
                                {
                                    throw new InvalidOperationException("SkButton.Name is required.");
                                }
                            }
                        }
                    }
                }
                SkiaRenderer.UpdateSelectedItems(SelectedItems);
                SkiaRenderer.SetColumns(Columns!);
                SkiaRenderer.SetGroup(GroupSettings);
                if (GroupSettings != null)
                {
                    if (string.IsNullOrEmpty(GroupSettings.GroupBy))
                    {
                        throw new InvalidOperationException("GroupSettings.GroupBy is required when GroupSettings is set.");
                    }
                    if (_collectionView != null)
                    {
                        _collectionView.ClearGroup();
                        _collectionView.ApplyGroup(GroupSettings.GroupBy);
                        _collectionView.GroupFields = GroupSettings?.HeaderFields?.ToList() ;
                    }
                    UpdateGroupCollection();
                }
                else
                    UpdateTotalRows();
                if (_collectionView != null)
                {
                    _collectionView.CollectionChanged -= CollectionViewChanged;
                    _collectionView.CollectionChanged += CollectionViewChanged;
                    _collectionView.IsLiveSort = IsLiveSorting;
                    _collectionView.FilterByGroup = FilterByGroup;
                }



                SkiaRenderer.UpdateVisibleColumns();
                SkiaRenderer.SetGridLinesVisibility(ShowGridLines);
                SkiaRenderer.SetScrollBars(HorizontalScrollViewer, VerticalScrollViewer);
                SkiaRenderer.SetFontSize(SKFontSize);
                SkiaRenderer.SetFontFamily(SKFontFamily);
                SkiaRenderer.SetFontStyle(SKFontStyle);
                SkiaRenderer.SetGridLinesColor(GridLinesColor);
                SkiaRenderer.SetForeground(ForegroundColor);
                SkiaRenderer.SetRowBackgroundColor(RowBackground);
                SkiaRenderer.SetAlternatingRowBackground(AlternatingRowBackground);
                SkiaRenderer.SetGroupRowBackgroundColor(GroupSettings?.RowBackground);
                SkiaRenderer.SetGroupFontColor(GroupSettings?.ForegroundColor);
                SubscribeToGroupColumnEvents(GroupSettings);

                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.Activated += ParentWindow_Activated;
                    parentWindow.Deactivated += ParentWindow_Deactivated;
                }
                DataListView.AddHandler(
                       DataGridColumnHeader.PreviewMouseDoubleClickEvent,
                       new MouseButtonEventHandler((sender, e) =>
                       {
                           if (e.OriginalSource is FrameworkElement element &&
                               element.TemplatedParent is System.Windows.Controls.Primitives.Thumb)
                           {
                               e.Handled = true; // Cancel default resizing
                           }
                       }),
                       true);
                SortTimer.Tick += SortTimerChanged;
                if (SortEvery.HasValue && SortEvery.Value > 0)
                {
                    SortTimer.Interval = TimeSpan.FromSeconds(SortEvery.Value);
                    SortTimer.Start();
                }


            };
            Unloaded += (s, e) =>
            {
                if (_collectionView != null)
                    _collectionView.CollectionChanged -= CollectionViewChanged;

                SortTimer.Tick -= SortTimerChanged;
                SortTimer.Stop();

                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.Activated -= ParentWindow_Activated;
                    parentWindow.Deactivated -= ParentWindow_Deactivated;
                }
            };
            SizeChanged += (s, o) =>
            {
                SetScale();
                UpdateSkiaGrid();
                UpdateScrollValues();
                UpdateHorizontalScroll();
                UpdateVerticalScroll();
            };

        }

        #region Columns
        public SkGridColumnCollection Columns
        {
            get => (SkGridColumnCollection)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(nameof(Columns), typeof(SkGridColumnCollection), typeof(SkiaGridViewV2), new PropertyMetadata(null, OnColumnsChanged));

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        #endregion Columns

        #region GroupColumns
        public SKGroupDefinition GroupSettings
        {
            get => (SKGroupDefinition)GetValue(GroupSettingsProperty);
            set => SetValue(GroupSettingsProperty, value);
        }

        public static readonly DependencyProperty GroupSettingsProperty =
            DependencyProperty.Register(nameof(GroupSettings), typeof(SKGroupDefinition), typeof(SkiaGridViewV2), new PropertyMetadata(null));


        //public bool ShowGroupColumn
        //{
        //    get => (bool)GetValue(ShowGroupColumnProperty);
        //    set => SetValue(ShowGroupColumnProperty, value);
        //}

        //public static readonly DependencyProperty ShowGroupColumnProperty =
        //    DependencyProperty.Register(nameof(ShowGroupColumn), typeof(bool), typeof(SkiaGridViewV2), new PropertyMetadata(true));
        #endregion GroupColumns

        #region ItemSource
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SkiaGridViewV2), new PropertyMetadata(default, OnItemsSourceChanged));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;

            grid._collectionView = new CustomCollectionView((IEnumerable)e.NewValue, grid.reflectionHelper);

            grid.SkiaRenderer.UpdateItems(grid._collectionView);

            grid._collectionView.IsLiveSort = grid.IsLiveSorting;
            grid._collectionView.CollectionChanged -= grid.CollectionViewChanged;
            grid._collectionView.CollectionChanged += grid.CollectionViewChanged;
            grid._collectionView.FilterByGroup = grid.FilterByGroup;
            grid._collectionView.GroupFields = grid.GroupSettings?.HeaderFields?.ToList();
            if (grid.Columns != null)
            {
                var sortcolumn = grid.Columns.LastOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None);
                if (sortcolumn?.BindingPath != null && sortcolumn?.GridViewColumnSort != null)
                    grid.ApplySort(sortcolumn.BindingPath, sortcolumn.GridViewColumnSort == SkGridViewColumnSort.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }

            grid.lastSelectedRowIndex = 0;
            grid.UpdateSkiaGrid();
            grid.UpdateScrollValues();

        }


        public IEnumerable? ViewList
        {
            get { if (_collectionView != null) return _collectionView.ViewList; else return null; }

        }


        //private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    UpdateSkiaGrid();
        //    UpdateScrollValues();
        //}
        private void TryGenerateColumns(object itemsSource)
        {
            if (itemsSource is IEnumerable enumerable)
            {
                var itemType = itemsSource.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault()
                    ?? enumerable.Cast<object>().FirstOrDefault()?.GetType();

                if (itemType != null && Columns.Count == 0)
                {
                    GenerateColumnsFromType(itemType);
                }
            }
        }
        private void GenerateColumnsFromType(Type modelType)
        {
            Columns.Clear();
            foreach (var prop in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Columns.Add(new SKGridViewColumn
                {
                    Header = prop.Name,
                    // BindingPath = prop.Name,
                    Width = 120,
                    IsVisible = true
                });
            }
            UpdateColumnsInDataGrid();
            UpdateScrollValues();
        }
        #endregion ItemSource

        #region SelectedItems
        public ObservableCollection<object> SelectedItems
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<object>), typeof(SkiaGridViewV2), new PropertyMetadata(default, OnSelectedItemsChanged));

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView && e.NewValue is ObservableCollection<object>)
            {
                skGridView.SkiaCanvas.InvalidateVisual();
            }
        }

        public bool RetainSelectionOnLFocusLost
        {
            get { return (bool)GetValue(RetainSelectionOnLFocusLostProperty); }
            set { SetValue(RetainSelectionOnLFocusLostProperty, value); }
        }

        public static readonly DependencyProperty RetainSelectionOnLFocusLostProperty =
            DependencyProperty.Register(
                nameof(RetainSelectionOnLFocusLost),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(false, RetainSelectionOnLFocusLostPropertyChanged));

        private static void RetainSelectionOnLFocusLostPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (!(bool)e.NewValue)
                    skGridView?.SelectedItems?.Clear();
            }
        }

        public bool CanUserSelectRows
        {
            get { return (bool)GetValue(CanUserSelectRowsProperty); }
            set { SetValue(CanUserSelectRowsProperty, value); }
        }

        public static readonly DependencyProperty CanUserSelectRowsProperty =
            DependencyProperty.Register(
                nameof(CanUserSelectRows),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(true, CanUserSelectRowsPropertyChanged));

        private static void CanUserSelectRowsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (!(bool)e.NewValue)
                    skGridView?.SelectedItems?.Clear();
            }
        }
        public Action<object, SKGridViewColumn> OnCellClicked
        {
            get { return (Action<object, SKGridViewColumn>)GetValue(OnCellClickedProperty); }
            set { SetValue(OnCellClickedProperty, value); }
        }

        public static readonly DependencyProperty OnCellClickedProperty =
            DependencyProperty.Register(nameof(OnCellClicked), typeof(Action<object, SKGridViewColumn>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action<object> OnRowClicked
        {
            get { return (Action<object>)GetValue(OnRowClickedProperty); }
            set { SetValue(OnRowClickedProperty, value); }
        }

        public static readonly DependencyProperty OnRowClickedProperty =
            DependencyProperty.Register(nameof(OnRowClicked), typeof(Action<object>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action<object> OnRowRightClicked
        {
            get { return (Action<object>)GetValue(OnRowRightClickedProperty); }
            set { SetValue(OnRowRightClickedProperty, value); }
        }

        public static readonly DependencyProperty OnRowRightClickedProperty =
            DependencyProperty.Register(nameof(OnRowRightClicked), typeof(Action<object>), typeof(SkiaGridViewV2), new PropertyMetadata(default));
        public Action<object> OnRowDoubleClicked
        {
            get { return (Action<object>)GetValue(OnRowDoubleClickedProperty); }
            set { SetValue(OnRowDoubleClickedProperty, value); }
        }

        public static readonly DependencyProperty OnRowDoubleClickedProperty =
            DependencyProperty.Register(nameof(OnRowDoubleClicked), typeof(Action<object>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action<Key> OnPreviewKeyDownEvent
        {
            get { return (Action<Key>)GetValue(OnPreviewKeyDownEventProperty); }
            set { SetValue(OnPreviewKeyDownEventProperty, value); }
        }

        public static readonly DependencyProperty OnPreviewKeyDownEventProperty =
            DependencyProperty.Register(nameof(OnPreviewKeyDownEvent), typeof(Action<Key>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action OnSkGridDoubleClicked
        {
            get { return (Action)GetValue(OnSkGridDoubleClickedProperty); }
            set { SetValue(OnSkGridDoubleClickedProperty, value); }
        }

        public static readonly DependencyProperty OnSkGridDoubleClickedProperty =
            DependencyProperty.Register(nameof(OnSkGridDoubleClicked), typeof(Action), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public bool CanUserReorderColumns
        {
            get { return (bool)GetValue(CanUserReorderColumnsProperty); }
            set { SetValue(CanUserReorderColumnsProperty, value); }
        }

        public static readonly DependencyProperty CanUserReorderColumnsProperty =
            DependencyProperty.Register(
                nameof(CanUserReorderColumns),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(true, CanUserReorderColumnsChanged));

        private static void CanUserReorderColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.DataListView.CanUserReorderColumns = (bool)e.NewValue;
            }
        }

        public bool CanUserResizeColumns
        {
            get { return (bool)GetValue(CanUserResizeColumnsProperty); }
            set { SetValue(CanUserResizeColumnsProperty, value); }
        }

        public static readonly DependencyProperty CanUserResizeColumnsProperty =
            DependencyProperty.Register(
                nameof(CanUserResizeColumns),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(true, CanUserResizeColumnsChanged));

        private static void CanUserResizeColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.DataListView.CanUserResizeColumns = (bool)e.NewValue;
            }
        }

        public bool CanUserSortColumns
        {
            get { return (bool)GetValue(CanUserSortColumnsProperty); }
            set { SetValue(CanUserSortColumnsProperty, value); }
        }

        public static readonly DependencyProperty CanUserSortColumnsProperty =
            DependencyProperty.Register(
                nameof(CanUserSortColumns),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(true, CanUserSortColumnsChanged));

        private static void CanUserSortColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.DataListView.CanUserSortColumns = (bool)e.NewValue;
            }
        }


        public float SKFontSize
        {
            get { return (float)GetValue(SKFontSizeProperty); }
            set { SetValue(SKFontSizeProperty, value); }
        }

        public static readonly DependencyProperty SKFontSizeProperty =
            DependencyProperty.Register(
                nameof(SKFontSize),
                typeof(float),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(12f, SKFontSizeChanged));

        private static void SKFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (e.NewValue != null)
                {
                    var size = (float)e.NewValue;
                    skGridView.RowHeight = (size + 4);
                    skGridView.DataListView.FontSize = size;
                    skGridView.SkiaRenderer.SetFontSize(size);
                    skGridView.SKGridColumnHeader.Height = new GridLength(skGridView.ColumnHeaderVisible ? (size + 10) : 0);
                }
            }
        }
        public string SKFontFamily
        {
            get { return (string)GetValue(SKFontFamilyProperty); }
            set { SetValue(SKFontFamilyProperty, value); }
        }

        public static readonly DependencyProperty SKFontFamilyProperty =
            DependencyProperty.Register(
                nameof(SKFontFamily),
                typeof(string),
                typeof(SkiaGridViewV2),
                new PropertyMetadata("Arial", SKFontFamilyChanged));

        private static void SKFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (e.NewValue != null)
                {
                    var fontFamily = (string)e.NewValue;
                    skGridView.SkiaRenderer.SetFontFamily(fontFamily);


                    skGridView.DataListView.FontFamily = new FontFamily(fontFamily);
                    //skGridView.DataListView.ColumnHeaderStyle = cellStyle;

                }
            }
        }
        public string SKFontStyle
        {
            get { return (string)GetValue(SKFontStyleProperty); }
            set { SetValue(SKFontStyleProperty, value); }
        }

        public static readonly DependencyProperty SKFontStyleProperty =
            DependencyProperty.Register(
                nameof(SKFontStyle),
                typeof(string),
                typeof(SkiaGridViewV2),
                new PropertyMetadata("Normal", SKFontStyleChanged));

        private static void SKFontStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (e.NewValue != null)
                {
                    var fontstyle = ((string)e.NewValue).ToLower();
                    skGridView.SkiaRenderer.SetFontStyle(fontstyle);

                    System.Windows.FontStyle wpfFontStyle = FontStyles.Normal;
                    if (fontstyle == "normal")
                        wpfFontStyle = FontStyles.Normal;
                    else if (fontstyle == "italic" || fontstyle.Contains("italic"))
                        wpfFontStyle = FontStyles.Italic;

                    skGridView.DataListView.FontStyle = wpfFontStyle;

                    FontWeight wpfFontWeight = FontWeights.Normal;
                    if (fontstyle.Contains("bold"))
                        wpfFontWeight = FontWeights.Bold;

                    skGridView.DataListView.FontWeight = wpfFontWeight;
                }
            }
        }


        public Action<string?, SkGridViewColumnSort?> SortChanged
        {
            get { return (Action<string?, SkGridViewColumnSort?>)GetValue(SortChangedProperty); }
            set { SetValue(SortChangedProperty, value); }
        }

        public static readonly DependencyProperty SortChangedProperty =
            DependencyProperty.Register(nameof(SortChanged), typeof(Action<string?, SkGridViewColumnSort?>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action SortChanging
        {
            get { return (Action)GetValue(SortChangingProperty); }
            set { SetValue(SortChangingProperty, value); }
        }

        public static readonly DependencyProperty SortChangingProperty =
            DependencyProperty.Register(nameof(SortChanging), typeof(Action), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public bool IsLiveSorting
        {
            get { return (bool)GetValue(IsLiveSortingProperty); }
            set { SetValue(IsLiveSortingProperty, value); }
        }

        public static readonly DependencyProperty IsLiveSortingProperty =
            DependencyProperty.Register(nameof(IsLiveSorting), typeof(bool), typeof(SkiaGridViewV2), new PropertyMetadata(true, IsLiveSortingChanged));

        private static void IsLiveSortingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            if (grid._collectionView != null)
            {
                grid._collectionView.IsLiveSort = (bool)e.NewValue;
                if ((bool)e.NewValue)
                {
                    grid._collectionView.Refresh();
                }
            }
        }

        public int? SortEvery
        {
            get { return (int?)GetValue(SortEveryProperty); }
            set { SetValue(SortEveryProperty, value); }
        }
        public static readonly DependencyProperty SortEveryProperty =
            DependencyProperty.Register(nameof(SortEvery), typeof(int?), typeof(SkiaGridViewV2), new PropertyMetadata(null, SortEveryChanged));

        private static void SortEveryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            var timerValue = e.NewValue as int?;
            if (timerValue.HasValue && timerValue.Value > 0)
            {
                grid.SortTimer.Stop();
                grid.SortTimer.Interval = TimeSpan.FromSeconds(timerValue.Value);
                grid.UpdateTimerbaseSortingColumnColor(TIMERBASED_SORTING_COLOR, grid.Columns.FirstOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None));
                grid.SortTimer.Start();
            }
            else
            {
                grid.SortTimer.Stop();
                grid.UpdateTimerbaseSortingColumnColor(NORMAL_GRID_COLUMN_COLOR, grid.Columns.FirstOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None));
            }
        }
        public bool FilterByGroup
        {
            get { return (bool)GetValue(FilterByGroupProperty); }
            set { SetValue(FilterByGroupProperty, value); }
        }
        public static readonly DependencyProperty FilterByGroupProperty =
            DependencyProperty.Register(nameof(FilterByGroup), typeof(bool), typeof(SkiaGridViewV2), new PropertyMetadata(false));

       

        private void UpdateTimerbaseSortingColumnColor(string color, SKGridViewColumn? column)
        {

            if (column != null)
            {
                var IsFilterApplied = _collectionView?.Filters.Any(x => x.Column == column.BindingPath);
                if (IsFilterApplied.HasValue && IsFilterApplied.Value && color == NORMAL_GRID_COLUMN_COLOR)
                {
                    color = FILTER_COLOR;
                }
                var gridCol = DataListView.Columns.Where(x => x.Header != null).FirstOrDefault(x => x.Header?.ToString() == column.Header || x.Header?.ToString() == column.DisplayHeader);

                if (gridCol != null)
                {
                    var newStyle = new Style(typeof(DataGridColumnHeader), gridCol.HeaderStyle);
                    newStyle.Setters.Add(new Setter(Control.BackgroundProperty, Helper.GetColorBrush(color)));
                    gridCol.HeaderStyle = newStyle;
                }
            }

        }

        public bool ColumnHeaderVisible
        {
            get { return (bool)GetValue(ColumnHeaderVisibleProperty); }
            set { SetValue(ColumnHeaderVisibleProperty, value); }
        }

        public static readonly DependencyProperty ColumnHeaderVisibleProperty =
            DependencyProperty.Register(
                nameof(ColumnHeaderVisible),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(true, OnColumnHeaderVisibleChanged));

        private static void OnColumnHeaderVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.SKGridColumnHeader.Height = new GridLength((bool)e.NewValue ? 21 : 0);
                skGridView.DataListView.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Hidden;
            }
        }
        public bool IsDeferredScrollingEnabled
        {
            get { return (bool)GetValue(IsDeferredScrollingEnabledProperty); }
            set { SetValue(IsDeferredScrollingEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsDeferredScrollingEnabledProperty =
            DependencyProperty.Register(
                nameof(IsDeferredScrollingEnabled),
                typeof(bool),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(false));



        public Action<SKGridViewColumn> ColumnRightClick
        {
            get { return (Action<SKGridViewColumn>)GetValue(ColumnRightClickProperty); }
            set { SetValue(ColumnRightClickProperty, value); }
        }

        public static readonly DependencyProperty ColumnRightClickProperty =
            DependencyProperty.Register(nameof(ColumnRightClick), typeof(Action<SKGridViewColumn>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action<SKGridViewColumn> ColumnLeftClick
        {
            get { return (Action<SKGridViewColumn>)GetValue(ColumnLeftClickProperty); }
            set { SetValue(ColumnLeftClickProperty, value); }
        }

        public static readonly DependencyProperty ColumnLeftClickProperty =
            DependencyProperty.Register(nameof(ColumnLeftClick), typeof(Action<SKGridViewColumn>), typeof(SkiaGridViewV2), new PropertyMetadata(default));
        public ContextMenu HeaderContextMenu
        {
            get { return (ContextMenu)GetValue(HeaderContextMenuProperty); }
            set { SetValue(HeaderContextMenuProperty, value); }
        }

        public static readonly DependencyProperty HeaderContextMenuProperty =
            DependencyProperty.Register(nameof(HeaderContextMenu), typeof(ContextMenu), typeof(SkiaGridViewV2), new PropertyMetadata(default, OnHeaderContextMenuChanged));

        private static void OnHeaderContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.DataListView.ContextMenu = e.NewValue as ContextMenu;
            }
        }


        public ContextMenu ContextMenu
        {
            get { return (ContextMenu)GetValue(ContextMenuProperty); }
            set { SetValue(ContextMenuProperty, value); }
        }

        public static readonly DependencyProperty ContextMenuProperty =
            DependencyProperty.Register(nameof(ContextMenu), typeof(ContextMenu), typeof(SkiaGridViewV2), new PropertyMetadata(default, OnContextMenuChanged));

        private static void OnContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.skiaContainer.ContextMenu = e.NewValue as ContextMenu;
            }
        }

        public ContextMenu ItemsContextMenu
        {
            get { return (ContextMenu)GetValue(ItemsContextMenuProperty); }
            set { SetValue(ItemsContextMenuProperty, value); }
        }

        public static readonly DependencyProperty ItemsContextMenuProperty =
            DependencyProperty.Register(nameof(ItemsContextMenu), typeof(ContextMenu), typeof(SkiaGridViewV2), new PropertyMetadata(default, OnItemsContextMenuChanged));

        private static void OnItemsContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                skGridView.SkiaCanvas.ContextMenu = e.NewValue as ContextMenu;
            }
        }
        public Action<double> HorizontalScrollBarPositionChanged
        {
            get { return (Action<double>)GetValue(HorizontalScrollBarPositionChangedProperty); }
            set { SetValue(HorizontalScrollBarPositionChangedProperty, value); }
        }

        public static readonly DependencyProperty HorizontalScrollBarPositionChangedProperty =
            DependencyProperty.Register(nameof(HorizontalScrollBarPositionChanged), typeof(Action<double>), typeof(SkiaGridViewV2), new PropertyMetadata(default));

        public Action<double> VerticalScrollBarPositionChanged
        {
            get { return (Action<double>)GetValue(VerticalScrollBarPositionChangedProperty); }
            set { SetValue(VerticalScrollBarPositionChangedProperty, value); }
        }

        public static readonly DependencyProperty VerticalScrollBarPositionChangedProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarPositionChanged), typeof(Action<double>), typeof(SkiaGridViewV2), new PropertyMetadata(default));


        public SKScrollBarVisibility HorizontalScrollBarVisible
        {
            get { return (SKScrollBarVisibility)GetValue(HorizontalScrollBarVisibleProperty); }
            set { SetValue(HorizontalScrollBarVisibleProperty, value); }
        }

        public static readonly DependencyProperty HorizontalScrollBarVisibleProperty =
            DependencyProperty.Register(
                nameof(HorizontalScrollBarVisible),
                typeof(SKScrollBarVisibility),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(SKScrollBarVisibility.Auto, OnHorizontalScrollBarVisibilityChanged));

        private static void OnHorizontalScrollBarVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (((SKScrollBarVisibility)e.NewValue) == SKScrollBarVisibility.Visible)
                    skGridView.HorizontalScrollViewer.Visibility = Visibility.Visible;
                else if (((SKScrollBarVisibility)e.NewValue) == SKScrollBarVisibility.Hidden)
                    skGridView.HorizontalScrollViewer.Visibility = Visibility.Collapsed;
            }
        }


        public SKScrollBarVisibility VerticalScrollBarVisible
        {
            get { return (SKScrollBarVisibility)GetValue(VerticalScrollBarVisibleProperty); }
            set { SetValue(VerticalScrollBarVisibleProperty, value); }
        }

        public static readonly DependencyProperty VerticalScrollBarVisibleProperty =
            DependencyProperty.Register(
                nameof(VerticalScrollBarVisible),
                typeof(SKScrollBarVisibility),
                typeof(SkiaGridViewV2),
                new PropertyMetadata(SKScrollBarVisibility.Auto, OnVerticalScrollBarVisibilityChanged));

        private static void OnVerticalScrollBarVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaGridViewV2 skGridView)
            {
                if (((SKScrollBarVisibility)e.NewValue) == SKScrollBarVisibility.Visible)
                    skGridView.VerticalScrollViewer.Visibility = Visibility.Visible;
                else if (((SKScrollBarVisibility)e.NewValue) == SKScrollBarVisibility.Hidden)
                    skGridView.VerticalScrollViewer.Visibility = Visibility.Collapsed;
            }
        }
        #endregion SelectedItems

        #region ColorProperties

        public string ForegroundColor
        {
            get { return (string)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register(nameof(ForegroundColor), typeof(string), typeof(SkiaGridViewV2), new PropertyMetadata(default, ForegroundColorChanged));

        private static void ForegroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            grid.SkiaRenderer.SetForeground((string)e.NewValue);
        }

        public string RowBackground
        {
            get { return (string)GetValue(RowBackgroundProperty); }
            set { SetValue(RowBackgroundProperty, value); }
        }

        public static readonly DependencyProperty RowBackgroundProperty =
            DependencyProperty.Register(nameof(RowBackground), typeof(string), typeof(SkiaGridViewV2), new PropertyMetadata(default, RowBackgroundChanged));

        private static void RowBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            grid.SkiaRenderer.SetRowBackgroundColor((string)e.NewValue);
        }

        public string? AlternatingRowBackground
        {
            get { return (string?)GetValue(AlternatingRowBackgroundProperty); }
            set { SetValue(AlternatingRowBackgroundProperty, value); }
        }

        public static readonly DependencyProperty AlternatingRowBackgroundProperty =
            DependencyProperty.Register(nameof(AlternatingRowBackground), typeof(string), typeof(SkiaGridViewV2), new PropertyMetadata(null, AlternatingRowBackgroundChanged));

        private static void AlternatingRowBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            grid.SkiaRenderer.SetAlternatingRowBackground((string)e.NewValue);
        }

        public bool ShowGridLines
        {
            get { return (bool)GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }

        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register(nameof(ShowGridLines), typeof(bool), typeof(SkiaGridViewV2), new PropertyMetadata(false, ShowgGridLineChanged));

        private static void ShowgGridLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            grid.SkiaRenderer.SetGridLinesVisibility((bool)e.NewValue);
        }

        public string GridLinesColor
        {
            get { return (string)GetValue(GridLinesColorProperty); }
            set { SetValue(GridLinesColorProperty, value); }
        }

        public static readonly DependencyProperty GridLinesColorProperty =
            DependencyProperty.Register(nameof(GridLinesColor), typeof(string), typeof(SkiaGridViewV2), new PropertyMetadata(default, GridLinesColorChanged));

        private static void GridLinesColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SkiaGridViewV2 grid)
                return;
            grid.SkiaRenderer.SetGridLinesColor((string)e.NewValue);
        }


        #endregion ColorProperties

        #region PrivateMethods

        private void SortTimerChanged(object? sender, EventArgs e)
        {
            if (_collectionView != null)
            {
                _collectionView.Refresh();
                SortChanged?.Invoke(null, null);
            }

        }

        private void ParentWindow_Deactivated(object? sender, EventArgs e)
        {
            SkiaRenderer.SetWindowActive(RetainSelectionOnLFocusLost ? true : false);

        }

        private void ParentWindow_Activated(object? sender, EventArgs e)
        {
            SkiaRenderer.SetWindowActive(true);
        }
        private void CollectionViewChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateGroupCollection();
        }

        private void UpdateColumnsInDataGrid()
        {
            if (Columns != null && Columns.Count > 0)
            {
                DataListView.Columns.Clear();
                //CreateGroupColumns();
                var sortColumns = Columns.Where(x => x?.GridViewColumnSort != null && x?.GridViewColumnSort != SkGridViewColumnSort.None).LastOrDefault();
                foreach (var column in Columns.OrderBy(x => x.DisplayIndex))
                {
                    var headerStyle = new Style(typeof(DataGridColumnHeader));

                    headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, column.ContentAlignment == CellContentAlignment.Right ? HorizontalAlignment.Right :
                                                                                         column.ContentAlignment == CellContentAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Center));

                    headerStyle.Setters.Add(new Setter(BackgroundProperty, Helper.GetColorBrush(column.BackColor ?? NORMAL_GRID_COLUMN_COLOR)));


                    headerStyle.BasedOn = this.Resources["ColumnHeaderStyle"] as Style;

                    var dgColumn = new DataGridTextColumn
                    {
                        Header = column.DisplayHeader ?? column.Header,
                        Width = column.Width,
                        Visibility = !column.IsVisible ? Visibility.Collapsed : Visibility.Visible,
                        HeaderStyle = headerStyle,
                        MinWidth = 40,

                    };

                    if (column.CanUserResize.HasValue)
                        dgColumn.CanUserResize = column.CanUserResize.Value;
                    if (column.CanUserReorder.HasValue)
                        dgColumn.CanUserReorder = column.CanUserReorder.Value;
                    if (column.CanUserSort.HasValue)
                        dgColumn.CanUserSort = column.CanUserSort.Value;
                    if (column.DisplayIndex != null)
                        dgColumn.DisplayIndex = column.DisplayIndex.Value;
                    DataListView.Columns.Add(dgColumn);
                }
                foreach (var item in DataListView.Columns)
                {
                    var col1 = Columns.FirstOrDefault(x => x.Header == item.Header);
                    if (col1 != null)
                    {
                        UnSubscribeColumnEvent(col1);
                        col1.DisplayIndex = item.DisplayIndex;
                        SubscribeColumnEvent(col1);
                    }
                }

                if (sortColumns != null)
                {
                    DataListView.Columns!.Where(x => x.Header as string == (sortColumns.DisplayHeader ?? sortColumns.Header)).FirstOrDefault()!
                                                    .SortDirection = sortColumns.GridViewColumnSort == SkGridViewColumnSort.Ascending ? ListSortDirection.Ascending
                                                                    : (sortColumns.GridViewColumnSort == SkGridViewColumnSort.Descending ? ListSortDirection.Descending : null);
                    ApplySort(sortColumns.BindingPath, sortColumns.GridViewColumnSort == SkGridViewColumnSort.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
                    if (SortEvery.HasValue && SortEvery.Value > 0)
                    {
                        UpdateTimerbaseSortingColumnColor(TIMERBASED_SORTING_COLOR, Columns.FirstOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None));
                    }
                }

                SubscribeToColumnEvents(Columns);
                MonitorColumnResize(DataListView);
            }
        }

        private void MonitorColumnResize(DataGrid dataGrid)
        {
            try
            {
                foreach (var column in dataGrid.Columns)
                {
                    var descriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

                    descriptor?.RemoveValueChanged(column, OnColumnWidthChanged!);
                    descriptor?.AddValueChanged(column, OnColumnWidthChanged!);
                }
            }
            catch (Exception)
            {
            }

        }

        private void MonitorColumnResizeRemove(DataGridColumn? column)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
            descriptor?.RemoveValueChanged(column, OnColumnWidthChanged!);
        }

        private void MonitorColumnResizeAdd(DataGridColumn? column)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
            descriptor?.AddValueChanged(column, OnColumnWidthChanged!);
        }

        private void OnColumnWidthChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is DataGridTextColumn column)
                {
                    if (column != null)
                    {
                        var colname = (column?.Header ?? "").ToString();
                        var col = Columns.FirstOrDefault(x => x.Header == colname || x.DisplayHeader == colname);
                        if (col != null)
                        {
                            UnSubscribeColumnEvent(col);
                            col.Width = column!.ActualWidth;
                            UpdateSkiaGrid();
                            UpdateScrollValues();
                            Refresh();
                            SkiaRenderer.UpdateVisibleColumns();
                            SubscribeColumnEvent(col);
                        }
                    }

                }
            }
            catch (Exception)
            {

            }

        }

        private void SubscribeToColumnEvents(IEnumerable<SKGridViewColumn> columns)
        {
            foreach (var col in columns)
            {
                UnSubscribeColumnEvent(col);
                SubscribeColumnEvent(col);
            }
        }

        private void UnSubscribeColumnEvent(SKGridViewColumn col)
        {
            col.PropertyChanged -= Column_PropertyChanged;
        }

        private void SubscribeColumnEvent(SKGridViewColumn col)
        {
            col.PropertyChanged += Column_PropertyChanged;
        }

        private void Group_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SKGroupDefinition column)
            {
                if (e.PropertyName == nameof(SKGroupDefinition.RowBackground))
                {
                    SkiaRenderer.SetGroupRowBackgroundColor(column.RowBackground);
                }
                if (e.PropertyName == nameof(SKGroupDefinition.ForegroundColor))
                {
                    SkiaRenderer.SetGroupFontColor(column.ForegroundColor);
                }
            }
        }
        private void SubscribeToGroupColumnEvents(SKGroupDefinition? group)
        {
            if (group != null)
            {
                group.PropertyChanged -= Group_PropertyChanged;
                group.PropertyChanged += Group_PropertyChanged;
            }

        }

        private void Column_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == nameof(SKGridViewColumn.Width))
            //    return;
            if (sender is SKGridViewColumn column)
            {
                DataGridColumn? col = DataListView.Columns.FirstOrDefault(x => x.Header?.ToString() == column?.Header?.ToString() || (column?.DisplayHeader != null && x.Header?.ToString() == column?.DisplayHeader?.ToString()));
                if (col != null)
                {
                    if (e.PropertyName == nameof(SKGridViewColumn.IsVisible))
                    {
                        col.Visibility = !column.IsVisible ? Visibility.Collapsed : Visibility.Visible;
                        UpdateScrollValues();
                        UpdateSkiaGrid();
                    }
                    if (e.PropertyName == nameof(SKGridViewColumn.CanUserReorder))
                        col.CanUserResize = column!.CanUserResize!.Value;
                    if (e.PropertyName == nameof(SKGridViewColumn.CanUserResize))
                        col.CanUserReorder = column!.CanUserReorder!.Value;
                    if (e.PropertyName == nameof(SKGridViewColumn.CanUserSort))
                        col.CanUserSort = column!.CanUserSort!.Value;
                    if (e.PropertyName == nameof(SKGridViewColumn.Width))
                    {
                        //if (IsBusy) return;
                        IsBusy = true;
                        MonitorColumnResizeRemove(col);
                        col.Width = new DataGridLength(column.Width);
                        UpdateScrollValues();
                        UpdateSkiaGrid();
                        MonitorColumnResizeAdd(col);
                        IsBusy = false;
                        Refresh();
                    }
                    if (e.PropertyName == nameof(SKGridViewColumn.GridViewColumnSort))
                    {
                        if (column.GridViewColumnSort != SkGridViewColumnSort.None)
                        {
                            ApplySort(column.BindingPath,
                                        column.GridViewColumnSort == SkGridViewColumnSort.Ascending
                                        ? ListSortDirection.Ascending : ListSortDirection.Descending);

                            //foreach (var item in DataListView.Columns)
                            //{
                            //    item.SortDirection = ListSortDirection.Descending;
                            //} 
                            col.SortDirection = column.GridViewColumnSort == SkGridViewColumnSort.Ascending
                                ? ListSortDirection.Ascending : ListSortDirection.Descending;

                            ResetGroupToggleValues();
                        }
                    }
                    if (e.PropertyName == nameof(SKGridViewColumn.BackColor))
                    {
                        var newStyle = new Style(typeof(DataGridColumnHeader), col.HeaderStyle);
                        newStyle.Setters.Add(new Setter(Control.BackgroundProperty, Helper.GetColorBrush(column.BackColor ?? NORMAL_GRID_COLUMN_COLOR)));
                        col.HeaderStyle = newStyle;
                    }
                    if (e.PropertyName == nameof(SKGridViewColumn.DisplayIndex))
                    {
                        DataListView.ColumnReordered -= DataListView_ColumnReordered!;
                        if (column.DisplayIndex.HasValue) col.DisplayIndex = column.DisplayIndex.Value; else { column.DisplayIndex = col.DisplayIndex; }
                        DataListView.ColumnReordered += DataListView_ColumnReordered!;

                    }
                    SkiaRenderer.UpdateVisibleColumns();
                }

            }
        }

        private double GetSkiaHeight()
        {
            if (skiaContainer.ActualHeight < (TotalRows * RowHeight))
            {
                return skiaContainer.ActualHeight;
            }
            return (TotalRows * RowHeight);
        }

        private double GetSkiaWidth()
        {
            var totalcolumnwidth = GetVisibleColumnsWidth();
            if (skiaContainer.ActualWidth > totalcolumnwidth)
            {
                return totalcolumnwidth;
            }
            return skiaContainer.ActualWidth;
        }

        private void UpdateSkiaGrid()
        {
            SkiaCanvas.Height = GetSkiaHeight();
            SkiaCanvas.Width = GetSkiaWidth();

        }

        private double GetVisibleColumnsWidth() => DataListView.Columns.Where(x => x.Visibility == Visibility.Visible).Sum(x => x.Width.Value);

        private void MarkDirty() => _isDirty = true;

        private void UpdateScrollValues()
        {

            VerticalScrollViewer.Minimum = 0;
            VerticalScrollViewer.ViewportSize = MainGrid.ActualHeight;
            VerticalScrollViewer.Maximum = ((TotalRows + 3.3) * RowHeight) - MainGrid.ActualHeight;

            ManageVerticalScrollBar();

            var IsVisible = VerticalScrollViewer.Visibility == Visibility.Visible ? 10 : 0;
            HorizontalScrollViewer.Minimum = 0;
            HorizontalScrollViewer.ViewportSize = MainGrid.ActualWidth;
            HorizontalScrollViewer.Maximum = (GetVisibleColumnsWidth() + IsVisible) - MainGrid.ActualWidth;

            ManageHorizontalScrollBar();

        }

        private void ManageVerticalScrollBar()
        {
            if (VerticalScrollBarVisible == SKScrollBarVisibility.Auto)
            {
                if (((TotalRows + 3.3) * RowHeight) - MainGrid.ActualHeight > 0)
                {
                    VerticalScrollViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    VerticalScrollViewer.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void ManageHorizontalScrollBar()
        {
            if (HorizontalScrollBarVisible == SKScrollBarVisibility.Auto)
            {
                if ((GetVisibleColumnsWidth() - MainGrid.ActualWidth) > 0)
                {
                    HorizontalScrollViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    HorizontalScrollViewer.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SetScale()
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                var res = source.CompositionTarget.TransformToDevice.M11;
                Scale = (float)res;
            }
            else
            {
                Scale = Helper.GetSystemDpi();
            }
        }

        private void ApplySort(string propertyName, ListSortDirection direction)
        {
            _collectionView?.ClearSortDescriptions();
            _collectionView?.AddSort(propertyName, direction);
            ResetGroupToggleValues();

        }

        private void UpdateGroupCollection()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(UpdateGroupCollection); // Recursive call on UI thread
                return;
            }

            if (GroupSettings != null)
            {
                SkiaRenderer.GroupItemSource = FlattenGroupedItems(_collectionView!, GroupToggleDetails);
            }

            UpdateTotalRows();

        }

        private void UpdateTotalRows()
        {
            if (GroupSettings != null)
            {
                TotalRows = SkiaRenderer.GroupItemSource.Where(x => x.IsExpanded == true || x.IsGroupHeader).Count();
            }
            else
            {
                TotalRows = _collectionView?.Count ?? 0;
            }
            UpdateSkiaGrid();
            UpdateScrollValues();
        }

        private void ResetGroupToggleValues()
        {
            if (GroupSettings == null || _collectionView == null || _collectionView.GroupList == null)
                return;
            foreach (var group in _collectionView.GroupList)
            {
                var groupName = group.Key?.ToString();
                if (groupName != null && GroupToggleDetails.ContainsKey(groupName))
                {
                    var res = GroupToggleDetails[groupName];
                    res.x = 0;
                    res.y = 0;
                    res.height = 0;
                    res.width = 0;
                    GroupToggleDetails[groupName] = res;
                }
            }
        }

        internal List<GroupModel> FlattenGroupedItems(ICustomCollectionView collectionView, Dictionary<string, (bool IsExpended, float x, float y, float height, float width)> groupToggelDetails)
        {
            var result = new List<GroupModel>();

            if (collectionView == null || collectionView.GroupList == null)
                return result;

            foreach (var group in collectionView.GroupList)
            {
                var isExpended = true;
                var groupName = group.Key?.ToString();

                if (groupName != null)
                {
                    if (groupToggelDetails.ContainsKey(groupName))
                        isExpended = groupToggelDetails[groupName].IsExpended;
                    else
                    {
                        groupToggelDetails.Add(groupName, (true, 0, 0, 0, 0));
                    }
                }

                result.Add(new GroupModel
                {
                    IsGroupHeader = true,
                    GroupName = groupName ?? "",
                    Item = new { Name = groupName },
                    IsExpanded = isExpended
                });

                foreach (var obj in group.Items)
                {
                    result.Add(new GroupModel
                    {
                        IsGroupHeader = false,
                        GroupName = groupName ?? "",
                        Item = obj,
                        IsExpanded = isExpended
                    });
                }
            }
            return result;
        }

        private void UpdateGroupToggle(double x, double y)
        {
            if (GroupSettings != null && TotalRows > 0)
            {
                var values = GroupToggleDetails.Where(v => (x >= v.Value.x && x <= v.Value.x + v.Value.width) && (y >= v.Value.y && y <= v.Value.y + v.Value.height)).LastOrDefault();
                ResetGroupToggleValues();
                if (values.Key != null)
                {
                    var res = GroupToggleDetails[values.Key];
                    res.IsExpended = !res.IsExpended;
                    GroupToggleDetails[values.Key] = res;
                    foreach (var item in SkiaRenderer.GroupItemSource.Where(x => x.GroupName == values.Key))
                    {
                        item.IsExpanded = res.IsExpended;
                    }
                    UpdateTotalRows();
                    Refresh();
                }
            }
        }

        private void InvokeHeaderClickWithDelay()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InvokeDataGridHeaderClick();
            }), DispatcherPriority.Loaded);
        }

        private void InvokeDataGridHeaderClick()
        {
            var headers = FindVisualChildren<DataGridColumnHeader>(DataListView);
            foreach (var header in headers)
            {
                header.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ColumnHeader_LeftClick));
                header.RemoveHandler(UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(ColumnHeader_RightClick));

                header.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ColumnHeader_LeftClick), true);
                header.AddHandler(UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(ColumnHeader_RightClick), true);
            }
        }

        private void ColumnHeader_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridColumnHeader header)
            {
                var col = Columns?.FirstOrDefault(x => x.Header == header.Content?.ToString() || x.DisplayHeader == header.Content?.ToString());
                ColumnLeftClick?.Invoke(col!);
            }
        }

        private void ColumnHeader_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridColumnHeader header)
            {
                var col = Columns.FirstOrDefault(x => x.Header == header.Content?.ToString() || x.DisplayHeader == header.Content?.ToString());
                ColumnRightClick?.Invoke(col!);
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion PrivateMethods

        #region Template
        public SKCellTemplate? CellTemplate
        {
            get => (SKCellTemplate?)GetValue(CellTemplateProperty);
            set => SetValue(CellTemplateProperty, value);
        }
        public static readonly DependencyProperty CellTemplateProperty =
            DependencyProperty.Register(nameof(CellTemplate), typeof(SKCellTemplate), typeof(SkiaGridViewV2), new PropertyMetadata(null));

        public SKRowTemplate? RowTemplate
        {
            get => (SKRowTemplate?)GetValue(RowTemplateProperty);
            set => SetValue(RowTemplateProperty, value);
        }
        public static readonly DependencyProperty RowTemplateProperty =
            DependencyProperty.Register(nameof(RowTemplate), typeof(SKRowTemplate), typeof(SkiaGridViewV2), new PropertyMetadata(null));

        #endregion Template


       
        private void MainGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                HorizontalScrollViewer.Value -= e.Delta / 1;
                UpdateHorizontalScroll();
            }
            else
            {
                VerticalScrollViewer.Value -= e.Delta / 1;
                UpdateVerticalScroll();
            }
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SkiaCanvas.Focus();
        }

        private float scrollOffsetX { get; set; } = 0;

        private void HorizontalScrollViewer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scrollOffsetX = (float)(e.NewValue);


        }
        private void HorizontalScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsDeferredScrollingEnabled)
                UpdateHorizontalScroll();
        }
        private void HorizontalScrollViewer_Scroll(object sender, ScrollEventArgs e)
        {
            if (!IsDeferredScrollingEnabled)
                UpdateHorizontalScroll();
        }
        private void UpdateHorizontalScroll()
        {
            if (IsBusy) return;
            IsBusy = true;
            ScrollOffsetX = scrollOffsetX;
            DataListViewScroll?.ScrollToHorizontalOffset(ScrollOffsetX);
            HorizontalScrollBarPositionChanged?.Invoke(scrollOffsetX);
            Refresh();
            IsBusy = false;
        }

        private float scrollOffsetY { get; set; } = 0;
        private void VerticalScrollViewer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scrollOffsetY = (float)(e.NewValue);

        }

        private void VerticalScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsDeferredScrollingEnabled)
                UpdateVerticalScroll();
        }

        private void VerticalScrollViewer_Scroll(object sender, ScrollEventArgs e)
        {
            if (!IsDeferredScrollingEnabled)
                UpdateVerticalScroll();
        }

        private void UpdateVerticalScroll()
        {
            if (IsBusy) return;
            IsBusy = true;
            ScrollOffsetY = scrollOffsetY;
            VerticalScrollBarPositionChanged?.Invoke(scrollOffsetY);
            Refresh();
            IsBusy = false;
        }

        private void SkiaCanvas_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            //if (IsBusy) return;

            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();
            canvas.Scale(Scale);
            canvas.Save();
            canvas.Translate(-ScrollOffsetX, -ScrollOffsetY);
            Draw(canvas);
            canvas.Restore();
        }

        private void Draw(SKCanvas canvas)
        {

            try
            {
                SkiaRenderer.Draw(canvas, ScrollOffsetX, ScrollOffsetY, RowHeight, TotalRows);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void SkiaCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_collectionView == null || SkiaRenderer == null || SkiaRenderer.GroupItemSource == null)
                return;

            var point = e.GetPosition(SkiaCanvas);

            int rowIndex = (int)((point.Y + ScrollOffsetY) / RowHeight);
            double x = point.X + ScrollOffsetX;
            int clickedColumnIndex = (int)((point.X + ScrollOffsetX));

            var s = GroupSettings == null
                ? _collectionView.Cast<object>().ToList()
                : SkiaRenderer.GroupItemSource.Where(x => x.IsExpanded || x.IsGroupHeader)
                                              .Select(x => x.Item)
                                              .ToList();

            if (rowIndex > s.Count - 1)
                return;

            foreach (var item in DataListView.Columns.OrderBy(x => x.DisplayIndex)
                                                     .Where(x => x.Visibility == Visibility.Visible))
            {
                x -= item.Width.Value;
                if (x <= 0)
                {
                    if (Columns != null)
                        OnCellClicked?.Invoke(s[rowIndex], Columns?.FirstOrDefault(x => x.Header == item?.Header?.ToString() || x.DisplayHeader == item?.Header?.ToString()));
                    break;
                }
            }

            if (e.RightButton == MouseButtonState.Pressed && CanUserSelectRows)
            {
                if (SelectedItems == null)
                {
                    SelectedItems = new();
                    SkiaRenderer.UpdateSelectedItems(SelectedItems);
                }

                if (!SelectedItems.Contains(s[rowIndex]))
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(s[rowIndex]);
                    lastSelectedRowIndex = rowIndex;
                }
            }

            OnRowRightClicked?.Invoke(s[rowIndex]);

            SkiaCanvas.InvalidateVisual();
            SkiaCanvas.Focus();
        }

        private void SkiaCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_collectionView == null || SkiaRenderer == null || SkiaRenderer.GroupItemSource == null)
                return;
            var point = e.GetPosition(SkiaCanvas);
            int rowIndex = (int)((point.Y + ScrollOffsetY) / RowHeight);
            double x = point.X + ScrollOffsetX;
            int clickedColumnIndex = (int)((point.X + ScrollOffsetX));
            UpdateGroupToggle(x, (point.Y + ScrollOffsetY));


            var itemsource = GroupSettings == null ? _collectionView?.Cast<object>().ToList() : SkiaRenderer.GroupItemSource.Where(x => x.IsExpanded || x.IsGroupHeader).Select(x => x.Item).ToList();
            if (itemsource?.Count() == 0)
                return;

            List<dynamic> s = itemsource!;

            if (SelectedItems == null)
            {
                SelectedItems = new();
                SkiaRenderer.UpdateSelectedItems(SelectedItems);
            }


            if (rowIndex > s.Count() - 1)
                return;

            foreach (var item in DataListView.Columns.OrderBy(x => x.DisplayIndex).Where(x => x.Visibility == Visibility.Visible))
            {
                x -= item.Width.Value;
                if (x <= 0)
                {
                    OnCellClicked?.Invoke(s[rowIndex], Columns?.FirstOrDefault(x => x.Header == item?.Header?.ToString() || x.DisplayHeader == item?.Header?.ToString()));
                    break;
                }
            }
            if (e.LeftButton == MouseButtonState.Pressed && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && CanUserSelectRows)
            {
                if (SelectedItems.Any(x => x.Equals(s[rowIndex])))
                {
                    SelectedItems.Remove(s[rowIndex]);

                }
                else
                {
                    SelectedItems.Add(s[rowIndex]);
                    lastSelectedRowIndex = rowIndex;
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && CanUserSelectRows)
            {
                if (SelectedItems != null && SelectedItems.Count > 0)
                {
                    var seletedRow = lastSelectedRowIndex;

                    var lastIndex = s.IndexOf(s[rowIndex]);

                    if (lastIndex < seletedRow)
                    {
                        SelectedItems.Clear();
                        for (int i = lastIndex; i <= seletedRow; i++)
                        {
                            SelectedItems.Add(s[i]);
                        }
                    }
                    else
                    {
                        SelectedItems.Clear();
                        for (int i = seletedRow; i <= lastIndex; i++)
                        {
                            SelectedItems.Add(s[i]);
                        }
                    }
                }

            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (CanUserSelectRows)
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(s[rowIndex]);
                    lastSelectedRowIndex = rowIndex;


                }
                OnRowClicked?.Invoke(s[rowIndex]);
                TriggerButtonClick(point.X + ScrollOffsetX, point.Y + ScrollOffsetY, rowIndex, itemsource);

            }

            if (e.ClickCount == 2)
                OnRowDoubleClicked?.Invoke(s[rowIndex]);
            SkiaCanvas.InvalidateVisual();
            SkiaCanvas.Focus();
        }


        private void TriggerButtonClick(double x, double y, int rowIndex, List<dynamic> itemsource)
        {
            var values = ButtonDetails.Where(v => (x >= v.Value.x && x <= v.Value.x + v.Value.width) && (y >= v.Value.y && y <= v.Value.y + v.Value.height)).LastOrDefault();
            if (values.Value.btn != null)
            {
                values.Value.btn.OnClicked?.Invoke(values.Value.btn, itemsource[rowIndex]);
            }
        }

        private void SkiaCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (SkiaRenderer.GroupItemSource == null && _collectionView == null)
                return;

            var items = GroupSettings == null ? _collectionView.Cast<object>().ToList() : SkiaRenderer.GroupItemSource.Where(x => x.IsExpanded || x.IsGroupHeader).Select(x => x.Item).ToList();
            if (items.Count == 0)
                return;

            if (SelectedItems == null || SelectedItems.Count == 0)
            {
                return;
            }
            bool isShift = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
            bool isCtrl = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));

            if (SelectedItems.Count == 1)
            {
                _selectionAnchorIndex = lastSelectedRowIndex;
            }

            int currentIndex = items.IndexOf(SelectedItems.Last());

            if (!isShift)
                _selectionAnchorIndex = null;

            int firstVisibleRow = Math.Max(0, (int)(ScrollOffsetY / RowHeight));
            int visibleRowCount = Math.Min((int?)(VerticalScrollViewer?.ViewportSize / RowHeight - 3) ?? 0, TotalRows - firstVisibleRow);
            // Shift + Up
            if (isShift && e.Key == Key.Up)
            {
                if (currentIndex == 0)
                {
                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    return;
                }

                if (_selectionAnchorIndex == null)
                    _selectionAnchorIndex = currentIndex;

                if (currentIndex > 0)
                {
                    var newIndex = currentIndex - 1;
                    var newItem = items[newIndex];

                    if (newIndex < _selectionAnchorIndex)
                        SelectedItems.Add(newItem);
                    else
                        SelectedItems.Remove(items[currentIndex]);

                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    if (currentIndex <= firstVisibleRow)
                    {
                        ScrollToVerticalOffset(ScrollOffsetY - RowHeight);
                    }
                    SkiaCanvas.InvalidateVisual();
                }
            }
            // Shift + Down
            else if (isShift && e.Key == Key.Down)
            {
                if (currentIndex == items.Count - 1)
                {
                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    return;
                }
                if (_selectionAnchorIndex == null)
                    _selectionAnchorIndex = currentIndex;

                if (currentIndex < items.Count - 1)
                {
                    var newIndex = currentIndex + 1;
                    var newItem = items[newIndex];

                    if (newIndex > _selectionAnchorIndex)
                        SelectedItems.Add(newItem);
                    else
                        SelectedItems.Remove(items[currentIndex]);

                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    if (currentIndex >= firstVisibleRow + visibleRowCount)
                    {
                        ScrollToVerticalOffset(ScrollOffsetY + RowHeight);
                    }
                    SkiaCanvas.InvalidateVisual();
                }
            }
            // Normal Up (no shift) – move single selection
            else if (e.Key == Key.Up)
            {
                if (currentIndex == 0)
                {
                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    return;
                }
                if (currentIndex > 0)
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(items[currentIndex - 1]);
                    _selectionAnchorIndex = currentIndex - 1;

                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    if (currentIndex <= firstVisibleRow)
                    {
                        ScrollToVerticalOffset(ScrollOffsetY - RowHeight);
                    }
                    SkiaCanvas.InvalidateVisual();
                }
            }
            // Normal Down (no shift)
            else if (e.Key == Key.Down)
            {
                if (currentIndex == items.Count - 1)
                {
                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    return;
                }
                if (currentIndex < items.Count - 1)
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(items[currentIndex + 1]);
                    _selectionAnchorIndex = currentIndex + 1;

                    e.Handled = true;
                    Keyboard.Focus(SkiaCanvas);
                    if (currentIndex >= firstVisibleRow + visibleRowCount)
                    {
                        ScrollToVerticalOffset(ScrollOffsetY + RowHeight);
                    }
                    SkiaCanvas.InvalidateVisual();
                }
            }
            else if (isCtrl && e.Key == Key.A)
            {
                SelectAllRows();
            }
            else if (isCtrl && e.Key == Key.C)
            {
                Clipboard.SetText(ExportData(SKExportType.Selected));
            }

            OnPreviewKeyDownEvent?.Invoke(e.Key);
        }

        public void SelectAllRows()
        {
            try
            {
                if (SkiaRenderer.GroupItemSource == null && _collectionView == null)
                    return;

                var items = GroupSettings == null
                    ? _collectionView.Cast<object>().ToList()
                    : SkiaRenderer.GroupItemSource.Where(x => x.IsExpanded || x.IsGroupHeader)
                                                  .Select(x => x.Item)
                                                  .ToList();

                if (items.Count == 0)
                    return;

                if (SelectedItems == null)
                {
                    SelectedItems = new();
                    SkiaRenderer.UpdateSelectedItems(SelectedItems);
                }

                SelectedItems.Clear();
                foreach (var item in items)
                    SelectedItems.Add(item);

                lastSelectedRowIndex = items.Count - 1;
                _selectionAnchorIndex = 0;

                SkiaCanvas.InvalidateVisual();
            }
            catch
            {

            }
        }

        private void DataListView_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = false;
            SortChanging?.Invoke();

            var column = e.Column;
            var direction = column.SortDirection != ListSortDirection.Ascending
                ? SkGridViewColumnSort.Ascending
                : SkGridViewColumnSort.Descending;

            column.SortDirection = column.SortDirection != ListSortDirection.Ascending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            if (SortEvery.HasValue && SortEvery.Value != 0)
            {
                var gridCol = Columns.FirstOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None);

                UpdateTimerbaseSortingColumnColor(NORMAL_GRID_COLUMN_COLOR, gridCol);
            }


            foreach (var item in Columns.Where(x => x.GridViewColumnSort != SkGridViewColumnSort.None))
            {
                item.GridViewColumnSort = SkGridViewColumnSort.None;
            }
            var col = Columns.FirstOrDefault(x => x.Header == column?.Header.ToString() || x.DisplayHeader == column?.Header.ToString());
            col!.GridViewColumnSort = direction;
            ApplySort(col.BindingPath,
                      col.GridViewColumnSort == SkGridViewColumnSort.Ascending
                      ? ListSortDirection.Ascending
                      : ListSortDirection.Descending);
            if (SortEvery.HasValue && SortEvery.Value != 0)
            {
                UpdateTimerbaseSortingColumnColor(TIMERBASED_SORTING_COLOR, col);
            }
            SortChanged?.Invoke(col.Header, direction);
        }

        private void MainGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    if (SelectedItems != null && SelectedItems?.Count > 0 && CanUserSelectRows)
            //    {
            //        //SelectedItems?.Clear();
            //        //SkiaCanvas.InvalidateVisual();
            //    }
            //}
            //catch { }
        }

        private void skiaContainer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not SkiaSharp.Views.WPF.SKElement)
            {
                if (SelectedItems?.Count > 0 && !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && CanUserSelectRows)
                {
                    SelectedItems.Clear();
                    SkiaCanvas.InvalidateVisual();
                }
                if (e.ClickCount == 2)
                {
                    OnSkGridDoubleClicked?.Invoke();
                }
            }
        }

        private void DataListView_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            if (sender is DataGrid items)
            {
                var columns = new List<SKGridViewColumn>();

                foreach (var item in items.Columns.OrderBy(c => c.DisplayIndex))
                {
                    var existingItem = Columns.FirstOrDefault(x => x.Header == item.Header?.ToString() || x.DisplayHeader == item.Header?.ToString());
                    UnSubscribeColumnEvent(existingItem!);
                    existingItem.DisplayIndex = item.DisplayIndex;

                    SubscribeColumnEvent(existingItem!);
                }
                UpdateSkiaGrid();
                UpdateScrollValues();
                SkiaRenderer.UpdateVisibleColumns();
                Refresh();
            }
        }

        private bool _isRefreshing = false;

        public void Refresh()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;
            SkiaCanvas.InvalidateVisual();
            _isRefreshing = false;
        }

        public string ExportData(SKExportType exportType)
        {
            return SkiaRenderer.ExportData(exportType);
        }

        public void ScrollToVerticalOffset(double offset)
        {
            VerticalScrollViewer.Value = offset;
            UpdateVerticalScroll();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private bool isLayoutUpdating = false;
        private void DataListView_LayoutUpdated(object sender, EventArgs e)
        {
            if (!isLayoutUpdating)
            {
                isLayoutUpdating = true;
                InvokeDataGridHeaderClick();
                isLayoutUpdating = false;
            }
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                SortTimer.Tick -= SortTimerChanged;
                SkiaRenderer?.Dispose();
                SkiaCanvas.PaintSurface -= SkiaCanvas_PaintSurface;
                SkiaCanvas.MouseLeftButtonDown -= SkiaCanvas_MouseLeftButtonDown;
                SkiaCanvas.MouseRightButtonDown -= SkiaCanvas_MouseRightButtonDown;
                SkiaCanvas.KeyDown -= SkiaCanvas_KeyDown;
                DataListView.Sorting -= DataListView_Sorting;
                DataListView.ColumnReordered -= DataListView_ColumnReordered!;
                
                HorizontalScrollViewer.ValueChanged -= HorizontalScrollViewer_ValueChanged;
                VerticalScrollViewer.ValueChanged -= VerticalScrollViewer_ValueChanged;
                _collectionView.CollectionChanged -= CollectionViewChanged;
                _collectionView.Dispose(); 
            }

        }

        public void AddOrUpdateFilter(Filter filter)
        {
            if (_collectionView != null)
            {
                if (_collectionView.AddOrUpdateFilter(filter))
                {
                    var col = Columns.FirstOrDefault(x => x.BindingPath == filter.Column);
                    if (col != null)
                    {
                        var gridCol = DataListView.Columns.Where(x => x.Header != null).FirstOrDefault(x => x.Header?.ToString() == col.Header || x.Header?.ToString() == col.DisplayHeader);
                        col.DisplayHeader = string.Format("{0} {1}", col.Header, filter.ToString());
                        gridCol!.Header = string.Format("{0} {1}", col.Header, filter.ToString());

                        var newStyle = new Style(typeof(DataGridColumnHeader), gridCol.HeaderStyle);
                        newStyle.Setters.Add(new Setter(Control.BackgroundProperty, Helper.GetColorBrush(FILTER_COLOR)));
                        gridCol.HeaderStyle = newStyle;
                    }
                    if (SortEvery.HasValue && SortEvery.Value > 0)
                        UpdateTimerbaseSortingColumnColor(TIMERBASED_SORTING_COLOR, Columns.FirstOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None));
                    ScrollToVerticalOffset(0);
                }

            }
        }

        public void RemoveFilter(Filter filter)
        {
            if (_collectionView != null)
            {
                if (_collectionView.RemoveFilter(filter))
                {
                    var col = Columns.FirstOrDefault(x => x.BindingPath == filter.Column);
                    if (col != null)
                    {
                        var gridCol = DataListView.Columns.Where(x => x.Header != null).FirstOrDefault(x => x.Header?.ToString() == col.Header || x.Header?.ToString() == col.DisplayHeader);
                        col.DisplayHeader = null;
                        gridCol!.Header = col.Header;

                        var newStyle = new Style(typeof(DataGridColumnHeader), gridCol.HeaderStyle);
                        newStyle.Setters.Add(new Setter(Control.BackgroundProperty, Helper.GetColorBrush(NORMAL_GRID_COLUMN_COLOR)));
                        gridCol.HeaderStyle = newStyle;
                    }
                    if (SortEvery.HasValue && SortEvery.Value > 0)
                        UpdateTimerbaseSortingColumnColor(TIMERBASED_SORTING_COLOR, Columns.FirstOrDefault(x => x.GridViewColumnSort != SkGridViewColumnSort.None));
                }
            }
        }

        public void ScrollToHorizontalOffset(double offset)
        {
            HorizontalScrollViewer.Value = offset;
            UpdateHorizontalScroll();
        }

        private void DataListView_ColumnReordering(object sender, DataGridColumnReorderingEventArgs e)
        {
            ColumnReordering?.Invoke(this, e);
        }
        public event EventHandler<DataGridColumnReorderingEventArgs> ColumnReordering;
        public void MoveRowUp()
        {
            if (SelectedItems != null && SelectedItems.Count == 1 && _collectionView != null)
            {
                _collectionView.MoveRowUp(SelectedItems!.FirstOrDefault()!);
            }
        }
        public void MoveRowDown()
        {
            if (SelectedItems != null && SelectedItems.Count == 1 && _collectionView != null)
            {
                _collectionView.MoveRowDown(SelectedItems!.FirstOrDefault()!);
            }
        }

        public void InsertBlankRow(object obj)
        {
            if (SelectedItems != null && SelectedItems.Count == 1 && _collectionView != null)
            {
                _collectionView.InsertBlankRow(SelectedItems!.FirstOrDefault()!, obj);
            }
        }

    }
}
