using System;
using System.Collections;
using System.Collections.Generic;

namespace XuchFramework.Core.Utils
{
    /// <summary>
    /// Implements a linked list with node pooling to reduce memory allocations and garbage collection for improved performance
    /// </summary>
    public sealed partial class LinkedListPro<T> : ICollection<T>, ICollection
    {
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

        private readonly LinkedList<T> _linkedList = new();
        private readonly Queue<LinkedListNode<T>> _nodePool = new();

        public int Count => _linkedList.Count;

        public LinkedListNode<T> First => _linkedList.First;

        public LinkedListNode<T> Last => _linkedList.Last;

        bool ICollection<T>.IsReadOnly => (_linkedList as ICollection<T>).IsReadOnly;

        bool ICollection.IsSynchronized => (_linkedList as ICollection).IsSynchronized;

        object ICollection.SyncRoot => (_linkedList as ICollection).SyncRoot;

        void ICollection<T>.Add(T value)
        {
            AddLast(value);
        }

        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddAfter(node, newNode);
            return newNode;
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            _linkedList.AddAfter(node, newNode);
        }

        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddBefore(node, newNode);
            return newNode;
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            _linkedList.AddBefore(node, newNode);
        }

        public LinkedListNode<T> AddFirst(T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddFirst(newNode);
            return newNode;
        }

        public void AddFirst(LinkedListNode<T> node)
        {
            _linkedList.AddFirst(node);
        }

        public LinkedListNode<T> AddLast(T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddLast(newNode);
            return newNode;
        }

        public void AddLast(LinkedListNode<T> node)
        {
            _linkedList.AddLast(node);
        }

        public void Clear()
        {
            LinkedListNode<T> current = _linkedList.First;
            while (current is not null)
            {
                ReleaseNode(current);
                current = current.Next;
            }

            _linkedList.Clear();
        }

        /// <summary>
        /// Clears the linked list and also clears the node pool
        /// </summary>
        public void ClearEntirely()
        {
            Clear();
            _nodePool.Clear();
        }

        public bool Contains(T value)
        {
            return _linkedList.Contains(value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _linkedList.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            (_linkedList as ICollection).CopyTo(array, index);
        }

        public LinkedListNode<T> Find(T value)
        {
            return _linkedList.Find(value);
        }

        public LinkedListNode<T> FindLast(T value)
        {
            return _linkedList.FindLast(value);
        }

        public bool Remove(T value)
        {
            LinkedListNode<T> node = _linkedList.Find(value);
            if (node is null)
            {
                return false;
            }

            ReleaseNode(node);
            _linkedList.Remove(node);
            return true;
        }

        public void Remove(LinkedListNode<T> node)
        {
            ReleaseNode(node);
            _linkedList.Remove(node);
        }

        public void RemoveFirst()
        {
            LinkedListNode<T> node = _linkedList.First;
            ReleaseNode(node);
            _linkedList.RemoveFirst();
        }

        public void RemoveLast()
        {
            LinkedListNode<T> node = _linkedList.Last;
            ReleaseNode(node);
            _linkedList.RemoveLast();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_linkedList);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private LinkedListNode<T> AcquireNode(T value)
        {
            if (_nodePool.Count > 0)
            {
                LinkedListNode<T> node = _nodePool.Dequeue();
                node.Value = value;
                return node;
            }
            else
            {
                return new LinkedListNode<T>(value);
            }
        }

        private void ReleaseNode(LinkedListNode<T> node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node), $"ReleaseNode Failed: Node {nameof(node)} is null.");
            }

            node.Value = default;
            _nodePool.Enqueue(node);
        }
    }
}