using Dalamud.Interface.Components;
using ECommons.SimpleGui;
using Lumina.Excel.Sheets;

namespace Hyperborea.Gui;
public class SettingsWindow : Window
{
    public SettingsWindow() : base("Hyperborea 設定")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        if (ImGuiGroup.BeginGroupBox("一般設定"))
        {
            ImGuiEx.Text("坐騎：");
            ImGuiEx.SetNextItemFullWidth(-10);
            if (ImGui.BeginCombo($"##mount", Utils.GetMountName(C.CurrentMount) ?? "選擇坐騎……"))
            {
                ImGui.SetNextItemWidth(150f);
                ImGui.InputTextWithHint("##search", "篩選", ref UI.MountFilter, 50);
                if (ImGui.Selectable("不使用坐騎"))
                {
                    C.CurrentMount = 0;
                }
                foreach (var x in Svc.Data.GetExcelSheet<Mount>())
                {
                    var name = Utils.GetMountName(x.RowId);
                    if (!name.IsNullOrEmpty())
                    {
                        if (UI.MountFilter.IsNullOrEmpty() || name.Contains(UI.MountFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            if (ImGui.Selectable(name))
                            {
                                C.CurrentMount = x.RowId;
                            }
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGuiGroup.EndGroupBox();
        }

        if (ImGuiGroup.BeginGroupBox("危險設定", EColor.RedBright.ToUint()))
        {
            if (P.Enabled) ImGui.BeginDisabled();
            ImGui.Checkbox("停用區域鎖定", ref C.DisableInnCheck);
            ImGuiComponents.HelpMarker("移除必須位於旅館房間才能使用 Hyperborea 的限制。若在公共區域使用時封包篩選器失效，可能造成風險。");
            if (P.Enabled)
            {
                ImGui.EndDisabled();
                ImGuiEx.TextWrapped(EColor.RedBright, "插件啟用期間無法變更此設定");
            }
            ImGuiGroup.EndGroupBox();
        }

        
    }
}
