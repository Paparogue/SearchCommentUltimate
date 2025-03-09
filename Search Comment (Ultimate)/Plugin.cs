using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Runtime.InteropServices;
using Tabnado.UI;

namespace SearchCommentUltimate
{
    public unsafe class Plugin : IDalamudPlugin
    {
        public string Name => "Search Comment (Ultimate)";

        [PluginService]
        public IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService]
        public ICommandManager CommandManager { get; set; } = null!;
        [PluginService]
        public IPluginLog Log { get; set; } = null!;
        [PluginService]
        public ISigScanner SigScanner { get; set; } = null!;
        [PluginService]
        public IClientState ClientState { get; set; } = null!;

        public PluginConfig PluginConfig;
        private IntPtr _originalInstructionAddress;
        private byte[] _originalBytes;
        public SearchCommentUI SCUI;
        private bool _isPatchApplied = false;

        #pragma warning disable CS8618
        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        #pragma warning restore CS8618
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            PluginConfig = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            PluginConfig.Initialize(PluginInterface);
            SCUI = new SearchCommentUI(this);

            CommandManager.AddHandler("/scu", new CommandInfo(OnToggleUI)
            {
                HelpMessage = "Toggles the Search Comment (Ultimate) settings window."
            });
            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenMainUi += OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi += OnToggleUI;
        }

        private void SearchCommentUnlocker()
        {
            try
            {
                string signature = "44 8B 81 84 0C 00 00 4C 8D A1 ?? ?? ?? ?? 4C 89 7C 24 30 48 8D 81 ?? ?? ?? ?? 4C 89 64 24 28";
                _originalInstructionAddress = SigScanner.ScanText(signature);

                if (_originalInstructionAddress != IntPtr.Zero)
                {
                    byte[] patchBytes = new byte[] { 0x41, 0xB8, 0xC0, 0x00, 0x00, 0x00, 0x90 };

                    _originalBytes = new byte[patchBytes.Length];
                    Marshal.Copy(_originalInstructionAddress, _originalBytes, 0, _originalBytes.Length);

                    SafeMemory.WriteBytes(_originalInstructionAddress, patchBytes);

                    Log.Information($"Successfully patched instruction at 0x{_originalInstructionAddress.ToInt64():X}");

                    _isPatchApplied = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying Search Comment (Ultimate) patch");
            }
        }

        private void OnToggleUI(string command, string args)
        {
            SCUI.ToggleVisibility();
        }

        private void OnToggleUI()
        {
            OnToggleUI(null!, null!);
        }

        private void OnDraw()
        {;
            if(ClientState is not null && ClientState.LocalPlayer is not null) {
                SCUI.Draw();
                if (_isPatchApplied && !PluginConfig.Active)
                    RestoreOriginal();

                if (PluginConfig.Active && !_isPatchApplied)
                {
                    SearchCommentUnlocker();
                    _isPatchApplied = true;
                }
            }
        }

        private void RestoreOriginal()
        {
            if (_originalInstructionAddress != IntPtr.Zero && _originalBytes != null)
            {
                try
                {
                    SafeMemory.WriteBytes(_originalInstructionAddress, _originalBytes);
                    Log.Information("Restored original instruction bytes");
                    _isPatchApplied = false;
                    _originalInstructionAddress = IntPtr.Zero;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error restoring original instruction bytes");
                }
            }
        }

        public void Dispose()
        {
            RestoreOriginal();
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenMainUi -= OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OnToggleUI;
        }
    }
}
