using AltV.Net;
using AltV.Net.Data;
using AltV.Net.Elements.Entities;
using Server.Factories;
using System.Net.Http.Headers;
using System.Text;
using Console = Server.Helpers.Console;

namespace Server
{
    public class Settings
    {
        public const int MaxRadioChannels = 9;
        public const string UNIQUE_SERVER_ID = "";
        public const int CHANNEL_ID = 1;
        public const string CHANNEL_PASSWORD = "";
        public const int DEFAULT_CHANNEL_ID = 0;
    }

    internal class Voice : IScript
    {
        #region Properties
        private static HashSet<string> NameSet = new HashSet<string>();
        private static Dictionary<int, string> RadioFrequencyMap = new Dictionary<int, string>();
        #endregion

        #region Core functions
        public static string GenerateRandomString(int length = 50, string possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
        {
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(possible.Length);

                stringBuilder.Append(possible[index]);
            }

            return stringBuilder.ToString();
        }

        public static string GenerateRandomName(YaCAPlayer player)
        {
            string name = null;

            for (int i = 0; i < 100; i++)
            {
                string generatedName = GenerateRandomString(15, "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789");

                if (!NameSet.Contains(generatedName))
                {
                    name = generatedName;
                    NameSet.Add(name);
                    break;
                }
            }

            if (string.IsNullOrEmpty(name) && player.Exists)
            {
                player.Kick("No TeamSpeak-Name!");
            }

            return name;
        }

        public static void Connect(YaCAPlayer player)
        {
            player.VoiceSettings.VoiceFirstConnect = true;

            player.Emit("client:yaca:init", Settings.UNIQUE_SERVER_ID, Settings.CHANNEL_ID, Settings.DEFAULT_CHANNEL_ID, Settings.CHANNEL_PASSWORD, player.VoiceSettings.IngameName);
        }

        public static void ConnectToVoice(YaCAPlayer player)
        {
            if (!player.Exists)
            {
                return;
            }

            string name = GenerateRandomName(player);

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            player.VoiceSettings = new VoiceSettings
            {
                VoiceRange = 3,
                VoiceFirstConnect = false,
                MaxVoceRangeInMeter = 15,
                Muted = false,
                IngameName = name
            };

            player.RadioSettings = new RadioSettings
            {
                Activated = false,
                CurrentChannel = 1,
                HasLong = false,
                Frequencies = new Dictionary<int, string>()
            };

            Connect(player);
        }

        public static void ChangeMegaphoneState(YaCAPlayer player, bool state, bool forced = false)
        {
            if (!state && player.HasStreamSyncedMetaData("yaca:megaphoneactive"))
            {
                player.DeleteStreamSyncedMetaData("yaca:megaphoneactive");

                if (forced)
                {
                    player.SetLocalMetaData("lastMegaphoneState", false);
                }
            } else if (state && !player.HasStreamSyncedMetaData("yaca:megaphoneactive"))
            {
                player.SetStreamSyncedMetaData("yaca:megaphoneactive", 30);
            }
        }

        public static void ChangePlayerAliveStatus(YaCAPlayer player, bool alive)
        {
            if (!player.IsDead && alive)
            {
                return;
            }

            player.VoiceSettings.Muted = !alive;

            Alt.EmitAllClients("client:yaca:muteTarget", player.Id, !alive);

            if (player.VoicePlugin != null)
            {
                player.VoicePlugin.ForceMuted = !alive;
            }
        }

        public static void IsLongRadioPermitted(YaCAPlayer player)
        {
            player.RadioSettings.HasLong = true;
        }

        public static void LeaveRadioFrequency(YaCAPlayer player, int channel, string frequency)
        {
            if (!player.Exists)
            {
                return;
            }

            if (frequency == "0")
            {
                frequency = player.RadioSettings.Frequencies[channel];
            }

            int parsedFrequency = int.Parse(frequency);

            if (!RadioFrequencyMap.ContainsKey(parsedFrequency))
            {
                return;
            }

            RadioFrequencyMap[parsedFrequency].Remove(player.Id);

            player.RadioSettings.Frequencies[channel] = "0";

            player.Emit("client:yaca:setRadioFreq", channel, 0);

            if (RadioFrequencyMap[parsedFrequency] == null)
            {
                RadioFrequencyMap.Remove(parsedFrequency);
            }
        }

