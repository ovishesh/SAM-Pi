using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GrovePi;
using GrovePi.Sensors;
using PiMe.Helpers;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiMe
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Pi _pi;
        private DispatcherTimer _timer;
        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private bool isCameraOn = false;

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;

            _pi = new Pi();
            _pi.DisplayText("Click to Capture", Colors.White);
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //await InitPreview();

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 1);
            CameraOn();

            _timer.Start();
            _timer.Tick += _timer_Tick;


        }

        private async Task CameraOn()
        {
            isCameraOn = true;
            _pi.SetLed(isCameraOn);

            //Video and Audio is initialized by default  
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            // Start Preview                  
            PreviewElement.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();
        }

        //private async Task CameraOff()
        //{
        //    //await mediaCapture.StopPreviewAsync();

        //    isCameraOn = false;
        //    _pi.SetLed(isCameraOn);
        //    _pi.DisplayText("Camera is Off", Colors.White);
        //}

        private async void _timer_Tick(object sender, object e)
        {
            Debug.WriteLine("Timer tick");
            try
            {
                if (_pi.IsButtonPressed())
                {
                    Debug.WriteLine("Taking photo....");
                    _pi.DisplayText("Button pressed. Taking your photo...", Colors.White);
                    TakePhoto();

                }
            }
            finally
            {
            }
        }

        private async void TakePhoto()
        {
            try
            {
                Debug.WriteLine("TakePhoto()");
                _pi.DisplayText("Taking your photo...", Colors.Pink);

                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                var imageProperties = ImageEncodingProperties.CreateJpeg();

                var resolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).Select(x => x as VideoEncodingProperties);

                ////stop preview
                await mediaCapture.StopPreviewAsync();

                ////highest res possible
                var maxRes = resolutions.OrderByDescending(x => x.Height * x.Width).FirstOrDefault();
                //await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, maxRes);
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);

                var photoStream = await photoFile.OpenReadAsync();
                var bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;

                PreviewElement.Source = null;

                var imageAnalysisResult = await UploadAndAnalyzeImage(photoFile.Path);

                Debug.WriteLine("Recognizing...");
                _pi.DisplayText(imageAnalysisResult.Description.Captions[0].Text, Colors.Pink);

                Speak("I see, " + imageAnalysisResult.Description.Captions[0].Text);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                await CameraOn();
                _pi.DisplayText("Click to Capture", Colors.White);
                Debug.WriteLine("Timer on.");
                _timer.Start();
                Debug.WriteLine("Recognizing...");
            }
        }

        private async Task<AnalysisResult> UploadAndAnalyzeImage(string imageFilePath)
        {
            //
            // Create Project Oxford Vision API Service client
            //
            Speak("Ok, I am analysing your photo!");
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
            SpeechSynthesizer synth = new SpeechSynthesizer()
            {
                Voice = SpeechSynthesizer.AllVoices[1]
            };


            //foreach (VoiceInformation voice in SpeechSynthesizer.AllVoices)
            //{
            //    Debug.WriteLine(voice.DisplayName + ", " + voice.Description + ", " + voice.Gender);
            //}

            // Initialize a new instance of the SpeechSynthesizer.
            //synth.Voice.Gender = VoiceGender.Female;

            SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);

            // Send the stream to the media object.
            mediaElement.SetSource(stream, stream.ContentType);
            mediaElement.Play();

            mediaElement.Stop();
            synth.Dispose();
        }
    }
}
