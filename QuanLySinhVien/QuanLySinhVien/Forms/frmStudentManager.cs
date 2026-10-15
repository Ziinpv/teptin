using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QuanLySinhVien.Business;
using QuanLySinhVien.Data;
using QuanLySinhVien.Models;

namespace QuanLySinhVien
{
    public partial class frmStudentManager : Form
    {
        private readonly StudentManager _manager = new StudentManager();
        private readonly StudentRepository _repository = new StudentRepository();
        private readonly ToolTip _toolTip = new ToolTip();

        private string _currentFilePath;
        private string _originalMssv;
        private bool _autoSaveEnabled;
        private bool _exitConfirmed;
        private bool _isFiltered;
        private bool _suppressSelection;

        /// <summary>
        /// Bật khi chạy kiểm tra tự động để không dừng ở MessageBox.
        /// </summary>
        internal static bool AutomationMode { get; set; }

        public frmStudentManager()
        {
            InitializeComponent();
            dtpDob.Value = new DateTime(2004, 1, 1);
            InitializeCatalogs();
            _toolTip.SetToolTip(txtMssv,
                "7 chữ số dạng AABBCCC. AA lấy từ lớp (K22... → 22), BB = 10, CCC là số thứ tự.");
            _toolTip.SetToolTip(clbSubjects, "Nhấp chuột phải để thêm hoặc xóa môn.");
            _toolTip.SetToolTip(lvStudents, "Đánh dấu ô chọn để xóa nhiều sinh viên. Chuột phải để mở menu xóa.");
        }

        private void InitializeCatalogs()
        {
            cboClass.Items.Clear();
            foreach (ClassOption option in StudentCatalog.Classes)
                cboClass.Items.Add(option);

            clbSubjects.Items.Clear();
            foreach (string subject in StudentCatalog.Subjects)
                clbSubjects.Items.Add(subject);
        }

