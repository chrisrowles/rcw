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
using Windows.Gaming.Input;
using Windows.Gaming.Input.Custom;
using Windows.Gaming.Input.ForceFeedback;
using System.ComponentModel;
using System.Runtime.CompilerServices;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RacingWheelTracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly object refLock = new object();

        private List<RawGameController> racingWheels = new List<RawGameController>();

        private RawGameController activeRacingWheel;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public MainPage()
        {
            this.InitializeComponent();
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void GetRacingWheels()
        {
            lock (refLock)
            {
                foreach (var racingWheel in RawGameController.RawGameControllers)
                {
                    bool racingWheelInList = racingWheels.Contains(racingWheel);

                    if (!racingWheelInList)
                    {
                        racingWheels.Add(racingWheel);
                    }
                }
            }
        }

        private ulong GetCurrentReading(RawGameController racingWheel)
        {
            bool[] buttonArray = new bool[racingWheel.ButtonCount];
            double[] axisArray = new double[racingWheel.AxisCount];
            GameControllerSwitchPosition[] switchArray = new GameControllerSwitchPosition[racingWheel.ButtonCount];

            return racingWheel.GetCurrentReading(buttonArray, switchArray, axisArray);
        }

        private RawGameController GetActiveRacingWheel()
        {
            return racingWheels.First();
        }

        private void ConnectToController(object sender, RoutedEventArgs e)
        {
            this.GetRacingWheels();

            if (racingWheels.Count > 0)
            {
                this.activeRacingWheel = GetActiveRacingWheel();
                this.ControllerName.Text = this.activeRacingWheel.DisplayName;
            } else
            {
                this.ControllerName.Text = "No controller detected.";
            }
        }
    }
}
