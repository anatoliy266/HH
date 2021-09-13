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
        /// <summary>
        /// Список заказов на игроков
        /// </summary>
        [XmlElement]
        public HashSet<BountedPlayer> BountedPlayers { get; set; }
    }
}
