using System;
using System.Collections.Generic;
using GameConfig;
using General;
using Newtonsoft.Json.Linq;
using SavedData;
using UnityEngine;

namespace RemoteConfig
{
    public class RemoteConfigService : IRemoteConfigService
    {
        private readonly ISavedDataService _savedDataService;
        private readonly RemoteConfigModel _remoteConfigModel;

        public RemoteConfigService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
        }

        public bool ApplyFromRemoteConfigJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson) || rawJson == "{}")
                return false;

            try
            {
                var root = JObject.Parse(rawJson);
                ApplyString(root, "rate_trigger_levels", v => _remoteConfigModel.rateTriggerLevels = v);
                ApplyInt(root, "starting_coins", v => _remoteConfigModel.StartingCoins = v);
                ApplyInt(root, "win_reward_coins", v => _remoteConfigModel.winRewardCoins = v);
                ApplyInt(root, "unlockable_slot_coin_cost", v => _remoteConfigModel.unlockableSlotCoinCost = v);
                ApplyInt(root, "shuffle_cost", v => _remoteConfigModel.shuffleCost = v);
                ApplyBool(root, "is_hardness_config_active", v => _remoteConfigModel.isHardnessConfigActive = v);
                ApplyJson(root, "hardness_config", v => _remoteConfigModel.hardnessConfig = v);
                ApplyInt(root, "rainbow_unlock_level", v => _remoteConfigModel.rainbowUnlockLevel = v);
                ApplyInt(root, "hidden_unlock_level", v => _remoteConfigModel.hiddenUnlockLevel = v);
                ApplyInt(root, "sticky_unlock_level", v => _remoteConfigModel.stickyUnlockLevel = v);
                ApplyInt(root, "iced_unlock_level", v => _remoteConfigModel.icedUnlockLevel = v);
                _savedDataService.SaveData(_remoteConfigModel);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[RemoteConfig] Failed to parse/apply RC: " + e.Message);
                return false;
            }
        }

        
        private void ApplyString(JObject root, string key, Action<string> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            var value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
            if (string.IsNullOrWhiteSpace(value))
                return;

            setter(value);
        }

        private void ApplyInt(JObject root, string key, Action<int> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            if (!TryReadInt(token, out var value))
                return;

            setter(value);
        }

        private void ApplyBool(JObject root, string key, Action<bool> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            if (token.Type == JTokenType.Boolean)
            {
                setter(token.Value<bool>());
                return;
            }

            if (token.Type == JTokenType.String && bool.TryParse(token.Value<string>(), out var value))
            {
                setter(value);
            }
        }

        private void ApplyJson(JObject root, string key, Action<string> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            var rawJson = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
            JObject.Parse(rawJson);
            setter(rawJson);
        }

        private bool TryReadInt(JToken token, out int value)
        {
            if (token.Type == JTokenType.Integer)
            {
                value = token.Value<int>();
                return true;
            }

            if (token.Type == JTokenType.Float)
            {
                value = (int)token.Value<float>();
                return true;
            }

            if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out var parsed))
            {
                value = parsed;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
