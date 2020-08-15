using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using VRage.Game.ModAPI;

namespace HeadHunter.Utils
{
    public static class PlayerUtils
    {
        public static IMyPlayer GetPlayer(ulong steamId)
        {
            return (IMyPlayer)((MyPlayerCollection)MySession.Static.Players).GetPlayerById(new MyPlayer.PlayerId(steamId)) ?? (IMyPlayer)null;
        }

        public static ulong GetSteamId(IMyPlayer player)
        {
            return player == null ? 0UL : player.SteamUserId;
        }

        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            using (IEnumerator<MyIdentity> enumerator = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).GetEnumerator())
            {
                while (((IEnumerator)enumerator).MoveNext())
                {
                    MyIdentity current = enumerator.Current;
                    ulong result;
                    if (current.DisplayName == playerNameOrSteamId || ulong.TryParse(playerNameOrSteamId, out result) && (long)((MyPlayerCollection)MySession.Static.Players).TryGetSteamId(current.IdentityId) == (long)result)
                        return current;
                }
            }
            return (MyIdentity)null;
        }

        public static long GetOwner(MyCubeGrid grid)
        {
            List<long> bigOwners = grid.BigOwners;
            int count = bigOwners.Count;
            long num = 0;
            if (count > 0 && (ulong)bigOwners[0] > 0UL)
                return bigOwners[0];
            return count > 1 ? bigOwners[1] : num;
        }

        public static MyIdentity GetIdentityByName(string playerName)
        {
            using (IEnumerator<MyIdentity> enumerator = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).GetEnumerator())
            {
                while (((IEnumerator)enumerator).MoveNext())
                {
                    MyIdentity current = enumerator.Current;
                    if (current.DisplayName == playerName)
                        return current;
                }
            }
            return (MyIdentity)null;
        }

        public static MyIdentity GetIdentityById(long playerId)
        {
            using (IEnumerator<MyIdentity> enumerator = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).GetEnumerator())
            {
                while (((IEnumerator)enumerator).MoveNext())
                {
                    MyIdentity current = enumerator.Current;
                    if (current.IdentityId == playerId)
                        return current;
                }
            }
            return (MyIdentity)null;
        }

        public static string GetPlayerNameById(long playerId)
        {
            MyIdentity identityById = PlayerUtils.GetIdentityById(playerId);
            return identityById != null ? identityById.DisplayName : "Nobody";
        }

        public static bool IsNpc(long playerId)
        {
            return ((MyPlayerCollection)MySession.Static.Players).IdentityIsNpc(playerId);
        }

        public static bool IsPlayer(long playerId)
        {
            return PlayerUtils.GetPlayerIdentity(playerId) != null;
        }

        public static bool HasIdentity(long playerId)
        {
            return ((MyPlayerCollection)MySession.Static.Players).HasIdentity(playerId);
        }

        public static List<MyIdentity> GetAllPlayerIdentities()
        {
            if (MySession.Static == null)
                return new List<MyIdentity>();
            List<MyIdentity> list = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).ToList<MyIdentity>();
            List<long> npcs = ((MyPlayerCollection)MySession.Static.Players).GetNPCIdentities().ToList<long>();
            return ((IEnumerable<MyIdentity>)((IEnumerable<MyIdentity>)list).Where<MyIdentity>((Func<MyIdentity, bool>)(i => !npcs.Any<long>((Func<long, bool>)(n => n == i.IdentityId)))).OrderBy<MyIdentity, string>((Func<MyIdentity, string>)(i => i.DisplayName))).ToList<MyIdentity>();
        }

        public static MyIdentity GetPlayerIdentity(long identityId)
        {
            return ((IEnumerable<MyIdentity>)PlayerUtils.GetAllPlayerIdentities()).Where<MyIdentity>((Func<MyIdentity, bool>)(c => c.IdentityId == identityId)).FirstOrDefault<MyIdentity>();
        }

        public static IMyPlayer GetPlayer(long identityId)
        {
            try
            {
                return (IMyPlayer)((MyPlayerCollection)MySession.Static.Players).GetPlayerById(new MyPlayer.PlayerId(((MyPlayerCollection)MySession.Static.Players).TryGetSteamId(identityId))) ?? (IMyPlayer)null;
            } catch (Exception e)
            {
                return null;
            }
            
        }

        public static List<IMyPlayer> GetAllPlayers()
        {
            if (MySession.Static == null)
                return new List<IMyPlayer>();
            List<IMyPlayer> imyPlayerList = new List<IMyPlayer>();
            ((IMyPlayerCollection)MyAPIGateway.Players).GetPlayers(imyPlayerList, (Func<IMyPlayer, bool>)null);
            return imyPlayerList;
        }

        public static bool IsAdmin(IMyPlayer player)
        {
            return player != null && (player.PromoteLevel == MyPromoteLevel.Owner || player.PromoteLevel == MyPromoteLevel.Admin || player.PromoteLevel == MyPromoteLevel.Moderator);
        }

        public static long GetIdentityIdByName(string name)
        {
            MyIdentity myIdentity = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).Where<MyIdentity>((Func<MyIdentity, bool>)(c => c.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase))).FirstOrDefault<MyIdentity>();
            return myIdentity != null ? myIdentity.IdentityId : 0L;
        }
    }
}
