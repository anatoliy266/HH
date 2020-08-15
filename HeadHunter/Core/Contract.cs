using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;

namespace HeadHunter.Core
{
    public enum ContractState
    {
        Acquired = 1,
        Failed,
        Succeeded,
    }
    public class ContractEventArgs : EventArgs
    {
        public ContractState State { get; set; }

        public int MoneyReward { get; set; }

        public int Collateral { get; set; }

        public int Duration { get; set; }
        public ContractEventArgs() : base()
        {

        }
    }
    
    public class HeadHunterContract : IMyContract, IMyContractBounty, IMyContractCustom
    {
        public delegate void ContractEventHandler(object sender, ContractEventArgs e);
        public event ContractEventHandler ContractSucceeded;
        public event ContractEventHandler ContractFailed;
        public event ContractEventHandler ContractAcquired;
        public long StartBlockId { get; set; }

        public int MoneyReward { get; set; }

        public int Collateral { get; set; }

        public int Duration { get; set; }

        public Action<long> OnContractAcquired { get => OnContractAcquired; set => OnAcquiredContract(value); }
        public Action OnContractSucceeded { get => OnContractSucceeded; set => OnSuceededContract(value); }
        public Action OnContractFailed { get => OnContractFailed; set => OnFailedContract(value); }

        public long TargetIdentityId { get; set; }

        public MyDefinitionId DefinitionId { get; set; }

        public long? EndBlockId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int ReputationReward { get; set; }

        public int FailReputationPrice { get; set; }

        public HeadHunterContract(long startBlockId, int moneyReward, int collateral, int duration, long target)
        {
            StartBlockId = startBlockId;
            MoneyReward = moneyReward;
            Collateral = collateral;
            Duration = duration;
            TargetIdentityId = target;
            Name = "HeadHunter contract";
            ReputationReward = 10;
            FailReputationPrice = 10;
            Description = "find him and kill";
            EndBlockId = null;
            DefinitionId = new MyDefinitionId();
        }

        public static HeadHunterContract CreateContract(long StartBlockId, int MoneyReward, int Collateral, int Duration, long Target)
        {
            return new HeadHunterContract(StartBlockId, MoneyReward, Collateral, Duration, Target);
        }

        private void OnFailedContract(Action onContractFailed)
        {
            OnContractFailed = onContractFailed;

            //var sender = (IMyPlayer)OnContractFailed.Target;
            //ContractFailed(OnContractFailed.Target, new ContractEventArgs() { State = ContractState.Failed, Collateral = Collateral, Duration = Duration, MoneyReward = MoneyReward });
        }

        private void OnSuceededContract(Action value)
        {
            OnContractSucceeded = value;
            //ContractSucceeded(OnContractFailed.Target, new ContractEventArgs() { State = ContractState.Succeeded, Collateral = Collateral, Duration = Duration, MoneyReward = MoneyReward });
        }
        private void OnAcquiredContract(Action<long> value)
        {
            OnContractAcquired = value;
            //ContractAcquired(OnContractFailed.Target, new ContractEventArgs() { State = ContractState.Failed, Collateral = Collateral, Duration = Duration, MoneyReward = MoneyReward });
        }
    }
}
