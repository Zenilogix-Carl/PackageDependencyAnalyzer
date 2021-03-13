using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private class ObservableKey : ICollection<TKey>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            private readonly IDictionary<TKey, TValue> _dictionary;
            public int Count => _dictionary.Keys.Count;

            public bool IsReadOnly => _dictionary.Keys.IsReadOnly;

            public event NotifyCollectionChangedEventHandler CollectionChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            public ObservableKey(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public void Add(TKey item)
            {
                _dictionary.Keys.Add(item);
            }

            public void Clear()
            {
                _dictionary.Keys.Clear();
            }

            public bool Contains(TKey item)
            {
                return _dictionary.Keys.Contains(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                _dictionary.Keys.CopyTo(array, arrayIndex);
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return _dictionary.Keys.GetEnumerator();
            }

            public bool Remove(TKey item)
            {
                return _dictionary.Keys.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _dictionary.Keys.GetEnumerator();
            }

            public void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
            {
                CollectionChanged?.Invoke(this, args);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }

            public void OnPropertyChanged(PropertyChangedEventArgs args)
            {
                PropertyChanged?.Invoke(this, args);
            }
        }

        private class ObservableValue : ICollection<TValue>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            private readonly IDictionary<TKey, TValue> _dictionary;
            public int Count => _dictionary.Values.Count;

            public bool IsReadOnly => _dictionary.Values.IsReadOnly;

            public event NotifyCollectionChangedEventHandler CollectionChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            public ObservableValue(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public void Add(TValue item)
            {
                _dictionary.Values.Add(item);
            }

            public void Clear()
            {
                _dictionary.Values.Clear();
            }

            public bool Contains(TValue item)
            {
                return _dictionary.Values.Contains(item);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                _dictionary.Values.CopyTo(array, arrayIndex);
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return _dictionary.Values.GetEnumerator();
            }

            public bool Remove(TValue item)
            {
                return _dictionary.Values.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _dictionary.Values.GetEnumerator();
            }

            public void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
            {
                CollectionChanged?.Invoke(this, args);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }

            public void OnPropertyChanged(PropertyChangedEventArgs args)
            {
                PropertyChanged?.Invoke(this, args);
            }
        }

        private class ObservableHashSet<T> : HashSet<T>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            public void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
            {
                CollectionChanged?.Invoke(this, args);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
        }

        private readonly IDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();
        private readonly ObservableKey _keys;
        private readonly ObservableValue _values;
        public Dispatcher Dispatcher { get; set; }

        public ObservableDictionary()
        {
            _keys = new ObservableKey(_dictionary);
            _values = new ObservableValue(_dictionary);
        }

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
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        public void Clear()
        {
            _dictionary.Clear();
            OnCollectionReset();
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
            if (remove)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            }
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
            _dictionary.Add(key, value);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key,value));
        }

        public bool Remove(TKey key)
        {
            var wasFound = _dictionary.TryGetValue(key, out var value);
            var remove = _dictionary.Remove(key);
            if (wasFound && remove)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value));
            }
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
                OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        public ICollection<TKey> Keys => _keys;

        public ICollection<TValue> Values => _values;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (Dispatcher == null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        private void OnCollectionReset()
        {
            if (Dispatcher == null)
            {
                var notifyCollectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                CollectionChanged?.Invoke(this, notifyCollectionChangedEventArgs);
                _keys.OnCollectionChanged(notifyCollectionChangedEventArgs);
                _values.OnCollectionChanged(notifyCollectionChangedEventArgs);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    var notifyCollectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    CollectionChanged?.Invoke(this, notifyCollectionChangedEventArgs);
                    _keys.OnCollectionChanged(notifyCollectionChangedEventArgs);
                    _values.OnCollectionChanged(notifyCollectionChangedEventArgs);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                });
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey,TValue> kvp)
        {
            if (Dispatcher == null)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, kvp));
                if (action == NotifyCollectionChangedAction.Remove)
                {
                    // Can't efficiently get position for removed item, so just reset
                    _keys.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    _values.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else
                {
                    _keys.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, kvp.Key));
                    _values.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, kvp.Value));
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, kvp));
                    if (action == NotifyCollectionChangedAction.Remove)
                    {
                        // Can't efficiently get position for removed item, so just reset
                        _keys.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        _values.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
                    else
                    {
                        _keys.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, kvp.Key));
                        _values.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, kvp.Value));
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                });
            }
        }
    }
}
