using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Steamworks;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("SkinBox", "FuJiCuRa", "1.16.3", ResourceId = 17)]
    [Description("SkinBox is a plugin to convert any skinnable item into each skin variant")]
    internal class SkinBox : RustPlugin
    {
        [PluginReference] private Plugin QuickSort, ServerRewards, Economics, StacksExtended;
        private bool skinsLoaded;
        private bool Initialized;
        private bool Changed;
        private static SkinBox skinBox = null;
        private bool activeServerRewards;
        private bool activeEconomics;
        private bool activePointSystem;
        private int maxItemsShown;
        private bool _stacksExtendedExtrasDisabled;
        private Dictionary<string, LinkedList<ulong>> skinsCache = new Dictionary<string, LinkedList<ulong>>();
        private Dictionary<string, LinkedList<ulong>> skinsCacheLimited = new Dictionary<string, LinkedList<ulong>>();
        private Dictionary<string, int> approvedSkinsCount = new Dictionary<string, int>();
        private Dictionary<string, DateTime> cooldownTimes = new Dictionary<string, DateTime>();
        private Dictionary<string, string> NameToItemName = new Dictionary<string, string>();
        private Dictionary<string, string> ItemNameToName = new Dictionary<string, string>();
        private Dictionary<string, object> manualAddedSkinsPre = new Dictionary<string, object>();
        private Dictionary<string, List<ulong>> manualAddedSkins = new Dictionary<string, List<ulong>>();
        private List<ulong> excludedSkins = new List<ulong>();
        private List<object> excludedSkinsPre = new List<object>();
        private Dictionary<ulong, SBH> activeSkinBoxes = new Dictionary<ulong, SBH>();
        private Dictionary<ulong, string> skinWorkshopNames = new Dictionary<ulong, string>();
        private List<object> altSkinBoxCommand;
        private string skinBoxCommand;
        private string permissionUse;
        private bool showLoadedSkinCounts;
        private int exludedSkinsAuthLevel;
        private bool hideQuickSort;
        private string steamApiKey;
        private int accessOverrideAuthLevel;
        private bool allowStackedItems;
        private bool enableCustomPerms;
        private string permCustomPlayerwearable;
        private string permCustomWeapon;
        private string permCustomDeployable;
        private bool useInbuiltSkins;
        private bool useApprovedSkins;
        private int approvedSkinsLimit;
        private bool useManualAddedSkins;
        private int maxPagesShown;
        private bool enableCooldown;
        private int cooldownBox;
        private bool cooldownOverrideAdmin;
        private int cooldownOverrideAuthLevel;
        private bool activateAfterSkinTaken;
        private bool enableUsageCost;
        private bool useServerRewards;
        private bool useEconomics;
        private int costBoxOpen;
        private int costWeapon;
        private int costPlayerwearable;
        private int costDeployable;
        private bool costExcludeAdmins;
        private string costExcludePerm;
        private bool costExcludePermEnabled;

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            Dictionary<string, object> data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }

            return value;
        }

        private object getSkincache()
        {
            if (skinsLoaded) return new Dictionary<string, LinkedList<ulong>>(skinsCache);
            else return false;
        }

        private void LoadVariables()
        {
            useInbuiltSkins = Convert.ToBoolean(GetConfig("AvailableSkins", "useInbuiltSkins", true));
            useApprovedSkins = Convert.ToBoolean(GetConfig("AvailableSkins", "useApprovedSkins", false));
            approvedSkinsLimit = Convert.ToInt32(GetConfig("AvailableSkins", "approvedSkinsLimit", -1));
            useManualAddedSkins = Convert.ToBoolean(GetConfig("AvailableSkins", "useManualAddedSkins", true));
            maxPagesShown = Convert.ToInt32(GetConfig("AvailableSkins", "maxPagesShown", 2));
            skinBoxCommand = Convert.ToString(GetConfig("Settings", "skinBoxCommand", "skinbox"));
            altSkinBoxCommand = (List<object>)GetConfig("Settings", "altSkinBoxCommands", new List<object>());
            permissionUse = Convert.ToString(GetConfig("Settings", "permissionUse", "skinbox.use"));
            showLoadedSkinCounts = Convert.ToBoolean(GetConfig("Settings", "showLoadedSkinCounts", true));
            exludedSkinsAuthLevel = Convert.ToInt32(GetConfig("Settings", "exludedSkinsAuthLevel", 2));
            accessOverrideAuthLevel = Convert.ToInt32(GetConfig("Settings", "accessOverrideAuthLevel", 2));
            hideQuickSort = Convert.ToBoolean(GetConfig("Settings", "hideQuickSort", true));
            allowStackedItems = Convert.ToBoolean(GetConfig("Settings", "allowStackedItems", false));
            steamApiKey = Convert.ToString(GetConfig("Settings", "steamApiKey", "https://steamcommunity.com/dev/apikey << get it THERE and saved it HERE"));
            enableCustomPerms = Convert.ToBoolean(GetConfig("CustomPermissions", "enableCustomPerms", false));
            permCustomPlayerwearable = Convert.ToString(GetConfig("CustomPermissions", "permCustomPlayerwearable", "skinbox.playerwearable"));
            permCustomWeapon = Convert.ToString(GetConfig("CustomPermissions", "permCustomWeapon", "skinbox.weapon"));
            permCustomDeployable = Convert.ToString(GetConfig("CustomPermissions", "permCustomDeployable", "skinbox.deployable"));
            enableCooldown = Convert.ToBoolean(GetConfig("Cooldown", "enableCooldown", false));
            cooldownBox = Convert.ToInt32(GetConfig("Cooldown", "cooldownBox", 60));
            cooldownOverrideAdmin = Convert.ToBoolean(GetConfig("Cooldown", "cooldownOverrideAdmin", true));
            cooldownOverrideAuthLevel = Convert.ToInt32(GetConfig("Cooldown", "cooldownOverrideAuthLevel", 2));
            activateAfterSkinTaken = Convert.ToBoolean(GetConfig("Cooldown", "activateAfterSkinTaken", true));
            manualAddedSkinsPre = (Dictionary<string, object>) GetConfig("SkinsAdded", "SkinList", new Dictionary<string, object> { });
            excludedSkinsPre = (List<object>) GetConfig("SkinsExcluded", "SkinList", new List<object> { });
            enableUsageCost = Convert.ToBoolean(GetConfig("UsageCost", "enableUsageCost", false));
            useServerRewards = Convert.ToBoolean(GetConfig("UsageCost", "useServerRewards", true));
            useEconomics = Convert.ToBoolean(GetConfig("UsageCost", "useEconomics", false));
            costBoxOpen = Convert.ToInt32(GetConfig("UsageCost", "costBoxOpen", 5));
            costWeapon = Convert.ToInt32(GetConfig("UsageCost", "costWeapon", 30));
            costPlayerwearable = Convert.ToInt32(GetConfig("UsageCost", "costPlayerwearable", 20));
            costDeployable = Convert.ToInt32(GetConfig("UsageCost", "costDeployable", 10));
            costExcludeAdmins = Convert.ToBoolean(GetConfig("UsageCost", "costExcludeAdmins", true));
            costExcludePerm = Convert.ToString(GetConfig("UsageCost", "costExcludePerm", "skinbox.costexcluded"));
            costExcludePermEnabled = Convert.ToBoolean(GetConfig("UsageCost", "costExcludePermEnabled", false));
            bool configremoval = false;
            if ((Config.Get("AvailableSkins") as Dictionary<string, object>).ContainsKey("useWebskinsRankedByTrend"))
            {
                (Config.Get("AvailableSkins") as Dictionary<string, object>).Remove("useWebskinsRankedByTrend");
                (Config.Get("AvailableSkins") as Dictionary<string, object>).Remove("usedRankedByTrendDays");
                (Config.Get("AvailableSkins") as Dictionary<string, object>).Remove("usedConnectionsToWorkshop");
                configremoval = true;
            }

            if (!Changed & !configremoval)
                return;
            SaveConfig();
            Changed = false;
            configremoval = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    {"NoPermission", "You don't have permission to use the SkinBox"},
                    {"ToNearPlayer", "The SkinBox is currently not usable at this place"},
                    {"CooldownTime", "You need to wait {0} seconds to re-open the SkinBox again"},
                    {"NotEnoughBalanceOpen", "You need at least '{0}' bucks to open the SkinBox"},
                    {"NotEnoughBalanceUse", "You would need at least '{0}' bucks to skin '{1}'"},
                    {"NotEnoughBalanceTake", "'{0}' was not skinned. You had not enough bucks"},
                }, this);
        }

        private void Loaded()
        {
            LoadVariables();
            LoadDefaultMessages();
            if (allowStackedItems) SbscrbSplt();
            else UnsbscrbSplt();

            cmd.AddChatCommand(skinBoxCommand, this, "cmdSknBx");

            for (int i = 0; i < altSkinBoxCommand.Count; i++)
                cmd.AddChatCommand(altSkinBoxCommand[i].ToString(), this, "cmdSknBx");

            permission.RegisterPermission(permissionUse, this);
            permission.RegisterPermission(permCustomPlayerwearable, this);
            permission.RegisterPermission(permCustomWeapon, this);
            permission.RegisterPermission(permCustomDeployable, this);
            permission.RegisterPermission(costExcludePerm, this);
            skinsCache = new Dictionary<string, LinkedList<ulong>>();
            skinsCacheLimited = new Dictionary<string, LinkedList<ulong>>();
            approvedSkinsCount = new Dictionary<string, int>();
            NameToItemName = new Dictionary<string, string>();
            ItemNameToName = new Dictionary<string, string>();
            skinsLoaded = false;
            skinBox = this;
            if (maxPagesShown < 1) maxPagesShown = 1;
            maxItemsShown = 42 * maxPagesShown;
        }

        private void Unload()
        {
            List<SBH> objs = UnityEngine.Object.FindObjectsOfType<SBH>().ToList();
            if (objs.Count > 0)
                foreach (SBH obj in objs)
                {
                    if (obj.looter == null) continue;
                    obj.looter.EndLooting();
                    obj.PlayerStoppedLooting(obj.looter);
                    UnityEngine.Object.Destroy(obj);
                }

            if (Interface.Oxide.IsShuttingDown) return;
        }

        private void OnServerInitialized()
        {
            if (steamApiKey == null || steamApiKey == string.Empty || steamApiKey.Length != 32)
            {
                PrintWarning(_("FxvaObk pbasvt arrqf `fgrnzNcvXrl` sebz `uggcf://fgrnzpbzzhavgl.pbz/qri/ncvxrl` >> Cyhtva haybnqrq"));
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (enableUsageCost)
            {
                if (ServerRewards != null && useServerRewards) activeServerRewards = true;
                if (Economics != null && useEconomics) activeEconomics = true;
                if (activeServerRewards && activeEconomics) activeEconomics = false;
                if (activeServerRewards || activeEconomics) activePointSystem = true;
            }

            if (allowStackedItems && StacksExtended)
            {
                _stacksExtendedExtrasDisabled = (bool) StacksExtended.CallHook("DisableExtraFeatures");
                if (!_stacksExtendedExtrasDisabled)
                {
                    Unsubscribe(nameof(CanStackItem));
                    Unsubscribe(nameof(OnItemSplit));
                }
                else
                {
                    Subscribe(nameof(CanStackItem));
                    Subscribe(nameof(OnItemSplit));
                }
            }

            foreach (Skinnable skin in Skinnable.All.ToList())
            {
                if (skin.Name == null || skin.Name == string.Empty) continue;
                if (skin.ItemName == null || skin.ItemName == string.Empty) continue;
                if (!NameToItemName.ContainsKey(skin.Name.ToLower()))
                    NameToItemName.Add(skin.Name.ToLower(), skin.ItemName.ToLower());
                if (!ItemNameToName.ContainsKey(skin.ItemName.ToLower()))
                    ItemNameToName.Add(skin.ItemName.ToLower(), skin.Name.ToLower());
            }

            excludedSkins = excludedSkinsPre.ConvertAll(obj => Convert.ToUInt64(obj));
            Puts(_("FxvaObk pbzznaq bireivrj: > fxvaobk.pzqf <"));
            if (useManualAddedSkins)
            {
                List<ulong> s = new List<ulong>();
                foreach (KeyValuePair<string, object> m in manualAddedSkinsPre)
                    s.AddRange((m.Value as List<object>).ConvertAll(obj => Convert.ToUInt64(obj)));

                Puts(_("Dhrelvat Fgrnz sbe znahnyyl nqqrq jbexfubc fxvaf"));               
                CllMnlSkinsWb(s, 0);
            }
            else
            {
                GtItmSkns();
            }

            Initialized = true;
        }

        private void FnllyLdd()
        {
            skinsLoaded = true;
            Puts(_("Cyhtva unf svavfurq vzcbegvat nyy fxvaf"));
        }

        private void GtItmSkns()
        {
            if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0)
            {
                PrintWarning("Waiting for Steamworks to update item definitions....");
                Steamworks.SteamInventory.OnDefinitionsUpdated += GtItmSkns;
                return;
            }

            Steamworks.SteamInventory.OnDefinitionsUpdated -= GtItmSkns;

            int countInbuilt = 0;
            foreach (ItemDefinition itemDef in ItemManager.GetItemDefinitions())
            {
                List<ulong> skins = new List<ulong> {0};
                if (useInbuiltSkins)
                    skins.AddRange(ItemSkinDirectory.ForItem(itemDef).Select(skin => Convert.ToUInt64(skin.id)));
                skinsCache.Add(itemDef.shortname, new LinkedList<ulong>(skins));
                if (skins.Count > 1) countInbuilt += skins.Count - 1;
            }

            if (showLoadedSkinCounts && useInbuiltSkins) Puts(_("Ybnqrq {0} vaohvyg fxvaf"), countInbuilt);
            if (useManualAddedSkins)
            {
                int countManual = 0;
                foreach (KeyValuePair<string, List<ulong>> manualskins in manualAddedSkins)
                {
                    string shortname = manualskins.Key;
                    if (!ItemNameToName.ContainsKey(shortname)) continue;
                    string itemname = ItemNameToName[shortname];
                    List<ulong> fileids = manualskins.Value;
                    foreach (ulong fileid in fileids)
                    {
                        if (!skinsCache.ContainsKey(shortname))
                        {
                            skinsCache.Add(shortname, new LinkedList<ulong>());
                            skinsCache[shortname].AddFirst(0);
                        }

                        if (!skinsCache[shortname].Contains(fileid))
                        {
                            skinsCache[shortname].AddAfter(skinsCache[shortname].First, fileid);
                            countManual++;
                        }
                    }
                }

                if (showLoadedSkinCounts && countManual > 0) Puts(_("Ybnqrq {0} znahny nqqrq fxvaf"), countManual);
            }

            if (useApprovedSkins)
            {
                ChckApprvdSkns();
                return;
            }

            FnllyLdd();
        }

        private string BldDtlsSt(List<ulong> list, int pg)
        {
            int st = pg * 100;
            int ed = st + 100 > list.Count ? list.Count : st + 100;

            string details = string.Format(_("?xrl={0}&vgrzpbhag={1}"), steamApiKey, ed - st);

            for (int i = st; i < ed; i++)
                details += string.Format(_("&choyvfurqsvyrvqf[{0}]={1}"), i - st, list[i]);

            return details;
        }

        private void CllMnlSkinsWb(List<ulong> i, int pg)
        {
            int ttlPgs = Mathf.CeilToInt((float)i.Count / 100f) - 1;

            string b = BldDtlsSt(i, pg);

            try
            {
                webrequest.Enqueue(u1, b, (cd, res) => ServerMgr.Instance.StartCoroutine(PrfMnlSkinsWb(cd, res, i, pg, ttlPgs)), this, RequestMethod.POST);
            }
            catch
            {
                GtItmSkns();
            }
        }

        private IEnumerator PrfMnlSkinsWb(int cd, string res, List<ulong> i, int pg, int ttlPgs)
        {
            if (res != null && cd == 200)
            {
                GtPblshdFlDtls pfd = JsonConvert.DeserializeObject<GtPblshdFlDtls>(res);
                if (pfd != null && pfd.response != null && pfd.response.publishedfiledetails?.Count > 0)
                {
                    Puts(string.Format(_("Cebprffvat jbexfubc erfcbafr. Cntr: {0} / {1}"), pg + 1, ttlPgs + 1));
                    foreach (GtPblshdFlDtls.Response.Publishedfiledetail det in pfd.response.publishedfiledetails)
                    {
                        if (det.tags != null && det.tags.Count > 2)
                        {
                            foreach (GtPblshdFlDtls.Tag tag in det.tags)
                            {
                                string t = tag.tag.ToLower();
                                string sn = string.Empty;
                                if (NameToItemName.ContainsKey(t)) sn = NameToItemName[t];
                                else continue;
                                skinWorkshopNames[det.publishedfileid] = det.title;

                                if (!manualAddedSkins.ContainsKey(sn))
                                    manualAddedSkins.Add(sn, new List<ulong>());

                                if (!manualAddedSkins[sn].Contains(det.publishedfileid))
                                    manualAddedSkins[sn].Add(det.publishedfileid);
                            }
                        }
                    }

                    yield return CoroutineEx.waitForEndOfFrame;
                    yield return CoroutineEx.waitForEndOfFrame;

                    if (pg < ttlPgs)
                    {
                        CllMnlSkinsWb(i, pg + 1);
                        yield break;
                    }
                }

            }
            if (pg < ttlPgs)
            {
                CllMnlSkinsWb(i, pg + 1);
                yield break;
            }
            else GtItmSkns();
        }

        private void ChckApprvdSkns()
        {           
            int count = 0;
            foreach (InventoryDef item in Steamworks.SteamInventory.Definitions)
            {
                string shortname = item.GetProperty("itemshortname");
                if (string.IsNullOrEmpty(shortname) || item.Id < 100)
                    continue;

                ulong wsid;
                if (!ulong.TryParse(item.GetProperty("workshopid"), out wsid))
                    continue;
                
                skinWorkshopNames[wsid] = item.Name;
                
                if (!approvedSkinsCount.ContainsKey(shortname))
                    approvedSkinsCount[shortname] = 0;

                if (approvedSkinsLimit > 0 && approvedSkinsCount[shortname] >= approvedSkinsLimit)
                {
                    if (!skinsCacheLimited.ContainsKey(shortname))
                        skinsCacheLimited[shortname] = new LinkedList<ulong>();
                    skinsCacheLimited[shortname].AddLast(wsid);
                }

                if (skinsCacheLimited.ContainsKey(shortname) && skinsCacheLimited[shortname].Contains(wsid))
                    continue;

                if (!skinsCache.ContainsKey(shortname))
                    skinsCache[shortname] = new LinkedList<ulong>();

                if (!skinsCache[shortname].Contains(wsid) && skinsCache[shortname].Count < maxItemsShown)
                {
                    skinsCache[shortname].AddLast(wsid);
                    approvedSkinsCount[shortname]++;
                    count++;
                }
            }

            if (showLoadedSkinCounts && count > 0)
                Puts(_("Vzcbegrq {0} nccebirq fxvaf sbe '{1}' glcrf"), count, skinsCache.Where(c => c.Value.Count > 1).ToList().Count);
            FnllyLdd();
        }
        //void ChckApprvdSknsWb(int cd, string res)
        //{
        //    if (res == null || cd != 200)
        //    {
        //        FnllyLdd();
        //        return;
        //    }
        //    var schm = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(res);
        //    if (schm == null || !(schm is Rust.Workshop.ItemSchema) || schm.items.Length == 0)
        //    {
        //        FnllyLdd();
        //        return;
        //    }
        //    int count = 0;
        //    foreach (var item in schm.items)
        //    {
        //        if (string.IsNullOrEmpty(item.itemshortname) || string.IsNullOrEmpty(item.workshopid) || string.IsNullOrEmpty(item.workshopdownload))
        //            continue;
        //        ulong wsid = Convert.ToUInt64(item.workshopid);
        //        skinWorkshopNames[wsid] = item.name;
        //        string shortname = item.itemshortname;
        //        if (!approvedSkinsCount.ContainsKey(shortname))
        //            approvedSkinsCount[shortname] = 0;
        //        if (approvedSkinsLimit > 0 && approvedSkinsCount[shortname] >= approvedSkinsLimit)
        //        {
        //            if (!skinsCacheLimited.ContainsKey(shortname))
        //                skinsCacheLimited[shortname] = new LinkedList<ulong>();
        //            skinsCacheLimited[shortname].AddLast(wsid);
        //        }
        //        if (skinsCacheLimited.ContainsKey(shortname) && skinsCacheLimited[shortname].Contains(wsid))
        //            continue;
        //        if (!skinsCache.ContainsKey(shortname))
        //            skinsCache[shortname] = new LinkedList<ulong>();
        //        if (!skinsCache[shortname].Contains(wsid) && skinsCache[shortname].Count < maxItemsShown)
        //        {
        //            skinsCache[shortname].AddLast(wsid);
        //            approvedSkinsCount[shortname]++;
        //            count++;
        //        }
        //    }
        //    if (showLoadedSkinCounts && count > 0)
        //        Puts(_("Vzcbegrq {0} nccebirq fxvaf sbe '{1}' glcrf"), count, skinsCache.Where(c => c.Value.Count > 1).ToList().Count);
        //    FnllyLdd();
        //}

        [ConsoleCommand("skinbox.cmds")]
        private void cmdListCmds(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n> SkinBox command overview <");
            TextTable textTable = new TextTable();
            textTable.AddColumn("Command");
            textTable.AddColumn("Description");
            textTable.AddRow(new string[]
                {"skinbox.addskin", "Does add one or multiple skin-id\'s to the manual skin list"});
            textTable.AddRow(new string[]
                {"skinbox.removeskin", "Does remove one or multiple skin-id\'s from the manual skin list"});
            textTable.AddRow(new string[]
                {"skinbox.addexcluded", "Does add one or multiple skin-id\'s to the exclusion list (for players)"});
            textTable.AddRow(new string[]
                {"skinbox.removeexcluded", "Does remove one or multiple skin-id\'s from the exclusion list"});
            textTable.AddRow(new string[]
                {"skinbox.addcollection", "Adds a whole skin-collection to the manual skins list"});
            textTable.AddRow(new string[]
                {"skinbox.removecollection", "Removes a whole collection from the manual skin list"});
            sb.AppendLine(textTable.ToString());
            SendReply(arg, sb.ToString());
        }

        [ConsoleCommand("skinbox.addskin")]
        private void consoleAddSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more Workshop FileId's");
                return;
            }

            List<ulong> fileIds = new List<ulong>();
            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
                        continue;
                    }

                    fileIds.Add(fileId);
                }
            }

            CallSkinsImportWeb(fileIds, arg);
        }

        [ConsoleCommand("skinbox.removeskin")]
        private void consoleRemoveSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more Workshop FileId's");
                return;
            }

            List<ulong> fileIds = new List<ulong>();
            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
                        continue;
                    }

                    fileIds.Add(fileId);
                }
            }

            bool doSave = false;
            int removed = 0;
            foreach (KeyValuePair<string, List<ulong>> addedSkins in manualAddedSkins)
            {
                foreach (ulong fileId in fileIds)
                {
                    if (addedSkins.Value.Contains(fileId))
                    {
                        manualAddedSkins[addedSkins.Key].Remove(fileId);
                        skinsCache[addedSkins.Key].Remove(fileId);
                        removed++;
                        doSave = true;
                    }
                }
            }

            if (doSave)
            {
                string[] keys = manualAddedSkins.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    manualAddedSkins[keys[i]] = manualAddedSkins[keys[i]].Distinct().ToList();
                }

                Config["SkinsAdded", "SkinList"] = manualAddedSkins;
                Config.Save();
                SendReply(arg, $"Removed {removed} FileId's");
            }
        }

        private void CallSkinsImportWeb(List<ulong> i, ConsoleSystem.Arg arg = null)
        {
            string b = string.Format(_("?xrl={0}&vgrzpbhag={1}"), steamApiKey, i.Count);
            int p = 0;
            foreach (ulong f in i)
            {
                b += string.Format(_("&choyvfurqsvyrvqf[{0}]={1}"), p, f);
                p++;
            }

            try
            {
                webrequest.Enqueue(u1, b, (cd, res) => ProofSkinsImportWeb(cd, res, arg), this, RequestMethod.POST);
            }
            catch
            {
            }
        }

        private void ProofSkinsImportWeb(int cd, string res, ConsoleSystem.Arg arg = null)
        {
            if (res == null || cd != 200)
                return;

            GtPblshdFlDtls pfd = JsonConvert.DeserializeObject<GtPblshdFlDtls>(res);
            if (pfd == null || !(pfd is GtPblshdFlDtls) || pfd.response.result == 0 || pfd.response.resultcount == 0)
                return;

            bool doSave = false;
            foreach (GtPblshdFlDtls.Response.Publishedfiledetail det in pfd.response.publishedfiledetails)
            {
                if (det.tags != null && det.tags.Count > 2)
                {
                    string sn = string.Empty;
                    ulong wsid = det.publishedfileid;

                    foreach (GtPblshdFlDtls.Tag tag in det.tags)
                    {
                        string t = tag.tag.ToLower();
                        if (NameToItemName.ContainsKey(t))
                            sn = NameToItemName[t];
                        else continue;

                        if (manualAddedSkins.ContainsKey(sn))
                        {
                            if (manualAddedSkins[sn].Contains(wsid))
                            {
                                SndRplyCl(arg, $"'{det.title} ({wsid})' was already added");
                                continue;
                            }
                        }

                        if (skinsCache.ContainsKey(sn))
                        {
                            if ((skinsCache[sn] as LinkedList<ulong>).Contains(wsid))
                            {
                                SndRplyCl(arg, $"'{det.title} ({wsid})' belongs already to approved/ranked");
                                continue;
                            }
                        }

                        if (!manualAddedSkins.ContainsKey(sn))
                            manualAddedSkins.Add(sn, new List<ulong>());
                        manualAddedSkins[sn].Add(wsid);

                        if (!skinsCache.ContainsKey(sn))
                        {
                            skinsCache.Add(sn, new LinkedList<ulong>());
                            skinsCache[sn].AddLast(0);
                        }

                        skinsCache[sn].AddAfter(skinsCache[sn].First, wsid);
                        skinWorkshopNames[wsid] = det.title;
                        SndRplyCl(arg, $"'{det.title} ({wsid})' added to the list for '{sn}'");
                        doSave = true;
                    }
                }
            }

            if (doSave)
            {
                string[] keys = manualAddedSkins.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)                
                    manualAddedSkins[keys[i]] = manualAddedSkins[keys[i]].Distinct().ToList();
                
                Config["SkinsAdded", "SkinList"] = manualAddedSkins;
                Config.Save();
            }
        }

        [ConsoleCommand("skinbox.addexcluded")]
        private void consoleAddExcluded(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more Workshop FileId's");
                return;
            }

            List<ulong> fileIds = new List<ulong>();
            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
                        continue;
                    }

                    fileIds.Add(fileId);
                }
            }

            int countAdded = 0;
            foreach (ulong fileId in fileIds)
                if (!excludedSkins.Contains(fileId))
                {
                    excludedSkins.Add(fileId);
                    countAdded++;
                }

            if (countAdded > 0)
            {
                Config["SkinsExcluded", "SkinList"] = excludedSkins;
                Config.Save();
                SendReply(arg, $"Added {countAdded} skins to exclusion list");
            }
        }

        [ConsoleCommand("skinbox.removeexcluded")]
        private void consoleRemoveExcluded(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more Workshop FileId's");
                return;
            }

            List<ulong> fileIds = new List<ulong>();
            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as of not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as of not 9/10-Digits");
                        continue;
                    }

                    fileIds.Add(fileId);
                }
            }

            int countRemoved = 0;
            foreach (ulong fileId in fileIds)
                if (excludedSkins.Contains(fileId))
                {
                    excludedSkins.Remove(fileId);
                    countRemoved++;
                }

            if (countRemoved > 0)
            {
                Config["SkinsExcluded", "SkinList"] = excludedSkins;
                Config.Save();
                SndRplyCl(arg, $"Removed {countRemoved} skins from exclusion");
            }
        }

        [ConsoleCommand("skinbox.addcollection")]
        private void consoleAddCollection(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in a valid collection id");
                return;
            }

            ulong collId = 0;
            if (!ulong.TryParse(arg.Args[0], out collId))
            {
                SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' is not a number");
                return;
            }
            else
            {
                if (arg.Args[0].Length < 9 || arg.Args[0].Length > 10)
                {
                    SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' has not 9/10-Digits");
                    return;
                }
            }

            string b = string.Format(_("?xrl={0}&pbyyrpgvbapbhag=1&choyvfurqsvyrvqf[0]={1}"), steamApiKey, arg.Args[0]);
            try
            {
                webrequest.Enqueue(u2, b, (cd, res) => PstCllbckAdd(cd, res, arg), this, RequestMethod.POST);
            }
            catch
            {
                SndRplyCl(arg, "Steam webrequest failed!");
            }
        }

        public void PstCllbckAdd(int cd, string res, ConsoleSystem.Arg arg = null)
        {
            if (res == null || cd != 200)
            {
                SndRplyCl(arg, "Steam webrequest failed by wrong response!");
                return;
            }

            GtCllctnDtls col = JsonConvert.DeserializeObject<GtCllctnDtls>(res);
            if (col == null || !(col is GtCllctnDtls))
            {
                SndRplyCl(arg, "No Collection data received!");
                return;
            }

            if (col.response.resultcount == 0 || col.response.collectiondetails == null ||
                col.response.collectiondetails.Count == 0 || col.response.collectiondetails[0].result != 1)
            {
                SndRplyCl(arg, "The Steam collection could not be found!");
                return;
            }

            List<ulong> fileIds = new List<ulong>();
            foreach (GtCllctnDtls.Response.Collectiondetail.Child child in col.response.collectiondetails[0].children)
                try
                {
                    fileIds.Add(Convert.ToUInt64(child.publishedfileid));
                }
                catch
                {
                }

            if (fileIds.Count == 0)
            {
                SndRplyCl(arg, "No skin numbers found. Workshop search cancelled.");
                return;
            }

            CallSkinsImportWeb(fileIds, arg);
        }

        [ConsoleCommand("skinbox.removecollection")]
        private void consoleRemoveCollection(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in a valid collection id");
                return;
            }

            ulong collId = 0;
            if (!ulong.TryParse(arg.Args[0], out collId))
            {
                SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' is not a number");
                return;
            }
            else
            {
                if (arg.Args[0].Length < 9 || arg.Args[0].Length > 10)
                {
                    SendReply(arg, $"Collection ID not correct: '{arg.Args[0]}' has not 9/10-Digits");
                    return;
                }
            }

            string b = string.Format(_("?xrl={0}&pbyyrpgvbapbhag=1&choyvfurqsvyrvqf[0]={1}"), steamApiKey, arg.Args[0]);
            try
            {
                webrequest.Enqueue(u2, b, (cd, res) => PostCallbackRemove(cd, res, arg), this, RequestMethod.POST);
            }
            catch
            {
                SndRplyCl(arg, "Steam webrequest failed!");
            }
        }

        public void PostCallbackRemove(int cd, string res, ConsoleSystem.Arg arg = null)
        {
            if (res == null || cd != 200)
            {
                SndRplyCl(arg, "Steam webrequest failed by wrong response!");
                return;
            }

            GtCllctnDtls col = JsonConvert.DeserializeObject<GtCllctnDtls>(res);
            if (col == null || !(col is GtCllctnDtls))
            {
                SndRplyCl(arg, "No Collection data received!");
                return;
            }

            if (col.response.resultcount == 0 || col.response.collectiondetails == null ||
                col.response.collectiondetails.Count == 0 || col.response.collectiondetails[0].result != 1)
            {
                SndRplyCl(arg, "The Steam collection could not be found!");
                return;
            }

            List<ulong> fileIds = new List<ulong>();
            foreach (GtCllctnDtls.Response.Collectiondetail.Child child in col.response.collectiondetails[0].children)
                try
                {
                    fileIds.Add(Convert.ToUInt64(child.publishedfileid));
                }
                catch
                {
                }

            if (fileIds.Count == 0)
            {
                SndRplyCl(arg, "No skin numbers found. Workshop search cancelled.");
                return;
            }

            int removed = 0;
            foreach (KeyValuePair<string, List<ulong>> addSkins in new Dictionary<string, List<ulong>>(manualAddedSkins)
            )
            foreach (ulong skin in addSkins.Value.ToList())
                if (fileIds.Contains(skin))
                {
                    manualAddedSkins[addSkins.Key].Remove(skin);
                    if (skinsCache.ContainsKey(addSkins.Key)) skinsCache[addSkins.Key].Remove(skin);
                    removed++;
                }

            if (removed > 0)
            {
                SndRplyCl(arg, $"Removed '{removed}' manual skins by collection remove.");

                string[] keys = manualAddedSkins.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                    manualAddedSkins[keys[i]] = manualAddedSkins[keys[i]].Distinct().ToList();

                Config["SkinsAdded", "SkinList"] = manualAddedSkins;
                Config.Save();
            }
            else
            {
                SndRplyCl(arg, $"No manual skins to remove by collection remove.");
            }
        }

        [ConsoleCommand("skinbox.open")]
        private void consoleSkinboxOpen(ConsoleSystem.Arg arg)
        {
            if (arg == null) return;
            if (arg.Connection == null)
            {
                if (arg.Args == null || arg.Args.Length == 0)
                {
                    SendReply(arg, $"'skinbox.open' cmd needs a passed steamid ");
                    return;
                }

                ulong argId = 0uL;
                if (!ulong.TryParse(arg.Args[0], out argId))
                {
                    SendReply(arg, $"'skinbox.open' cmd for '{arg.Args[0]}' failed (no valid number)");
                    return;
                }

                BasePlayer argPlayer = BasePlayer.FindByID(argId);
                if (argPlayer == null)
                {
                    SendReply(arg, $"'skinbox.open' cmd for userID '{argId}' failed (player not found)");
                    return;
                }

                if (!argPlayer.inventory.loot.IsLooting()) OpnSknBx(argPlayer);
            }
            else if (arg.Connection != null && arg.Connection.player != null)
            {
                BasePlayer player = arg.Player();
                if (player.inventory.loot.IsLooting()) return;
                if (!(player.IsAdmin || player.net.connection.authLevel >= accessOverrideAuthLevel) &&
                    !permission.UserHasPermission(player.UserIDString, permissionUse))
                {
                    player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }

                if (!ChckOpnBlnc(player)) return;
                if (enableCooldown && !(cooldownOverrideAdmin &&
                                        (player.IsAdmin ||
                                         player.net.connection.authLevel >= cooldownOverrideAuthLevel)))
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime time;
                    string key = player.UserIDString + "-box";
                    if (cooldownTimes.TryGetValue(key, out time))
                        if (time > now.AddSeconds(-cooldownBox))
                        {
                            player.ChatMessage(string.Format(lang.GetMessage("CooldownTime", this, player.UserIDString),
                                (time - now.AddSeconds(-cooldownBox)).Seconds));
                            return;
                        }
                }

                OpnSknBx(player);
            }
        }

        private void cmdSknBx(BasePlayer player, string command, string[] args)
        {
            if (player.inventory.loot.IsLooting()) return;
            if (!(player.IsAdmin || player.net.connection.authLevel >= accessOverrideAuthLevel) &&
                !permission.UserHasPermission(player.UserIDString, permissionUse))
            {
                player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }

            if (!ChckOpnBlnc(player)) return;
            if (enableCooldown && !(cooldownOverrideAdmin &&
                                    (player.IsAdmin || player.net.connection.authLevel >= cooldownOverrideAuthLevel)))
            {
                DateTime now = DateTime.UtcNow;
                DateTime time;
                string key = player.UserIDString + "-box";
                if (cooldownTimes.TryGetValue(key, out time))
                    if (time > now.AddSeconds(-cooldownBox))
                    {
                        player.ChatMessage(string.Format(lang.GetMessage("CooldownTime", this, player.UserIDString),
                            (time - now.AddSeconds(-cooldownBox)).Seconds));
                        return;
                    }
            }

            timer.Once(0.2f, () => { OpnSknBx(player); });
        }

        public bool ChckOpnBlnc(BasePlayer player)
        {
            if (!activePointSystem || costBoxOpen <= 0 || player.IsAdmin && costExcludeAdmins ||
                costExcludePermEnabled && permission.UserHasPermission(player.UserIDString, costExcludePerm))
                return true;
            object getMoney = null;
            if (activeServerRewards) getMoney = (int) (Interface.Oxide.CallHook("CheckPoints", player.userID) ?? 0);
            if (activeEconomics) getMoney = (double) (Interface.Oxide.CallHook("Balance", player.UserIDString) ?? 0.0);
            int playerMoney = 0;
            playerMoney = Convert.ToInt32(getMoney);
            if (playerMoney < costBoxOpen)
            {
                player.ChatMessage(string.Format(lang.GetMessage("NotEnoughBalanceOpen", this, player.UserIDString),
                    costBoxOpen));
                return false;
            }

            if (activeServerRewards) Interface.Oxide.CallHook("TakePoints", player.userID, costBoxOpen);
            if (activeEconomics) Interface.Oxide.CallHook("Withdraw", player.userID, Convert.ToDouble(costBoxOpen));
            return true;
        }

        public bool ChckSknBlnc(BasePlayer player, Item item)
        {
            if (!activePointSystem || player.IsAdmin && costExcludeAdmins || costExcludePermEnabled &&
                permission.UserHasPermission(player.UserIDString, costExcludePerm)) return true;
            object getMoney = null;
            if (activeServerRewards) getMoney = (int) (Interface.Oxide.CallHook("CheckPoints", player.userID) ?? 0);
            if (activeEconomics) getMoney = (double) (Interface.Oxide.CallHook("Balance", player.UserIDString) ?? 0.0);
            int playerMoney = 0;
            playerMoney = Convert.ToInt32(getMoney);
            bool hasBalance = false;
            int getCost = 0;
            switch (item.info.category.ToString())
            {
                case "Weapon":
                case "Tool":
                    if (costWeapon <= 0 || playerMoney > costWeapon) hasBalance = true;
                    getCost = costWeapon;
                    break;
                case "Attire":
                    if (costPlayerwearable <= 0 || playerMoney > costPlayerwearable) hasBalance = true;
                    getCost = costPlayerwearable;
                    break;
                case "Items":
                case "Construction":
                    if (costDeployable <= 0 || playerMoney > costDeployable) hasBalance = true;
                    getCost = costDeployable;
                    break;
                default:
                    hasBalance = true;
                    break;
            }

            if (!hasBalance)
            {
                player.ChatMessage(string.Format(lang.GetMessage("NotEnoughBalanceUse", this, player.UserIDString),
                    getCost, item.info.displayName.translated));
                return false;
            }

            return true;
        }

        public bool WthdrwBlnc(BasePlayer player, Item item)
        {
            if (!activePointSystem || player.IsAdmin && costExcludeAdmins || costExcludePermEnabled &&
                permission.UserHasPermission(player.UserIDString, costExcludePerm)) return true;
            int getCost = 0;
            switch (item.info.category.ToString())
            {
                case "Weapon":
                case "Tool":
                    getCost = costWeapon;
                    break;
                case "Attire":
                    getCost = costPlayerwearable;
                    break;
                case "Items":
                case "Construction":
                    getCost = costDeployable;
                    break;
                default: break;
            }

            bool hadMoney = false;
            if (activeServerRewards && (bool) Interface.Oxide.CallHook("TakePoints", player.userID, getCost))
                hadMoney = true;
            if (activeEconomics && (bool) Interface.Oxide.CallHook("Withdraw", player.userID, Convert.ToDouble(getCost))
            ) hadMoney = true;
            if (!hadMoney)
            {
                player.ChatMessage(string.Format(lang.GetMessage("NotEnoughBalanceTake", this, player.UserIDString),
                    item.info.displayName.translated));
                return false;
            }

            return true;
        }

        internal class SBH : FacepunchBehaviour
        {
            public bool isCreating;
            public bool isBlocked;
            public bool isEmptied;
            public int itemId;
            public int itemAmount;
            public Item currentItem;
            public BasePlayer looter;
            public ItemContainer loot;
            public BaseEntity entityOwner;
            public ulong skinId;
            public int currentPage;
            public int totalPages;
            public int lastPageCount;
            public LinkedList<ulong> itemSkins;
            public int skinsTotal;
            public int perPageTotal;
            public int maxPages;
            public bool refundItem;

            private void Awake()
            {
                isCreating = false;
                isBlocked = false;
                isEmptied = false;
                skinId = 0uL;
                currentPage = 1;
                totalPages = 1;
                lastPageCount = 0;
                skinsTotal = 1;
                perPageTotal = 1;
                itemAmount = 1;
                maxPages = skinBox.maxPagesShown;
                itemSkins = new LinkedList<ulong>();
            }

            public void ShwUi()
            {
                if (totalPages > 1 && maxPages > 1)
                {
                    int p = Math.Min(maxPages, totalPages);
                    skinBox.CrtUi(looter, currentPage, p);
                }
            }

            public void ClsUi()
            {
                skinBox.DstryUi(looter);
            }

            public void PgNxt()
            {
                if (totalPages > 1 && currentPage < maxPages && currentPage < totalPages && !isCreating)
                {
                    currentPage++;
                    FllSknBx(currentPage);
                    ShwUi();
                }
            }

            public void PgPrv()
            {
                if (totalPages > 1 && currentPage > 1 && !isCreating)
                {
                    currentPage--;
                    FllSknBx(currentPage);
                    ShwUi();
                }
            }

            public void StrtNwItm(ItemContainer container, Item item)
            {
                isBlocked = true;
                currentItem = item;
                itemAmount = item.amount;
                itemId = item.info.itemid;
                skinId = item.skin;
                string shortname = currentItem.info.shortname == _("evsyr.ye300")
                    ? _("ye300.vgrz")
                    : currentItem.info.shortname;
                itemSkins = new LinkedList<ulong>(skinBox.skinsCache[shortname] as LinkedList<ulong>);
                itemSkins.Remove(0uL);
                itemSkins.Remove(skinId);
                skinsTotal = itemSkins.Count;
                perPageTotal = skinId == 0uL ? 41 : 40;
                currentPage = 1;
                totalPages = Mathf.CeilToInt(skinsTotal / (float) perPageTotal);
                lastPageCount = totalPages == 1 ? skinsTotal : skinsTotal % perPageTotal;
                loot = container;
                entityOwner = loot.entityOwner;
            }

            public void FllSknBx(int page = 1)
            {
                isCreating = true;
                string origname = currentItem.info.shortname;
                bool hasCondition = currentItem.hasCondition;
                float condition = currentItem.condition;
                float maxCondition = currentItem.maxCondition;
                bool isWeapon = currentItem.GetHeldEntity() is BaseProjectile;
                bool hasMods = false;
                int contents = 0;
                int capacity = 0;
                ItemDefinition ammoType = null;
                Dictionary<int, float> itemMods = new Dictionary<int, float>();
                if (isWeapon)
                {
                    contents = (currentItem.GetHeldEntity() as BaseProjectile).primaryMagazine.contents;
                    capacity = (currentItem.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                    ammoType = (currentItem.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType;
                    if (currentItem.contents != null && currentItem.contents.itemList.Count > 0)
                    {
                        hasMods = true;
                        foreach (Item mod in currentItem.contents.itemList)
                            itemMods.Add(mod.info.itemid, mod.condition);
                    }
                }

                isEmptied = false;
                if (currentItem.contents != null && currentItem.contents.itemList.Count > 0)
                {
                    Item[] array = currentItem.contents.itemList.ToArray();
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i].info.category == ItemCategory.Weapon) continue;
                        Item item = array[i];
                        looter.inventory.GiveItem(item, null);
                    }
                }

                skinBox.RmvItm(loot, currentItem);
                skinBox.ClrCntnr(loot);
                int startIndex = page * perPageTotal - perPageTotal;
                int rangeIndex = page == totalPages ? lastPageCount : perPageTotal;
                List<ulong> skins = new List<ulong> {0uL};
                if (skinId != 0uL) skins.Add(skinId);
                if (maxPages > 1 && totalPages > 1) skins.AddRange(itemSkins.ToList().GetRange(startIndex, rangeIndex));
                else skins.AddRange(itemSkins.ToList());
                loot.capacity = skins.Count();
                ItemDefinition itemDef = ItemManager.FindItemDefinition(origname);
                if (skins.Count() > 42) loot.capacity = 42;
                else loot.capacity = skins.Count;
                foreach (ulong skin in skins)
                {
                    if (loot.IsFull()) break;
                    if (skinBox.excludedSkins.Contains(skin) && looter.net.connection.authLevel < skinBox.exludedSkinsAuthLevel)
                        continue;

                    Item newItem = ItemManager.Create(itemDef, 1, skin);
                    if (skinBox.skinWorkshopNames.ContainsKey(skin)) newItem.name = skinBox.skinWorkshopNames[skin];
                    if (hasCondition)
                    {
                        newItem.condition = condition;
                        newItem.maxCondition = maxCondition;
                    }

                    if (isWeapon)
                    {
                        BaseProjectile gun = newItem.GetHeldEntity() as BaseProjectile;
                        gun.primaryMagazine.contents = contents;
                        gun.primaryMagazine.capacity = capacity;
                        gun.primaryMagazine.ammoType = ammoType;
                        if (hasMods)
                        {
                            foreach (KeyValuePair<int, float> mod in itemMods)
                            {
                                Item newMod = ItemManager.CreateByItemID((int) mod.Key, 1);
                                newMod.condition = Convert.ToSingle(mod.Value);
                                newMod.MoveToContainer(newItem.contents, -1, false);
                            }

                            newItem.contents.SetFlag(ItemContainer.Flag.IsLocked, true);
                            newItem.contents.SetFlag(ItemContainer.Flag.NoItemInput, true);
                            newItem.contents.MarkDirty();
                        }
                    }

                    newItem.MarkDirty();
                    skinBox.InsrtItm(loot, newItem);
                }

                isCreating = false;
                loot.MarkDirty();
            }

            public void PlayerStoppedLooting(BasePlayer player)
            {
                if (!isEmptied && currentItem != null)
                {
                    isEmptied = true;
                    if (refundItem) player.GiveItem(currentItem);
                }

                if (!GetComponent<BaseEntity>().IsDestroyed)
                    GetComponent<BaseEntity>().Kill(BaseNetworkable.DestroyMode.None);
                if (skinBox.enableCooldown) skinBox.cooldownTimes[player.UserIDString + "-box"] = DateTime.UtcNow;
            }

            private void OnDestroy()
            {
                skinBox.DstryUi(looter);
                skinBox.activeSkinBoxes.Remove(looter.userID);
                if (!isEmptied && currentItem != null)
                {
                    isEmptied = true;
                    looter.GiveItem(currentItem);
                }

                looter.EndLooting();
            }
        }

        private void OpnSknBx(BasePlayer player, bool refundItem = true)
        {
            BaseEntity skinBox = GameManager.server.CreateEntity(StringPool.Get(4080262419),
                player.transform.position - new Vector3(0, 250f + UnityEngine.Random.Range(-25f, 25f), 0));
            (skinBox as BaseNetworkable).limitNetworking = true;
            UnityEngine.Object.Destroy(skinBox.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(skinBox.GetComponent<GroundWatch>());
            skinBox.Spawn();
            SBH lootHandler = skinBox.gameObject.AddComponent<SBH>();
            lootHandler.looter = player;
            lootHandler.refundItem = refundItem;
            StorageContainer container = skinBox.GetComponent<StorageContainer>();
            if (!allowStackedItems)
            {
                container.maxStackSize = 1;
                container.inventory.maxStackSize = 1;
            }

            container.inventory.capacity = 1;
            container.SetFlag(BaseEntity.Flags.Open, true, false);
            if (QuickSort && hideQuickSort) StrtLtngEntty(player.inventory.loot, container);
            else player.inventory.loot.StartLootingEntity(container, false);
            player.inventory.loot.AddContainer(container.inventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, _("ECP_BcraYbbgCnary"), _("trarevpynetr"));
            container.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            activeSkinBoxes.Add(player.userID, lootHandler);
        }

        public void StrtLtngEntty(PlayerLoot loot, BaseEntity targetEntity)
        {
            loot.Clear();
            if (!targetEntity) return;
            loot.PositionChecks = false;
            loot.entitySource = targetEntity;
            loot.itemSource = null;
            loot.MarkDirty();
        }

        public void ClrCntnr(ItemContainer container)
        {
            while (container.itemList.Count > 0)
            {
                Item removeItem = container.itemList[0];
                RmvItm(container, removeItem);
                removeItem.Remove(0f);
            }
        }

        private object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container.entityOwner == null) return null;
            SBH lootHandler;
            if ((lootHandler = container.entityOwner.GetComponent<SBH>()) == null) return null;
            string shortname = item.info.shortname == _("evsyr.ye300") ? _("ye300.vgrz") : item.info.shortname;
            if (lootHandler.isCreating || lootHandler.isBlocked ||
                enableCustomPerms && !ChckItmPrms(lootHandler.looter, item) || item.isBroken ||
                !skinsCache.ContainsKey(shortname) || (skinsCache[shortname] as LinkedList<ulong>).Count <= 1 ||
                !ChckSknBlnc(lootHandler.looter, item)) return ItemContainer.CanAcceptResult.CannotAccept;
            return null;
        }

        public bool ChckItmPrms(BasePlayer player, Item item)
        {
            string category = item.info.category.ToString();
            switch (category)
            {
                case "Weapon":
                    if (permission.UserHasPermission(player.UserIDString, permCustomWeapon)) return true;
                    break;
                case "Tool":
                    if (permission.UserHasPermission(player.UserIDString, permCustomWeapon)) return true;
                    break;
                case "Attire":
                    if (permission.UserHasPermission(player.UserIDString, permCustomPlayerwearable)) return true;
                    break;
                case "Items":
                    if (permission.UserHasPermission(player.UserIDString, permCustomDeployable)) return true;
                    break;
                case "Construction":
                    if (permission.UserHasPermission(player.UserIDString, permCustomDeployable)) return true;
                    break;
                default: return true;
            }

            return false;
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null || container.entityOwner == null) return;
            SBH lootHandler = container.entityOwner.GetComponent<SBH>();
            if (lootHandler == null || lootHandler.isBlocked) return;
            lootHandler.StrtNwItm(container, item);
            lootHandler.FllSknBx();
            if (maxPagesShown > 1) lootHandler.ShwUi();
        }

        public bool InsrtItm(ItemContainer container, Item item, bool mark = false)
        {
            if (container.itemList.Contains(item)) return false;
            if (container.IsFull()) return false;
            container.itemList.Add(item);
            item.parent = container;
            if (!container.FindPosition(item)) return false;
            if (mark) container.MarkDirty();
            if (container.onItemAddedRemoved != null) container.onItemAddedRemoved(item, true);
            return true;
        }

        public bool RmvItm(ItemContainer container, Item item, bool mark = false)
        {
            if (!container.itemList.Contains(item)) return false;
            if (container.onPreItemRemove != null) container.onPreItemRemove(item);
            container.itemList.Remove(item);
            item.parent = null;
            if (mark) container.MarkDirty();
            if (container.onItemAddedRemoved != null) container.onItemAddedRemoved(item, false);
            return true;
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null || container.entityOwner == null) return;
            SBH lootHandler = container.entityOwner.GetComponent<SBH>();
            if (lootHandler == null || !lootHandler.isBlocked) return;
            if (item.GetHeldEntity() is BaseProjectile && item.contents != null)
            {
                item.contents.SetFlag(ItemContainer.Flag.IsLocked, false);
                item.contents.SetFlag(ItemContainer.Flag.NoItemInput, false);
            }

            if (lootHandler.itemAmount > 1)
            {
                item.amount = lootHandler.itemAmount;
                item.MarkDirty();
                lootHandler.itemAmount = 1;
            }

            ClrCntnr(container);
            if (lootHandler.currentItem != null)
            {
                lootHandler.currentItem.Remove(0f);
                lootHandler.currentItem = null;
            }

            container.MarkDirty();
            lootHandler.isEmptied = true;
            lootHandler.ClsUi();
            container.capacity = 1;
            if (item.skin == 0uL)
            {
                lootHandler.skinId = 0uL;
                lootHandler.isBlocked = false;
                return;
            }

            if (!WthdrwBlnc(lootHandler.looter, item))
            {
                item.skin = lootHandler.skinId;
                if (item.GetHeldEntity()) item.GetHeldEntity().skinID = lootHandler.skinId;
                item.MarkDirty();
            }

            if (enableCooldown && activateAfterSkinTaken &&
                !(cooldownOverrideAdmin && (lootHandler.looter.IsAdmin ||
                                            lootHandler.looter.net.connection.authLevel >= cooldownOverrideAuthLevel)
                    ) && item.skin != lootHandler.skinId)
            {
                lootHandler.looter.EndLooting();
                skinBox.cooldownTimes[lootHandler.looter.UserIDString + "-box"] = DateTime.UtcNow;
            }

            lootHandler.skinId = 0uL;
            lootHandler.isBlocked = false;
        }

        public void UnsbscrbSplt()
        {
            Unsubscribe(nameof(OnItemSplit));
            Unsubscribe(nameof(CanStackItem));
        }

        public void SbscrbSplt()
        {
            Subscribe(nameof(OnItemSplit));
            Subscribe(nameof(CanStackItem));
        }

        public void SndRplyCl(ConsoleSystem.Arg arg, string format)
        {
            if (arg != null && arg.Connection != null) SendReply(arg, format);
            Puts(format);
        }

        private static string _(string i)
        {
            return !string.IsNullOrEmpty(i)
                ? new string(i.Select(x =>
                    x >= 'a' && x <= 'z' ? (char) ((x - 'a' + 13) % 26 + 'a') :
                    x >= 'A' && x <= 'Z' ? (char) ((x - 'A' + 13) % 26 + 'A') : x).ToArray())
                : i;
        }

        public void DstryUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SkinBoxUI");
        }

        public void CrtUi(BasePlayer player, int page = 1, int total = 1)
        {
            string panelName = "SkinBoxUI";
            CuiHelper.DestroyUi(player, panelName);
            string contentColor = "0.7 0.7 0.7 1.0";
            string buttonColor = "0.75 0.75 0.75 0.1";
            string buttonTextColor = "0.77 0.68 0.68 1";
            CuiElementContainer result = new CuiElementContainer();
            string rootPanelName =
                result.Add(
                    new CuiPanel
                    {
                        Image = new CuiImageComponent {Color = "0 0 0 0"},
                        RectTransform = {AnchorMin = "0.9505 0.15", AnchorMax = "0.99 0.6"}
                    }, "Hud.Menu", panelName);
            result.Add(
                new CuiPanel
                {
                    Image = new CuiImageComponent {Color = "0.65 0.65 0.65 0.06"},
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}
                }, rootPanelName);
            result.Add(
                new CuiButton
                {
                    RectTransform = {AnchorMin = "0.025 0.7", AnchorMax = "0.975 1.0"},
                    Button = {Command = _("fxvaobk.cntrceri"), Color = buttonColor},
                    Text = {Align = TextAnchor.MiddleCenter, Text = "◀", Color = buttonTextColor, FontSize = 50}
                }, rootPanelName);
            result.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0.025 0.3", AnchorMax = "0.975 0.7"},
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter, Text = $"{page}\nof\n{total}", Color = contentColor,
                        FontSize = 20
                    }
                }, rootPanelName);
            result.Add(
                new CuiButton
                {
                    RectTransform = {AnchorMin = "0.025 0", AnchorMax = "0.975 0.3"},
                    Button = {Command = _("fxvaobk.cntrarkg"), Color = buttonColor},
                    Text = {Align = TextAnchor.MiddleCenter, Text = "▶", Color = buttonTextColor, FontSize = 50}
                }, rootPanelName);
            CuiHelper.AddUi(player, result);
        }

        [ConsoleCommand("skinbox.pagenext")]
        private void cmdPageNext(ConsoleSystem.Arg arg)
        {
            if (maxPagesShown <= 1 || arg == null || arg.Connection == null) return;
            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player == null) return;
            if (activeSkinBoxes.ContainsKey(player.userID)) activeSkinBoxes[player.userID].PgNxt();
        }

        [ConsoleCommand("skinbox.pageprev")]
        private void cmdPagePrev(ConsoleSystem.Arg arg)
        {
            if (maxPagesShown <= 1 || arg == null || arg.Connection == null) return;
            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player == null) return;
            if (activeSkinBoxes.ContainsKey(player.userID)) activeSkinBoxes[player.userID].PgPrv();
        }

        private bool IsSkinBoxPlayer(ulong playerId) => activeSkinBoxes.ContainsKey(playerId);

        private void OnPluginUnloaded(Plugin name)
        {
            if (Initialized && name.Name == _("FgnpxfRkgraqrq")) ChckSbscrptns(true);
        }

        private void OnPluginLoaded(Plugin name)
        {
            if (Initialized && name.Name == _("FgnpxfRkgraqrq")) ChckSbscrptns(false);
        }

        public void ChckSbscrptns(bool wasUnload)
        {
            if (!allowStackedItems) return;
            if (wasUnload)
            {
                _stacksExtendedExtrasDisabled = false;
                Subscribe(nameof(CanStackItem));
                Subscribe(nameof(OnItemSplit));
            }
            else
            {
                _stacksExtendedExtrasDisabled = (bool) StacksExtended.CallHook("DisableExtraFeatures");
                if (!_stacksExtendedExtrasDisabled)
                {
                    Unsubscribe(nameof(CanStackItem));
                    Unsubscribe(nameof(OnItemSplit));
                }
                else
                {
                    Subscribe(nameof(CanStackItem));
                    Subscribe(nameof(OnItemSplit));
                }
            }
        }

        private object OnItemSplit(Item thisI, int split_Amount)
        {
            if (thisI.skin == 0uL) return null;
            Item item = null;
            item = ItemManager.CreateByItemID(thisI.info.itemid, 1, thisI.skin);
            if (item != null)
            {
                thisI.amount -= split_Amount;
                thisI.MarkDirty();
                item.amount = split_Amount;
                item.OnVirginSpawn();
                if (thisI.IsBlueprint()) item.blueprintTarget = thisI.blueprintTarget;
                if (thisI.hasCondition) item.condition = thisI.condition;
                item.MarkDirty();
                return item;
            }

            return null;
        }

        private object CanStackItem(Item thisI, Item item)
        {
            if (thisI.skin == item.skin) return null;
            if (thisI.skin != item.skin) return false;
            if (thisI.skin == item.skin)
            {
                if (thisI.hasCondition && item.hasCondition)
                    if (item.condition != thisI.condition)
                        return false;
                return true;
            }

            return null;
        }

        private string u1 = _("uggcf://ncv.fgrnzcbjrerq.pbz/VFgrnzErzbgrFgbentr/TrgChoyvfurqSvyrQrgnvyf/i1/");
        private string u2 = _("uggcf://ncv.fgrnzcbjrerq.pbz/VFgrnzErzbgrFgbentr/TrgPbyyrpgvbaQrgnvyf/i1/");
        //private string u3 = _("uggc://f3.nznmbanjf.pbz/f3.cynlehfg.pbz/vpbaf/vairagbel/ehfg/fpurzn.wfba");

        public class GtPblshdFlDtls
        {
            [JsonProperty("response")] public Response response;

            public class Tag
            {
                [JsonProperty("tag")] public string tag;
            }

            public class Response
            {
                [JsonProperty("result")] public int result;
                [JsonProperty("resultcount")] public int resultcount;
                [JsonProperty("publishedfiledetails")] public List<Publishedfiledetail> publishedfiledetails;

                public class Publishedfiledetail
                {
                    [JsonProperty("publishedfileid")] public ulong publishedfileid;
                    [JsonProperty("result")] public int result;
                    [JsonProperty("creator")] public string creator;
                    [JsonProperty("creator_app_id")] public int creator_app_id;
                    [JsonProperty("consumer_app_id")] public int consumer_app_id;
                    [JsonProperty("filename")] public string filename;
                    [JsonProperty("file_size")] public int file_size;
                    [JsonProperty("preview_url")] public string preview_url;
                    [JsonProperty("hcontent_preview")] public string hcontent_preview;
                    [JsonProperty("title")] public string title;
                    [JsonProperty("description")] public string description;
                    [JsonProperty("time_created")] public int time_created;
                    [JsonProperty("time_updated")] public int time_updated;
                    [JsonProperty("visibility")] public int visibility;
                    [JsonProperty("banned")] public int banned;
                    [JsonProperty("ban_reason")] public string ban_reason;
                    [JsonProperty("subscriptions")] public int subscriptions;
                    [JsonProperty("favorited")] public int favorited;

                    [JsonProperty("lifetime_subscriptions")]
                    public int lifetime_subscriptions;

                    [JsonProperty("lifetime_favorited")] public int lifetime_favorited;
                    [JsonProperty("views")] public int views;
                    [JsonProperty("tags")] public List<Tag> tags;
                }
            }
        }

        public class GtCllctnDtls
        {
            [JsonProperty("response")] public Response response;

            public class Response
            {
                [JsonProperty("result")] public int result;
                [JsonProperty("resultcount")] public int resultcount;
                [JsonProperty("collectiondetails")] public List<Collectiondetail> collectiondetails;

                public class Collectiondetail
                {
                    [JsonProperty("publishedfileid")] public string publishedfileid;
                    [JsonProperty("result")] public int result;
                    [JsonProperty("children")] public List<Child> children;

                    public class Child
                    {
                        [JsonProperty("publishedfileid")] public string publishedfileid;
                        [JsonProperty("sortorder")] public int sortorder;
                        [JsonProperty("filetype")] public int filetype;
                    }
                }
            }
        }
    }
}