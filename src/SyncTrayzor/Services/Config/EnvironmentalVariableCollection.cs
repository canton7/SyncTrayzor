using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    public class EnvironmentalVariableCollection : Dictionary<string, string>, IXmlSerializable
    {
        public EnvironmentalVariableCollection()
        {
        }

        public EnvironmentalVariableCollection(IEnumerable<KeyValuePair<string, string>> source)
        {
            foreach (var kvp in source)
            {
                this.Add(kvp.Key, kvp.Value);
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            // Used to use XElement.Load(reader.ReadSubtree()), but that effectively closed the reader
            // and nothing else would get parsed.
            var root = XElement.Parse(reader.ReadOuterXml());
            foreach (var element in root.Elements("Item"))
            {
                this.Add(element.Element("Key").Value, element.Element("Value").Value);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            var elements = this.Select(item =>
            {
                return new XElement("Item",
                    new XElement("Key", item.Key),
                    new XElement("Value", item.Value)
                );
            });
            foreach (var element in elements)
            {
                element.WriteTo(writer);
            }
        }
    }
}
