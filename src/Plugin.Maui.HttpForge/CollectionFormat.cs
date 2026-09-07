namespace Plugin.Maui.HttpForge;

/// <summary>
/// How a collection query parameter is written on the wire.
/// </summary>
public enum CollectionFormat
{
    /// <summary><c>ages=1&amp;ages=2</c></summary>
    Multi = 0,

    /// <summary><c>ages=1,2</c></summary>
    Csv = 1,

    /// <summary><c>ages=1 2</c></summary>
    Ssv = 2,

    /// <summary><c>ages=1\t2</c></summary>
    Tsv = 3,

    /// <summary><c>ages=1|2</c></summary>
    Pipes = 4
}
