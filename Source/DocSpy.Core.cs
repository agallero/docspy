namespace DocSpy
{
    public record TRoot(string WebFolder, string Name, string? BuildCommand, string? BuildCommandArguments, string? Editor, string? EditorArguments)
    {
        public override string ToString() => Name;
    }
}