        /// <summary>
        /// Khi mở form: đọc tập tin mặc định trong App.config (students.json cạnh file exe).
        /// </summary>
        private void frmStudentManager_Load(object sender, EventArgs e)
        {
            _currentFilePath = GetDefaultFilePath();
            SelectFormatByPath(_currentFilePath);

            if (!File.Exists(_currentFilePath))
            {
                _autoSaveEnabled = true;
                UpdateStatusBar();
                Notify(
                    "Không tìm thấy tập tin mặc định:\n" + _currentFilePath
                    + "\n\nChương trình bắt đầu với danh sách trống. Dữ liệu sẽ được tạo khi lưu hoặc khi thoát.",
                    "Không tìm thấy tập tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<Student> students = _repository.Load(_currentFilePath);
                _manager.ReplaceAll(students);
                _autoSaveEnabled = true;
                _isFiltered = false;
                DisplayStudents(_manager.Students, null);
                WarnDuplicateMssv();
            }
            catch (Exception ex)
            {
                // File hỏng: không bật tự lưu, tránh ghi đè file gốc bằng danh sách rỗng.
                _autoSaveEnabled = false;
                _currentFilePath = null;
                UpdateStatusBar();
                Notify(
                    "Không đọc được tập tin mặc định. Danh sách được để trống và chương trình sẽ không ghi đè file này khi thoát.\n\n"
                    + GetFriendlyMessage(ex),
                    "Tập tin không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Khi đóng form: hỏi xác nhận, rồi ghi lại đúng tập tin đang mở.
        /// </summary>
        private void frmStudentManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_exitConfirmed)
            {
                DialogResult answer = Notify(
                    ExitConfirmMessage(),
                    "Xác nhận thoát",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                _exitConfirmed = true;
            }

            if (!_autoSaveEnabled || string.IsNullOrWhiteSpace(_currentFilePath))
                return;

            try
            {
                SaveCurrentFile();
            }
            catch (Exception ex)
            {
                DialogResult keepGoing = Notify(
                    "Không lưu được dữ liệu trước khi thoát.\n" + GetFriendlyMessage(ex)
                    + "\n\nBạn vẫn muốn thoát và bỏ qua bản lưu này?",
                    "Lỗi lưu tập tin",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (keepGoing != DialogResult.Yes)
                {
                    _exitConfirmed = false;
                    e.Cancel = true;
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            DialogResult answer = Notify(
                ExitConfirmMessage(),
                "Xác nhận thoát",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            _exitConfirmed = true;
            Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Student student;
            string error;
            if (!TryReadStudentFromForm(out student, out error))
            {
                Notify(error, "Thiếu hoặc sai thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_manager.Add(student, out error))
            {
                Notify(error, "Không thêm được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isFiltered = false;
            DisplayStudents(_manager.Students, null);
            ClearInputs();
            PersistAfterChange();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_originalMssv))
            {
                Notify(
                    "Hãy chọn một sinh viên trong danh sách trước khi cập nhật.",
                    "Cập nhật",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Student student;
            string error;
            if (!TryReadStudentFromForm(out student, out error))
            {
                Notify(error, "Thiếu hoặc sai thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_manager.Update(_originalMssv, student, out error))
            {
                Notify(error, "Không cập nhật được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string updatedId = student.MSSV;
            _originalMssv = updatedId;
            _isFiltered = false;
            DisplayStudents(_manager.Students, updatedId);
            PersistAfterChange();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            List<string> ids = GetCheckedIds();
            if (ids.Count == 0)
                ids = GetSelectedIds();

            DeleteByIds(ids, "Hãy chọn hoặc đánh dấu sinh viên cần xóa.");
        }

        private void btnShowAll_Click(object sender, EventArgs e)
        {
            _isFiltered = false;
            DisplayStudents(_manager.Students, _originalMssv);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            using (frmTimKiem dialog = new frmTimKiem())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                List<Student> found = _manager.Search(dialog.MssvKeyword, dialog.NameKeyword, dialog.ClassKeyword);
                _isFiltered = true;
                DisplayStudents(found, null);
                if (found.Count == 0)
                {
                    Notify(
                        "Không tìm thấy sinh viên phù hợp với điều kiện đã nhập.",
                        "Tìm kiếm",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Mở danh sách sinh viên";
                dialog.Filter = "Tất cả (*.txt;*.xml;*.json)|*.txt;*.xml;*.json|Văn bản (*.txt)|*.txt|XML (*.xml)|*.xml|JSON (*.json)|*.json";
                dialog.FilterIndex = FilterIndexOf(SelectedFormat);
                dialog.InitialDirectory = InitialDirectory();
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    List<Student> students = _repository.Load(dialog.FileName);
                    _manager.ReplaceAll(students);
                    _currentFilePath = dialog.FileName;
                    _autoSaveEnabled = true;
                    _isFiltered = false;
                    SelectFormatByPath(_currentFilePath);
                    ClearInputs();
                    DisplayStudents(_manager.Students, null);
                    WarnDuplicateMssv();
                }
                catch (Exception ex)
                {
                    Notify(GetFriendlyMessage(ex), "Không mở được tập tin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                DataFormat format = SelectedFormat;
                dialog.Title = "Lưu danh sách sinh viên";
                dialog.Filter = "Văn bản (*.txt)|*.txt|XML (*.xml)|*.xml|JSON (*.json)|*.json";
                dialog.FilterIndex = format == DataFormat.Text ? 1 : format == DataFormat.Xml ? 2 : 3;
                dialog.DefaultExt = StudentRepository.GetExtension(format).TrimStart('.');
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                dialog.InitialDirectory = InitialDirectory();
                dialog.FileName = "students" + StudentRepository.GetExtension(format);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    string path = EnsureKnownExtension(dialog.FileName);
                    _repository.Save(path, _manager.Students.ToList());
                    _currentFilePath = path;
                    _autoSaveEnabled = true;
                    SelectFormatByPath(path);
                    UpdateStatusBar();
                    Notify("Đã lưu danh sách sinh viên vào:\n" + path, "Lưu tập tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Notify(GetFriendlyMessage(ex), "Không lưu được tập tin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void lvStudents_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSelection || lvStudents.SelectedItems.Count == 0)
                return;

            string mssv = lvStudents.SelectedItems[0].Tag as string;
            Student student = _manager.FindByMssv(mssv);
            if (student != null)
                BindStudentToForm(student);
        }

        private void lvStudents_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ListViewItem item = lvStudents.GetItemAt(e.X, e.Y);
            if (item == null)
                return;

            item.Selected = true;
            item.Focused = true;
        }

        private void clbSubjects_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int index = clbSubjects.IndexFromPoint(e.Location);
            if (index >= 0)
                clbSubjects.SelectedIndex = index;
        }

        private void mnuAddSubject_Click(object sender, EventArgs e)
        {
            string name;
            if (!InputPrompt.TryGetText(this, "Thêm môn học", "Nhập tên môn học cần thêm:", string.Empty, out name))
                return;

            if (string.IsNullOrWhiteSpace(name))
            {
                Notify("Tên môn không được để trống.", "Thêm môn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int existing = IndexOfSubject(name);
            if (existing >= 0)
            {
                clbSubjects.SetItemChecked(existing, true);
                return;
            }

            clbSubjects.Items.Add(name.Trim(), true);
        }

        private void mnuRemoveSubject_Click(object sender, EventArgs e)
        {
            if (clbSubjects.SelectedIndex < 0)
            {
                Notify("Hãy chọn môn cần xóa trong danh sách môn.", "Xóa môn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            clbSubjects.Items.RemoveAt(clbSubjects.SelectedIndex);
        }

        private void mnuDeleteSelected_Click(object sender, EventArgs e)
        {
            DeleteByIds(GetSelectedIds(), "Hãy chọn sinh viên cần xóa.");
        }

        private void mnuDeleteChecked_Click(object sender, EventArgs e)
        {
            DeleteByIds(GetCheckedIds(), "Hãy đánh dấu các sinh viên cần xóa.");
        }

        private void DigitsOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void DeleteByIds(List<string> ids, string emptyMessage)
        {
            if (ids == null || ids.Count == 0)
            {
                Notify(emptyMessage, "Xóa sinh viên", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult answer = Notify(
                "Bạn có chắc muốn xóa " + ids.Count + " sinh viên đã chọn?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            int removed = _manager.DeleteMany(ids);
            if (removed == 0)
            {
                Notify("Không xóa được sinh viên nào.", "Xóa sinh viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_originalMssv != null && ids.Any(id => string.Equals(id, _originalMssv, StringComparison.OrdinalIgnoreCase)))
                ClearInputs();

            _isFiltered = false;
            DisplayStudents(_manager.Students, null);
            PersistAfterChange();
        }

        private bool TryReadStudentFromForm(out Student student, out string error)
        {
            student = new Student
            {
                MSSV = txtMssv.Text.Trim(),
                LastName = txtLastName.Text.Trim(),
                FirstName = txtFirstName.Text.Trim(),
                DateOfBirth = dtpDob.Value.Date,
                Gender = rdMale.Checked,
                CitizenId = txtCitizenId.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Address = txtAddress.Text.Trim(),
                Subjects = new List<string>()
            };

            ClassOption selectedClass = cboClass.SelectedItem as ClassOption;
            if (selectedClass == null)
            {
                error = "Hãy chọn lớp.";
                cboClass.Focus();
                return false;
            }

            student.ClassName = selectedClass.Code;
            foreach (object item in clbSubjects.CheckedItems)
                student.Subjects.Add(item.ToString());

            if (!StudentValidator.TryValidate(student, out error))
            {
                FocusInvalidField(error);
                return false;
            }

            return true;
        }

        private void FocusInvalidField(string error)
        {
            if (error == null)
                return;

            if (error.Contains("MSSV"))
                txtMssv.Focus();
            else if (error.Contains("họ và tên lót"))
                txtLastName.Focus();
            else if (error.Contains("nhập tên"))
                txtFirstName.Focus();
            else if (error.Contains("CMND"))
                txtCitizenId.Focus();
            else if (error.Contains("điện thoại"))
                txtPhone.Focus();
            else if (error.Contains("Email"))
                txtEmail.Focus();
            else if (error.Contains("địa chỉ"))
                txtAddress.Focus();
            else if (error.Contains("môn"))
                clbSubjects.Focus();
            else if (error.Contains("lớp") || error.Contains("Lớp"))
                cboClass.Focus();
        }

        private void BindStudentToForm(Student student)
        {
            _originalMssv = student.MSSV;
            txtMssv.Text = student.MSSV;
            txtLastName.Text = student.LastName;
            txtFirstName.Text = student.FirstName;
            rdMale.Checked = student.Gender;
            rdFemale.Checked = !student.Gender;

            DateTime dob = student.DateOfBirth;
            if (dob < dtpDob.MinDate || dob > dtpDob.MaxDate)
                dob = dtpDob.MinDate;
            dtpDob.Value = dob;

            SelectClass(student.ClassName);
            txtCitizenId.Text = student.CitizenId;
            txtPhone.Text = student.Phone;
            txtEmail.Text = student.Email;
            txtAddress.Text = student.Address;

            for (int i = 0; i < clbSubjects.Items.Count; i++)
                clbSubjects.SetItemChecked(i, false);

            if (student.Subjects == null)
                return;

            foreach (string subject in student.Subjects)
            {
                int index = IndexOfSubject(subject);
                if (index < 0)
                    clbSubjects.Items.Add(subject, true);
                else
                    clbSubjects.SetItemChecked(index, true);
            }
        }

        private void SelectClass(string className)
        {
            for (int i = 0; i < cboClass.Items.Count; i++)
            {
                ClassOption option = cboClass.Items[i] as ClassOption;
                if (option != null && string.Equals(option.Code, className, StringComparison.OrdinalIgnoreCase))
                {
                    cboClass.SelectedIndex = i;
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(className))
            {
                ClassOption extra = new ClassOption(className, "Lớp đọc từ tập tin");
                cboClass.Items.Add(extra);
                cboClass.SelectedItem = extra;
            }
            else
            {
                cboClass.SelectedIndex = -1;
            }
        }

        private int IndexOfSubject(string name)
        {
            for (int i = 0; i < clbSubjects.Items.Count; i++)
            {
                if (string.Equals(clbSubjects.Items[i].ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private void DisplayStudents(IEnumerable<Student> students, string reselectMssv)
        {
            _suppressSelection = true;
            lvStudents.BeginUpdate();
            lvStudents.Items.Clear();

            List<Student> ordered = (students ?? new List<Student>()).OrderBy(s => s.MSSV).ToList();
            ListViewItem itemToSelect = null;
            foreach (Student student in ordered)
            {
                ListViewItem item = new ListViewItem(student.MSSV);
                item.Tag = student.MSSV;
                item.SubItems.Add(student.LastName);
                item.SubItems.Add(student.FirstName);
                item.SubItems.Add(student.GenderText);
                item.SubItems.Add(student.DateOfBirth.ToString("dd/MM/yyyy"));
                item.SubItems.Add(student.ClassName);
                item.SubItems.Add(student.CitizenId);
                item.SubItems.Add(student.Phone);
                item.SubItems.Add(student.Email);
                item.SubItems.Add(student.Address);
                item.SubItems.Add(student.Subjects == null ? string.Empty : string.Join(", ", student.Subjects));
                lvStudents.Items.Add(item);

                if (reselectMssv != null && string.Equals(student.MSSV, reselectMssv, StringComparison.OrdinalIgnoreCase))
                    itemToSelect = item;
            }

            lvStudents.EndUpdate();
            _suppressSelection = false;

            if (itemToSelect != null)
            {
                itemToSelect.Selected = true;
                itemToSelect.Focused = true;
            }

            UpdateStatusBar();
        }

        private void ClearInputs()
        {
            _suppressSelection = true;
            lvStudents.SelectedItems.Clear();
            _suppressSelection = false;

            _originalMssv = null;
            txtMssv.Clear();
            txtLastName.Clear();
            txtFirstName.Clear();
            rdMale.Checked = true;
            dtpDob.Value = new DateTime(2004, 1, 1);
            cboClass.SelectedIndex = -1;
            txtCitizenId.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
            for (int i = 0; i < clbSubjects.Items.Count; i++)
                clbSubjects.SetItemChecked(i, false);

            txtMssv.Focus();
        }

        private void UpdateStatusBar()
        {
            lblStatusFile.Text = string.IsNullOrWhiteSpace(_currentFilePath)
                ? "Tập tin: (chưa mở)"
                : "Tập tin: " + _currentFilePath;

            if (_isFiltered)
                lblStatusCount.Text = "Tổng số sinh viên: " + _manager.Count + "  |  Đang hiển thị: " + lvStudents.Items.Count;
            else
                lblStatusCount.Text = "Tổng số sinh viên: " + _manager.Count;
        }

        private void PersistAfterChange()
        {
            if (!_autoSaveEnabled || string.IsNullOrWhiteSpace(_currentFilePath))
            {
                Notify(
                    "Thao tác đã thực hiện trên danh sách, nhưng chưa có tập tin để ghi. Hãy dùng nút Lưu file.",
                    "Chưa lưu tập tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                SaveCurrentFile();
            }
            catch (Exception ex)
            {
                Notify(
                    "Đã cập nhật danh sách trên màn hình nhưng chưa ghi được xuống đĩa.\n" + GetFriendlyMessage(ex),
                    "Lỗi lưu tập tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentFile()
        {
            _repository.Save(_currentFilePath, _manager.Students.ToList());
            UpdateStatusBar();
        }

        private void WarnDuplicateMssv()
        {
            string message = _manager.FindDuplicateMssvMessage();
            if (message != null)
                Notify(message, "MSSV trùng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private List<string> GetCheckedIds()
        {
            List<string> ids = new List<string>();
            foreach (ListViewItem item in lvStudents.CheckedItems)
            {
                string id = item.Tag as string;
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
            }

            return ids;
        }

        private List<string> GetSelectedIds()
        {
            List<string> ids = new List<string>();
            foreach (ListViewItem item in lvStudents.SelectedItems)
            {
                string id = item.Tag as string;
                if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
                    ids.Add(id);
            }

            return ids;
        }

        private DataFormat SelectedFormat
        {
            get
            {
                if (rdXml.Checked)
                    return DataFormat.Xml;
                if (rdJson.Checked)
                    return DataFormat.Json;
                return DataFormat.Text;
            }
        }

        private void SelectFormatByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                switch (StudentRepository.DetectFormat(path))
                {
                    case DataFormat.Text:
                        rdTxt.Checked = true;
                        break;
                    case DataFormat.Xml:
                        rdXml.Checked = true;
                        break;
                    default:
                        rdJson.Checked = true;
                        break;
                }
            }
            catch (InvalidDataException)
            {
                rdJson.Checked = true;
            }
        }

        private static int FilterIndexOf(DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Text:
                    return 2;
                case DataFormat.Xml:
                    return 3;
                default:
                    return 4;
            }
        }

        private string EnsureKnownExtension(string path)
        {
            string extension = Path.GetExtension(path);
            if (StudentRepository.IsSupportedExtension(extension))
                return path;

            return path + StudentRepository.GetExtension(SelectedFormat);
        }

        private string InitialDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_currentFilePath))
            {
                string directory = Path.GetDirectoryName(_currentFilePath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    return directory;
            }

            return Application.StartupPath;
        }

        private string ExitConfirmMessage()
        {
            if (!_autoSaveEnabled && _manager.Count > 0)
            {
                return "Bạn có chắc chắn muốn thoát chương trình?\n\n"
                    + "Danh sách chưa gắn với tập tin hợp lệ nên sẽ không tự ghi khi thoát. "
                    + "Chọn Không, rồi bấm Lưu file nếu bạn muốn giữ dữ liệu.";
            }

            return "Bạn có chắc chắn muốn thoát chương trình?";
        }

        private static string GetDefaultFilePath()
        {
            string configured = null;
            try
            {
                configured = ConfigurationManager.AppSettings["DefaultDataFile"];
            }
            catch (ConfigurationErrorsException)
            {
                configured = null;
            }

            if (string.IsNullOrWhiteSpace(configured))
                configured = "students.json";

            if (!Path.IsPathRooted(configured))
                configured = Path.Combine(Application.StartupPath, configured);

            return configured;
        }

        private DialogResult Notify(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (AutomationMode)
                return buttons == MessageBoxButtons.YesNo ? DialogResult.Yes : DialogResult.OK;

            return MessageBox.Show(this, text, caption, buttons, icon);
        }

        private static string GetFriendlyMessage(Exception ex)
        {
            FileNotFoundException missing = ex as FileNotFoundException;
            if (missing != null)
                return "Không tìm thấy tập tin:\n" + missing.FileName;

            if (ex is InvalidDataException)
                return ex.Message;

            if (ex is IOException || ex is UnauthorizedAccessException)
            {
                return "Không thể đọc hoặc ghi tập tin. Hãy kiểm tra quyền truy cập, hoặc đóng chương trình khác đang mở tập tin này.\n\n"
                    + ex.Message;
            }

            return ex.Message;
        }
    }
}
