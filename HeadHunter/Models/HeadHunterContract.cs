using Sandbox.ModAPI;

using System.Xml.Serialization;

using VRage.Game.ModAPI;

namespace HeadHunter.Models
{
    [XmlRoot]
    public class HeadHunterContract
    {
        /// <summary>
        /// Id контракта
        /// </summary>
        [XmlAttribute]
        public long contract_id { get; set; }
        /// <summary>
        /// Еще один Id контракта
        /// </summary>
        [XmlAttribute]
        public long contract_condition_id { get; set; }
        /// <summary>
        /// Id игрока взявшего контракт
        /// </summary>
        [XmlAttribute]
        public ulong HunterSteamID { get; set; }
        /// <summary>
        /// Статус контракта (из VRage.Game.ModAPI)
        /// </summary>
        [XmlAttribute]
        public MyCustomContractStateEnum State { get; set; }
    }
}
