using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Business
{
    /// <summary>
    /// Danh sách sinh viên trong bộ nhớ và các thao tác thêm, sửa, xóa, tìm.
    /// Lớp này không đọc/ghi tập tin. Việc lưu file do StudentRepository đảm nhiệm.
    /// </summary>
    public class StudentManager
    {
        private readonly List<Student> _students = new List<Student>();

        public IReadOnlyList<Student> Students
        {
            get { return _students.AsReadOnly(); }
        }

        public int Count
        {
            get { return _students.Count; }
        }

        public void ReplaceAll(IEnumerable<Student> students)
        {
            _students.Clear();
            if (students == null)
                return;

            foreach (Student student in students)
            {
                if (student != null)
                    _students.Add(student.Clone());
            }
        }

        public Student FindByMssv(string mssv)
        {
            if (string.IsNullOrWhiteSpace(mssv))
                return null;

            return _students.FirstOrDefault(s =>
                string.Equals(s.MSSV, mssv.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public bool Add(Student student, out string error)
        {
            if (!StudentValidator.TryValidate(student, out error))
                return false;

            if (FindByMssv(student.MSSV) != null)
            {
                error = "MSSV đã tồn tại. Không thể thêm sinh viên trùng mã.";
                return false;
            }

            _students.Add(student.Clone());
            error = null;
            return true;
        }

        public bool Update(string originalMssv, Student student, out string error)
        {
            if (string.IsNullOrWhiteSpace(originalMssv))
            {
                error = "Hãy chọn một sinh viên trong danh sách trước khi cập nhật.";
                return false;
            }

            if (!StudentValidator.TryValidate(student, out error))
                return false;

            int index = _students.FindIndex(s =>
                string.Equals(s.MSSV, originalMssv.Trim(), StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                error = "Không tìm thấy sinh viên cần cập nhật.";
                return false;
            }

            bool duplicated = _students.Any(s =>
                !string.Equals(s.MSSV, originalMssv.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(s.MSSV, student.MSSV, StringComparison.OrdinalIgnoreCase));
            if (duplicated)
            {
                error = "MSSV đã tồn tại. Không thể cập nhật trùng mã sinh viên.";
                return false;
            }

            _students[index] = student.Clone();
            error = null;
            return true;
        }

        public int DeleteMany(IEnumerable<string> mssvList)
        {
            if (mssvList == null)
                return 0;

            HashSet<string> ids = new HashSet<string>(
                mssvList.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return _students.RemoveAll(s => ids.Contains(s.MSSV));
        }

        /// <summary>
        /// Lọc theo các điều kiện được nhập. Điều kiện bỏ trống không tham gia lọc.
        /// Nhiều điều kiện cùng lúc được kết hợp theo AND.
        /// </summary>
        public List<Student> Search(string mssv, string name, string className)
        {
            IEnumerable<Student> query = _students;

            if (!string.IsNullOrWhiteSpace(mssv))
            {
                string keyword = mssv.Trim();
                query = query.Where(s =>
                    (s.MSSV ?? string.Empty).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                string keyword = RemoveDiacritics(name.Trim());
                query = query.Where(s =>
                    RemoveDiacritics(s.FullName).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(className))
            {
                string keyword = className.Trim();
                query = query.Where(s =>
                    string.Equals(s.ClassName, keyword, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(s => s.MSSV).ToList();
        }

        public string FindDuplicateMssvMessage()
        {
            List<string> duplicated = _students
                .GroupBy(s => s.MSSV ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Key.Length > 0 && g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicated.Count == 0)
                return null;

            return "Tập tin đang có MSSV bị trùng: " + string.Join(", ", duplicated)
                + ".\nHãy sửa hoặc xóa bớt trước khi lưu thêm.";
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder(normalized.Length);
            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D');
        }
    }
}
