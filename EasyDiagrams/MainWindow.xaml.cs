using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EasyDiagrams
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Timers.Timer quietInterval;

        public MainWindow()
        {
            InitializeComponent();
            Title = "Easy Diagrams";
            quietInterval = new System.Timers.Timer(1500);
            quietInterval.Elapsed += quietInterval_Elapsed;
            quietInterval.Start();
        }


        void quietInterval_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (SourceArea == null || DrawArea == null) { return; }
            quietInterval.Stop();

            // here is where we could generate the diagram...
            String txt = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                txt = SourceArea.Text;
            });

            // here's where we would produce the diagram...
            // now invoke the WPF thread to draw it...
            var diagram = Parser.Parse(new Tokenizer(txt));
            var renderer = new Renderer(DrawArea, diagram);

            Application.Current.Dispatcher.Invoke(() =>
            {
                renderer.Draw();
                SrcErrorState = diagram.HasErrors;
            });
        }


        private void TheTextChanged(object sender, TextChangedEventArgs e)
        {
            if (quietInterval == null) { return; }
            quietInterval.Stop();
            quietInterval.Start();
        }

        // maintain the error state, which sets the background color of the source editing area.
        private bool srcErrorState = false;
        public bool SrcErrorState
        {
            get { return srcErrorState; }

            set
            {
                srcErrorState = value;
                SourceArea.Background = new SolidColorBrush(value ? Colors.Pink : Colors.White);
            }
        }

        private void PNG_Save(object sender, RoutedEventArgs e)
        {
            // first get a filename ....
            var sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.FileName = "Diagram";
            sfd.DefaultExt = ".png";
            sfd.Filter = "PNG Files|*.png";
            Nullable<bool> result = sfd.ShowDialog();
            string? filename = null;
            if (result.HasValue && result.Value)
            {
                filename = sfd.FileName;
            }

            if (filename == null) return;


            // now save the file...
            try
            {
                var csize = DrawArea.GetAggregateSize();
                var rect = new Rect(csize);
                rect.Width += Renderer.MARGIN;
                double dpi = 150.0;
                rect.Width = rect.Width * dpi / 96.0;
                rect.Height = rect.Height * dpi / 96.0;
                var rtb = new RenderTargetBitmap((int)rect.Width, (int)rect.Height, dpi, dpi, PixelFormats.Default);
                rtb.Render(DrawArea);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using( var fs = System.IO.File.Create(filename) ) {
                    encoder.Save(fs);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
