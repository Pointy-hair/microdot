﻿using Gigya.Microdot.Interfaces.Configuration;
using Gigya.Microdot.LanguageExtensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Gigya.Microdot.Hosting.Configuration
{
    public sealed class LegacyFileHostConfigurationSource : IHostConfigurationSource
    {
        // TODO: Do we need Zone and Region, or can it be abstracted away in favor of generic configuration properties
        public string Zone { get; }

        public string Region { get; }

        public string DeploymentEnvironment { get; }

        public string ConsulAddress { get; }

        public CurrentApplicationInfo ApplicationInfo { get; }

        public DirectoryInfo ConfigRoot { get; }

        public FileInfo LoadPathsFile { get; }

        public IDictionary<string, string> CustomKeys { get; }

        public LegacyFileHostConfigurationSource(string path)
        {
            var entries =
                ReadFromJsonFile(path)
                .ToDictionary(x => x.Key);

            Zone = get("ZONE") ?? get("DC");
            Region = get("REGION");
            DeploymentEnvironment = get("ENV");
            ConsulAddress = get("CONSUL");

            ConfigRoot = get("GIGYA_CONFIG_ROOT")?.To(x => new DirectoryInfo(x));
            LoadPathsFile = get("GIGYA_CONFIG_PATHS_FILE")?.To(x => new FileInfo(x));

            CustomKeys = entries.ToDictionary(x => x.Key, x => x.Value.Value);

            string get(string key)
            {
                return GetOrNull(entries, key);
            }
        }

        private string GetOrNull(Dictionary<string, Entry> entries, string key)
        {
            if (entries.TryGetValue(key, out var val))
                return val.Value;
            return null;
        }

        private sealed class Entry
        {
            public string Key { get; }
            public string Value { get; }

            public Entry(string key, string value)
            {
                this.Key = key.NullWhenEmpty() ?? throw new ArgumentNullException(nameof(key));
                this.Value = value;
            }
        }

        private static IEnumerable<Entry> ReadFromJsonFile(string path)
        {
            JObject envVarsObject;

            try
            {
                var text = File.ReadAllText(path);

                if (string.IsNullOrEmpty(text))
                    return Enumerable.Empty<Entry>();

                envVarsObject = JObject.Parse(text);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException($"Missing or invalid configuration file: {path}", ex);
            }

            if (envVarsObject == null)
                return Enumerable.Empty<Entry>();

            return
                envVarsObject
                .Properties()
                .Where(a => a.HasValues)
                .Select(x => new Entry(x.Name.ToUpperInvariant(), x.Value.Value<string>()))
                .ToArray();
        }
    }
}