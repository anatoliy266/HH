using HeadHunter.Config;
using HeadHunter.Models;
using HeadHunter.Utils;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Contracts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torch;

using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game.GUI;
using VRage.Game.GUI.TextPanel;

namespace HeadHunter.Core
{
    public class HeadHunterCore
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private HeadHunterPlugin Plugin { get; set; }

        List<MyContractBlock> ContractBlocks = new List<MyContractBlock>();

        List<MyProgrammableBlock> HeadHunterProgramBlocks = new List<MyProgrammableBlock>();

        public HeadHunterContractsStorage ContractStorage { get; set; }
        public string NetworkTag { get; set; }

        /// <summary>
        /// Инициализация плагина при загрузке сервера
        /// </summary>
        /// <param name="plugin"></param>
        public void Init(HeadHunterPlugin plugin)
        {
            Plugin = plugin;
            LoadContracts();
            NetworkTag = "0x1HHNetwork";
        }

        /// <summary>
        /// Загрузка ранее записанных контрактов(при запуске/перезапуске)
        /// </summary>
        private void LoadContracts()
        {
            var contractsFile = Path.Combine(Plugin.StoragePath, "HeadHunterContracts.cfg");
            if (!File.Exists(contractsFile))
            {
                ContractStorage = new HeadHunterContractsStorage();
                ConfigUtils.Save<HeadHunterContractsStorage>(Plugin, ContractStorage, contractsFile);
            } else
            {
                try
                {
                    ContractStorage = ConfigUtils.Load<HeadHunterContractsStorage>(Plugin, contractsFile);
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
            }
        }

        /// <summary>
        /// Сохранение текущего списка контрактов в файл
        /// </summary>
        private void SaveContracts()
        {
            try
            {
                var contractsFile = Path.Combine(Plugin.StoragePath, "HeadHunterContracts.cfg");
                //_ContractStorage.Save();
                ConfigUtils.Save<HeadHunterContractsStorage>(Plugin, ContractStorage, contractsFile);
                //Log.Info("contracts Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "contracts failed to save");
            }
        }

        /// <summary>
        /// Добавление нового контракта за голову
        /// </summary>
        /// <param name="steamID">SteamID цели контракта</param>
        public void AddHeadHunterContract(ulong steamID)
        {
            try
            {
                ///парсим строку определения кастомного контракта и пишем в новую переменную
                MyDefinitionId definitionId;
                MyDefinitionId.TryParse("MyObjectBuilder_ContractTypeDefinition/CustomContract", out definitionId);

                ///Получаем интерфейс игрока на сервере
                var player = PlayerUtils.GetPlayer(steamID);
                ///Если игрока нет в сети или не существует - контракт не добавляется
                if (player == null)
                    return;
                ///Создаем описание контракта
                var contractDescr = new ContractDescription(player.DisplayName);
                ///в глобальные переменные пишем список контрактных блоков и загружаем ранее созданные контракты
                GetContractBlocks();
                LoadContracts();

                ///блокируем список на изменение в других потоках
                lock (ContractStorage)
                {
                    ///проверяем контракты, есть ли уже заказ на этого игрока
                    ContractBlocks.ForEach(contractBlock => {
                        if (ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).Count() > 0)
                        {
                            ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).ForEach(x => {
                                ///если заказ найден проверяем остаток на награде 
                                ///если награда 0 - контракты не добавляются в контрактные блоки
                                if (x.Bounty == 0)
                                    return;

                                ///если награда не 0 - создаем новый контракт
                                var bounty = x.Bounty;
                                var collateral = Plugin.Config.ContractCollateral;
                                var contract = new MyContractCustom(definitionId, contractBlock.EntityId, bounty, collateral, 0, contractDescr.GetContractName(), contractDescr.GetDescription(), 10, -10, null);
                                
                                ///добавляем новый контракт в систему контрактов
                                var contractAddResult = MyAPIGateway.ContractSystem.AddContract(contract);
                                ///если добавление прошло успешно - записываем в список заказов новый контракт
                                if (contractAddResult.Success)
                                {
                                    var _contract = new HeadHunterContract() { contract_id = contractAddResult.ContractId, contract_condition_id = contractAddResult.ContractConditionId, State = MyAPIGateway.ContractSystem.GetContractState(contractAddResult.ContractId) };
                                    x.Contracts.Add(_contract);
                                } else
                                {
                                    Log.Info("failed add contract");
                                }
                            });
                        }
                    });
                }
                ///записываем контракты в файл
                SaveContracts();
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }
        /// <summary>
        /// Добавление заказа на игрока/добавление награды к существующему заказу
        /// </summary>
        /// <param name="steamID"></param>
        /// <param name="bounty"></param>
        public void AddPlayerBounty(ulong steamID, int bounty)
        {
            try
            {
                ///загружаем список заказов и проверяем есть ли уже на игрока с указанным SteamId заказ
                LoadContracts();
                
                if (ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).Count() > 0)
                {
                    ///если заказ есть - добавляем к награде сумму bounty космокредитов
                    ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).ForEach(x => x.Bounty += bounty);
                }
                else
                {
                    ///если заказа нет - создаем новый и записываем в хранилище
                    var bountedPlayer = new BountedPlayer()
                    {
                        SteamID = steamID,
                        Name = PlayerUtils.GetPlayer(steamID).DisplayName,
                        Bounty = bounty
                    };
                    ContractStorage.BountedPlayers.Add(bountedPlayer);
                }
                ///сохраняем заказы в файл
                SaveContracts();
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }

        /// <summary>
        /// Обновление статусов активных заказов
        /// </summary>
        public void UpdateContracts()
        {
            try
            {
                ///загружаем список заказов и смотрим есть ли там что-нибудь
                LoadContracts();
                if (ContractStorage.BountedPlayers.Count > 0)
                {
                    lock (ContractStorage)
                    {
                        ContractStorage.BountedPlayers.ForEach(x => {
                            ///для каждого найденного заказа обновляем статусы
                            if (x.Contracts.Count > 0)
                            {
                                x.Contracts.ForEach(y => {
                                    y.State = MyAPIGateway.ContractSystem.GetContractState(y.contract_id);
                                });
                            }
                        });
                    }
                    ///сохраняем список заказов в файл
                    SaveContracts();
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }


        /// <summary>
        /// Пересоздание неактивных контрактов
        /// </summary>
        public void ReCreateInactiveContracts()
        {
            try
            {
                ///если контракт был выполнен или отменен по какой то причине - удаляем его из списка активных контрактов 
                LoadContracts();
                if (ContractStorage.BountedPlayers.Count > 0)
                {
                    lock (ContractStorage)
                    {
                        ContractStorage.BountedPlayers.ForEach(x => {
                            if (x.Contracts.Count > 0)
                            {
                                ///добавляем контракты со статусом не Active в список на удаление
                                var contractsToRemove = new List<HeadHunterContract>();
                                x.Contracts.ForEach(y => {
                                    if (y.State != MyCustomContractStateEnum.Active)
                                    {
                                        MyAPIGateway.ContractSystem.RemoveContract(y.contract_id);
                                        contractsToRemove.Add(y);
                                    }
                                });
                                ///удаляем неактивные контракты
                                contractsToRemove.ForEach(y => x.Contracts.Remove(y));
                            }
                            ///создаем новые контракты в контрактных блоках(если осталась награда)
                            AddHeadHunterContract(x.SteamID);
                        });
                    }
                    ///сохраняем обновленный список в файл
                    SaveContracts();
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }


        /// <summary>
        /// Обработчик события регистрации урона от игрока игроку
        /// </summary>
        /// <param name="targetPlayer">Жертва</param>
        /// <param name="attackerPlayer">Фгрессор</param>
        internal void OnDamage(IMyPlayer targetPlayer, IMyPlayer attackerPlayer)
        {
            ///лагает (((
            ///скорее всего изза постоянного обновления контрактов при нанесении урона
            try
            {
                LoadContracts();
                lock (ContractStorage)
                {
                    ///проверяем есть ли в заказах игрок-жертва
                    if (ContractStorage.BountedPlayers.Where(x => x.SteamID == targetPlayer.SteamUserId).Count() > 0)
                    {
                        ContractStorage.BountedPlayers.Where(x => x.SteamID == targetPlayer.SteamUserId).ForEach(x =>
                        {
                            ///проверяем есть ли активные контракты на жертву
                            if (x.Contracts.Count > 0)
                            {
                                x.Contracts.ForEach(y => {
                                    ///если агрессор - владелец контракта на жертву - проверяяем на эксплойты
                                    if (y.HunterSteamID == attackerPlayer.SteamUserId)
                                    {
                                        ///если взял сам на себя контракт и атаковал сам себя (урон об воксели/структуры взял под дистанционное управление свою турель и выстрелил в себя)
                                        if (targetPlayer.SteamUserId == attackerPlayer.SteamUserId)
                                        {
                                            ///отменяем контракт
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId) == null)
                                        {
                                            ///заглушка от фарма
                                            ///Игрок жертва должен находиться во фракции, если нет - отменяем контракт
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId) == null)
                                        {
                                            ///Игрок агрессор также должен находиться во фракции, если нет - удаляем контракт
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId) == MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId))
                                        {
                                            ///если охотник и жертва состоят в одной фракции - отменяем контракт
                                            ///Защита от договорного фарма
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        //Фракции должны находиться друг с другом в состоянии войны
                                        //не работает функция, возвращает null вне зависимости от статусов фракций по отношению друк к другу (((
                                        //else if (!MyVisualScriptLogicProvider.AreFactionsEnemies(MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId), MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId)))
                                        //{
                                        //    Log.Info($"activator faction !enemy bountedPlayer faction");
                                        //    if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                        //    {
                                        //        Log.Info($"contract removed sucess");
                                        //    }
                                        //    else
                                        //    {
                                        //        Log.Info($"contract removed failed");
                                        //    }
                                        //}
                                        else {
                                            ///если проверки пройдены - завершаем контракт и выплачиваем ревард
                                            if (MyAPIGateway.ContractSystem.TryFinishCustomContract(y.contract_id))
                                            {
                                                Log.Info("contract finish sucess");
                                            }
                                        } 
                                    }
                                });
                            }
                        });

                    }
                    ///если жертва наносит урон по охотнику
                    else if (ContractStorage.BountedPlayers.Where(x => x.SteamID == attackerPlayer.SteamUserId).Count() > 0)
                    {
                        ContractStorage.BountedPlayers.Where(x => x.SteamID == attackerPlayer.SteamUserId).ForEach(x =>
                        {
                            if (x.Contracts.Count > 0)
                            {
                                x.Contracts.ForEach(y => {
                                    if (y.HunterSteamID == targetPlayer.SteamUserId)
                                    {
                                        //те же самые проверки на эксплойты
                                        if (targetPlayer.SteamUserId == attackerPlayer.SteamUserId)
                                        {
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId) == null)
                                        {
                                            //Log.Info($"activator not in faction");
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId) == null) 
                                        {
                                            //Log.Info($"activator not in faction");
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }//если аттакер без фракции)
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId) == MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId))
                                        {
                                            Log.Info($"activator faction = bountedPlayer faction");
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                            return;

                                        }
                                        //else if (!MyVisualScriptLogicProvider.AreFactionsEnemies(MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId), MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId)))
                                        //{
                                        //    Log.Info($"activator faction !enemy bountedPlayer faction");
                                        //    if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                        //    {
                                        //        Log.Info($"contract removed sucess");
                                        //    }
                                        //    else
                                        //    {
                                        //        Log.Info($"contract removed failed");
                                        //    }
                                        //}
                                        else
                                        {
                                            ///если все проверки пройдены - завершаем контракт выплачиваем вознаграждение
                                            if (MyAPIGateway.ContractSystem.TryFailCustomContract(y.contract_id))
                                            {
                                                Log.Info("contract failed sucess");
                                            }
                                        }
                                    }
                                });
                            }
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }


        /// <summary>
        /// Обработчик события взятия контракта в контрактном блоке
        /// </summary>
        /// <param name="contractID"></param>
        /// <param name="player"></param>
        public void OnActivateContract(long contractID, IMyPlayer player)
        {
            try
            {
                ///добавляем взятый контракт в список активных
                LoadContracts();
                lock (ContractStorage)
                {
                    ContractStorage.BountedPlayers.Where(x => x.Contracts.Where(y => y.contract_id == contractID).Count() > 0).ForEach(x => {
                        x.Contracts.Where(y => y.contract_id == contractID).ForEach(y => {
                            y.HunterSteamID = player.SteamUserId;
                            y.State = MyCustomContractStateEnum.Active;
                        });
                    });
                }
                ///сохраняем контракты в файл
                SaveContracts();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }

        }


        /// <summary>
        /// Обработчик события завершения контракта
        /// </summary>
        /// <param name="contractID"></param>
        public void OnFinishContract(long contractID)
        {
            try
            {
                //Log.Info("start finish contract");
                LoadContracts();
                ContractStorage.BountedPlayers.Where(x => x.Contracts.Where(y => y.contract_id == contractID).Count() > 0).ForEach(x => {
                    //Log.Info($"contracts for player {x.SteamID}");
                    ///если контракт был принят - удаляем его из блока и у игроков принявших контракт
                    x.Contracts.ForEach(y => {
                        if (y.contract_id != contractID && y.HunterSteamID != 0)
                        {
                            //Log.Info($"try finish contract with hunter != killer");
                            var player = PlayerUtils.GetPlayer(y.HunterSteamID);
                            MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, player.IdentityId);
                            MyAPIGateway.ContractSystem.RemoveContract(y.contract_id);
                            //Log.Info($"remove contract");
                        } 
                        /// если контракт не был принят - удаляем его из контрактного блока
                        else if (y.contract_id != contractID && y.HunterSteamID == 0)
                        {
                            MyAPIGateway.ContractSystem.RemoveContract(y.contract_id);
                            //Log.Info($"remove contract");
                        }
                        
                    });
                    //Log.Info($"try change bounty for target");
                    ///вычитаем сумму вознаграждения из суммы за голову игрока
                    AddPlayerBounty(x.SteamID, -x.Bounty);
                    //Log.Info($"clear contracts");
                    ///очищаем список активных контрактов
                    x.Contracts.Clear();
                });
                ///сохраняем список заказов в файл
                SaveContracts();
                ///пересоздаем контракты в контрактных блоках
                ReCreateInactiveContracts();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }

        /// <summary>
        /// Обработчик события провала контракта
        /// </summary>
        /// <param name="contractID"></param>
        /// <param name="player"></param>
        public void OnFailContract(long contractID, IMyPlayer player)
        {
            try
            {
                //Log.Info("start failed contract");
                LoadContracts();
                ///эксплойт !!!!!!!!!!!
                ///можно убивать охотника и апать себе награду в несколько раз и потом договариваться на слив 50/50 с другим игроком, как програмно запретить пока не придумал
                ContractStorage.BountedPlayers.Where(x => x.Contracts.Where(y => y.contract_id == contractID).Count() > 0).ForEach(x => {
                    x.Contracts.ForEach(y => {
                        ///меняем статус контракта 
                        if (y.HunterSteamID == player.SteamUserId)
                        {
                            y.State = MyCustomContractStateEnum.Invalid;
                        }

                    });
                    ///увеличиваем баланс жертвы на значение из конфига
                    PlayerUtils.GetPlayer(x.SteamID).RequestChangeBalance(x.Bounty / (Plugin.Config.KillingHunterRewardPercentage / 100));
                    //Log.Info("add bounty for killing hunter");
                    ///добавляем к награде за голову значение из конфига
                    AddPlayerBounty(x.SteamID, x.Bounty / (Plugin.Config.HuntedPlayerBountyUpByKillingHunterPercentage / 100));
                });
                ///сохраняем и пересоздаем контракты
                SaveContracts();
                ReCreateInactiveContracts();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }


        /// <summary>
        /// Удаление неактивных контрактов 
        /// </summary>
        public void RemoveInvalid()
        {
            try
            {
                LoadContracts();
                if (ContractStorage.BountedPlayers.Count > 0)
                {
                    ContractStorage.BountedPlayers.ForEach(x => {
                        if (x.Contracts.Count > 0)
                        {
                            ///добавляем в список контракты со статусом инактив 
                            var toRemove = new List<HeadHunterContract>();
                            x.Contracts.ForEach(y => {
                                if (y.State == MyCustomContractStateEnum.Invalid)
                                {
                                    toRemove.Add(y);
                                }
                            });
                            ///удаляем контракты 
                            toRemove.ForEach(y => x.Contracts.Remove(y));
                        }
                    });
                }
                ///сохраняем оставшиеся контакты в файл
                SaveContracts();
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }


        /// <summary>
        /// Получение всех контрактных блоков в мире игры
        /// </summary>
        public void GetContractBlocks()
        {
            try
            {
                ContractBlocks.Clear();
                var entities = new HashSet<IMyEntity>();
                ///собираем все сущности - структуры на сервере
                MyAPIGateway.Entities.GetEntities(entities);
                entities.OfType<IMyCubeGrid>().ForEach(entity =>
                {
                    ///для каждой структуры проверяем наличие в ней контрактного блока
                    (entity as MyCubeGrid).GridSystems.TerminalSystem.Blocks.OfType<MyContractBlock>().ToList().ForEach(contractBlock => {
                        if (PlayerUtils.IsAdmin(PlayerUtils.GetPlayer(contractBlock.OwnerId))/* || PlayerUtils.IsNpc(contractBlock.OwnerId)*/)
                        {
                            ///если еще не был добавлен - добавляем в список
                            if (!ContractBlocks.Contains(contractBlock))
                                ContractBlocks.Add(contractBlock);
                        }
                    });
                });
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }


        /// <summary>
        /// Получает все програмные блоки для вставки скрипта регистрации игрока 
        /// не реализовано
        /// </summary>
        public void GetHeadHunterNetworkAdapters()
        {
            try
            {
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                entities.OfType<IMyCubeGrid>().ForEach(entity =>
                {
                    (entity as MyCubeGrid).GridSystems.TerminalSystem.Blocks.OfType<MyProgrammableBlock>().Where(x => x.DisplayName == "HHNetworkAdapter").ToList().ForEach(progBlock => {
                        progBlock.UpdateProgram("");
                        if (!HeadHunterProgramBlocks.Contains(progBlock))
                            HeadHunterProgramBlocks.Add(progBlock);
                    });
                });
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }

        /// <summary>
        /// Вывод списка заказов на lcd-панель
        /// </summary>
        public void GetHeadHunterLcds()
        {
            try
            {
                ///берем все lcd 
                ///берем все lcd панели на сервере
                ///ищем с именем определенным в конфиге
                ///вставляем текст со списком заказов типа TOP 10
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                entities.OfType<IMyCubeGrid>().ForEach(entity =>
                {
                    var gridBlocks = new List<IMySlimBlock>();
                    //Log.Info(entity.DisplayName);
                    entity.GetBlocks(gridBlocks);
                    if (gridBlocks.Count == 0)
                        return;
                    gridBlocks.Where(x => x.BlockDefinition.DisplayNameText == "Text panel").Where(x => (x.FatBlock as IMyTextPanel).DisplayNameText == Plugin.Config.HHLcdName).ForEach(x => {
                        //Log.Info($"{x.BlockDefinition.DisplayNameText}");
                        var textpanel = (x.FatBlock as IMyTextPanel);
                        //xtpanel.PanelComponent.ShowTextFlag = VRage.Game.GUI.TextPanel.ShowTextOnScreenFlag.PUBLIC;
                        textpanel.ContentType = ContentType.TEXT_AND_IMAGE;
                        textpanel.Font = "Red";
                        textpanel.FontColor = new VRageMath.Color(255, 0, 0);
                        textpanel.FontSize = 0.7f;
                        textpanel.Alignment = TextAlignment.CENTER;
                        if (ContractStorage.BountedPlayers.Count > 0)
                        {
                            var text = "TOP BOUNTIES\r\n\r\n\r\n";
                            ContractStorage.BountedPlayers.OrderBy(y => y.Bounty).ForEach(bounty =>
                            {
                                text += $"{bounty.Name}\r\n";
                                text += $"{bounty.Bounty}\r\n\r\n";
                            });
                            text += "Look for contracts in Dance Dead Bar";
                            textpanel.WriteText(text);
                        } else
                        {
                            textpanel.WriteText("No bounties - no contracts");
                        }
                    });
                });
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
             
        }

        private void SendNetworkMessage(string message)
        {
            //todo
        }


        /// <summary>
        /// Метод для получения интерфейса игрока в случае если огонь велся не с ручного оружия
        /// </summary>
        /// <param name="entityID"></param>
        /// <returns></returns>
        public IMyPlayer GetPlayer(long entityID)
        {
            
            try
            {
                IMyPlayer player = (IMyPlayer)null;
                IMyEntity entityById = MyAPIGateway.Entities.GetEntityById((long)entityID);
                switch (entityById)
                {
                    case IMyCubeBlock _:
                        MyCubeBlock myCubeBlock = entityById is MyCubeBlock ? entityById as MyCubeBlock : (MyCubeBlock)(entityById as MyFunctionalBlock);
                        if (myCubeBlock.CubeGrid != null)
                        {
                            if (myCubeBlock.CubeGrid.HasMainCockpit() && myCubeBlock.CubeGrid.MainCockpit != null)
                            {
                                IMyShipController mainCockpit = myCubeBlock.CubeGrid.MainCockpit as IMyShipController;
                                IMyControllerInfo controllerInfo = ((IMyControllableEntity)mainCockpit).ControllerInfo;
                                if (controllerInfo != null && controllerInfo.ControllingIdentityId > 0L)
                                    player = PlayerUtils.GetPlayer(((IMyControllableEntity)mainCockpit).ControllerInfo.ControllingIdentityId);
                            }
                            if (player == null)
                                player = PlayerUtils.GetPlayer(PlayerUtils.GetOwner(myCubeBlock.CubeGrid));
                            break;
                        }
                        break;
                    case IMyCubeGrid _:
                        player = PlayerUtils.GetPlayer(PlayerUtils.GetOwner(entityById as MyCubeGrid));
                        break;
                    case IMyPlayer _:
                        player = entityById as IMyPlayer;
                        break;
                    case IMyCharacter _:
                        player = PlayerUtils.GetPlayer((entityById as MyCharacter).GetPlayerIdentityId());
                        break;
                    case IMyHandheldGunObject<MyGunBase> _:
                        player = PlayerUtils.GetPlayer((entityById as IMyHandheldGunObject<MyGunBase>).OwnerIdentityId);
                        break;
                    case IMyHandheldGunObject<MyToolBase> _:
                        player = PlayerUtils.GetPlayer((entityById as IMyHandheldGunObject<MyToolBase>).OwnerIdentityId);
                        break;
                    default:
                        ///если не удалось определить по какой причине умер игрок
                        Log.Warn("Undetected kill type:");
                        Log.Warn("> Name: " + entityById.DisplayName);
                        Log.Warn(string.Format("> Type: {0}", (object)((object)entityById).GetType()));
                        break;
                }
                return player;
            } catch (Exception e)
            {
                return null;
            }
            
        }
    }
}
