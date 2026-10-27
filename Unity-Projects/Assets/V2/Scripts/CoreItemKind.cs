/// <summary>
/// The two Core Item pieces needed to escape. Both drop independently from
/// gather trips - neither is a precondition for the other.
/// </summary>
public enum CoreItemKind
{
    /// <summary>The sealed box. Only the matching key opens it.</summary>
    A,
    /// <summary>The key. Its colour always matches the box found this run.</summary>
    B
}

/// <summary>
/// Which box/key pair a run rolled. The box decides the key: an orange box is
/// opened by the orange key, a yellow box by the grey key - so the key the
/// player finds is always the one that fits.
/// </summary>
public enum CoreItemVariant
{
    /// <summary>Orange box + orange key.</summary>
    Orange,
    /// <summary>Yellow box + grey key.</summary>
    Yellow
}
