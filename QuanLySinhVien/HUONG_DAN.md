# Quản lý sinh viên — Chủ đề 3: Thao tác tập tin

Ứng dụng Windows Forms (.NET Framework 4.8) quản lý danh sách sinh viên và đọc/ghi ba định dạng `students.txt`, `students.xml`, `students.json`.

## 1. Mở và chạy

1. Cài Visual Studio 2022, workload **.NET desktop development** (có .NET Framework 4.8 targeting pack).
2. Mở `QuanLySinhVien.sln`.
3. Bấm **F5**.

Thư viện **Newtonsoft.Json 13.0.3** đã nằm sẵn tại `QuanLySinhVien\Lib\Newtonsoft.Json.dll`, nên solution build được khi không có mạng. Nếu muốn cài lại theo giáo trình: chuột phải project → Manage NuGet Packages, hoặc trong Package Manager Console:

```powershell
Install-Package Newtonsoft.Json -Version 13.0.3
```

Sau khi cài bằng NuGet, sửa `HintPath` trong file `.csproj` cho trỏ tới `packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll`.

Khi chạy, chương trình đọc `students.json` nằm cạnh file `.exe` (thư mục `bin\Debug`). Đường dẫn này khai báo trong `App.config`, khóa `DefaultDataFile`. Ba file mẫu được copy từ `SampleData` ra thư mục output lúc build.

Muốn lấy lại dữ liệu mẫu: đóng ứng dụng, xóa thư mục `bin` và `obj`, rồi build lại.

## 2. Cấu trúc mã nguồn

| Thành phần | File | Vai trò |
| --- | --- | --- |
| Model | `Models\Student.cs` | Thuộc tính sinh viên |
| Ràng buộc | `Models\StudentValidator.cs` | MSSV, CMND, số điện thoại, email, môn học |
| Danh mục | `Models\StudentCatalog.cs` | Lớp (Kyy...) và môn mặc định |
| Nghiệp vụ | `Business\StudentManager.cs` | Thêm, sửa, xóa, tìm trên `List<Student>` |
| Đọc/ghi file | `Data\StudentRepository.cs` | TXT, XML, JSON |
| Form chính | `Forms\frmStudentManager.cs` | Giao diện, không chứa cú pháp đọc file |
| Tìm kiếm | `Forms\frmTimKiem.cs` | Nhập một hoặc nhiều điều kiện |

`StudentManager` chỉ xử lý danh sách trong bộ nhớ. Mọi đọc/ghi đĩa nằm trong `StudentRepository`.

## 3. Ánh xạ yêu cầu Chủ đề 3

- Lớp `Student`: MSSV, họ và tên lót, tên, ngày sinh, giới tính, lớp, CMND, số điện thoại, email, địa chỉ, môn đăng ký. `FullName` ghép từ họ và tên. `Id` là bí danh của MSSV, không ghi ra file.
- `StudentManager`: thêm, cập nhật, xóa một hoặc nhiều, tìm theo MSSV / họ tên / lớp.
- Đọc và ghi `students.txt` bằng `StreamReader` / `StreamWriter`, ngăn cách trường bằng `|`.
- Đọc và ghi `students.xml` bằng `XmlSerializer`.
- Đọc và ghi `students.json` bằng `JsonConvert` (Newtonsoft.Json).
- Mở chương trình thì nạp file mặc định. Thêm, sửa, xóa thì ghi lại file đang mở. Đóng chương trình thì hỏi và tự lưu.
- MSSV đúng 7 số, dạng `AABBCCC`: AA là 2 số cuối năm nhập học lấy từ mã lớp `Kyy` (lớp `K22CNTT` thì MSSV có dạng `2210xxx`), BB cố định là `10`, CCC là 3 chữ số. Không trùng MSSV.
- CMND đúng 9 chữ số. Số điện thoại đúng 10 chữ số.
- Phải nhập đủ thông tin rồi mới thêm hoặc cập nhật.
- Chọn một dòng thì dữ liệu điền lên form.
- Danh sách dùng `ListView` (Details, GridLines, Checkbox). Chuột phải để xóa dòng đang chọn hoặc các dòng đã đánh dấu.
- Môn học là `CheckedListBox`. Chuột phải để thêm hoặc xóa môn.
- Nút **Tìm kiếm** mở form điều kiện. Ô để trống được bỏ qua. Nhiều ô cùng lúc thì sinh viên phải thỏa tất cả.
- Nút **Thoát** hỏi lại trước khi đóng.
- **Mở file** / **Lưu file** dùng `OpenFileDialog` và `SaveFileDialog`. Radio TXT, XML, JSON chọn định dạng.
- `StatusStrip` hiện đường dẫn file đang mở và tổng số sinh viên.
- File không tồn tại hoặc sai định dạng thì báo bằng `MessageBox`, không làm vỡ chương trình. File mặc định bị hỏng thì không ghi đè khi thoát.

