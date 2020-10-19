using System.Collections.ObjectModel;

namespace FileManager
{
    /// <summary>
    /// Реализует множество элементов, которое сохраняет порядок их вставки.
    /// </summary>
    /// <typeparam name="T">Тип элементов.</typeparam>
    public class OrderedSet<T> : KeyedCollection<T, T>
    {
        protected override T GetKeyForItem(T item)
        {
            return item;
        }
    }
}