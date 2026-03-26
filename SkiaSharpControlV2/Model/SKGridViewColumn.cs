using SkiaSharp;
using SkiaSharpControlV2.Data.Enum;
using SkiaSharpControlV2.Helpers;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace SkiaSharpControlV2
{
    public class SKGridViewColumn : DependencyObject, INotifyPropertyChanged
    {
        public SKGridViewColumn()
        {

        }
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));


        public string? Name
        {
            get => (string?)GetValue(NameProperty);
            set { SetValue(NameProperty, value); OnPropertyChanged(); }
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(nameof(Name), typeof(string), typeof(SKGridViewColumn), new PropertyMetadata(null));
        public string? Header
        {
            get => (string?)GetValue(HeaderProperty);
            set { SetValue(HeaderProperty, value); OnPropertyChanged(); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SKGridViewColumn), new PropertyMetadata(null));

        public string BindingPath
        {
            get => (string)GetValue(BindingPathProperty);
            set => SetValue(BindingPathProperty, value);
        }

        public static readonly DependencyProperty BindingPathProperty =
            DependencyProperty.Register(nameof(BindingPath), typeof(string), typeof(SKGridViewColumn), new PropertyMetadata(null));

        public string? DisplayHeader
        {
            get => (string?)GetValue(DisplayHeaderProperty);
            set => SetValue(DisplayHeaderProperty, value);
        }

        public static readonly DependencyProperty DisplayHeaderProperty =
            DependencyProperty.Register(nameof(DisplayHeader), typeof(string), typeof(SKGridViewColumn), new PropertyMetadata(null));

        public double Width
        {
            get => (double)GetValue(WidthProperty);
            set { SetValue(WidthProperty, value); }
        }

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(nameof(Width), typeof(double), typeof(SKGridViewColumn), new PropertyMetadata(100.0, (s, e) => TriggerChanged(s, e)));



        public string? BackColor
        {
            get => (string?)GetValue(BackColorProperty);
            set => SetValue(BackColorProperty, value);
        }

        public static readonly DependencyProperty BackColorProperty =
            DependencyProperty.Register(nameof(BackColor), typeof(string), typeof(SKGridViewColumn), new PropertyMetadata(null, (s, e) => TriggerChanged(s, e)));



        public CellContentAlignment ContentAlignment
        {
            get => (CellContentAlignment)GetValue(ContentAlignmentProperty);
            set => SetValue(ContentAlignmentProperty, value);
        }

        public static readonly DependencyProperty ContentAlignmentProperty =
            DependencyProperty.Register(nameof(ContentAlignment), typeof(CellContentAlignment), typeof(SKGridViewColumn), new PropertyMetadata(CellContentAlignment.Left));

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(nameof(IsVisible), typeof(bool?), typeof(SKGridViewColumn), new PropertyMetadata(true, (s, e) => TriggerChanged(s, e)));


        public bool? CanUserResize
        {
            get => (bool?)GetValue(CanUserResizeProperty);
            set => SetValue(CanUserResizeProperty, value);
        }

        public static readonly DependencyProperty CanUserResizeProperty =
            DependencyProperty.Register(nameof(CanUserResize), typeof(bool?), typeof(SKGridViewColumn), new PropertyMetadata(true, (s, e) => TriggerChanged(s, e)));

        public bool? CanUserReorder
        {
            get => (bool?)GetValue(CanUserReorderProperty);
            set => SetValue(CanUserReorderProperty, value);
        }

        public static readonly DependencyProperty CanUserReorderProperty =
            DependencyProperty.Register(nameof(CanUserReorder), typeof(bool?), typeof(SKGridViewColumn), new PropertyMetadata(true, (s, e) => TriggerChanged(s, e)));

        public bool? CanUserSort
        {
            get => (bool?)GetValue(CanUserSortProperty);
            set => SetValue(CanUserSortProperty, value);
        }

        public static readonly DependencyProperty CanUserSortProperty =
            DependencyProperty.Register(nameof(CanUserSort), typeof(bool?), typeof(SKGridViewColumn), new PropertyMetadata(true, (s, e) => TriggerChanged(s, e)));

        public SkGridViewColumnSort GridViewColumnSort
        {
            get => (SkGridViewColumnSort)GetValue(GridViewColumnSortProperty);
            set => SetValue(GridViewColumnSortProperty, value);
        }

        public static readonly DependencyProperty GridViewColumnSortProperty =
            DependencyProperty.Register(nameof(GridViewColumnSort), typeof(SkGridViewColumnSort), typeof(SKGridViewColumn), new PropertyMetadata(SkGridViewColumnSort.None, (s, e) => TriggerChanged(s, e)));

        public SKCellTemplate? CellTemplate
        {
            get => (SKCellTemplate?)GetValue(CellTemplateProperty);
            set => SetValue(CellTemplateProperty, value);
        }
        public static readonly DependencyProperty CellTemplateProperty =
            DependencyProperty.Register(nameof(CellTemplate), typeof(SKCellTemplate), typeof(SKGridViewColumn), new PropertyMetadata(null));


        public int? DisplayIndex
        {
            get => (int?)GetValue(DisplayIndexProperty);
            set => SetValue(DisplayIndexProperty, value);
        }
        public static readonly DependencyProperty DisplayIndexProperty =
            DependencyProperty.Register(nameof(DisplayIndex), typeof(int?), typeof(SKGridViewColumn), new PropertyMetadata(null, (s, e) => TriggerChanged(s, e)));

        public string? Format
        {
            get => (string?)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(nameof(Format), typeof(string), typeof(SKGridViewColumn), new PropertyMetadata(null));

        public bool ShowBracketOnNegative
        {
            get => (bool)GetValue(ShowBracketOnNegativeProperty);
            set => SetValue(ShowBracketOnNegativeProperty, value);
        }
        public static readonly DependencyProperty ShowBracketOnNegativeProperty =
            DependencyProperty.Register(nameof(ShowBracketOnNegative), typeof(bool), typeof(SKGridViewColumn), new PropertyMetadata(false));
        public bool FormatWithAcronym
        {
            get => (bool)GetValue(FormatWithAcronymProperty);
            set => SetValue(FormatWithAcronymProperty, value);
        }
        public static readonly DependencyProperty FormatWithAcronymProperty =
            DependencyProperty.Register(nameof(FormatWithAcronym), typeof(bool), typeof(SKGridViewColumn), new PropertyMetadata(false));

        public bool DataVisible
        {
            get => (bool)GetValue(DataVisibleProperty);
            set => SetValue(DataVisibleProperty, value);
        }
        public static readonly DependencyProperty DataVisibleProperty =
            DependencyProperty.Register(nameof(DataVisible), typeof(bool), typeof(SKGridViewColumn), new PropertyMetadata(true));

        private static void TriggerChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is SKGridViewColumn a) a.OnPropertyChanged(e.Property.ToString());
        }

    }

    public class SKGroupDefinition : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public string ForegroundColor
        {
            get { return (string)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register(nameof(ForegroundColor), typeof(string), typeof(SKGroupDefinition), new PropertyMetadata(default, (s, e) => TriggerChanged(s, e)));

        public string RowBackground
        {
            get { return (string)GetValue(RowBackgroundProperty); }
            set { SetValue(RowBackgroundProperty, value); }
        }

        public static readonly DependencyProperty RowBackgroundProperty =
            DependencyProperty.Register(nameof(RowBackground), typeof(string), typeof(SKGroupDefinition), new PropertyMetadata(default, (s, e) => TriggerChanged(s, e)));

        public SKGroupCellTemplate? GroupCellTemplate
        {
            get => (SKGroupCellTemplate?)GetValue(GroupCellTemplateProperty);
            set => SetValue(GroupCellTemplateProperty, value);
        }
        public static readonly DependencyProperty GroupCellTemplateProperty =
            DependencyProperty.Register(nameof(GroupCellTemplate), typeof(SKGroupCellTemplate), typeof(SKGroupDefinition), new PropertyMetadata(null));

        public string? GroupBy { get; set; }
        public string? Target { get; set; }
        public ObservableCollection<SKGroupField>? HeaderFields { get; set; } = new();
        public SKGroupToggleSymbol? ToggleSymbol { get; set; }


        private static void TriggerChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is SKGroupDefinition a) a.OnPropertyChanged(e.Property.ToString());
        }
    }

    public class SKGroupField : DependencyObject
    {
        public required string BindingPath { get; set; }
        public required string TargetColumns { get; set; }
        public SkAggregation Aggregation { get; set; }  // Sum, Count, Avg, Min, Max

        public SKGroupCellTemplate? GroupCellTemplate
        {
            get => (SKGroupCellTemplate?)GetValue(GroupCellTemplateProperty);
            set => SetValue(GroupCellTemplateProperty, value);
        }
        public static readonly DependencyProperty GroupCellTemplateProperty =
            DependencyProperty.Register(nameof(GroupCellTemplate), typeof(SKGroupCellTemplate), typeof(SKGroupField), new PropertyMetadata(null));
    }

    public class SKSetter : DependencyObject
    {
        public required SkStyleProperty Property { get; set; }

        public string? ValuePath { get; set; }

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
       DependencyProperty.Register(nameof(Value), typeof(object), typeof(SKSetter), new PropertyMetadata(null));
    }
    public class SKCondition : DependencyObject
    {
        public object Binding
        {
            get => GetValue(BindingProperty);
            set => SetValue(BindingProperty, value);
        }
        public static readonly DependencyProperty BindingProperty =
       DependencyProperty.Register(nameof(Binding), typeof(object), typeof(SKCondition), new PropertyMetadata(null));
        public required string BindingPath { get; set; }
        public required SKOperation Operator { get; set; }


        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(SKCondition), new PropertyMetadata(null));

    }

    public class SKGroupCondition : DependencyObject
    {
        public object Binding
        {
            get => GetValue(BindingProperty);
            set => SetValue(BindingProperty, value);
        }
        public static readonly DependencyProperty BindingProperty =
       DependencyProperty.Register(nameof(Binding), typeof(object), typeof(SKGroupCondition), new PropertyMetadata(null));
        public string? BindingPath { get; set; }

        public SkAggregation Aggregation { get; set; }

        public SKOperation Operator { get; set; }
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(SKGroupCondition), new PropertyMetadata(null));

    }

    public abstract class SKTrigger : DependencyObject
    {

        public bool IsTimerBased
        {
            get => (bool)GetValue(IsTimerBasedProperty);
            set => SetValue(IsTimerBasedProperty, value);
        }
        public static readonly DependencyProperty IsTimerBasedProperty =
       DependencyProperty.Register(nameof(IsTimerBased), typeof(bool), typeof(SKTrigger), new PropertyMetadata(false));

        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }
        public static readonly DependencyProperty DurationProperty =
       DependencyProperty.Register(nameof(Duration), typeof(double), typeof(SKTrigger), new PropertyMetadata(0.0));

        public ObservableCollection<SKSetter> Setters { get; set; } = new();
        public abstract bool Evaluate(object dataContext, ReflectionHelper helper);
        public static bool EvaluateCondition(string? leftStr, Type? leftType, object rightVal, SKOperation op)
        {
            if (leftType == null || leftStr == null) return false;

            try
            {
                var left = Convert.ChangeType(leftStr, leftType);
                var right = Convert.ChangeType(rightVal, leftType);

                int cmp = Comparer.DefaultInvariant.Compare(left, right);

                return op switch
                {
                    SKOperation.Equals => cmp == 0,
                    SKOperation.NotEquals => cmp != 0,
                    SKOperation.GreaterThan => cmp > 0,
                    SKOperation.LessThan => cmp < 0,
                    SKOperation.GreaterThanOrEqual => cmp >= 0,
                    SKOperation.LessThanOrEqual => cmp <= 0,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }
    }
    public abstract class SKGroupTrigger : DependencyObject
    {
        public ObservableCollection<SKSetter> Setters { get; set; } = new();

    }

    public class SkGroupDataTrigger : SKGroupTrigger
    {
        public object Binding
        {
            get => GetValue(BindingProperty);
            set => SetValue(BindingProperty, value);
        }
        public static readonly DependencyProperty BindingProperty =
       DependencyProperty.Register(nameof(Binding), typeof(object), typeof(SkGroupDataTrigger), new PropertyMetadata(null));
        public string? BindingPath { get; set; }

        public SkAggregation Aggregation { get; set; }

        public SKOperation Operator { get; set; }
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(SkGroupDataTrigger), new PropertyMetadata(null));

    }
    public class SKGroupMultiTrigger : SKGroupTrigger
    {
        public ObservableCollection<SKGroupCondition> Conditions { get; set; } = new();

    }
    public class SKDataTrigger : SKTrigger
    {
        public object Binding
        {
            get => GetValue(BindingProperty);
            set => SetValue(BindingProperty, value);
        }
        public static readonly DependencyProperty BindingProperty =
       DependencyProperty.Register(nameof(Binding), typeof(object), typeof(SKDataTrigger), new PropertyMetadata(null));

        public string? BindingPath { get; set; }
        public SKOperation Operator { get; set; }
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(SKDataTrigger), new PropertyMetadata(null));


        public override bool Evaluate(object dataContext, ReflectionHelper helper)
        {
            string? actualValue = null;
            Type? valueType = null;

            if (!string.IsNullOrEmpty(BindingPath))
            {
                (actualValue, valueType) = helper.ReadCurrentItemWithTypes(dataContext, BindingPath);
            }
            else if (Binding != null)
            {
                actualValue = Binding?.ToString();
                valueType = Binding?.GetType();
            }

            // If still null, then invalid comparison
            if (actualValue == null || Value == null)
                return false;

            return EvaluateCondition(actualValue, valueType, Value, Operator);
        }
    }

    public class SKGroupToggleSymbol : DependencyObject
    {
        public string? TargetColumns { get; set; }
        public string? Expand { get; set; }
        public string? Collapse { get; set; }
        public bool? ShowGroupDetail { get; set; }
    }
    public class SKMultiTrigger : SKTrigger
    {
        public ObservableCollection<SKCondition> Conditions { get; set; } = new();
        public override bool Evaluate(object dataContext, ReflectionHelper helper)
        {
            foreach (var item in Conditions)
            {
                string? actualValue = null;
                Type? valueType = null;

                if (!string.IsNullOrEmpty(item.BindingPath))
                {
                    (actualValue, valueType) = helper.ReadCurrentItemWithTypes(dataContext, item.BindingPath);
                }
                else if (item.Binding != null)
                {
                    actualValue = item.Binding?.ToString();
                    valueType = item.Binding?.GetType();
                }

                // If still null, then invalid comparison
                if (actualValue == null || item.Value == null)
                    return false;

                var res = EvaluateCondition(actualValue, valueType, item.Value, item.Operator);
                if (!res) return false;
            }
            return true;
        }

    }

    [ContentProperty(nameof(Triggers))]
    public class SKCellTemplate : DependencyObject
    {
        public SkButton? SkButton { get; set; }
        public ObservableCollection<SkButton> SkButtons { get; set; } = new();
        public Func<object, List<SkButton>> DrawButton
        {
            get => (Func<object, List<SkButton>>)GetValue(DrawButtonProperty);
            set => SetValue(DrawButtonProperty, value);
        }

        public static readonly DependencyProperty DrawButtonProperty =
            DependencyProperty.Register(nameof(DrawButton), typeof(Func<object, List<SkButton>>), typeof(SKCellTemplate), new PropertyMetadata(default));


        public ObservableCollection<SKSetter> Setters { get; set; } = new();
        public ObservableCollection<SKTrigger> Triggers { get; set; } = new();
    }

    public class SkButton : DependencyObject
    {

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set { SetValue(NameProperty, value); }
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(nameof(Name), typeof(string), typeof(SkButton), new PropertyMetadata(OnNameChanged));

        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                throw new InvalidOperationException("SkButton.Name is required.");
            }
        }
        public string? Text
        {
            get => (string?)GetValue(TextProperty);
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SkButton), new PropertyMetadata(null));

        public double? Width
        {
            get => (double?)GetValue(WidthProperty);
            set { SetValue(WidthProperty, value); }
        }

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(nameof(Width), typeof(double?), typeof(SkButton), new PropertyMetadata(null));

        public string? BackgroundColor
        {
            get => (string?)GetValue(BackgroundColorProperty);
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(nameof(BackgroundColor), typeof(string), typeof(SkButton), new PropertyMetadata(null));

        public string? BorderColor
        {
            get => (string?)GetValue(BorderColorProperty);
            set { SetValue(BorderColorProperty, value); }
        }

        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register(nameof(BorderColor), typeof(string), typeof(SkButton), new PropertyMetadata(null));

        public string? ForegroundColor
        {
            get => (string?)GetValue(ForegroundColorProperty);
            set { SetValue(ForegroundColorProperty, value); }
        }

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register(nameof(ForegroundColor), typeof(string), typeof(SkButton), new PropertyMetadata(null));

        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(SkButton), new PropertyMetadata(null));

        public double MarginRight
        {
            get => (double)GetValue(MarginRightProperty);
            set { SetValue(MarginRightProperty, value); }
        }

        public static readonly DependencyProperty MarginRightProperty =
            DependencyProperty.Register(nameof(MarginRight), typeof(double), typeof(SkButton), new PropertyMetadata(0.0));


        public double MarginLeft
        {
            get => (double)GetValue(MarginLeftProperty);
            set { SetValue(MarginLeftProperty, value); }
        }

        public static readonly DependencyProperty MarginLeftProperty =
            DependencyProperty.Register(nameof(MarginLeft), typeof(double), typeof(SkButton), new PropertyMetadata(0.0));

        public CellContentAlignment ContentAlignment
        {
            get => (CellContentAlignment)GetValue(ContentAlignmentProperty);
            set => SetValue(ContentAlignmentProperty, value);
        }

        public static readonly DependencyProperty ContentAlignmentProperty =
            DependencyProperty.Register(nameof(ContentAlignment), typeof(CellContentAlignment), typeof(SkButton), new PropertyMetadata(CellContentAlignment.Center));

        public Action<SkButton, object> OnClicked
        {
            get { return (Action<SkButton, object>)GetValue(OnClickedProperty); }
            set { SetValue(OnClickedProperty, value); }
        }

        public static readonly DependencyProperty OnClickedProperty =
            DependencyProperty.Register(nameof(OnClicked), typeof(Action<SkButton, object>), typeof(SkButton), new PropertyMetadata(default));

    }

    [ContentProperty(nameof(Triggers))]
    public class SKGroupCellTemplate
    {
        public ObservableCollection<SKSetter> Setters { get; set; } = new(); // Can be SKSetter or SKMultiSetter
        public ObservableCollection<SKGroupTrigger> Triggers { get; set; } = new(); // Can be SKDataTrigger or SKMultiTrigger
    }

    [ContentProperty(nameof(Triggers))]
    public class SKRowTemplate
    {
        public ObservableCollection<SKSetter> Setters { get; set; } = new(); // Can be SKSetter or SKMultiSetter
        public ObservableCollection<SKTrigger> Triggers { get; set; } = new(); // Can be SKDataTrigger or SKMultiTrigger
    }


    public enum SkStyleProperty
    {
        Background,
        Foreground,
        BorderColor,
        //BorderThickness,
        // ContentAlignment,
        // Format,
    }
    public enum SkAggregation
    {
        None,
        Sum,
        Avg,
        Min,
        Max,
        Count,
        Distinct
    }
    public enum SKOperation
    {
        GreaterThan,
        LessThan,
        Equals,
        NotEquals,
        GreaterThanOrEqual,
        LessThanOrEqual,
    }

    public class SkGridColumnCollection : List<SKGridViewColumn>
    {
    }


    public static class TriggerEvaluator
    {
        //bool Evaluate(SKCondition cond, object rowData)
        //{
        //    var value = ReflectionHelper.GetPropertyValue(rowData, cond.Binding);
        //    return cond.Operator switch
        //    {
        //        SKConditionOperator.Equal => Equals(value, cond.Value),
        //        SKConditionOperator.GreaterThan => Convert.ToDouble(value) > Convert.ToDouble(cond.Value),
        //        SKConditionOperator.LessThan => Convert.ToDouble(value) < Convert.ToDouble(cond.Value),
        //        SKConditionOperator.NotEqual => !Equals(value, cond.Value),
        //        SKConditionOperator.GreaterThanOrEqual => Convert.ToDouble(value) >= Convert.ToDouble(cond.Value),
        //        SKConditionOperator.LessThanOrEqual => Convert.ToDouble(value) <= Convert.ToDouble(cond.Value),
        //        _ => false
        //    };
        //}
        //public static bool EvaluateTrigger(object item, SKCellTrigger trigger)
        //{
        //    if (trigger is SKDataTrigger dt)
        //    {
        //        var val = GetBoundValue(item, dt.Binding);
        //        return EvaluateCondition(val, dt.Condition, dt.Value);
        //    }
        //    else if (trigger is And at)
        //    {
        //        return at.Conditions.All(c => EvaluateCondition(GetBoundValue(item, c.Binding), c.ConditionType, c.Value));
        //    }
        //    else if (trigger is Or ot)
        //    {
        //        return ot.Conditions.Any(c => EvaluateCondition(GetBoundValue(item, c.Binding), c.ConditionType, c.Value));
        //    }
        //    return false;
        //}

        public static object? GetBoundValue(object dataContext, string path)
        {
            if (dataContext == null || string.IsNullOrWhiteSpace(path)) return null;
            var props = path.Split('.');
            object? current = dataContext;
            foreach (var prop in props)
            {
                if (current == null) return null;
                var propInfo = current.GetType().GetProperty(prop);
                if (propInfo == null) return null;
                current = propInfo.GetValue(current);
            }
            return current;
        }

        public static bool EvaluateCondition(object? actualValue, string op, object expectedValue)
        {
            try
            {
                double actual = Convert.ToDouble(actualValue);
                double expected = Convert.ToDouble(expectedValue);

                return op switch
                {
                    ">" => actual > expected,
                    "<" => actual < expected,
                    ">=" => actual >= expected,
                    "<=" => actual <= expected,
                    "=" or "==" => actual == expected,
                    "!=" => actual != expected,
                    _ => false
                };
            }
            catch
            {
                return actualValue?.ToString() == expectedValue?.ToString();
            }
        }
    }
}