        public static void CallPlayer(YaCAPlayer player, YaCAPlayer target, bool state)
        {
            if (!player.Exists || target == null || !target.Exists)
            {
                return;
            }

            target.Emit("client:yaca:phone", player.Id, state);
            player.Emit("client:yaca:phone", target.Id, state);
        }

        public static void CallPlayerOldEffect(YaCAPlayer player, YaCAPlayer target, bool state)
        {
            if (!player.Exists || target == null || !target.Exists)
            {
                return;
            }

            target.Emit("client:yaca:phoneOld", player.Id, state);
            player.Emit("client:yaca:phoneOld", target.Id, state);
        }

        public static void MuteOnPhone(YaCAPlayer player, bool state)
        {
            if (!player.Exists)
            {
                return;
            }

            if (state)
            {
                player.SetSyncedMetaData("yaca:isMutedOnPhone", state);
            } else
            {
                player.DeleteSyncedMetaData("yaca:isMutedOnPhone");
            }
        }

        public static void EnablePhoneSpeaker(YaCAPlayer player, bool state, int[] phoneCallMemberIds)
        {
            if (!player.Exists)
            {
                return;
            }

            if (state)
            {
                player.SetSyncedMetaData("yaca:phoneSpeaker", phoneCallMemberIds);
            } else
            {
                player.DeleteSyncedMetaData("yaca:phoneSpeaker");
            }
        }
        #endregion

        #region Core events
        [ScriptEvent(ScriptEventType.PlayerConnect)]
        public void PlayerConnect(YaCAPlayer player, string reason)
        {
            ConnectToVoice(player);
        }

        [ScriptEvent(ScriptEventType.PlayerDisconnect)]
        public void HandlePlayerDisconnect(YaCAPlayer player, string reason)
        {
            int playerId = player.Id;

            NameSet.Remove(player.VoiceSettings.IngameName);

            foreach (var key in RadioFrequencyMap.Keys.ToList())
            {
                if (RadioFrequencyMap.TryGetValue(key, out var value))
                {
                    value.Remove(playerId);

                    if (value == null)
                    {
                        RadioFrequencyMap.Remove(key);
                    }
                }
            }
        }

        [ScriptEvent(ScriptEventType.PlayerLeaveVehicle)]
        public void HandlePlayerLeaveVehicle(IVehicle vehicle, YaCAPlayer player, byte seat)
        {
            ChangeMegaphoneState(player, false, true);
        }

