using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QuanLySinhVien.Business;
using QuanLySinhVien.Data;
using QuanLySinhVien.Models;

namespace QuanLySinhVien
{
    /// <summary>
    /// Kiểm tra đọc/ghi 3 định dạng và quy tắc MSSV. Chạy: QuanLySinhVien.exe --selftest
    /// </summary>
    internal static class SmokeTest
    {
        public static int Run()
        {
            StringBuilder log = new StringBuilder();
            string logPath = Path.Combine(Path.GetTempPath(), "qlsv-selftest.txt");
            try
            {
                using (frmStudentManager form = new frmStudentManager())
                {
                    log.AppendLine("Form created: " + form.Text);
                }

                ValidateRules(log);
                RoundTrip(log);
                LoadSamples(log);
                log.AppendLine("OK");
                File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);
                return 0;
            }
            catch (Exception ex)
            {
                log.AppendLine("FAIL");
                log.AppendLine(ex.ToString());
                File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);
                return 1;
            }
        }

        private static void ValidateRules(StringBuilder log)
        {
            Student valid = SampleStudents()[0];
            string error;
            if (!StudentValidator.TryValidate(valid.Clone(), out error))
                throw new InvalidOperationException("Sinh viên mẫu phải hợp lệ: " + error);

            Student badPhone = valid.Clone();
            badPhone.Phone = "090123456";
            if (StudentValidator.TryValidate(badPhone, out error))
                throw new InvalidOperationException("Số điện thoại 9 số không được qua.");

            Student badMssv = valid.Clone();
            badMssv.MSSV = "2211001";
            if (StudentValidator.TryValidate(badMssv, out error))
                throw new InvalidOperationException("BB khác 10 không được qua.");

            Student badYear = valid.Clone();
            badYear.MSSV = "2310001";
            if (StudentValidator.TryValidate(badYear, out error))
                throw new InvalidOperationException("AA không khớp lớp không được qua.");

            StudentManager manager = new StudentManager();
            if (!manager.Add(valid.Clone(), out error))
                throw new InvalidOperationException(error);
            if (manager.Add(valid.Clone(), out error))
                throw new InvalidOperationException("MSSV trùng phải bị từ chối.");

            List<Student> found = manager.Search(null, "an", null);
            if (found.Count != 1)
                throw new InvalidOperationException("Tìm theo tên thất bại.");

            log.AppendLine("Validation OK");
        }

        private static void RoundTrip(StringBuilder log)
        {
            StudentRepository repository = new StudentRepository();
            List<Student> original = SampleStudents();
            string folder = Path.Combine(Path.GetTempPath(), "qlsv-roundtrip-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                foreach (string name in new[] { "students.txt", "students.xml", "students.json" })
                {
                    string path = Path.Combine(folder, name);
                    repository.Save(path, original);
                    List<Student> loaded = repository.Load(path);
                    if (loaded.Count != original.Count)
                        throw new InvalidOperationException(name + " count " + loaded.Count);

                    for (int i = 0; i < original.Count; i++)
                        AssertSame(original[i], loaded.First(s => s.MSSV == original[i].MSSV), name);

                    log.AppendLine("RoundTrip " + name + " OK");
                }

                string broken = Path.Combine(folder, "broken.txt");
                File.WriteAllText(broken, "khong dung dinh dang\r\n", Encoding.UTF8);
                try
                {
                    repository.Load(broken);
                    throw new InvalidOperationException("File TXT hỏng phải ném lỗi.");
                }
                catch (InvalidDataException)
                {
                    log.AppendLine("Broken TXT rejected OK");
                }

                try
                {
                    repository.Load(Path.Combine(folder, "missing.json"));
                    throw new InvalidOperationException("File không tồn tại phải ném lỗi.");
                }
                catch (FileNotFoundException)
                {
                    log.AppendLine("Missing file rejected OK");
                }
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        private static void LoadSamples(StringBuilder log)
        {
            StudentRepository repository = new StudentRepository();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string name in new[] { "students.txt", "students.xml", "students.json" })
            {
                string path = Path.Combine(baseDir, name);
                List<Student> students = repository.Load(path);
                if (students.Count != 3)
                    throw new InvalidOperationException(name + " mẫu phải có 3 sinh viên, hiện có " + students.Count);

                Student first = students.First(s => s.MSSV == "2210001");
                if (first.FirstName != "An" || first.Subjects.Count != 2)
                    throw new InvalidOperationException(name + " đọc sai dữ liệu mẫu.");

                log.AppendLine("Sample " + name + " OK");
            }
        }

        private static void AssertSame(Student expected, Student actual, string format)
        {
            if (expected.MSSV != actual.MSSV
                || expected.LastName != actual.LastName
                || expected.FirstName != actual.FirstName
                || expected.Gender != actual.Gender
                || expected.DateOfBirth.Date != actual.DateOfBirth.Date
                || expected.ClassName != actual.ClassName
                || expected.CitizenId != actual.CitizenId
                || expected.Phone != actual.Phone
                || expected.Email != actual.Email
                || expected.Address != actual.Address
                || !expected.Subjects.SequenceEqual(actual.Subjects))
            {
                throw new InvalidOperationException("Dữ liệu " + format + " không khớp MSSV " + expected.MSSV);
            }
        }

        private static List<Student> SampleStudents()
        {
            return new List<Student>
            {
                new Student
                {
                    MSSV = "2210001",
                    LastName = "Nguyễn Văn",
                    FirstName = "An",
                    DateOfBirth = new DateTime(2004, 5, 12),
                    Gender = true,
                    ClassName = "K22CNTT",
                    CitizenId = "079204001",
                    Phone = "0901234567",
                    Email = "an.nguyen@student.edu.vn",
                    Address = "12 Nguyễn Huệ, Q.1, TP.HCM",
                    Subjects = new List<string> { "Mạng máy tính", "Hệ điều hành" }
                },
                new Student
                {
                    MSSV = "2310015",
                    LastName = "Trần Thị",
                    FirstName = "Bình",
                    DateOfBirth = new DateTime(2005, 11, 3),
                    Gender = false,
                    ClassName = "K23HTTT",
                    CitizenId = "079205112",
                    Phone = "0912345678",
                    Email = "binh.tran@student.edu.vn",
                    Address = "45 Lê Lợi, Q.1, TP.HCM",
                    Subjects = new List<string> { "Lập trình CSDL", "Phương pháp NCKH" }
                },
                new Student
                {
                    MSSV = "2210042",
                    LastName = "Lê Hoàng",
                    FirstName = "Cường",
                    DateOfBirth = new DateTime(2004, 8, 21),
                    Gender = true,
                    ClassName = "K22KTPM",
                    CitizenId = "079204333",
                    Phone = "0987654321",
                    Email = "cuong.le@student.edu.vn",
                    Address = "8 Trần Hưng Đạo, Q.5, TP.HCM",
                    Subjects = new List<string> { "An toàn và bảo mật hệ thống", "Lập trình mạng" }
                }
            };
        }
    }
}
