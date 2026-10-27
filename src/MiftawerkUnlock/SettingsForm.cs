// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

using KeePass.UI;

using Windows.Security.Credentials;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Settings dialog in the RamzX design: KeePass banner with the RamzX logo, a status section
    /// (what works on this computer), the settings, stored data and an about line.
    /// Built in code (no designer files).
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private readonly Settings m_settings;
        private readonly PictureBox m_banner = new PictureBox();
        private readonly NumericUpDown m_days = new NumericUpDown();
        private readonly CheckBox m_offer = new CheckBox();
        private readonly CheckBox m_lock = new CheckBox();
        private readonly ComboBox m_language = new ComboBox();
        private readonly StatusPill m_hello = new StatusPill();
        private readonly StatusPill m_passkey = new StatusPill();
        private readonly StatusPill m_usage = new StatusPill();
        private Bitmap m_logo, m_footerLogo;

        internal SettingsForm(Settings settings)
        {
            m_settings = settings;

            Text = Strings.Get(Strings.SettingsTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = SystemFonts.MessageBoxFont;
            BackColor = Color.White;
            if (Strings.RightToLeft) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }

            int fh = Font.Height, w = fh * 32;

            m_banner.Dock = DockStyle.Top;
            m_banner.Height = fh * 4;

            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoSize = true;
            f.Location = new Point(fh, m_banner.Height + fh);

            // ---- status ----
            f.Controls.Add(Section(Strings.SectionStatus, 0));
            foreach (StatusPill p in new[] { m_hello, m_passkey, m_usage })
            {
                p.Width = w;
                p.Margin = new Padding(0, fh / 3, 0, 0);
                p.Set(PillKind.Neutral, "…");
                f.Controls.Add(p);
            }
            if (SystemInformation.TerminalServerSession)
            {
                StatusPill remote = new StatusPill { Width = w, Margin = new Padding(0, fh / 3, 0, 0) };
                remote.Set(PillKind.Neutral, Strings.Get(Strings.StatusRemote));
                f.Controls.Add(remote);
            }

            // ---- settings ----
            f.Controls.Add(Section(Strings.SectionSettings, fh));

            FlowLayoutPanel days = Row();
            days.Controls.Add(Inline(Strings.Get(Strings.ValidityLabel)));
            m_days.Minimum = Settings.MinValidityDays;
            m_days.Maximum = Settings.MaxValidityDays;
            m_days.Value = settings.ValidityDays;
            m_days.Width = fh * 4;
            days.Controls.Add(m_days);
            f.Controls.Add(days);
            f.Controls.Add(Note(Strings.Get(Strings.ValidityNote), w));

            m_offer.Text = Strings.Get(Strings.OfferEnrollment);
            m_offer.Checked = settings.OfferEnrollment;
            m_lock.Text = Strings.Get(Strings.LockWithWindows);
            m_lock.Checked = settings.LockWithWindows;
            foreach (CheckBox c in new[] { m_offer, m_lock })
            {
                c.AutoSize = true;
                c.MaximumSize = new Size(w, 0);
                c.Margin = new Padding(0, fh / 2, 0, 0);
                f.Controls.Add(c);
            }

            FlowLayoutPanel lang = Row();
            lang.Margin = new Padding(0, fh / 2, 0, 0);
            lang.Controls.Add(Inline(Strings.Get(Strings.LanguageLabel)));
            m_language.DropDownStyle = ComboBoxStyle.DropDownList;
            m_language.Width = fh * 16;
            m_language.Items.Add(Strings.Get(Strings.LanguageAuto));
            foreach (string name in Strings.LanguageNames) m_language.Items.Add(name);
            int sel = Array.IndexOf(Strings.Languages, settings.Language);
            m_language.SelectedIndex = sel < 0 ? 0 : sel + 1;
            lang.Controls.Add(m_language);
            f.Controls.Add(lang);
            f.Controls.Add(Note(Strings.Get(Strings.LanguageNote), w));

            // ---- stored data ----
            f.Controls.Add(Section(Strings.SectionData, fh));
            Button forget = new Button();
            forget.Text = Strings.Get(Strings.ForgetButton);
            forget.AutoSize = true;
            forget.MinimumSize = new Size(0, fh * 2);
            forget.ForeColor = Brand.Error;
            forget.Margin = new Padding(0, fh / 3, 0, 0);
            forget.Click += OnForget;
            f.Controls.Add(forget);

            StatusPill pin = new StatusPill { Width = w, Margin = new Padding(0, fh, 0, 0) };
            pin.Set(PillKind.Warn, Strings.Get(Strings.PinNote));
            f.Controls.Add(pin);

            // ---- footer: about line + buttons ----
            TableLayoutPanel footer = new TableLayoutPanel();
            footer.ColumnCount = 2;
            footer.AutoSize = true;
            footer.Width = w;
            footer.Margin = new Padding(0, fh, 0, 0);
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            FlowLayoutPanel about = Row();
            m_footerLogo = Brand.Logo(fh + 4);
            PictureBox small = new PictureBox { Image = m_footerLogo, SizeMode = PictureBoxSizeMode.AutoSize, Margin = new Padding(0, 4, 6, 0) };
            about.Controls.Add(small);
            LinkLabel link = new LinkLabel();
            link.AutoSize = true;
            link.Text = "RamzX · ramzx.de · v" + Version();
            link.LinkArea = new LinkArea(8, 8);
            link.LinkColor = Brand.Blue;
            link.Margin = new Padding(0, 6, 0, 0);
            link.LinkClicked += delegate { OpenWebsite(); };
            about.Controls.Add(link);
            footer.Controls.Add(about, 0, 0);

            FlowLayoutPanel buttons = Row();
            Button cancel = new Button { Text = Strings.Get(Strings.Cancel), DialogResult = DialogResult.Cancel, AutoSize = true };
            Button ok = new Button { Text = Strings.Get(Strings.Ok), DialogResult = DialogResult.OK, AutoSize = true };
            cancel.MinimumSize = ok.MinimumSize = new Size(fh * 6, fh * 2);
            ok.Font = new Font(Font, FontStyle.Bold);
            ok.Click += OnOk;
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            footer.Controls.Add(buttons, 1, 0);
            f.Controls.Add(footer);

            Controls.Add(f);
            Controls.Add(m_banner);
            AcceptButton = ok;
            CancelButton = cancel;
            ClientSize = new Size(w + fh * 2, m_banner.Height + f.PreferredSize.Height + fh * 2);
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            m_logo = Brand.Logo(48);
            BannerFactory.CreateBannerEx(this, m_banner, m_logo, Ui.Title, Strings.Get(Strings.SettingsSubtitle));

            bool passkeys = WebAuthnPrf.IsAvailable();
            m_passkey.Set(passkeys ? PillKind.Ok : PillKind.Warn,
                Strings.Get(passkeys ? Strings.StatusPasskey : Strings.StatusPasskeyNo));
            UpdateUsage();

            bool hello;
            try { hello = await KeyCredentialManager.IsSupportedAsync(); }
            catch (Exception) { hello = false; }
            if (IsDisposed) return;
            // Over Remote Desktop this computer's own Windows Hello does not matter: passkeys are used.
            bool ok = hello || (SystemInformation.TerminalServerSession && passkeys);
            m_hello.Set(ok ? PillKind.Ok : PillKind.Error, Strings.Get(ok ? Strings.StatusHello : Strings.StatusHelloNo));
            PerformLayout();
        }

        private void UpdateUsage()
        {
            int dbs = 0, devices = 0;
            try
            {
                dbs = RecordStore.CountDatabases();
                devices = RecordStore.LoadPasskeyIds().Count;
            }
            catch (Exception) { }
            m_usage.Set(PillKind.Neutral, Strings.Format(Strings.StatusUsage, dbs, devices));
        }

        private void OnForget(object sender, EventArgs e)
        {
            if (!Ui.Ask(this, Strings.Get(Strings.ForgetConfirm))) return;
            try { Ui.Info(this, Strings.Format(Strings.ForgetDone, RecordStore.DeleteAll())); }
            catch (Exception ex) { Ui.Warn(this, Strings.Format(Strings.DeleteFailed, ex.Message)); }
            UpdateUsage();
        }

        private void OnOk(object sender, EventArgs e)
        {
            m_settings.ValidityDays = (int)m_days.Value;
            m_settings.OfferEnrollment = m_offer.Checked;
            m_settings.LockWithWindows = m_lock.Checked;
            m_settings.Language = m_language.SelectedIndex <= 0 ? string.Empty : Strings.Languages[m_language.SelectedIndex - 1];
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (m_logo != null) { m_logo.Dispose(); m_logo = null; }
            if (m_footerLogo != null) { m_footerLogo.Dispose(); m_footerLogo = null; }
            base.OnFormClosed(e);
        }

        private static void OpenWebsite()
        {
            try { System.Diagnostics.Process.Start(Brand.Website); }
            catch (Exception) { }
        }

        private static string Version()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        private SectionLabel Section(string key, int top)
        {
            SectionLabel s = new SectionLabel(Strings.Get(key), Font);
            s.Margin = new Padding(0, top, 0, 0);
            return s;
        }

        private static FlowLayoutPanel Row()
        {
            return new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0) };
        }

        private Label Inline(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(0, Font.Height / 3, Font.Height / 2, 0) };
        }

        private static Label Note(string text, int width)
        {
            return new Label { Text = text, AutoSize = true, MaximumSize = new Size(width, 0), ForeColor = Brand.Muted, Margin = new Padding(0, 4, 0, 0) };
        }
    }
}
