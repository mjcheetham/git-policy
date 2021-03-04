using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mjcheetham.Git.Policy
{
    public class Profile
    {
        [JsonPropertyName("policies")]
        public ICollection<string> Policies { get; set; } = new List<string>();
    }

    public class Policy
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("desc")]
        public string? Description { get; set; }

        [JsonPropertyName("cfg")]
        public ICollection<GitConfiguration> Configuration { get; set; } = new List<GitConfiguration>();

        [JsonPropertyName("vmin")]
        public string? MinimumVersion { get; set; }

        [JsonPropertyName("vmax")]
        public string? MaximumVersion { get; set; }

        public Policy(string id, string? description)
        {
            Id = id;
            Description = description;
        }
    }

    public class GitConfiguration
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        public GitConfiguration(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
