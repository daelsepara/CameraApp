using Gtk;

namespace gtkApp
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
                System.Console.WriteLine("Args: {0}", args);

            Application.Init();
            MainWindow win = new MainWindow();
            win.Show();
            Application.Run();
        }
    }
}
