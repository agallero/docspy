using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace DocSpy
{
    public partial class UiLogger : ILogger
    {
        private readonly string _name;
        private readonly IExternalScopeProvider _scopeProvider;
        private readonly Action<string> _logAction; // Action to update the UI

        public UiLogger(string name, IExternalScopeProvider scopeProvider, Action<string> logAction)
        {
            _name = name;
            _scopeProvider = scopeProvider;
            _logAction = logAction;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true; // Enable all log levels for UI display

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = formatter(state, exception);
            _logAction?.Invoke($"{DateTime.Now:HH:mm:ss} [{logLevel}] {_name}: {message}");
        }
    }

    public partial class UiLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, UiLogger> _loggers = new();
        private readonly IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
        private readonly Action<string> _logAction;

        public UiLoggerProvider(Action<string> logAction)
        {
            _logAction = logAction;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new UiLogger(name, _scopeProvider, _logAction));
        }

        public void Dispose() => _loggers.Clear();
    }
}
