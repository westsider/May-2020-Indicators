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
	public class OrderFlowCumDeltaAvg : Indicator
	{
		private OrderFlowCumulativeDelta cumulativeDelta;
		private OrderFlowCumulativeDelta cumulativeDeltaRth;
		private double cumDeltaValue = 0.0;
		private string biasMessage = "no message";
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "OrderFlow Cum Delta Avg";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= false;
				DrawVerticalGridLines						= false;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				Smoothing = 34;
				ColorBars = false; 
				AddPlot(new Stroke(Brushes.DimGray, 2), PlotStyle.Line, "Cumualtive");
				AddPlot(new Stroke(Brushes.DimGray, 3), PlotStyle.Line, "CumSma");
				AddPlot(new Stroke(Brushes.DimGray, 3), PlotStyle.Line, "CumSmaLonger"); 
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{				
			      // Instantiate the indicator
			      cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);
				  cumulativeDeltaRth = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
			}
			
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 10 ) return;
			
			if (BarsInProgress == 0)
			{ 				
			}
			else if (BarsInProgress == 1)
			{
				// We have to update the secondary series of the hosted indicator to make sure the values we get in BarsInProgress == 0 are in sync
			    cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
				cumulativeDeltaRth.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
				CumSma[0] = EMA(cumulativeDeltaRth.DeltaClose, Smoothing)[0];
				
				
				// set cumulative delta avg 
				if ( CumSma[0] >= 0.0  ) { 
					PlotBrushes[2][0] = Brushes.Cyan;
					biasMessage = "Weak Bull";
					if( ColorBars ) {
							BarBrush = Brushes.Cyan;
							CandleOutlineBrush = Brushes.Cyan;
						}
					if (CumSma[0] <= cumulativeDeltaRth.DeltaClose[0] ) {
						PlotBrushes[2][0] = Brushes.DodgerBlue;
						biasMessage = "Bull";
						if( ColorBars ) {
							BarBrush = Brushes.DodgerBlue;
							CandleOutlineBrush = Brushes.DodgerBlue;
						}
					}
				} 
				
				if ( CumSma[0] <= 0.0  ) { 
					PlotBrushes[2][0] = Brushes.Magenta;
					biasMessage = "Weak Bear";
					if( ColorBars ) {
						BarBrush = Brushes.Magenta;
						CandleOutlineBrush = Brushes.Magenta;
					}
					if (CumSma[0] >= cumulativeDeltaRth.DeltaClose[0] ) {
						PlotBrushes[2][0] = Brushes.Red;
						biasMessage = "Bear";
						if( ColorBars ) {
							BarBrush = Brushes.Red;
							CandleOutlineBrush = Brushes.Red;
						}
					}
				}
			}
			Draw.TextFixed(this, "MyTextFixed", biasMessage, TextPosition.TopRight);
		}
		
		private string FormatDateTime() {
			DateTime myDate = Time[0];  // DateTime type
			string prettyDate = myDate.ToString("M/d/yyyy") + " " + myDate.ToString("hh:mm");
			return prettyDate;
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Smoothing", Order=1, GroupName="Parameters")]
		public int Smoothing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ColorBars", Order=2, GroupName="Parameters")]
		public bool ColorBars
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Momo
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Cumualtive
		{
			get { return Values[1]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CumSma
		{
			get { return Values[2]; }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrderFlowCumDeltaAvg[] cacheOrderFlowCumDeltaAvg;
		public OrderFlowCumDeltaAvg OrderFlowCumDeltaAvg(int smoothing, bool colorBars)
		{
			return OrderFlowCumDeltaAvg(Input, smoothing, colorBars);
		}

		public OrderFlowCumDeltaAvg OrderFlowCumDeltaAvg(ISeries<double> input, int smoothing, bool colorBars)
		{
			if (cacheOrderFlowCumDeltaAvg != null)
				for (int idx = 0; idx < cacheOrderFlowCumDeltaAvg.Length; idx++)
					if (cacheOrderFlowCumDeltaAvg[idx] != null && cacheOrderFlowCumDeltaAvg[idx].Smoothing == smoothing && cacheOrderFlowCumDeltaAvg[idx].ColorBars == colorBars && cacheOrderFlowCumDeltaAvg[idx].EqualsInput(input))
						return cacheOrderFlowCumDeltaAvg[idx];
			return CacheIndicator<OrderFlowCumDeltaAvg>(new OrderFlowCumDeltaAvg(){ Smoothing = smoothing, ColorBars = colorBars }, input, ref cacheOrderFlowCumDeltaAvg);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrderFlowCumDeltaAvg OrderFlowCumDeltaAvg(int smoothing, bool colorBars)
		{
			return indicator.OrderFlowCumDeltaAvg(Input, smoothing, colorBars);
		}

		public Indicators.OrderFlowCumDeltaAvg OrderFlowCumDeltaAvg(ISeries<double> input , int smoothing, bool colorBars)
		{
			return indicator.OrderFlowCumDeltaAvg(input, smoothing, colorBars);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrderFlowCumDeltaAvg OrderFlowCumDeltaAvg(int smoothing, bool colorBars)
		{
			return indicator.OrderFlowCumDeltaAvg(Input, smoothing, colorBars);
		}

		public Indicators.OrderFlowCumDeltaAvg OrderFlowCumDeltaAvg(ISeries<double> input , int smoothing, bool colorBars)
		{
			return indicator.OrderFlowCumDeltaAvg(input, smoothing, colorBars);
		}
	}
}

#endregion
