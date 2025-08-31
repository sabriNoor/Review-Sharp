using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ReviewSharpApp.Tests.TestHelpers
{
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public IEnumerable<string> Keys => _store.Keys;
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public void Clear() => _store.Clear();
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value)
        {
            bool found = _store.TryGetValue(key, out var temp);
            value = temp!; // ISession expects non-nullable, so use null-forgiving operator
            return found;
        }
        public void SetString(string key, string value) => Set(key, System.Text.Encoding.UTF8.GetBytes(value));
        public string? GetString(string key) => _store.TryGetValue(key, out var value) ? System.Text.Encoding.UTF8.GetString(value) : null;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}