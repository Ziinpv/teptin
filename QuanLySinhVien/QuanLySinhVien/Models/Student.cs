using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace QuanLySinhVien.Models
{
    /// <summary>
    /// Sinh viên. MSSV là khóa chính.
    /// FullName được ghép từ LastName (họ và tên lót) và FirstName (tên)
    /// để khớp giao diện giáo trình, không ghi riêng ra file.
    /// </summary>
    public class Student
    {
        public Student()
        {
            Subjects = new List<string>();
        }

        /// <summary>Mã số sinh viên, đúng 7 chữ số dạng AABBCCC.</summary>
        public string MSSV { get; set; }

        /// <summary>Họ và tên lót.</summary>
        public string LastName { get; set; }

        /// <summary>Tên.</summary>
        public string FirstName { get; set; }

        public DateTime DateOfBirth { get; set; }

        /// <summary>true = Nam, false = Nữ.</summary>
        public bool Gender { get; set; }

        /// <summary>Mã lớp dạng Kyy..., ví dụ K22CNTT. yy là năm nhập học.</summary>
        public string ClassName { get; set; }

        /// <summary>Số CMND, đúng 9 chữ số.</summary>
        public string CitizenId { get; set; }

        /// <summary>Số điện thoại, đúng 10 chữ số.</summary>
        public string Phone { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        /// <summary>Các môn học đã đăng ký.</summary>
        public List<string> Subjects { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public string Id
        {
            get { return MSSV; }
            set { MSSV = value; }
        }

        [XmlIgnore]
        [JsonIgnore]
        public string FullName
        {
            get { return ((LastName ?? string.Empty) + " " + (FirstName ?? string.Empty)).Trim(); }
        }

        [XmlIgnore]
        [JsonIgnore]
        public string GenderText
        {
            get { return Gender ? "Nam" : "Nữ"; }
        }

        public Student Clone()
        {
            return new Student
            {
                MSSV = MSSV,
                LastName = LastName,
                FirstName = FirstName,
                DateOfBirth = DateOfBirth,
                Gender = Gender,
                ClassName = ClassName,
                CitizenId = CitizenId,
                Phone = Phone,
                Email = Email,
                Address = Address,
                Subjects = Subjects == null ? new List<string>() : new List<string>(Subjects)
            };
        }
    }
}
