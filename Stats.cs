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
using System.IO;
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
	public class Stats : Indicator
	{
		private double Open_D = 0.0;
		private double Close_D = 0.0;
		private double Gap_D = 0.0;
		private string message = "no message";
		private long startTime = 0;
		private	long endTime = 0;
		
		private int RTH_Counter = 0; 
		private double Y_High = 0.0;
		private double Y_Low = 0.0;
		
		private int GX_Counter = 0;
		private double GX_Low = 0.0;
		private double GX_High = 0.0; 
		private string path;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Stats";
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
				RTHopen						= DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHclose						= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				path 			= NinjaTrader.Core.Globals.UserDataDir + "JigsawLevels.csv";
			}
			else if (State == State.Configure)
			{
				startTime = long.Parse(RTHopen.ToString("HHmmss"));
			 	endTime = long.Parse(RTHclose.ToString("HHmmss"));
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 5 ) { return; }

			if ("Sunday"  == Time[0].DayOfWeek.ToString()) { return; }
			
			if (BarsInProgress == 1 && ToTime(Time[0]) == startTime ) { 
				Open_D = Open[0];
				Gap_D = Open_D - Close_D;
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString() + "   Open: " + Open_D.ToString() +  "   Gap: " + Gap_D.ToString();
				Print(message);
				//Draw.Dot(this, "open"+CurrentBar, false, 0, Open_D, Brushes.White);
			}
			
			if (BarsInProgress == 1 && ToTime(Time[0]) == endTime ) { 
				Close_D = Close[0];
				//Print(Time[0].ToShortDateString() + " \t" + Time[0].ToShortTimeString() + "\t close: " + Close_D.ToString());
				//Draw.Dot(this, "close"+CurrentBar, false, 0, Close_D, Brushes.White);
			}
			
			/// pre market gap
			if (BarsInProgress == 1 && ToTime(Time[0]) < startTime ) { 
				Gap_D = Close[0] - Close_D;
				message =  Time[0].ToShortDateString() + " \t"  + Time[0].ToShortTimeString() +  " \t Pre M Gap: " + Gap_D.ToString();
				//Print(message);
			}
			
			// after open
			if (BarsInProgress == 1 && ToTime(Time[0]) > startTime ) { 
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString() + "   Open: " + Open_D.ToString() +  "   Gap: " + Gap_D.ToString();
			}
			
			// RTH High - Low
			if (BarsInProgress == 1 && ToTime(Time[0]) >= startTime && ToTime(Time[0]) <= endTime ) {  
				RTH_Counter += 1; 
				Draw.Text(this, "yhigh"+CurrentBar, "-", 0, Y_High, Brushes.Red);
				Draw.Text(this, "ylow"+CurrentBar, "-", 0, Y_Low, Brushes.Green);
				Draw.Text(this, "gxhigh"+CurrentBar, "-", 0, GX_High, Brushes.Magenta);
				Draw.Text(this, "gxlow"+CurrentBar, "-", 0, GX_Low, Brushes.Cyan);
				if (ToTime(Time[0]) == endTime && RTH_Counter > 0 ) { 
					Y_High = MAX(High, RTH_Counter)[0];
					Y_Low = MIN(Low, RTH_Counter)[0];
					RTH_Counter = 0;
				} 
			}
			if (BarsInProgress == 1 && (ToTime(Time[0]) <= startTime || ToTime(Time[0]) >= endTime )) { 
				GX_Counter += 1;
				//Draw.Text(this, "GX_Counter"+CurrentBar, GX_Counter.ToString(), 0, High[0] + (2 * TickSize), Brushes.White);
				if (ToTime(Time[0]) == startTime && GX_Counter > 0 ) { 
					GX_High = MAX(High, GX_Counter)[0];
					GX_Low = MIN(Low, GX_Counter)[0];
					GX_Counter = 0;
					
					WriteFile(path: path, newLine: Open_D.ToString() + ", open", header: true);
					WriteFile(path: path, newLine: Close_D.ToString() + ", settle", header: false);
					WriteFile(path: path, newLine: Y_High.ToString() + ", y high", header: false);
					WriteFile(path: path, newLine: Y_Low.ToString() + ", y low", header: false);
					WriteFile(path: path, newLine: GX_High.ToString() + ", gx high", header: false);
					WriteFile(path: path, newLine: GX_Low.ToString() + ", gx low", header: false); 
					
					 
					
				}
			}
			calcStats(); 
		}
		
		private void calcStats() {
			var Message = "open " + Open_D.ToString() + "\nsettle  " + Close_D.ToString();
			Message += "\ny high " + Y_High.ToString() + "\ny low  " + Y_Low.ToString();
			Message += "\ngx high " + GX_High.ToString() + "\ngx low  " + GX_Low.ToString();
			Draw.TextFixed(this, "Message", Message, TextPosition.TopRight);	
		}
		
		private void ClearFile(string path)
        {
            try    
			{    
				// Check if file exists with its full path    
				if (File.Exists(path))    
				{    
					// If file found, delete it    
					File.Delete(path);    
					Print("File deleted.");    
				} 
				else  Print("File not found");    
			}    
			catch (IOException ioExp)    
			{    
				Print(ioExp.Message);    
			}  
        }
		
		private void WriteFile(string path, string newLine, bool header)
        {
			if ( header ) {
				ClearFile(path: path);
				using (var tw = new StreamWriter(path, true))
	            {
	                tw.WriteLine(newLine); 
	                tw.Close();
	            }
				return;
			}
			
            using (var tw = new StreamWriter(path, true))
            {
                tw.WriteLine(newLine);
                tw.Close();
            }
        }

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHopen", Order=1, GroupName="Parameters")]
		public DateTime RTHopen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHclose", Order=2, GroupName="Parameters")]
		public DateTime RTHclose
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Stats[] cacheStats;
		public Stats Stats(DateTime rTHopen, DateTime rTHclose)
		{
			return Stats(Input, rTHopen, rTHclose);
		}

		public Stats Stats(ISeries<double> input, DateTime rTHopen, DateTime rTHclose)
		{
			if (cacheStats != null)
				for (int idx = 0; idx < cacheStats.Length; idx++)
					if (cacheStats[idx] != null && cacheStats[idx].RTHopen == rTHopen && cacheStats[idx].RTHclose == rTHclose && cacheStats[idx].EqualsInput(input))
						return cacheStats[idx];
			return CacheIndicator<Stats>(new Stats(){ RTHopen = rTHopen, RTHclose = rTHclose }, input, ref cacheStats);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Stats Stats(DateTime rTHopen, DateTime rTHclose)
		{
			return indicator.Stats(Input, rTHopen, rTHclose);
		}

		public Indicators.Stats Stats(ISeries<double> input , DateTime rTHopen, DateTime rTHclose)
		{
			return indicator.Stats(input, rTHopen, rTHclose);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Stats Stats(DateTime rTHopen, DateTime rTHclose)
		{
			return indicator.Stats(Input, rTHopen, rTHclose);
		}

		public Indicators.Stats Stats(ISeries<double> input , DateTime rTHopen, DateTime rTHclose)
		{
			return indicator.Stats(input, rTHopen, rTHclose);
		}
	}
}

#endregion
