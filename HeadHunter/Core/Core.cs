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

        public void Init(HeadHunterPlugin plugin)
        {
            Plugin = plugin;
            LoadContracts();
            NetworkTag = "0x1HHNetwork";
        }

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

        public void AddHeadHunterContract(ulong steamID)
        {
            try
            {
                MyDefinitionId definitionId;
                MyDefinitionId.TryParse("MyObjectBuilder_ContractTypeDefinition/CustomContract", out definitionId);

                var player = PlayerUtils.GetPlayer(steamID);
                if (player == null)
                    return;
                var contractDescr = new ContractDescription(player.DisplayName);
                GetContractBlocks();
                LoadContracts();
                lock (ContractStorage)
                {
                    ContractBlocks.ForEach(contractBlock => {
                        if (ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).Count() > 0)
                        {
                            ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).ForEach(x => {
                                if (x.Bounty == 0)
                                    return;
                                var bounty = x.Bounty;
                                var collateral = Plugin.Config.ContractCollateral;

                                var contract = new MyContractCustom(definitionId, contractBlock.EntityId, bounty, collateral, 0, contractDescr.GetContractName(), contractDescr.GetDescription(), 10, -10, null);
                                var contractAddResult = MyAPIGateway.ContractSystem.AddContract(contract);
                                if (contractAddResult.Success)
                                {
                                    var _contract = new HeadHunterContract() { contract_id = contractAddResult.ContractId, contract_condition_id = contractAddResult.ContractConditionId, State = MyAPIGateway.ContractSystem.GetContractState(contractAddResult.ContractId) };
                                    x.Contracts.Add(_contract);
                                } else
                                {
                                    //Log.Info("failed add contract");
                                }
                            });
                        }
                    });
                }
                SaveContracts();
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }
        public void AddPlayerBounty(ulong steamID, int bounty)
        {
            try
            {
                LoadContracts();
                //lock (ContractStorage)
                //{

                //}
                if (ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).Count() > 0)
                {
                    ContractStorage.BountedPlayers.Where(x => x.SteamID == steamID).ForEach(x => x.Bounty += bounty);
                }
                else
                {
                    var bountedPlayer = new BountedPlayer()
                    {
                        SteamID = steamID,
                        Name = PlayerUtils.GetPlayer(steamID).DisplayName,
                        Bounty = bounty
                    };
                    ContractStorage.BountedPlayers.Add(bountedPlayer);
                }
                SaveContracts();
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }
        public void UpdateContracts()
        {
            try
            {
                LoadContracts();
                if (ContractStorage.BountedPlayers.Count > 0)
                {
                    lock (ContractStorage)
                    {
                        ContractStorage.BountedPlayers.ForEach(x => {
                            if (x.Contracts.Count > 0)
                            {
                                x.Contracts.ForEach(y => {
                                    y.State = MyAPIGateway.ContractSystem.GetContractState(y.contract_id);
                                });
                            }
                        });
                    }
                    SaveContracts();
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }
        public void ReCreateInactiveContracts()
        {
            try
            {
                LoadContracts();
                if (ContractStorage.BountedPlayers.Count > 0)
                {
                    lock (ContractStorage)
                    {
                        ContractStorage.BountedPlayers.ForEach(x => {
                            if (x.Contracts.Count > 0)
                            {
                                var contractsToRemove = new List<HeadHunterContract>();
                                x.Contracts.ForEach(y => {
                                    if (y.State != MyCustomContractStateEnum.Active)
                                    {
                                        MyAPIGateway.ContractSystem.RemoveContract(y.contract_id);
                                        contractsToRemove.Add(y);
                                    }
                                });
                                contractsToRemove.ForEach(y => x.Contracts.Remove(y));
                            }
                            AddHeadHunterContract(x.SteamID);
                        });
                    }
                    SaveContracts();
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }

        internal void OnDamage(IMyPlayer targetPlayer, IMyPlayer attackerPlayer)
        {
            try
            {
                LoadContracts();
                lock (ContractStorage)
                {
                    if (ContractStorage.BountedPlayers.Where(x => x.SteamID == targetPlayer.SteamUserId).Count() > 0)
                    {
                        ContractStorage.BountedPlayers.Where(x => x.SteamID == targetPlayer.SteamUserId).ForEach(x =>
                        {
                            if (x.Contracts.Count > 0)
                            {
                                x.Contracts.ForEach(y => {
                                    if (y.HunterSteamID == attackerPlayer.SteamUserId)
                                    {
                                        if (targetPlayer.SteamUserId == attackerPlayer.SteamUserId)
                                        {
                                            //Log.Info($"activator steamID = bountedPlayerSteamID");
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId) == null)
                                        {
                                            //Log.Info($"activator not in faction");
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId) == null)
                                        {
                                            Log.Info($"activator not in faction");
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }//если аттакер без фракции)
                                        else if (MyVisualScriptLogicProvider.GetPlayersFactionTag(targetPlayer.IdentityId) == MyVisualScriptLogicProvider.GetPlayersFactionTag(attackerPlayer.IdentityId))
                                        {
                                            if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, attackerPlayer.IdentityId))
                                            {
                                                Log.Info($"contract removed sucess");
                                            }
                                        }
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
                    else if (ContractStorage.BountedPlayers.Where(x => x.SteamID == attackerPlayer.SteamUserId).Count() > 0)
                    {
                        ContractStorage.BountedPlayers.Where(x => x.SteamID == attackerPlayer.SteamUserId).ForEach(x =>
                        {
                            if (x.Contracts.Count > 0)
                            {
                                x.Contracts.ForEach(y => {
                                    if (y.HunterSteamID == targetPlayer.SteamUserId)
                                    {
                                        if (targetPlayer.SteamUserId == attackerPlayer.SteamUserId)
                                        {
                                            //Log.Info($"activator steamID = bountedPlayerSteamID");
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
                                            //if (MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, targetPlayer.IdentityId))
                                            //{
                                            //    Log.Info($"contract removed sucess");
                                            //}
                                            //else
                                            //{
                                            //    Log.Info($"contract removed failed");
                                            //}
                                            ////попробовать ничего не делать
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
        public void OnActivateContract(long contractID, IMyPlayer player)
        {
            try
            {
                //Log.Info("Contract activated");
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
                SaveContracts();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }

        }
        public void OnFinishContract(long contractID)
        {
            try
            {
                //Log.Info("start finish contract");
                LoadContracts();
                ContractStorage.BountedPlayers.Where(x => x.Contracts.Where(y => y.contract_id == contractID).Count() > 0).ForEach(x => {
                    //Log.Info($"contracts for player {x.SteamID}");
                    x.Contracts.ForEach(y => {
                        if (y.contract_id != contractID && y.HunterSteamID != 0)
                        {
                            //Log.Info($"try finish contract with hunter != killer");
                            var player = PlayerUtils.GetPlayer(y.HunterSteamID);
                            MyAPIGateway.ContractSystem.TryAbandonCustomContract(y.contract_id, player.IdentityId);
                            MyAPIGateway.ContractSystem.RemoveContract(y.contract_id);
                            //Log.Info($"remove contract");
                        } else if (y.contract_id != contractID && y.HunterSteamID == 0)
                        {
                            MyAPIGateway.ContractSystem.RemoveContract(y.contract_id);
                            //Log.Info($"remove contract");
                        }
                        
                    });
                    //Log.Info($"try change bounty for target");
                    AddPlayerBounty(x.SteamID, -x.Bounty);
                    //Log.Info($"clear contracts");
                    x.Contracts.Clear();
                });
                SaveContracts();
                ReCreateInactiveContracts();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }
        public void OnFailContract(long contractID, IMyPlayer player)
        {
            try
            {
                //Log.Info("start failed contract");
                LoadContracts();
                ContractStorage.BountedPlayers.Where(x => x.Contracts.Where(y => y.contract_id == contractID).Count() > 0).ForEach(x => {
                    x.Contracts.ForEach(y => {
                        if (y.HunterSteamID == player.SteamUserId)
                        {
                            y.State = MyCustomContractStateEnum.Invalid;
                        }

                    });
                    //Log.Info("changing balance request");
                    PlayerUtils.GetPlayer(x.SteamID).RequestChangeBalance(x.Bounty / (Plugin.Config.KillingHunterRewardPercentage / 100));
                    //Log.Info("add bounty for killing hunter");
                    AddPlayerBounty(x.SteamID, x.Bounty / (Plugin.Config.HuntedPlayerBountyUpByKillingHunterPercentage / 100));
                });
                SaveContracts();
                ReCreateInactiveContracts();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
            
        }
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
                            var toRemove = new List<HeadHunterContract>();
                            x.Contracts.ForEach(y => {
                                if (y.State == MyCustomContractStateEnum.Invalid)
                                {
                                    toRemove.Add(y);
                                }
                            });
                            toRemove.ForEach(y => x.Contracts.Remove(y));
                        }
                    });
                }
                SaveContracts();
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }
        public void GetContractBlocks()
        {
            try
            {
                ContractBlocks.Clear();
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                entities.OfType<IMyCubeGrid>().ForEach(entity =>
                {
                    (entity as MyCubeGrid).GridSystems.TerminalSystem.Blocks.OfType<MyContractBlock>().ToList().ForEach(contractBlock => {
                        if (PlayerUtils.IsAdmin(PlayerUtils.GetPlayer(contractBlock.OwnerId))/* || PlayerUtils.IsNpc(contractBlock.OwnerId)*/)
                        {
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

        public void GetHeadHunterLcds()
        {
            try
            {
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
