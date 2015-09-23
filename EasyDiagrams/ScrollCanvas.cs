using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EasyDiagrams
{
    public class ScrollCanvas : Canvas
    {
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            System.Windows.Size ans = base.MeasureOverride(constraint);
            if(base.InternalChildren.Count > 0) { 
              ans.Width  = base.InternalChildren.OfType<System.Windows.UIElement>().Max(i => i.DesiredSize.Width  + (double)i.GetValue(Canvas.LeftProperty));
              ans.Height = base.InternalChildren.OfType<System.Windows.UIElement>().Max(i => i.DesiredSize.Height + (double)i.GetValue(Canvas.TopProperty));
            }
            return ans;
        }

        public System.Windows.Size GetAggregateSize()
        {
            // var lst = base.InternalChildren.OfType<System.Windows.UIElement>().Select(i => i.GetValue(Canvas.LeftProperty)).ToList();
            return MeasureOverride(new System.Windows.Size(Double.MaxValue,Double.MaxValue));
        }
    }
}
