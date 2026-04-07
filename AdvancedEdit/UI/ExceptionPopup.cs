using Hexa.NET.ImGui;

namespace AdvEditRework.UI;

public class ExceptionPopup
{
    public bool Open = false;
    private bool _logFileCreated = false;
    private readonly string _windowName;
    private readonly Exception _ex;
    
    const uint IDMagic = 0x6ade348f;

    public ExceptionPopup(string windowName, Exception ex)
    {
        _windowName = windowName;
        _ex = ex;
        ImGuiP.PushOverrideID(IDMagic);
        ImGui.OpenPopup(windowName);
        ImGui.PopID();
        Open = true;
    }

    public void Update()
    {
        ImGuiP.PushOverrideID(IDMagic);
        if (ImGui.BeginPopupModal(_windowName, ref Open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Error Occured!");
            ImGui.Text(_ex.Message);
            if (!_logFileCreated)
            {
                CreateLogFile(_ex);
                _logFileCreated = true;
            }
            ImGui.EndPopup();
        }

        ImGui.PopID();
    }

    private static void CreateLogFile(Exception ex)
    {
        string logDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AdvancedEdit", "Logs");
        Directory.CreateDirectory(logDirPath);
        var file = Path.Combine(logDirPath, $"Log-{DateTime.Now:yyyy-MM-dd-HH:mm:ss}.txt");
        File.WriteAllText(file, ex.ToString());
    }
}