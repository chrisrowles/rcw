using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Gaming.Input;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace RacingWheelTracker
{
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Lock for looping through controllers.
        /// A lock is required due to the possibility of controller state changes
        /// i.e. connects and disconnects.
        /// </summary>
        private readonly object refLock = new object();

        /// <summary>
        /// List to contain detected controllers.
        /// </summary>
        private List<RawGameController> controllers = new List<RawGameController>();

        /// <summary>
        /// The active controller.
        /// </summary>
        private RawGameController activeController;


        /// <summary>
        /// Handler for detecting controller connection.
        /// </summary>
        private static event EventHandler<RawGameController> ControllerAdded;

        /// <summary>
        /// Handler for detecting controller disconnection.
        /// </summary>
        private static event EventHandler<RawGameController> ControllerRemoved;

        /// <summary>
        /// Timestamp of the active controller's current reading.
        /// </summary>
        private ulong currentReading;

        /// <summary>
        /// Array to map controller button states.
        /// i.e. {'BUTTON_0': false, 'BUTTON_1': false}...
        /// </summary>
        private bool[] controllerButtonMap;

        /// <summary>
        /// Array to map controller axis state
        /// i.e. {0.0, 0.0, 0.0, 0.0}
        /// </summary>
        private double[] controllerAxisMap;

        /// <summary>
        /// Active contoller x-axis value.
        /// </summary>
        private double xAxisCurrentValue;

        /// <summary>
        /// Active controller switch position mapping.
        /// i.e. {'Center': 0, 'Down': 5}
        /// </summary>
        private GameControllerSwitchPosition[] controllerSwitchMap;

        /// <summary>
        /// Flag to determine whether or not the polling
        /// task is running for the active controller.
        /// </summary>
        private bool taskRunning = true;

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(750, 650);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        /// <summary>
        /// Handle controller connect event.
        /// </summary>
        /// <param name="controller"></param>
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

        /// <summary>
        /// Handle controller disconnect event.
        /// </summary>
        /// <param name="controller"></param>
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

        /// <summary>
        /// Initialise the active controller.
        /// This method begins by detecting all connected game controllers.
        /// It then sets the activeController and updates the UI controller
        /// name field.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void InitActiveController(object sender, RoutedEventArgs e)
        {
            DetectConnectedControllers();

            Uri uri = new Uri("http://127.0.0.1:8086");
            WebViewM.Navigate(uri);

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

        /// <summary>
        /// Initialise active controller tracking.
        /// This method begins by setting the values of the active
        /// controller mapping properties. It then dispatches the
        /// polling task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void InitControllerTracking(object sender, RoutedEventArgs e)
        {
            controllerButtonMap = new bool[activeController.ButtonCount];
            controllerAxisMap = new double[activeController.AxisCount];
            controllerSwitchMap = new GameControllerSwitchPosition[activeController.ButtonCount];

            SetActiveControllerMaps();

            Task.Run(() => TrackActiveController());
        }

        /// <summary>
        /// Active controller tracking task.
        /// This method first checks to see if the task is running.
        /// If it is, then it fetches the current reading of the
        /// active controller and updates the controller mapping
        /// properties accordingly.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Detect connected controllers.
        /// Loops through the RawGameControllers object and
        /// adds connected controllers to the list.
        /// </summary>
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

        /// <summary>
        /// Set the task to true to start running it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartControllerTracking(object sender, RoutedEventArgs e)
        {
            taskRunning = true;
        }


        /// <summary>
        /// Set the task to true to stop running it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopControllerTracking(object sender, RoutedEventArgs e)
        {
            taskRunning = false;
        }

        /// <summary>
        /// Set the active controller's mapping properties.
        /// </summary>
        private async void SetActiveControllerMaps()
        {
            var xAxisNextValue = controllerAxisMap.ElementAt(0);

            CurrentThrottle.Text = Math.Round(controllerAxisMap.ElementAt(2), 4).ToString();
            CurrentBrake.Text = Math.Round(controllerAxisMap.ElementAt(3), 4).ToString();
            CurrentXAxis.Text = Math.Round(xAxisNextValue - 0.5022, 4).ToString();

            if (xAxisNextValue != xAxisCurrentValue)
            {
                CurrentXAxisDirection.Text = xAxisNextValue > xAxisCurrentValue
                    ? "Right"
                    : "Left";
            }

            xAxisCurrentValue = xAxisNextValue;

            string dispatchAxisUpdateEvent = String.Format("const event = new CustomEvent('axis-update', {{ detail: {0} }}); document.dispatchEvent(event);", xAxisCurrentValue);
            await WebViewM.InvokeScriptAsync("eval", new string[] { dispatchAxisUpdateEvent });
        }
    }
}
