// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace MiftawerkUnlock
{
    /// <summary>Settings dialog, built in code (no designer files).</summary>
    internal sealed class SettingsForm : Form
    {
        private readonly Settings m_settings;
        private readonly NumericUpDown m_days = new NumericUpDown();
        private readonly CheckBox m_offer = new CheckBox();
        private readonly CheckBox m_lock = new CheckBox();

        internal SettingsForm(Settings settings)
        {
            m_settings = settings;

            Text = Strings.Get(Strings.SettingsTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);
            if (Strings.RightToLeft)
            {
                RightToLeft = RightToLeft.Yes;
                RightToLeftLayout = true;
            }

            TableLayoutPanel t = new TableLayoutPanel();
            t.AutoSize = true;
            t.ColumnCount = 2;
            t.Dock = DockStyle.Fill;

            Label lblDays = new Label();
            lblDays.Text = Strings.Get(Strings.ValidityLabel);
            lblDays.AutoSize = true;
            lblDays.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblDays.Margin = new Padding(0, 6, 8, 0);
            t.Controls.Add(lblDays, 0, 0);

            m_days.Minimum = Settings.MinValidityDays;
            m_days.Maximum = Settings.MaxValidityDays;
            m_days.Value = settings.ValidityDays;
            m_days.Width = 70;
            t.Controls.Add(m_days, 1, 0);

            t.Controls.Add(Note(Strings.Get(Strings.ValidityNote)), 0, 1);
            t.SetColumnSpan(t.GetControlFromPosition(0, 1), 2);

            m_offer.Text = Strings.Get(Strings.OfferEnrollment);
            m_offer.AutoSize = true;
            m_offer.Checked = settings.OfferEnrollment;
            m_offer.Margin = new Padding(0, 12, 0, 0);
            t.Controls.Add(m_offer, 0, 2);
            t.SetColumnSpan(m_offer, 2);

            m_lock.Text = Strings.Get(Strings.LockWithWindows);
            m_lock.AutoSize = true;
            m_lock.Checked = settings.LockWithWindows;
            t.Controls.Add(m_lock, 0, 3);
            t.SetColumnSpan(m_lock, 2);

            Label pin = Note(Strings.Get(Strings.PinNote));
            pin.Margin = new Padding(0, 12, 0, 0);
            t.Controls.Add(pin, 0, 4);
            t.SetColumnSpan(pin, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, 12, 0, 0);

            Button cancel = new Button();
            cancel.Text = Strings.Get(Strings.Cancel);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.AutoSize = true;
            Button ok = new Button();
            ok.Text = Strings.Get(Strings.Ok);
            ok.DialogResult = DialogResult.OK;
            ok.AutoSize = true;
            ok.Click += OnOk;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            t.Controls.Add(buttons, 0, 5);
            t.SetColumnSpan(buttons, 2);

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(t);
        }

        private static Label Note(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.MaximumSize = new Size(440, 0);
            l.ForeColor = SystemColors.GrayText;
            return l;
        }

        private void OnOk(object sender, EventArgs e)
        {
            m_settings.ValidityDays = (int)m_days.Value;
            m_settings.OfferEnrollment = m_offer.Checked;
            m_settings.LockWithWindows = m_lock.Checked;
        }
    }
}
