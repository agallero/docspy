namespace DocSpy
{
    public record TRoot(string WebFolder, string Name, string? BuildCommand, 
        string? BuildCommandArguments, string? Editor, string? EditorArguments,
        string? UploadCommand, string? UploadCommandArguments)
    {
        public override string ToString() => Name;
    }
}
