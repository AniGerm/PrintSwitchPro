using System.Diagnostics;
using System.Reflection;
using PrintSwitchPro.Core.Localization;
using PrintSwitchPro.Core.Models;
using PrintSwitchPro.Core.Services;
using PrintSwitchPro.Core.Storage;
using PrintSwitchPro.Core.Update;
using PrintSwitchPro.Windows;

namespace PrintSwitchPro.Forms;

internal sealed class MainForm : Form
{
    private readonly SettingsStore _store = new();
    private readonly PrinterSwitchService _switcher = new();
    private readonly HttpClient _http = CreateHttpClient();
    private readonly bool _startMinimized;

    private AppSettings _settings;
    private UiStrings _ui;
    private UpdateService _updater = null!;
    private System.Windows.Forms.Timer _pollTimer = null!;
    private System.Windows.Forms.Timer _updateTimer = null!;
    private NotifyIcon _tray = null!;
    private ListView _list = null!;
    private StatusStrip _status = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStrip _toolbar = null!;
    private ContextMenuStrip _listMenu = null!;
    private ContextMenuStrip _trayMenu = null!;
    private bool _allowClose;
    private Font? _mappedFont;

    public MainForm(bool startMinimized)
    {
        _startMinimized = startMinimized;
        _settings = _store.Load();
        _ui = new UiStrings(_settings.Language);

        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 480);
        Size = new Size(900, 600);
        try
        {
            Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        }
        catch
        {
            // Icon optional in dev.
        }

        BuildUi();
        WireUpdater();
        ApplyLanguage();
        RefreshProcessList();

