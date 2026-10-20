using Framework.Messaging;
using Framework.Model;
using Framework.Themes;
using Framework.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Framework.Utilities.DrawingHelper;
using static Framework.View.UiHelper;

namespace Framework.View
{
    public partial class MainWindow : Window
    {
        private readonly MainVM _mainVM;
        private readonly IMessageBus _messageBus;
        private Point _dragStart;
        private bool _isDragging = false;
        private System.Windows.Shapes.Rectangle _selectionRectangle;

        public MainWindow()
        {
            InitializeComponent();

            _messageBus = MessageBus.Instance;
            _mainVM = (MainVM)DataContext;

            _messageBus.Subscribe<RefreshOverlaysMessage>(this, _ => Update());
            Closed += (s, e) => _messageBus.Unsubscribe<RefreshOverlaysMessage>(this);

            UpdateThemeMenu();
        }

        #region Selection markers
        public void Update()
        {
            double scaleValue = sliderZoom.Value;

            RemoveUiElements(initialImageCanvas, processedImageCanvas);
            DrawUiElements(initialImageCanvas, processedImageCanvas, scaleValue, _mainVM);
        }
        #endregion

        #region Mouse input
        private void ImageMouseMove(object sender, MouseEventArgs e)
        {
            var canvasType = sender == initialImage ? ImageCanvasType.Initial : ImageCanvasType.Processed;
            _messageBus.Publish(new MouseMoveMessage(e.GetPosition((IInputElement)sender), canvasType));

            if (!_isDragging || sender != initialImage)
                return;

            Point current = e.GetPosition(initialImage);

            if (_selectionRectangle != null)
                RemoveUiElement(initialImageCanvas, _selectionRectangle);

            Point leftTop = new Point(
                Math.Min(_dragStart.X, current.X),
                Math.Min(_dragStart.Y, current.Y));

            Point rightBottom = new Point(
                Math.Max(_dragStart.X, current.X),
                Math.Max(_dragStart.Y, current.Y));

            _selectionRectangle = DrawRectangle(
                initialImageCanvas,
                leftTop,
                rightBottom,
                2,
                System.Windows.Media.Brushes.Red,
                sliderZoom.Value);
        }

        private void ImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender != initialImage)
                return;

            if (_mainVM.GetImageSize(ImageCanvasType.Initial) == null)
                return;

            _dragStart = e.GetPosition(initialImage);
            _isDragging = true;

            _mainVM.ClearSelectedPoints();
            _mainVM.SelectPoint(_dragStart);

            initialImage.CaptureMouse();
        }

        private void ImageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging)
                return;

            Point dragEnd = e.GetPosition(initialImage);

            _isDragging = false;
            initialImage.ReleaseMouseCapture();

            _mainVM.SelectPoint(dragEnd);
        }

        private void ImageMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mainVM.ClearSelectedPoints();
        }
        #endregion

        #region Zoom and scrolling
        private void SliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mainVM == null) return;

            double scaleValue = sliderZoom.Value;

            UpdateShapesProperties(initialImageCanvas, scaleValue);
            UpdateShapesProperties(processedImageCanvas, scaleValue);

            _mainVM.InitialCanvasWidth = initialImage.ActualWidth * scaleValue;
            _mainVM.InitialCanvasHeight = initialImage.ActualHeight * scaleValue;
            _mainVM.ProcessedCanvasWidth = processedImage.ActualWidth * scaleValue;
            _mainVM.ProcessedCanvasHeight = processedImage.ActualHeight * scaleValue;
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == initialImageCanvasScrollViewer)
            {
                processedImageCanvasScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                processedImageCanvasScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else
            {
                initialImageCanvasScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                initialImageCanvasScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }
        #endregion

        #region Theme
        private void UpdateThemeMenu()
        {
            string themeName = ThemeRegistry.GetName(_mainVM.Theme);
            foreach (MenuItem item in themeMenu.Items)
                item.IsChecked = item.Header.ToString() == themeName;
        }

        private void ThemeModeSelector(object sender, RoutedEventArgs e)
        {
            string selectedTheme = ((MenuItem)sender).Header.ToString();
            _mainVM.SetThemeMode(selectedTheme);
            UpdateThemeMenu();

            Properties.Settings.Default.Theme = selectedTheme;
            Properties.Settings.Default.Save();
        }
        #endregion
    }
}