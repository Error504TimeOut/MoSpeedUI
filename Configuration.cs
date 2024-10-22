using System.ComponentModel;  // Notwendig für INotifyPropertyChanged
using System.Xml.Serialization;

namespace MoSpeedUI
{
    public class Configuration : INotifyPropertyChanged
    {
        // Das PropertyChanged Event für die Bindungen
        public event PropertyChangedEventHandler PropertyChanged;

        private string _moSpeedPath;
        private string _javaPath;
        private bool _logoDecoration = true;

        [XmlElement("mospeed")]
        public string MoSpeedPath
        {
            get => _moSpeedPath;
            set
            {
                if (_moSpeedPath != value)
                {
                    _moSpeedPath = value;
                    OnPropertyChanged(nameof(MoSpeedPath));
                }
            }
        }

        [XmlElement("javapath")]
        public string JavaPath
        {
            get => _javaPath;
            set
            {
                if (_javaPath != value)
                {
                    _javaPath = value;
                    OnPropertyChanged(nameof(JavaPath));
                }
            }
        }

        [XmlElement("logodec")]
        public bool LogoDecoration
        {
            get => _logoDecoration;
            set
            {
                if (_logoDecoration != value)
                {
                    _logoDecoration = value;
                    OnPropertyChanged(nameof(LogoDecoration));
                }
            }
        }

        // Methode zum Auslösen des PropertyChanged-Events
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}