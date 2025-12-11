using System.Collections;
using System.Collections.Generic;

namespace Xuch.Framework.Utils
{
    public sealed partial class LinkedListPro<T>
    {
        /// <summary>
        /// 对 LinckedList.Enumerator 的封装
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private LinkedList<T>.Enumerator _enumerator;

            internal Enumerator(LinkedList<T> linkedList)
            {
                _enumerator = linkedList!.GetEnumerator();
            }

            public T Current => _enumerator.Current;

            readonly object IEnumerator.Current => (_enumerator as IEnumerator).Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            readonly void IEnumerator.Reset()
            {
                (_enumerator as IEnumerator).Reset();
            }
        }
    }
}