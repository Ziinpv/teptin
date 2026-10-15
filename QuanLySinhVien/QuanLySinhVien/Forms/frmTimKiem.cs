using System;
using System.Drawing;
using System.Windows.Forms;
using QuanLySinhVien.Models;

namespace QuanLySinhVien
{
    /// <summary>
    /// Hộp thoại nhập một hoặc nhiều điều kiện tìm kiếm (MSSV, họ tên, lớp).
    /// Ô để trống không được dùng làm điều kiện. Nhiều ô cùng lúc được kết hợp AND.
    /// </summary>
    public class frmTimKiem : Form
    {
        private readonly TextBox _txtMssv;
        private readonly TextBox _txtName;
        private readonly ComboBox _cboClass;

        public frmTimKiem()
        {
            Text = "Tìm kiếm sinh viên";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 230);
            Font = new Font("Segoe UI", 9F);

            Label hint = new Label
            {
                Left = 16,
                Top = 12,
                Width = 428,
                Height = 36,
                Text = "Nhập một hoặc nhiều điều kiện. Ô để trống sẽ được bỏ qua. Sinh viên phải thỏa tất cả điều kiện đã nhập."
            };

            Label lblMssv = new Label { Left = 16, Top = 58, Width = 90, Text = "MSSV:" };
            _txtMssv = new TextBox { Left = 120, Top = 54, Width = 320, MaxLength = 7 };

            Label lblName = new Label { Left = 16, Top = 94, Width = 90, Text = "Họ tên:" };
            _txtName = new TextBox { Left = 120, Top = 90, Width = 320, MaxLength = 80 };

            Label lblClass = new Label { Left = 16, Top = 130, Width = 90, Text = "Lớp:" };
            _cboClass = new ComboBox
            {
                Left = 120,
                Top = 126,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboClass.Items.Add("(Tất cả các lớp)");
            foreach (ClassOption option in StudentCatalog.Classes)
                _cboClass.Items.Add(option);
            _cboClass.SelectedIndex = 0;

            Button btnFind = new Button
            {
                Text = "Tìm kiếm",
                Left = 250,
                Top = 176,
                Width = 90,
                Height = 32
            };
            Button btnClose = new Button
            {
                Text = "Đóng",
                Left = 350,
                Top = 176,
                Width = 90,
                Height = 32,
                DialogResult = DialogResult.Cancel
            };

            btnFind.Click += btnFind_Click;
            AcceptButton = btnFind;
            CancelButton = btnClose;

            Controls.Add(hint);
            Controls.Add(lblMssv);
            Controls.Add(_txtMssv);
            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblClass);
            Controls.Add(_cboClass);
            Controls.Add(btnFind);
            Controls.Add(btnClose);
        }

        public string MssvKeyword { get; private set; }

        public string NameKeyword { get; private set; }

        public string ClassKeyword { get; private set; }

        private void btnFind_Click(object sender, EventArgs e)
        {
            MssvKeyword = _txtMssv.Text.Trim();
            NameKeyword = _txtName.Text.Trim();
            ClassOption selected = _cboClass.SelectedItem as ClassOption;
            ClassKeyword = selected == null ? string.Empty : selected.Code;

            if (MssvKeyword.Length == 0 && NameKeyword.Length == 0 && ClassKeyword.Length == 0)
            {
                MessageBox.Show(
                    this,
                    "Hãy nhập ít nhất một điều kiện tìm kiếm (MSSV, họ tên hoặc lớp).",
                    "Tìm kiếm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
