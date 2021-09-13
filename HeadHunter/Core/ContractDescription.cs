namespace HeadHunter.Core
{
    /// <summary>
    /// Описание контракта за голову
    /// </summary>
    public class ContractDescription
    {
        /// <summary>
        /// Имя цели
        /// </summary>
        public string TargetName { get; set; }
        /// <summary>
        /// Конструктор (для блока контрактов)
        /// </summary>
        /// <param name="targetName"></param>
        public ContractDescription(string targetName)
        {
            TargetName = targetName;
        }
        /// <summary>
        /// Описание контракта (для блока контрактов)
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return $"Опасный пират {TargetName} появился в обитаемом космосе. Найдите и убейте его!";
        }

        /// <summary>
        /// Генерируемое название контракта (для блока контрактов)
        /// </summary>
        /// <returns></returns>
        public string GetContractName()
        {
            return $"WANTED! {TargetName}";
        }
    }
}