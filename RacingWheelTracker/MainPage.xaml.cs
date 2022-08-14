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
using Windows.UI.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text;
using Windows.UI.ViewManagement;

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

        private static event EventHandler<RawGameController> ControllerAdded;

        private static event EventHandler<RawGameController> ControllerRemoved;

        private ulong currentReading;

        private bool[] controllerButtonMap;

        private double[] controllerAxisMap;

        private double xAxisCurrentValue;

        private string xAxisDirection;

        private GameControllerSwitchPosition[] controllerSwitchMap;

        private bool taskRunning = true;

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(1000, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        public void OnControllerAdded(RawGameController controller)
        {
            EventHandler<RawGameController> handler = ControllerAdded;
            handler?.Invoke(this, controller);

            bool controllerInList = controllers.Contains(controller);

            if (!controllerInList)
            {
                controllers.Add(controller);
            }
        }

        public void OnControllerRemoved(RawGameController controller)
        {
            EventHandler<RawGameController> handler = ControllerRemoved;
            handler?.Invoke(this, controller);

            bool controllerInList = controllers.Contains(controller);

            if (!controllerInList)
            {
                controllers.Remove(controller);
            }
        }

        public void InitActiveController(object sender, RoutedEventArgs e)
        {
            DetectConnectedControllers();

            if (controllers.Count > 0)
            {
                activeController = controllers.First();
                ControllerDisplayName.Text = activeController.DisplayName;
            }
            else
            {
                ControllerDisplayName.Text = "No controller detected.";
            }
        }

        public void InitControllerTracking(object sender, RoutedEventArgs e)
        {
            controllerButtonMap = new bool[activeController.ButtonCount];
            controllerAxisMap = new double[activeController.AxisCount];
            controllerSwitchMap = new GameControllerSwitchPosition[activeController.ButtonCount];

            SetActiveControllerMaps();

            Task.Run(() => TrackActiveController());
        }

        private async Task TrackActiveController()
        {
            while (true)
            {
                if (taskRunning)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        currentReading = activeController.GetCurrentReading(controllerButtonMap, controllerSwitchMap, controllerAxisMap);

                        ActiveReadingId.Text = currentReading.ToString();
                        SetActiveControllerMaps();
                    });
                }

                await Task.Delay(50);
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

        private void StartControllerTracking(object sender, RoutedEventArgs e)
        {
            taskRunning = true;
        }

        private void StopControllerTracking(object sender, RoutedEventArgs e)
        {
            taskRunning = false;
        }

        private void SetActiveControllerMaps()
        {
            var xAxisNextValue = controllerAxisMap.ElementAt(0);

            CurrentThrottle.Text = controllerAxisMap.ElementAt(2).ToString();
            CurrentBrake.Text = controllerAxisMap.ElementAt(3).ToString();
            CurrentXAxis.Text = xAxisNextValue.ToString();

            xAxisDirection = xAxisNextValue > xAxisCurrentValue
                ? "Right"
                : "Left";

            xAxisCurrentValue = xAxisNextValue;
        }

        private void SetControllerDirection()
        { }
    }
}