        [ScriptEvent(ScriptEventType.ColShape)]
        public void HandleEntityHitColShape(YaCAColShape colShape, IEntity entity, bool state)
        {
            switch (entity)
            {
                case YaCAPlayer player:
                    if (state)
                    {
                        if (!(colShape.VoiceRangeInfos is VoiceRangeInfos voiceRangeInfos) || !(entity is YaCAPlayer) || !player.Exists)
                        {
                            return;
                        }

                        player.Emit("client:yaca:setMaxVoiceRange", voiceRangeInfos.MaxRange);

                        switch (voiceRangeInfos.MaxRange)
                        {
                            case 5:
                                player.VoiceSettings.MaxVoceRangeInMeter = 20;
                                break;
                            case 6:
                                player.VoiceSettings.MaxVoceRangeInMeter = 25;
                                break;
                            case 7:
                                player.VoiceSettings.MaxVoceRangeInMeter = 30;
                                break;
                            case 8:
                                player.VoiceSettings.MaxVoceRangeInMeter = 40;
                                break;
                        }

                        Console.Log(player.Name + " has entered a col shape.");
                    }
                    else
                    {
                        if (!(colShape.VoiceRangeInfos is VoiceRangeInfos voiceRangeInfos) || !(entity is YaCAPlayer) || !player.Exists)
                        {
                            return;
                        }

                        player.VoiceSettings.VoiceRange = 15;

                        if (player.VoiceSettings.VoiceRange > 15)
                        {
                            player.Emit("client:yaca:setMaxVoiceRange", 15);
                        }

                        Console.Log(player.Name + " has left a col shape.");
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Custom standard events
        [ClientEvent("server:yaca:noVoicePlugin")]
        public void PlayerNoVoicePlugin(YaCAPlayer player)
        {
            if (player.Exists)
            {
                player.Kick("No YaCA-Plugin!");
            }
        }

        [ClientEvent("server:yaca:addPlayer")]
        public static void AddNewPlayer(YaCAPlayer player, int clientId)
        {
            if (!player.Exists || clientId == 0)
            {
                return;
            }

            player.VoicePlugin = new VoicePlugin
            {
                ClientId = clientId,
                ForceMuted = player.VoiceSettings.Muted,
                Range = player.VoiceSettings.VoiceRange,
                PlayerId = player.Id
            };

            Alt.EmitAllClients("client:yaca:addPlayers", player.VoicePlugin);

            var allPlayers = Alt.GetAllPlayers();
            var allPlayersData = new List<VoicePlugin>();

            foreach (YaCAPlayer playerServer in allPlayers)
            {
                if (playerServer.VoicePlugin == null && playerServer.Id == player.Id)
                {
                    allPlayersData.Add(playerServer.VoicePlugin);
                }
            }

            player.Emit("client:yaca:addPlayers", allPlayersData);
        }

        [ClientEvent("server:yaca:wsReady")]
        public void PlayerReconnect(YaCAPlayer player, bool isFirstConnect)
        {
            if (!player.Exists)
            {
                return;
            }

            if (!isFirstConnect)
            {
                string name = GenerateRandomName(player);

                if (string.IsNullOrEmpty(name))
                {
                    return;
                }

                NameSet.Remove(player.VoiceSettings.IngameName);

                player.VoiceSettings.IngameName = name;
            }

            Connect(player);
        }

        [ClientEvent("server:yaca:lipsync")]
        public void LipSync(YaCAPlayer player, bool state, int[] players)
        {
            var nearTargets = Alt.GetAllPlayers().Where(p => p.Exists && players.Contains(p.Id));

            if (nearTargets.Any())
            {
                foreach (var target in nearTargets)
                {
                    target.Emit("client:yaca:player:sync:lips", player.Id, state);
                }
            }
        }

        [ClientEvent("server:yaca:changeVoiceRange")]
        public void ChangeVoiceRange(YaCAPlayer player, int range)
        {
            if (player.VoiceSettings.MaxVoceRangeInMeter < range)
            {
                player.Emit("clietn:yaca:setMaxVoiceRange", 15);
                return;
            }

            player.VoiceSettings.VoiceRange = range;

            Alt.EmitAllClients("client:yaca:changeVoiceRange", player.Id, player.VoiceSettings.VoiceRange);

            if (player.VoicePlugin != null)
            {
                player.VoicePlugin.Range = range;
            }
        }

        [ClientEvent("server:yaca:useMegaphone")]
        public void PlayerUseMegaphone(YaCAPlayer player, bool state)
        {
            if (!player.IsInVehicle && !player.HasLocalMetaData("canUseMegaphone"))
            {
                return;
            }

            if (player.IsInVehicle && (!player.Vehicle.Exists || new[] { 1, 2 }.All(seat => seat != player.Seat)))
            {
                return;
            }

            if ((!state && !player.HasStreamSyncedMetaData("yaca:megaphoneactive")) || (state && player.HasStreamSyncedMetaData("yaca:megaphoneactive")))
            {
                return;
            }

            ChangeMegaphoneState(player, state);
        }
        #endregion

        #region Custom radio events
        [ClientEvent("server:yaca:enableRadio")]
        public void EnableRadio(YaCAPlayer player, bool state)
        {
            if (!player.Exists)
            {
                return;
            }

            player.RadioSettings.Activated = state;

            IsLongRadioPermitted(player);

            player.SetStreamSyncedMetaData("yaca:radioEnabled", state);
        }

        [ClientEvent("server:yaca:changeRadioFrequency")]
        public void ChangeRadioFrequency(YaCAPlayer player, int channel, string frequency)
        {
            if (!player.Exists)
            {
                return;
            }

            if (!player.RadioSettings.Activated)
            {
                return;
            }

            if (!int.TryParse(frequency, out int parsetFrequency) || parsetFrequency < 1 || parsetFrequency > Settings.MaxRadioChannels)
            {
                Console.Error("Error: radio frequency!");
                return;
            }

            if (frequency == "0")
            {
                LeaveRadioFrequency(player, channel, frequency);
                return;
            }

            int parsedFrequency = int.Parse(frequency);

            player.RadioSettings.Frequencies[channel] = frequency;

            player.Emit("client:yaca:setRadioFreq", channel, frequency);
        }

        [ClientEvent("server:yaca:muteRadioChannel")]
        public void RadioChannelMute(YaCAPlayer player, int channel)
        {
            if (player == null || player.RadioSettings == null || player.RadioSettings.Frequencies == null)
            {
                return;
            }

            string radioFrequency = channel < player.RadioSettings.Frequencies.Count ? player.RadioSettings.Frequencies[channel] : null;

            if (string.IsNullOrEmpty(radioFrequency) || !int.TryParse(radioFrequency, out int parsedRadioFrequency))
            {
                return;
            }

            if (RadioFrequencyMap.TryGetValue(parsedRadioFrequency, out var foundPlayer))
            {
                foundPlayer.VoiceSettings.Muted = !foundPlayer.VoiceSettings.Muted;
                player.Emit("client:yaca:setRadioMuteState", channel, foundPlayer.VoiceSettings.Muted);
            }
        }

        [ClientEvent("server:yaca:radioTalking")]
        public void RadioTalkingState(YaCAPlayer player, bool state)
        {
            if (!player.Exists)
            {
                return;
            }

            if (!player.RadioSettings.Activated)
            {
                return;
            }

            string radioFrequency = player.RadioSettings.Frequencies[player.RadioSettings.CurrentChannel];
            int parsedRadioFrequency = int.Parse(radioFrequency);

            if (string.IsNullOrEmpty(radioFrequency))
            {
                return;
            }

            int playerID = player.Id;

            if (!RadioFrequencyMap.TryGetValue(parsedRadioFrequency, out var getPlayers)) return;

            List<IPlayer[]> targets = new List<IPlayer[]>();
            Dictionary<int, Dictionary<string, bool>> radioInfos = new Dictionary<int, Dictionary<string, bool>>();

            foreach (var kvp in getPlayers)
            {
                int key = kvp.Key;
                var values = kvp.Value;

                if (values.Muted)
                {
                    if (key == playerID)
                    {
                        targets.Clear();
                        break;
                    }
                    continue;
                }

                if (key == playerID) continue;

                YaCAPlayer target = Alt.GetAllPlayers().FirstOrDefault(player => player.Id == player.Id);

                if (target == null || !target.Exists || !target.RadioSettings.Activated) continue;

                bool shortRange = !player.RadioSettings.HasLong && !target.RadioSettings.HasLong;
                if ((player.RadioSettings.HasLong && target.RadioSettings.HasLong) || shortRange)
                {
                    targets.Add(target);

                    Dictionary<string, bool> radioInfo = new Dictionary<string, bool>
            {
                { "shortRange", shortRange }
            };
                    radioInfos.Add(target.Id, radioInfo);
                }
            }

            if (targets.Count > 0)
            {
                Dictionary<int, Dictionary<string, bool>> radioInfoDictionary = new Dictionary<int, Dictionary<string, bool>>();
                foreach (var kvp in radioInfos)
                {
                    radioInfoDictionary.Add(kvp.Key, kvp.Value);
                }

                Alt.EmitClients(targets, "client:yaca:radioTalking", player.Id, radioFrequency, state, radioInfoDictionary);
            }
        }

        [ClientEvent("server:yaca:changeActiveRadioChannel")]
        public void RadioActiveChannelChange(YaCAPlayer player, int channel)
        {
            if (!player.Exists || double.IsNaN(channel) || channel < 1 || channel > Settings.MaxRadioChannels)
            {
                return;
            }

            player.RadioSettings.CurrentChannel = channel;
        }
        #endregion
    }
}
