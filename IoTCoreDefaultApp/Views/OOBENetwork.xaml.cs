﻿using System;
using Windows.Devices.WiFi;
using Windows.Security.Credentials;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace IoTCoreDefaultApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OOBENetwork : Page
    {
        private NetworkPresenter networkPresenter;
        private bool Automatic = true;
        private string CurrentPassword = string.Empty;

        public OOBENetwork()
        {
            this.InitializeComponent();
            SetupEthernet();
            SetupWifi();
        }

        private void SetupEthernet()
        {
            var ethernetProfile = NetworkPresenter.GetDirectConnectionName();

            if (ethernetProfile.Equals("None found"))
            {
                NoneFoundText.Visibility = Visibility.Visible;
                DirectConnectionStackPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoneFoundText.Visibility = Visibility.Collapsed;
                DirectConnectionStackPanel.Visibility = Visibility.Visible;
            }
        }

        private async void SetupWifi()
        {
            networkPresenter = new NetworkPresenter();

            if (await NetworkPresenter.WifiIsAvailable())
            {
                var networks = await networkPresenter.GetAvailableNetworks();

                if (networks.Count > 0)
                {
                    WifiListView.ItemsSource = networks;

                    NoWifiFoundText.Visibility = Visibility.Collapsed;
                    WifiListView.Visibility = Visibility.Visible;
                    return;
                }
            }

            NoWifiFoundText.Visibility = Visibility.Visible;
            WifiListView.Visibility = Visibility.Collapsed;
        }

        private void WifiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;

            foreach(var item in e.RemovedItems)
            {
                var listViewItem = listView.ContainerFromItem(item) as ListViewItem;
                listViewItem.ContentTemplate = WifiInitialState;
            }

            foreach(var item in e.AddedItems)
            {
                Automatic = true;
                var listViewItem = listView.ContainerFromItem(item) as ListViewItem;
                listViewItem.ContentTemplate = WifiConnectState;
            }
        }

        private void ConnectButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            var network = button.DataContext as WiFiAvailableNetwork;
            if (NetworkPresenter.IsNetworkOpen(network))
            {
                ConnectToWifi(network, null, Window.Current.Dispatcher);
            }
            else
            {
                SwitchToItemState(network, WifiPasswordState);
            }
        }

        private async void ConnectToWifi(WiFiAvailableNetwork network, PasswordCredential credential, CoreDispatcher dispatcher)
        {
            var didConnect = credential == null ?
                networkPresenter.ConnectToNetwork(network, Automatic) :
                networkPresenter.ConnectToNetworkWithPassword(network, Automatic, credential);

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SwitchToItemState(network, WifiConnectingState);
            });

            if (await didConnect)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    NavigationUtils.NavigateToScreen(typeof(MainPage));
                });
            }
            else
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var item = SwitchToItemState(network, WifiInitialState);
                    item.IsSelected = false;
                });
            }
        }

        private void NextButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            PasswordCredential credential;

            if (string.IsNullOrEmpty(CurrentPassword))
            {
                credential = null;
            }
            else
            {
                credential = new PasswordCredential()
                {
                    Password = CurrentPassword
                };
            }

            var network = button.DataContext as WiFiAvailableNetwork;
            ConnectToWifi(network, credential, Window.Current.Dispatcher);
        }

        private void CancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            var item = SwitchToItemState(button.DataContext, WifiInitialState);
            item.IsSelected = false;
        }

        private ListViewItem SwitchToItemState(object dataContext, DataTemplate template)
        {
            var item = WifiListView.ContainerFromItem(dataContext) as ListViewItem;
            item.ContentTemplate = template;

            return item;
        }

        private void BackButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigationUtils.GoBack();
        }

        private void SkipButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(MainPage));
        }

        private void ConnectAutomaticallyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;

            Automatic = checkbox.IsChecked ?? false;
        }

        private void WifiPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            CurrentPassword = passwordBox.Password;
        }
    }
}