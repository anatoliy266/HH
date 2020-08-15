using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HeadHunter.Models
{
    [XmlRoot]
    public class BountedPlayer
    {
        [XmlAttribute]
        public ulong SteamID { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public int Bounty { get; set; }
        [XmlElement]
        public List<HeadHunterContract> Contracts { get; set; }
    }
}
