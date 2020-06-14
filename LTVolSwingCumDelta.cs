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
	public class LTVolSwingCumDelta : Indicator
	{
		private NinjaTrader.NinjaScript.Indicators.LizardTrader.LT_Swing_Trend LT_Swing_Trend1;
		private bool upSwing = false;
		private int lastObservation = 0;
		private double lastSwingVolUp = 0.0;
		private double lastSwingVolDn = 0.0;
		private string trendMessage = "no message";
		private string messageS = "no message";
		private string messageL = "no message";
		private Brush	LineNowColor					= Brushes.Red;
		private bool deBug = false;
		private int swingTrend = 0;
		private double swingVol = 0.0;
		private	double swingSize = 0.0;
		
		private OrderFlowCumulativeDelta cumulativeDelta;
		private OrderFlowCumulativeDelta cumulativeDeltaRth;
		private double cumDeltaValue = 0.0;
		private string biasMessage = "no message";
		private Series<double> fastSeries;
		private int cumDeltaTrend = 0;
		private NinjaTrader.NinjaScript.Indicators.LizardIndicators.amaDSSBressert amaDSSBressert1;
		
		/// STATS
		private bool inLong = false;
		private double longEntryPrice = 0.0;
		private int longEntryBar = 0;
		private int longTradeCount = 0;
		private int longWingCount = 0;
		private List<int> longProfit = new List<int>();
		
		private bool inShort = false;
		private double shortEntryPrice = 0.0;
		private int shortEntryBar = 0;
		private int shortTradeCount = 0;
		private int shortWinCount = 0;
		private List<int> shortProfit = new List<int>();
		
		private int dailySum = 0;
		private List<int> dailySumArr = new List<int>();
		private int dailyAvg = 0;
		private bool systemOff = false;
		 
		//private string message = "no message";
		/// [X] Audio alerts -- download sounds
		/// [X] Stats 7 - 9 am  if low 
		/// [X] Avg Daily Gain?
		/// [X] Daily Gain Limit of 16T? Hurts Performance
		/// [X] Daily Loss Limit of 16T?
		/// [ ] Combine stats window
		/// [ ] Auto generate a chart
		/// [ ] Genetic optimize params
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "LT Vol Swing Cum Delta";
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
				AddPlot(Brushes.Orange, "CumSma");
				UpColor					= Brushes.DodgerBlue;
				DnColor					= Brushes.Red;
				ColorBars				= true;
				SetUpBars				= true;
				AudioAlerts				= true;
				StartTime				= DateTime.Parse("07:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime					= DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
				StopTicks 				= 12;
				TargetTicks				= 12;
				CalcStats				= true;
				BackgroundOpacity 		= 90;
				NoteLocation			= TextPosition.TopLeft;
				BackgroundColor			= Brushes.DimGray;
				FontColor				= Brushes.WhiteSmoke;
				OutlineColor			= Brushes.DimGray;
				NoteFont				= new SimpleFont("Arial", 12);
				MaxLoss = 16;
				MaxGain = 60;
				fastSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			else if (State == State.Configure)
			{
				ClearOutputWindow();
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{				
				LT_Swing_Trend1 = LT_Swing_Trend(Close, ltSwingTrendDeviationType.ATR, false, false, 72, 3, 5, 0.15);
				cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);
				cumulativeDeltaRth = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
				amaDSSBressert1				= amaDSSBressert(Close, 10, 3, 7, 80, 20);
			}
		}
		
		protected override void OnBarUpdate()
		{
		
			calcCumDelta();
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 0)
				return;
			calcVolumeSwings();
			
			if (CurrentBar > 34) {
				double theLows = MIN(Low, 120)[0];
				CumSma[0] = theLows - TickSize *8;
				calcBressert();
			}
			tradeStatsShorts();
			tradeStatsLong(); 
			
			// if eod, get daily sum
			DateTime endOfDay = DateTime.Parse("23:00", System.Globalization.CultureInfo.InvariantCulture);
			if (ToTime(Time[0]) >  ToTime(endOfDay) && ToTime(Time[1]) <  ToTime(endOfDay) ) { 
				Draw.VerticalLine(this, "MyVerticalLine"+CurrentBar, 0, Brushes.White);
				dailySumArr.Add(dailySum);
				dailySum = 0;
				systemOff = false;	
			} 
			if (CurrentBar < Count -2) return;
			if (dailySumArr.Count > 0 ) { 
				Print("---------------");
				foreach ( int i in dailySumArr) {
					Print(i);	
				}
				dailyAvg = Convert.ToInt16(dailySumArr.Average());
				Print("daily avg: " + dailyAvg);
				Print("---------------");
			}

			if (CalcStats) {
				string statsSring = "SYSTEM STATISTICS\n\n" + "gain " + MaxGain + " loss " + MaxLoss 
					+ "\ntarget " + TargetTicks + " stop " + StopTicks 
					+ "\n" + StartTime.ToShortTimeString() + " - " + EndTime.ToShortTimeString() 
					+ "\n\n"
					+ messageL + "\n\n" +  messageS + "\n" + "daily avg: " + dailyAvg;
				Draw.TextFixed(this, "myStatsFixed", 
					statsSring, 
					NoteLocation, 
					FontColor,  // text color
					NoteFont, 
					OutlineColor, // outline color
					BackgroundColor, 
					BackgroundOpacity);
			}
		}
		
		private void tradeStatsLong() {

			if (dailySum >= MaxGain - 1 && !systemOff) { 
				systemOff = true;	
				dailySum = MaxGain;
			}
			if (dailySum <= -MaxLoss + 1 && !systemOff) {
				systemOff = true;	
				dailySum = -MaxLoss;
			}
			if (systemOff) { return;}
			if ( !inLong || CurrentBar == longEntryBar || !CalcStats  ) { return; }
			double target = longEntryPrice + (TickSize * TargetTicks );
			double stop = longEntryPrice - (TickSize * StopTicks );
			if ( High[0] > target) {
				Draw.Dot(this, "LX"+CurrentBar, false, 0, target, Brushes.CornflowerBlue);
				inLong = false;
				longEntryPrice = 999999.0;
				longWingCount += 1;
				longProfit.Add(TargetTicks);
				dailySum += TargetTicks;
			} else if ( Low[0] <= stop){
				Draw.Dot(this, "LStp"+CurrentBar, false, 0, stop, Brushes.Red);
				inLong = false;
				longEntryPrice = 999999.0;
				if ( !systemOff ) {
					longProfit.Add(-StopTicks);
					dailySum -= StopTicks;
				} 
			}
			double winPct = (Convert.ToDouble( longWingCount) / Convert.ToDouble( longTradeCount)) * 100;
			string winPctSTring = String.Format("{0:0.#}", winPct);
			double sumProfit = longProfit.Sum();
			double sumDollars = (sumProfit * 12.5);
			messageL = longProfit.Count + " long trades\n" + longWingCount + " wins\n" + winPctSTring 
				+ "% long wins\n" + sumProfit.ToString("N0")  + " ticks\n" + sumDollars.ToString("c");  
		}
		
		private void tradeStatsShorts() {

			if (dailySum >=MaxGain - 1 && !systemOff) { 
				systemOff = true;	
				dailySum = MaxGain;
			}
			if (dailySum <= -MaxLoss + 1 && !systemOff) {
				systemOff = true;	
				dailySum = -MaxLoss;
			}
			if (systemOff) { return;}
			if ( !inShort || CurrentBar == shortEntryBar || !CalcStats ) { return; }
			double target = shortEntryPrice - (TickSize * TargetTicks );
			double stop = shortEntryPrice + (TickSize * StopTicks );
			if ( Low[0] < target) {
				Draw.Dot(this, "SX"+CurrentBar, false, 0, target, Brushes.Magenta);
				inShort = false;
				shortEntryPrice = -999999.0;
				shortWinCount += 1;
				shortProfit.Add(TargetTicks);
				dailySum += TargetTicks;
			} else if ( High[0] >= stop){
				Draw.Dot(this, "LStp"+CurrentBar, false, 0, stop, Brushes.Cyan);
				inShort = false;
				shortEntryPrice = 999999.0;
				shortProfit.Add(-StopTicks);
				dailySum -= StopTicks;
			}
			double winPct = (Convert.ToDouble( shortWinCount) / Convert.ToDouble( shortTradeCount)) * 100;
			string winPctSTring = String.Format("{0:0.#}", winPct);
			double sumProfit = shortProfit.Sum();
			double sumDollars = (sumProfit * 12.5);
		 	messageS = shortTradeCount + " short trades\n" + shortWinCount + " wins\n" + winPctSTring 
				+ "% short wins\n" + sumProfit.ToString("N0")  + " ticks\n" + sumDollars.ToString("c"); 
		}
		
		private void enterLong() {
			if (ToTime(Time[0]) < ToTime(StartTime) || ToTime(Time[0]) > ToTime(EndTime)  ) { return;} 
			if ( (inLong || !CalcStats) && !systemOff) { return; }
			if ( Close[0] > Open[0] ) {
				Draw.ArrowUp(this, "LE"+CurrentBar, false, 0, Low[0] - (TickSize * 3), Brushes.DodgerBlue);
				inLong = true;
				longEntryPrice = Close[0];
				longEntryBar = CurrentBar;
				longTradeCount += 1;
			} 
		}
		
		private void enterShort() {
			if (ToTime(Time[0]) < ToTime(StartTime) || ToTime(Time[0]) > ToTime(EndTime) ) { return;} 
			if ( ( inShort || !CalcStats ) && !systemOff) { return; }
			if ( Close[0] < Open[0] ) {
				Draw.ArrowDown(this, "SE"+CurrentBar, false, 0, High[0] + (TickSize * 3), Brushes.Red);
				inShort = true;
				shortEntryPrice = Close[0];
				shortEntryBar = CurrentBar;
				shortTradeCount += 1;
			} 
		}
		
		private void audioAlert(string sound) {
			if( !AudioAlerts) { return; }
			Alert("myAlert"+CurrentBar, Priority.High, "Trade Alert", 
			NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ sound,10, 
			Brushes.Black, Brushes.Yellow);  
		}
				
		private void calcBressert() {
			double dTrend = amaDSSBressert1.DTrend[0];
			double kTrend = amaDSSBressert1.KTrend[0];
				
			if ( (swingTrend + cumDeltaTrend) == 2 && dTrend < 0) {
				if( ColorBars ) {
					BarBrush = Brushes.DodgerBlue;
					CandleOutlineBrush = Brushes.DodgerBlue;
					audioAlert(sound: "smsAlert1.wav");
					enterLong();
				}
			} else if (ColorBars && swingTrend + cumDeltaTrend == 2 && kTrend < 0) {
				BarBrush = Brushes.Cyan;
				CandleOutlineBrush = Brushes.Cyan;
				audioAlert(sound: "smsAlert2.wav");
			}
			
			if ( (swingTrend + cumDeltaTrend) == -2 && dTrend > 0) {
				if( ColorBars ) {
					BarBrush = Brushes.Red;
					CandleOutlineBrush = Brushes.Red;
					audioAlert(sound: "smsAlert1.wav");
					enterShort();
				}
			} else if (swingTrend + cumDeltaTrend == -2 && kTrend > 0) {
				BarBrush = Brushes.Magenta;
				CandleOutlineBrush = Brushes.Magenta;
				audioAlert(sound: "smsAlert2.wav");
			}
		}
		
		private void calcCumDelta() {
			if (BarsInProgress == 1 && CurrentBars[0] > 35 )
			{
				// We have to update the secondary series of the hosted indicator to make sure the values we get in BarsInProgress == 0 are in sync
			    cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);
				cumulativeDeltaRth.Update(cumulativeDelta.BarsArray[1].Count - 1, 1); 
				fastSeries[0] = EMA(cumulativeDeltaRth.DeltaClose, 34)[0];
				
				// set cumulative delta avg 
				if ( fastSeries[0] >= 0.0  ) { 
					PlotBrushes[1][0] = Brushes.Cyan;
					biasMessage = "Weak Bull";
					cumDeltaTrend = 1;
					if (fastSeries[0] <= cumulativeDeltaRth.DeltaClose[0] ) {
						PlotBrushes[1][0] = Brushes.DodgerBlue;
						biasMessage = "Bull";
						cumDeltaTrend = 1;
					}
				} 
				
				if ( fastSeries[0]<= 0.0  ) { 
					PlotBrushes[1][0] = Brushes.Magenta;
					biasMessage = "Weak Bear";
					cumDeltaTrend = -1;
					if (fastSeries[0] >= cumulativeDeltaRth.DeltaClose[0] ) {
						PlotBrushes[1][0] = Brushes.Red;
						biasMessage = "Bear";
						cumDeltaTrend = -1;
					}
				}
				
			}
			
			//Draw.TextFixed(this, "MyTextFixed", biasMessage, TextPosition.TopRight);
		}
		
		
		private void calcVolumeSwings() {
			swingVol = LT_Swing_Trend1.SwingVolume[0];
			swingSize = LT_Swing_Trend1.SwingSize[0];
			if (swingVol > 0 && swingSize > 0 ) {
				if (upSwing) { 
					RemoveDrawObject( "up"+lastObservation);
				}
				if (swingVol > lastSwingVolDn ) {
					trendMessage = "Bullish";
					LineNowColor = UpColor;
					swingTrend = 1;
				} else {
					trendMessage = "Bearish";
					LineNowColor = DnColor;
					swingTrend = -1;
				}
				
				if ( deBug ) {
					string messages = swingVol.ToString() + "\n" + lastSwingVolDn.ToString() + "\n" + trendMessage;
					Draw.Text(this, "up"+CurrentBar, messages, 0, Low[0] - 2 * TickSize, Brushes.White); 
				}
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
					swingTrend = 1;
				} else {
					trendMessage = "Bearish";
					LineNowColor = DnColor;
					swingTrend = -1;
				}
				
				if ( deBug ) {
					string messages = swingVol.ToString() + "\n" + lastSwingVolUp.ToString() + "\n" + trendMessage;
					Draw.Text(this, "dn"+CurrentBar, messages, 0, High[0] + 2 * TickSize, Brushes.White); 
				}
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
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CumSma
		{
			get { return Values[1]; }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Up Color", Description="Chop zone color.", Order=1, GroupName="2. Visualize Swings")]
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
		[Display(Name="Dn Color", Description="Chop zone color.", Order=2, GroupName="2. Visualize Swings")]
		public Brush DnColor
		{ get; set; }
		
		[Browsable(false)]
		public string DnColorSerializable
		{
			get { return Serialize.BrushToString(DnColor); }
			set { DnColor = Serialize.StringToBrush(value); }
		}
		
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Color Signal Bars", GroupName = "Parameters", Order = 3)]
		public bool ColorBars
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Color Setup Bars", GroupName = "Parameters", Order = 4)]
		public bool SetUpBars
		{ get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "Audio Alerts", GroupName = "Parameters", Order = 5)]
		public bool AudioAlerts
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time", Order=6, GroupName="Parameters")]
		public DateTime StartTime
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time", Order=7, GroupName="Parameters")]
		public DateTime EndTime
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Ticks", Order=8, GroupName="Parameters")]
		public int StopTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Target Ticks", Order=9, GroupName="Parameters")]
		public int TargetTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MaxLoss", Order=10, GroupName="Parameters")]
		public int MaxLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MaxGain", Order=11, GroupName="Parameters")]
		public int MaxGain
		{ get; set; }
		
		// -------------------------------
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Stats", GroupName = "Stats", Order = 1)]
		public bool CalcStats
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Background Color", Description="Background Color", Order=10, GroupName="Stats")]
		public Brush BackgroundColor
		{ get; set; }

		[Browsable(false)]
		public string BackgroundColorSerializable
		{
			get { return Serialize.BrushToString(BackgroundColor); }
			set { BackgroundColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Font Color", Description="Font Color", Order=2, GroupName="Stats")]
		public Brush FontColor
		{ get; set; }

		[Browsable(false)]
		public string FontColorSerializable
		{
			get { return Serialize.BrushToString(FontColor); }
			set { FontColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="OutlineColor Color", Description="OutlineColor Color", Order=3, GroupName="Stats")]
		public Brush OutlineColor
		{ get; set; }

		[Browsable(false)]
		public string OutlineColorSerializable
		{
			get { return Serialize.BrushToString(OutlineColor); }
			set { OutlineColor = Serialize.StringToBrush(value); }
		}
		 
		[NinjaScriptProperty]
		[Display(Name="Note Font", Description="Note Font", Order=4, GroupName="Stats")]
		public SimpleFont NoteFont
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Background Opacity", Description="Background Opacity", Order=5, GroupName="Stats")]
		public int BackgroundOpacity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Note1Location", Description="Note Location", Order=6, GroupName="Stats")]
		public TextPosition NoteLocation
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private LTVolSwingCumDelta[] cacheLTVolSwingCumDelta;
		public LTVolSwingCumDelta LTVolSwingCumDelta(Brush upColor, Brush dnColor, DateTime startTime, DateTime endTime, int stopTicks, int targetTicks, int maxLoss, int maxGain, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return LTVolSwingCumDelta(Input, upColor, dnColor, startTime, endTime, stopTicks, targetTicks, maxLoss, maxGain, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public LTVolSwingCumDelta LTVolSwingCumDelta(ISeries<double> input, Brush upColor, Brush dnColor, DateTime startTime, DateTime endTime, int stopTicks, int targetTicks, int maxLoss, int maxGain, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			if (cacheLTVolSwingCumDelta != null)
				for (int idx = 0; idx < cacheLTVolSwingCumDelta.Length; idx++)
					if (cacheLTVolSwingCumDelta[idx] != null && cacheLTVolSwingCumDelta[idx].UpColor == upColor && cacheLTVolSwingCumDelta[idx].DnColor == dnColor && cacheLTVolSwingCumDelta[idx].StartTime == startTime && cacheLTVolSwingCumDelta[idx].EndTime == endTime && cacheLTVolSwingCumDelta[idx].StopTicks == stopTicks && cacheLTVolSwingCumDelta[idx].TargetTicks == targetTicks && cacheLTVolSwingCumDelta[idx].MaxLoss == maxLoss && cacheLTVolSwingCumDelta[idx].MaxGain == maxGain && cacheLTVolSwingCumDelta[idx].BackgroundColor == backgroundColor && cacheLTVolSwingCumDelta[idx].FontColor == fontColor && cacheLTVolSwingCumDelta[idx].OutlineColor == outlineColor && cacheLTVolSwingCumDelta[idx].NoteFont == noteFont && cacheLTVolSwingCumDelta[idx].BackgroundOpacity == backgroundOpacity && cacheLTVolSwingCumDelta[idx].NoteLocation == noteLocation && cacheLTVolSwingCumDelta[idx].EqualsInput(input))
						return cacheLTVolSwingCumDelta[idx];
			return CacheIndicator<LTVolSwingCumDelta>(new LTVolSwingCumDelta(){ UpColor = upColor, DnColor = dnColor, StartTime = startTime, EndTime = endTime, StopTicks = stopTicks, TargetTicks = targetTicks, MaxLoss = maxLoss, MaxGain = maxGain, BackgroundColor = backgroundColor, FontColor = fontColor, OutlineColor = outlineColor, NoteFont = noteFont, BackgroundOpacity = backgroundOpacity, NoteLocation = noteLocation }, input, ref cacheLTVolSwingCumDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.LTVolSwingCumDelta LTVolSwingCumDelta(Brush upColor, Brush dnColor, DateTime startTime, DateTime endTime, int stopTicks, int targetTicks, int maxLoss, int maxGain, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.LTVolSwingCumDelta(Input, upColor, dnColor, startTime, endTime, stopTicks, targetTicks, maxLoss, maxGain, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public Indicators.LTVolSwingCumDelta LTVolSwingCumDelta(ISeries<double> input , Brush upColor, Brush dnColor, DateTime startTime, DateTime endTime, int stopTicks, int targetTicks, int maxLoss, int maxGain, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.LTVolSwingCumDelta(input, upColor, dnColor, startTime, endTime, stopTicks, targetTicks, maxLoss, maxGain, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.LTVolSwingCumDelta LTVolSwingCumDelta(Brush upColor, Brush dnColor, DateTime startTime, DateTime endTime, int stopTicks, int targetTicks, int maxLoss, int maxGain, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.LTVolSwingCumDelta(Input, upColor, dnColor, startTime, endTime, stopTicks, targetTicks, maxLoss, maxGain, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public Indicators.LTVolSwingCumDelta LTVolSwingCumDelta(ISeries<double> input , Brush upColor, Brush dnColor, DateTime startTime, DateTime endTime, int stopTicks, int targetTicks, int maxLoss, int maxGain, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.LTVolSwingCumDelta(input, upColor, dnColor, startTime, endTime, stopTicks, targetTicks, maxLoss, maxGain, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}
	}
}

#endregion
