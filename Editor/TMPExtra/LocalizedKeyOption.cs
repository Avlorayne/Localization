#if UNITY_EDITOR

internal sealed class LocalizedKeyOption
{
    public string NamespaceId;
    public string Key;
    public string Preview;
    public string Comment;

    public string Placeholder => $"<{NamespaceId}|{Key}>";
}

#endif
