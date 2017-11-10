using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChopshopSignin
{
    internal class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action Dirty;

        protected void FirePropertyChanged([CallerMemberName] string propertyName = null)
        {
            FirePropertyChanged(true, propertyName);
        }

        protected void FirePropertyChanged(bool valueChanged, [CallerMemberName] string propertyName = null)
        {
            CheckPropertyNameAndThrow(propertyName);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (valueChanged)
                Dirty?.Invoke();
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            var valueChanged = false;

            // If the property is an IComparable, use the IComparable interface to test them
            if (field is IComparable || value is IComparable)
                valueChanged = IsValueChanged((IComparable)field, (IComparable)value);
            else
                // Otherwise default to same reference
                valueChanged = !SameObject(field, value);

            if (valueChanged)
            {
                field = value;
                FirePropertyChanged(true, propertyName);
            }

            return valueChanged;
        }

        protected bool IsValueChanged<T>(T oldValue, T newValue) where T : IComparable
        {
            if (object.ReferenceEquals(oldValue, null) && !object.ReferenceEquals(newValue, null))
                return true;

            return oldValue.CompareTo(newValue) != 0;
        }

        [Conditional("DEBUG")]
        private void CheckPropertyNameAndThrow(string propertyName)
        {
            if (!IsPropertyInObject(propertyName))
                throw new MissingMemberException(GetType().Name, propertyName);
        }

        private bool SameObject<T>(T previousValue, T newValue)
        {
            return object.ReferenceEquals(previousValue, newValue);
        }

        private bool IsPropertyInObject(string propName)
        {
            if (string.IsNullOrEmpty(propName))
                return true;

            lock (_sync)
            {
                if (!_properties.ContainsKey(this.GetType()))
                    _properties[this.GetType()] = GetPropertySet(this.GetType());
            }

            return _properties[GetType()].Contains(propName);
        }

        private static HashSet<string> GetPropertySet(Type type)
        {
            return new HashSet<string>(type.GetProperties().Select(x => x.Name));
        }

        private static readonly Dictionary<Type, HashSet<string>> _properties = new Dictionary<Type, HashSet<string>>();
        private static readonly object _sync = new object();
    }
}
