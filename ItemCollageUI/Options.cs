using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ItemCollage
{
    class Options : INotifyPropertyChanged
    {
        bool _topMost;
        bool _checkForUpdates;
        bool _itemToClipboard;
        bool _collageToClipboard;

        private string settingsFile;
        private bool dirty = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool TopMost
        {
            get { return _topMost; }
            set
            {
                if (value != _topMost)
                {
                    dirty = true;
                    _topMost = value;
                    NotifyPropertyChanged("TopMost");
                }
            }
        }

        public bool CheckForUpdates
        {
            get { return _checkForUpdates; }
            set
            {
                if (value != _checkForUpdates)
                {
                    dirty = true;
                    _checkForUpdates = value;
                    NotifyPropertyChanged("CheckForUpdates");
                }
            }
        }

        public bool ItemToClipboard
        {
            get { return _itemToClipboard; }
            set
            {
                if (value != _itemToClipboard)
                {
                    dirty = true;
                    _itemToClipboard = value;
                    NotifyPropertyChanged("ItemToClipboard");
                }
            }
        }

        public bool CollageToClipboard
        {
            get { return _collageToClipboard; }
            set
            {
                if (value != _collageToClipboard)
                {
                    dirty = true;
                    _collageToClipboard = value;
                    NotifyPropertyChanged("CollageToClipboard");
                }
            }
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
            TopMost = true;
            CheckForUpdates = true;
            ItemToClipboard = true;
            CollageToClipboard = true;

            settingsFile = fileName;
            try
            {
                Load(File.ReadAllText(fileName));
            }
            catch
            {
                settingsFile = null;
                dirty = true;
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Load(string opt)
        {
            var lines = opt.Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                try
                {
                    var index = line.IndexOf('=');
                    var key = line.Substring(0, index);
                    var val = line.Substring(index + 1);

                    switch (key.ToLowerInvariant())
                    {
                        // conflate handling for all boolean properties
                        case "topmost":
                        case "checkforupdates":
                        case "itemtoclipboard":
                        case "collagetoclipboard":
                            typeof(Options)
                            .GetProperties()
                            .First(p => p.Name.Equals(key,
                                StringComparison.InvariantCultureIgnoreCase))
                            .SetValue(this, bool.Parse(val), null);
                            break;
                    }
                }
                catch { }
            }
        }

        private static string GetApplicationDirectory()
        {
            var appdata = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            var foldername = Path.Combine(appdata, "D3IC");
            return foldername;
        }

        public void Save()
        {
            if (!dirty)
                return;

            // write to the default settings file if none was read
            if (settingsFile == null)
            {
                var applicationDir = GetApplicationDirectory();
                Directory.CreateDirectory(applicationDir);
                settingsFile = Path.Combine(applicationDir,
                    "settings.ini");
            }

            var propertiesToIgnore = new List<string> { };

            var propertiesToSave =
                typeof(Options)
                .GetProperties()
                //.Where(p =>
                //    !propertiesToIgnore.Contains(p.Name.ToLowerInvariant()))
                .ToList();

            using (var file = new StreamWriter(File.Open(settingsFile,
                FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8))
            {
                foreach (var prop in propertiesToSave)
                    file.WriteLine("{0}={1}", prop.Name, prop.GetValue(this, null));
            }
        }
    }
}
