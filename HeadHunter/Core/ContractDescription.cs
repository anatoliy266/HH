namespace HeadHunter.Core
{
    public class ContractDescription
    {
        public string TargetName { get; set; }
        public ContractDescription(string targetName)
        {
            TargetName = targetName;
        }
        public string GetDescription()
        {
            return $"Опасный пират {TargetName} появился в обитаемом космосе. Найдите и убейте его!";
        }

        public string GetContractName()
        {
            return $"WANTED! {TargetName}";
        }
    }
}