using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace ItemCollage
{
    class Options : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        private string settingsFile;
        private bool dirty;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool TopMost
        {
            get => GetValueOrDefault(true);
            set => SetValue(value);
        }

        public bool CheckForUpdates
        {
            get => GetValueOrDefault(true);
            set => SetValue(value);

        }

        public bool ItemToClipboard
        {
            get => GetValueOrDefault(true);
            set => SetValue(value);
        }

        public bool CollageToClipboard
        {
            get => GetValueOrDefault(true);
            set => SetValue(value);
        }

        /// <summary>
        ///     Initialises a new instance of the Options class
        ///     with the default options.
        /// </summary>
        public Options()
            : this(Path.Combine(GetApplicationDirectory(), "settings.ini"))
        { }

        /// <summary>
        ///     Initialises a new instance of the Options class with the
        ///     options given in a file.
        /// </summary>
        /// <param name="opt">
        ///     A settings file, containing a <code>key=value</code> pair
        ///     per line.
        /// </param>
        public Options(string fileName)
        {
            settingsFile = fileName;
            try
            {
                Load(File.ReadAllLines(fileName));
            }
            catch
            {
                settingsFile = null;
                dirty = true;
            }
        }

        private T GetValueOrDefault<T>(T @default, [CallerMemberName] string key = null) =>
            _values.TryGetValue(key, out var value) && value is T t ? t : @default;

        private void SetValue(object value, [CallerMemberName] string key = null)
        {
            if (!_values.ContainsKey(key) || _values[key] != value)
            {
                dirty = true;
                _values[key] = value;
                NotifyPropertyChanged(key);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Load(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                try
                {
                    var index = line.IndexOf('=');
                    var key = line.Substring(0, index);
                    var val = line.Substring(index + 1);

                    switch (key)
                    {
                        // conflate handling for all boolean properties
                        case "TopMost":
                            TopMost = Convert.ToBoolean(val);
                            break;
                        case "CheckForUpdates":
                            CheckForUpdates = Convert.ToBoolean(val);
                            break;
                        case "ItemToClipboard":
                            ItemToClipboard = Convert.ToBoolean(val);
                            break;
                        case "CollageToClipboard":
                            CollageToClipboard = Convert.ToBoolean(val);
                            break;
                    }
                }
                catch { }
            }
        }

        private static string GetApplicationDirectory()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var foldername = Path.Combine(appdata, "D3IC");
            return foldername;
        }

        public void Save()
        {
            if (!dirty)
            {
                return;
            }

            // write to the default settings file if none was read
            if (settingsFile == null)
            {
                var applicationDir = GetApplicationDirectory();
                Directory.CreateDirectory(applicationDir);
                settingsFile = Path.Combine(applicationDir,
                    "settings.ini");
            }

            using (var file = new StreamWriter(File.Open(settingsFile,
                FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8))
            {
                foreach (var entry in _values)
                {
                    file.WriteLine("{0}={1}", entry.Key, entry.Value);
                }
            }
        }
    }
}
