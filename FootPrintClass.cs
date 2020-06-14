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
	public class FootPrintClass : Indicator
	{
		private List<double> priceList = new List<double>();
		private double RangeValue = 0.0;
		private double barSize = 0.0;
		private NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType;
		private List<double> resistanceList = new List<double>();
		private List<double> supportList = new List<double>();
		private int lastBar = 0;
		
		private class BarData
		{
			public  double 	Price 			{ get; set; }
			public  bool 	BidImbalance	{ get; set; } 
			public  bool 	AskImbalance	{ get; set; }
			public	int 	Barnum			{ get; set; }
		}
		private List<BarData> BarList = new List<BarData>();
		
		private class Imbalances
		{
			public  double 	Price 			{ get; set; }
			public  bool 	BidImbalance	{ get; set; } 
			public  bool 	AskImbalance	{ get; set; }
			public	int 	Barnum			{ get; set; }
		}
		private List<Imbalances> ImbalanceList = new List<Imbalances>();
		
		//public Dictionary<int, double> SRdict = new Dictionary<int, double>();
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "FootPrint Class";
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
			}
			else if (State == State.Configure)
			{
			}
			else if(State == State.DataLoaded)
			{
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			if (Bars == null)
			return;
			if( CurrentBar < 10 ) { return;}
	        lastBar = CurrentBar - 1;
			// This sample assumes the Volumetric series is the primary DataSeries on the chart, if you would want to add a Volumetric series to a  
			// script, you could call AddVolumetric() in State.Configure and then for example use
			// NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType = BarsArray[1].BarsType as
			// NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;

			barsType = Bars.BarsSeries.BarsType as    
			NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;

			if (barsType == null)
			  return;
			
			Print("\n=========================================================================");
			Print("Bar: " + CurrentBar);
			SetUpPriceBar(); 
			try
			{
			  double price;
				/// need to see imabalance
				/// loop thru ask with the index of bid -1 and do math
				BarList.Clear(); 
				int index2 = 1;
				for (int index = 0; index <= priceList.Count; index++) 
				{ 
					/// eval bid = shift ask up, means increace bid index by 1
						double bidVol = barsType.Volumes[CurrentBar].GetBidVolumeForPrice(priceList[index +1] );
						double askVol = barsType.Volumes[CurrentBar].GetAskVolumeForPrice(priceList[index]);
					if ( index <= priceList.Count -1) { 
						Print(index + " Seeking bid imbal " + bidVol + " to ask vol " + askVol );
						if( (bidVol / 2) > askVol) { 
							BarData barData = new BarData();
							barData.Price = priceList[index+1];
							barData.BidImbalance = true;
							barData.Barnum = CurrentBar;
							BarList.Add(barData); 
							Print("\tbid " + bidVol + " has ask imbalance of " + askVol.ToString() + " red color");
							//Draw.Dot(this, "resistance"+CurrentBar+index, false, 0, priceList[index+1], Brushes.Red); 
						} 
					}
					if ( index2 <= priceList.Count - 1 ) { 
						/// eval ask = shift ask down, means decrease ask index by 1
						bidVol = barsType.Volumes[CurrentBar].GetBidVolumeForPrice(priceList[index2] );
						askVol = barsType.Volumes[CurrentBar].GetAskVolumeForPrice(priceList[index2 -1]);
						Print(index2 + " seeking ASK imbal " + askVol + " to bid vol " + bidVol );
						if( (askVol / 2) > bidVol ) { 
							BarData barData2 = new BarData();
							barData2.Price = priceList[index2-1];
							barData2.Barnum = CurrentBar;
							barData2.AskImbalance = true;
							BarList.Add(barData2);
							Print("\task " + askVol + " has ask imbalance of " + bidVol.ToString() + " blue color");
							//Draw.Dot(this, "support"+CurrentBar+index, false, -1, priceList[index2-1], Brushes.DodgerBlue);
						}
						index2 += 1;
					}  
				} 
			}
			catch{}
				
			RemoveDuplicates();
			FindConsecutiveZones();
			//Draw.Text(this, "CurrentBar"+CurrentBar, CurrentBar.ToString(), 0, High[0] + 2 * TickSize, Brushes.AntiqueWhite);
			
			/// prove the list
			Print("Bar: " + CurrentBar);
			Print("----------------------------------");
			double lastPrice = 0.0;
			for (int i = 0; i < BarList.Count; i++) 
			{
				Print(BarList[i].Price.ToString("N2") + " \t\t" + BarList[i].BidImbalance + " \t\t" + BarList[i].AskImbalance );
			} 

			/// remove old ask imbalances
			for (int index = 0; index < ImbalanceList.Count; index++) 
			{
				if( High[0]  > ImbalanceList[index].Price + 0.5 && ImbalanceList[index].AskImbalance) {
					Print("--> \tRemoved Price " + ImbalanceList[index].Price);
					ImbalanceList.RemoveAt(index); 
				} else if( Low[0]  < ImbalanceList[index].Price - 0.5 && ImbalanceList[index].BidImbalance) {
					Print("--> \tRemoved Price " + ImbalanceList[index].Price);
					ImbalanceList.RemoveAt(index); 
				}
			}
			
			/// show ask imbalances
			foreach(Imbalances row in ImbalanceList)
			{
				if( row.AskImbalance) { 
					string name = "resistLine"+row.Price+row.Barnum;
					RemoveDrawObject(name + lastBar);
					int lineLength = CurrentBar - row.Barnum; 
					Draw.Rectangle(this, name + CurrentBar, false, lineLength , row.Price, 1, row.Price +TickSize, Brushes.Transparent, Brushes.Red, 20); 
				}
				if( row.BidImbalance) { 
					string name = "supportLine"+row.Price+row.Barnum;
					RemoveDrawObject(name + lastBar);
					int lineLength = CurrentBar - row.Barnum; 
					Draw.Rectangle(this, name + CurrentBar, false, lineLength , row.Price, 1, row.Price +TickSize, Brushes.Transparent, Brushes.DodgerBlue, 20); 
					
				}
			}
		}
		
		private void FindConsecutiveZones() {
			double lastPrice = 0.0;
			for (int index = 0; index < BarList.Count(); index++)  {
				Print("Checking consec in " + BarList[index].Price + " from last of " + lastPrice);
				if (lastPrice - TickSize == BarList[index].Price) {
					Print("\t\t\t found a consecutive price of " + BarList[index].Price + " from last of " + lastPrice);
					if( BarList[index].BidImbalance && BarList[index-1].BidImbalance ) {
						//Draw.Dot(this, "consec"+CurrentBar+index, false, 0,BarList[index].Price, Brushes.Red); 
						Imbalances imb = new Imbalances(); 
						imb.Price 			= BarList[index].Price;
						imb.BidImbalance	= false; 
						imb.AskImbalance	= true;
						imb.Barnum			= CurrentBar; 
						ImbalanceList.Add(imb);
					} 
					if( BarList[index].AskImbalance && BarList[index-1].AskImbalance ) {
						//Draw.Dot(this, "consec"+CurrentBar+index, false, 0,BarList[index].Price, Brushes.DodgerBlue); 
						Imbalances imb = new Imbalances(); 
						imb.Price 			= BarList[index].Price;
						imb.BidImbalance	= true; 
						imb.AskImbalance	= false;
						imb.Barnum			= CurrentBar; 
						ImbalanceList.Add(imb);
					}
					
				}
				lastPrice = BarList[index].Price;
			}
			
		}
		
		private void RemoveDuplicates() {
			double lastPrice = 0.0;
			for (int index = 0; index < BarList.Count(); index++) 
			{
				if  ( lastPrice == BarList[index].Price ) {
					Print("\t\t*** FOUND DUPLICATE ***  " + index);
					BarList[index].AskImbalance = true;
					BarList[index].BidImbalance = true;
					if( index > 1 ) {
					
						int lastIndex = index - 1;
						Print("Lets remove element at " + lastIndex + " price " + BarList[lastIndex].Price.ToString()  + " array size " + BarList.Count());
						BarList.RemoveAt(lastIndex); 
					}
					Print(" array size " + BarList.Count() + " accessing index " + index);
					//index -= 1;
					lastPrice = BarList[index-1].Price;
				} else {
					lastPrice = BarList[index].Price; 
				}
			}
		}
		
		private void SetUpPriceBar() {
			/// set up current bar size
			RangeValue = Range()[0];
			barSize = RangeValue / TickSize;
			Print("Ask for 86.5= 155: " + barsType.Volumes[CurrentBar].GetAskVolumeForPrice(3386.5));
			Print("The barSize is " + barSize.ToString());
			
			/// add bar prices to a List 
			double ticksFromHigh = 0.0;
			priceList.Clear(); 
			for (int index = 0; index <= barSize; index++) 
			{
				double price = High[0] - ticksFromHigh;
				priceList.Add(price);	
				ticksFromHigh += 0.25;
			}
			
			string myArray = string.Join(", ", priceList);
			Print(myArray); 
			Print("");
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FootPrintClass[] cacheFootPrintClass;
		public FootPrintClass FootPrintClass()
		{
			return FootPrintClass(Input);
		}

		public FootPrintClass FootPrintClass(ISeries<double> input)
		{
			if (cacheFootPrintClass != null)
				for (int idx = 0; idx < cacheFootPrintClass.Length; idx++)
					if (cacheFootPrintClass[idx] != null &&  cacheFootPrintClass[idx].EqualsInput(input))
						return cacheFootPrintClass[idx];
			return CacheIndicator<FootPrintClass>(new FootPrintClass(), input, ref cacheFootPrintClass);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FootPrintClass FootPrintClass()
		{
			return indicator.FootPrintClass(Input);
		}

		public Indicators.FootPrintClass FootPrintClass(ISeries<double> input )
		{
			return indicator.FootPrintClass(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FootPrintClass FootPrintClass()
		{
			return indicator.FootPrintClass(Input);
		}

		public Indicators.FootPrintClass FootPrintClass(ISeries<double> input )
		{
			return indicator.FootPrintClass(input);
		}
	}
}

#endregion
