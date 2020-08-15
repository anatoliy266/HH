using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using HeadHunter.Core;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using Sandbox.Game.World;

namespace HeadHunter.Commands
{
    public class Commands : CommandModule
    {
        public HeadHunterPlugin Plugin => (HeadHunterPlugin)Context.Plugin;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Category("HeadHunter")]
        [Command("Hunt", "set contract for player kill")]
        [Permission(MyPromoteLevel.Admin)]
        public void Hunt(string player)
        {
            try
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                players.ForEach(x => {
                    if (x.DisplayName == player)
                    {
                        Plugin.Core.AddHeadHunterContract(x.SteamUserId);
                        Context.Respond($"New contracts created");
                        return;
                    }
                });
                
            } catch (Exception e)
            {
                Context.Respond($"Error: {e.Message}");
            }
        }
        [Category("HeadHunter")]
        [Command("AddBounty", "set reward for player kill")]
        [Permission(MyPromoteLevel.None)]
        public void AddBounty(string player, int bounty)
        {
            try
            {
                if (bounty < Plugin.Config.MinBounty)
                {
                    Context.Respond($"minimal bounty {Plugin.Config.MinBounty}");
                    return;
                }

                if (Context.Player.DisplayName == player)
                {
                    Context.Respond($"you cannot set selfbounty");
                    return;
                }
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                players.ForEach(x => {
                    if (x.DisplayName == player)
                    {
                        long balance = 0;
                        Context.Player.TryGetBalanceInfo(out balance);
                        if (balance < (long)bounty)
                        {
                            Context.Respond($"Not enough money!");
                            return;
                        }
                        Context.Player.RequestChangeBalance(-(long)bounty);
                        Plugin.Core.AddPlayerBounty(x.SteamUserId, bounty);
                        Context.Respond($"{player} now is wanted! Thank you for your cooperation with Head Hunters!");
                        return;
                    }
                });
            }
            catch (Exception e)
            {
                Context.Respond($"Error: {e.Message}");
            }
        }
    }
}
