
using FFMpegCore;
using FFMpegCore.Enums;

namespace DavinciFfmpegRecoder;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty CurrentProgressProperty =
        BindableProperty.Create("CurrentProgress", typeof(double), typeof(MainPage), false);

    public double CurrentProgress
    {
        get => (double)GetValue(CurrentProgressProperty);
        set => SetValue(CurrentProgressProperty, value);
    }

    private async void FileDialogButtonClicked(object sender, EventArgs e)
    {
        FileDialogButton.IsVisible = false;

        var fileResult = await PickFile(new PickOptions());
        if (fileResult == null) return;

        subHeading.Text = "Converting...";
        mainProgressBar.IsVisible = true;

        var ffmpegFolderPath = $"{AppDomain.CurrentDomain.BaseDirectory}/put_ffmpeg_exe_and_ffprobe_exe_inside_here";
        GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegFolderPath);

        var mediaInfo = await FFProbe.AnalyseAsync(fileResult.FullPath);
        var totalTime = mediaInfo.Duration;

        var path = fileResult.FullPath.Replace(fileResult.FileName, "");
        var outFileName = $"output-{fileResult.FileName}-{DateTime.Now:yyyy-MM-dd--HH-mm}.mp4";
        var outputFilePath = Path.Combine(path, outFileName).ToString();

        await FFMpegArguments
            .FromFileInput(fileResult.FullPath)
            .OutputToFile(outputFilePath, false, options => options
                    .CreateHdOptions()
                    )
                    .NotifyOnProgress(progress => UpdateProgress(progress, mediaInfo.Duration))
            .ProcessAsynchronously();

        FileDialogButton.IsVisible = true;
        mainProgressBar.IsVisible = false;
        mainProgressBar.Progress = 0;
        subHeading.Text = "Converting finished. You can select another file if you want";

    }

    public void UpdateProgress(TimeSpan progress, TimeSpan movieLength)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var progressPercentage = Math.Ceiling((progress / movieLength) * 100);
            var text = $"{progress:hh'h 'mm'm 'ss's'}/{movieLength:hh'h 'mm'm 'ss's'} ({progressPercentage}%) ";
            subHeading.Text = text;
            mainProgressBar.Progress = progressPercentage / 100;
        });
    }

    public async Task<FileResult> PickFile(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return result;
        }
        catch (Exception)
        {
            // The user canceled or something went wrong
        }

        return null;
    }

}

public static class MyExtensions
{
    public static FFMpegArgumentOptions CreateHdOptions(this FFMpegArgumentOptions options)
    {
        return options
            .WithVideoCodec(VideoCodec.LibX264)
            .ForcePixelFormat("yuv420p")
            .WithConstantRateFactor(21)
            .WithAudioCodec(AudioCodec.Aac)
            .WithVariableBitrate(4)
            .WithVideoFilters(filterOptions => filterOptions.Scale(VideoSize.Hd))
            .WithFastStart();
    }

    public static FFMpegArgumentOptions CreateSdOptions(this FFMpegArgumentOptions options)
    {
        return options
            .WithVideoCodec(VideoCodec.LibX264)
            .WithConstantRateFactor(23)
            .WithAudioCodec(AudioCodec.LibVorbis)
            .WithAudioBitrate(128)
            .WithVideoFilters(filterOptions => filterOptions.Scale(VideoSize.Hd))
            .WithSpeedPreset(Speed.VeryFast)
            .WithFastStart();
    }
}

