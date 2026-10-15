using System;
using System.Linq;
using System.Windows.Forms;

namespace QuanLySinhVien
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (HasArg(args, "--selftest"))
            {
                Environment.ExitCode = SmokeTest.Run();
                return;
            }

            Application.Run(new frmStudentManager());
        }

        private static bool HasArg(string[] args, string name)
        {
            if (args == null)
                return false;

            return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
