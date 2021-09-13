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
    /// <summary>
    /// Вспомогательные методы SpaceEngineersApi
    /// </summary>
    public static class PlayerUtils
    {
        /// <summary>
        /// Получение интерфейса игрока по Steam Id
        /// </summary>
        /// <param name="steamId">Steam Id игрока</param>
        /// <returns></returns>
        public static IMyPlayer GetPlayer(ulong steamId)
        {
            return (IMyPlayer)((MyPlayerCollection)MySession.Static.Players).GetPlayerById(new MyPlayer.PlayerId(steamId)) ?? (IMyPlayer)null;
        }

        /// <summary>
        /// Получение SteamId игрока по его интерфейсу
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static ulong GetSteamId(IMyPlayer player)
        {
            return player == null ? 0UL : player.SteamUserId;
        }


        /// <summary>
        /// Получение сущности игрока в мире по его имени или Steam Id
        /// </summary>
        /// <param name="playerNameOrSteamId"></param>
        /// <returns></returns>
        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            using (IEnumerator<MyIdentity> enumerator = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).GetEnumerator())
            {
                while (((IEnumerator)enumerator).MoveNext())
                {
                    ///перебором проходим по всем игрокам на сервере
                    ///если нашлось соответствие по свойствам SteamId или Name - возвращаем Identity
                    
                    MyIdentity current = enumerator.Current;
                    ulong result;
                    if (current.DisplayName == playerNameOrSteamId || ulong.TryParse(playerNameOrSteamId, out result) && (long)((MyPlayerCollection)MySession.Static.Players).TryGetSteamId(current.IdentityId) == (long)result)
                        return current;
                }
            }
            ///если сущности не нашлось - возвращаем null
            return (MyIdentity)null;
        }

        /// <summary>
        /// Получение владельца структуры
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static long GetOwner(MyCubeGrid grid)
        {
            ///Может быть прописано несколько игроков в структуре, владелец определяется в зависимости от того сколько блоков принадлежит игроку
            ///Владелец большинства блоков - владелец структуры
            List<long> bigOwners = grid.BigOwners;
            int count = bigOwners.Count;
            long num = 0;
            if (count > 0 && (ulong)bigOwners[0] > 0UL)
                return bigOwners[0];
            return count > 1 ? bigOwners[1] : num;
        }

        /// <summary>
        /// Получение сущности игрока в мире игры по имени
        /// </summary>
        /// <param name="playerName">Имя игрока</param>
        /// <returns></returns>
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


        /// <summary>
        /// Получение сущности игрока в мире игры по ее ID
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Получение имени игрока по его ID
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static string GetPlayerNameById(long playerId)
        {
            MyIdentity identityById = PlayerUtils.GetIdentityById(playerId);
            return identityById != null ? identityById.DisplayName : "Nobody";
        }

        /// <summary>
        /// Проверка является ли сущность неигровым персонажем
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static bool IsNpc(long playerId)
        {
            return ((MyPlayerCollection)MySession.Static.Players).IdentityIsNpc(playerId);
        }

        /// <summary>
        /// Проверка является ли сущность игровым персонажем
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static bool IsPlayer(long playerId)
        {
            return PlayerUtils.GetPlayerIdentity(playerId) != null;
        }

        /// <summary>
        /// Проверка пользователя на наличие сущности в мире игры (отлов фрикамщиков не админов как я понимаю :))
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static bool HasIdentity(long playerId)
        {
            return ((MyPlayerCollection)MySession.Static.Players).HasIdentity(playerId);
        }

        /// <summary>
        /// Получение сущностей всех игроков на сервере
        /// </summary>
        /// <returns></returns>
        public static List<MyIdentity> GetAllPlayerIdentities()
        {
            if (MySession.Static == null)
                return new List<MyIdentity>();
            List<MyIdentity> list = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).ToList<MyIdentity>();
            List<long> npcs = ((MyPlayerCollection)MySession.Static.Players).GetNPCIdentities().ToList<long>();
            return ((IEnumerable<MyIdentity>)((IEnumerable<MyIdentity>)list).Where<MyIdentity>((Func<MyIdentity, bool>)(i => !npcs.Any<long>((Func<long, bool>)(n => n == i.IdentityId)))).OrderBy<MyIdentity, string>((Func<MyIdentity, string>)(i => i.DisplayName))).ToList<MyIdentity>();
        }

        /// <summary>
        /// Получение обьекта сущности игрока в мире игры (через апи торча (лучше чем через апи игры так как можно расщирить))
        /// </summary>
        /// <param name="identityId"></param>
        /// <returns></returns>
        public static MyIdentity GetPlayerIdentity(long identityId)
        {
            return ((IEnumerable<MyIdentity>)PlayerUtils.GetAllPlayerIdentities()).Where<MyIdentity>((Func<MyIdentity, bool>)(c => c.IdentityId == identityId)).FirstOrDefault<MyIdentity>();
        }

        /// <summary>
        /// Получение интерфейса игрока по его identityId(через апи игры)
        /// </summary>
        /// <param name="identityId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Получение всех игроков на сервере через апи игры
        /// </summary>
        /// <returns></returns>
        public static List<IMyPlayer> GetAllPlayers()
        {
            if (MySession.Static == null)
                return new List<IMyPlayer>();
            List<IMyPlayer> imyPlayerList = new List<IMyPlayer>();
            ((IMyPlayerCollection)MyAPIGateway.Players).GetPlayers(imyPlayerList, (Func<IMyPlayer, bool>)null);
            return imyPlayerList;
        }

        /// <summary>
        /// Проверка является ли игрок администратором
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsAdmin(IMyPlayer player)
        {
            return player != null && (player.PromoteLevel == MyPromoteLevel.Owner || player.PromoteLevel == MyPromoteLevel.Admin || player.PromoteLevel == MyPromoteLevel.Moderator);
        }

        /// <summary>
        /// Получение сущности обьекта в мире по его имени(блок/структура/игрок/астероиды/планеты и т.д.)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static long GetIdentityIdByName(string name)
        {
            MyIdentity myIdentity = ((IEnumerable<MyIdentity>)((MyPlayerCollection)MySession.Static.Players).GetAllIdentities()).Where<MyIdentity>((Func<MyIdentity, bool>)(c => c.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase))).FirstOrDefault<MyIdentity>();
            return myIdentity != null ? myIdentity.IdentityId : 0L;
        }
    }
}
