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

using System.Windows.Media.Imaging;
using System.Net.Mail;
using System.Net.Mime;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class EMJTrend : Indicator
	{ 
		private DashStyleHelper					dashStyle					= DashStyleHelper.Dash;
		private int trend = 0;
		
		private Series<double> fastSeries;
		private Series<double> slowSeries;
		
		private int chopCounter;
		private int lastBar;
		private double chopHigh;
		private double chopLow;
		private double resistance;
		private double support;
		private int left;
		private int right;
		private string name = "";
		NinjaTrader.Gui.Chart.Chart 	chart;
        BitmapFrame 					outputFrame;
		private bool shortEnabled;
		private bool longEnabled;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "EMJTrend";
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
				Slow					= 116;
				Medium					= 68;
				Fast					= 34;
				PlotAverages 			= false;
				PlotChop				= true;
				MajorWaveUpColor					= Brushes.Goldenrod;
				ChopOpacity					= 30;
				PlotKeltnerEntries = false;
				ColorBars				= true;
				UpColor					= Brushes.DodgerBlue;
				DnColor					= Brushes.Red;
				
				AddPlot(new Stroke(Brushes.DarkGoldenrod, 8), PlotStyle.Line, "SlowPlot");
				AddPlot(new Stroke(Brushes.MediumBlue, 4), PlotStyle.Line, "MedPlot");
				AddPlot(new Stroke(Brushes.Red, 1), PlotStyle.Line, "FastPlot");
				
				fastSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
				slowSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);

			}
			else if (State == State.Configure)
			{
				Plots[2].DashStyleHelper = dashStyle;	
				
				Dispatcher.BeginInvoke(new Action(() =>
				{
					chart = Window.GetWindow(ChartControl) as Chart;
				}));
			
			}
		}

		protected override void OnBarUpdate()
		{
			name = Bars.Instrument.MasterInstrument.Name;
			int opacity = 30;
			if (CurrentBar < Slow) { return; }
			lastBar = CurrentBar -1;
			double fastAvg = EMA(Close, Fast)[0];
			fastSeries[0] = EMA(Close, Fast)[0];
			double medAvg = EMA(Close, Medium)[0];
			double slowAvg = EMA(Close, Slow)[0];
			slowSeries[0] = EMA(Close, Slow)[0];
			trend = 0;
			
			if (fastAvg >= medAvg && medAvg >= slowAvg && High[0] > fastAvg ) {
				trend = 1;
			} 
			if (fastAvg < medAvg && medAvg < slowAvg && Low[0] < fastAvg) {
				trend = -1;
			} 
			
			if ( ColorBars ) {
			 	BarBrush = MajorWaveUpColor;
				CandleOutlineBrush  = MajorWaveUpColor;
			}
			
			switch (trend) {
				case 1:
					PlotBrushes[0][0] = UpColor;
					PlotBrushes[1][0] = UpColor;
					PlotBrushes[2][0] = UpColor;
					chopCounter = 0;
					if ( ColorBars ) {
						BarBrush = UpColor;
						CandleOutlineBrush  = UpColor;
					}
					break;
				case -1:
					PlotBrushes[0][0] = DnColor;
					PlotBrushes[1][0] = DnColor;
					PlotBrushes[2][0] = DnColor;
					chopCounter = 0;
					if ( ColorBars ) {
						BarBrush = DnColor;
						CandleOutlineBrush  = DnColor;
					}
					break;
				case 0:
					chopCounter +=1;
					PlotBrushes[0][0] = MajorWaveUpColor;
					PlotBrushes[1][0] = MajorWaveUpColor;
					PlotBrushes[2][0] = MajorWaveUpColor;
					showChop();
					break;
				default:
					
					break;
			}	
			if ( PlotAverages) {
				SlowPlot[0] = slowAvg;
				MedPlot[0] = medAvg;
				FastPlot[0] = fastAvg;
			}
			setArrows(trend: trend);
			showKeltnerEntries() ;
		}

		private void showChop() {
			if ( PlotChop ) {
				// arrow and circle position
				chopHigh = MAX(High,chopCounter)[1];
				chopLow = MIN(Low,chopCounter)[1];
				RemoveDrawObject("chop"+lastBar);
				Draw.Rectangle(this, "chop"+CurrentBar, true, chopCounter, chopLow, 0, chopHigh, 
						Brushes.Transparent, MajorWaveUpColor, ChopOpacity);
			}
		}
		
		private void showKeltnerEntries() {
			if ( PlotKeltnerEntries ) {
				if (CrossBelow(Low, KeltnerChannel(Close, 1.25, 13).Lower[0],1 )) {
					shortEnabled = true;
				}
				if (CrossAbove(High, KeltnerChannel(Close, 1.25, 13).Upper[0],1 )) {
					longEnabled = true;
				}
				// short 
				if ( shortEnabled && trend == -1 && CrossAbove(High, SMA(13), 1) ) {
					Draw.ArrowDown(this, "short"+CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
					sendAlert(message: "Short Pullback Entry", sound: "ES_EnteringShortZone.wav");
					shortEnabled = false;
				}
				if ( longEnabled && trend == 1 && CrossBelow(Low, SMA(13), 1) ) {
					Draw.ArrowUp(this, "long"+CurrentBar, false, 0, Low[0] - TickSize, Brushes.DodgerBlue);
					sendAlert(message: "Long Pullback Entry", sound: "ES_EnteringLongZone.wav");
					longEnabled = false;
				}
			}
		}
		
		private void sendAlert(string message, string sound ) {
			message += " " + Bars.Instrument.MasterInstrument.Name;
			Alert("myAlert"+CurrentBar, Priority.High, message, NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ sound,10, Brushes.Black, Brushes.Yellow);  
			if (CurrentBar < Count -2) return;
			SendMailChart(name + " Alert",message,"ticktrade10@gmail.com","13103824522@tmomail.net","smtp.gmail.com",587,"ticktrade10","WH2403wh");
		}
		
		private void SendMailChart(string Subject, string Body, string From, string To, string Host, int Port, string Username, string Password)
		{
			try	
			{	
				Dispatcher.BeginInvoke(new Action(() =>
				{
						if (chart != null)
				        {
							
							RenderTargetBitmap	screenCapture = chart.GetScreenshot(ShareScreenshotType.Chart);
		                    outputFrame = BitmapFrame.Create(screenCapture);
							
		                    if (screenCapture != null)
		                    {
								PngBitmapEncoder png = new PngBitmapEncoder();
		                        png.Frames.Add(outputFrame);
								System.IO.MemoryStream stream = new System.IO.MemoryStream();
								png.Save(stream);
								stream.Position = 0;
							
								MailMessage theMail = new MailMessage(From, To, Subject, Body);
								System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(stream, "image.png");
								theMail.Attachments.Add(attachment);
							
								SmtpClient smtp = new SmtpClient(Host, Port);
								smtp.EnableSsl = true;
								smtp.Credentials = new System.Net.NetworkCredential(Username, Password);
								string token = Instrument.MasterInstrument.Name + ToDay(Time[0]) + " " + ToTime(Time[0]) + CurrentBar.ToString();
								
								Print("Sending Mail!");
								smtp.SendAsync(theMail, token);
				            }
						}
				}));
			}
			catch (Exception ex) {
				Print("Sending " + name + "Chart email failed -  " + ex);
			}
		}
		
		private void setArrows(int trend) {
			
			var Bars					= 10;
			var	Size					= 6;
			var	Forward					= 5;
			resistance = MAX(High,Bars)[0];
			support = MIN(Low,Bars)[0];
			var middle = -(3 + Forward);
			left = -Forward;
			right  = -(Size+Forward);
			
			if ( !PlotArrows) { return;}
			RemoveDrawObject("sell"+lastBar);
			RemoveDrawObject("buy"+lastBar);
			RemoveDrawObject("top"+lastBar);
			RemoveDrawObject("bottom"+lastBar);
			
			if (trend == -1) {
				Draw.Triangle(this, "sell"+ CurrentBar,true, left, resistance + ((Size-3) * TickSize) , middle , resistance, 
				right, resistance + ((Size-3) * TickSize), Brushes.Transparent, Brushes.Red,100);
			} else  if (trend == 1) {
				Draw.Triangle(this, "buy"+ CurrentBar,true, left, support - ((Size-3) * TickSize) , middle, support, 
				right, support - ((Size-3) * TickSize) , Brushes.Transparent, Brushes.DodgerBlue,100);
			} else {
				// plot oscillator and green circles
				showRange(circles: false);
			}
		}
		
		private void showRange(bool circles) {
			if ( circles ) {
				Draw.Ellipse(this, "top"+CurrentBar, true, left, resistance + (3*TickSize), right, resistance, 
					Brushes.Transparent, Brushes.DarkGray, 80);	
				Draw.Ellipse(this, "bottom"+CurrentBar, true, left, support + (3*TickSize), right, support, 
					Brushes.Transparent, Brushes.DarkGray, 80);
			} else {
				Draw.Rectangle(this, "top"+CurrentBar, false, left-2, support - (3*TickSize), right, resistance + (3*TickSize), 
					Brushes.Transparent,MajorWaveUpColor, ChopOpacity);
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Slow", Order=1, GroupName="Parameters")]
		public int Slow
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Medium", Order=2, GroupName="Parameters")]
		public int Medium
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fast", Order=3, GroupName="Parameters")]
		public int Fast
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Averages", GroupName = "NinjaScriptParameters", Order = 4)]
		public bool PlotAverages
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Arrows", GroupName = "NinjaScriptParameters", Order = 5)]
		public bool PlotArrows
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Chop", GroupName = "NinjaScriptParameters", Order = 6)]
		public bool PlotChop
		{ get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Keltner Entries", GroupName = "NinjaScriptParameters", Order = 7)]
		public bool PlotKeltnerEntries
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Chop Opacity", Order=8, GroupName="Parameters")]
		public int ChopOpacity
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SlowPlot
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MedPlot
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FastPlot
		{
			get { return Values[2]; }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Consolidation Color", Description="Chop zone color.", Order=9, GroupName="Parameters")]
		public Brush MajorWaveUpColor
		{ get; set; }
		
		[Browsable(false)]
		public string MajorWaveUpColorSerializable
		{
			get { return Serialize.BrushToString(MajorWaveUpColor); }
			set { MajorWaveUpColor = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "ColorBars", GroupName = "NinjaScriptParameters", Order = 10)]
		public bool ColorBars
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Up Color", Description="Chop zone color.", Order=11, GroupName="Parameters")]
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
		[Display(Name="Dn Color", Description="Chop zone color.", Order=12, GroupName="Parameters")]
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
		private EMJTrend[] cacheEMJTrend;
		public EMJTrend EMJTrend(int slow, int medium, int fast, int chopOpacity, Brush majorWaveUpColor, Brush upColor, Brush dnColor)
		{
			return EMJTrend(Input, slow, medium, fast, chopOpacity, majorWaveUpColor, upColor, dnColor);
		}

		public EMJTrend EMJTrend(ISeries<double> input, int slow, int medium, int fast, int chopOpacity, Brush majorWaveUpColor, Brush upColor, Brush dnColor)
		{
			if (cacheEMJTrend != null)
				for (int idx = 0; idx < cacheEMJTrend.Length; idx++)
					if (cacheEMJTrend[idx] != null && cacheEMJTrend[idx].Slow == slow && cacheEMJTrend[idx].Medium == medium && cacheEMJTrend[idx].Fast == fast && cacheEMJTrend[idx].ChopOpacity == chopOpacity && cacheEMJTrend[idx].MajorWaveUpColor == majorWaveUpColor && cacheEMJTrend[idx].UpColor == upColor && cacheEMJTrend[idx].DnColor == dnColor && cacheEMJTrend[idx].EqualsInput(input))
						return cacheEMJTrend[idx];
			return CacheIndicator<EMJTrend>(new EMJTrend(){ Slow = slow, Medium = medium, Fast = fast, ChopOpacity = chopOpacity, MajorWaveUpColor = majorWaveUpColor, UpColor = upColor, DnColor = dnColor }, input, ref cacheEMJTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMJTrend EMJTrend(int slow, int medium, int fast, int chopOpacity, Brush majorWaveUpColor, Brush upColor, Brush dnColor)
		{
			return indicator.EMJTrend(Input, slow, medium, fast, chopOpacity, majorWaveUpColor, upColor, dnColor);
		}

		public Indicators.EMJTrend EMJTrend(ISeries<double> input , int slow, int medium, int fast, int chopOpacity, Brush majorWaveUpColor, Brush upColor, Brush dnColor)
		{
			return indicator.EMJTrend(input, slow, medium, fast, chopOpacity, majorWaveUpColor, upColor, dnColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMJTrend EMJTrend(int slow, int medium, int fast, int chopOpacity, Brush majorWaveUpColor, Brush upColor, Brush dnColor)
		{
			return indicator.EMJTrend(Input, slow, medium, fast, chopOpacity, majorWaveUpColor, upColor, dnColor);
		}

		public Indicators.EMJTrend EMJTrend(ISeries<double> input , int slow, int medium, int fast, int chopOpacity, Brush majorWaveUpColor, Brush upColor, Brush dnColor)
		{
			return indicator.EMJTrend(input, slow, medium, fast, chopOpacity, majorWaveUpColor, upColor, dnColor);
		}
	}
}

#endregion
