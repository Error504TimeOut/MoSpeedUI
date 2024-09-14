using System.Xml.Serialization;

namespace MoSpeedUI;

public class Configuration
{
    [XmlElement("mospeed")]
    public string MoSpeedPath { get; set; }
   
    /*[XmlElement("javaskip")]
    public bool SkipJavaCheck { get; set; }*/
    [XmlElement("javapath")]
    public string JavaPath { get; set; }
}