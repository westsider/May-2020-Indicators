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
	public class OrderFlowCDaverage : Indicator
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
				Name										= "OrderFlowCDaverage";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.DodgerBlue, "AvgDelta");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{
				//emaFast = EMA(32);
				cumulativeDeltaRth = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 10 ) return;
			Print(Time[0].ToShortTimeString());
			if (BarsInProgress == 1)
			{
				// We have to update the secondary series of the hosted indicator to make sure the values we get in BarsInProgress == 0 are in sync
			    cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
//				cumulativeDeltaRth.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
//				AvgDelta[0] = cumulativeDeltaRth.DeltaClose[0];
//				CumSma[0] = SMA(cumulativeDeltaRth.DeltaClose, Smoothing)[0];
//				CumSmaLonger[0] = SMA(cumulativeDeltaRth.DeltaClose, Smoothing * 3)[0];
			}

		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> AvgDelta
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrderFlowCDaverage[] cacheOrderFlowCDaverage;
		public OrderFlowCDaverage OrderFlowCDaverage()
		{
			return OrderFlowCDaverage(Input);
		}

		public OrderFlowCDaverage OrderFlowCDaverage(ISeries<double> input)
		{
			if (cacheOrderFlowCDaverage != null)
				for (int idx = 0; idx < cacheOrderFlowCDaverage.Length; idx++)
					if (cacheOrderFlowCDaverage[idx] != null &&  cacheOrderFlowCDaverage[idx].EqualsInput(input))
						return cacheOrderFlowCDaverage[idx];
			return CacheIndicator<OrderFlowCDaverage>(new OrderFlowCDaverage(), input, ref cacheOrderFlowCDaverage);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrderFlowCDaverage OrderFlowCDaverage()
		{
			return indicator.OrderFlowCDaverage(Input);
		}

		public Indicators.OrderFlowCDaverage OrderFlowCDaverage(ISeries<double> input )
		{
			return indicator.OrderFlowCDaverage(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrderFlowCDaverage OrderFlowCDaverage()
		{
			return indicator.OrderFlowCDaverage(Input);
		}

		public Indicators.OrderFlowCDaverage OrderFlowCDaverage(ISeries<double> input )
		{
			return indicator.OrderFlowCDaverage(input);
		}
	}
}

#endregion
