using HarmonyLib;
using BetterTeamUpgrades.Config;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

namespace BetterTeamUpgrades.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar), "Start")]
    public class LateJoinPlayerUpgradeSyncPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerAvatar __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            RunManager rm = RunManager.instance;
            if (rm == null ||
                rm.levelCurrent == rm.levelMainMenu ||
                rm.levelCurrent == rm.levelRecording ||
                rm.levelCurrent == rm.levelSplashScreen)
            {
                return;
            }

            __instance.StartCoroutine(SyncWithDelay(__instance));
        }

        private static IEnumerator SyncWithDelay(PlayerAvatar newPlayer)
        {
            float timeWaited = 0f;
            float timeout = 10f;

            while (string.IsNullOrEmpty(SemiFunc.PlayerGetSteamID(newPlayer)) && timeWaited < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                timeWaited += 0.5f;
            }

            yield return new WaitForSeconds(1f);

            if (StatsManager.instance == null || PunManager.instance == null) yield break;

            PhotonView punView = PunManager.instance.GetComponent<PhotonView>();
            if (punView == null)
            {
                Plugin.Log.LogWarning("Late Join: PunManager PhotonView not found.");
                yield break;
            }

            string newPlayerID = SemiFunc.PlayerGetSteamID(newPlayer);
            if (string.IsNullOrEmpty(newPlayerID))
            {
                Plugin.Log.LogWarning($"Late Join: Timed out waiting for SteamID for player {newPlayer.photonView.ViewID}. Skipping sync.");
                yield break;
            }

            string playerName = (string)AccessTools.Field(typeof(PlayerAvatar), "playerName").GetValue(newPlayer);

            Plugin.Log.LogInfo($"Late Join: Player {playerName} ({newPlayerID}) is ready. Starting sync...");

            List<PlayerAvatar> players = SemiFunc.PlayerGetAll();
            List<string> steamIDs = players
                .Select(p => SemiFunc.PlayerGetSteamID(p))
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            foreach (KeyValuePair<string, Dictionary<string, int>> kvp in StatsManager.instance.dictionaryOfDictionaries)
            {
                if (!kvp.Key.StartsWith("playerUpgrade")) continue;

                string fullKey = kvp.Key;
                var upgradeDict = kvp.Value;

                int maxLevel = 0;
                foreach (string id in steamIDs)
                {
                    if (upgradeDict.TryGetValue(id, out int level))
                    {
                        if (level > maxLevel) maxLevel = level;
                    }
                }

                if (maxLevel > 0)
                {
                    bool isVanilla = SharedUpgradesPatch.VanillaKeys.Contains(fullKey);

                    foreach (string id in steamIDs)
                    {
                        int currentLevel = upgradeDict.ContainsKey(id) ? upgradeDict[id] : 0;
                        int diff = maxLevel - currentLevel;

                        if (diff > 0)
                        {
                            if (isVanilla)
                            {
                                string commandName = fullKey.Substring("playerUpgrade".Length);
                                punView.RPC("TesterUpgradeCommandRPC", RpcTarget.Others, id, commandName, diff);

                                if (upgradeDict.ContainsKey(id)) upgradeDict[id] += diff;
                                else upgradeDict[id] = diff;
                            }
                            else
                            {
                                punView.RPC("UpdateStatRPC", RpcTarget.Others, fullKey, id, maxLevel);
                                upgradeDict[id] = maxLevel;
                            }

                            string pName = "Unknown";
                            PlayerAvatar pObj = players.FirstOrDefault(p => SemiFunc.PlayerGetSteamID(p) == id);
                            if (pObj != null) pName = (string)AccessTools.Field(typeof(PlayerAvatar), "playerName").GetValue(pObj);

                            Plugin.Log.LogInfo($"Late Join: Synced {fullKey} for {pName} (+{diff})");
                        }
                    }
                }
            }
            Plugin.Log.LogInfo($"Late Join: Sync complete for {playerName}.");
        }
    }
}