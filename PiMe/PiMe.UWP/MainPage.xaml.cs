using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechSynthesis;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiMe.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer _timer;
        private string lastPerson;
        private MediaCapture mediaCapture;

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitPreview();

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 1);

            _timer.Start();
            _timer.Tick += _timer_Tick;
        }

        private async Task InitPreview()
        {
            //Video and Audio is initialized by default  
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            // Start Preview                  
            PreviewElement.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();

            _lastTake = DateTime.Now.AddSeconds(0);
        }

        private void _timer_Tick(object sender, object e)
        {
            Debug.WriteLine("Timer tick");
            try
            {
                TimerCallBack(null);
                //System.Diagnostics.Debug.WriteLine("Button is " + _pi.IsButtonPressed());
            }
            finally
            {

            }
        }

        private int _page = 0;
        private bool recognizing = false;
        int _timeCount = 0;
        int _rangeTreshold = 75;
        int _rangeTimeout = 5;
        DateTime _lastTake;
        bool inTheTimer = false;

        private void TimerCallBack(object o)
        {
            if (inTheTimer) return;
            inTheTimer = true;

            try
            {
                _timeCount++;
               // var range = _pi.GetRange();

                //var t = _pi.GetTemperatureAndHumidity();
                //await Task.Delay(500);
                //var l = _pi.GetLight();

                //Debug.WriteLine("L: " + l + " R: " + r);
                //Debug.WriteLine("Range: " + range);

                int sinceLast = (int)DateTime.Now.Subtract(_lastTake).TotalSeconds;

                //if (_pi.IsButtonPressed())
                //{
                //    _timer.Stop();
                //    Debug.WriteLine("Taking photo....");
                //    _pi.DisplayText(lastPerson ?? ("Button pressed. Taking your photo..."), Colors.White);
                //    _lastTake = DateTime.Now;
                //    TakePhoto();
                //}
                //else if (range < _rangeTreshold)
                //{
                //    if (sinceLast > _rangeTimeout)
                //    {
                //        _timer.Stop();
                //        Debug.WriteLine("Taking photo....");
                //        _pi.DisplayText(lastPerson ?? ("Taking your photo..."), Colors.White);
                //        _lastTake = DateTime.Now;
                //        TakePhoto();
                //    }
                //    else
                //    {
                //        Debug.WriteLine("Too soon....");
                //        _pi.DisplayText(lastPerson ?? ("Wait a bit..."), Colors.Orange);
                //    }
                //}
                //else
                //{
                //    Debug.WriteLine("Ready... nobody near.");
                //    _pi.DisplayText(
                //        lastPerson ?? ("Come closer!"),
                //        "R: " + range.ToString().PadRight(4) + "T: " + sinceLast,
                //        Colors.Green);
                //}

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                inTheTimer = false;
            }
        }

        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";

        private async void TakePhoto()
        {
            try
            {
                Debug.WriteLine("TakePhoto()");
                //_pi.DisplayText("Taking your photo...", Colors.Pink);
                Speak("Ok, let me process that picture!");

                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                var imageProperties = ImageEncodingProperties.CreateJpeg();

                var resolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).Select(x => x as VideoEncodingProperties);

                ////stop preview
                await mediaCapture.StopPreviewAsync();

                ////highest res possible
                var maxRes = resolutions.OrderByDescending(x => x.Height * x.Width).FirstOrDefault();
                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, maxRes);
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);

                var photoStream = await photoFile.OpenReadAsync();
                var bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;

                PreviewElement.Source = null;

                var imageAnalysisResult = await UploadAndAnalyzeImage(photoFile.Path);

                Debug.WriteLine("Recognizing...");
                //_pi.DisplayText(imageAnalysisResult.Description.Captions[0].Text, Colors.Pink);

                Speak("I see, " + imageAnalysisResult.Description.Captions[0].Text);

                if (lastPerson == "Error")
                {
                    //_pi.DisplayText("Who are you?!", "Go away!", Colors.Red);
                    Debug.WriteLine("Error on Recognizing");
                }
                else
                {
                    //_pi.DisplayText("It's you, " + lastPerson, Colors.DeepPink);
                    Debug.WriteLine("It's " + @lastPerson);
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                string myIp = GetLocalIp();

                if (myIp.Length > 1)
                    Speak("Error! My IP Address is " + myIp);
                else
                    Speak("Woops, no internet.");
            }
            finally
            {
                _lastTake = DateTime.Now;
                await InitPreview();
                Debug.WriteLine("Timer on.");
                _timer.Start();
            }

        }

        private async Task<AnalysisResult> UploadAndAnalyzeImage(string imageFilePath)
        {
            //
            // Create Project Oxford Vision API Service client
            //
            Speak("I am analysing your photo now!");
            VisionServiceClient VisionServiceClient = new VisionServiceClient("52dc919127a645ca99a050f06ab49c9d");
            Debug.WriteLine("VisionServiceClient is created");

            StorageFile file = await StorageFile.GetFileFromPathAsync(imageFilePath);

            using (Stream imageFileStream = (await file.OpenReadAsync()).AsStreamForRead())
            {
                // Analyze the image for all visual features
                Debug.WriteLine("Calling VisionServiceClient.AnalyzeImageAsync()...");
                VisualFeature[] visualFeatures = new VisualFeature[]
                {
                        VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description,
                        VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags
                };

                AnalysisResult analysisResult =
                    await VisionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);
                Debug.WriteLine(analysisResult);

                return analysisResult;
            }
        }

        // Speak the text
        private async void Speak(string text)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                _Speak(text);
            }
            );
        }

        // Internal speak method
        private async void _Speak(string text)
        {
            MediaElement mediaElement = new MediaElement();
            SpeechSynthesizer synth = new SpeechSynthesizer();


            //foreach (VoiceInformation voice in SpeechSynthesizer.AllVoices)
            //{
            //    Debug.WriteLine(voice.DisplayName + ", " + voice.Description);
            //}

            // Initialize a new instance of the SpeechSynthesizer.
            SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);

            // Send the stream to the media object.
            mediaElement.SetSource(stream, stream.ContentType);
            mediaElement.Play();

            mediaElement.Stop();
            synth.Dispose();
        }

        private string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return "";
            var hostname =
                NetworkInformation.GetHostNames()
                    .SingleOrDefault(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            // the ip address
            return hostname?.CanonicalName;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Taking photo....");
            TakePhoto();
            button.IsEnabled = false;
        }
    }
}
