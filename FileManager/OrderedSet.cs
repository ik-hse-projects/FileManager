using System.Collections.ObjectModel;

namespace FileManager
{
    public class OrderedSet<T> : KeyedCollection<T, T>
    {
        protected override T GetKeyForItem(T item)
        {
            return item;
        }
    }
}