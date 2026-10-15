using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Data
{
    public enum DataFormat
    {
        Text,
        Xml,
        Json
    }

    /// <summary>
    /// Đọc và ghi danh sách sinh viên ra 3 định dạng của Chủ đề 3:
    /// students.txt (StreamReader/StreamWriter), students.xml (XmlSerializer),
    /// students.json (Newtonsoft.Json).
    /// </summary>
    public class StudentRepository
    {
        public const char TextSeparator = '|';
        public const int TextFieldCount = 11;

        /// <summary>
        /// students.txt, mỗi sinh viên một dòng, 11 trường:
        /// MSSV|LastName|FirstName|DateOfBirth|Gender|ClassName|CitizenId|Phone|Email|Address|Subjects
        /// Ngày sinh: yyyy-MM-dd. Giới tính: 1 = Nam, 0 = Nữ. Môn học cách nhau bởi dấu chấm phẩy.
        /// </summary>
        public const string TextHeader =
            "# MSSV|LastName|FirstName|DateOfBirth(yyyy-MM-dd)|Gender(1=Nam,0=Nu)|ClassName|CitizenId|Phone|Email|Address|Subjects(phân cách bằng ;)";

        public static DataFormat DetectFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath ?? string.Empty);
            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                return DataFormat.Text;
            if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                return DataFormat.Xml;
            if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                return DataFormat.Json;

            throw new InvalidDataException(
                "Định dạng tập tin không được hỗ trợ. Chỉ dùng .txt, .xml hoặc .json.");
        }

        public static string GetExtension(DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Xml:
                    return ".xml";
                case DataFormat.Json:
                    return ".json";
                default:
                    return ".txt";
            }
        }

        public static bool IsSupportedExtension(string extension)
        {
            return extension != null
                && (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".json", StringComparison.OrdinalIgnoreCase));
        }

        public List<Student> Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn tập tin không hợp lệ.");

            // File.Exists được kiểm tra trước khi mở để báo lỗi rõ ràng,
            // thay vì để StreamReader ném FileNotFoundException tiếng Anh.
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy tập tin.", filePath);

            List<Student> students;
            switch (DetectFormat(filePath))
            {
                case DataFormat.Text:
                    students = ReadTextFile(filePath);
                    break;
                case DataFormat.Xml:
                    students = ReadXmlFile(filePath);
                    break;
                case DataFormat.Json:
                    students = ReadJsonFile(filePath);
                    break;
                default:
                    throw new InvalidDataException("Định dạng tập tin không được hỗ trợ.");
            }

            return Normalize(students);
        }

        public void Save(string filePath, IList<Student> students)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn tập tin không hợp lệ.");

            if (students == null)
                students = new List<Student>();

            string directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            switch (DetectFormat(filePath))
            {
                case DataFormat.Text:
                    WriteTextFile(filePath, students);
                    break;
                case DataFormat.Xml:
                    WriteXmlFile(filePath, students);
                    break;
                case DataFormat.Json:
                    WriteJsonFile(filePath, students);
                    break;
                default:
                    throw new InvalidDataException("Định dạng tập tin không được hỗ trợ.");
            }
        }

        /// <summary>
        /// Đọc students.txt bằng StreamReader, lần lượt từng dòng.
        /// Dòng trống và dòng chú thích (bắt đầu bằng #) được bỏ qua.
        /// Mỗi dòng hợp lệ có đúng 11 trường, ngăn cách bởi ký tự '|'.
        /// Nếu một dòng sai số trường hoặc sai ngày/giới tính thì dừng và báo đúng số dòng,
        /// để người dùng biết tập tin bị hỏng ở đâu.
        /// </summary>
        private static List<Student> ReadTextFile(string filePath)
        {
            List<Student> students = new List<Student>();

            // detectEncodingFromByteOrderMarks: true giúp đọc được file UTF-8 có BOM
            // (Notepad trên Windows thường ghi BOM). Encoding.UTF8 giữ nguyên tiếng Việt.
            using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8, true))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    string[] parts = trimmed.Split(TextSeparator);
                    if (parts.Length != TextFieldCount)
                    {
                        throw new InvalidDataException(
                            "Dòng " + lineNumber + " của tập tin TXT không đúng định dạng. Cần "
                            + TextFieldCount + " trường phân cách bởi '" + TextSeparator + "'.");
                    }

                    DateTime dateOfBirth;
                    if (!DateTime.TryParseExact(
                        parts[3].Trim(),
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out dateOfBirth))
                    {
                        throw new InvalidDataException(
                            "Dòng " + lineNumber + ": ngày sinh phải có dạng yyyy-MM-dd.");
                    }

                    bool gender;
                    string genderText = parts[4].Trim();
                    if (genderText == "1" || genderText.Equals("Nam", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = true;
                    }
                    else if (genderText == "0"
                        || genderText.Equals("Nu", StringComparison.OrdinalIgnoreCase)
                        || genderText.Equals("Nữ", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = false;
                    }
                    else
                    {
                        throw new InvalidDataException(
                            "Dòng " + lineNumber + ": giới tính phải là 1 (Nam) hoặc 0 (Nữ).");
                    }

                    List<string> subjects = string.IsNullOrWhiteSpace(parts[10])
                        ? new List<string>()
                        : parts[10]
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(item => item.Trim())
                            .Where(item => item.Length > 0)
                            .ToList();

                    students.Add(new Student
                    {
                        MSSV = parts[0].Trim(),
                        LastName = parts[1].Trim(),
                        FirstName = parts[2].Trim(),
                        DateOfBirth = dateOfBirth,
                        Gender = gender,
                        ClassName = parts[5].Trim(),
                        CitizenId = parts[6].Trim(),
                        Phone = parts[7].Trim(),
                        Email = parts[8].Trim(),
                        Address = parts[9].Trim(),
                        Subjects = subjects
                    });
                }
            }

            return students;
        }

        /// <summary>
        /// Ghi students.txt bằng StreamWriter, ghi đè toàn bộ file (append = false).
        /// Dùng UTF-8 có BOM để Notepad mở tiếng Việt không bị lỗi font.
        /// Ký tự '|' và xuống dòng trong dữ liệu được thay thế để mỗi sinh viên vẫn là một dòng.
        /// </summary>
        private static void WriteTextFile(string filePath, IList<Student> students)
        {
            using (StreamWriter writer = new StreamWriter(
                filePath,
                false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
            {
                writer.WriteLine(TextHeader);
                foreach (Student student in students)
                {
                    string subjects = student.Subjects == null
                        ? string.Empty
                        : string.Join(";", student.Subjects.Select(SafeText));

                    string line = string.Join(TextSeparator.ToString(), new[]
                    {
                        SafeText(student.MSSV),
                        SafeText(student.LastName),
                        SafeText(student.FirstName),
                        student.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        student.Gender ? "1" : "0",
                        SafeText(student.ClassName),
                        SafeText(student.CitizenId),
                        SafeText(student.Phone),
                        SafeText(student.Email),
                        SafeText(student.Address),
                        subjects
                    });

                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Đọc students.xml bằng XmlSerializer.
        /// XmlSerializer nạp cả tài liệu vào bộ nhớ rồi ánh xạ thẻ XML sang List&lt;Student&gt;
        /// theo tên thuộc tính public (MSSV, LastName, ...). Không cần duyệt XPath thủ công
        /// như ví dụ XmlDocument/books.xml trong giáo trình.
        /// Thẻ gốc mặc định là ArrayOfStudent, mỗi sinh viên nằm trong thẻ Student.
        /// </summary>
        private static List<Student> ReadXmlFile(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Student>));
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    object data = serializer.Deserialize(stream);
                    return data as List<Student> ?? new List<Student>();
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException(
                    "Tập tin XML bị hỏng hoặc không đúng cấu trúc danh sách sinh viên.\nChi tiết: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Ghi students.xml bằng XmlSerializer.
        /// Cách này phù hợp khi đã có danh sách đối tượng: không phải tự viết từng thẻ
        /// bằng XmlWriter. XmlWriterSettings chỉ để căn lề và ghi khai báo encoding UTF-8,
        /// giống dạng file XML minh họa trong giáo trình.
        /// </summary>
        private static void WriteXmlFile(string filePath, IList<Student> students)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Student>));
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = new UTF8Encoding(false),
                    OmitXmlDeclaration = false
                };

                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                {
                    serializer.Serialize(writer, students.ToList());
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException("Không ghi được tập tin XML.", ex);
            }
        }

        /// <summary>
        /// Đọc students.json.
        /// StreamReader lấy toàn bộ chuỗi JSON (UTF-8). JsonConvert.DeserializeObject
        /// của Newtonsoft.Json chuyển chuỗi đó thành List&lt;Student&gt;.
        /// File phải là một mảng JSON: [ { "MSSV": "...", ... }, ... ].
        /// Gói Newtonsoft.Json được cài theo hướng dẫn Chủ đề 3
        /// (Install-Package Newtonsoft.Json). Bản DLL đi kèm trong thư mục Lib.
        /// </summary>
        private static List<Student> ReadJsonFile(string filePath)
        {
            try
            {
                string json;
                using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8, true))
                {
                    json = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(json))
                    return new List<Student>();

                List<Student> students = JsonConvert.DeserializeObject<List<Student>>(json);
                return students ?? new List<Student>();
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Tập tin JSON bị hỏng hoặc không đúng cấu trúc danh sách sinh viên.\nChi tiết: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Ghi students.json bằng JsonConvert.SerializeObject, có thụt dòng (Formatting.Indented)
        /// để mở bằng Notepad vẫn đọc được. StreamWriter ghi UTF-8 có BOM.
        /// Thuộc tính chỉ đọc (FullName, Id) được đánh dấu JsonIgnore nên không ghi ra file.
        /// </summary>
        private static void WriteJsonFile(string filePath, IList<Student> students)
        {
            try
            {
                string json = JsonConvert.SerializeObject(students.ToList(), Newtonsoft.Json.Formatting.Indented);
                using (StreamWriter writer = new StreamWriter(
                    filePath,
                    false,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
                {
                    writer.Write(json);
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Không ghi được tập tin JSON.", ex);
            }
        }

        private static List<Student> Normalize(List<Student> students)
        {
            if (students == null)
                return new List<Student>();

            students.RemoveAll(student => student == null);
            foreach (Student student in students)
            {
                if (student.Subjects == null)
                    student.Subjects = new List<string>();

                student.MSSV = (student.MSSV ?? string.Empty).Trim();
                student.LastName = (student.LastName ?? string.Empty).Trim();
                student.FirstName = (student.FirstName ?? string.Empty).Trim();
                student.ClassName = (student.ClassName ?? string.Empty).Trim();
                student.CitizenId = (student.CitizenId ?? string.Empty).Trim();
                student.Phone = (student.Phone ?? string.Empty).Trim();
                student.Email = (student.Email ?? string.Empty).Trim();
                student.Address = (student.Address ?? string.Empty).Trim();
            }

            return students;
        }

        /// <summary>
        /// Bỏ ký tự phân cách và ký tự xuống dòng để một sinh viên luôn chiếm đúng một dòng TXT.
        /// </summary>
        private static string SafeText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("|", "/")
                .Replace(";", ",")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }
    }
}
