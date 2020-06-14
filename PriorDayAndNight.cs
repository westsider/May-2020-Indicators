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
	public class PriorDayAndNight : Indicator
	{
		private int 	startTime 	= 930; 
        private int	 	endTime 	= 1600;
		private int	 	IBendTime 	= 1030;
		private int		ninja_Start_Time;
		private int		ninja_End_Time;
		private int		ninja_IB_End_Time;
		private int		latestRTHbar;
		private int		lastBar;
		private double	gxHigh;
		private double	gxLow;
		private double	gxMid;
		private int 	gxMidBarsAgo;
		private int 	ibMidBarsAgo;
		private double	ibHigh;
		private double	ibLow;
		private double	ibMid;
		private double	open_D;
		private double	yHigh;
		private double	yLow;
		private double	yMid;
		private double	todaysClose;
		private double	ibRange;
		private double  ibExtension;
		private double  yIB;
		private bool    plotBox = false;
		private Brush   textColor = Brushes.DimGray;
		private int 	rthStartBarNum;
		private int 	rthEndBarNum;
		private int 	ibEndBarNum;
		private string 	yDate;
		private bool 	sunday = false;
		private int 	barsRight = 5;
		private double gap;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Prior Day And Night";
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
				RTHOpen					= 930;
				RTHclose				= 1600;
				AddPlot(Brushes.Red, "YH");
				AddPlot(Brushes.DarkGray, "YL");
				AddPlot(Brushes.DarkGray, "YC");
				AddPlot(Brushes.DarkGray, "GxHigh");
				AddPlot(Brushes.DarkGray, "GxLow");
				AddPlot(Brushes.DarkGray, "IBHighPlot");
				AddPlot(Brushes.DarkGray, "IBLowPlot");
				AddPlot(Brushes.DarkGray, "IBX1");
				AddPlot(Brushes.DarkGray, "IBX1l");
				AddPlot(Brushes.DarkGray, "IBX2");
				AddPlot(Brushes.DarkGray, "IBX2l");
				AddPlot(Brushes.DarkGray, "IBX3");
				AddPlot(Brushes.DarkGray, "IBX3l");
				AddPlot(Brushes.DarkGray, "IBX4");
				AddPlot(Brushes.DarkGray, "IBX4l");
				AddPlot(Brushes.DarkGray, "HalfGap");
				RTHOpen					= 930;
				RTHclose				= 1600;
				ShowExten = true;
				ShowGx = false;
				ShowIb = false;
			}
			else if (State == State.Configure)
			{
				ClearOutputWindow();
			} 
			else if (State == State.DataLoaded) 
			{
				/// Cnnvert Military Time to Ninja Time
				ninja_Start_Time = startTime * 100;
				ninja_End_Time = endTime * 100;
				ninja_IB_End_Time = IBendTime * 100;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			lastBar = CurrentBar - 1;
			holidayOrSunday();
			
			/// Session Start
			if (ToTime(Time[1])<= ninja_Start_Time && ToTime(Time[0]) >= ninja_Start_Time && !sunday) {
				open_D = Open[0];
				rthStartBarNum = CurrentBar ;
                /// more efficient find globex range - onlu calc once per day
                int globexLength = rthStartBarNum - rthEndBarNum;
				if ( globexLength > 0 ) {
	                gxHigh = MAX(High, globexLength)[1];
	                gxLow = MIN(Low, globexLength)[1];
	                gxMid = ((gxHigh - gxLow) / 2) + gxLow;
	                gxMidBarsAgo = globexLength / 2;
					if (todaysClose != null) {
						gap = open_D - todaysClose;
						Print(gap);
					}
				}
//calcStats();
            }

            /// Session End
			if (ToTime(Time[1]) <= ninja_End_Time && ToTime(Time[0]) >= ninja_End_Time && !sunday)
            {
                todaysClose = Close[0];
                yMid = ((yHigh - yLow) / 2) + yLow;
                yIB = ibRange;
                rthEndBarNum = CurrentBar;
                /// more efficient find RTH Range
                int rthLength = rthEndBarNum - rthStartBarNum;
				if ( rthLength > 0 ) {
	                yHigh = MAX(High, rthLength)[0];
	                yLow = MIN(Low, rthLength)[0];
				}
            }
			
			/// Find IB Range
			if (ToTime(Time[1]) <= ninja_IB_End_Time && ToTime(Time[0]) >= ninja_IB_End_Time  && !sunday ) {
                ibEndBarNum = CurrentBar;
                int ibLength = CurrentBar - rthStartBarNum;
				if ( ibLength > 0 ) {
						ibHigh = MAX(High, ibLength)[0];
						ibLow = MIN(Low, ibLength)[0];	
						ibMid = ( ( ibHigh - ibLow ) /2  )+ ibLow;
					if (ibLength > 2 ) {
						ibMidBarsAgo = ibLength / 2;
					} else {
						ibMidBarsAgo = ibLength;
					}
				}
//calcStats();
			}
			
			/// plot prior day Hi and Low
			if (ToTime(Time[0]) >= ninja_Start_Time && ToTime(Time[0]) < ninja_End_Time  && !sunday ) {
				latestRTHbar = CurrentBar - rthStartBarNum;
				
				
				if( yLow != 0 && yMid != 0 && yHigh != 0) {
					if ( plotBox ) {
						RemoveDrawObject("RthBoxU"+lastBar);
						RemoveDrawObject("RthBoxL"+lastBar);
						Draw.Rectangle(this, "RthBoxU"+ CurrentBar.ToString(), true, latestRTHbar, yMid, -barsRight, yHigh, Brushes.Transparent, Brushes.DarkBlue, 15);
						Draw.Rectangle(this, "RthBoxL"+ CurrentBar.ToString(), true, latestRTHbar, yLow, -barsRight, yMid, Brushes.Transparent, Brushes.DarkRed, 15);
					} else {
						YH[0] = yHigh;
						YL[0] = yLow;
						YC[0] = todaysClose;
						double halfGap =  open_D - (gap * 0.5);
						if (ToTime(Time[0]) <= ninja_IB_End_Time)
						HalfGap[0] = halfGap;
						
						Draw.Text(this, "yHigh", "yh", -barsRight, yHigh, textColor);
						Draw.Text(this, "yLow", "yl", -barsRight, yLow, textColor);
						Draw.Text(this, "yMid", "yc", -barsRight, todaysClose, textColor);
						Draw.Text(this, "hg", "hg", -barsRight, halfGap, textColor);
						if ( ShowGx ) {
							GxHigh[0] = gxHigh;
							GxLow[0] = gxLow;
							Draw.Text(this, "gxHigh", "gxH", -barsRight, gxHigh, textColor);
							Draw.Text(this, "gxLow", "gxL", -barsRight, gxLow, textColor);
						}
						
					}
				}	
			}
			
			/// plot ib
			if ( ToTime(Time[0]) > ninja_IB_End_Time  && ToTime(Time[0]) < ninja_End_Time && !sunday  ) {
				if ( ShowIb ) {
					IBHighPlot[0] = ibHigh;
					IBLowPlot[0] = ibLow;
					Draw.Text(this, "ibHigh", "ibH", -barsRight, ibHigh, textColor);
					Draw.Text(this, "ibLow", "ibL", -barsRight, ibLow, textColor);
//calcStats();
					
					// plot extensions
					if ( ShowExten ) {
						ibExtension = ibRange / 2;
						IBX1Plot[0] = ibHigh + ibExtension;
						IBX1lPlot[0] = ibLow -  ibExtension;
						IBX2Plot[0] = ibHigh + ibRange;
						IBX2lPlot[0] = ibLow -  ibRange;
						IBX3Plot[0] = ibHigh + ibRange + ibExtension;
						IBX3lPlot[0] = ibLow -  ibRange - ibExtension;
						IBX4Plot[0] = ibHigh + ibRange + ibRange;
						IBX4lPlot[0] = ibLow -  ibRange - ibRange;
					}
				}
			}

            /// plot globex box
            if (plotBox && (ToTime(Time[0]) <= ninja_Start_Time || ToTime(Time[0]) >= ninja_End_Time))  //  Find GX Hi/Lo
            {
                RemoveDrawObject("GxBox" + lastBar);
                Draw.Rectangle(this, "GxBox" + CurrentBar.ToString(), true, ibEndBarNum, gxLow, 0, gxHigh, Brushes.Goldenrod, Brushes.Goldenrod, 5);
            }

            ///// plot arrow from GX mid to OR mid
            //if (ToTime(Time[1]) >= ninja_IB_End_Time && ToTime(Time[0]) <= ninja_IB_End_Time)
            //{
            //    //RemoveDrawObject("ibArrow" + lastBar);
            //    if (ibMid > gxMid)
            //    {
            //        Draw.ArrowLine(this, "ibArrow" + CurrentBar.ToString(), gxMidBarsAgo + rthCounter, gxMid, ibMidBarsAgo, ibMid, Brushes.DodgerBlue, DashStyleHelper.Solid, 6);
            //    }
            //    else
            //    {
            //        Draw.ArrowLine(this, "ibArrow" + CurrentBar.ToString(), gxMidBarsAgo + rthCounter, gxMid, ibMidBarsAgo, ibMid, Brushes.DarkRed, DashStyleHelper.Solid, 6);
            //    }

            //}
        }
		
		private void calcStats() {
			ibRange = ibHigh - ibLow;
			var ibMessage = "IB " + ibRange.ToString() + "\nY  " + yIB.ToString() + "\nG " + gap.ToString();
			Draw.TextFixed(this, "ibRangeToday", ibMessage, TextPosition.TopRight);	
			
		}
		
		private void holidayOrSunday() {
			
			yDate = Time[0].DayOfWeek.ToString();
			if (yDate == "Sunday" ) {
				Print("----------------------------------------");
				sunday = true;
			} else {
				sunday = false;	
			}
			foreach(KeyValuePair<DateTime, string> holiday in TradingHours.Holidays)
			{
                string dateOnly = String.Format("{0:MM/dd/yyyy}", holiday.Key);
                DateTime myDate = Time[0];  // DateTime type
                string prettyDate = myDate.ToString("MM/d/yyyy");
                if (dateOnly == prettyDate)
                {
                    Print("\nToday is " + holiday.Value + "\n");
					sunday = true;
                    if (Bars.IsFirstBarOfSession)
                    {
						//RemoveDrawObject();
                        NinjaTrader.Gui.Tools.SimpleFont myFont = new NinjaTrader.Gui.Tools.SimpleFont("Helvetica", 18) { Size = 18, Bold = false };
                        Draw.Text(this, "holiday"+prettyDate, true, holiday.Value, 0, MAX(High, 20)[1], 1, Brushes.LightGray, myFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 50);

                    }
                }
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RTHOpen", Order=1, GroupName="Parameters")]
		public int RTHOpen
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RTHclose", Order=2, GroupName="Parameters")]
		public int RTHclose
		{ get; set; }

		
		[NinjaScriptProperty]
		[Display(Name="Show IB Extensions", Description="Show IB extension lines", Order=3, GroupName="Parameters")]
		public bool ShowExten
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Globex", Description="Show IB extension lines", Order=4, GroupName="Parameters")]
		public bool ShowGx 
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show IB", Description="Show IB extension lines", Order=4, GroupName="Parameters")]
		public bool ShowIb 
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YH
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YL
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YC
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GxHigh
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GxLow
		{
			get { return Values[4]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBHighPlot
		{
			get { return Values[5]; }
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBLowPlot
		{
			get { return Values[6]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX1Plot
		{
			get { return Values[7]; }
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX1lPlot
		{
			get { return Values[8]; }
		}
		/*
				AddPlot(Brushes.LightGray, "ibHigh");
				AddPlot(Brushes.LightGray, "ibLow");
		
				AddPlot(Brushes.LightGray, "ibX1");
				AddPlot(Brushes.LightGray, "ibX1l");
		
				AddPlot(Brushes.LightGray, "ibX2");
				AddPlot(Brushes.LightGray, "ibX2l");
		
				AddPlot(Brushes.LightGray, "ibX3");
				AddPlot(Brushes.LightGray, "ibX3l");
		
				AddPlot(Brushes.LightGray, "ibX4");
				AddPlot(Brushes.LightGray, "ibX4l");
		*/
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX2Plot
		{
			get { return Values[9]; }
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX2lPlot
		{
			get { return Values[10]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX3Plot
		{
			get { return Values[11]; }
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX3lPlot
		{
			get { return Values[12]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX4Plot
		{
			get { return Values[13]; }
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBX4lPlot
		{
			get { return Values[14]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HalfGap
		{
			get { return Values[15]; }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriorDayAndNight[] cachePriorDayAndNight;
		public PriorDayAndNight PriorDayAndNight(int rTHOpen, int rTHclose, bool showExten, bool showGx, bool showIb)
		{
			return PriorDayAndNight(Input, rTHOpen, rTHclose, showExten, showGx, showIb);
		}

		public PriorDayAndNight PriorDayAndNight(ISeries<double> input, int rTHOpen, int rTHclose, bool showExten, bool showGx, bool showIb)
		{
			if (cachePriorDayAndNight != null)
				for (int idx = 0; idx < cachePriorDayAndNight.Length; idx++)
					if (cachePriorDayAndNight[idx] != null && cachePriorDayAndNight[idx].RTHOpen == rTHOpen && cachePriorDayAndNight[idx].RTHclose == rTHclose && cachePriorDayAndNight[idx].ShowExten == showExten && cachePriorDayAndNight[idx].ShowGx == showGx && cachePriorDayAndNight[idx].ShowIb == showIb && cachePriorDayAndNight[idx].EqualsInput(input))
						return cachePriorDayAndNight[idx];
			return CacheIndicator<PriorDayAndNight>(new PriorDayAndNight(){ RTHOpen = rTHOpen, RTHclose = rTHclose, ShowExten = showExten, ShowGx = showGx, ShowIb = showIb }, input, ref cachePriorDayAndNight);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriorDayAndNight PriorDayAndNight(int rTHOpen, int rTHclose, bool showExten, bool showGx, bool showIb)
		{
			return indicator.PriorDayAndNight(Input, rTHOpen, rTHclose, showExten, showGx, showIb);
		}

		public Indicators.PriorDayAndNight PriorDayAndNight(ISeries<double> input , int rTHOpen, int rTHclose, bool showExten, bool showGx, bool showIb)
		{
			return indicator.PriorDayAndNight(input, rTHOpen, rTHclose, showExten, showGx, showIb);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriorDayAndNight PriorDayAndNight(int rTHOpen, int rTHclose, bool showExten, bool showGx, bool showIb)
		{
			return indicator.PriorDayAndNight(Input, rTHOpen, rTHclose, showExten, showGx, showIb);
		}

		public Indicators.PriorDayAndNight PriorDayAndNight(ISeries<double> input , int rTHOpen, int rTHclose, bool showExten, bool showGx, bool showIb)
		{
			return indicator.PriorDayAndNight(input, rTHOpen, rTHclose, showExten, showGx, showIb);
		}
	}
}

#endregion
