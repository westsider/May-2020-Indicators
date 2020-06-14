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
using System.IO;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class OutputData : Indicator
	{
		private double open_D = 0.0;
		private double close_D = 0.0;
		private double gap = 0.0;
		private double overNightVolume = 0.0;
		private double rthVolume = 0.0;
		private double preOpenGap = 0.0;
		private int 	startTime 	= 930; 
		private int	 	endTime 	= 1615;
		private int		ninja_Start_Time;
		private int		ninja_End_Time;
		private int	 	IBendTime 	= 1030;
		private int		ninja_IB_End_Time;
		private string 	yDate;
		private bool 	sunday = false;
		private bool 	firstRthTick = false;
		private bool 	lastRthTick = false;
		private string 	gapMessage = "";
		private string  gapAverageString = "";
		private double 	halfGap = 0.0;
		private int 	overNightBars = 0;
		private int 	firstTenMinBars = 0;
		private double 	gxRange = 0.0;
		private double 	firstTenMinRange = 0.0;
		private double 	firstTenMinGain = 0.0;
		private double 	firstTenMinRangeZB = 0.0;
		private double 	firstTenMinGainZB = 0.0;
		private int 	ibBars = 0;
		private double	ibRange = 0.0;
		private double  ibGain = 0.0;
		private bool	onHigh = true;
		private bool 	headerIn = false;
		
		private int 	rthBars = 0;
		private double		rthRange = 0.0;
		private double	rthChange = 0.0;
		private double		priotRthRange = 0.0;
		private double	priorRthChange = 0.0;
		
		private string csvPath = @"C:\Users\trade\Documents\_Send_To_Mac\";  // @"C:\Users\trade\Desktop\ES_IB_Data.csv";
        
		
		List<double> gapList = new List<double>();
 		List<double> gxRangeList = new List<double>();
		List<double> gxVolList = new List<double>();
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Output Data";
				Calculate									= Calculate.OnEachTick;
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
				MaxDays = 10;
				BianaryOutput = true;
			}
			else if (State == State.Configure)
			{
				// AddDataSeries("ZB 09-19", BarsPeriodType.Minute, 5); 
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
			
			AddHeader();
			if (CurrentBar < 2 ) { return;}
			if ( Bars.IsLastBarOfSession ) { firstRthTick = false;}
			if ( Bars.IsLastBarOfSession ) { lastRthTick = false;}
	
			
			//MeasureFirst10MinZB();
			holidayOrSunday();
			MeasureOvernight();
			MeasureFirst10Min();
			MeasureIB();
			MeasureRTH();
			
			/// close
			if (ToTime(Time[1])<= ninja_End_Time && ToTime(Time[0]) >= ninja_End_Time && !sunday &&  !lastRthTick ) {
				close_D = Open[0];
				lastRthTick = true; 
				AddNewLine(); 
				/// reset vars for next session
				overNightVolume = 0.0;
				overNightBars = 0;
				gxRange = 0.0;
				firstTenMinBars = 0;
				ibBars = 0; 
				rthBars = 0;
			}
	
			
			/// open
			if (ToTime(Time[1])<= ninja_Start_Time && ToTime(Time[0]) >= ninja_Start_Time && !sunday && !firstRthTick ) {
				if (IsFirstTickOfBar) {
					Print("");
					open_D = Open[0];
					firstRthTick = true;
					gap = open_D - close_D;
					if ( gap > 500 ) { gap = 2.0;}
					// save each gap at open
					//PersistGap();
					//PersistGxVol();
					//PersistGxRange();					
					CalcGxVolume(preMarket: false);
					CalcGxRange(preMarket: false);
					
				}
			}

			///MARK: - TODO - 10delta
			///MARK: - TODO - gxdelta

		}
		
		private string FormatDateTime() {
			DateTime myDate = Time[0];  // DateTime type
			string prettyDate = myDate.ToString("M/d/yyyy") + " " + myDate.ToString("hh:mm");
			//Print(prettyDate);
			return prettyDate;
		}
		
		private void AddNewLine() {
			if ( BarsInProgress == 1  ) { return;}
            /// date, priorChange, priorRange, gxRange, gxVol, gap, 10range, 10change, ibDir  // or ibRange
            double normalizedVol = overNightVolume * 0.00001;

            string thisLine = FormatDateTime() + ", " + priorRthChange + ", " + priotRthRange + 
				", " + gxRange + ", " + normalizedVol + ", " + gap + ", " +  firstTenMinRange + 
				", " + firstTenMinGain + ", " + ibGain;
			Print(thisLine );
			WriteFile(path: csvPath, newLine: thisLine, header: false);
			//FormatDateTime();
		}
		
		private void AddHeader() {
			if (CurrentBar < 2 ) {  
				if ( !headerIn && BarsInProgress == 0  ) {
					SetFileName();
					string header = "Date, PriorChange, PriorRange, GxRange, GxVol, Gap, 10range, 10change, IBdir";  // or ibRange
					Print(header);
					WriteFile(path: csvPath, newLine: header, header: true);
					headerIn = true;
				}
				return;
			}
		}
		
		private void SetFileName() {
			string inst = Instrument.FullName;
			string instOnly = inst.Remove(inst.Length-6);
			DateTime myDate = DateTime.Today;  // DateTime type
			string prettyDate = myDate.ToString("M_d_yyyy");
			string instDate = instOnly + "_" + prettyDate + ".csv";
			csvPath += instDate;
			Print(instDate);
		}
		private void WriteFile(string path, string newLine, bool header)
        {
			if ( header ) {
				ClearFile(path: csvPath);
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
		
		private void CalcGxRange(bool preMarket) {
			gxRange = MAX(High, overNightBars)[1] - MIN(Low, overNightBars)[1];
		}
		
		private void CalcGxVolume(bool preMarket) {
			string volString = overNightVolume.ToString();
		}
		
		private void MeasureOvernight() {
			if (ToTime(Time[0])> ninja_End_Time || ToTime(Time[0]) < ninja_Start_Time) {
				overNightVolume += Volume[0];
				overNightBars += 1;
			}
		}
		
		private void MeasureFirst10Min() {
			if (ToTime(Time[0])> ninja_Start_Time && ToTime(Time[0]) <= ninja_Start_Time + 1000 ) {
				firstTenMinBars += 1;
				//Draw.Dot(this, "firstTenMinBars"+CurrentBar, false, 0, Low[0] - TickSize, Brushes.White); 
			}
			if (ToTime(Time[0]) == ninja_Start_Time + 1000 ) {
				firstTenMinRange = MAX(High, firstTenMinBars)[0] - MIN(Low, firstTenMinBars)[0];
				firstTenMinGain = Close[0] - open_D;
				//Draw.Text(this, "firstTenMinRange"+CurrentBar, firstTenMinGain.ToString(), 0, High[0] + 4 * TickSize, Brushes.Cyan); 
			}
		}

		
		private void MeasureIB() {
			if (ToTime(Time[0])> ninja_Start_Time && ToTime(Time[0]) <= ninja_IB_End_Time) {
				ibBars += 1;
				//Draw.Dot(this, "ibBars"+CurrentBar, false, 0, Low[0] - TickSize, Brushes.White); 
			}
			if (ToTime(Time[0]) == ninja_IB_End_Time ) {
				ibRange = MAX(High, ibBars)[0] - MIN(Low, ibBars)[0];
				ibGain = Close[0] - open_D;
				if ( BianaryOutput ) {
					if ( ibGain > 0 ) { ibGain = 1; } else { ibGain = 0; }	
				}
				//Draw.Text(this, "ibRange"+CurrentBar, ibGain.ToString(), 0, High[0] + 4 * TickSize, Brushes.Cyan); 
			}
		}
		
		private void MeasureRTH() {
			if (ToTime(Time[0])> ninja_Start_Time && ToTime(Time[0]) <= ninja_End_Time) {
				rthBars += 1;
				//Draw.Dot(this, "rthBars"+CurrentBar, false, 0, Low[0] - TickSize, Brushes.White); 
			}
			if (rthBars == 0 ) { return; }
			if (ToTime(Time[0]) == ninja_End_Time ) {
				priotRthRange = rthRange;
				priorRthChange = rthChange;
				rthRange = MAX(High, rthBars)[0] - MIN(Low, rthBars)[0];
				rthChange = Close[0] - open_D;
				//Draw.Text(this, "ibRrthChangeange"+CurrentBar, rthRange.ToString(), 0, High[0] + 4 * TickSize, Brushes.AntiqueWhite); 
			}
		}
		
//		private void PersistGap() {
//			if ( gap > 500 ) { gap = 2.0;}
//			//if ( gap < 0 ) { gap = gap * -1;}
//			gapList.Add(gap);  
//		}
		
		private void PersistGxVol() {
			gxVolList.Add(overNightVolume);
		}
		
		private void PersistGxRange() {
			gxRangeList.Add(gxRange);
		}
		
		private void holidayOrSunday() {
			
			yDate = Time[0].DayOfWeek.ToString();
			if (yDate == "Sunday" ) {
				//Print("Sunday");
				sunday = true;
				Draw.Dot(this, "sunday"+CurrentBar, false, 0, Low[0] - TickSize, Brushes.Yellow);
				
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
		[Display(Name="Max Days", Order=1, GroupName="Parameters")]
		public int MaxDays
		{ get; set; }
		
		//
		[NinjaScriptProperty]
		[Display(Name="Binary Output", Description="BianaryOutput", Order=2, GroupName="Parameters")]
		public bool BianaryOutput 
		{ get; set; }
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OutputData[] cacheOutputData;
		public OutputData OutputData(int maxDays, bool bianaryOutput)
		{
			return OutputData(Input, maxDays, bianaryOutput);
		}

		public OutputData OutputData(ISeries<double> input, int maxDays, bool bianaryOutput)
		{
			if (cacheOutputData != null)
				for (int idx = 0; idx < cacheOutputData.Length; idx++)
					if (cacheOutputData[idx] != null && cacheOutputData[idx].MaxDays == maxDays && cacheOutputData[idx].BianaryOutput == bianaryOutput && cacheOutputData[idx].EqualsInput(input))
						return cacheOutputData[idx];
			return CacheIndicator<OutputData>(new OutputData(){ MaxDays = maxDays, BianaryOutput = bianaryOutput }, input, ref cacheOutputData);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OutputData OutputData(int maxDays, bool bianaryOutput)
		{
			return indicator.OutputData(Input, maxDays, bianaryOutput);
		}

		public Indicators.OutputData OutputData(ISeries<double> input , int maxDays, bool bianaryOutput)
		{
			return indicator.OutputData(input, maxDays, bianaryOutput);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OutputData OutputData(int maxDays, bool bianaryOutput)
		{
			return indicator.OutputData(Input, maxDays, bianaryOutput);
		}

		public Indicators.OutputData OutputData(ISeries<double> input , int maxDays, bool bianaryOutput)
		{
			return indicator.OutputData(input, maxDays, bianaryOutput);
		}
	}
}

#endregion
