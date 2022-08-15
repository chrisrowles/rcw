using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Core;


namespace RacingWheelTracker
{
    public class ControllerManager
    {
        /// <summary>
        /// List to contain detected controllers.
        /// </summary>
        public List<RawGameController> controllers = new List<RawGameController>();

        /// <summary>
        /// The active controller.
        /// </summary>
        public RawGameController activeController;

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
        public ulong currentReading;

        /// <summary>
        /// Array to map controller button states.
        /// i.e. {'BUTTON_0': false, 'BUTTON_1': false}...
        /// </summary>
        public bool[] controllerButtonMap;

        /// <summary>
        /// Array to map controller axis state
        /// i.e. {0.0, 0.0, 0.0, 0.0}
        /// </summary>
        public double[] controllerAxisMap;

        /// <summary>
        /// Active contoller x-axis value.
        /// </summary>
        public double xAxisCurrentValue;

        /// <summary>
        /// Active controller switch position mapping.
        /// i.e. {'Center': 0, 'Down': 5}
        /// </summary>
        public GameControllerSwitchPosition[] controllerSwitchMap;

        /// <summary>
        /// Main page
        /// TODO better way
        /// </summary>
        public MainPage mainPage;

        /// <summary>
        /// Flag to determine whether or not the polling
        /// task is running for the active controller.
        /// </summary>
        public bool taskRunning = true;

        /// <summary>
        /// Lock for looping through controllers.
        /// A lock is required due to the possibility of controller state changes
        /// i.e. connects and disconnects.
        /// </summary>
        private readonly object refLock = new object();

        public ControllerManager(MainPage mainPage)
        {
            this.mainPage = mainPage;
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
        public void InitActiveController()
        {
            DetectConnectedControllers();

            Uri uri = new Uri("http://127.0.0.1:8086");
            mainPage.controllerTrackingWebview.Navigate(uri);

            if (controllers.Count > 0)
            {
                activeController = controllers.First();
                mainPage.controllerDisplayName = activeController.DisplayName;
            }
            else
            {
                mainPage.controllerDisplayName = "No controller detected.";
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
        public void InitControllerTracking()
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
                    await mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        currentReading = activeController.GetCurrentReading(controllerButtonMap, controllerSwitchMap, controllerAxisMap);

                        mainPage.activeReadingIdField = currentReading.ToString();
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
        /// Set the active controller's mapping properties.
        /// </summary>
        private async void SetActiveControllerMaps()
        {
            var xAxisNextValue = controllerAxisMap.ElementAt(0);

            mainPage.controllerCurrentThrottleValueField = Math.Round(controllerAxisMap.ElementAt(2), 4).ToString();
            mainPage.controllerCurrentBrakeValueField = Math.Round(controllerAxisMap.ElementAt(3), 4).ToString();
            mainPage.controllerCurrentXAxisValueField = Math.Round(xAxisNextValue - 0.5022, 4).ToString();

            if (xAxisNextValue != xAxisCurrentValue)
            {
                mainPage.controllerCurrentXAxisDirectionField = xAxisNextValue > xAxisCurrentValue
                    ? "Right"
                    : "Left";
            }

            xAxisCurrentValue = xAxisNextValue;

            string dispatchAxisUpdateEvent = String.Format("const event = new CustomEvent('axis-update', {{ detail: {0} }}); document.dispatchEvent(event);", xAxisCurrentValue);
            await mainPage.controllerTrackingWebview.InvokeScriptAsync("eval", new string[] { dispatchAxisUpdateEvent });
        }
    }
}