## 4. Thao tác trên form

1. Nhập đủ các ô có dấu `*`, chọn ít nhất một môn, bấm **Thêm mới**.
2. Chọn một dòng để sửa, rồi bấm **Cập nhật**.
3. Đánh dấu checkbox (hoặc chọn dòng) rồi bấm **Xóa**, hoặc chuột phải trên danh sách.
4. **Tìm kiếm** theo MSSV, họ tên hoặc lớp. **Hiện tất cả** để bỏ lọc. Tìm họ tên không phân biệt dấu (gõ `binh` vẫn ra `Bình`).
5. Chọn TXT, XML hoặc JSON rồi **Lưu file** để ghi đúng định dạng. **Mở file** để nạp lại.
6. **Thoát** hỏi xác nhận. Nếu đồng ý, danh sách đầy đủ (không phải danh sách đang lọc) được ghi vào file hiện tại.

Ví dụ MSSV hợp lệ:

| Lớp | MSSV hợp lệ |
| --- | --- |
| K22CNTT | 2210001 |
| K23HTTT | 2310015 |
| K24KTPM | 2410123 |

## 5. Định dạng tập tin

### TXT

Mỗi sinh viên một dòng, UTF-8. Dòng bắt đầu bằng `#` là chú thích.

```text
MSSV|LastName|FirstName|yyyy-MM-dd|Gender|ClassName|CitizenId|Phone|Email|Address|Mon1;Mon2
2210001|Nguyễn Văn|An|2004-05-12|1|K22CNTT|079204001|0901234567|an.nguyen@student.edu.vn|12 Nguyễn Huệ, Q.1, TP.HCM|Mạng máy tính;Hệ điều hành
```

`Gender`: `1` = Nam, `0` = Nữ.

### XML

`XmlSerializer` ghi thẻ gốc `ArrayOfStudent`, mỗi sinh viên là một thẻ `Student`. Tên thẻ trùng tên thuộc tính.

### JSON

Một mảng đối tượng. `Gender` là `true` / `false`. `DateOfBirth` dạng ISO `yyyy-MM-ddTHH:mm:ss`. `Subjects` là mảng chuỗi.

## 6. Kiểm tra đọc/ghi không cần mở form

Trong terminal, tại thư mục chứa file exe:

```powershell
.\QuanLySinhVien.exe --selftest
```

Lệnh này kiểm tra quy tắc MSSV, từ chối số điện thoại sai và MSSV trùng, ghi rồi đọc lại cả ba định dạng, từ chối file TXT hỏng hoặc file không tồn tại, và đọc ba file mẫu. Kết quả ghi tại `%TEMP%\qlsv-selftest.txt`. Mã thoát `0` là đạt.

## 7. Sự cố thường gặp

- **Không thấy tiếng Việt trong Notepad:** file TXT/JSON do chương trình ghi là UTF-8 có BOM. Khi mở, chọn encoding UTF-8.
- **Báo file JSON/XML hỏng:** file không phải danh sách sinh viên (XML phải có gốc `ArrayOfStudent`, JSON phải là một mảng).
- **Thêm bị từ chối vì MSSV:** đối chiếu 2 số đầu với năm của lớp và kiểm tra cặp số tiếp theo có đúng `10` hay không.
- **Build lỗi thiếu Newtonsoft.Json:** kiểm tra file `QuanLySinhVien\Lib\Newtonsoft.Json.dll` còn tồn tại.
