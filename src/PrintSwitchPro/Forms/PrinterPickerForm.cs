using System.Drawing.Printing;
using PrintSwitchPro.Core.Localization;

namespace PrintSwitchPro.Forms;

internal sealed class PrinterPickerForm : Form
{
    private readonly ListBox _list = new();
    private readonly Button _ok = new();
    private readonly Button _cancel = new();
    private readonly UiStrings _ui;

    public string SelectedPrinter { get; private set; } = string.Empty;

    public PrinterPickerForm(UiStrings ui, string titleContext)
    {
        _ui = ui;
        Text = string.IsNullOrWhiteSpace(titleContext)
            ? ui.ChoosePrinter
            : ui.MapPrinterFor(titleContext);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 320);
        AutoScaleMode = AutoScaleMode.Dpi;

        _list.Dock = DockStyle.Fill;
        _list.IntegralHeight = false;
        _list.DoubleClick += (_, _) => AcceptIfSelected();

        _ok.Text = ui.Ok;
        _ok.DialogResult = DialogResult.None;
        _ok.Click += (_, _) => AcceptIfSelected();
        _ok.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

        _cancel.Text = ui.Cancel;
        _cancel.DialogResult = DialogResult.Cancel;
        _cancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12) };
        _cancel.SetBounds(buttons.Width - 100, 10, 88, 28);
        _ok.SetBounds(buttons.Width - 196, 10, 88, 28);
        buttons.Controls.Add(_ok);
        buttons.Controls.Add(_cancel);
        buttons.Resize += (_, _) =>
        {
            _cancel.Left = buttons.ClientSize.Width - _cancel.Width - 12;
            _ok.Left = _cancel.Left - _ok.Width - 8;
            _ok.Top = _cancel.Top = (buttons.ClientSize.Height - _ok.Height) / 2;
        };

        Controls.Add(_list);
        Controls.Add(buttons);
        AcceptButton = _ok;
        CancelButton = _cancel;

        Load += (_, _) =>
        {
            _list.Items.Clear();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                _list.Items.Add(printer);
            }

            if (_list.Items.Count == 0)
            {
                MessageBox.Show(this, _ui.NoPrintersFound, _ui.ChoosePrinter,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
    }

    private void AcceptIfSelected()
    {
        if (_list.SelectedItem is not string name || string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, _ui.PleaseChoosePrinter, _ui.ChoosePrinter,
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        SelectedPrinter = name;
        DialogResult = DialogResult.OK;
        Close();
    }
}
