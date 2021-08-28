using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using Twainsoft.Articles.Dnp.EsriMapping.Annotations;

namespace Twainsoft.Articles.Dnp.EsriMapping.Models
{
    public sealed class CustomerMapViewModel : INotifyPropertyChanged
    {
        private Map _map;
        private GraphicsOverlayCollection _graphicsOverlays;
        private GraphicsOverlay _locationsGraphicsOverlay;
        private GraphicsOverlay _routeAndStopsOverlay;
        private ObservableCollection<string> _directions;
        
        private Graphic _startGraphic;
        private Graphic _endGraphic;
        private Graphic _routeGraphic;

        private enum RouteBuilderStatus
        {
            NotStarted,
            SelectedStart,
            SelectedStartAndEnd
        }
        
        private RouteBuilderStatus _currentState = RouteBuilderStatus.NotStarted;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public GraphicsOverlayCollection GraphicsOverlays
        {
            get => _graphicsOverlays;
            set
            {
                _graphicsOverlays = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<string> Directions
        {
            get => _directions;
            set
            {
                _directions = value;
                OnPropertyChanged();
            }
        }

        public Map Map
        {
            get => _map;
            set
            {
                _map = value;
                OnPropertyChanged();
            }
        }
        
        public CustomerMapViewModel()
        {
            SetupMap();
            CreateGraphics();
            
            var startOutlineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 2);
            _startGraphic = new Graphic(null, new SimpleMarkerSymbol
                {
                    Style = SimpleMarkerSymbolStyle.Diamond,
                    Color = Color.Orange,
                    Size = 8,
                    Outline = startOutlineSymbol
                }
            );

            var endOutlineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 2);
            _endGraphic = new Graphic(null, new SimpleMarkerSymbol
                {
                    Style = SimpleMarkerSymbolStyle.Square,
                    Color = Color.Green,
                    Size = 8,
                    Outline = endOutlineSymbol
                }
            );

            _routeGraphic = new Graphic(null, new SimpleLineSymbol(
                SimpleLineSymbolStyle.Solid,
                Color.Blue,
                4
            ));
            
            _routeAndStopsOverlay.Graphics.AddRange(new [] { _startGraphic, _endGraphic, _routeGraphic });
        }
        
        private void SetupMap()
        {
            var vectorTiledLayer =
                new ArcGISVectorTiledLayer(
                    new Uri("https://basemaps-api.arcgis.com/arcgis/rest/services/styles/e3b2e5b2aa2b4c79b2ef083a6d570a16"));

            var trailsUri = new Uri(
                "https://services3.arcgis.com/GVgbJbqm8hXASVYi/ArcGIS/rest/services/Trails/FeatureServer/0"
            );
            
            var trailsLayer = new FeatureLayer(trailsUri);
            
            Map = new Map(new Basemap(vectorTiledLayer));
            Map.OperationalLayers.Add(trailsLayer);
        }
        
        private void CreateGraphics()
        {
            _locationsGraphicsOverlay = new GraphicsOverlay();
            _routeAndStopsOverlay = new GraphicsOverlay();

            GraphicsOverlays = new GraphicsOverlayCollection
            {
                _locationsGraphicsOverlay,
                _routeAndStopsOverlay
            };
        }

        public void AddLocationPoint([CanBeNull] MapPoint displayLocation, [CanBeNull] string label)
        {
            var markerSymbol = new SimpleMarkerSymbol
            {
                Style = SimpleMarkerSymbolStyle.Circle,
                Color = Color.LightBlue,
                Size = 25.0,
                
                Outline = new SimpleLineSymbol
                {
                    Style = SimpleLineSymbolStyle.Solid,
                    Color = Color.DarkGray,
                    Width = 2.0
                }
            };

            var pointGraphic = new Graphic(displayLocation, markerSymbol);

            _locationsGraphicsOverlay.Graphics.Add(pointGraphic);
        }

        private async Task FindRoute()
        {
            var routeTask = 
                await RouteTask.CreateAsync(
                    new Uri("https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World"));
         
            var routeParameters = await routeTask.CreateDefaultParametersAsync();

            routeParameters.ReturnRoutes = true;
            routeParameters.ReturnDirections = true;
            routeParameters.DirectionsLanguage = "de";

            var stops = new[] { _startGraphic.Geometry, _endGraphic.Geometry }.Select(g => new Stop((MapPoint)g));
            routeParameters.SetStops(stops);
            
            var result = await routeTask.SolveRouteAsync(routeParameters);
            
            if (result?.Routes?.FirstOrDefault() is Route routeResult)
            {
                _routeGraphic.Geometry = routeResult.RouteGeometry;
                Directions = new ObservableCollection<string>(routeResult.DirectionManeuvers
                    .Select(maneuver => $"{maneuver.DirectionText} {Math.Round(maneuver.Length)}m"));
                _currentState = RouteBuilderStatus.NotStarted;
            }
            else
            {
                ResetState();
                Debug.WriteLine("No Route Found!");
            }
        }
        
        public void ResetState()
        {
            _startGraphic.Geometry = null;
            _endGraphic.Geometry = null;
            _routeGraphic.Geometry = null;
            Directions = null;
            _currentState = RouteBuilderStatus.NotStarted;
        }
        
        public async Task HandleTap(MapPoint tappedPoint)
        {
            switch (_currentState)
            {
                case RouteBuilderStatus.NotStarted:
                    ResetState();
                    _startGraphic.Geometry = tappedPoint;
                    _currentState = RouteBuilderStatus.SelectedStart;
                    break;
                case RouteBuilderStatus.SelectedStart:
                    _endGraphic.Geometry = tappedPoint;
                    _currentState = RouteBuilderStatus.SelectedStartAndEnd;
                    await FindRoute();
                    break;
                case RouteBuilderStatus.SelectedStartAndEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}