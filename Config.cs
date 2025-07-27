using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocSpy
{
    internal class Config
    {
        public TRoot[] Roots { get; private set; } = [];
        public static Config Instance { get; } = new Config();

        public bool LoadConfigs()
        {
            string? FileName = ConfigFileName();

            if (!File.Exists(FileName))
            {
                return false;
            }

            IConfigurationRoot? cfg = null;
            try
            {
                cfg = new ConfigurationBuilder().AddIniFile(FileName).Build();
            }
            catch (Exception)
            {
                return false;
            }

            var Sections = cfg.GetChildren().ToArray();

            var Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var Config = Sections.FirstOrDefault(s => s.Key.Equals("_config_", StringComparison.OrdinalIgnoreCase));
            if (Config != null)
            {
                LoadConfigsVariables(Config, Variables);
            }

            Roots = [];
            foreach (var section in Sections)
            {
                if (section.Key.Equals("_config_", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip the _config_ section, it is not a root.
                    continue;
                }
                var folder = ReplaceVariables(section["source"], Variables);
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }
                var Command = ReplaceVariables(section["command"], Variables);
                var CommandArguments = ReplaceVariables(section["command-arguments"], Variables);

                var Editor = ReplaceVariables(section["editor"], Variables);
                var EditorArguments = ReplaceVariables(section["editor-arguments"], Variables);

                string? name = ReplaceVariables(section.Key, Variables);
                if (name == null || name.Length == 0)
                {
                    name = Guid.NewGuid().ToString();
                }
                Roots = [.. Roots, new TRoot(folder, name, Command, CommandArguments, Editor, EditorArguments)];
            }

            return Roots?.Length > 0;
        }

        private static string ConfigFileName()
        {
            return Settings.Instance.ViewModel.ConfigFilePath;
        }

        private static void LoadConfigsVariables(IConfigurationSection config, Dictionary<string, string> variables)
        {
            foreach (var child in config.GetChildren())
            {
                variables[child.Key] = child.Value ?? "";
            }
        }

        private static string? ReplaceVariables(string? key, Dictionary<string, string> variables)
        {
            if (key == null)
            {
                return null;
            }

            foreach (var variable in variables)
            {
                key = key.Replace("{{" + variable.Key + "}}", variable.Value, StringComparison.OrdinalIgnoreCase);
            }
            return key;
        }

    }
}
