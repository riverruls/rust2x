using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Magic Message Panel", "MJSU", "1.0.1")]
    [Description("Displays messages in magic panel")]
    public class MagicMessagePanel : RustPlugin
    {
        #region Class Fields
        [PluginReference] private readonly Plugin MagicPanel;

        private PluginConfig _pluginConfig; //Plugin Config

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
            config.Panels = config.Panels ?? new Hash<string, PanelData>
            {
                [$"{Name}_1"] = new PanelData
                {
                    Panel = new Panel
                    {
                        Text = new PanelText
                        {
                            Enabled = true,
                            Color = "#FFFFFFFF",
                            Order = 1,
                            Width = 1f,
                            FontSize = 14,
                            Padding = new TypePadding(0.05f, 0.05f, 0.1f, 0.00f),
                            TextAnchor = TextAnchor.MiddleCenter,
                            Text = ""
                        }
                    },
                    PanelSettings = new PanelRegistration
                    {
                        BackgroundColor = "#FFF2DF08",
                        Dock = "bottom",
                        Order = 0,
                        Width = 0.2954f
                    },
                    Messages = new List<string>
                    {
                        "Message 1",
                        "Message 2",
                        "<color=#FF0000>This message is red</color>"
                    },
                    UpdateRate = 15f,
                    Enabled = true
                }
            };
            
            return config;
        }

        private void OnServerInitialized()
        {
            MagicPanelRegisterPanels();

            foreach (IGrouping<float, KeyValuePair<string, PanelData>> panelUpdates in _pluginConfig.Panels.Where(p => p.Value.Enabled).GroupBy(p => p.Value.UpdateRate))
            {
                timer.Every(panelUpdates.Key, () =>
                {
                    foreach (KeyValuePair<string, PanelData> data in panelUpdates)
                    {
                        MagicPanel?.Call("UpdatePanel", data.Key, (int)UpdateEnum.Text);
                    }
                });
            }
        }

        private void MagicPanelRegisterPanels()
        {
            if (MagicPanel == null)
            {
                PrintError("Missing plugin dependency MagicPanel: https://umod.org/plugins/magic-panel");
                return;
            }
        
            foreach (KeyValuePair<string, PanelData> panel in _pluginConfig.Panels)
            {
                if (!panel.Value.Enabled)
                {
                    continue;
                }
                
                MagicPanel?.Call("RegisterGlobalPanel", this, panel.Key, JsonConvert.SerializeObject(panel.Value.PanelSettings), nameof(GetPanel));
            }
        }
        #endregion

        #region MagicPanel Hook

        private Hash<string, object> GetPanel(string panelName)
        {
            PanelData panelData = _pluginConfig.Panels[panelName];
            Panel panel = panelData.Panel;
            PanelText text = panel.Text;
            if (text != null)
            {
                if (panelData.Messages.Count == 0)
                {
                    text.Text = string.Empty;
                }
                else if (panelData.Messages.Count == 1)
                {
                    text.Text = panelData.Messages[0];
                }
                else
                {
                    text.Text = panelData.Messages.Where(m => text.Text != m).ToList().GetRandom();
                }
            }

            return panel.ToHash();
        }
        #endregion

        #region Classes
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Message Panels")]
            public Hash<string, PanelData> Panels { get; set; }
        }

        private class PanelData
        {
            [JsonProperty(PropertyName = "Panel Settings")]
            public PanelRegistration PanelSettings { get; set; }
            
            [JsonProperty(PropertyName = "Panel Layout")]
            public Panel Panel { get; set; }
            
            [JsonProperty(PropertyName = "Messages")]
            public List<string> Messages { get; set; }
            
            [JsonProperty(PropertyName = "Update Rate (Seconds)")]
            public float UpdateRate { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }
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
            public PanelText Text { get; set; }
            
            public Hash<string, object> ToHash()
            {
                return new Hash<string, object>
                {
                    [nameof(Text)] = Text.ToHash()
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

        private class PanelText : PanelType
        {
            public string Text { get; set; }
            public int FontSize { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public TextAnchor TextAnchor { get; set; }
            
            public override Hash<string, object> ToHash()
            {
                Hash<string, object> hash = base.ToHash();
                hash[nameof(Text)] = Text;
                hash[nameof(FontSize)] = FontSize;
                hash[nameof(TextAnchor)] = TextAnchor;
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
