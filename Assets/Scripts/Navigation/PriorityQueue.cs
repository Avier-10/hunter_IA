using System;

public class PriorityQueue<T>
{
    private T[] _items;
    private float[] _priorities;
    private int _count;

    public int Count => _count;

    public PriorityQueue(int capacity = 64)
    {
        _items = new T[Math.Max(1, capacity)];
        _priorities = new float[_items.Length];
    }

    public void Enqueue(T item, float priority)
    {
        if (_count == _items.Length) Grow();

        int i = _count++;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_priorities[parent] <= priority) break;

            _items[i] = _items[parent];
            _priorities[i] = _priorities[parent];
            i = parent;
        }
        _items[i] = item;
        _priorities[i] = priority;
    }

    public T Dequeue()
    {
        if (_count == 0) throw new InvalidOperationException("PriorityQueue is empty.");

        T result = _items[0];
        _count--;

        T last = _items[_count];
        float lastPriority = _priorities[_count];
        _items[_count] = default;

        if (_count > 0)
        {
            int i = 0;
            while (true)
            {
                int child = i * 2 + 1;
                if (child >= _count) break;

                if (child + 1 < _count && _priorities[child + 1] < _priorities[child]) child++;
                if (_priorities[child] >= lastPriority) break;

                _items[i] = _items[child];
                _priorities[i] = _priorities[child];
                i = child;
            }
            _items[i] = last;
            _priorities[i] = lastPriority;
        }

        return result;
    }

    public void Clear()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    private void Grow()
    {
        Array.Resize(ref _items, _items.Length * 2);
        Array.Resize(ref _priorities, _items.Length);
    }
}
