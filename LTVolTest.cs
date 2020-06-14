#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class LTVolTest : Indicator
	{
		private NinjaTrader.NinjaScript.Indicators.LizardTrader.LT_Swing_Trend LT_Swing_Trend1;
		private bool upSwing = false;
		private int lastObservation = 0;
		private double lastSwingVolUp = 0.0;
		private double lastSwingVolDn = 0.0;
		private string trendMessage = "no message";
		private string message = "no message";
		private Brush	LineNowColor					= Brushes.Red;
		private bool deBug = false;
		private int swingTrend = 0;
		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "LT Vol Test";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.Orange, "TrendDir");
				UpColor					= Brushes.DodgerBlue;
				DnColor					= Brushes.Red;
			}
			else if (State == State.Configure)
			{
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{				
				LT_Swing_Trend1				= LT_Swing_Trend(Close, ltSwingTrendDeviationType.ATR, false, false, 72, 3, 5, 0.15);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 0)
				return;

			double swingVol = LT_Swing_Trend1.SwingVolume[0];
			double swingSize = LT_Swing_Trend1.SwingSize[0];
			if (swingVol > 0 && swingSize > 0 ) {
				if (upSwing) { 
					RemoveDrawObject( "up"+lastObservation);
				}
				if (swingVol > lastSwingVolDn ) {
					trendMessage = "Bullish";
					LineNowColor = UpColor;
				} else {
					trendMessage = "Bearish";
					LineNowColor = DnColor;
				}
				message = swingVol.ToString() + "\n" + lastSwingVolDn.ToString() + "\n" + trendMessage;
				if ( deBug ) {
				Draw.Text(this, "up"+CurrentBar, message, 0, Low[0] - 2 * TickSize, Brushes.White); }
				upSwing = true;
				lastObservation = CurrentBar;
				lastSwingVolUp = swingVol;
				
			} else if (swingVol > 0 && swingSize < 0 ) {
				//Print("\t" + Time[0].ToString() + " \nDn \t " +  swingVol+ " \t Size: " + swingSize);
				if (!upSwing) { 
					RemoveDrawObject( "dn"+lastObservation);
				}
				if (swingVol < lastSwingVolUp ) {
					trendMessage = "Bullish";
					LineNowColor = UpColor;
				} else {
					trendMessage = "Bearish";
					LineNowColor = DnColor;
				}
				message = swingVol.ToString() + "\n" + lastSwingVolUp.ToString() + "\n" + trendMessage;
				if ( deBug ) {
				Draw.Text(this, "dn"+CurrentBar, message, 0, High[0] + 2 * TickSize, Brushes.White); }
				upSwing = false;
				lastObservation = CurrentBar;
				lastSwingVolDn = swingVol;
			}
			PlotBrushes[0][0] = LineNowColor;
			TrendDir[0] = MIN(Low, 120)[0];
			
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> TrendDir
		{
			get { return Values[0]; }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Up Color", Description="Chop zone color.", Order=19, GroupName="2. Visualize Swings")]
		public Brush UpColor
		{ get; set; }
		
		[Browsable(false)]
		public string UpColorSerializable
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Dn Color", Description="Chop zone color.", Order=20, GroupName="2. Visualize Swings")]
		public Brush DnColor
		{ get; set; }
		
		[Browsable(false)]
		public string DnColorSerializable
		{
			get { return Serialize.BrushToString(DnColor); }
			set { DnColor = Serialize.StringToBrush(value); }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private LTVolTest[] cacheLTVolTest;
		public LTVolTest LTVolTest(Brush upColor, Brush dnColor)
		{
			return LTVolTest(Input, upColor, dnColor);
		}

		public LTVolTest LTVolTest(ISeries<double> input, Brush upColor, Brush dnColor)
		{
			if (cacheLTVolTest != null)
				for (int idx = 0; idx < cacheLTVolTest.Length; idx++)
					if (cacheLTVolTest[idx] != null && cacheLTVolTest[idx].UpColor == upColor && cacheLTVolTest[idx].DnColor == dnColor && cacheLTVolTest[idx].EqualsInput(input))
						return cacheLTVolTest[idx];
			return CacheIndicator<LTVolTest>(new LTVolTest(){ UpColor = upColor, DnColor = dnColor }, input, ref cacheLTVolTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.LTVolTest LTVolTest(Brush upColor, Brush dnColor)
		{
			return indicator.LTVolTest(Input, upColor, dnColor);
		}

		public Indicators.LTVolTest LTVolTest(ISeries<double> input , Brush upColor, Brush dnColor)
		{
			return indicator.LTVolTest(input, upColor, dnColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.LTVolTest LTVolTest(Brush upColor, Brush dnColor)
		{
			return indicator.LTVolTest(Input, upColor, dnColor);
		}

		public Indicators.LTVolTest LTVolTest(ISeries<double> input , Brush upColor, Brush dnColor)
		{
			return indicator.LTVolTest(input, upColor, dnColor);
		}
	}
}

#endregion
