using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.System
{
    public class IndexableStack<T>
    {
        private List<T> _values = new List<T>();
        private int _pos = 0;

        public T this[int index]
        {
            get { return _values[index]; }
        }
        
        public T Pop()
        {
            _pos--;
            if (_pos < 0)
                throw new Exception("Stack underflow, attempt to pop an empty stack");

            return _values[_pos];
        }

        public void Push(T value)
        {
            _values.Add(value);
            _pos++;
        }

        public void Load(T[] initialValues)
        {
            _values = new List<T>(initialValues);
            _pos = _values.Count;
        }
    }
}
