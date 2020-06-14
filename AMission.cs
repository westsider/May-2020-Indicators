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
using System.Timers;
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
	public class AMission : Indicator
	{
		private static Timer timer;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "A Mission";
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
				TopMessage					= @"Success: rules, limits, control, humility  .";
				BottomMessage = "Failure: following feelings, intuition, no loss limit, huberis   .";
				TextColor				= Brushes.Red;
				TopTextColor				= Brushes.DodgerBlue;
				BackGroundCOlor			= Brushes.WhiteSmoke;
				NoteFont				= new SimpleFont("Arial", 14);
			}
			else if (State == State.Configure)
			{
				timer = new System.Timers.Timer();
			}
		}

		protected override void OnBarUpdate()
		{
			
			
			//Print("bar called at " + ToTime[0]);
//			Draw.TextFixed(this,"topMessage", "  "+TopMessage+"  ", TextPosition.TopLeft, 
//				TopTextColor,
//  				NoteFont, 
//				Brushes.Transparent, 
//				BackGroundCOlor, 100);
			
//			Draw.TextFixed(this,"bottomMessage", "  "+BottomMessage+"  ", TextPosition.BottomLeft, 
//				TextColor,
//  				NoteFont, 
//				Brushes.Transparent, 
//				BackGroundCOlor, 100);	
//			if ( !HistoricalDataGridCellBackgroundConverter ) {
//				timer.Interval = 5000; 
//      			timer.AutoReset = true;
//      			timer.Enabled = true;
//			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Top Message", Order=1, GroupName="Parameters")]
		public string TopMessage
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Bottom Message", Order=1, GroupName="Parameters")]
		public string BottomMessage
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Bottom Text Color", Order=2, GroupName="Parameters")]
		public Brush TextColor
		{ get; set; }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Top Text Color", Order=2, GroupName="Parameters")]
		public Brush TopTextColor
		{ get; set; }
		
		
		[Browsable(false)]
		public string TopTextColorSerializable
		{
			get { return Serialize.BrushToString(TopTextColor); }
			set { TopTextColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="BackGround Color", Order=3, GroupName="Parameters")]
		public Brush BackGroundCOlor
		{ get; set; }

		[Browsable(false)]
		public string BackGroundCOlorSerializable
		{
			get { return Serialize.BrushToString(BackGroundCOlor); }
			set { BackGroundCOlor = Serialize.StringToBrush(value); }
		}	
		
		[NinjaScriptProperty]
		[Display(Name="Note Font", Description="Note Font", Order=4, GroupName="Style")]
		public SimpleFont NoteFont
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private AMission[] cacheAMission;
		public AMission AMission(string topMessage, string bottomMessage, Brush textColor, Brush topTextColor, Brush backGroundCOlor, SimpleFont noteFont)
		{
			return AMission(Input, topMessage, bottomMessage, textColor, topTextColor, backGroundCOlor, noteFont);
		}

		public AMission AMission(ISeries<double> input, string topMessage, string bottomMessage, Brush textColor, Brush topTextColor, Brush backGroundCOlor, SimpleFont noteFont)
		{
			if (cacheAMission != null)
				for (int idx = 0; idx < cacheAMission.Length; idx++)
					if (cacheAMission[idx] != null && cacheAMission[idx].TopMessage == topMessage && cacheAMission[idx].BottomMessage == bottomMessage && cacheAMission[idx].TextColor == textColor && cacheAMission[idx].TopTextColor == topTextColor && cacheAMission[idx].BackGroundCOlor == backGroundCOlor && cacheAMission[idx].NoteFont == noteFont && cacheAMission[idx].EqualsInput(input))
						return cacheAMission[idx];
			return CacheIndicator<AMission>(new AMission(){ TopMessage = topMessage, BottomMessage = bottomMessage, TextColor = textColor, TopTextColor = topTextColor, BackGroundCOlor = backGroundCOlor, NoteFont = noteFont }, input, ref cacheAMission);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.AMission AMission(string topMessage, string bottomMessage, Brush textColor, Brush topTextColor, Brush backGroundCOlor, SimpleFont noteFont)
		{
			return indicator.AMission(Input, topMessage, bottomMessage, textColor, topTextColor, backGroundCOlor, noteFont);
		}

		public Indicators.AMission AMission(ISeries<double> input , string topMessage, string bottomMessage, Brush textColor, Brush topTextColor, Brush backGroundCOlor, SimpleFont noteFont)
		{
			return indicator.AMission(input, topMessage, bottomMessage, textColor, topTextColor, backGroundCOlor, noteFont);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.AMission AMission(string topMessage, string bottomMessage, Brush textColor, Brush topTextColor, Brush backGroundCOlor, SimpleFont noteFont)
		{
			return indicator.AMission(Input, topMessage, bottomMessage, textColor, topTextColor, backGroundCOlor, noteFont);
		}

		public Indicators.AMission AMission(ISeries<double> input , string topMessage, string bottomMessage, Brush textColor, Brush topTextColor, Brush backGroundCOlor, SimpleFont noteFont)
		{
			return indicator.AMission(input, topMessage, bottomMessage, textColor, topTextColor, backGroundCOlor, noteFont);
		}
	}
}

#endregion
