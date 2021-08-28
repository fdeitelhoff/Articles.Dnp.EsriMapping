using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI.Controls;
using Newtonsoft.Json;
using Twainsoft.Articles.Dnp.EsriMapping.Models;

namespace Twainsoft.Articles.Dnp.EsriMapping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly CustomerMapViewModel _customerMapViewModel;
        private readonly HttpClient _client;
        
        public MainWindow()
        {
            InitializeComponent();

            _customerMapViewModel = new CustomerMapViewModel();
            DataContext = _customerMapViewModel;
            _client = new HttpClient();
        }
        
        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var mapCenterPoint
                = CoordinateFormatter.FromLatitudeLongitude("51°30'51.2784''N 7°28'6.3444''E", SpatialReferences.Wgs84);
            
            await MainMapView.SetViewpointAsync(new Viewpoint(mapCenterPoint, 100000));
        }

        private async Task<GeocodeResult> LocateAddress(string address)
        {
            var locatorTask = new LocatorTask(new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer"));

            var geocodeParams = new GeocodeParameters
            {
                CountryCode = "Germany",
                OutputLanguage = new System.Globalization.CultureInfo("de-DE")
            };
            
            var results = await locatorTask.GeocodeAsync(address, geocodeParams);
            
            return results?.FirstOrDefault();
        }

        private async void DisplayAddresses_OnClick(object sender, RoutedEventArgs e)
        {
            var contactsResult = await _client.GetStringAsync("https://localhost:5001/api/Contacts");
            var contacts = new JsonSerializer().Deserialize<List<Contact>>(new JsonTextReader(new StringReader(contactsResult)));
            
            if (contacts == null) return;
            
            foreach (var contact in contacts)
            {
                var result = await LocateAddress($"{contact.Street} {contact.StreetNo}, {contact.ZipCode} {contact.City}");
                
                if (result?.DisplayLocation != null)
                {
                    await MainMapView.SetViewpointAsync(new Viewpoint(result.DisplayLocation, scale: 5000));
                    _customerMapViewModel.AddLocationPoint(result.DisplayLocation, result.Label);
                }
            }
        }

        private async void MainMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                await _customerMapViewModel.HandleTap(e.Location);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message);
            }
        }

        private void MainMapView_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _customerMapViewModel.ResetState();
        }
    }
}