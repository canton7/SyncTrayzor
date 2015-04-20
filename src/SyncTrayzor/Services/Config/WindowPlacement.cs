using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    public class WindowPlacement : IXmlSerializable
    {
        private static readonly TypeConverter pointConverter = TypeDescriptor.GetConverter(typeof(Point));
        private static readonly TypeConverter rectangleConverter = TypeDescriptor.GetConverter(typeof(Rectangle));

        public bool IsMaximised { get; set; }
        public Point MinPosition { get; set; }
        public Point MaxPosition { get; set; }
        public Rectangle NormalPosition { get; set; }

        public override string ToString()
        {
            return String.Format("<WindowPlacement IsMaximized={0} MinPosition={1} MaxPosition={2} Normalposition={3}>",
                this.IsMaximised, this.MinPosition, this.MaxPosition, this.NormalPosition);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var root = XElement.Parse(reader.ReadOuterXml());
            this.IsMaximised = (bool)root.Element("IsMaximised");
            this.MinPosition = (Point)pointConverter.ConvertFrom(root.Element("MinPosition").Value);
            this.MaxPosition = (Point)pointConverter.ConvertFrom(root.Element("MaxPosition").Value);
            this.NormalPosition = (Rectangle)rectangleConverter.ConvertFrom(root.Element("NormalPosition").Value);
        }

        public void WriteXml(XmlWriter writer)
        {
            var elements = new[]
            {
                new XElement("IsMaximised", this.IsMaximised),
                new XElement("MinPosition", pointConverter.ConvertToString(this.MinPosition)),
                new XElement("MaxPosition", pointConverter.ConvertToString(this.MaxPosition)),
                new XElement("NormalPosition", rectangleConverter.ConvertToString(this.NormalPosition))
            };

            foreach (var element in elements)
            {
                element.WriteTo(writer);
            }
        }
    }
}
