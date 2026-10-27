// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.UI;

namespace MiftawerkUnlock
{
    /// <summary>
    /// The "Enable quick unlock?" question in the RamzX design (instead of a plain message box):
    /// banner with the RamzX logo, a face/fingerprint symbol, the question and the honest PIN note.
    /// Default button: "Not now" (nothing happens without an explicit yes).
    /// </summary>
    internal sealed class EnrollForm : Form
    {
        private readonly PictureBox m_banner = new PictureBox();
        private Bitmap m_logo, m_symbol;

        internal EnrollForm(string question)
        {
            Text = Ui.Title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = SystemFonts.MessageBoxFont;
            BackColor = Color.White;
            if (Strings.RightToLeft) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }

            int fh = Font.Height, w = fh * 30;
            m_banner.Dock = DockStyle.Top;
            m_banner.Height = fh * 4;

            // The question text without its last paragraph (the PIN note), which gets its own pill.
            string body = question, note = null;
            int cut = question.LastIndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (cut > 0) { body = question.Substring(0, cut); note = question.Substring(cut + 4); }

            TableLayoutPanel t = new TableLayoutPanel();
            t.ColumnCount = 2;
            t.AutoSize = true;
            t.Location = new Point(fh, m_banner.Height + fh);

            m_symbol = Brand.HelloSymbol(fh * 4);
            PictureBox symbol = new PictureBox { Image = m_symbol, SizeMode = PictureBoxSizeMode.AutoSize, Margin = new Padding(0, 0, fh, 0) };
            t.Controls.Add(symbol, 0, 0);

            Label text = new Label { Text = body, AutoSize = true, MaximumSize = new Size(w - fh * 5, 0), ForeColor = Brand.Text };
            t.Controls.Add(text, 1, 0);

            if (note != null)
            {
                StatusPill pill = new StatusPill { Width = w, Margin = new Padding(0, fh, 0, 0) };
                pill.Set(PillKind.Warn, note);
                t.Controls.Add(pill, 0, 1);
                t.SetColumnSpan(pill, 2);
            }

            FlowLayoutPanel buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, fh, 0, 0) };
            Button yes = new Button { Text = Strings.Get(Strings.EnrollEnable), DialogResult = DialogResult.Yes, AutoSize = true };
            Button no = new Button { Text = Strings.Get(Strings.EnrollNotNow), DialogResult = DialogResult.No, AutoSize = true };
            yes.MinimumSize = no.MinimumSize = new Size(fh * 7, fh * 2);
            yes.Font = new Font(Font, FontStyle.Bold);
            yes.BackColor = Brand.Blue;
            yes.ForeColor = Color.White;
            yes.FlatStyle = FlatStyle.Flat;
            yes.FlatAppearance.BorderColor = Brand.Blue;
            buttons.Controls.Add(yes);
            buttons.Controls.Add(no);
            t.Controls.Add(buttons, 0, 2);
            t.SetColumnSpan(buttons, 2);

            Controls.Add(t);
            Controls.Add(m_banner);
            AcceptButton = no;  // Enter = "Not now": enabling needs a deliberate click
            CancelButton = no;
            ClientSize = new Size(w + fh * 2, m_banner.Height + t.PreferredSize.Height + fh * 2);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            m_logo = Brand.Logo(48);
            BannerFactory.CreateBannerEx(this, m_banner, m_logo, Ui.Title, Strings.Get(Strings.EnrollTitle));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (m_logo != null) { m_logo.Dispose(); m_logo = null; }
            if (m_symbol != null) { m_symbol.Dispose(); m_symbol = null; }
            base.OnFormClosed(e);
        }

        /// <summary>Shows the question; true only for an explicit "Enable".</summary>
        internal static bool Ask(IWin32Window owner, string question)
        {
            using (EnrollForm f = new EnrollForm(question))
                return f.ShowDialog(owner) == DialogResult.Yes;
        }
    }
}
