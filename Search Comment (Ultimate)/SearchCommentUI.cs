using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Tabnado.UI;

namespace SearchCommentUltimate
{
    public class SearchCommentUI
    {

        private readonly PluginConfig config;
        private readonly IDalamudPluginInterface pluginInterface;
        private bool settingsVisible = false;
        private IPluginLog log;

        public SearchCommentUI(Plugin plugin)
        {
            this.pluginInterface = plugin.PluginInterface;
            this.config = plugin.PluginConfig;
            this.log = plugin.Log;

        }

        public void ToggleVisibility()
        {
            settingsVisible = !settingsVisible;
        }

        public void Draw()
        {
            if (!settingsVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(450, 450), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Tabnado Target Settings", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                bool configChanged = false;
                bool active = config.Active;

                if (ImGui.Checkbox("Activate Search Comment (Ultimate)", ref active))
                {
                    config.Active = active;
                    configChanged = true;
                }

                if (configChanged)
                    config.Save();
            }
            ImGui.End();
        }
    }
}