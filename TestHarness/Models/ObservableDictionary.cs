using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TestHarness.Models
{
    public class ObservableDictionary<TKey,TValue> : IDictionary<TKey,TValue>,INotifyCollectionChanged,INotifyPropertyChanged
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item.Key, item.Value);
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            OnCollectionChanged(NotifyCollectionChangedAction.Add);
        }

        public void Clear()
        {
            _dictionary.Clear();
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var remove = _dictionary.Remove(item.Key);
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            return remove;
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key,value);
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            OnCollectionChanged(NotifyCollectionChangedAction.Add);
        }

        public bool Remove(TKey key)
        {
            var remove = _dictionary.Remove(key);
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            return remove;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                _dictionary[key] = value;
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Values));
                OnCollectionChanged(NotifyCollectionChangedAction.Add);
            }
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action));
        }
    }
}
