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
	public class OrderFlowBias : Indicator
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
				Name										= "OrderFlow Bias";
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
				
				Smoothing = 5;
				ColorBars = false;
				AddPlot(new Stroke(Brushes.DimGray, 6), PlotStyle.Bar, "Momo");
				AddPlot(new Stroke(Brushes.DimGray, 2), PlotStyle.Line, "Cumualtive");
				AddPlot(new Stroke(Brushes.DimGray, 3), PlotStyle.Line, "CumSma");
				AddPlot(new Stroke(Brushes.DimGray, 3), PlotStyle.Line, "CumSmaLonger");
				AddPlot(Brushes.Transparent, "DeltaMomo");
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
			      // Print the close of the cumulative delta bar with a delta type of Bid Ask and with a Session period
			     // Print("Delta Close: " + cumulativeDelta.DeltaClose[0]);
				//if ( IsFirstTickOfBar )
					//Print(FormatDateTime() + "\t" + cumDeltaValue);
				
			}
			else if (BarsInProgress == 1)
			{
				// We have to update the secondary series of the hosted indicator to make sure the values we get in BarsInProgress == 0 are in sync
			    cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
				cumulativeDeltaRth.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
				Cumualtive[0] = cumulativeDeltaRth.DeltaClose[0];
				CumSma[0] = SMA(cumulativeDeltaRth.DeltaClose, Smoothing)[0];
				CumSmaLonger[0] = SMA(cumulativeDeltaRth.DeltaClose, Smoothing * 3)[0];
				
				 // set cumulative delta momentum
				if (cumulativeDelta.DeltaClose[0] > 0)
				{
					Momo[0] = 0;
					if (DeltaMomo[1] > 0)
						DeltaMomo[0] = DeltaMomo[1] + cumulativeDelta.DeltaClose[0];
					else
						DeltaMomo[0] = cumulativeDelta.DeltaClose[0];
					
					Momo[0] = DeltaMomo[0];
					PlotBrushes[0][0] = Brushes.DodgerBlue;
				}

				if (cumulativeDelta.DeltaClose[0] < 0)
				{
					Momo[0] = 0;
					if (DeltaMomo[1] < 0)
						DeltaMomo[0] = DeltaMomo[1] + cumulativeDelta.DeltaClose[0];
					else
						DeltaMomo[0] = cumulativeDelta.DeltaClose[0];
					
					Momo[0] = DeltaMomo[0];
					PlotBrushes[0][0] = Brushes.Red;
				}
				
				// set cumulative delta avg 
				if ( CumSma[0] >= 0.0 ) { 
					PlotBrushes[2][0] = Brushes.DodgerBlue;
				} else {
					PlotBrushes[2][0] = Brushes.Red;	
				}
				 
				
				// set CumSmaLonger delta avg 
				if ( CumSmaLonger[0] >= 0.0 ) { 
					PlotBrushes[3][0] = Brushes.DodgerBlue;
				} else {
					PlotBrushes[3][0] = Brushes.Red;	
				}

				// bearish bias
				if ( Cumualtive[0] < 0.0 ) {
					if( ColorBars ) {
						BarBrush = Brushes.Red;
						CandleOutlineBrush = Brushes.Red;
					}
					biasMessage = "Bear";
					PlotBrushes[1][0] = Brushes.Red;
					if ( Cumualtive[0] > CumSma[0] ) {
						biasMessage = "Weak Bear";
						if( ColorBars ) {
							BarBrush = Brushes.Magenta;
							CandleOutlineBrush = Brushes.Magenta;
						}
						PlotBrushes[1][0] = Brushes.Magenta;
					}
				}
				// bullish bias
				if ( Cumualtive[0] > 0.0 ) {
					if( ColorBars ) {
						BarBrush = Brushes.DodgerBlue;
						CandleOutlineBrush = Brushes.DodgerBlue;
					}
					biasMessage = "Bull";
					PlotBrushes[1][0] = Brushes.DodgerBlue;
					if ( Cumualtive[0] < CumSma[0] ) {
						biasMessage = "Weak Bull";
						if( ColorBars ) {
							BarBrush = Brushes.Cyan;
							CandleOutlineBrush = Brushes.Cyan;
						}
						PlotBrushes[1][0] = Brushes.Cyan;
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
		
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CumSmaLonger
		{
			get { return Values[3]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DeltaMomo
		{
			get { return Values[4]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrderFlowBias[] cacheOrderFlowBias;
		public OrderFlowBias OrderFlowBias(int smoothing, bool colorBars)
		{
			return OrderFlowBias(Input, smoothing, colorBars);
		}

		public OrderFlowBias OrderFlowBias(ISeries<double> input, int smoothing, bool colorBars)
		{
			if (cacheOrderFlowBias != null)
				for (int idx = 0; idx < cacheOrderFlowBias.Length; idx++)
					if (cacheOrderFlowBias[idx] != null && cacheOrderFlowBias[idx].Smoothing == smoothing && cacheOrderFlowBias[idx].ColorBars == colorBars && cacheOrderFlowBias[idx].EqualsInput(input))
						return cacheOrderFlowBias[idx];
			return CacheIndicator<OrderFlowBias>(new OrderFlowBias(){ Smoothing = smoothing, ColorBars = colorBars }, input, ref cacheOrderFlowBias);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrderFlowBias OrderFlowBias(int smoothing, bool colorBars)
		{
			return indicator.OrderFlowBias(Input, smoothing, colorBars);
		}

		public Indicators.OrderFlowBias OrderFlowBias(ISeries<double> input , int smoothing, bool colorBars)
		{
			return indicator.OrderFlowBias(input, smoothing, colorBars);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrderFlowBias OrderFlowBias(int smoothing, bool colorBars)
		{
			return indicator.OrderFlowBias(Input, smoothing, colorBars);
		}

		public Indicators.OrderFlowBias OrderFlowBias(ISeries<double> input , int smoothing, bool colorBars)
		{
			return indicator.OrderFlowBias(input, smoothing, colorBars);
		}
	}
}

#endregion
