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


        #region Donate
        public string Username { get => _Username; set => SetValue(ref _Username, value); }
        public string Password { get => _Password; set => SetValue(ref _Password, value); }

        public int AuthToken { get => _AuthToken; set => SetValue(ref _AuthToken, value); }
        #endregion
        public bool PreferBulkChanges { get => _PreferBulkChanges; set => SetValue(ref _PreferBulkChanges, value); }

        /// <summary>
        /// Количество космокредитов за убийство охотника
        /// </summary>
        public int KillingHunterRewardPercentage { get => _KillingHunterReward; set => SetValue(ref _KillingHunterReward, value); }
        /// <summary>
        /// На сколько растет цена за голову после убийства охотника
        /// </summary>
        public int HuntedPlayerBountyUpByKillingHunterPercentage { get => _HuntedPlayerBountyUpByKillingHunter; set => SetValue(ref _HuntedPlayerBountyUpByKillingHunter, value); }
        /// <summary>
        /// 
        /// </summary>
        public int ContractCollateral { get => _ContractCollateral; set => SetValue(ref _ContractCollateral, value); }
        /// <summary>
        /// Минимальная цена для создания контракта на игрока в космокредитах
        /// </summary>
        public int MinBounty { get => _MinBounty; set => SetValue(ref _MinBounty, value); }
        /// <summary>
        /// Имя для Lcd панели с контрактами
        /// </summary>
        public string HHLcdName { get => _HHLcdName; set => SetValue(ref _HHLcdName, value); }
        /// <summary>
        /// Частота обновления контрактов
        /// </summary>
        public int ContractUpdateIntervalSec { get => _ContractUpdateIntervalSec; set => SetValue(ref _ContractUpdateIntervalSec, value); }
    }
}
