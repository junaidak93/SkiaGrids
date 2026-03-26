using SkiaSharpControlV2.Data;
using SkiaSharpControlV2.Data.Enum;
using SkiaSharpControlV2.Data.Models;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SkiaSharpControlV2.Helpers
{

    public interface ICustomCollectionView : IEnumerable,IDisposable
    {
        IEnumerable Items { get; }
        bool IsLiveSort { get; set; }
        bool FilterByGroup { get; set; }
        List<object> ViewList { get; }
        List<Filter> Filters { get; }
        List<SKGroupField>? GroupFields { get; set; }
        int Count { get; }
        IEnumerable<Group> GroupList { get; }
        void ApplyGroup(string propertyName);
        void ClearGroup();
        void AddSort(string propertyName, ListSortDirection sortDirection);
        void ClearSortDescriptions();
        bool AddOrUpdateFilter(Filter filter);
        bool RemoveFilter(Filter filter);
        void Refresh();
        bool MoveRowUp(object item);
        bool MoveRowDown(object item);
        bool InsertBlankRow(object item, object newObject);

        event NotifyCollectionChangedEventHandler CollectionChanged;
    }



    public class Group
    {
        public object Key { get; set; }
        public List<object> Items { get; } = new List<object>();
        public Dictionary<string, object> Aggregates { get; } = new();

    }
    public class CustomCollectionView : ICustomCollectionView
    {
        private readonly IList _source;
        internal readonly List<object> _viewList = new();
        private readonly List<Filter> _filters = new();
        private readonly List<(string Prop, ListSortDirection Dir)> _sorts = new();
        private string _groupProperty;

        public bool IsLiveSort { get; set; }
        public bool FilterByGroup { get; set; }
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IEnumerable Items => GetCurrentItems();
        public int Count => _viewList.Count;
        public IEnumerable<Group> GroupList => _groups;
        public List<SKGroupField>? GroupFields { get; set; }
        List<object> ICustomCollectionView.ViewList => _viewList;

        public List<Filter> Filters => _filters;

        private readonly List<Group> _groups = new();
        private ReflectionHelper reflectionHelper;

        public CustomCollectionView(IEnumerable items, ReflectionHelper reflectionHelper)
        {
            this.reflectionHelper = reflectionHelper;
            _source = items as IList ?? throw new ArgumentException("Items must be IList");

            foreach (var item in _source)
            {
                _viewList.Add(item);
            }

            if (_source is INotifyCollectionChanged notifier)
            {
                notifier.CollectionChanged -= Source_CollectionChanged;
                notifier.CollectionChanged += Source_CollectionChanged;
            }
            Task.Run(() =>
            {
                foreach (var item in _source)
                {
                    AddPropertyChange(item);
                }
            });
            Refresh();
        }

        private void AddPropertyChange(object item)
        {
            if (item is INotifyPropertyChanged npc)
                npc.PropertyChanged += Item_PropertyChanged;
        }
        private void RemovePropertyChange(object item)
        {
            if (item is INotifyPropertyChanged npc)
                npc.PropertyChanged -= Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Normal filters
            if (_filters.Any(f => f.Column == e.PropertyName) && !FilterByGroup)
            {
                Refresh();
            }

            // Group-based filters
            if (FilterByGroup && GroupFields != null && GroupFields.Count > 0)
            {
                var groupField = GroupFields.FirstOrDefault(x => x.BindingPath == e.PropertyName);
                if (groupField != null)
                {
                    var groupKey = reflectionHelper.GetPropValue(sender, _groupProperty);
                    var group = _groups.FirstOrDefault(g => Equals(g.Key, groupKey));
                    if (group != null)
                    {
                        CalculateAggregates(group);

                        if (!PassGroupFilter(group))
                            _groups.Remove(group);
                        else if (!_groups.Contains(group))
                            _groups.Add(group);

                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
                }
            }
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    InsertNewItem(item);
                    AddPropertyChange(item);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    if (_viewList.Contains(item))
                    {
                        _viewList.Remove(item);
                        RemovePropertyChange(item);
                        Refresh();
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in _viewList)
                {
                    RemovePropertyChange(item);
                }
                _viewList.Clear();
                Refresh();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
            }
        }

        private void InsertNewItem(object item)
        {
            if (!PassAllFilter(item) && !FilterByGroup)
                return;

            if (!string.IsNullOrEmpty(_groupProperty))
            {
                var key = reflectionHelper.GetPropValue(item, _groupProperty);
                var group = _groups.FirstOrDefault(g => Equals(g.Key, key));
                if (group == null)
                {
                    group = new Group { Key = key };
                    _groups.Add(group);
                }
                group.Items.Add(item);
                CalculateAggregates(group);

                if (FilterByGroup && !PassGroupFilter(group))
                    _groups.Remove(group);
            }
            else
            {
                _viewList.Insert(0, item);
            }
        }

        public void ApplyGroup(string propertyName)
        {
            _groupProperty = propertyName;
            Refresh();
        }

        public void ClearGroup()
        {
            _groupProperty = null;
            _groups.Clear();
            Refresh();
        }

        public void AddSort(string propertyName, ListSortDirection sortDirection)
        {
            _sorts.Add((propertyName, sortDirection));
            Refresh();
        }

        public void ClearSortDescriptions()
        {
            _sorts.Clear();
            Refresh();
        }

        public bool AddOrUpdateFilter(Filter filter)
        {
            var f = _filters.FirstOrDefault(x => x.Column == filter.Column);
            if (f == null)
            {
                _filters.Add(filter);
                Refresh();
                return true;
            }
            else
            {
                _filters.Remove(f);
                _filters.Add(filter);

                Refresh();
                return true;
            }
        }

        public bool RemoveFilter(Filter filter)
        {
            var f = _filters.FirstOrDefault(x => x.Column == filter.Column);
            if (f != null)
            {
                _filters.Remove(f);
                Refresh();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Refresh()
        {
            _viewList.Clear();
            _groups.Clear();

            // Normal mode (FilterByGroup = false)
            foreach (var item in _source)
            {
                if (!FilterByGroup && PassAllFilter(item))
                    _viewList.Add(item);
                else if (FilterByGroup)
                    _viewList.Add(item); 
            }

            if (_sorts.Any())
                _viewList.Sort(new SortComparer(_sorts, reflectionHelper.GetPropValue));

            if (!string.IsNullOrEmpty(_groupProperty))
            {
                foreach (var g in _viewList.GroupBy(x => reflectionHelper.GetPropValue(x, _groupProperty)))
                {
                    var group = new Group { Key = g.Key };
                    group.Items.AddRange(g);

                    if (_sorts.Any())
                        group.Items.Sort(new SortComparer(_sorts, reflectionHelper.GetPropValue));

                    CalculateAggregates(group);

                    if (!FilterByGroup || PassGroupFilter(group))
                        _groups.Add(group);
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void CalculateAggregates(Group group)
        {
            if (GroupFields == null) return;

            group.Aggregates.Clear();

            foreach (var field in GroupFields)
            {
                var values = group.Items
                    .Select(x => reflectionHelper.GetPropValue(x, field.BindingPath))
                    .Where(v => v != null)
                    .ToList();

                if (values.Count == 0) continue;

                object? result = null;

                switch (field.Aggregation)
                {
                    case SkAggregation.Sum:
                        result = SafeNumericSum(values);
                        break;

                    case SkAggregation.Avg:
                        result = SafeNumericAverage(values);
                        break;

                    case SkAggregation.Min:
                        result = SafeComparable(values, isMin: true);
                        break;

                    case SkAggregation.Max:
                        result = SafeComparable(values, isMin: false);
                        break;

                    case SkAggregation.Count:
                        result = values.Count;
                        break;

                    case SkAggregation.Distinct:
                        result = string.Join('/',
                            values.OfType<string>()
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .OrderBy(x => x));
                        break;

                    case SkAggregation.None:
                        result = null;
                        break;
                }

                group.Aggregates[field.BindingPath] = result;
            }
        }

        /// <summary> Safely sums only numeric values </summary>
        private decimal SafeNumericSum(IEnumerable<object> values)
        {
            return values.OfType<IConvertible>()
                         .Select(v => Convert.ToDecimal(v))
                         .Sum();
        }

        /// <summary> Safely averages only numeric values </summary>
        private decimal SafeNumericAverage(IEnumerable<object> values)
        {
            var nums = values.OfType<IConvertible>()
                             .Select(v => Convert.ToDecimal(v))
                             .ToList();

            return nums.Count > 0 ? nums.Average() : 0;
        }

        /// <summary> Handles Min/Max for IComparable values </summary>
        private object? SafeComparable(List<object> values, bool isMin)
        {
            var comparableValues = values.OfType<IComparable>().ToList();
            if (comparableValues.Count == 0) return null;

            return isMin
                ? comparableValues.Min()
                : comparableValues.Max();
        }

        private bool PassGroupFilter(Group group)
        {
            foreach (var filter in _filters)
            {
                if (!string.IsNullOrEmpty(filter.Column))
                {
                    if (group.Aggregates.TryGetValue(filter.Column, out var val))
                    {
                        string strValue = val?.ToString() ?? "";

                        switch (filter.FilterType)
                        {
                            case FilterType.Text:
                                if (!WildcardMatch(strValue, filter.Text))
                                    return false;
                                break;

                            case FilterType.List:
                                if (filter.List == null || !filter.List.Contains(strValue == "" ? null : strValue))
                                    return false;
                                break;

                            case FilterType.Value:
                                if (!ApplyComparison(strValue, filter.Value.Operator, filter.Value.Value, filter.Value.DataType))
                                    return false;
                                break;
                        }
                    }
                }
            }
            return true;
        }

        private IEnumerable GetCurrentItems()
        {
            if (!string.IsNullOrEmpty(_groupProperty))
                return _groups.SelectMany(g => g.Items);
            return _viewList;
        }

        private bool PassAllFilter(object item)
        {
            foreach (var filter in _filters)
            {
                if (!string.IsNullOrEmpty(filter.Column))
                {
                    (string? Value, Type? Type) value = reflectionHelper.ReadCurrentItemWithTypes(item, filter.Column);
                    if (value.Value != null)
                    {
                        object val = value.Value;

                        if (DateTime.TryParse(val?.ToString(), out DateTime dtValue) && filter?.Value.DataType == typeof(DateOnly))
                            val = DateOnly.FromDateTime(dtValue);

                        if (DateTime.TryParse(val?.ToString(), out DateTime dtValue1) && filter?.Value.DataType == typeof(TimeSpan))
                            val = dtValue1.TimeOfDay;

                        string strValue = val?.ToString() ?? "";

                        switch (filter.FilterType)
                        {
                            case FilterType.Text:
                                if (!WildcardMatch(strValue, filter.Text))
                                    return false;
                                break;

                            case FilterType.List:
                                if (filter.List == null || !filter.List.Contains(strValue == "" ? null : strValue))
                                    return false;
                                break;

                            case FilterType.Value:
                                if (!ApplyComparison(strValue, filter.Value.Operator, filter.Value.Value, filter.Value.DataType))
                                    return false;
                                break;
                        }
                    }
                }
            }
            return true;
        }

        private static bool WildcardMatch(string input, string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                                            .Replace("\\*", ".*")
                                            .Replace("\\?", ".") + "$";
            return Regex.IsMatch(input, regexPattern);
        }

        private static bool ApplyComparison(string actualStr, string @operator, string expectedStr, Type valueType)
        {
            if (!TryConvert(actualStr, valueType, out var actual) ||
                !TryConvert(expectedStr, valueType, out var expected))
            {
                return @operator switch
                {
                    "=" => string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase),
                    "<>" => !string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }

            var comp = Comparer<object>.Default;

            return @operator switch
            {
                "=" => Equals(actual, expected),
                "<>" => !Equals(actual, expected),
                ">" => comp.Compare(actual, expected) > 0,
                "<" => comp.Compare(actual, expected) < 0,
                ">=" => comp.Compare(actual, expected) >= 0,
                "<=" => comp.Compare(actual, expected) <= 0,
                _ => false
            };
        }

        private static bool TryConvert(string input, Type type, out object result)
        {
            try
            {
                if (type == typeof(double) && double.TryParse(input, out var d))
                {
                    result = d;
                    return true;
                }
                if (type == typeof(int) && int.TryParse(input, out var i))
                {
                    result = i;
                    return true;
                }
                if (type == typeof(decimal) && decimal.TryParse(input, out var m))
                {
                    result = m;
                    return true;
                }
                if (type == typeof(DateTime) && DateTime.TryParse(input, out var dt))
                {
                    result = dt;
                    return true;
                }
                if (type == typeof(TimeSpan) && TimeSpan.TryParse(input, out var ts))
                {
                    result = ts;
                    return true;
                }
                if (type == typeof(DateOnly) && DateOnly.TryParse(input, out var doVal))
                {
                    result = doVal;
                    return true;
                }

                result = Convert.ChangeType(input, type);
                return true;
            }
            catch
            {
                result = null!;
                return false;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _viewList.GetEnumerator();
        }

        public bool MoveRowUp(object item)
        {
            if (item == null) return false;

            var index = _viewList.IndexOf(item);
            if (index > 0)
            {
                _viewList.RemoveAt(index);
                _viewList.Insert(index - 1, item);
                return true;
            }
            return false;
        }

        public bool MoveRowDown(object item)
        {
            if (item == null) return false;

            var index = _viewList.IndexOf(item);
            if (index >= 0 && index < _viewList.Count - 1)
            {
                _viewList.RemoveAt(index);
                _viewList.Insert(index + 1, item);
                return true;
            }
            return false;
        }

        public bool InsertBlankRow(object item, object newObject)
        {
            var index = _viewList.IndexOf(item);
            if (index > 0 && index < _viewList.Count - 1)
            {
                _viewList.Insert(index, newObject);
                return true;
            }
            return false;
        }

        private bool _disposed;

        // your fields...

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Unsubscribe from source collection events
                if (_source is INotifyCollectionChanged notifier)
                {
                    notifier.CollectionChanged -= Source_CollectionChanged;
                }

                // Unsubscribe property change handlers
                foreach (var item in _source)
                {
                    RemovePropertyChange(item);
                }

                // Clear everything
                _viewList.Clear();
                _groups.Clear();
                _filters.Clear();
                _sorts.Clear();
                GroupFields?.Clear();

                // Remove subscribers to our event
                CollectionChanged = null;
            }

            _disposed = true;
        }

        ~CustomCollectionView()
        {
            Dispose(false);
        }

        private class SortComparer : IComparer<object>
        {
            private readonly List<(string Prop, ListSortDirection Dir)> _sorts;
            private readonly Func<object, string, object> _getter;

            public SortComparer(List<(string Prop, ListSortDirection Dir)> sorts, Func<object, string, object> getter)
            {
                _sorts = sorts;
                _getter = getter;
            }

            public int Compare(object x, object y)
            {
                foreach (var (prop, dir) in _sorts)
                {
                    var xv = _getter(x, prop);
                    var yv = _getter(y, prop);
                    var result = Comparer.Default.Compare(xv, yv);
                    if (result != 0)
                        return dir == ListSortDirection.Ascending ? result : -result;
                }
                return 0;
            }
        }
    }



    //public class CustomCollectionView : ICustomCollectionView
    //{
    //    private readonly IList _source;
    //    internal readonly List<object> _viewList = new();
    //    private readonly List<Filter> _filters = new();
    //    private readonly List<(string Prop, ListSortDirection Dir)> _sorts = new();
    //    private string _groupProperty;

    //    public bool IsLiveSort { get; set; }
    //    public bool FilterByGroup { get; set; }
    //    public event NotifyCollectionChangedEventHandler CollectionChanged;

    //    public IEnumerable Items => GetCurrentItems();
    //    public int Count => _viewList.Count;
    //    public IEnumerable<Group> GroupList => _groups;
    //    public List<SKGroupField>? GroupFields { get; set; }
    //    List<object> ICustomCollectionView.ViewList => _viewList;

    //    public List<Filter> Filters => _filters;

    //    private readonly List<Group> _groups = new();
    //    private ReflectionHelper reflectionHelper;

    //    public CustomCollectionView(IEnumerable items, ReflectionHelper reflectionHelper)
    //    {
    //        this.reflectionHelper = reflectionHelper;
    //        _source = items as IList ?? throw new ArgumentException("Items must be IList");

    //        foreach (var item in _source)
    //        {
    //            _viewList.Add(item);
    //        }

    //        if (_source is INotifyCollectionChanged notifier)
    //        {
    //            notifier.CollectionChanged -= Source_CollectionChanged;
    //            notifier.CollectionChanged += Source_CollectionChanged;
    //        }
    //        Task.Run(() =>
    //        {
    //            foreach (var item in _source)
    //            {
    //                AddPropertyChange(item);
    //            }
    //        });
    //        Refresh();

    //    }

    //    private void AddPropertyChange(object item)
    //    {
    //        if (item is INotifyPropertyChanged npc)
    //            npc.PropertyChanged += Item_PropertyChanged;
    //    }
    //    private void RemovePropertyChange(object item)
    //    {
    //        if (item is INotifyPropertyChanged npc)
    //            npc.PropertyChanged -= Item_PropertyChanged;
    //    }
    //    private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    //    {
    //        if (_filters.Any(f => f.Column == e.PropertyName))
    //        {
    //            Refresh();
    //        }
    //        if (FilterByGroup && GroupFields != null && GroupFields.Count > 0)
    //        {
    //            var group = GroupFields.FirstOrDefault(x => x.BindingPath == e.PropertyName);
    //            if (group != null)
    //            {
    //                Refresh();
    //            }
    //        }
    //    }

    //    private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //    {
    //        if (e.NewItems != null)
    //        {
    //            foreach (var item in e.NewItems)
    //            {
    //                InsertNewItem(item);
    //                AddPropertyChange(item);
    //                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    //            }
    //        }
    //        if (e.Action == NotifyCollectionChangedAction.Remove)
    //        {
    //            foreach (var item in e.OldItems)
    //            {
    //                if (_viewList.Contains(item))
    //                {
    //                    _viewList.Remove(item);
    //                    RemovePropertyChange(item);
    //                    Refresh();
    //                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
    //                }
    //            }
    //        }
    //        if (e.Action == NotifyCollectionChangedAction.Reset)
    //        {
    //            foreach (var item in _viewList)
    //            {
    //                RemovePropertyChange(item);
    //            }
    //            _viewList.Clear();
    //            Refresh();
    //            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
    //        }
    //    }

    //    private void InsertNewItem(object item)
    //    {
    //        if (!PassAllFilter(item))
    //            return;

    //        if (!string.IsNullOrEmpty(_groupProperty))
    //        {
    //            var key = reflectionHelper.GetPropValue(item, _groupProperty);
    //            var group = _groups.FirstOrDefault(g => Equals(g.Key, key));
    //            if (group == null)
    //            {
    //                group = new Group { Key = key };
    //                _groups.Add(group);
    //            }
    //            if (IsLiveSort && _sorts.Any())
    //            {
    //                var index = group.Items.BinarySearch(item, new SortComparer(_sorts, reflectionHelper.GetPropValue));
    //                group.Items.Insert(index < 0 ? ~index : index, item);
    //            }
    //            else
    //            {
    //                group.Items.Insert(0, item);
    //            }
    //        }
    //        else
    //        {
    //            if (IsLiveSort && _sorts.Any())
    //            {
    //                var index = _viewList.BinarySearch(item, new SortComparer(_sorts, reflectionHelper.GetPropValue));
    //                _viewList.Insert(index < 0 ? ~index : index, item);
    //            }
    //            else
    //            {
    //                _viewList.Insert(0, item);
    //            }
    //        }
    //    }

    //    public void ApplyGroup(string propertyName)
    //    {
    //        _groupProperty = propertyName;
    //        Refresh();
    //    }

    //    public void ClearGroup()
    //    {
    //        _groupProperty = null;
    //        _groups.Clear();
    //        Refresh();
    //    }

    //    public void AddSort(string propertyName, ListSortDirection sortDirection)
    //    {
    //        _sorts.Add((propertyName, sortDirection));
    //        Refresh();
    //    }


    //    public void ClearSortDescriptions()
    //    {
    //        _sorts.Clear();
    //        Refresh();
    //    }

    //    public bool AddOrUpdateFilter(Filter filter)
    //    {
    //        var f = _filters.FirstOrDefault(x => x.Column == filter.Column);
    //        if (f == null)
    //        {
    //            _filters.Add(filter);
    //            Refresh();
    //            return true;
    //        }
    //        else
    //        {
    //            _filters.Remove(f);
    //            _filters.Add(filter);

    //            Refresh();
    //            return true;
    //        }
    //    }

    //    public bool RemoveFilter(Filter filter)
    //    {
    //        var f = _filters.FirstOrDefault(x => x.Column == filter.Column);
    //        if (f != null)
    //        {
    //            _filters.Remove(f);
    //            Refresh();
    //            return true;
    //        }
    //        else
    //        {
    //            return false;
    //        }
    //    }

    //    public void Refresh()
    //    {
    //        _viewList.Clear();
    //        _groups.Clear();

    //        foreach (var item in _source)
    //        {
    //            if (PassAllFilter(item))
    //                _viewList.Add(item);
    //        }

    //        if (_sorts.Any())
    //        {
    //            _viewList.Sort(new SortComparer(_sorts, reflectionHelper.GetPropValue));
    //        }

    //        if (!string.IsNullOrEmpty(_groupProperty))
    //        {
    //            foreach (var g in _viewList.GroupBy(x => reflectionHelper.GetPropValue(x, _groupProperty)))
    //            {
    //                var group = new Group { Key = g.Key };
    //                group.Items.AddRange(g);
    //                if (_sorts.Any())
    //                    group.Items.Sort(new SortComparer(_sorts, reflectionHelper.GetPropValue));
    //                _groups.Add(group);
    //            }
    //        }

    //        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    //    }

    //    private IEnumerable GetCurrentItems()
    //    {
    //        if (!string.IsNullOrEmpty(_groupProperty))
    //            return _groups.SelectMany(g => g.Items);
    //        return _viewList;
    //    }

    //    private bool PassAllFilter(object item)
    //    {
    //        foreach (var filter in _filters)
    //        {
    //            if (!string.IsNullOrEmpty(filter.Column))
    //            {
    //                (string? Value, Type? Type) value = reflectionHelper.ReadCurrentItemWithTypes(item, filter.Column);
    //                if (value.Value != null)
    //                {
    //                    object val = value.Value;

    //                    if (DateTime.TryParse(val?.ToString(), out DateTime dtValue) && filter?.Value.DataType == typeof(DateOnly))
    //                        val = DateOnly.FromDateTime(dtValue);

    //                    if (DateTime.TryParse(val?.ToString(), out DateTime dtValue1) && filter?.Value.DataType == typeof(TimeSpan))
    //                        val = dtValue1.TimeOfDay;


    //                    string strValue = val?.ToString() ?? "";

    //                    // Handle filter types
    //                    switch (filter.FilterType)
    //                    {
    //                        case FilterType.Text:
    //                            if (!WildcardMatch(strValue, filter.Text))
    //                                return false;
    //                            break;

    //                        case FilterType.List:
    //                            if (filter.List == null || !filter.List.Contains(strValue == "" ? null : strValue))
    //                                return false;
    //                            break;

    //                        case FilterType.Value:
    //                            if (!ApplyComparison(strValue, filter.Value.Operator, filter.Value.Value, filter.Value.DataType))
    //                                return false;
    //                            break;
    //                    }
    //                }
    //            }
    //        }

    //        return true;
    //    }
    //    private static bool WildcardMatch(string input, string pattern)
    //    {
    //        // Convert wildcard pattern to regex
    //        var regexPattern = "^" + Regex.Escape(pattern)
    //                                        .Replace("\\*", ".*")
    //                                        .Replace("\\?", ".") + "$";
    //        return Regex.IsMatch(input, regexPattern);
    //    }

    //    private static bool ApplyComparison(string actualStr, string @operator, string expectedStr, Type valueType)
    //    {
    //        if (!TryConvert(actualStr, valueType, out var actual) ||
    //            !TryConvert(expectedStr, valueType, out var expected))
    //        {
    //            // Fall back to string comparison ONLY for = and <>
    //            return @operator switch
    //            {
    //                "=" => string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase),
    //                "<>" => !string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase),
    //                _ => false // For <, >, <=, >= etc. — no type match = no comparison
    //            };
    //        }

    //        var comp = Comparer<object>.Default;

    //        return @operator switch
    //        {
    //            "=" => Equals(actual, expected),
    //            "<>" => !Equals(actual, expected),
    //            ">" => comp.Compare(actual, expected) > 0,
    //            "<" => comp.Compare(actual, expected) < 0,
    //            ">=" => comp.Compare(actual, expected) >= 0,
    //            "<=" => comp.Compare(actual, expected) <= 0,
    //            _ => false
    //        };
    //    }
    //    private static bool TryConvert(string input, Type type, out object result)
    //    {
    //        try
    //        {
    //            if (type == typeof(double) && double.TryParse(input, out var d))
    //            {
    //                result = d;
    //                return true;
    //            }
    //            if (type == typeof(int) && int.TryParse(input, out var i))
    //            {
    //                result = i;
    //                return true;
    //            }
    //            if (type == typeof(decimal) && decimal.TryParse(input, out var m))
    //            {
    //                result = m;
    //                return true;
    //            }
    //            if (type == typeof(DateTime) && DateTime.TryParse(input, out var dt))
    //            {
    //                result = dt;
    //                return true;
    //            }
    //            if (type == typeof(TimeSpan) && TimeSpan.TryParse(input, out var ts))
    //            {
    //                result = ts;
    //                return true;
    //            }
    //            if (type == typeof(DateOnly) && DateOnly.TryParse(input, out var doVal))
    //            {
    //                result = doVal;
    //                return true;
    //            }

    //            // Default attempt
    //            result = Convert.ChangeType(input, type);
    //            return true;
    //        }
    //        catch
    //        {
    //            result = null!;
    //            return false;
    //        }
    //    }


    //    public IEnumerator GetEnumerator()
    //    {
    //        return _viewList.GetEnumerator();
    //    }
    //    public bool MoveRowUp(object item)
    //    {
    //        if (item == null) return false;

    //        var index = _viewList.IndexOf(item);
    //        if (index > 0)
    //        {
    //            _viewList.RemoveAt(index);
    //            _viewList.Insert(index - 1, item);
    //            return true;
    //        }
    //        return false;
    //    }

    //    public bool MoveRowDown(object item)
    //    {
    //        if (item == null) return false;

    //        var index = _viewList.IndexOf(item);
    //        if (index >= 0 && index < _viewList.Count - 1)
    //        {
    //            _viewList.RemoveAt(index);
    //            _viewList.Insert(index + 1, item);
    //            return true;
    //        }
    //        return false;
    //    }
    //    public bool InsertBlankRow(object item, object newObject)
    //    {
    //        var index = _viewList.IndexOf(item);
    //        if (index > 0 && index < _viewList.Count - 1)
    //        {
    //            _viewList.Insert(index, newObject);
    //            return true;
    //        }
    //        return false;
    //    }


    //    private class SortComparer : IComparer<object>
    //    {
    //        private readonly List<(string Prop, ListSortDirection Dir)> _sorts;
    //        private readonly Func<object, string, object> _getter;

    //        public SortComparer(List<(string Prop, ListSortDirection Dir)> sorts, Func<object, string, object> getter)
    //        {
    //            _sorts = sorts;
    //            _getter = getter;
    //        }

    //        public int Compare(object x, object y)
    //        {
    //            foreach (var (prop, dir) in _sorts)
    //            {
    //                var xv = _getter(x, prop);
    //                var yv = _getter(y, prop);
    //                var result = Comparer.Default.Compare(xv, yv);
    //                if (result != 0)
    //                    return dir == ListSortDirection.Ascending ? result : -result;
    //            }
    //            return 0;
    //        }
    //    }
    //}




}
