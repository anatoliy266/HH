using Sandbox.ModAPI;

using System.Xml.Serialization;

using VRage.Game.ModAPI;

namespace HeadHunter.Models
{
    [XmlRoot]
    public class HeadHunterContract
    {
        [XmlAttribute]
        public long contract_id { get; set; }
        [XmlAttribute]
        public long contract_condition_id { get; set; }
        [XmlAttribute]
        public ulong HunterSteamID { get; set; }
        [XmlAttribute]
        public MyCustomContractStateEnum State { get; set; }

        //public HeadHunterContract(long incoming_contract_id, long incoming_contract_condition_id)
        //{
        //    contract_id = incoming_contract_id;
        //    State = MyAPIGateway.ContractSystem.GetContractState(incoming_contract_id);
        //    contract_condition_id = incoming_contract_condition_id;
        //}
    }
}
