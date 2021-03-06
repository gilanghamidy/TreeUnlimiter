using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using TreeUnlimiter.OptionsFramework;
using UnityEngine;

namespace TreeUnlimiter
{
    public class LoadingExtension : LoadingExtensionBase
    {
        internal static bool LastSaveUsedPacking = false;
        internal static bool LastFileClearedFlag = false;
        internal static List<int> LastSaveList;
        public static bool InGame { get; set; } = false;

        public override void OnCreated(ILoading loading)
        {
            InGame = true;
            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog("OnCreated fired.  " + DateTime.Now.ToString(Mod.DTMilli)); }

            //It's useless to try and detect loadingmode here as the simmanager loading obj is empty on first start
            //and there after only contains the previous state at this stage, ie the prior map or assset or game.
            //it's fresh state gets updated sometime after OnCreated. Below both will fail with null obj.
            //SimulationManager.UpdateMode mUpdateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
            //Debug.Log("[TreeUnlimiter:OnCreated] " + loading.currentMode.ToString());
            // Damn shame, as I have to crap in levelloaded() which is after deserialization.
            try
            {
                if (Mod.IsEnabled == true & Mod.IsSetupActive == false)
                {
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog("Enabled and redirects not setup yet"); }
                    //if (mUpdateMode != SimulationManager.UpdateMode.LoadAsset || mUpdateMode != SimulationManager.UpdateMode.NewAsset)
                    //{
                    //    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Debug.Log("[TreeUnlimiter:OnCreated]  AssetModeNotDetcted"); }

                    //1.6.0 - This does not work anymore because onCreated() is firing
                    // way to late in the load process now, so while I'm keeping it here.
                    // it's really now as a back up.
                    Mod.Setup();  //by default we always run setup again.

                    
                    //}
                }
                //9-25-2015 - *no longer needed
                /*
                            if (Mod.IsEnabled == true & Mod.IsSetupActive == true && OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                            {
                                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Debug.Log("[TreeUnlimiter:Loader::OnCreated] enabled and redirect setup already"); }
                                //if (mUpdateMode == SimulationManager.UpdateMode.LoadAsset || mUpdateMode == SimulationManager.UpdateMode.NewAsset)
                                //{
                                //    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Debug.Log("[TreeUnlimiter:OnCreated]  AssetModeDetcted"); }
                                //    Mod.ReveseSetup();
                                //}

                            }
                */
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Onlevelload Exception:", ex, true);
            }
            
