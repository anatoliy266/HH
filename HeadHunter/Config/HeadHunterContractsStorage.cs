using HeadHunter.Core;
using HeadHunter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Torch;

namespace HeadHunter.Config
{
    [XmlRoot]
    public class HeadHunterContractsStorage
    {
        //private HashSet<BountedPlayer> _BountedPlayers = new HashSet<BountedPlayer>();
        //public HashSet<BountedPlayer> BountedPlayers { get => _BountedPlayers; set => SetValue<HashSet<BountedPlayer>>(new Action<HashSet<BountedPlayer>>(SetAction), value); }

        //public void Add(BountedPlayer value)
        //{
        //    _BountedPlayers.Add(value);
        //}

        //private void SetAction(HashSet<BountedPlayer> value)
        //{
        //    value.ForEach(x => _BountedPlayers.Add(x));
        //}
        [XmlElement]
        public HashSet<BountedPlayer> BountedPlayers { get; set; }
    }
}
