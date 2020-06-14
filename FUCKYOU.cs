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
using PriceActionSwing.Base;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class FUCKYOU : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "FUCKYOU";
				Calculate									= Calculate.OnBarClose;
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
			}
			else if (State == State.Configure)
			{
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			//var n = PriceActionSwingOscillator(Close, PriceActionSwing.Base.SwingStyle.Standard, 7, 20, false, PriceActionSwing.Base.Show.Volume, true, true, true);
			//PriceActionSwingOscillator(PriceActionSwing.Base.SwingStyle.Standard, 7, 20, false, PriceActionSwing.Base.Show.Volume, true, true, true);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FUCKYOU[] cacheFUCKYOU;
		public FUCKYOU FUCKYOU()
		{
			return FUCKYOU(Input);
		}

		public FUCKYOU FUCKYOU(ISeries<double> input)
		{
			if (cacheFUCKYOU != null)
				for (int idx = 0; idx < cacheFUCKYOU.Length; idx++)
					if (cacheFUCKYOU[idx] != null &&  cacheFUCKYOU[idx].EqualsInput(input))
						return cacheFUCKYOU[idx];
			return CacheIndicator<FUCKYOU>(new FUCKYOU(), input, ref cacheFUCKYOU);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FUCKYOU FUCKYOU()
		{
			return indicator.FUCKYOU(Input);
		}

		public Indicators.FUCKYOU FUCKYOU(ISeries<double> input )
		{
			return indicator.FUCKYOU(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FUCKYOU FUCKYOU()
		{
			return indicator.FUCKYOU(Input);
		}

		public Indicators.FUCKYOU FUCKYOU(ISeries<double> input )
		{
			return indicator.FUCKYOU(input);
		}
	}
}

#endregion
