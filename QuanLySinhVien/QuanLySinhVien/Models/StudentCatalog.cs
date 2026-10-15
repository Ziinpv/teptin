using System;
using System.Text.RegularExpressions;

namespace QuanLySinhVien.Models
{
    /// <summary>
    /// Một lớp học. Hai chữ số sau chữ K là 2 số cuối năm nhập học,
    /// dùng để kiểm tra phần AA của MSSV.
    /// </summary>
    public sealed class ClassOption
    {
        public ClassOption(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || !Regex.IsMatch(code, @"^K\d{2}", RegexOptions.IgnoreCase))
                throw new ArgumentException("Mã lớp phải bắt đầu bằng K và 2 chữ số năm, ví dụ K22CNTT.");

            Code = code.Trim().ToUpperInvariant();
            Name = name ?? string.Empty;
        }

        public string Code { get; private set; }

        public string Name { get; private set; }

        public string EnrollmentYearSuffix
        {
            get { return Code.Substring(1, 2); }
        }

        public override string ToString()
        {
            return Code + " - " + Name;
        }
    }

    public static class StudentCatalog
    {
        public static readonly ClassOption[] Classes =
        {
            new ClassOption("K22CNTT", "Công nghệ thông tin, khóa 2022"),
            new ClassOption("K22HTTT", "Hệ thống thông tin, khóa 2022"),
            new ClassOption("K22KTPM", "Kỹ thuật phần mềm, khóa 2022"),
            new ClassOption("K23CNTT", "Công nghệ thông tin, khóa 2023"),
            new ClassOption("K23HTTT", "Hệ thống thông tin, khóa 2023"),
            new ClassOption("K23KTPM", "Kỹ thuật phần mềm, khóa 2023"),
            new ClassOption("K24CNTT", "Công nghệ thông tin, khóa 2024"),
            new ClassOption("K24HTTT", "Hệ thống thông tin, khóa 2024"),
            new ClassOption("K24KTPM", "Kỹ thuật phần mềm, khóa 2024")
        };

        public static readonly string[] Subjects =
        {
            "Mạng máy tính",
            "Hệ điều hành",
            "Lập trình CSDL",
            "Lập trình mạng",
            "Đồ án cơ sở",
            "Phương pháp NCKH",
            "Lập trình trên thiết bị di động",
            "An toàn và bảo mật hệ thống"
        };
    }
}
