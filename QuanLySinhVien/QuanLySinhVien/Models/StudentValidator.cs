using System;
using System.Text.RegularExpressions;

namespace QuanLySinhVien.Models
{
    /// <summary>
    /// Kiểm tra dữ liệu trước khi thêm hoặc cập nhật.
    /// MSSV dạng AABBCCC: AA lấy từ mã lớp Kyy, BB cố định là 10, CCC là 3 chữ số.
    /// </summary>
    public static class StudentValidator
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryGetEnrollmentSuffix(string className, out string suffix)
        {
            Match match = Regex.Match(className ?? string.Empty, @"^K(\d{2})", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                suffix = null;
                return false;
            }

            suffix = match.Groups[1].Value;
            return true;
        }

        public static bool TryValidate(Student student, out string error)
        {
            error = null;
            if (student == null)
            {
                error = "Không có dữ liệu sinh viên.";
                return false;
            }

            string mssv = (student.MSSV ?? string.Empty).Trim();
            string lastName = (student.LastName ?? string.Empty).Trim();
            string firstName = (student.FirstName ?? string.Empty).Trim();
            string className = (student.ClassName ?? string.Empty).Trim();
            string citizenId = (student.CitizenId ?? string.Empty).Trim();
            string phone = (student.Phone ?? string.Empty).Trim();
            string email = (student.Email ?? string.Empty).Trim();
            string address = (student.Address ?? string.Empty).Trim();

            if (lastName.Length == 0)
            {
                error = "Hãy nhập họ và tên lót.";
                return false;
            }

            if (firstName.Length == 0)
            {
                error = "Hãy nhập tên.";
                return false;
            }

            if (className.Length == 0)
            {
                error = "Hãy chọn lớp.";
                return false;
            }

            if (!Regex.IsMatch(mssv, @"^\d{7}$"))
            {
                error = "MSSV phải gồm đúng 7 chữ số.";
                return false;
            }

            string yearSuffix;
            if (!TryGetEnrollmentSuffix(className, out yearSuffix))
            {
                error = "Lớp phải có dạng Kyy... (ví dụ K22CNTT) để xác định năm nhập học.";
                return false;
            }

            string expectedPrefix = yearSuffix + "10";
            if (!mssv.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                error = "MSSV của lớp " + className
                    + " phải có dạng " + expectedPrefix + "xxx.\n"
                    + "AA là 2 số cuối năm nhập học (" + yearSuffix + "), BB = 10, CCC là 3 chữ số bất kỳ.";
                return false;
            }

            if (!Regex.IsMatch(citizenId, @"^\d{9}$"))
            {
                error = "Số CMND phải gồm đúng 9 chữ số.";
                return false;
            }

            if (!Regex.IsMatch(phone, @"^\d{10}$"))
            {
                error = "Số điện thoại phải gồm đúng 10 chữ số.";
                return false;
            }

            if (email.Length == 0 || !EmailRegex.IsMatch(email))
            {
                error = "Email không đúng định dạng.";
                return false;
            }

            if (address.Length == 0)
            {
                error = "Hãy nhập địa chỉ liên lạc.";
                return false;
            }

            if (student.DateOfBirth.Date > DateTime.Today)
            {
                error = "Ngày sinh không được lớn hơn ngày hiện tại.";
                return false;
            }

            if (student.Subjects == null || student.Subjects.Count == 0)
            {
                error = "Hãy chọn ít nhất một môn học đăng ký.";
                return false;
            }

            student.MSSV = mssv;
            student.LastName = lastName;
            student.FirstName = firstName;
            student.ClassName = className.ToUpperInvariant();
            student.CitizenId = citizenId;
            student.Phone = phone;
            student.Email = email;
            student.Address = address;
            return true;
        }
    }
}
