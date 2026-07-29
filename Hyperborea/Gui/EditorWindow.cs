using Dalamud.Interface.Components;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea.Gui;
public unsafe class EditorWindow : Window
{
    Dictionary<string, HashSet<uint>> BgToTerritoryType = [];
    internal uint SelectedTerritory = 0;
    uint TerrID => SelectedTerritory == 0 ? Svc.ClientState.TerritoryType : SelectedTerritory;
    public EditorWindow() : base("Hyperborea 區域編輯器###Hyperborea Zone Editor")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
        foreach(var x in Svc.Data.GetExcelSheet<TerritoryType>())
        {
            var bg = ((TerritoryType?)x).GetBG();
            if (!bg.IsNullOrEmpty())
            {
                if(!BgToTerritoryType.TryGetValue(bg, out var list))
                {
                    list = [];
                    BgToTerritoryType[bg] = list;
                }
                list.Add(x.RowId);
            }
        }
    }

    public override void Draw()
    {
        var cur = ImGui.GetCursorPos();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.SetCursorPosX(ImGuiEx.GetWindowContentRegionWidth() - ImGui.CalcTextSize("\uf0c7").X);
        if(Utils.TryGetZoneInfo(ExcelTerritoryHelper.GetBG(TerrID), out _, out var isOverriden1))
        {
            if (isOverriden1)
            {
                ImGuiEx.Text(EColor.YellowBright, $"\uf0c7");
                ImGui.PopFont();
                ImGuiEx.Tooltip("區域資料正從你的覆寫資料檔載入。");
            }
            else
            {
                ImGuiEx.Text(EColor.GreenBright, $"\uf0c7");
                ImGui.PopFont();
                ImGuiEx.Tooltip("區域資料已從主要資料檔載入。");
            }
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, $"\uf0c7");
            ImGui.PopFont();
            ImGuiEx.Tooltip("主要資料檔與覆寫資料檔中都找不到此區域的設定。");
        }
        ImGui.SetCursorPos(cur);
        var shares = BgToTerritoryType.TryGetValue(ExcelTerritoryHelper.GetBG(TerrID), out var set) ? set : [];
        ImGuiEx.TextWrapped($"目前編輯：{ExcelTerritoryHelper.GetName(TerrID, true)}");
        if(shares.Count > 1)
        {
            ImGuiComponents.HelpMarker($"與下列區域共用資料：\n{shares.Where(z => z != TerrID).Select(z => ExcelTerritoryHelper.GetName(z, true)).Print("\n")}");
        }
        if (ImGuiComponents.IconButtonWithText((FontAwesomeIcon)0xf002, "瀏覽###Browse"))
        {
            new TerritorySelector(SelectedTerritory, (_, x) =>
            {
                SelectedTerritory = x;
            });
        }
        ImGui.SameLine();
        if(ImGuiComponents.IconButtonWithText((FontAwesomeIcon)0xf276, "目前區域###Current Zone"))
        {
            SelectedTerritory = 0;
        }

        var bg = ExcelTerritoryHelper.GetBG(TerrID);
        if (bg.IsNullOrEmpty())
        {
            ImGuiEx.Text("不支援此區域");
        }
        else
        {
            if (Utils.TryGetZoneInfo(bg, out var info, out var isOverriden))
            {
                var overrideSpawn = info.Spawn != null;
                if (ImGui.Checkbox("自訂出生點###Custom Spawn Point", ref overrideSpawn))
                {
                    info.Spawn = overrideSpawn ? new() : null;
                }
                if (overrideSpawn)
                {
                    UI.CoordBlock("X:", ref info.Spawn.X);
                    ImGui.SameLine();
                    UI.CoordBlock("Y:", ref info.Spawn.Y);
                    ImGui.SameLine();
                    UI.CoordBlock("Z:", ref info.Spawn.Z);
                    ImGui.SameLine();
                    if (ImGuiEx.IconButton("\uf3c5")) info.Spawn = Player.Object.Position.ToPoint3();
                    ImGuiEx.Tooltip("將區域出生點設為角色目前位置。");
                }
                ImGui.Separator();
                ImGuiEx.TextV("階段：");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesome.Plus))
                {
                    info.Phases.Add(new());
                }
                ImGuiEx.Tooltip("建立新階段。");
                foreach (var p in info.Phases)
                {
                    ImGui.PushID(p.GUID);
                    if (ImGui.CollapsingHeader($"{p.Name}###phase"))
                    {
                        ImGuiEx.TextV("名稱：");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(150f);
                        ImGui.InputText($"##Name", ref p.Name, 20);
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesome.Trash) && ImGuiEx.Ctrl)
                        {
                            new TickScheduler(() => info.Phases.RemoveAll(z => z.GUID == p.GUID));
                        }
                        ImGuiEx.Tooltip("按住 Ctrl 即可刪除此階段。");
                        ImGuiEx.TextV("天氣：");
                        ImGui.SameLine();
                        if (ImGui.BeginCombo("##Weather", $"{Utils.GetWeatherName(p.Weather)}"))
                        {
                            foreach (var x in (uint[])[0, .. P.Weathers[TerrID]])
                            {
                                if (ImGui.Selectable($"{x} - {Utils.GetWeatherName(x)}"))
                                {
                                    if (P.Enabled && Svc.ClientState.TerritoryType == TerrID && Utils.GetPhase(Svc.ClientState.TerritoryType) == p)
                                    {
                                        EnvManager.Instance()->ActiveWeather = (byte)x;
                                        EnvManager.Instance()->TransitionTime = 0.5f;
                                    }
                                    p.Weather = x;
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGuiEx.TextV("地圖效果：");
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesome.Plus))
                        {
                            p.MapEffects.Add(new());
                        }
                        ImGuiEx.Tooltip("新增地圖效果。");
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                        {
                            Copy(P.YamlFactory.Serialize(p.MapEffects, true));
                        }
                        ImGuiEx.Tooltip("複製此階段設定的地圖效果。");
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                        {
                            Safe(() => p.MapEffects = P.YamlFactory.Deserialize<List<MapEffectInfo>>(Paste()));
                        }
                        ImGuiEx.Tooltip("貼上並覆寫此階段的地圖效果。");
                        foreach (var x in p.MapEffects)
                        {
                            ImGui.PushID(x.GUID);
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputInt($"##a1", ref x.a1);
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputInt($"##a2", ref x.a2);
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputInt($"##a3", ref x.a3);
                            ImGui.SameLine();
                            if (ImGui.Button("刪除###Delete"))
                            {
                                new TickScheduler(() => p.MapEffects.RemoveAll(z => z.GUID == x.GUID));
                            }
                            ImGui.PopID();
                        }
                    }
                    ImGui.PopID();
                }
                if (ImGui.Button("儲存###Save"))
                {
                    Utils.CreateZoneInfoOverride(bg, info.JSONClone(), true);
                    P.SaveZoneData();
                }
                if(isOverriden)
                {
                    if(ImGui.Button("重設###Reset"))
                    {
                        Utils.LoadBuiltInZoneData();
                        new TickScheduler(() =>
                        {
                            P.ZoneData.Data.Remove(bg);
                            P.SaveZoneData();
                        });
                    }
                }
            }
            else
            {
                ImGuiEx.Text("找不到資料");
                if (ImGui.Button("建立覆寫資料###Create override"))
                {
                    Utils.CreateZoneInfoOverride(bg, new()
                    {
                        Name = ExcelTerritoryHelper.GetName(TerrID),
                    });
                }
            }
        }
    }
}
