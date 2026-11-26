using System.Diagnostics;
using System.Reflection;
using PrintSwitchPro.Core.Localization;
using PrintSwitchPro.Core.Models;
using PrintSwitchPro.Core.Update;

namespace PrintSwitchPro.Forms;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly UpdateService _updater;
    private readonly Action<AppSettings> _onSaved;
    private UiStrings _ui;

    private readonly ComboBox _language = new();
    private readonly CheckBox _autostart = new();
    private readonly CheckBox _minimize = new();
    private readonly CheckBox _autoCheck = new();
    private readonly Label _versionLabel = new();
    private readonly Label _updateStatus = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _check = new();
    private readonly Button _download = new();
    private readonly Button _install = new();
    private readonly Button _save = new();
    private readonly Button _cancel = new();
    private readonly Label _langLabel = new();
    private readonly Label _updatesLabel = new();
    private readonly Label _updatesHint = new();

    private string? _availableVersion;
    private string? _downloadedVersion;
    private bool _checking;
    private bool _downloading;

    public SettingsForm(AppSettings settings, UpdateService updater, Action<AppSettings> onSaved)
    {
        _settings = settings;
        _updater = updater;
        _onSaved = onSaved;
        _ui = new UiStrings(settings.Language);

        Text = _ui.Settings;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 420);
        AutoScaleMode = AutoScaleMode.Dpi;
        Padding = new Padding(16);

        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.Items.AddRange([_ui.German, _ui.English]);
        _language.SelectedIndex = settings.Language == "en" ? 1 : 0;
        _language.SelectedIndexChanged += (_, _) =>
        {
            _ui = new UiStrings(_language.SelectedIndex == 1 ? "en" : "de");
            ApplyLanguage();
        };

        _autostart.Checked = settings.AutoStart;
        _minimize.Checked = settings.MinimizeOnStartup;
        _autoCheck.Checked = settings.AutoCheckUpdates;

        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Visible = false;

        _check.Click += async (_, _) => await CheckAsync();
        _download.Click += async (_, _) => await DownloadAsync();
        _install.Click += (_, _) => Install();
        _save.Click += (_, _) => SaveAndClose();
        _cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _updater.StatusChanged += OnUpdateStatus;

        BuildLayout();
        ApplyLanguage();
        RefreshUpdateButtons();

        FormClosed += (_, _) => _updater.StatusChanged -= OnUpdateStatus;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            AutoSize = true,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(_langLabel);
        root.Controls.Add(_language);
        root.Controls.Add(_autostart);
        root.Controls.Add(_minimize);
        root.Controls.Add(_updatesLabel);
        root.Controls.Add(_versionLabel);
        root.Controls.Add(_updatesHint);
        root.Controls.Add(_autoCheck);

        var updateButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Dock = DockStyle.Fill,
        };
        updateButtons.Controls.Add(_check);
        updateButtons.Controls.Add(_download);
        updateButtons.Controls.Add(_install);
        root.Controls.Add(updateButtons);
        root.Controls.Add(_progress);
        root.Controls.Add(_updateStatus);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(0, 8, 0, 0),
        };
        footer.Controls.Add(_cancel);
        footer.Controls.Add(_save);

        Controls.Add(root);
        Controls.Add(footer);
        AcceptButton = _save;
        CancelButton = _cancel;
    }

    private void ApplyLanguage()
    {
        Text = _ui.Settings;
        _langLabel.Text = _ui.LanguageLabel;
        _autostart.Text = _ui.Autostart;
        _minimize.Text = _ui.MinimizeOnStartup;
        _autoCheck.Text = _ui.AutoCheckUpdates;
        _updatesLabel.Text = "Updates";
        _updatesHint.Text = _ui.UpdatesHint;
        _versionLabel.Text = _ui.CurrentVersion(GetAppVersion());
        _check.Text = _ui.CheckForUpdates;
        _download.Text = _ui.Download;
        _install.Text = _ui.InstallNow;
        _save.Text = _ui.Save;
        _cancel.Text = _ui.Cancel;

        var selected = _language.SelectedIndex;
        _language.Items.Clear();
        _language.Items.AddRange([_ui.German, _ui.English]);
        _language.SelectedIndex = selected >= 0 ? selected : 0;
    }

    private static string GetAppVersion()
        => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "3.0.0";

    private void OnUpdateStatus(UpdateStatus status)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => OnUpdateStatus(status));
            return;
        }

        switch (status)
        {
            case UpdateStatus.Checking:
                _checking = true;
                _updateStatus.Text = _ui.SearchingUpdates;
                break;
            case UpdateStatus.UpdateAvailable(var version):
                _checking = false;
                _availableVersion = version;
                _downloadedVersion = null;
                _updateStatus.Text = _ui.UpdateAvailable(version);
                break;
            case UpdateStatus.UpdateNotAvailable:
                _checking = false;
                _availableVersion = null;
                _updateStatus.Text = _ui.UpToDate;
                break;
            case UpdateStatus.DownloadProgress(var percent):
                _downloading = true;
                _progress.Visible = true;
                _progress.Value = percent;
                _updateStatus.Text = _ui.DownloadProgress(percent);
                break;
            case UpdateStatus.UpdateDownloaded(var version):
                _checking = false;
                _downloading = false;
                _progress.Visible = true;
                _progress.Value = 100;
                _availableVersion = version;
                _downloadedVersion = version;
                _updateStatus.Text = _ui.UpdateReady(version);
                break;
            case UpdateStatus.Installing:
                _updateStatus.Text = _ui.Installing;
                break;
            case UpdateStatus.Error(var message):
                _checking = false;
                _downloading = false;
                _updateStatus.Text = message;
                break;
        }

        RefreshUpdateButtons();
    }

    private void RefreshUpdateButtons()
    {
        _check.Enabled = !_checking && !_downloading;
        _download.Enabled = !string.IsNullOrEmpty(_availableVersion)
            && string.IsNullOrEmpty(_downloadedVersion)
            && !_downloading;
        _download.Visible = !string.IsNullOrEmpty(_availableVersion) && string.IsNullOrEmpty(_downloadedVersion);
        _install.Visible = !string.IsNullOrEmpty(_downloadedVersion);
        _install.Enabled = !string.IsNullOrEmpty(_downloadedVersion);
    }

    private async Task CheckAsync()
    {
        _checking = true;
        _updateStatus.Text = _ui.SearchingUpdates;
        RefreshUpdateButtons();
        await _updater.CheckForUpdatesAsync();
    }

    private async Task DownloadAsync()
    {
        _downloading = true;
        _progress.Visible = true;
        _progress.Value = 0;
        _updateStatus.Text = _ui.DownloadProgress(0);
        RefreshUpdateButtons();
        await _updater.DownloadUpdateAsync();
    }

    private void Install()
    {
        if (!_updater.TryBeginInstall(out var path, out _))
        {
            return;
        }

        MessageBox.Show(this, _ui.InstallConfirmMessage, _ui.InstallConfirmTitle,
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
            Application.Exit();
        }
        catch (Exception ex)
        {
            _updater.ResetInstallLock();
            _updateStatus.Text = ex.Message;
        }
    }

    private void SaveAndClose()
    {
        _settings.Language = _language.SelectedIndex == 1 ? "en" : "de";
        _settings.AutoStart = _autostart.Checked;
        _settings.MinimizeOnStartup = _minimize.Checked;
        _settings.AutoCheckUpdates = _autoCheck.Checked;
        _onSaved(_settings);
        DialogResult = DialogResult.OK;
        Close();
    }
}
