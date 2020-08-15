using System;
using NLog;
using Torch;
using Torch.API;
using System.Collections.Generic;
using System.Linq;
using HeadHunter.Config;
using System.IO;
using HeadHunter.GUI;
using Torch.API.Managers;
using Torch.Session;
using Torch.API.Session;
using Sandbox.ModAPI;
using HeadHunter.Core;
using VRage.Game.ModAPI;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Game.Entities.Character;
using VRage.Game.ObjectBuilders.Components;
using Sandbox.Game.SessionComponents;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using HeadHunter.Utils;
using Sandbox.Game.Entities.Blocks;

namespace HeadHunter
{

    public class HeadHunterPlugin : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Persistent<HeadHunterConfig> _config;
        public HeadHunterConfig Config => _config?.Data;
        ///HeadHunter core
        ///
        int counter = 0;
        public HeadHunterCore Core { get; set; }
        ///GUI
        private HeadHunterGUI _control;
        public HeadHunterGUI GetControl() => _control ?? (_control = new HeadHunterGUI(this));

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();
            try
            {
                Core = new HeadHunterCore();
                Core.Init(this);
                
                var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
                if (sessionManager != null)
                {
                    sessionManager.SessionStateChanged += SessionChanged;
                    
                }
                else
                    Log.Warn("No session manager loaded!");
            } catch (Exception e)
            {
                Log.Error($"CRITICAL ERROR, TERMINATED");
                Log.Error($"{e.Message}");
            }
        }

        public override void Update()
        {
            ++counter;
            if (counter % 3600 == 0)
            {
                Core.UpdateContracts();
            }
            if (counter == Config.ContractUpdateIntervalSec)
            {
                Core.RemoveInvalid();
                Core.ReCreateInactiveContracts();
                Core.GetHeadHunterLcds();
                counter = 0;
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            //Log.Info("Session-State is now " + newState);
            switch (newState)
            {
                case TorchSessionState.Loaded:
                    {
                        MyAPIGateway.ContractSystem.CustomActivateContract += ContractSystem_CustomActivateContract;
                        MyAPIGateway.ContractSystem.CustomFailFor += ContractSystem_CustomFailFor;
                        MyAPIGateway.ContractSystem.CustomFinish += ContractSystem_CustomFinish;
                        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageApplied);
                        break;
                    }
            }
        }

        private void ContractSystem_CustomFinish(long contractId)
        {
            Core.OnFinishContract(contractId);
        }

        private void ContractSystem_CustomFailFor(long contractId, long identityId, bool isAbandon)
        {
            if (!isAbandon)
            {
                var player = PlayerUtils.GetPlayer(identityId);
                Core.OnFailContract(contractId, player);
            }
        }

        private void DamageApplied(object target, ref MyDamageInformation info)
        {
            try
            {
                if (!(target is MyCharacter))
                    return;
                if (!(target is MyCharacter targetCharacter))
                    return;

                //Log.Info($"Target type {target.GetType().Name}");
                var damageInfo = info;
                //Log.Info($"target is player");
                //Log.Info($"damage amount {damageInfo.Amount}");
                //Log.Info($"attacker id {damageInfo.AttackerId}");
                //Log.Info($"deformation? {damageInfo.IsDeformation}");
                //Log.Info($"attack type {damageInfo.Type}");

                //var attackerIdentity = damageInfo.AttackerId;
                //var targetPlayer = MyPlayer.GetPlayerFromCharacter(targetCharacter);
                var targetPlayer = PlayerUtils.GetPlayer(targetCharacter.GetPlayerIdentityId());
                if (targetPlayer == null)
                    return;

                var targetHealth = MyVisualScriptLogicProvider.GetPlayersHealth(targetPlayer.Identity.IdentityId);
                if (targetHealth < damageInfo.Amount)
                {
                    IMyPlayer attackerPlayer = Core.GetPlayer(damageInfo.AttackerId);
                    if (attackerPlayer == null)
                        return;
                    if (attackerPlayer != null)
                    {
                        Core.OnDamage(targetPlayer, attackerPlayer);
                    }
                }
            } catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error("--------------------------");
                Log.Error(e.StackTrace);
            }
        }

        private void ContractSystem_CustomActivateContract(long contractId, long identityId)
        {
            try
            {
                
                var player = PlayerUtils.GetPlayer(identityId);
                Core.OnActivateContract(contractId, player);
            } catch (Exception e)
            {
                Log.Error(e, "customActivate error:" + e.Message);
            }
            ///tag checked if contract target and quest_player in one tag - failed contract
        }


        private void SetupConfig()
        {
            var configFile = Path.Combine(StoragePath, "HeadHunterConfig.cfg");
            try
            {
                _config = Persistent<HeadHunterConfig>.Load(configFile);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {
                //Log.Info("Create Default Config, because none was found!");
                _config = new Persistent<HeadHunterConfig>(configFile, new HeadHunterConfig());
                _config.Save();
            }
        }
        public void Save()
        {
            try
            {
                _config.Save();
                //Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                //Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}
