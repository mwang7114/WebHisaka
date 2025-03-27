using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebHasaki.DesignPattern
{

    public interface IIterator
    {
        object First();
        object Next();
        bool IsDone { get; }
        object CurrentItem { get; }

        void ForEachItem(Action<object> func);
    }
    public class ArrayListIterator : IIterator
    {
        private ArrayList _arrayList;
        private int current = 0;
        private int step = 1;

        public ArrayListIterator(ArrayList arrayList)
        {
            _arrayList = arrayList;
        }

        public bool IsDone
        {
            get { return current >= _arrayList.Count; }
        }

        public object CurrentItem => _arrayList[current];

        public object First()
        {
            current = 0;
            if (_arrayList.Count > 0)
                return _arrayList[current];
            return null;
        }

        public object Next()
        {
            current += step;
            if (!IsDone)
                return _arrayList[current];
            else
                return null;
        }

        public void ForEachItem(Action<object> func)
        {
            int i = 0;
            while (i < _arrayList.Count)
            {
                func(_arrayList[i++]);
            }
        }
    }
}