        Load += OnLoad;
        FormClosing += OnFormClosing;
        Resize += OnResize;
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("PrintSwitchPro/3.0");
        return http;
    }

    private void BuildUi()
    {
        _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        _toolbar.Items.Add(new ToolStripButton(_ui.Refresh, null, (_, _) => RefreshProcessList()) { Name = "refresh" });
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(new ToolStripButton(_ui.SetDefaultPrinter, null, (_, _) => SetDefaultPrinter()) { Name = "defaultPrinter" });
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(new ToolStripButton(_ui.Settings, null, (_, _) => OpenSettings()) { Name = "settings" });
        _toolbar.Items.Add(new ToolStripButton(_ui.About, null, (_, _) => ShowAbout()) { Name = "about" });

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
        };
        _list.Columns.Add(_ui.Processes, 280);
        _list.Columns.Add(_ui.Printer, 400);
        _list.DoubleClick += (_, _) => EditSelectedMapping();

        _listMenu = new ContextMenuStrip();
        _listMenu.Items.Add(_ui.EditMapping, null, (_, _) => EditSelectedMapping());
        _listMenu.Items.Add(_ui.RemoveMapping, null, (_, _) => RemoveSelectedMapping());
        _list.ContextMenuStrip = _listMenu;
        _listMenu.Opening += (_, e) =>
        {
            if (_list.SelectedItems.Count == 0)
            {
                e.Cancel = true;
            }
        };

        _status = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel(string.Empty) { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        var github = new ToolStripStatusLabel("GitHub")
        {
            IsLink = true,
            Spring = false,
        };
        github.Click += (_, _) => OpenUrl("https://github.com/AniGerm/PrintSwitchPro");
        _status.Items.Add(_statusLabel);
        _status.Items.Add(github);

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add(_ui.Restore, null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add(_ui.Settings, null, (_, _) => OpenSettings());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(_ui.Quit, null, (_, _) => QuitApp());

        _tray = new NotifyIcon
        {
            Visible = true,
            Text = _ui.AppName,
            ContextMenuStrip = _trayMenu,
        };
        try
        {
            _tray.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico"));
        }
        catch
        {
            _tray.Icon = SystemIcons.Application;
        }

        _tray.DoubleClick += (_, _) => RestoreFromTray();

        _pollTimer = new System.Windows.Forms.Timer { Interval = Math.Clamp(_settings.PollIntervalMs, 500, 10_000) };
        _pollTimer.Tick += (_, _) => PollForeground();

        _updateTimer = new System.Windows.Forms.Timer { Interval = (int)UpdateService.CheckInterval.TotalMilliseconds };
        _updateTimer.Tick += async (_, _) =>
        {
            if (_settings.AutoCheckUpdates)
            {
                await _updater.CheckForUpdatesAsync();
            }
        };

        Controls.Add(_list);
        Controls.Add(_toolbar);
        Controls.Add(_status);
        _mappedFont = new Font(_list.Font, FontStyle.Bold);
    }

    private void WireUpdater()
    {
        var updatesDir = Path.Combine(_store.AppDataDirectory, "updates");
        var client = new GitHubReleaseClient(_http);
        _updater = new UpdateService(
            client,
            () => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "3.0.0",
            updatesDir,
            () =>
            {
                var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PrintSwitchPro/3.0");
                return http;
            });
        _updater.StatusChanged += status =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(() =>
            {
                _statusLabel.Text = status switch
                {
                    UpdateStatus.Checking => _ui.SearchingUpdates,
                    UpdateStatus.UpdateAvailable(var v) => _ui.UpdateAvailable(v),
                    UpdateStatus.UpdateNotAvailable => _ui.UpToDate,
                    UpdateStatus.DownloadProgress(var p) => _ui.DownloadProgress(p),
                    UpdateStatus.UpdateDownloaded(var v) => _ui.UpdateReady(v),
                    UpdateStatus.Installing => _ui.Installing,
                    UpdateStatus.Error(var m) => m,
                    _ => _statusLabel.Text,
                };
            });
        };
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        if (_startMinimized || _settings.MinimizeOnStartup)
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Hide();
        }

        AutostartService.SetEnabled(_settings.AutoStart, Application.ExecutablePath);
        _pollTimer.Start();

        if (_settings.AutoCheckUpdates)
        {
            await Task.Delay(UpdateService.StartDelay);
            if (!IsDisposed)
            {
                await _updater.CheckForUpdatesAsync();
                _updateTimer.Start();
            }
        }
    }

    private void ApplyLanguage()
    {
        _ui = new UiStrings(_settings.Language);
        Text = $"{_ui.MainTitle} v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
        _tray.Text = _ui.AppName;

        if (_toolbar.Items["refresh"] is ToolStripButton refresh)
        {
            refresh.Text = _ui.Refresh;
        }

        if (_toolbar.Items["defaultPrinter"] is ToolStripButton def)
        {
            def.Text = _ui.SetDefaultPrinter;
        }

        if (_toolbar.Items["settings"] is ToolStripButton settings)
        {
            settings.Text = _ui.Settings;
        }

        if (_toolbar.Items["about"] is ToolStripButton about)
        {
            about.Text = _ui.About;
        }

        if (_list.Columns.Count >= 2)
        {
            _list.Columns[0].Text = _ui.Processes;
            _list.Columns[1].Text = _ui.Printer;
        }

        _listMenu.Items.Clear();
        _listMenu.Items.Add(_ui.EditMapping, null, (_, _) => EditSelectedMapping());
        _listMenu.Items.Add(_ui.RemoveMapping, null, (_, _) => RemoveSelectedMapping());

        _trayMenu.Items.Clear();
        _trayMenu.Items.Add(_ui.Restore, null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add(_ui.Settings, null, (_, _) => OpenSettings());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(_ui.Quit, null, (_, _) => QuitApp());
    }

    private void RefreshProcessList()
    {
        var selected = _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Text : null;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var name in ProcessScanner.GetProcessesWithWindows())
        {
            var mapping = _settings.Mappings.FirstOrDefault(m =>
                string.Equals(m.ProcessName, name, StringComparison.OrdinalIgnoreCase));
            var item = new ListViewItem(name);
            item.SubItems.Add(mapping?.MappedPrinter ?? string.Empty);
            if (mapping is not null && _mappedFont is not null)
            {
                item.Font = _mappedFont;
            }

            _list.Items.Add(item);
        }

        _list.EndUpdate();
        if (selected is not null)
        {
            foreach (ListViewItem item in _list.Items)
            {
                if (string.Equals(item.Text, selected, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }
    }

    private void EditSelectedMapping()
    {
        if (_list.SelectedItems.Count == 0)
        {
            return;
        }

        var processName = _list.SelectedItems[0].Text;
        using var picker = new PrinterPickerForm(_ui, processName);
        if (picker.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var existing = _settings.Mappings.FirstOrDefault(m =>
            string.Equals(m.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            _settings.Mappings.Add(new ProcessMapping
            {
                ProcessName = processName,
                MappedPrinter = picker.SelectedPrinter,
            });
        }
        else
        {
            existing.MappedPrinter = picker.SelectedPrinter;
        }

        PersistSettings();
        RefreshProcessList();
    }

    private void RemoveSelectedMapping()
    {
        if (_list.SelectedItems.Count == 0)
        {
            return;
        }

        var processName = _list.SelectedItems[0].Text;
        if (MessageBox.Show(this, _ui.ConfirmRemoveMapping(processName), _ui.RemoveMappingTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _settings.Mappings.RemoveAll(m =>
            string.Equals(m.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
        PersistSettings();
        _switcher.Reset();
        RefreshProcessList();
    }

    private void SetDefaultPrinter()
    {
        using var picker = new PrinterPickerForm(_ui, string.Empty);
        if (picker.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings.DefaultPrinter = picker.SelectedPrinter;
        PersistSettings();
        _statusLabel.Text = _ui.StatusDefaultChanged(picker.SelectedPrinter);
        if (!string.IsNullOrWhiteSpace(picker.SelectedPrinter))
        {
            NativeMethods.SetDefaultPrinter(picker.SelectedPrinter);
        }
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings, _updater, saved =>
        {
            _settings = saved;
            PersistSettings();
            AutostartService.SetEnabled(_settings.AutoStart, Application.ExecutablePath);
            _pollTimer.Interval = Math.Clamp(_settings.PollIntervalMs, 500, 10_000);
            ApplyLanguage();
            if (_settings.AutoCheckUpdates && !_updateTimer.Enabled)
            {
                _updateTimer.Start();
            }
            else if (!_settings.AutoCheckUpdates)
            {
                _updateTimer.Stop();
            }
        });
        form.ShowDialog(this);
    }

    private void ShowAbout()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "3.0.0";
        MessageBox.Show(
            this,
            $"{_ui.AppName} {version}\nWindows 10/11\n\nhttps://github.com/AniGerm/PrintSwitchPro",
            _ui.About,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void PollForeground()
    {
        try
        {
            var processName = ProcessScanner.GetForegroundProcessName();
            if (string.IsNullOrWhiteSpace(processName))
            {
                return;
            }

            var printer = _switcher.ResolvePrinterToApply(processName, _settings);
            if (printer is null)
            {
                return;
            }

            if (NativeMethods.SetDefaultPrinter(printer))
            {
                _statusLabel.Text = _ui.StatusSwitched(printer);
            }
        }
        catch
        {
            // Foreground process may exit between query and apply.
        }
    }

    private void PersistSettings()
    {
        try
        {
            _store.Save(_settings);
        }
        catch
        {
            MessageBox.Show(this, _ui.SaveFailed, _ui.Settings, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose || e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            return;
        }

        e.Cancel = true;
        WindowState = FormWindowState.Minimized;
        Hide();
        ShowInTaskbar = false;
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            ShowInTaskbar = false;
        }
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void QuitApp()
    {
        _allowClose = true;
        PersistSettings();
        _pollTimer.Stop();
        _updateTimer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _mappedFont?.Dispose();
        _http.Dispose();
        Close();
        Application.Exit();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer?.Dispose();
            _updateTimer?.Dispose();
            _tray?.Dispose();
            _mappedFont?.Dispose();
            _http?.Dispose();
        }

        base.Dispose(disposing);
    }
}
