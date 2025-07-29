using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace DocSpy
{
    internal class Settings
    {
        public static Settings Instance { get; } = new Settings();
        public SettingsViewModel ViewModel { get; } = new();

    }

    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        readonly IPropertySet Values = Microsoft.Windows.Storage.ApplicationData.GetDefault().LocalSettings.Values;

        public string ConfigFilePath
        {
            get
            {
                return Get("ConfigFilePath", string.Empty);
            }
            set
            {
                Set("ConfigFilePath", value);
            }
        }
        public bool HasConfigFilePath
        {
            get
            {
                return Get("ConfigFilePath", string.Empty) == string.Empty;
            }
        }
        public bool ConfigFilePathExists
        {
            get
            {
                return File.Exists(Get("ConfigFilePath", string.Empty));
            }
        }

        public GridLength ButtonPanelWidth
        {
            get
            {
                double Pix = Get("ButtonPanelWidth", 120.0);
                return new GridLength(Pix, GridUnitType.Pixel);
            }
            set
            {
                Set("ButtonPanelWidth", value.Value);
            }
        }

        public string BrowserUrl
        {
            get
            {
                return Get("BrowserUrl", "about:blank");
            }
            set
            {
                Set("BrowserUrl", value);
            }
        }

        public bool IsJsEnabled
        {
            get
            {
                return Get("IsJsEnabled", true);
            }
            set
            {
                Set("IsJsEnabled", value);
            }
        }

        public Size WindowSize
        {
            get
            {
                var size = Get("WindowSize", new Size(0, 0));
                return size;
            }
            set
            {
                Set("WindowSize", value);
            }
        }

        public Point WindowPosition
        {
            get
            {
                var position = Get("WindowPosition", new Point(-1, -1));
                return position;
            }
            set
            {
                Set("WindowPosition", value);
            }
        }

        public ApplicationDataCompositeValue BrowserHistory
        {
            get
            {
                return Get("BrowserHistory", new ApplicationDataCompositeValue());
            }
            set
            {
                Set("BrowserHistory", value);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public T Get<T>(string PropertyName, T DefaultValue)
        {
            if (Values.TryGetValue(PropertyName, out object? value) && value is T typedValue)
            {
                return typedValue;
            }
            return DefaultValue;
        }

        public void Set<T>(string PropertyName, T Value)
        {
            //If setting doesn't exist or the oldValue is different, create the setting or set new oldValue, respectively.
            if (!Values.TryGetValue(PropertyName, out object? oldValue) || !((T)oldValue).Equals(Value))
            {
                Values[PropertyName] = Value;
                OnPropertyChanged(PropertyName);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            if (name == nameof(ConfigFilePath))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasConfigFilePath)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConfigFilePathExists)));
            }
 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }

}
