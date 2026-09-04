using System.Collections.Immutable;

namespace Plugin.Maui.HttpForge.Generator;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array.IsDefault ? ImmutableArray<T>.Empty : array;
    }

    public EquatableArray(IEnumerable<T> items)
    {
        _array = items.ToImmutableArray();
    }

    public ImmutableArray<T> AsImmutableArray() => _array;

    public int Length => _array.Length;

    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_array.Length != other._array.Length)
            return false;

        for (var i = 0; i < _array.Length; i++)
        {
            if (!_array[i].Equals(other._array[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in _array)
                hash = (hash * 31) + (item?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
