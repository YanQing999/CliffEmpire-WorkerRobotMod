// WorkerRobotMod REMASTER版 - v2.0：美化HUD面板+中英文
// 面板式: 半透明背景 + 彩色标题 + 数据行 + 语言切换按钮(点击检测)
// 中文用系统字体微软雅黑（动态加载）
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace WorkerRobotModREMASTER
{
    [BepInPlugin("com.g.workerrobotmod.remaster", "Worker Robot Mod (REMASTER)", "2.8.0")]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource Log;
        internal static readonly Dictionary<TERRA, int> RobotCounts = new Dictionary<TERRA, int>();
        internal static readonly Dictionary<TERRA, int> SavedWorks = new Dictionary<TERRA, int>();
        internal static readonly Dictionary<TERRA, int> BoostedWorks = new Dictionary<TERRA, int>();
        internal const int RobotEfficiency = 2;
        // v2.2 建造消耗（2026-09-25 定案：建造=400¥+1铀+1引擎+3物质 / 拆除返=200¥+1引擎+1物质）
        internal const int CostMoney = 800;
        internal const int RefundMoney = 200;
        internal static string ToastMsg = null;
        internal static float ToastTimer = 0f;
        // v2.1: 槽位识别（从mod列表名字定位Robot Factory，不依赖可调参数）
        internal static readonly List<int> FactorySlots = new List<int>();
        internal static float SlotCheckTimer = 0f;
        internal static bool UseChinese = true;
        internal static bool HudCreated = false;
        internal static float HudTimer = 0f;

        // UI引用
        internal static Text TitleText = null;
        internal static Text BodyText = null;
        internal static RectTransform ZhBtnRect = null;
        internal static RectTransform EnBtnRect = null;
        internal static Image ZhBgImage = null;
        internal static Image EnBgImage = null;
        internal static Text ZhBtnText = null;
        internal static Text EnBtnText = null;
        internal static Text HintText = null;
        internal static Font EnFont = null;
        internal static Font ZhFont = null;
        internal static bool ZhBtnHover, EnBtnHover;

        // ===== v2.6.0: 蓝段 v2——克隆 Population/Fill，紧贴住宅段右端 =====
        internal static GameObject LaborRobotGO = null;
        internal static RectTransform LaborRobotRect = null;
        internal static RectTransform HousingFillRect = null;
        internal static float LaborBar2RetryAt = 0f;

        // ===== v2.4.0: HUD 交互（拖拽 + 折叠 + 分辨率自适应）=====
        internal static Vector2 HudOffset = Vector2.zero;      // 用户拖动偏移（显示坐标系：x右 y下）
        internal static bool HudCollapsed = false;             // 是否收起
        internal static bool DraggingHud = false;              // 拖动中
        internal static Vector2 DragStartMouse;
        internal static Vector2 DragStartOffset;
        internal static int LastScreenW = 0, LastScreenH = 0;  // 分辨率追踪
        internal static RectTransform BgRT = null;
        internal static RectTransform TitleBarRT = null;
        internal static RectTransform ArrowRT = null;
        internal static Text ArrowLabel = null;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("[WRM-R] v2.8.0 加载(发布版·无开发键)");

            try
            {
                var harmony = new Harmony("com.g.workerrobotmod.remaster");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("[WRM-R] Harmony PatchAll完成");
            }
            catch (Exception e)
            {
                Log.LogError($"[WRM-R] Harmony异常: {e}");
            }

            HudCreated = false;
        }

        // ===== 存档 =====
        internal static string GetModSavePath()
        {
            try
            {
                int slot = H_SAVE_LOAD.SAVESLOT;
                string dir = Application.persistentDataPath;
                return Path.Combine(dir, "save" + slot + ".wrmod");
            }
            catch { return null; }
        }

        internal static void WriteModSave()
        {
            try
            {
                string path = GetModSavePath();
                if (path == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("WRM2 v1");
                foreach (var kv in RobotCounts)
                {
                    if (kv.Key == null) continue;
                    int id = 0;
                    try { id = kv.Key.ID; } catch { continue; }
                    sb.AppendLine($"{id}={kv.Value}");
                }
                File.WriteAllText(path, sb.ToString());
                Log.LogInfo($"[WRM-R] 存档写入OK ({RobotCounts.Count}场景): {path}");
            }
            catch (Exception e) { Log.LogError($"[WRM-R] 写存档失败: {e}"); }
        }

        internal static void ReadModSave()
        {
            try
            {
                RobotCounts.Clear();
                string path = GetModSavePath();
                if (path == null || !File.Exists(path)) return;
                var idToTerra = new Dictionary<int, TERRA>();
                TERRA[] all = UnityEngine.Object.FindObjectsOfType<TERRA>();
                foreach (var t2 in all)
                {
                    if (t2 == null) continue;
                    try { idToTerra[t2.ID] = t2; } catch { }
                }
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("WRM2")) continue;
                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    int id, cnt;
                    if (int.TryParse(parts[0], out id) && int.TryParse(parts[1], out cnt))
                    {
                        if (idToTerra.ContainsKey(id) && cnt > 0)
                        {
                            RobotCounts[idToTerra[id]] = cnt;
                            Log.LogInfo($"[WRM-R] 读档: 场景{id} 恢复{cnt}机器人");
                        }
                    }
                }
            }
            catch (Exception e) { Log.LogError($"[WRM-R] 读存档失败: {e}"); }
        }

        // ===== 工具: 创建UI元素 =====
        internal static Texture2D _whiteTex;
        internal static Sprite _whiteSprite;

        internal static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            _whiteTex = new Texture2D(2, 2);
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                    _whiteTex.SetPixel(x, y, Color.white);
            _whiteTex.Apply();
            _whiteSprite = Sprite.Create(_whiteTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }

        internal static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        internal static Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, Color color, Font font, TextAnchor align = TextAnchor.UpperLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.supportRichText = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return txt;
        }

        // ===== HUD创建 =====
        internal static void CreateHud()
        {
            try
            {
                if (HudCreated) return;

                // 字体
                EnFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                try { ZhFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 16); } catch { ZhFont = EnFont; }

                // Canvas
                var go = new GameObject("WRM_Canvas");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                go.AddComponent<GraphicRaycaster>();
                UnityEngine.Object.DontDestroyOnLoad(go);

                // 面板背景
                float px = 20, pyTop = Screen.height * 0.55f;
                float panelW = 330, panelH = 285;
                var bg = CreateImage(canvas.transform, "BG", new Vector2(px, -pyTop), new Vector2(panelW, panelH), new Color(0.04f, 0.04f, 0.1f, 0.88f));
                BgRT = bg.rectTransform;
                LastScreenW = Screen.width; LastScreenH = Screen.height;

                // 标题条（可拖动 + 右侧三角形折叠按钮）
                var titleBar = CreateImage(canvas.transform, "TitleBar", new Vector2(px, -pyTop), new Vector2(panelW, 34), new Color(0.1f, 0.25f, 0.45f, 0.9f));
                TitleBarRT = titleBar.rectTransform;
                TitleText = CreateText(canvas.transform, "Title", new Vector2(px + 12, -(pyTop + 4)), new Vector2(panelW - 54, 30), 21, new Color(0.48f, 0.84f, 1f, 1f), UseChinese ? ZhFont : EnFont, TextAnchor.MiddleLeft);

                // 三角形折叠按钮（标题条右侧，点一下收起/展开）
                var arrowBg = CreateImage(canvas.transform, "ArrowBtn", new Vector2(px + panelW - 34, -(pyTop + 4)), new Vector2(26, 26), new Color(0.18f, 0.36f, 0.6f, 0.95f));
                ArrowRT = arrowBg.rectTransform;
                ArrowLabel = CreateText(arrowBg.transform, "ArrowTxt", new Vector2(0, 0), new Vector2(26, 24), 16, Color.white, ZhFont, TextAnchor.MiddleCenter);
                ArrowLabel.rectTransform.anchorMin = Vector2.zero;
                ArrowLabel.rectTransform.anchorMax = Vector2.one;
                ArrowLabel.rectTransform.anchoredPosition = Vector2.zero;
                ArrowLabel.rectTransform.sizeDelta = Vector2.zero;

                // 内容
                BodyText = CreateText(canvas.transform, "Body", new Vector2(px + 14, -(pyTop + 48)), new Vector2(panelW - 24, 150), 18, Color.white, UseChinese ? ZhFont : EnFont);

                // 语言按钮（文字挂在按钮Image下）
                float btnY = pyTop + 185;
                var zhBg = CreateImage(canvas.transform, "ZhBtn", new Vector2(px + 10, -btnY), new Vector2(74, 30), new Color(0.9f, 0.95f, 1f, 1f));
                ZhBtnRect = zhBg.rectTransform;
                ZhBtnText = CreateText(zhBg.transform, "T", new Vector2(0, 0), new Vector2(74, 24), 15, Color.black, ZhFont, TextAnchor.MiddleCenter);
                ZhBtnText.rectTransform.anchorMin = Vector2.zero;
                ZhBtnText.rectTransform.anchorMax = Vector2.one;
                ZhBtnText.rectTransform.anchoredPosition = Vector2.zero;
                ZhBtnText.rectTransform.sizeDelta = Vector2.zero;

                var enBg = CreateImage(canvas.transform, "EnBtn", new Vector2(px + 96, -btnY), new Vector2(74, 30), new Color(0.35f, 0.4f, 0.5f, 1f));
                EnBtnRect = enBg.rectTransform;
                EnBtnText = CreateText(enBg.transform, "T", new Vector2(0, 0), new Vector2(74, 24), 15, Color.white, EnFont, TextAnchor.MiddleCenter);
                EnBtnText.rectTransform.anchorMin = Vector2.zero;
                EnBtnText.rectTransform.anchorMax = Vector2.one;
                EnBtnText.rectTransform.anchoredPosition = Vector2.zero;
                EnBtnText.rectTransform.sizeDelta = Vector2.zero;

                Plugin.ZhBgImage = zhBg;
                Plugin.EnBgImage = enBg;
                ZhBtnText.text = "中文";
                EnBtnText.text = "EN";

                // 快捷键提示
                var hint = CreateText(canvas.transform, "Hint", new Vector2(px + 12, -(btnY + 36)), new Vector2(panelW - 20, 56), 13, new Color(0.75f, 0.75f, 0.8f, 1f), UseChinese ? ZhFont : EnFont, TextAnchor.MiddleLeft);
                Plugin.HintText = hint;
                hint.supportRichText = true;

                HudCreated = true;
                LayoutHud();
                Log.LogInfo("[WRM-R] 美化HUD创建成功 (v2.4 可拖动+折叠)");
                RefreshHud();
            }
            catch (Exception e)
            {
                Log.LogError($"[WRM-R] CreateHud异常: {e}");
            }
        }

        // ===== v2.4.0: HUD 统一布局（拖动偏移 + 分辨率自适应 + 折叠） =====
        internal static void LayoutHud()
        {
            try
            {
                float px = 20f + HudOffset.x;
                float pyTop = Screen.height * 0.55f + HudOffset.y;
                float panelW = 330f;
                float panelH = HudCollapsed ? 34f : 285f;

                if (BgRT != null) { BgRT.anchoredPosition = new Vector2(px, -pyTop); BgRT.sizeDelta = new Vector2(panelW, panelH); }
                if (TitleBarRT != null) TitleBarRT.anchoredPosition = new Vector2(px, -pyTop);
                if (TitleText != null) TitleText.rectTransform.anchoredPosition = new Vector2(px + 12, -(pyTop + 4));
                if (ArrowRT != null) ArrowRT.anchoredPosition = new Vector2(px + panelW - 34, -(pyTop + 4));

                if (BodyText != null) BodyText.rectTransform.anchoredPosition = new Vector2(px + 14, -(pyTop + 48));
                float btnY = pyTop + 185f;
                if (ZhBtnRect != null) ZhBtnRect.anchoredPosition = new Vector2(px + 10, -btnY);
                if (EnBtnRect != null) EnBtnRect.anchoredPosition = new Vector2(px + 96, -btnY);
                if (HintText != null) HintText.rectTransform.anchoredPosition = new Vector2(px + 12, -(btnY + 36));

                bool show = !HudCollapsed;
                if (BodyText != null) BodyText.gameObject.SetActive(show);
                if (ZhBgImage != null) ZhBgImage.gameObject.SetActive(show);
                if (EnBgImage != null) EnBgImage.gameObject.SetActive(show);
                if (HintText != null) HintText.gameObject.SetActive(show);
                if (ArrowLabel != null) ArrowLabel.text = HudCollapsed ? "\u25B6" : "\u25BC";
            }
            catch (Exception e) { Log.LogError($"[WRM-R] LayoutHud异常: {e.Message}"); }
        }

        // ===== 刷新内容 =====
        internal static string SceneName(TERRA t)
        {
            if (t == null) return "?";
            if (IsSpaceStation(t)) return UseChinese ? "空间站" : "Orbital Station";
            return UseChinese ? $"场景{t.ID}" : $"Terra {t.ID}";
        }

        // ===== Robot Factory识别与上限 =====
        // v2.1 (2026-09-21): 改为「mod槽位名」识别——从 USER_UI.MODE_LOADED 里找 BuildName=Robot Factory
        // 的mod槽位，建筑实例名含 "MOD_{槽位}(" 即命中。用户改任何参数都不影响识别。
        // 保留旧参数指纹作为兼容通道（已发布版建筑 Humans=0 + 1000钱 + 100金属）。
        internal static void RefreshFactorySlots()
        {
            try
            {
                var ui = USER_UI.singletone;
                if (ui == null) return;
                var loaded = ui.MODE_LOADED;
                if (loaded == null) return;
                List<int> found = new List<int>();
                for (int i = 0; i < loaded.Length; i++)
                {
                    bool match = false;
                    try
                    {
                        var go = loaded[i];
                        if (go != null)
                        {
                            var bp = go.GetComponent<BuildParams>();
                            if (bp != null)
                            {
                                string n = bp.BuildName;
                                if (n == "Robot Factory" || n == "机器人工厂") match = true;
                            }
                        }
                    }
                    catch { }
                    if (match) found.Add(i + 1);
                }
                bool changed = found.Count != FactorySlots.Count;
                if (!changed)
                {
                    for (int k = 0; k < found.Count; k++)
                    {
                        if (found[k] != FactorySlots[k]) { changed = true; break; }
                    }
                }
                if (changed)
                {
                    FactorySlots.Clear();
                    FactorySlots.AddRange(found);
                    string slots = found.Count == 0 ? "(未找到)" : string.Join(",", found);
                    Log.LogInfo("[WRM-R] 工厂槽位刷新: MOD_" + slots);
                }
            }
            catch (Exception e) { Log.LogError("[WRM-R] 槽位扫描异常: " + e.Message); }
        }

        // ===== v2.3.1: zzz图标同步——游戏对mod建筑的"禁用→图标显示"联动没触发，插件补刀 =====
        // 证据(2026-09-25 F11两轮侦察): 工厂 Sleep=True 时 MyZZZAsAllowSleep.activeSelf 仍=False;
        // 原生建筑 Sleep=True 时 activeSelf=True。游戏→mod建筑缺一步SetActive，这里每0.5s对齐。
        internal static float ZzzSyncTimer = 0f;
        internal static void SyncZzzIcons(float dt)
        {
            ZzzSyncTimer -= dt;
            if (ZzzSyncTimer > 0f) return;
            ZzzSyncTimer = 0.5f;
            try
            {
                TERRA terra = GetActiveTerra();
                if (terra == null) return;
                var all = terra.AllBlds;
                if (all == null) return;
                int fixedCnt = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    BLD b = null;
                    try { b = all[i]; } catch { continue; }
                    if (b == null) continue;
                    if (!IsRobotFactory(b)) continue;
                    try
                    {
                        bool slp = b.Sleep;
                        var z = b.MyZZZAsAllowSleep;
                        if (z == null) continue;
                        if (z.activeSelf != slp)
                        {
                            z.SetActive(slp);
                            fixedCnt++;
                        }
                    }
                    catch { }
                }
                if (fixedCnt > 0) Log.LogInfo($"[WRM-R] zzz图标同步: {fixedCnt}个工厂");
            }
            catch { }
        }

        // ===== v2.6.0: 蓝段 v2——克隆 Population/Fill；从"住宅段右端"开始画；长度=当量/岗位；封顶100% =====
        internal static void InitLaborBar2()
        {
            try
            {
                if (Time.unscaledTime < LaborBar2RetryAt) return;
                var popT = GameObject.Find("H_CORE/USER_UI/UI_VIS/PopulationBar/PosPop/Population");
                var housingT = GameObject.Find("H_CORE/USER_UI/UI_VIS/PopulationBar/PosPop/Housing");
                if (popT == null || housingT == null) { LaborBar2RetryAt = Time.unscaledTime + 1f; return; }
                var srcFill = popT.transform.Find("Fill");
                var hFill = housingT.transform.Find("Fill");
                if (srcFill == null || hFill == null) { LaborBar2RetryAt = Time.unscaledTime + 1f; return; }

                HousingFillRect = hFill.GetComponent<RectTransform>();

                var clone = UnityEngine.Object.Instantiate(srcFill.gameObject, popT.transform);
                clone.name = "WRM_LaborRobotFill";
                for (int i = clone.transform.childCount - 1; i >= 0; i--)
                {
                    var ch = clone.transform.GetChild(i).gameObject;
                    ch.SetActive(false);
                    UnityEngine.Object.Destroy(ch);
                }
                var cImg = clone.GetComponent<Image>();
                if (cImg != null)
                {
                    cImg.color = new Color(0.30f, 0.62f, 1.00f, 1.00f);
                    cImg.raycastTarget = false;
                }
                var crt = clone.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.52f, 0f);
                crt.anchorMax = new Vector2(0.52f, 1f);
                crt.offsetMin = Vector2.zero;
                crt.offsetMax = Vector2.zero;
                clone.transform.SetAsLastSibling();
                clone.SetActive(false);

                LaborRobotGO = clone;
                LaborRobotRect = crt;
                Log.LogInfo("[WRM-LB2] ✅ 蓝段就绪（克隆 Population/Fill，稍后随数值显示）");
            }
            catch (Exception e) { Log.LogError($"[WRM-LB2] Init 异常: {e.Message}"); }
        }

        internal static void UpdateLaborBar2()
        {
            try
            {
                if (LaborRobotGO == null)
                {
                    InitLaborBar2();
                    if (LaborRobotGO == null) return;
                }
                TERRA terra = GetActiveTerra();
                if (terra == null) return;
                int robots = RobotCounts.TryGetValue(terra, out int r) ? r : 0;
                int eff = robots * RobotEfficiency;
                int req = 0;
                try { req = terra.PopulationWorkersREQ; } catch { }
                if (req <= 0 || eff <= 0)
                {
                    if (LaborRobotGO.activeSelf) LaborRobotGO.SetActive(false);
                    return;
                }

                float start = 0.52f;
                try { if (HousingFillRect != null) start = HousingFillRect.anchorMax.x; } catch { }
                start = Mathf.Clamp01(start);
                float end = Mathf.Clamp01(start + (float)eff / (float)req);
                if (end - start < 0.001f)
                {
                    if (LaborRobotGO.activeSelf) LaborRobotGO.SetActive(false);
                    return;
                }
                if (!LaborRobotGO.activeSelf) LaborRobotGO.SetActive(true);
                if (Math.Abs(LaborRobotRect.anchorMin.x - start) > 0.0005f || Math.Abs(LaborRobotRect.anchorMax.x - end) > 0.0005f)
                {
                    LaborRobotRect.anchorMin = new Vector2(start, 0f);
                    LaborRobotRect.anchorMax = new Vector2(end, 1f);
                    LaborRobotRect.offsetMin = Vector2.zero;
                    LaborRobotRect.offsetMax = Vector2.zero;
                }
            }
            catch { }
        }

        // ===== v2.4.1: F4 UI 抓取器——找"劳动力"条的真身结构 =====
        internal static bool IsRobotFactory(BLD b)
        {
            if (b == null) return false;
            try
            {
                // 通道1: mod槽位名匹配（v2.1主通道）
                if (FactorySlots.Count > 0)
                {
                    string n = null;
                    try { n = b.gameObject != null ? b.gameObject.name : null; } catch { }
                    if (!string.IsNullOrEmpty(n))
                    {
                        for (int k = 0; k < FactorySlots.Count; k++)
                        {
                            if (n.Contains("MOD_" + FactorySlots[k] + "(")) return true;
                        }
                    }
                }
                // 通道2: 兼容旧发布版参数指纹
                if (b.Humans == 0 && b.BuildPrice_ICmoney == 1000 && b.BuildPrice_Metal == 100) return true;
                return false;
            }
            catch { return false; }
        }

        // 统计指定场景里的Robot Factory数量（含运行中和未运行的）
        internal static int CountRobotFactory(TERRA target)
        {
            try
            {
                if (target == null) return 0;
                int count = 0;
                BLD[] all = UnityEngine.Object.FindObjectsOfType<BLD>();
                if (all == null) return 0;
                int targetId = -1;
                try { targetId = target.ID; } catch { }
                foreach (var b in all)
                {
                    if (b == null) continue;
                    if (!IsRobotFactory(b)) continue;
                    // 诊断: MyTerra.ID
                    int myTerraId = -999;
                    try
                    {
                        var t = b.MyTerra;
                        if (t != null) myTerraId = t.ID;
                        else myTerraId = -1;
                    }
                    catch (Exception ex) { myTerraId = -2; }
                    if (myTerraId == targetId) count++;
                }
                return count;
            }
            catch { return 0; }
        }

        // 机器人上限 = min(居民数, 工厂数×50)
        internal static int GetRobotLimit(TERRA terra)
        {
            int byPop = Mathf.Max(0, terra.PopulationCount - terra.PopulationMIGRANT);
            int byFactory = CountRobotFactory(terra) * 50;
            int limit = Mathf.Min(byPop, byFactory);
            return limit;
        }

        internal static void RefreshHud()
        {
            try
            {
                if (!HudCreated || TitleText == null || BodyText == null) return;

                // 字体切换
                Font font = UseChinese ? ZhFont : EnFont;
                TitleText.font = font;
                BodyText.font = font;

                // 按钮高亮：当前语言亮色，另一个暗色
                if (ZhBgImage != null && EnBgImage != null)
                {
                    if (UseChinese)
                    {
                        ZhBgImage.color = new Color(0.85f, 0.95f, 1f, 1f);   // 亮
                        ZhBtnText.color = new Color(0.05f, 0.2f, 0.5f, 1f);
                        ZhBtnText.fontStyle = FontStyle.Bold;
                        EnBgImage.color = new Color(0.3f, 0.33f, 0.4f, 1f);   // 暗
                        EnBtnText.color = new Color(0.7f, 0.72f, 0.8f, 1f);
                        EnBtnText.fontStyle = FontStyle.Normal;
                    }
                    else
                    {
                        EnBgImage.color = new Color(0.5f, 0.8f, 1f, 1f);      // 亮
                        EnBtnText.color = new Color(0.02f, 0.1f, 0.3f, 1f);
                        EnBtnText.fontStyle = FontStyle.Bold;
                        ZhBgImage.color = new Color(0.3f, 0.33f, 0.4f, 1f);   // 暗
                        ZhBtnText.color = new Color(0.7f, 0.72f, 0.8f, 1f);
                        ZhBtnText.fontStyle = FontStyle.Normal;
                    }
                    ZhBtnText.font = ZhFont;
                    EnBtnText.font = EnFont;
                }

                // 热键提示
                if (HintText != null)
                {
                    HintText.font = font;
                    string line1 = UseChinese
                        ? "F9 制造 · " + CostMoney + "¥"
                        : "F9 build · " + CostMoney + "$";
                    string line2 = UseChinese
                        ? "F10 拆除 · 返 " + RefundMoney + "¥"
                        : "F10 remove · refund " + RefundMoney + "$";
                    string line3 = UseChinese ? "存档自动保存" : "auto-save";
                    if (ToastMsg != null)
                        HintText.text = "<color=#ff9a7d><b>" + ToastMsg + "</b></color>\n" + line2 + "\n" + line3;
                    else
                        HintText.text = line1 + "\n" + line2 + "\n" + line3;
                }

                TERRA terra = GetActiveTerra();
                if (terra == null)
                {
                    TitleText.text = UseChinese ? "⚙ 工作机器人" : "⚙ Worker Robots";
                    BodyText.text = UseChinese ? "等待场景..." : "waiting...";
                    return;
                }

                TitleText.text = UseChinese
                    ? $"⚙ 工作机器人 - {SceneName(terra)}"
                    : $"⚙ Worker Robots - {SceneName(terra)}";

                int robots = RobotCounts.TryGetValue(terra, out int r) ? r : 0;
                int factoryCount = CountRobotFactory(terra);
                int maxRobots = GetRobotLimit(terra);
                int req = Mathf.Max(0, terra.PopulationWorkersREQ);
                int works = Mathf.Max(0, terra.PopulationWORKS);
                int eff = works + robots * RobotEfficiency;
                float noMed = terra.PopulationAffectNoMedAff;
                float aff = terra.PopulationAffect;
                bool isStation = IsSpaceStation(terra);

                if (isStation)
                {
                    BodyText.text = UseChinese
                        ? "空间站不支持机器人\n\n(其他场景可用)"
                        : "No robots on station\n\n(available on other terra)";
                }
                else
                {
                    // 颜色: 充足绿色/缺人黄色/严重红色
                    int def = req - eff;
                    string defColor = def <= 0 ? "#7dffa0" : (def < 50 ? "#ffd97d" : "#ff8a7d");
                    string robColor = robots > 0 ? "#7ad7ff" : "#aaaaaa";
                    string factStr = UseChinese ? $"工厂: {factoryCount} 座" : $"Factories: {factoryCount}";
                    string factColor = factoryCount > 0 ? "#7dffa0" : "#ff8a7d";
                    if (UseChinese)
                    {
                        BodyText.text =
                            $"机器人: <color={robColor}><b>{robots}</b></color> / {maxRobots}\n" +
                            $"<color={factColor}>机器人工厂: {factoryCount} 座</color> (每座提供50名额)\n" +
                            $"效率: {RobotEfficiency}x (顶{RobotEfficiency}个工人)\n" +
                            $"劳动力: {works} + {robots * RobotEfficiency} = <b>{eff}</b>\n" +
                            $"岗位(REQ): {req}   缺口: <color={defColor}><b>{def}</b></color>\n" +
                            $"劳动力系数: {noMed * 100f:F1}%   (医疗后 {aff * 100f:F1}%)";
                    }
                    else
                    {
                        BodyText.text =
                            $"Robots: <color={robColor}><b>{robots}</b></color> / {maxRobots}\n" +
                            $"<color={factColor}>Robot Factories: {factoryCount}</color> (each = 50 slots)\n" +
                            $"Efficiency: {RobotEfficiency}x (={RobotEfficiency} workers)\n" +
                            $"Workers: {works} + {robots * RobotEfficiency} = <b>{eff}</b>\n" +
                            $"Jobs(REQ): {req}   Deficit: <color={defColor}><b>{def}</b></color>\n" +
                            $"Labor Affect: {noMed * 100f:F1}%  (after med {aff * 100f:F1}%)";
                    }
                }
            }
            catch { }
        }

        internal static TERRA GetActiveTerra()
        {
            try
            {
                var ui = USER_UI.singletone;
                if (ui != null && ui.CurrentTerra != null)
                    return ui.CurrentTerra;
            }
            catch { }
            TERRA[] all = UnityEngine.Object.FindObjectsOfType<TERRA>();
            if (all == null || all.Length == 0) return null;
            return all[0];
        }

        internal static bool IsSpaceStation(TERRA terra)
        {
            if (terra == null) return false;
            try { return terra.ID == 10; } catch { return false; }
        }

        // ===== 诊断：列出场景所有建筑的名字（验证能否识别mod建筑）=====
        internal static double GetMoney(TERRA t)
        {
            try
            {
                var p = t.GetType().GetProperty("ICmoney");
                if (p == null) return -1;
                object v = p.GetValue(t, null);
                return System.Convert.ToDouble(v);
            }
            catch { return -1; }
        }
        internal static bool SetMoney(TERRA t, double val)
        {
            try
            {
                var p = t.GetType().GetProperty("ICmoney");
                if (p == null) return false;
                Type pt = p.PropertyType;
                object cv;
                if (pt == typeof(int)) cv = (int)Math.Round(val);
                else if (pt == typeof(float)) cv = (float)val;
                else if (pt == typeof(long)) cv = (long)Math.Round(val);
                else if (pt == typeof(double)) cv = val;
                else cv = System.Convert.ChangeType(val, pt);
                p.SetValue(t, cv, null);
                return true;
            }
            catch { return false; }
        }
        internal static void ShowToast(string msg)
        {
            ToastMsg = msg;
            ToastTimer = 3f;
        }

        internal static void ChangeRobot(int delta)
        {
            try
            {
                TERRA terra = GetActiveTerra();
                if (terra == null) { Log.LogWarning("[WRM-R] 无TERRA"); return; }
                if (IsSpaceStation(terra)) { Log.LogInfo("[WRM-R] 空间站不支持机器人"); return; }
                int current = RobotCounts.TryGetValue(terra, out int v) ? v : 0;
                int factoryCount = CountRobotFactory(terra);
                int maxRobots = GetRobotLimit(terra);
                // 无工厂时不能造机器人（只有拆除允许）
                if (delta > 0 && factoryCount <= 0)
                {
                    Log.LogInfo("[WRM-R] 没有Robot Factory，无法制造机器人");
                    ShowToast(UseChinese ? "没有机器人工厂" : "No Robot Factory");
                    return;
                }
                // 建造受上限限制；拆除不受（防"工厂没了拆不掉"）
                int next = delta > 0 ? Mathf.Clamp(current + delta, 0, maxRobots) : Mathf.Max(0, current + delta);
                if (next == current)
                {
                    if (delta > 0) ShowToast(UseChinese ? "已达机器人上限" : "Robot limit reached");
                    else ShowToast(UseChinese ? "没有机器人可拆" : "No robot to remove");
                    return;
                }

                // ===== v2.7 建造结算：只花钱（物资消耗已取消——发布版规则）=====
                if (delta > 0)
                {
                    double money = GetMoney(terra);
                    string lack = null;
                    if (money >= 0 && money < CostMoney)
                        lack = (UseChinese ? "钱" : "Money") + " " + CostMoney + (UseChinese ? "(有" + (int)money + ")" : "(have " + (int)money + ")");
                    if (lack != null)
                    {
                        Log.LogInfo("[WRM-R] 资源不足，无法建造，缺: " + lack);
                        ShowToast((UseChinese ? "⚠ 资源不足: 缺 " : "⚠ Not enough: ") + lack);
                        return;
                    }
                    if (money >= 0) SetMoney(terra, money - CostMoney);
                    double moneyLeft = money >= 0 ? money - CostMoney : -99;
                    Log.LogInfo("[WRM-R] 造价扣款 " + current + "→" + next + ": 钱-" + CostMoney + "(余" + (int)moneyLeft + ") | v2.7 只花钱（物资消耗已取消）");
                }
                else
                {
                    double money = GetMoney(terra);
                    if (money >= 0) SetMoney(terra, money + RefundMoney);
                    Log.LogInfo("[WRM-R] 拆除返还 " + current + "→" + next + ": 钱+" + RefundMoney + " | v2.7 只返钱（防造拆循环刷物资）");
                }

                RobotCounts[terra] = next;
                int req = Mathf.Max(0, terra.PopulationWorkersREQ);
                int works = Mathf.Max(0, terra.PopulationWORKS);
                int eff = works + next * RobotEfficiency;
                Log.LogInfo($"[WRM-R] 场景{terra.ID} 机器人 {current}→{next}/{maxRobots} | 有效 {eff} | 岗位{req} | 缺{req - eff}");
                terra.StatsRefresh();
            }
            catch (Exception e) { Log.LogError($"[WRM-R] ChangeRobot异常: {e}"); }
        }
    }

    // ===== Prefix: 临时抬高WORKS =====
    [HarmonyPatch(typeof(TERRA), "StatsRefresh")]
    internal static class Patch_StatsRefresh
    {
        static void Prefix(TERRA __instance)
        {
            try
            {
                if (__instance == null) return;
                if (Plugin.IsSpaceStation(__instance)) return;
                int robots = Plugin.RobotCounts.TryGetValue(__instance, out int r) ? r : 0;
                if (robots <= 0) return;
                int original = __instance.PopulationWORKS;
                int req = Mathf.Max(0, __instance.PopulationWorkersREQ);
                int boosted = Mathf.Min(original + robots * Plugin.RobotEfficiency, Mathf.Max(original, req));
                Plugin.SavedWorks[__instance] = original;
                Plugin.BoostedWorks[__instance] = boosted;
                __instance.PopulationWORKS = boosted;
            }
            catch { }
        }

        static void Postfix(TERRA __instance)
        {
            try
            {
                if (__instance == null) return;
                // v2.1.1 修复：StatsRefresh 内部会重算 WORKS（包含最新人口）。
                // 只有当 WORKS 仍是我们抬上去的值时才恢复原值；
                // 若游戏已重算（值变了），保留游戏的新值——不然会把人
                // 口/工人变化覆盖回旧数据（HUD 不更新的根因）。
                if (Plugin.SavedWorks.TryGetValue(__instance, out int original))
                {
                    int boosted = Plugin.BoostedWorks.TryGetValue(__instance, out int bw) ? bw : int.MinValue;
                    if (__instance.PopulationWORKS == boosted)
                        __instance.PopulationWORKS = original;
                    Plugin.SavedWorks.Remove(__instance);
                    Plugin.BoostedWorks.Remove(__instance);
                }
            }
            catch { }
        }
    }

    // ===== 每帧: 按键/点击检测 + HUD刷新 =====
    [HarmonyPatch(typeof(USER_UI), "Update")]
    internal static class Patch_UserUIUpdate
    {
        static int _lastFrame = -1;

        static void Postfix()
        {
            try
            {
                if (!Plugin.HudCreated)
                {
                    Plugin.CreateHud();
                    return; // 创建后下帧再处理
                }

                PendingLoadBridge.Check();

                int frame = Time.frameCount;
                if (frame == _lastFrame) return;
                _lastFrame = frame;

                // 快捷键
                if (Input.GetKeyDown(KeyCode.F9)) Plugin.ChangeRobot(1);
                else if (Input.GetKeyDown(KeyCode.F10)) Plugin.ChangeRobot(-1);

                // 语言按钮点击检测（屏幕坐标左下原点, Canvas Overlay无scaler=像素一致）
                if (Input.GetMouseButtonDown(0) && Plugin.ZhBtnRect != null && Plugin.EnBtnRect != null)
                {
                    Vector2 mp = Input.mousePosition;
                    if (RectContainsScreen(Plugin.ZhBtnRect, mp))
                    {
                        Plugin.UseChinese = true;
                        Plugin.RefreshHud();
                        Plugin.Log.LogInfo("[WRM-R] 切换中文");
                    }
                    else if (RectContainsScreen(Plugin.EnBtnRect, mp))
                    {
                        Plugin.UseChinese = false;
                        Plugin.RefreshHud();
                        Plugin.Log.LogInfo("[WRM-R] 切换英文");
                    }
                }

                // Toast 计时（3秒后自动消失）
                if (Plugin.ToastTimer > 0f)
                {
                    Plugin.ToastTimer -= Time.deltaTime;
                    if (Plugin.ToastTimer <= 0f) { Plugin.ToastMsg = null; Plugin.RefreshHud(); }
                }

                // HUD 0.3秒刷新
                Plugin.HudTimer -= Time.deltaTime;
                if (Plugin.HudTimer <= 0f)
                {
                    Plugin.HudTimer = 0.3f;
                    Plugin.RefreshHud();
                }

                // v2.1: 每2秒刷新mod槽位识别
                Plugin.SlotCheckTimer -= Time.deltaTime;
                if (Plugin.SlotCheckTimer <= 0f)
                {
                    Plugin.SlotCheckTimer = 2f;
                    Plugin.RefreshFactorySlots();
                }

                // v2.3.1: zzz图标同步（0.5s节流）——工厂禁用时点亮zzz图标
                Plugin.SyncZzzIcons(Time.deltaTime);

                // v2.6.0: 劳动力条蓝段刷新（克隆 Population/Fill 版）
                Plugin.UpdateLaborBar2();

                // ===== v2.4.0: HUD 交互（拖拽 / 折叠 / 分辨率自适应）=====
                if (Screen.width != Plugin.LastScreenW || Screen.height != Plugin.LastScreenH)
                {
                    Plugin.LastScreenW = Screen.width;
                    Plugin.LastScreenH = Screen.height;
                    Plugin.LayoutHud();
                }
                if (Plugin.HudCreated && Input.GetMouseButtonDown(0))
                {
                    Vector2 mpH = Input.mousePosition;
                    if (Plugin.ArrowRT != null && RectContainsScreen(Plugin.ArrowRT, mpH))
                    {
                        Plugin.HudCollapsed = !Plugin.HudCollapsed;
                        Plugin.LayoutHud();
                        Plugin.Log.LogInfo(Plugin.HudCollapsed ? "[WRM-R] HUD收起" : "[WRM-R] HUD展开");
                    }
                    else if (Plugin.TitleBarRT != null && RectContainsScreen(Plugin.TitleBarRT, mpH))
                    {
                        Plugin.DraggingHud = true;
                        Plugin.DragStartMouse = mpH;
                        Plugin.DragStartOffset = Plugin.HudOffset;
                    }
                }
                if (Plugin.DraggingHud && Input.GetMouseButton(0))
                {
                    Vector2 mpH = Input.mousePosition;
                    Vector2 delta = new Vector2(mpH.x - Plugin.DragStartMouse.x, -(mpH.y - Plugin.DragStartMouse.y));
                    Plugin.HudOffset = Plugin.DragStartOffset + delta;
                    Plugin.LayoutHud();
                }
                if (Input.GetMouseButtonUp(0)) Plugin.DraggingHud = false;
            }
            catch { }
        }

        // RectTransform(anchor 0,1 + anchoredPosition) 转屏幕坐标判断点击
        static bool RectContainsScreen(RectTransform rt, Vector2 screenPos)
        {
            try
            {
                // rt.anchoredPosition是左上原点(anchor 0,1), 屏幕是左下原点
                float x = rt.anchoredPosition.x;
                float topY = rt.anchoredPosition.y; // 正值=往下(因为我们在Create里传了负y转正? 见下)
                // CreateImage用pos=(x, -pyTop), pivot(0,1) → anchoredPosition.y=-pyTop表示顶部在pyTop处
                float screenTop = -rt.anchoredPosition.y;
                float left = rt.anchoredPosition.x;
                float w = rt.sizeDelta.x;
                float h = rt.sizeDelta.y;
                float screenY = Screen.height - screenTop; // 顶部y
                bool inside = screenPos.x >= left && screenPos.x <= left + w
                           && screenPos.y <= screenY && screenPos.y >= screenY - h;
                return inside;
            }
            catch { return false; }
        }
    }

    // ===== 存档Hook =====
    [HarmonyPatch(typeof(H_SAVE_LOAD), "SAVE")]
    internal static class Patch_Save
    {
        static void Postfix(bool save_settings, bool save_base)
        {
            try
            {
                if (save_base) Plugin.WriteModSave();
            }
            catch (Exception e) { UnityEngine.Debug.LogError("[WRM-R] Save hook: " + e); }
        }
    }

    [HarmonyPatch(typeof(H_SAVE_LOAD), "LOAD")]
    internal static class Patch_Load
    {
        static bool _pending = false;
        static void Postfix(bool load_settings, bool load_base)
        {
            try { if (load_base) _pending = true; }
            catch (Exception e) { UnityEngine.Debug.LogError("[WRM-R] Load hook: " + e); }
        }
        internal static void TryPendingLoad()
        {
            if (!_pending) return;
            _pending = false;
            Plugin.ReadModSave();
        }
    }

    public static class PendingLoadBridge
    {
        public static void Check()
        {
            try { Patch_Load.TryPendingLoad(); } catch { }
        }
    }
}
