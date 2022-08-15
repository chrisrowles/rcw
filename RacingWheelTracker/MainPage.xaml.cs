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
        public string controllerDisplayName
        {
            get
            {
                return ControllerDisplayLabel.Text;
            }
            set
            {
                ControllerDisplayLabel.Text = value;
            }
        }

        public WebView controllerTrackingWebview
        {
            get
            {
                return controllerTrackingFrame;
            }
        }

        public string controllerCurrentThrottleValueField
        {
            get
            {
                return CurrentThrottle.Text;
            }
            set
            {
                CurrentThrottle.Text = value;
            }
        }

        public string controllerCurrentBrakeValueField
        {
            get
            {
                return CurrentBrake.Text;
            }
            set
            {
                CurrentBrake.Text = value;
            }
        }

        public string controllerCurrentXAxisValueField
        {
            get
            {
                return CurrentXAxis.Text;
            }
            set
            {
                CurrentXAxis.Text = value;
            }
        }

        public string controllerCurrentXAxisDirectionField
        {
            get
            {
                return CurrentXAxisDirection.Text;
            }
            set
            {
                CurrentXAxisDirection.Text = value;
            }
        }

        public string activeReadingIdField
        {
            get
            {
                return ActiveReadingId.Text;
            }
            set
            {
                ActiveReadingId.Text = value;
            }
        }

        public ControllerManager controllerManager; 

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(750, 650);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            controllerManager = new ControllerManager(this);
        }

        private void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            controllerManager.InitActiveController();
        }
        
        private void InitTrackingButtonClick(object sender, RoutedEventArgs e)
        {
            controllerManager.InitControllerTracking();
        }

        private void StartControllerTrackingButtonClick(object sender, RoutedEventArgs e)
        {
            controllerManager.taskRunning = true; 
        }

        private void StopControllerTrackingButtonClick(object sender, RoutedEventArgs e)
        {
            controllerManager.taskRunning = false;
        }
    }
}
