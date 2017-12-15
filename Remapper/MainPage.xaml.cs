using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Search;
using Windows.UI.Popups;
using Windows.Storage.FileProperties;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Remapper
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        UnmappedMarkers unmappedMarkers = new UnmappedMarkers();
        StorageFolder markerFolder;
        int currentMarkerIndex = -1;
        DateTime currentReadStart;
        bool readingGPS = false;
        SerialDevice gpsSerial = null;
        DataReader dataReader = null;
        private CancellationTokenSource readCancellationTokenSource;
        CoordinateReadSet readSet = null;

        
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            markerFolder = await KnownFolders.PicturesLibrary.GetFolderAsync("CemRemap");
            await unmappedMarkers.GetMarkers(markerFolder);

            if (unmappedMarkers.Markers.Count > 0)
            {
                currentMarkerIndex = 0;
                UpdateCurrentMarker();
            }

            ConnectGPS(); 
        }

        private async void ConnectGPS()
        {
            string qFilter = SerialDevice.GetDeviceSelector("COM3");
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(qFilter);

            if (devices.Any())
            {
                string deviceId = devices.First().Id;

                try
                {
                    await OpenPort(deviceId);
                } catch (Exception e)
                {
                    SetAlert("GPS not connected");
                }
            }

            readCancellationTokenSource = new CancellationTokenSource();
        }


        private async Task OpenPort(string deviceId)
        {
            gpsSerial = await SerialDevice.FromIdAsync(deviceId);

            if (gpsSerial != null)
            {
                gpsSerial.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                gpsSerial.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                gpsSerial.BaudRate = 9600;
                gpsSerial.Parity = SerialParity.None;
                gpsSerial.StopBits = SerialStopBitCount.One;
                gpsSerial.DataBits = 8;
                gpsSerial.Handshake = SerialHandshake.None;
                ClearAlert();
                statusAndData.Text = "Communicating with GPS\r\n";
            }
        }


        private async Task Listen()
        {
            try
            {
                if (gpsSerial != null)
                {
                    dataReader = new DataReader(gpsSerial.InputStream);
                    await ReadAsync(readCancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                //statusAndData.Text = ex.Message;
            }
            finally
            {
                if (dataReader != null)    // Cleanup once complete
                {
                    dataReader.DetachStream();
                    dataReader = null;
                }
            }
        }


        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 256;  // only when this buffer would be full next code would be executed

            dataReader.InputStreamOptions = InputStreamOptions.Partial;

            loadAsyncTask = dataReader.LoadAsync(ReadBufferLength).AsTask(cancellationToken);   // Create a task object

            UInt32 bytesRead = await loadAsyncTask;    // Launch the task and wait until buffer would be full

            if (bytesRead > 0)
            {
                string gpsData = dataReader.ReadString(bytesRead);
                ProcessNMEA(gpsData);
                appendToStatusAndData(gpsData);
            }
        }

        private void appendToStatusAndData(string data)
        {
            string newstatusAndData = statusAndData.Text + data;

            string[] lines = newstatusAndData.Split(new string[] { "\r\n" }, 1000, StringSplitOptions.RemoveEmptyEntries); // 1000 becuase always want all of them, but no good string-based option short of max int....
            if (lines.Length > 10)
            {
                statusAndData.Text = string.Join("\r\n", lines.Skip(lines.Length - 10).Take(10));
            }
            else
            {
                statusAndData.Text = string.Join("\r\n", lines);
            }
        }

        private void ProcessNMEA(string nmea)
        {
            int start = nmea.IndexOf("$GPGGA");
            if (start >= 0) {
                int endLine = nmea.IndexOf("\n", start);
                CoordinateRead cRead = CoordinateRead.FromGGA(endLine >= 0 ? nmea.Substring(start, endLine - start) : nmea.Substring(start), currentReadStart);
                if (cRead != null)
                {
                    readSet.AddRead(cRead);
  //                  statusAndData.Text += cRead.ToString() + "\r\n";
                }
            }
            
        }

        private void CancelReadTask()
        {
            if (readCancellationTokenSource != null)
            {
                if (!readCancellationTokenSource.IsCancellationRequested)
                {
                    readCancellationTokenSource.Cancel();
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            CancelReadTask();
            if (gpsSerial != null)
            {
                gpsSerial.Dispose();
            }
            gpsSerial = null;
        }


        private void NextMarker_Click(object sender, RoutedEventArgs e)
        {
            if (unmappedMarkers.Markers.Count > 1)
            {
                if (currentMarkerIndex < unmappedMarkers.Markers.Count - 1)
                {
                    currentMarkerIndex++;
                }
                else
                {
                    currentMarkerIndex = 0;
                }
                UpdateCurrentMarker();
            }
        }

        private void UpdateCurrentMarker()
        {
            CurrentMarkerImage.Source = unmappedMarkers.Markers[currentMarkerIndex].FullImage;
            MarkerID.Text = "Pictured Marker ID: " + unmappedMarkers.Markers[currentMarkerIndex].MarkerID;
        }

        private void PrevMarker_Click(object sender, RoutedEventArgs e)
        {
            if (unmappedMarkers.Markers.Count > 1)
            {
                if (currentMarkerIndex > 0)
                {
                    currentMarkerIndex--;
                }
                else
                {
                    currentMarkerIndex = unmappedMarkers.Markers.Count - 1;
                }
                UpdateCurrentMarker();
            }
        }

        private async void ToggleGPSRead_Click(object sender, RoutedEventArgs e)
        {
            if (readingGPS)
            {
                CancelReadTask(); 
                readingGPS = false;
                ToggleGPSRead.Content = "Start Reading Coordinates";
            }
            else
            {
                if (gpsSerial == null)
                {
                    SetAlert("Attempting to Connect to GPS");
                    ConnectGPS();
                }
                if (gpsSerial != null)
                {
                    readingGPS = true;
                    currentReadStart = DateTime.Now.ToUniversalTime();
                    readSet = new CoordinateReadSet();
                    ToggleGPSRead.Content = "Cancel Coordinate Read";
                    await ReadGPS();
                    readingGPS = false;
                    ToggleGPSRead.Content = "Start Reading Coordinates";
                    if (readSet.Count == CoordinateReadSet.ReadSetSize) statusAndData.Text = "Coordinate Read Complete";
                    else statusAndData.Text = "Coordinate Read Canceled";
                }
            }
        }

        private async Task ReadGPS()
        {
            while (readSet.Count < CoordinateReadSet.ReadSetSize && readingGPS)
            {
                await Listen();
                UpdateCoordinates();
            }
        }

        private void UpdateCoordinates()
        {
            Latitude.Text = "Measured Latitude: " + (readSet.LatEstimate != null ? ((decimal)readSet.LatEstimate).ToString("000.000000000") : "");
            Longitude.Text = "Measured Longitude: " + (readSet.LongEstimate != null ? ((decimal)readSet.LongEstimate).ToString("000.000000000") : "");
            Error.Text = "LatRange: " + (readSet.LatError != null ? ((decimal)readSet.LatError).ToString("0.00") + "m" : "") + " " +
                         "LongRage: " + (readSet.LongError != null ? ((decimal)readSet.LongError).ToString("0.00") + "m" : "") + "    "
                         + readSet.Count.ToString();
        }

        private void SetAlert(string alertText)
        {
            Alert.Text = alertText;
            AlertBack.Width = Alert.Width < 396 ? Alert.Width + 10 : 406;
        }

        private void ClearAlert()
        {
            Alert.Text = "";
            AlertBack.Width = 0;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ControlPanel.Orientation = (this.ActualHeight > this.ActualWidth) ? Orientation.Horizontal : Orientation.Vertical;
        }
    }
}
