using System.Collections.ObjectModel;

namespace App;

public partial class MainPage : ContentPage
{
    private readonly WebService _service;

    // ICommand longPressCommand;
    //
    // public ICommand LongPressCommand => longPressCommand ??=
    //     new Command(async filename => await DisplayAlert("Alert", $"Long Pressed {filename}", "OK"));
    // public ICommand longPressCommand { get; }
    public ObservableCollection<WebService.ResponseItem.Result> resultList { get; } = new();

    public MainPage()
    {
        // longPressCommand =
        //     CommandFactory.Create(filename => DisplayAlert("Alert", $"Long Pressed {filename}", "OK"));
        InitializeComponent();
        _service = new();
        // ResultCollectionView.ItemsSource = ResultList;
    }

    async void OnScreenshotClicked(object sender, EventArgs e)
    {
        await TakeScreenshotAsync();
    }

    public async Task<ImageSource> TakeScreenshotAsync()
    {
        if (Screenshot.Default.IsCaptureSupported)
        {
            IScreenshotResult screen = await Screenshot.Default.CaptureAsync();
            Stream stream = await screen.OpenReadAsync();
            return ImageSource.FromStream(() => stream);
        }

        return null;
    }

    async void OnImageUrlEntryCompleted(object sender, EventArgs e)
    {
        resultList.Clear();
        var url = ImageUrlEntry.Text;
        var response = await _service.SearchByImageUrl(url, this);
        // await DisplayAlert("Alert", response?.ToString(), "OK");
        var responseResults = response?.result;
        if (responseResults == null) return;
        foreach (var result in responseResults)
        {
            resultList.Add(result);
        }
    }

    private async void OnPickPicClicked(object sender, EventArgs e)
    {
        resultList.Clear();
        var pic = await PickAndShow(new PickOptions
        {
            PickerTitle = "Please select a picture",
            FileTypes = FilePickerFileType.Images,
            // PresentationSourceBounds = System.Drawing.Rectangle.Empty,
            // PickerTitleIcon = System.Drawing.Icon.ExtractAssociatedIcon("icon.ico")
        });
        if (pic == null) return;
        var response = await _service.SearchByImageUpload(pic, this);
        // await DisplayAlert("Alert", response?.ToString(), "OK");
        var responseResults = response?.result;
        if (responseResults == null) return;
        foreach (var result in responseResults)
        {
            resultList.Add(result);
        }
    }

    private async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
            await DisplayAlert("Error", ex.ToString(), "OK");
        }

        return null;
    }

    private async void Title_OnTapped(object sender, TappedEventArgs e)
    {
        var oldResult = (sender as BindableObject)?.BindingContext as WebService.ResponseItem.Result;
        // await DisplayAlert("Alert", oldResult?.ToString(), "OK");
        resultList.Clear();
        var url = oldResult?.image;
        var response = await _service.SearchByImageUrl(url, this);
        // await DisplayAlert("Alert", response?.ToString(), "OK");
        var responseResultList = response?.result;
        if (responseResultList != null)
        {
            foreach (var result in responseResultList)
            {
                resultList.Add(result);
            }
        }
    }

    private void Image_OnTapped(object sender, TappedEventArgs e)
    {
        var result = (sender as BindableObject)?.BindingContext as WebService.ResponseItem.Result;
        if (result != null) result.isImage = false;
    }

    private void OpenWebviewPage(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not WebService.ResponseItem.Result result) return;
        var url = $"https://anilist.co/anime/{result.anilist}";
        Window newWindow = new(new WebviewPage(url))
        {
            Title = $"Anilist-{result.anilist}"
        };
        Application.Current?.OpenWindow(newWindow);
    }

    private async void ShowDetailMessage(object sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not WebService.ResponseItem.Result result) return;
        var answer = await DisplayAlert("Info", result.ToString(), "Copy", "OK");
        if (answer)
        {
            await Clipboard.SetTextAsync(result.ToString());
        }
    }
}