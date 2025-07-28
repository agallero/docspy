using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocSpy.Source
{
    public record TRoot(string WebFolder, string Name, string? BuildCommand, string? BuildCommandArguments, string? Editor, string? EditorArguments)
    {
        public override string ToString() => Name;
    }
}
