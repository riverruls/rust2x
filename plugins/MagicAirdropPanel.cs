﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Magic Airdrop Panel", "MJSU", "1.0.1")]
    [Description("Displays if the airdrop event is active")]
    public class MagicAirdropPanel : RustPlugin
    {
        #region Class Fields
        [PluginReference] private readonly Plugin MagicPanel, PlaneCrash, Airstrike;

        private PluginConfig _pluginConfig; //Plugin Config
        private List<CargoPlane> _activeAirdrops = new List<CargoPlane>();
        private bool _isAirdropActive;

        private enum UpdateEnum { All = 1, Panel = 2, Image = 3, Text = 4 }
        #endregion

        #region Setup & Loading
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            string path = $"{Manager.ConfigPath}/MagicPanel/{Name}.json";
            DynamicConfigFile newConfig = new DynamicConfigFile(path);
            if (!newConfig.Exists())
            {
                LoadDefaultConfig();
                newConfig.Save();
            }
            try
            {
                newConfig.Load();
            }
            catch (Exception ex)
            {
                RaiseError("Failed to load config file (is the config file corrupt?) (" + ex.Message + ")");
                return;
            }
            
            newConfig.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(newConfig.ReadObject<PluginConfig>());
            newConfig.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.Panel = new Panel
            {
                Image = new PanelImage
                {
                    Enabled = config.Panel?.Image?.Enabled ?? true,
                    Color = config.Panel?.Image?.Color ?? "#FFFFFFFF",
                    Order = config.Panel?.Image?.Order ?? 0,
                    Width = config.Panel?.Image?.Width ?? 1f,
                    Url = config.Panel?.Image?.Url ?? "http://i.imgur.com/dble6vf.png",
                    Padding = config.Panel?.Image?.Padding ?? new TypePadding(0.05f, 0.05f, 0.05f, 0.05f)
                }
            };
            config.PanelSettings = new PanelRegistration
            {
                BackgroundColor = config.PanelSettings?.BackgroundColor ?? "#FFF2DF08",
                Dock = config.PanelSettings?.Dock ?? "center",
                Order = config.PanelSettings?.Order ?? 0,
                Width = config.PanelSettings?.Width ?? 0.02f
            };
            return config;
        }

        private void OnServerInitialized()
        {
            MagicPanelRegisterPanels();

            NextTick(() =>
            {
                _activeAirdrops = UnityEngine.Object.FindObjectsOfType<CargoPlane>().Where(CanShowPanel).ToList();
                CheckAirdrop();
            });

            timer.Every(_pluginConfig.UpdateRate, CheckAirdrop);
        }

        private void MagicPanelRegisterPanels()
        {
            if (MagicPanel == null)
            {
                PrintError("Missing plugin dependency MagicPanel: https://umod.org/plugins/magic-panel");
                return;
            }

            MagicPanel?.Call("RegisterGlobalPanel", this, Name, JsonConvert.SerializeObject(_pluginConfig.PanelSettings), nameof(GetPanel));
        }

        private void CheckAirdrop()
        {
            _activeAirdrops.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);

            bool areAirdropsActive = _activeAirdrops.Count > 0;

            if (areAirdropsActive != _isAirdropActive)
            {
                _isAirdropActive = areAirdropsActive;
                MagicPanel?.Call("UpdatePanel", Name, (int)UpdateEnum.Image);
            }
        }
        #endregion

        #region uMod Hooks

        private void OnEntitySpawned(CargoPlane plane)
        {
            NextTick(() =>
            {
                if (!CanShowPanel(plane))
                {
                    return;
                }

                _activeAirdrops.Add(plane);
                CheckAirdrop();
            });
        }
        #endregion

        #region MagicPanel Hook
        private Hash<string, object> GetPanel()
        {
            Panel panel = _pluginConfig.Panel;
            PanelImage image = panel.Image;
            if (image != null)
            {
                image.Color = _isAirdropActive ? _pluginConfig.ActiveColor : _pluginConfig.InactiveColor;
            }

            return panel.ToHash();
        }
        #endregion
        
        #region Helper Methods
        private bool CanShowPanel(CargoPlane plane)
        {
            if (IsCrashPlane(plane))
            {
                return false;
            }

            if (IsStrikePlane(plane))
            {
                return false;
            }

            object result = Interface.Call("MagicPanelCanShow", Name, plane);
            if (result is bool)
            {
                return (bool) result;
            }

            return true;
        }
        
        private bool IsCrashPlane(CargoPlane plane)
        {
            return PlaneCrash?.Call<bool>("IsCrashPlane", plane) ?? false;
        }

        private bool IsStrikePlane(CargoPlane plane)
        {
            return Airstrike?.Call<bool>("isStrikePlane", plane) ?? false;
        }
        #endregion

        #region Classes

        private class PluginConfig
        {
            [DefaultValue("#00FF00FF")]
            [JsonProperty(PropertyName = "Active Color")]
            public string ActiveColor { get; set; }

            [DefaultValue("#FFFFFF1A")]
            [JsonProperty(PropertyName = "Inactive Color")]
            public string InactiveColor { get; set; }

            [DefaultValue(5f)]
            [JsonProperty(PropertyName = "Update Rate (Seconds)")]
            public float UpdateRate { get; set; }

            [JsonProperty(PropertyName = "Panel Settings")]
            public PanelRegistration PanelSettings { get; set; }

            [JsonProperty(PropertyName = "Panel Layout")]
            public Panel Panel { get; set; }
        }

        private class PanelRegistration
        {
            public string Dock { get; set; }
            public float Width { get; set; }
            public int Order { get; set; }
            public string BackgroundColor { get; set; }
        }

        private class Panel
        {
            public PanelImage Image { get; set; }
            
            public Hash<string, object> ToHash()
            {
                return new Hash<string, object>
                {
                    [nameof(Image)] = Image.ToHash(),
                };
            }
        }

        private abstract class PanelType
        {
            public bool Enabled { get; set; }
            public string Color { get; set; }
            public int Order { get; set; }
            public float Width { get; set; }
            public TypePadding Padding { get; set; }
            
            public virtual Hash<string, object> ToHash()
            {
                return new Hash<string, object>
                {
                    [nameof(Enabled)] = Enabled,
                    [nameof(Color)] = Color,
                    [nameof(Order)] = Order,
                    [nameof(Width)] = Width,
                    [nameof(Padding)] = Padding.ToHash(),
                };
            }
        }

        private class PanelImage : PanelType
        {
            public string Url { get; set; }
            
            public override Hash<string, object> ToHash()
            {
                Hash<string, object> hash = base.ToHash();
                hash[nameof(Url)] = Url;
                return hash;
            }
        }

        private class TypePadding
        {
            public float Left { get; set; }
            public float Right { get; set; }
            public float Top { get; set; }
            public float Bottom { get; set; }

            public TypePadding(float left, float right, float top, float bottom)
            {
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }
            
            public Hash<string, object> ToHash()
            {
                return new Hash<string, object>
                {
                    [nameof(Left)] = Left,
                    [nameof(Right)] = Right,
                    [nameof(Top)] = Top,
                    [nameof(Bottom)] = Bottom
                };
            }
        }
        #endregion
    }
}
