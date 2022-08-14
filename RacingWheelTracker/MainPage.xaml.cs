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
using System.Threading.Tasks;
using System.Text;

namespace RacingWheelTracker
{
    /// <summary>
    /// MonkaSteer (or some shit).
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly object refLock = new object();

        private List<RawGameController> controllers = new List<RawGameController>();

        private RawGameController activeController;

        private ulong currentReading;

        private bool[] controllerButtonMap;

        private double[] controllerAxisMap;

        private GameControllerSwitchPosition[] controllerSwitchMap;

        private Task pollingTask;

        private bool taskRunning = true;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void InitActiveController(object sender, RoutedEventArgs e)
        {
            this.DetectConnectedControllers();

            if (controllers.Count > 0)
            {
                this.activeController = controllers.First();
                this.ControllerDisplayName.Text = this.activeController.DisplayName;
            }
            else
            {
                this.ControllerDisplayName.Text = "No controller detected.";
            }
        }


        private void DetectConnectedControllers()
        {
            lock (refLock)
            {
                foreach (var controller in RawGameController.RawGameControllers)
                {
                    bool controllerInList = controllers.Contains(controller);

                    if (!controllerInList)
                    {
                        controllers.Add(controller);
                    }
                }
            }
        }

        private void InitControllerTracking(object sender, RoutedEventArgs e)
        {
            this.controllerButtonMap = new bool[this.activeController.ButtonCount];
            this.controllerAxisMap = new double[this.activeController.AxisCount];
            this.controllerSwitchMap = new GameControllerSwitchPosition[this.activeController.ButtonCount];

            this.pollingTask = Task.Run(() => PollController());
        }

        private void StartControllerTracking(object sender, RoutedEventArgs e)
        {
            this.taskRunning = true;
        }

        private void StopControllerTracking(object sender, RoutedEventArgs e)
        {
            this.taskRunning = false;
        }

        private async Task PollController()
        {
            while (true)
            {
                if (taskRunning)
                {
                    this.currentReading = this.activeController.GetCurrentReading(this.controllerButtonMap, this.controllerSwitchMap, this.controllerAxisMap);

                    this.ActiveReadingId.Text = this.currentReading.ToString();
                    this.CurrentThrottle.Text = "1";
                    this.CurrentBrake.Text = "1";
                    this.CurrentXAxis.Text = this.controllerAxisMap.First().ToString();
                }

                await Task.Delay(50);
            }
        }
    }
}
