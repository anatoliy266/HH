using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HeadHunter.Models
{
    /// <summary>
    /// Заказ за голову
    /// </summary>
    [XmlRoot]
    public class BountedPlayer
    {
        /// <summary>
        /// Steam Id заказанного игрока (чтобы не эвейдилось сменой имени)
        /// </summary>
        [XmlAttribute]
        public ulong SteamID { get; set; }
        /// <summary>
        /// Имя игрока(на всякий случай)
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }
        /// <summary>
        /// Сумма за голову в космокредитах
        /// </summary>
        [XmlAttribute]
        public int Bounty { get; set; }
        /// <summary>
        /// Список активных контрактов на игрока
        /// </summary>
        [XmlElement]
        public List<HeadHunterContract> Contracts { get; set; }
    }
}