            base.OnCreated(loading);

        }


        public override void OnLevelLoaded(LoadMode mode)
        {
            LastSaveUsedPacking = false;
            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog("Map LoadMode:" + mode.ToString() + "  " + DateTime.Now.ToString(Mod.DTMilli)); }
            try
            {
                if (Mod.IsEnabled == true & Mod.IsSetupActive == false)
                {
                    //should rarely, if ever, reach here as should be taken care of in onCreated().
                    // if we ran tried to run setup here we could but our Array was not expanded
                    // during the load deserialize process that came before us. Hence an attempt to save will produce
                    // a problem during custom serialze save as it'll exception error cause the buffer wasn't expanded.
                    // Will maybe enhance custom_serialzer to check for bigger buffer first, 
                    // though let's avoid that problem entirely here.

                    if (mode != LoadMode.LoadAsset & mode != LoadMode.NewAsset)  //fire only on non Assett modes, we don't want it to get setup on assett mode anyway.
                    {
                        if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog(" AssetModeNotDetcted"); }
                        string strmsg = "[TreeUnlimiter:OnLevelLoaded]  *** Enabled but not setup yet, why did this happen??\n" +
                            "Did OnCreated() not fire?? did redirections exception error?\n If you see this please contact author or make sure other mods did not cause a critical errors prior to this one during the load process." +
                            "\n We are now going to disable this mod from running during this map load attempt.";
                        Logger.dbgLog(strmsg);
                        DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, strmsg);
                        //1.2.0f3_Build007 above noted Bug - Mod.Setup();  
                    }
                }

                if (Mod.IsEnabled == true & Mod.IsSetupActive == true)
                {
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog("Enabled and setup already.(expected)"); }
                    
                    if (mode == LoadMode.LoadAsset || mode == LoadMode.NewAsset)
                    {
                        //if we are asseteditor then revert the redirects, and reset the treemanager data.
                        if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog("AssetModeDetcted, removing redirects and resetting treemanager"); }

                        //1.6.0 commented out these 2 lines
                        //ResetTreeMananger(Mod.DEFAULT_TREE_COUNT, Mod.DEFAULT_TREEUPDATE_COUNT, true);
                        //Mod.ReveseSetup();

                        if (mode == LoadMode.NewAsset & (Singleton<TreeManager>.instance.m_treeCount < 0))
                        {
                            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled()) { Logger.dbgLog("AssetModeDetcted, Treecount is less < then 0 !"); };
                            //{ Singleton<TreeManager>.instance.m_treeCount = 0; }
                        }
                    }

                    if (mode == LoadMode.NewMap || mode == LoadMode.LoadMap || mode == LoadMode.NewGame || mode == LoadMode.LoadMap || mode == LoadMode.NewGameFromScenario || mode == LoadMode.NewScenarioFromGame || mode == LoadMode.NewScenarioFromMap)
                    {
                        //total hack to address wierd behavior of -1 m_treecount and 0 itemcount
                        // this hack attempts to jimmy things up the way things appear without the mod loaded in the map editor.
                        // somehow the defaulting of 1 'blank' item doesn't get set correctly when using redirected functions.
                        // really would still like to remove this hack and find actual cause.
                        //                    uint inum;
                        if (Singleton<TreeManager>.instance.m_trees.ItemCount() == 0 & Singleton<TreeManager>.instance.m_treeCount == -1)
                        {
                            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog(" New or LoadMap Detected & itemcount==0 treecount == -1"); }
                            //removed for 1 vs 0 fix in Deserialize routine that was causing hack problem.
                            //                        if (Singleton<TreeManager>.instance.m_trees.CreateItem(out inum))
                            //                        {
                            //                            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or Loadmap Detected - Added padding, createditem# " + inum.ToString()); }
                            //                            Singleton<TreeManager>.instance.m_treeCount = (int)(Singleton<TreeManager>.instance.m_trees.ItemCount() - 1u);
                            //                            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or Loadmap Detected - treecount updated: " + Singleton<TreeManager>.instance.m_treeCount.ToString()); }
                            //                        }
                        }
                    }

                }

                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true)  //Debugging crap for the above stated hack.
                {
                    if (Singleton<SimulationManager>.instance.m_metaData != null)
                    {
                        Logger.dbgLog(String.Format("Mapname: {0}  Cityname: {1}", Singleton<SimulationManager>.instance.m_metaData.m_MapName, Singleton<SimulationManager>.instance.m_metaData.m_CityName));
                    }
                    TreeManager TreeMgr = Singleton<TreeManager>.instance;
                    int mtreecountr = TreeMgr.m_treeCount;
                    uint mtreebuffsize = TreeMgr.m_trees.m_size;
                    int mtreebuffleg = TreeMgr.m_trees.m_buffer.Length;
                    uint mtreebuffcount = TreeMgr.m_trees.ItemCount();
                    int mupdtreenum = TreeMgr.m_updatedTrees.Length;
                    int mburntreenum = TreeMgr.m_burningTrees.m_size;
                    Logger.dbgLog("Debugging-TreeManager: treecount=" + mtreecountr.ToString() + " msize=" + mtreebuffsize.ToString() + " mbuffleg=" + mtreebuffleg.ToString() + " buffitemcount=" + mtreebuffcount.ToString() + " UpdatedTreesSize=" + mupdtreenum.ToString() + " burntrees=" + mburntreenum.ToString() +"\r\n");
                    //Debug.Log("[TreeUnlimiter:OnLevelLoaded]  Done. ModStatus: " + Mod.IsEnabled.ToString() + "    RedirectStatus: " + Mod.IsSetupActive.ToString());
                }
            }

            catch (Exception ex)
            {
                Logger.dbgLog("Onlevelload Exception:", ex, true);
            }
            base.OnLevelLoaded(mode);
        }


        public override void OnLevelUnloading()
        {
            InGame = false;
            try
            {
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled()) {Logger.dbgLog("OnLevelUnloading() " + DateTime.Now.ToString(Mod.DTMilli)); }

                if (Mod.IsEnabled == true | Mod.IsSetupActive == true)
                {
                    //rebuild to org values seems to solve problem of mapeditor retaining prior map trees.
                    //side effect seems to be causes errors to be thrown in log when quitting app when in game.
                    //because there can be queued tasks\items that sort of continue for a second or so while it exits.
                    //TODO: either move this to on-released and find away around mapeditor issue\problem. (side effects?)
                    //      or ...idk maybe rework to always clear on load in data.deseralize upfront?...messy?.  
                    //^^ 12-22-2016 ^^ done!!
                    //ResetTreeMananger(Mod.DEFAULT_TREE_COUNT, Mod.DEFAULT_TREEUPDATE_COUNT);  


                    LoadingExtension.LastFileClearedFlag = false;
                    if (LastSaveList != null)
                    { LastSaveList.Clear(); LastSaveList.Capacity = 1; LastSaveList = null; }
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Error: ",ex,true);
            }
            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) {Logger.dbgLog("\r\n"); } //crlf easier on eyes.
            base.OnLevelUnloading();
        }


        public override void OnReleased()
        {
            try
            {
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled()) { Logger.dbgLog("OnReleased()  " + DateTime.Now.ToString(Mod.DTMilli) + "\r\n"); }

                if (Mod.IsEnabled == true | Mod.IsSetupActive == true)
                {
                    ResetTreeMananger(Mod.DEFAULT_TREE_COUNT, Mod.DEFAULT_TREEUPDATE_COUNT);
                    //1.6.0 -
                    // we're going to temp. not do this.
                    //Mod.ReveseSetup(); //attempt to revert redirects | has it's own try catch.
                }
            }
            catch(Exception ex)
            { Logger.dbgLog("", ex); }
            base.OnReleased();
        }


        //fuction to re-create the TreeManagers m_trees Array32 buffer entirely.
        //used to make sure it's clean and our objects don't carry over map 2 map.
        //also used in certain cases where we want to revert back to nomal treemanager sizes.
        public static void ResetTreeMananger(uint tsize, uint updatesize, bool bforce = false)
        {
            uint num;
            object[] ostring = new object[]{ tsize.ToString(), updatesize.ToString(), bforce.ToString(), Singleton<TreeManager>.instance.m_trees.m_buffer.Length.ToString() };
            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
            { Logger.dbgLog(String.Format("ResetTreeManager tszie= {0} updatesize= {1} bforce= {2} currentTMlen= {3}", ostring)); }

            if ((int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length != tsize || bforce == true)
            {
                Singleton<TreeManager>.instance.m_trees = new Array32<TreeInstance>((uint)tsize);
                Singleton<TreeManager>.instance.m_updatedTrees = new ulong[updatesize];
                Singleton<TreeManager>.instance.m_trees.CreateItem(out num);
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true) { Logger.dbgLog("Reset of TreeManager completed; forced=" + bforce.ToString() + "  " + DateTime.Now.ToString(Mod.DTMilli)); }
            }
        }
    }
}
