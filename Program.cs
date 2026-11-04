using HandaKaNaBa.UI.Forms;

namespace HandaKaNaBa
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MenuForm());
        }
    }
}
