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
    class Renderer
    {
        private Diagram d;
        private Canvas c;
        private FontFamily fontFamily;
        private static Size HUGE = new Size(Double.MaxValue, double.MaxValue);
        
        // some constants 
        public static double MARGIN = 10;
        private static double TEXTSIZE = 14;
        private static double BOXSEP = TEXTSIZE * 10;
        private static double ARROWSEP = TEXTSIZE * 2.5;
        private static double AHEADSIZE = TEXTSIZE*0.8;

        // rendering information...
        private Dictionary<string, double> actorPlacement; // centerline placement

        private double topOfDiagram;  // where should we start drawing under the title?
        private double topOfLines;    // where do the lines go under the actor boxes?
        private double boxHeight;     // how tall is the largest actor box?
        private double boxWidth;      // how wide is the widest actor box?
        private double computedBoxSep; // how far apart are the actor lines?
        
        // I'd make the following two static, but it has to be initialized in the WPF thread.
        private DoubleCollection? DASHES = null;
        private Brush? translucentBrush = null;

        public Renderer(Canvas canvas, Diagram diagram) { 
            c = canvas;  
            d = diagram;
            fontFamily = new FontFamily("Helvetica, Arial");
            actorPlacement = new Dictionary<string, double>();
        }

        public void Draw()
        {
            DASHES = new DoubleCollection() { 2 };
            translucentBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));

            c.Children.Clear();
            var title = titleWords();
            topOfDiagram = Renderer.MARGIN + title.DesiredSize.Height + Renderer.MARGIN;

            if (d.Actors.Count > 0)
            {
                actorBoxes();
                arrowLines();
            }
            else
            {
                // put a dummy value in actorplacement to make things run
                actorPlacement.Add("Dummy", 0.0);
            }
            centerTitle(title);
        }

        private void drawArrowHead(int dir, List<UIElement> lines, double x, double y)
        {
            var line = new Line();
            line.Stroke = Brushes.Black;
            line.StrokeThickness = 2;
            line.X1 = 0;
            line.X2 = dir * Renderer.AHEADSIZE;
            line.Y1 = 0;
            line.Y2 = Renderer.AHEADSIZE /2.0;
            Canvas.SetTop(line, y);
            Canvas.SetLeft(line, x);
            lines.Add(line);

            line = new Line();
            line.Stroke = Brushes.Black;
            line.StrokeThickness = 2;
            line.X1 = 0;
            line.X2 = dir * Renderer.AHEADSIZE;
            line.Y1 = 0;
            line.Y2 = -Renderer.AHEADSIZE/2.0;
            Canvas.SetTop(line, y);
            Canvas.SetLeft(line, x);
            lines.Add(line);

        }

        private void arrowLines()
        {
            var ylevel = Renderer.MARGIN + topOfLines;
            var arrows = new List<UIElement>();
            var texts = new List<UIElement>();

            foreach (var al in d.Lines)
            {
                if (al.From.Name.Equals(al.To.Name))
                {
                    if (al.Note)
                    {
                        ylevel = renderNote(ylevel, al, texts, arrows);
                    }
                    else
                    {
                        ylevel = renderSelfArrow(ylevel, al, texts, arrows);
                    }
                }
                else
                {
                    ylevel = renderArrow(ylevel, al, texts, arrows);
                }
            }
            
            // now that we know the line lengths, draw them...
            actorLines(ylevel-topOfLines);

            // now draw the arrows, then the text on top
            foreach (var arrow in arrows) { c.Children.Add(arrow); }
            foreach (var text in texts) { c.Children.Add(text); }
        }


        private double renderArrow(double ylevel, ActorLine al, List<UIElement> texts, List<UIElement> arrows)
        {
            // determine the width of the line
            var ax = actorPlacement[al.From.Name];
            var bx = actorPlacement[al.To.Name];
            var length = Math.Abs(ax - bx);
            var midpt = (ax + bx) / 2.0;

            // render text to 80% of the length
            var txt = renderArrowText(length, al.Desc);
            Canvas.SetTop(txt, ylevel);
            Canvas.SetLeft(txt, midpt - txt.DesiredSize.Width / 2.0);
            texts.Add(txt);

            // move down by the size of the text
            ylevel += txt.DesiredSize.Height + Renderer.MARGIN / 2.0;

            // draw the line
            var line = new Line();
            line.StrokeThickness = 2;
            line.Stroke = Brushes.Black; 
            if(al.Dashed) line.StrokeDashArray = DASHES;
            line.X1 = ax;
            line.X2 = bx;
            line.Y1 = ylevel;
            line.Y2 = ylevel;
            Canvas.SetTop(line, 0);
            Canvas.SetLeft(line, 0);
            arrows.Add(line);
            drawArrowHead(Math.Sign(ax - bx), arrows, bx, ylevel);

            // mvoe down a bit
            ylevel += Renderer.ARROWSEP;

            return ylevel;
        }


        private double renderSelfArrow(double ylevel, ActorLine al, List<UIElement> texts, List<UIElement> arrows)
        {
            // determine the midpoint between the nearest actors
            var ax = actorPlacement[al.From.Name];
            var midpt = (ax + ax + computedBoxSep) / 2.0;

            // render text to 80% of the length
            var txt = renderArrowText(computedBoxSep, al.Desc);
            Canvas.SetTop(txt, ylevel);
            Canvas.SetLeft(txt, midpt - txt.DesiredSize.Width / 2.0);
            texts.Add(txt);

            // move down by the size of the text
            ylevel += txt.DesiredSize.Height + Renderer.MARGIN / 2.0;
            var bottom = ylevel + Renderer.ARROWSEP;
            // draw the lines
            var line = new Polyline();
            line.StrokeThickness = 2;
            line.Stroke = Brushes.Black;
            line.Points = new PointCollection(new Point[] { new Point(ax,ylevel), 
                                                            new Point(midpt,ylevel),
                                                            new Point(midpt,bottom),
                                                            new Point(ax,bottom) });
            if (al.Dashed) line.StrokeDashArray = DASHES;
            Canvas.SetTop(line, 0);
            Canvas.SetLeft(line, 0);
            arrows.Add(line);
            drawArrowHead(1, arrows, ax, bottom);

            // mvoe down a bit
            ylevel = bottom + Renderer.ARROWSEP;

            return ylevel;
        }

        private double renderNote(double ylevel, ActorLine al, List<UIElement> texts, List<UIElement> arrows)
        {
            // determine the midpoint between the nearest actors
            var ax = actorPlacement[al.From.Name];
            var midpt = (ax + ax + computedBoxSep) / 2.0;

            // render text to 80% of the length
            var txt = renderArrowText(computedBoxSep, al.Desc, false);
            Canvas.SetTop(txt, ylevel + Renderer.MARGIN);
            Canvas.SetLeft(txt, midpt - txt.DesiredSize.Width / 2.0);
            texts.Add(txt);

            // draw the surrounding box
            var rect = new Rectangle();
            rect.Width = txt.DesiredSize.Width + 2.0*Renderer.MARGIN;
            rect.Height = txt.DesiredSize.Height + 2.0*Renderer.MARGIN;
            rect.Stroke = Brushes.Black;
            rect.StrokeThickness = 2;
            rect.Fill = Brushes.AntiqueWhite;
            Canvas.SetTop(rect, ylevel);
            Canvas.SetLeft(rect, midpt - rect.Width / 2.0);
            
            // draw a line leading to the box...
            var lin = new Line();
            lin.Stroke = Brushes.Black;
            lin.StrokeThickness = 2;
            lin.X1 = ax;
            lin.X2 = midpt;
            lin.Y1 = ylevel + rect.Height / 2.0;
            lin.Y2 = ylevel + rect.Height / 2.0;
            Canvas.SetLeft(lin, 0);
            Canvas.SetTop(lin, 0);
            arrows.Add(lin);
            arrows.Add(rect); // add the rect second for Z-ordering..

            // move down by the size of the text
            ylevel += rect.Height + Renderer.ARROWSEP;
            return ylevel;
        }


        private TextBlock renderArrowText(double len, string desc, bool translucent = true)
        {
            var txt = new TextBlock();
            if (translucent) { txt.Background = translucentBrush; }
            txt.FontSize = Renderer.TEXTSIZE;
            txt.FontFamily = fontFamily;
            txt.Text = desc;
            txt.TextWrapping = TextWrapping.Wrap;
            txt.MaxWidth = len*.8;
            txt.Measure(Renderer.HUGE);
            return txt;
        }

        private void actorLines(double lineLength)
        {
            foreach (var x in actorPlacement.Values)
            {
                var line = new Line();
                line.StrokeThickness = 2;
                line.Stroke = Brushes.DarkGray;
                line.X1 = 0;
                line.X2 = 0;
                line.Y1 = 0;
                line.Y2 = lineLength;

                addAt(line, x, topOfLines);
            }
        }

        private void centerTitle(UIElement title)
        {
            var midpt = (actorPlacement.Values.Max() + actorPlacement.Values.Min()) / 2.0;
            var offset = midpt - (title.DesiredSize.Width / 2.0);
            if (offset < 0) offset = Renderer.MARGIN;
            addAt(title, offset, Renderer.MARGIN);
        }

        private UIElement titleWords()
        {
            var titleBlock = new TextBlock();
            titleBlock.Text = d.Title;
            titleBlock.FontFamily = fontFamily;
            titleBlock.FontSize = Renderer.TEXTSIZE*1.5;
            titleBlock.Measure(Renderer.HUGE);
            return titleBlock;
        }

        private void actorBoxes()
        {
            var rnd = new Random();
 
            var texts = new List<UIElement>();

            foreach(var a in d.Actors) {                
                var txt = new TextBlock();
                txt.FontSize = Renderer.TEXTSIZE;
                txt.FontFamily = fontFamily;
                txt.Text = a.DisplayName;
                txt.TextWrapping = TextWrapping.Wrap;
                txt.MaxWidth = Renderer.TEXTSIZE * 10;
                txt.Measure(Renderer.HUGE);
                texts.Add(txt);
            }

            double xSoFar = Renderer.MARGIN;
            var maxHeight = texts.Max(t => t.DesiredSize.Height);
            var maxWidth = texts.Max(t => t.DesiredSize.Width);
            boxHeight = maxHeight + 2.0*Renderer.MARGIN;
            boxWidth = maxWidth + 2.0*Renderer.MARGIN;
            topOfLines = topOfDiagram + boxHeight;
            computedBoxSep = Math.Max(Renderer.BOXSEP, boxWidth * 1.5);

            foreach(var txt in texts) {
                var r = new Rectangle();
                r.Height = boxHeight;
                r.Width = boxWidth;
                r.StrokeThickness = 2;
                r.Stroke = Brushes.Black;
                r.Fill = Brushes.AntiqueWhite;
                addAt(r, xSoFar, topOfDiagram);
                addAt(txt, xSoFar + (r.Width - txt.DesiredSize.Width) / 2.0,
                           topOfDiagram + (r.Height - txt.DesiredSize.Height) / 2.0);
                xSoFar += computedBoxSep;
            }

            // finally... go through the actors again, and remember where they go..
            xSoFar = Renderer.MARGIN + boxWidth / 2.0;
            foreach (var a in d.Actors)
            {
                actorPlacement.Add(a.Name, xSoFar);
                xSoFar += computedBoxSep;
            }

        }

        private void addAt(UIElement el, double left, double top) {
            Canvas.SetLeft(el,left);
            Canvas.SetTop(el,top);
            c.Children.Add(el);
        }
    }
}
