using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Annotations;
using LogWatch.Messages;

namespace LogWatch.Features.Search {
    public sealed class SearchViewModel : ViewModelBase {
        private int count;
        private int current;
        private bool includeDebug;
        private bool includeError;
        private bool includeFatal;
        private bool includeInfo;
        private bool includeTrace;
        private bool includeWarn;
        private bool isActive;
        private bool matchCase;
        private string query;
        private bool useRegex;

        public SearchViewModel() {
            this.SearchCommand = new RelayCommand(this.Search, this.CanSearch);
            this.ResetCommand = new RelayCommand(this.Reset);

            this.ResetLevels();
        }

        private void ResetLevels() {
            this.includeTrace = true;
            this.includeDebug = true;
            this.includeInfo = true;
            this.includeWarn = true;
            this.includeError = true;
            this.includeFatal = true;
        }

        public bool IncludeTrace {
            get { return this.includeTrace; }
            set {
                if (this.CheckIncludeLevel(LogLevel.Trace, value))
                    this.Set(ref this.includeTrace, value);
            }
        }

        public bool IncludeDebug {
            get { return this.includeDebug; }
            set {
                if (this.CheckIncludeLevel(LogLevel.Debug, value))
                    this.Set(ref this.includeDebug, value);
            }
        }

        public bool IncludeInfo {
            get { return this.includeInfo; }
            set {
                if (this.CheckIncludeLevel(LogLevel.Info, value))
                    this.Set(ref this.includeInfo, value);
            }
        }

        public bool IncludeWarn {
            get { return this.includeWarn; }
            set {
                if (this.CheckIncludeLevel(LogLevel.Warn, value))
                    this.Set(ref this.includeWarn, value);
            }
        }

        public bool IncludeError {
            get { return this.includeError; }
            set {
                if (this.CheckIncludeLevel(LogLevel.Error, value))
                    this.Set(ref this.includeError, value);
            }
        }

        public bool IncludeFatal {
            get { return this.includeFatal; }
            set {
                if (this.CheckIncludeLevel(LogLevel.Fatal, value))
                    this.Set(ref this.includeFatal, value);
            }
        }

        public int Count {
            get { return this.count; }
            set { this.Set(ref this.count, value); }
        }

        public int Current {
            get { return this.current; }
            set { this.Set(ref this.current, value); }
        }

        public string Query {
            get { return this.query; }
            set {
                this.Set(ref this.query, value);
                this.SearchCommand.RaiseCanExecuteChanged();
            }
        }

        private bool IsFilterActive {
            get {
                return (!string.IsNullOrEmpty(this.query) && this.query.Length >= 3) ||
                       !(this.includeTrace &&
                         this.includeDebug &&
                         this.includeInfo &&
                         this.includeWarn &&
                         this.includeError &&
                         this.includeFatal);
            }
        }

        public bool IsActive {
            get { return this.isActive; }
            set { this.Set(ref this.isActive, value); }
        }

        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand ResetCommand { get; private set; }

        public bool UseRegex {
            get { return this.useRegex; }
            set { this.Set(ref this.useRegex, value); }
        }

        public bool MatchCase {
            get { return this.matchCase; }
            set { this.Set(ref this.matchCase, value); }
        }

        private bool CheckIncludeLevel(LogLevel level, bool value) {
            var stateByLevel = new Dictionary<LogLevel, bool> {
                {LogLevel.Trace, this.includeTrace},
                {LogLevel.Debug, this.includeDebug},
                {LogLevel.Info, this.includeInfo},
                {LogLevel.Warn, this.includeWarn},
                {LogLevel.Error, this.includeError},
                {LogLevel.Fatal, this.includeFatal},
            };

            return value || stateByLevel.Where(x => x.Key != level).Any(x => x.Value);
        }

        private void Reset() {
            this.Query = string.Empty;
            this.ResetLevels();
            this.MessengerInstance.Send(new RecordFilterChangedMessage(null));
        }

        private bool CanSearch() {
            return string.IsNullOrEmpty(this.query) || this.query.Length >= 3;
        }

        private void Search() {
            if (!this.IsFilterActive) {
                this.MessengerInstance.Send(new RecordFilterChangedMessage(null));
                return;
            }

            Regex regex = null;

            if (!string.IsNullOrEmpty(this.query))
                regex = new Regex(
                    this.UseRegex ? this.query : Regex.Escape(this.query),
                    RegexOptions.Compiled | (this.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase));

            Predicate<Record> predicate =
                record =>
                this.IsMatchLevel(record.Level) &&
                (IsMatch(regex, record.Message) ||
                 IsMatch(regex, record.Exception) ||
                 IsMatch(regex, record.Logger));

            this.MessengerInstance.Send(new RecordFilterChangedMessage(predicate));
        }

        private bool IsMatchLevel(LogLevel? level) {
            switch (level) {
                case LogLevel.Trace:
                    return this.includeTrace;
                case LogLevel.Debug:
                    return this.includeDebug;
                case LogLevel.Info:
                    return this.includeInfo;
                case LogLevel.Warn:
                    return this.includeWarn;
                case LogLevel.Error:
                    return this.includeError;
                case LogLevel.Fatal:
                    return this.includeFatal;
            }

            return false;
        }

        private static bool IsMatch(Regex regex, string value) {
            if (regex == null)
                return true;

            if (string.IsNullOrEmpty(value))
                return false;

            return regex.IsMatch(value);
        }

        [NotifyPropertyChangedInvocator]
        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }
    }
}