using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Linq;

namespace Storage.Api.Db;

internal static class QueryableExt
{
    /// <summary>
    /// Если условие истинно, то применяет фильтр
    /// </summary>
    /// <param name="source">Запрос</param>
    /// <param name="condition">Условие</param>
    /// <param name="predicate">Фильтр</param>
    /// <returns>Либо исходный запрос, либо с фильтром (если condition = true)</returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? source.Where(predicate) : source;
    
    /// <summary>
    /// Если условие истинно, то добавляет setter
    /// </summary>
    /// <param name="source">Запрос</param>
    /// <param name="condition">Условие</param>
    /// <param name="extract">Селектор обновляемого свойства</param>
    /// <param name="funcValue">Функция, которая возвращает новое значение свойства</param>
    /// <returns>Либо исходный запрос, либо с добавленным setter-ом (если condition = true)</returns>
    public static IUpdatable<T> SetIf<T, TV>(this IUpdatable<T> source, bool condition,  Expression<Func<T,TV>> extract, Func<TV> funcValue) =>
        condition ? source.Set(extract, funcValue()) : source;
}