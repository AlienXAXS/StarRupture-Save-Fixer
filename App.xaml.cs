using System.Windows;

namespace StarRuptureSaveFixer;

public partial class App : Application
{
    [System.STAThreadAttribute()]
    public static void Main()
    {
        var app = new App();
        app.Run(new MainWindow());
    }
}
