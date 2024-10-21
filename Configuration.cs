using System.Xml.Serialization;

namespace MoSpeedUI;

public class Configuration
{
    [XmlElement("mospeed")]
    public string MoSpeedPath { get; set; }
    [XmlElement("javapath")]
    public string JavaPath { get; set; }
    [XmlElement("logodec")]
    public bool LogoDecoration { get; set; } = true;
}