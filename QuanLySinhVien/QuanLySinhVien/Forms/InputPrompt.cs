using System.Drawing;
using System.Windows.Forms;

namespace QuanLySinhVien
{
    internal static class InputPrompt
    {
        public static bool TryGetText(IWin32Window owner, string title, string prompt, string defaultValue, out string value)
        {
            value = null;
            using (Form form = new Form())
            {
                form.Text = title;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(420, 140);
                form.Font = new Font("Segoe UI", 9F);

                Label label = new Label
                {
                    Left = 12,
                    Top = 12,
                    Width = 396,
                    Height = 36,
                    Text = prompt
                };
                TextBox textBox = new TextBox
                {
                    Left = 12,
                    Top = 52,
                    Width = 396,
                    Text = defaultValue ?? string.Empty
                };
                Button ok = new Button
                {
                    Text = "Đồng ý",
                    DialogResult = DialogResult.OK,
                    Left = 220,
                    Top = 92,
                    Width = 88,
                    Height = 30
                };
                Button cancel = new Button
                {
                    Text = "Hủy",
                    DialogResult = DialogResult.Cancel,
                    Left = 316,
                    Top = 92,
                    Width = 88,
                    Height = 30
                };

                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                form.Shown += (sender, args) =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                };

                if (form.ShowDialog(owner) != DialogResult.OK)
                    return false;

                value = textBox.Text.Trim();
                return true;
            }
        }
    }
}
