using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App;

public partial class WebviewPage : ContentPage
{
    public string source { get; set; }

    public WebviewPage(string source)
    {
        this.source = source;
        InitializeComponent();
    }

    private async void JumpToBrowser(object sender, EventArgs e)
    {
        await Launcher.OpenAsync(source);
        Application.Current?.CloseWindow(Window);
    }
}