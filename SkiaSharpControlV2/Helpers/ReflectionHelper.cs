
using System.Linq.Expressions;

namespace SkiaSharpControlV2.Helpers
{
    public class ReflectionHelper
    {
        private  readonly Dictionary<(Type, string), Func<object, object?>> _getterCache = new();

        /// <summary>
        /// Reads a property from currentItem using fast compiled reflection and returns value and type.
        /// </summary>
        internal  (string? Value, Type? Type) ReadCurrentItemWithTypes(object? currentItem, string propertyName)
        {
            if (currentItem == null || string.IsNullOrWhiteSpace(propertyName))
                return (null, null);

            var type = currentItem.GetType();
            var key = (type, propertyName);

            if (!_getterCache.TryGetValue(key, out var getter))
            {
                try
                {
                    var param = Expression.Parameter(typeof(object), "obj");
                    var castedObj = Expression.Convert(param, type);
                    var property = Expression.PropertyOrField(castedObj, propertyName);
                    var convert = Expression.Convert(property, typeof(object));
                    getter = Expression.Lambda<Func<object, object?>>(convert, param).Compile();

                    _getterCache[key] = getter;
                }
                catch
                {
                    return (null, null);
                }
            }

            try
            {
                var result = getter(currentItem);
                return (result?.ToString(), result?.GetType());
            }
            catch
            {
                return (null, null);
            }
        }
        public object GetPropValue(object obj, string prop)
        {
            var type = obj.GetType();
            var key = (type, prop);
            if (!_getterCache.TryGetValue(key, out var getter))
            {
                var param = Expression.Parameter(typeof(object));
                var body = Expression.Property(Expression.Convert(param, obj.GetType()), prop);
                var convert = Expression.Convert(body, typeof(object));
                getter = Expression.Lambda<Func<object, object>>(convert, param).Compile();
                _getterCache[key] = getter;
            }
            return getter(obj);
        }
    }
}
