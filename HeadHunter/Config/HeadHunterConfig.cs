using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace HeadHunter.Config
{
    public class HeadHunterConfig : ViewModel
    {
        private string _Username = "root";
        private string _Password = "";
        private int _AuthToken = 0;
        private bool _PreferBulkChanges = true;
        private int _KillingHunterReward = 0;
        private int _HuntedPlayerBountyUpByKillingHunter = 0;
        private int _ContractCollateral = 0;
        private int _MinBounty = 10000;
        private string _HHLcdName = "HeadHunterLcd";
        private int _ContractUpdateIntervalSec = 7200;

        public string Username { get => _Username; set => SetValue(ref _Username, value); }
        public string Password { get => _Password; set => SetValue(ref _Password, value); }
        public int AuthToken { get => _AuthToken; set => SetValue(ref _AuthToken, value); }
        public bool PreferBulkChanges { get => _PreferBulkChanges; set => SetValue(ref _PreferBulkChanges, value); }
        public int KillingHunterRewardPercentage { get => _KillingHunterReward; set => SetValue(ref _KillingHunterReward, value); }
        public int HuntedPlayerBountyUpByKillingHunterPercentage { get => _HuntedPlayerBountyUpByKillingHunter; set => SetValue(ref _HuntedPlayerBountyUpByKillingHunter, value); }
        public int ContractCollateral { get => _ContractCollateral; set => SetValue(ref _ContractCollateral, value); }
        public int MinBounty { get => _MinBounty; set => SetValue(ref _MinBounty, value); }
        public string HHLcdName { get => _HHLcdName; set => SetValue(ref _HHLcdName, value); }
        public int ContractUpdateIntervalSec { get => _ContractUpdateIntervalSec; set => SetValue(ref _ContractUpdateIntervalSec, value); }
    }
}
