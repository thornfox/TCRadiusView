// TC Radius View by ThornFox
// Tested on the RustRentner server: https://rustrentner.de
// License: MIT (see LICENSE)

using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TC Radius View", "ThornFox", "1.0.0")]
    [Description("Toggle a visual outline of the building privilege zone of your tool cupboards, visible only to you.")]
    public class TCRadiusView : RustPlugin
    {
        #region Fields

        private const string PermUse = "tcradiusview.use";
        private const string PermAdmin = "tcradiusview.admin";

        private Configuration config;

        // All tool cupboards currently on the server (kept in sync via spawn/kill hooks)
        private readonly HashSet<BuildingPrivlidge> cupboards = new HashSet<BuildingPrivlidge>();

        // Players who currently have the view enabled, with their refresh timer
        private readonly Dictionary<ulong, Timer> activeViewers = new Dictionary<ulong, Timer>();

        // Reusable buffers to avoid allocations on every refresh
        private readonly List<BuildingPrivlidge> candidates = new List<BuildingPrivlidge>();
        private readonly Vector3[] corners = new Vector3[8];

        private Color outlineColor;
        private Color labelColor;

        #endregion

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Chat commands")]
            public List<string> Commands = new List<string> { "tc" };

            [JsonProperty("Privilege zone extension in meters (Rust default: 16)")]
            public float ZoneExtension = 16f;

            [JsonProperty("Max distance from player to cupboard in meters")]
            public float MaxDistance = 150f;

            [JsonProperty("Max cupboards drawn per refresh")]
            public int MaxCupboards = 25;

            [JsonProperty("Refresh interval in seconds")]
            public float RefreshInterval = 2f;

            [JsonProperty("Outline color (hex)")]
            public string OutlineColor = "#2ECC71";

            [JsonProperty("Label color (hex)")]
            public string LabelColor = "#FFFFFF";

            [JsonProperty("Show label above cupboard")]
            public bool ShowLabel = true;
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new JsonException();
                SaveConfig();
            }
            catch
            {
                PrintWarning("Configuration file is invalid, loading default configuration.");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            // English defaults. German texts are provided via oxide/lang/de/TCRadiusView.json
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use this command.",
                ["Enabled"] = "Tool cupboard zones are now <color=#2ECC71>visible</color>. Type /{0} again to hide them.",
                ["Disabled"] = "Tool cupboard zones are now <color=#E74C3C>hidden</color>.",
                ["NoneFound"] = "No authorized tool cupboards within {0} m right now. The view stays active while you move around.",
                ["Label"] = "TC"
            }, this);
        }

        private string Msg(string key, string userId, params object[] args)
        {
            string message = lang.GetMessage(key, this, userId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(PermUse, this);
            permission.RegisterPermission(PermAdmin, this);

            foreach (string command in config.Commands)
                cmd.AddChatCommand(command, this, nameof(CmdToggle));
        }

        private void OnServerInitialized()
        {
            outlineColor = ParseColor(config.OutlineColor, Color.green);
            labelColor = ParseColor(config.LabelColor, Color.white);

            foreach (BaseNetworkable entity in BaseNetworkable.serverEntities)
            {
                BuildingPrivlidge tc = entity as BuildingPrivlidge;
                if (tc != null) cupboards.Add(tc);
            }
        }

        private void OnEntitySpawned(BuildingPrivlidge tc)
        {
            if (tc != null) cupboards.Add(tc);
        }

        private void OnEntityKill(BuildingPrivlidge tc)
        {
            if (tc != null) cupboards.Remove(tc);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player != null) StopViewing(player.userID);
        }

        private void Unload()
        {
            foreach (Timer t in activeViewers.Values) t?.Destroy();
            activeViewers.Clear();
            cupboards.Clear();
        }

        #endregion

        #region Commands

        private void CmdToggle(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, PermUse) &&
                !permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                SendReply(player, Msg("NoPermission", player.UserIDString));
                return;
            }

            ulong userId = player.userID;

            // Same command toggles the view off again
            if (activeViewers.ContainsKey(userId))
            {
                StopViewing(userId);
                SendReply(player, Msg("Disabled", player.UserIDString));
                return;
            }

            StartViewing(player, command);
        }

        #endregion

        #region Viewing

        private void StartViewing(BasePlayer player, string command)
        {
            ulong userId = player.userID;

            int drawn = DrawZones(player);

            activeViewers[userId] = timer.Every(config.RefreshInterval, () =>
            {
                BasePlayer target = BasePlayer.FindByID(userId);
                if (target == null || !target.IsConnected)
                {
                    StopViewing(userId);
                    return;
                }
                DrawZones(target);
            });

            SendReply(player, Msg("Enabled", player.UserIDString, command));
            if (drawn == 0)
                SendReply(player, Msg("NoneFound", player.UserIDString, config.MaxDistance));
        }

        private void StopViewing(ulong userId)
        {
            Timer t;
            if (activeViewers.TryGetValue(userId, out t))
            {
                t?.Destroy();
                activeViewers.Remove(userId);
            }
        }

        // Collects relevant cupboards and draws their zones for this player only. Returns the number drawn.
        private int DrawZones(BasePlayer player)
        {
            bool adminView = permission.UserHasPermission(player.UserIDString, PermAdmin);
            Vector3 playerPos = player.transform.position;
            float maxDistSqr = config.MaxDistance * config.MaxDistance;

            candidates.Clear();
            foreach (BuildingPrivlidge tc in cupboards)
            {
                if (tc == null || tc.IsDestroyed) continue;
                if ((tc.transform.position - playerPos).sqrMagnitude > maxDistSqr) continue;
                // Regular players only see cupboards they are authorized on
                if (!adminView && !tc.IsAuthed(player)) continue;
                candidates.Add(tc);
            }

            if (candidates.Count == 0) return 0;

            candidates.Sort((a, b) =>
                (a.transform.position - playerPos).sqrMagnitude.CompareTo((b.transform.position - playerPos).sqrMagnitude));

            int count = Mathf.Min(candidates.Count, config.MaxCupboards);
            float duration = config.RefreshInterval + 0.1f;
            string label = Msg("Label", player.UserIDString);

            // ddraw commands are only executed client-side for admins, so the flag is set briefly
            bool tempAdmin = !player.IsAdmin;
            if (tempAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }

            try
            {
                for (int i = 0; i < count; i++)
                    DrawCupboard(player, candidates[i], duration, label);
            }
            finally
            {
                if (tempAdmin)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }

            return count;
        }

        private void DrawCupboard(BasePlayer player, BuildingPrivlidge tc, float duration, string label)
        {
            if (TryGetZoneCorners(tc))
            {
                // Corner index bits: 1 = x, 2 = y, 4 = z. Connect corners that differ in exactly one bit (12 edges).
                for (int i = 0; i < 8; i++)
                {
                    for (int bit = 1; bit <= 4; bit <<= 1)
                    {
                        if ((i & bit) != 0) continue;
                        player.SendConsoleCommand("ddraw.line", duration, outlineColor, corners[i], corners[i | bit]);
                    }
                }
            }

            if (config.ShowLabel)
                player.SendConsoleCommand("ddraw.text", duration, labelColor, tc.transform.position + Vector3.up * 2f, label);
        }

        // Builds an oriented box around all building blocks of the cupboard's building, extended by the privilege range.
        // The real zone has rounded corners; this box is a close, readable approximation.
        private bool TryGetZoneCorners(BuildingPrivlidge tc)
        {
            BuildingManager.Building building = tc.GetBuilding();
            if (building == null || building.buildingBlocks == null || building.buildingBlocks.Count == 0)
                return false;

            // Align the box with the building: use the first foundation's yaw, otherwise any block
            Quaternion frame = Quaternion.identity;
            bool frameSet = false;
            foreach (BuildingBlock block in building.buildingBlocks)
            {
                if (block == null || block.IsDestroyed) continue;
                bool isFoundation = block.ShortPrefabName.StartsWith("foundation");
                if (!frameSet || isFoundation)
                {
                    frame = Quaternion.Euler(0f, block.transform.eulerAngles.y, 0f);
                    frameSet = true;
                    if (isFoundation) break;
                }
            }
            if (!frameSet) return false;

            Vector3 origin = tc.transform.position;
            Quaternion inverse = Quaternion.Inverse(frame);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (BuildingBlock block in building.buildingBlocks)
            {
                if (block == null || block.IsDestroyed) continue;

                OBB obb = block.WorldSpaceBounds();
                for (int i = 0; i < 8; i++)
                {
                    Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                    Vector3 world = obb.position + obb.rotation * Vector3.Scale(obb.extents, sign);
                    Vector3 local = inverse * (world - origin);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }

            Vector3 ext = Vector3.one * config.ZoneExtension;
            min -= ext;
            max += ext;

            for (int i = 0; i < 8; i++)
            {
                Vector3 local = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                corners[i] = origin + frame * local;
            }

            return true;
        }

        #endregion

        #region Helpers

        private static Color ParseColor(string hex, Color fallback)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : fallback;
        }

        #endregion
    }
}
