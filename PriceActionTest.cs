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
	public class PriceActionTest : Indicator
	{
		private Series<bool> upFlip;
        private Series<bool> dnFlip;
		private Swings swingHigh = new Swings();
        private Swings swingLow = new Swings();
		private SwingCurrent swingCurrent = new SwingCurrent();
		private List<SwingStruct> swingHighs = new List<SwingStruct>(); 
        private List<SwingStruct> swingLows = new List<SwingStruct>();
		private SwingProperties swingProperties;
		private SwingLengthStyle swingLengthType = SwingLengthStyle.Points;
		private SwingDurationStyle swingDurationType = SwingDurationStyle.Bars;
		private bool showSwingPrice = false;
		private bool showSwingLabel = false;
		private bool showSwingPercent = false;
		private SwingTimeStyle swingTimeType = SwingTimeStyle.False;
		private SwingVolumeStyle swingVolumeType = SwingVolumeStyle.Absolute;
		private VisualizationStyle visualizationType = VisualizationStyle.ZigZagVolume;
		private bool useBreakouts = true;
		private bool ignoreInsideBars = true;
		private Brush zigZagColorUp = Brushes.LimeGreen; 
        private Brush zigZagColorDn = Brushes.OrangeRed;
		private DashStyleHelper zigZagStyle = DashStyleHelper.Solid; 
        private int zigZagWidth = 3;
		private Brush textColorHigherHigh = Brushes.White; 
        private Brush textColorLowerHigh = Brushes.Black; 
        private Brush textColorDoubleTop = Brushes.Gold; 
        private Brush textColorHigherLow = Brushes.White; 
        private Brush textColorLowerLow = Brushes.Black; 
        private Brush textColorDoubleBottom = Brushes.Gold;
		private int textOffsetLength = 15;
		private int textOffsetPercent = 90;
		private int textOffsetPrice = 45; 
        private int textOffsetLabel = 60; 
        private int textOffsetTime = 75; 
        private int textOffsetVolume = 30;
		private int decimalPlaces;
		
		private Brush	LineNowColor					= Brushes.Red;
		private String message = "No Message";
		private SimpleFont textFont = new SimpleFont("Courier", 15);
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Price Action Test";
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
				
				AddPlot(new Stroke(Brushes.Gold, DashStyleHelper.Solid, 3), PlotStyle.Dot, "DoubleBottom");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 3), PlotStyle.Dot, "LowerLow");
				AddPlot(new Stroke(Brushes.Green, DashStyleHelper.Solid, 3), PlotStyle.Dot, "HigherLow");
				AddPlot(new Stroke(Brushes.Gold, DashStyleHelper.Solid, 3), PlotStyle.Dot, "DoubleTop");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 3), PlotStyle.Dot, "LowerHigh");
				AddPlot(new Stroke(Brushes.Green, DashStyleHelper.Solid, 3), PlotStyle.Dot, "HigherHigh");
				AddPlot(new Stroke(Brushes.Blue, DashStyleHelper.Solid, 1), PlotStyle.Square, "GannSwing");
				AddPlot(new Stroke(Brushes.DarkGoldenrod, 3), PlotStyle.Line, "TrendPlot");
				
				UpColor					= Brushes.DodgerBlue;
				DnColor					= Brushes.Red;
				
				DtbStrength = 20;
				SwingSize = 7;
				SwingType = SwingStyle.Standard;
				UseCloseValues = false;
			}
			else if (State == State.Configure)
			{
				dnFlip = new Series<bool>(this);
				upFlip = new Series<bool>(this);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{				
				 // Calculate decimal places
	            decimal increment = Convert.ToDecimal(Instrument.MasterInstrument.TickSize);
	            int incrementLength = increment.ToString().Length;
	            decimalPlaces = 0;
	            if (incrementLength == 1)
	                decimalPlaces = 0;
	            else if (incrementLength > 2)
	                decimalPlaces = incrementLength - 2;
				
				SwingSize = SwingSize < 1 ? 1 : Math.Round(SwingSize, MidpointRounding.AwayFromZero);
				
				swingProperties = new SwingProperties(SwingType, SwingSize, DtbStrength,
	                swingLengthType, swingDurationType, showSwingPrice, showSwingLabel,
	                showSwingPercent, swingTimeType, swingVolumeType, visualizationType,
	                useBreakouts, ignoreInsideBars, true,
	                zigZagColorUp, zigZagColorDn, zigZagStyle, zigZagWidth, textColorHigherHigh,
	                textColorLowerHigh, textColorDoubleTop, textColorHigherLow,
	                textColorLowerLow, textColorDoubleBottom, textFont, textOffsetLength, 
	                textOffsetPercent, textOffsetPrice, textOffsetLabel, textOffsetTime, 
	                textOffsetVolume, UseCloseValues, true);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[BarsInProgress] <= 1 || CurrentBars[BarsInProgress] < SwingSize) { return;}
			if ( IsFirstTickOfBar) {   // (dnFlip[0] || upFlip[0]) &&
				Print(swingHigh.CurDateTime.ToString("HH:mm") + " \t " + swingHigh.CurVolume  +" \t " + swingLow.CurDateTime + "  \t " 
				+ swingLow.CurVolume);
				Print("flip up " + upFlip[0] + " \t" + CurrentBar);
				if (upFlip[0]) {
					if (swingHigh.CurVolume >  swingLow.CurVolume) {
						//Draw.Dot(this, "MyDot" + CurrentBar, false, 0, Low[0] - 2 * TickSize, UpColor);
						LineNowColor = UpColor;
						message = "Bullish";
					} else {
						//Draw.Dot(this, "MyDot" + CurrentBar, false, 0, Low[0] - 2 * TickSize, DnColor);
						LineNowColor = DnColor;
						message = "Bearish";
					}
				} else {
					if (swingHigh.CurVolume >  swingLow.CurVolume) {
						//Draw.Dot(this, "MyDot" + CurrentBar, false, 0, High[0] + 2 * TickSize, UpColor);
						LineNowColor = UpColor;
						message = "Bullish";
					} else {
						//Draw.Dot(this, "MyDot" + CurrentBar, false, 0, High[0] + 2 * TickSize, DnColor);
						LineNowColor = DnColor;
						message = "Bearish";
					}
				}
				Print(LineNowColor);
			}
			
			TrendPlot[0] = MIN(Low, 150)[0];
			PlotBrushes[7][0] = LineNowColor;
			Draw.TextFixed(this, "trendmessage", message, TextPosition.BottomRight);
			
        	InitializeSwingCalculation(swingHigh, swingLow, swingCurrent, upFlip, swingHighs,dnFlip, swingLows);
			CalculateSwingStandard(swingHigh, swingLow, swingCurrent, swingProperties,
                    upFlip, swingHighs, dnFlip, swingLows, decimalPlaces, UseCloseValues,
                    DoubleBottom, LowerLow, HigherLow, DoubleTop, LowerHigh, HigherHigh, 
                    GannSwing);
		}
		
		#region Initialize swing calculation
        //#########################################################################################
        public void InitializeSwingCalculation(Swings swingHigh, Swings swingLow,
            SwingCurrent swingCur, Series<bool> upFlip, List<SwingStruct> swingHighs,
            Series<bool> dnFlip, List<SwingStruct> swingLows)
        {
            if (IsFirstTickOfBar)
            {
                swingCur.StopOutsideBarCalc = false;

                // Initialize first swing
                if (swingHighs.Count == 0)
                {
                    swingHigh.CurBar = CurrentBars[BarsInProgress];
                    swingHigh.CurPrice = Highs[BarsInProgress][CurrentBars[BarsInProgress]];
                    swingHigh.CurDateTime = swingHigh.LastDateTime =
                        Times[BarsInProgress][CurrentBars[BarsInProgress]];
                    SwingStruct up = new SwingStruct(swingHigh.CurPrice, swingHigh.CurBar,
                        Times[BarsInProgress][CurrentBars[BarsInProgress] - 1], 0, 0, -1,
                        Convert.ToInt64(Volumes[BarsInProgress][0]));
                    swingHighs.Add(up);
                    swingHigh.ListCount = swingHighs.Count;
                }
                if (swingLows.Count == 0)
                {
                    swingLow.CurBar = CurrentBars[BarsInProgress];
                    swingLow.CurPrice = Lows[BarsInProgress][CurrentBars[BarsInProgress]];
                    swingLow.CurDateTime = swingLow.LastDateTime =
                        Times[BarsInProgress][CurrentBars[BarsInProgress]];
                    SwingStruct dn = new SwingStruct(swingLow.CurPrice, swingLow.CurBar,
                        Times[BarsInProgress][CurrentBars[BarsInProgress] - 1], 0, 0, -1,
                        Convert.ToInt64(Volumes[BarsInProgress][0]));
                    swingLows.Add(dn);
                    swingLow.ListCount = swingLows.Count;
                }
            }
            // Set new/update high/low back to false, to avoid function calls which depends on
            // them
            dnFlip[0] = false;
            upFlip[0] = false;
            swingHigh.New = swingLow.New = swingHigh.Update = swingLow.Update = false;
        }
        //#########################################################################################
        #endregion
		
		#region Calculate Swing Standard
		protected void CalculateSwingStandard(Swings swingHigh, Swings swingLow, 
            SwingCurrent swingCur, SwingProperties swingProp, Series<bool> upFlip,
            List<SwingStruct> swingHighs, Series<bool> dnFlip, List<SwingStruct> swingLows,
            int decimalPlaces, bool useCloseValues, Series<double> doubleBottom, Series<double> lowerLow,
            Series<double> higherLow, Series<double> doubleTop, Series<double> lowerHigh,
            Series<double> higherHigh, Series<double> gannSwing)
        {
            // Check if high and low values are used or only close values
            PriceSeries[] highs;
            PriceSeries[] lows;
            if (useCloseValues == true)
            {
                lows = Closes;
                highs = Closes;
            }
            else
            {
                lows = Lows;
                highs = Highs;
            }

            // For a new swing high in an uptrend, Highs[BarsInProgress][0] must be 
            // greater than the current swing high
            if (swingCur.SwingSlope == 1 && highs[BarsInProgress][0] <= swingHigh.CurPrice)
                swingHigh.New = false;
            else
                swingHigh.New = true;

            // For a new swing low in a downtrend, Lows[BarsInProgress][0] must be 
            // smaller than the current swing low
            if (swingCur.SwingSlope == -1 && lows[BarsInProgress][0] >= swingLow.CurPrice)
                swingLow.New = false;
            else
                swingLow.New = true;

            // CalculatOnBarClose == true
            if (Calculate == Calculate.OnBarClose)
            {
                // test if Highs[BarsInProgress][0] is higher than the last 
                // calculationSize highs = new swing high
                if (swingHigh.New)
                {
                    for (int i = 1; i < swingProp.SwingSize + 1; i++)
                    {
                        if (highs[BarsInProgress][0] <= highs[BarsInProgress][i])
                        {
                            swingHigh.New = false;
                            break;
                        }
                    }
                }
                // test if Lows[BarsInProgress][0] is lower than the last 
                // calculationSize lows = new swing low
                if (swingLow.New)
                {
                    for (int i = 1; i < swingProp.SwingSize + 1; i++)
                    {
                        if (lows[BarsInProgress][0] >= lows[BarsInProgress][i])
                        {
                            swingLow.New = false;
                            break;
                        }
                    }
                }

                // New swing high and new swing low
                if (swingHigh.New && swingLow.New)
                {
                    // Downtrend - ignore the swing high
                    if (swingCur.SwingSlope == -1)
                        swingHigh.New = false;
                    // Uptrend   - ignore the swing low
                    else
                        swingLow.New = false;
                }
            }
            // CalculatOnBarClose == false
            else
            {
                // Used to control, that only one swing is set for 
                // each bar
                if (IsFirstTickOfBar)
                    swingCur.NewSwing = 0;

                // No swing or an up swing is found
                if (swingCur.NewSwing != -1)
                {
                    // test if Highs[BarsInProgress][0] is higher than the last 
                    // calculationSize highs = new swing high
                    if (swingHigh.New)
                    {
                        for (int i = 1; i < swingProp.SwingSize + 1; i++)
                        {
                            if (highs[BarsInProgress][0] <= highs[BarsInProgress][i])
                            {
                                swingHigh.New = false;
                                break;
                            }
                        }
                        // Found a swing high
                        if (swingHigh.New)
                            swingCur.NewSwing = 1;
                    }
                }

                // No swing or an down swing is found
                if (swingCur.NewSwing != 1)
                {
                    // test if Lows[BarsInProgress][0] is lower than the last 
                    // calculationSize lows = new swing low
                    if (swingLow.New)
                    {
                        for (int i = 1; i < swingProp.SwingSize + 1; i++)
                        {
                            if (lows[BarsInProgress][0] >= lows[BarsInProgress][i])
                            {
                                swingLow.New = false;
                                break;
                            }
                        }
                        // Found a swing low
                        if (swingLow.New)
                            swingCur.NewSwing = -1;
                    }
                }

                // Set newLow back to false
                if (swingCur.NewSwing == 1)
                    swingLow.New = false;
                // Set newHigh back to false
                if (swingCur.NewSwing == -1)
                    swingHigh.New = false;
            }

            // Swing high
            if (swingHigh.New)
            {
                int bar;
                double price;
                // New swing high
                if (swingCur.SwingSlope != 1)
                {
                    bar = CurrentBars[BarsInProgress] -
                        HighestBar(highs[BarsInProgress], CurrentBars[BarsInProgress] -
                        swingLow.CurBar);
                    price = highs[BarsInProgress][HighestBar(highs[BarsInProgress],
                        CurrentBars[BarsInProgress] - swingLow.CurBar)];
                    swingHigh.Update = false;
                }
                // Update swing high
                else
                {
                    bar = CurrentBars[BarsInProgress];
                    price = highs[BarsInProgress][0];
                    swingHigh.Update = true;
                }
                CalcUpSwing(bar, price, swingHigh.Update, swingHigh, swingLow, swingCur,
                    swingProp, upFlip, swingHighs, decimalPlaces, doubleBottom, lowerLow, 
                    higherLow, doubleTop, lowerHigh, higherHigh, gannSwing);
            }
            // Swing low
            else if (swingLow.New)
            {
                int bar;
                double price;
                // New swing low
                if (swingCur.SwingSlope != -1)
                {
                    bar = CurrentBars[BarsInProgress] - LowestBar(lows[BarsInProgress],
                        CurrentBars[BarsInProgress] - swingHigh.CurBar);
                    price = lows[BarsInProgress][LowestBar(lows[BarsInProgress],
                        CurrentBars[BarsInProgress] - swingHigh.CurBar)];
                    swingLow.Update = false;
                }
                // Update swing low
                else
                {
                    bar = CurrentBars[BarsInProgress];
                    price = lows[BarsInProgress][0];
                    swingLow.Update = true;
                }
                CalcDnSwing(bar, price, swingLow.Update, swingHigh, swingLow, swingCur,
                    swingProp, dnFlip, swingLows, decimalPlaces, doubleBottom, lowerLow, 
                    higherLow, doubleTop, lowerHigh, higherHigh, gannSwing);
            }            
        }
		#endregion
		
		#region Calculate down swing
        //#########################################################################################
        protected void CalcDnSwing(int bar, double low, bool updateLow, Swings swingHigh,
            Swings swingLow, SwingCurrent swingCur, SwingProperties swingProp, Series<bool> dnFlip,
            List<SwingStruct> swingLows, int decimalPlaces, Series<double> doubleBottom,
            Series<double> lowerLow, Series<double> higherLow, Series<double> doubleTop, Series<double> lowerHigh,
            Series<double> higherHigh, Series<double> gannSwing)
        {
            #region New and update Swing values
            //=====================================================================================
            if (!updateLow)
            {
				
                if (swingProp.VisualizationType == VisualizationStyle.GannStyle)
                {
                    for (int i = CurrentBar - swingCur.SwingSlopeChangeBar; i >= 0; i--)
                        gannSwing[i] = swingHigh.CurPrice;
                    gannSwing[0] = low;
                }
                swingLow.LastPrice = swingLow.CurPrice;
                swingLow.LastBar = swingLow.CurBar;
                swingLow.LastDateTime = swingLow.CurDateTime;
                swingLow.LastDuration = swingLow.CurDuration;
                swingLow.LastLength = swingLow.CurLength;
                swingLow.LastTime = swingLow.CurTime;
                swingLow.LastPercent = swingLow.CurPercent;
                swingLow.LastRelation = swingLow.CurRelation;
                swingLow.LastVolume = swingLow.CurVolume;
                swingLow.Counter++;
                swingCur.SwingSlope = -1;
                swingCur.SwingSlopeChangeBar = bar;
                dnFlip[0] = true; 
				
            }
            else
            {
                if (swingProp.VisualizationType == VisualizationStyle.Dots
                    || swingProp.VisualizationType == VisualizationStyle.Dots_ZigZag)
                {
                    doubleBottom.Reset(CurrentBar - swingLow.CurBar);
                    higherLow.Reset(CurrentBar - swingLow.CurBar);
                    lowerLow.Reset(CurrentBar - swingLow.CurBar);
                }
                swingLows.RemoveAt(swingLows.Count - 1);
            }
            swingLow.CurBar = bar;
            swingLow.CurPrice = Math.Round(low, decimalPlaces, MidpointRounding.AwayFromZero);
            swingLow.CurTime = ToTime(Times[BarsInProgress][CurrentBars[BarsInProgress] -
                swingLow.CurBar]);
            swingLow.CurDateTime = Times[BarsInProgress][CurrentBars[BarsInProgress] -
                swingLow.CurBar];
            swingLow.CurLength = Convert.ToInt32(Math.Round((swingLow.CurPrice -
                swingHigh.CurPrice) / TickSize, 0, MidpointRounding.AwayFromZero));
            if (swingHigh.CurLength != 0)
                swingLow.CurPercent = Math.Round(100.0 / swingHigh.CurLength *
                    Math.Abs(swingLow.CurLength), 1);
            swingLow.CurDuration = swingLow.CurBar - swingHigh.CurBar;
            double dtbOffset = ATR(BarsArray[BarsInProgress], 14)[CurrentBars[BarsInProgress] -
                swingLow.CurBar] * swingProp.DtbStrength / 100;
            if (swingLow.CurPrice > swingLow.LastPrice - dtbOffset && swingLow.CurPrice <
                swingLow.LastPrice + dtbOffset)
                swingLow.CurRelation = 0;
            else if (swingLow.CurPrice < swingLow.LastPrice)
                swingLow.CurRelation = -1;
            else
                swingLow.CurRelation = 1;
            if (Calculate != Calculate.OnBarClose)
                swingHigh.SignalBarVolume = Volumes[BarsInProgress][0];
            double swingVolume = 0.0;
            for (int i = 0; i < swingLow.CurDuration; i++)
                swingVolume = swingVolume + Volumes[BarsInProgress][i];
            if (Calculate != Calculate.OnBarClose)
                swingVolume = swingVolume + (Volumes[BarsInProgress][CurrentBars[BarsInProgress] -
                    swingHigh.CurBar] - swingLow.SignalBarVolume);
            if (swingProp.SwingVolumeType == SwingVolumeStyle.Relative)
                swingVolume = Math.Round(swingVolume / swingLow.CurDuration, 0,
                    MidpointRounding.AwayFromZero);
            swingLow.CurVolume = Convert.ToInt64(swingVolume); 			
            //=====================================================================================
            #endregion

            #region Visualize swing
            //=====================================================================================
            switch (swingProp.VisualizationType)
            {
                case VisualizationStyle.False:
                    break;
                case VisualizationStyle.Dots:
                    switch (swingLow.CurRelation)
                    {
                        case 1:
                            higherLow[CurrentBar - swingLow.CurBar] = swingLow.CurPrice;
                            break;
                        case -1:
                            lowerLow[CurrentBar - swingLow.CurBar] = swingLow.CurPrice;
                            break;
                        case 0:
                            doubleBottom[CurrentBar - swingLow.CurBar] = swingLow.CurPrice;
                            break;
                    }
                    break;
                case VisualizationStyle.Dots_ZigZag:
                    switch (swingLow.CurRelation)
                    {
                        case 1:
                            higherLow[CurrentBar - swingLow.CurBar] = swingLow.CurPrice;
                            break;
                        case -1:
                            lowerLow[CurrentBar - swingLow.CurBar] = swingLow.CurPrice;
                            break;
                        case 0:
                            doubleBottom[CurrentBar - swingLow.CurBar] = swingLow.CurPrice;
                            break;
                    }
                    Draw.Line(this, "ZigZagDown" + swingLow.Counter,
                        swingProp.UseAutoScale, CurrentBar - swingHigh.CurBar, swingHigh.CurPrice,
                        CurrentBar - swingLow.CurBar, swingLow.CurPrice, swingProp.ZigZagColorDn,
                        swingProp.ZigZagStyle, swingProp.ZigZagWidth, swingProp.DrawSwingsOnPricePanel);
                    break;
                case VisualizationStyle.ZigZag:
                    Draw.Line(this, "ZigZagDown" + swingLow.Counter,
                        swingProp.UseAutoScale, CurrentBar - swingHigh.CurBar, swingHigh.CurPrice,
                        CurrentBar - swingLow.CurBar, swingLow.CurPrice, swingProp.ZigZagColorDn,
                        swingProp.ZigZagStyle, swingProp.ZigZagWidth, swingProp.DrawSwingsOnPricePanel);
                    break;
                case VisualizationStyle.GannStyle:
                    for (int i = CurrentBar - swingCur.SwingSlopeChangeBar; i >= 0; i--)
                        gannSwing[i] = swingLow.CurPrice;
                    break;
                case VisualizationStyle.ZigZagVolume:
                    if (swingLow.CurVolume > swingHigh.CurVolume)
                        Draw.Line(this, "ZigZagDown" + swingLow.Counter,
                            swingProp.UseAutoScale, CurrentBar - swingHigh.CurBar, 
                            swingHigh.CurPrice, CurrentBar - swingLow.CurBar, swingLow.CurPrice, 
                            swingProp.ZigZagColorDn, swingProp.ZigZagStyle, swingProp.ZigZagWidth,
							swingProp.DrawSwingsOnPricePanel);
                    else
                        Draw.Line(this, "ZigZagDown" + swingLow.Counter,
                            swingProp.UseAutoScale, CurrentBar - swingHigh.CurBar,
                            swingHigh.CurPrice, CurrentBar - swingLow.CurBar, swingLow.CurPrice,
                            swingProp.ZigZagColorUp, swingProp.ZigZagStyle, swingProp.ZigZagWidth, 
							swingProp.DrawSwingsOnPricePanel);
                    break;
            }
            //=====================================================================================
            #endregion

            #region Swing value output
            //=====================================================================================
            string output = "";
            switch (swingProp.SwingLengthType)
            {
                case SwingLengthStyle.False:
                    break;
                case SwingLengthStyle.Ticks:
                    output = swingLow.CurLength.ToString();
                    break;
                case SwingLengthStyle.Ticks_Price:
                    output = swingLow.CurLength.ToString() + " / " + swingLow.CurPrice.ToString();
                    break;
                case SwingLengthStyle.Price_Ticks:
                    output = swingLow.CurPrice.ToString() + " / " + swingLow.CurLength.ToString();
                    break;
                case SwingLengthStyle.Points:
                    output = (swingLow.CurLength * TickSize).ToString();
                    break;
                case SwingLengthStyle.Points_Price:
                    output = (swingLow.CurLength * TickSize).ToString() + " / " +
                        swingLow.CurPrice.ToString();
                    break;
                case SwingLengthStyle.Price_Points:
                    output = swingLow.CurPrice.ToString() + " / " +
                        (swingLow.CurLength * TickSize).ToString();
                    break;
                case SwingLengthStyle.Price:
                    output = swingLow.CurPrice.ToString();
                    break;
                case SwingLengthStyle.Percent:
                    output = (Math.Round((100.0 / swingHigh.CurPrice * (swingLow.CurLength *
                        TickSize)), 2, MidpointRounding.AwayFromZero)).ToString();
                    break;
            }
            string outputDuration = "";
            TimeSpan timeSpan;
            int hours, minutes, seconds = 0;
            switch (swingProp.SwingDurationType)
            {
                case SwingDurationStyle.False:
                    break;
                case SwingDurationStyle.Bars:
                    outputDuration = swingLow.CurDuration.ToString();
                    break;
                case SwingDurationStyle.MMSS:
                    timeSpan = swingLow.CurDateTime.Subtract(swingHigh.CurDateTime);
                    minutes = timeSpan.Minutes;
                    seconds = timeSpan.Seconds;
                    if (minutes == 0)
                        outputDuration = "0:" + seconds.ToString();
                    else if (seconds == 0)
                        outputDuration = minutes + ":00";
                    else
                        outputDuration = minutes + ":" + seconds;
                    break;
                case SwingDurationStyle.HHMM:
                    timeSpan = swingLow.CurDateTime.Subtract(swingHigh.CurDateTime);
                    hours = timeSpan.Hours;
                    minutes = timeSpan.Minutes;
                    if (hours == 0)
                        outputDuration = "0:" + minutes.ToString();
                    else if (minutes == 0)
                        outputDuration = hours + ":00";
                    else
                        outputDuration = hours + ":" + minutes;
                    break;
                case SwingDurationStyle.SecondsTotal:
                    timeSpan = swingLow.CurDateTime.Subtract(swingHigh.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalSeconds, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
                case SwingDurationStyle.MinutesTotal:
                    timeSpan = swingLow.CurDateTime.Subtract(swingHigh.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalMinutes, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
                case SwingDurationStyle.HoursTotal:
                    timeSpan = swingLow.CurDateTime.Subtract(swingHigh.CurDateTime);
                    outputDuration = timeSpan.TotalHours.ToString();
                    break;
                case SwingDurationStyle.Days:
                    timeSpan = swingLow.CurDateTime.Subtract(swingHigh.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalDays, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
            }
            if (swingProp.SwingLengthType != SwingLengthStyle.False)
            {
                if (swingProp.SwingDurationType != SwingDurationStyle.False)
                    output = output + " / " + outputDuration;
            }
            else
                output = outputDuration;

            string swingLabel = null;
            Brush textColor = Brushes.Transparent;
            switch (swingLow.CurRelation)
            {
                case 1:
                    swingLabel = "HL";
                    textColor = swingProp.TextColorHigherLow;
                    break;
                case -1:
                    swingLabel = "LL";
                    textColor = swingProp.TextColorLowerLow;
                    break;
                case 0:
                    swingLabel = "DB";
                    textColor = swingProp.TextColorDoubleBottom;
                    break;
            }
            if (output != null)
                Draw.Text(this, "DnLength" + swingLow.Counter, swingProp.UseAutoScale,
                    output.ToString(), CurrentBar - swingLow.CurBar, swingLow.CurPrice,
                    -swingProp.TextOffsetLength, textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            if (swingProp.ShowSwingLabel)
                Draw.Text(this, "DnLabel" + swingLow.Counter, swingProp.UseAutoScale,
                    swingLabel, CurrentBar - swingLow.CurBar, swingLow.CurPrice,
                    -swingProp.TextOffsetLabel, textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            if (swingProp.ShowSwingPrice)
                Draw.Text(this, "DnPrice" + swingLow.Counter, swingProp.UseAutoScale,
                    String.Format("{0:F" + decimalPlaces + "}", swingLow.CurPrice),
                    CurrentBar - swingLow.CurBar, swingLow.CurPrice, -swingProp.TextOffsetPrice,
                    textColor, swingProp.TextFont, TextAlignment.Center, Brushes.Transparent,
                    Brushes.Transparent, 0);
            if (swingProp.ShowSwingPercent && swingLow.CurPercent != 0)
                Draw.Text(this, "DnPerc" + swingLow.Counter, swingProp.UseAutoScale,
                    String.Format("{0:F1}", swingLow.CurPercent) + "%", CurrentBar - swingLow.CurBar,
                    swingLow.CurPrice, -swingProp.TextOffsetPercent, textColor,
                    swingProp.TextFont, TextAlignment.Center, Brushes.Transparent,
                    Brushes.Transparent, 0);
            if (swingProp.SwingTimeType != SwingTimeStyle.False)
            {
                string timeOutput = "";
                switch (swingProp.SwingTimeType)
                {
                    case SwingTimeStyle.False:
                        break;
                    case SwingTimeStyle.Integer:
                        timeOutput = swingLow.CurTime.ToString();
                        break;
                    case SwingTimeStyle.HHMM:
                        timeOutput = string.Format("{0:t}",
                            Times[BarsInProgress][CurrentBars[BarsInProgress] - swingLow.CurBar]);
                        break;
                    case SwingTimeStyle.HHMMSS:
                        timeOutput = string.Format("{0:T}",
                            Times[BarsInProgress][CurrentBars[BarsInProgress] - swingLow.CurBar]);
                        break;
                    case SwingTimeStyle.DDMM:
                        timeOutput = string.Format("{0:dd.MM}",
                            Times[BarsInProgress][CurrentBars[BarsInProgress] - swingHigh.CurBar]);
                        break;
                }
                Draw.Text(this, "DnTime" + swingLow.Counter, swingProp.UseAutoScale,
                    timeOutput, CurrentBar - swingLow.CurBar, swingLow.CurPrice, -swingProp.TextOffsetTime,
                    textColor, swingProp.TextFont, TextAlignment.Center, Brushes.Transparent,
                    Brushes.Transparent, 0);
            }
            if (swingProp.SwingVolumeType != SwingVolumeStyle.False)
                Draw.Text(this, "DnVolume" + swingLow.Counter, swingProp.UseAutoScale,
                    TruncIntToStr(swingLow.CurVolume), CurrentBar - swingLow.CurBar,
                    swingLow.CurPrice, -swingProp.TextOffsetVolume, textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            //=====================================================================================
            #endregion

            SwingStruct dn = new SwingStruct(swingLow.CurPrice, swingLow.CurBar,
                swingLow.CurDateTime, swingLow.CurDuration, swingLow.CurLength,
                swingLow.CurRelation, swingLow.CurVolume);
            swingLows.Add(dn);
            swingLow.ListCount = swingLows.Count - 1;
			
			
			
        }
        //#########################################################################################
        #endregion

        #region Calculate up swing
        //#########################################################################################
        private void CalcUpSwing(int bar, double high, bool updateHigh, Swings swingHigh,
            Swings swingLow, SwingCurrent swingCur, SwingProperties swingProp, Series<bool> upFlip,
            List<SwingStruct> swingHighs, int decimalPlaces, Series<double> doubleBottom, 
            Series<double> lowerLow, Series<double> higherLow, Series<double> doubleTop, Series<double> lowerHigh, 
            Series<double> higherHigh, Series<double> gannSwing)
        {
            #region New and update swing values
            //=====================================================================================
            if (!updateHigh)
            {
				
                if (swingProp.VisualizationType == VisualizationStyle.GannStyle)
                {
                    for (int i = CurrentBar - swingCur.SwingSlopeChangeBar; i >= 0; i--)
                        gannSwing[i] = swingLow.CurPrice;
                    gannSwing[0] = high;
                }
                swingHigh.LastPrice = swingHigh.CurPrice;
                swingHigh.LastBar = swingHigh.CurBar;
                swingHigh.LastDateTime = swingHigh.CurDateTime;
                swingHigh.LastDuration = swingHigh.CurDuration;
                swingHigh.LastLength = swingHigh.CurLength;
                swingHigh.LastTime = swingHigh.CurTime;
                swingHigh.LastPercent = swingHigh.CurPercent;
                swingHigh.LastRelation = swingHigh.CurRelation;
                swingHigh.LastVolume = swingHigh.CurVolume;
                swingHigh.Counter++;
                swingCur.SwingSlope = 1;
                swingCur.SwingSlopeChangeBar = bar;
                upFlip[0] = true;
//Print("updated High: \t" + swingHigh.CurDateTime + "\t" + swingHigh.CurVolume);
				
            }
            else
            {
                if (swingProp.VisualizationType == VisualizationStyle.Dots
                    || swingProp.VisualizationType == VisualizationStyle.Dots_ZigZag)
                {
                    doubleTop.Reset(CurrentBar - swingHigh.CurBar);
                    higherHigh.Reset(CurrentBar - swingHigh.CurBar);
                    lowerHigh.Reset(CurrentBar - swingHigh.CurBar);
                }
                swingHighs.RemoveAt(swingHighs.Count - 1);
            }
            swingHigh.CurBar = bar;
            swingHigh.CurPrice = Math.Round(high, decimalPlaces, MidpointRounding.AwayFromZero);
            swingHigh.CurTime = ToTime(Times[BarsInProgress][CurrentBars[BarsInProgress] -
                swingHigh.CurBar]);
            swingHigh.CurDateTime = Times[BarsInProgress][CurrentBars[BarsInProgress] -
                swingHigh.CurBar];
            swingHigh.CurLength = Convert.ToInt32(Math.Round((swingHigh.CurPrice -
                swingLow.CurPrice) / TickSize, 0, MidpointRounding.AwayFromZero));
            if (swingLow.CurLength != 0)
                swingHigh.CurPercent = Math.Round(100.0 / Math.Abs(swingLow.CurLength) *
                    swingHigh.CurLength, 1);
            swingHigh.CurDuration = swingHigh.CurBar - swingLow.CurBar;
            double dtbOffset = ATR(BarsArray[BarsInProgress], 14)[CurrentBars[BarsInProgress] -
                swingHigh.CurBar] * swingProp.DtbStrength / 100;
            if (swingHigh.CurPrice > swingHigh.LastPrice - dtbOffset && swingHigh.CurPrice <
                swingHigh.LastPrice + dtbOffset)
                swingHigh.CurRelation = 0;
            else if (swingHigh.CurPrice < swingHigh.LastPrice)
                swingHigh.CurRelation = -1;
            else
                swingHigh.CurRelation = 1;
            if (Calculate != Calculate.OnBarClose)
                swingLow.SignalBarVolume = Volumes[BarsInProgress][0];
            double swingVolume = 0.0;
            for (int i = 0; i < swingHigh.CurDuration; i++)
                swingVolume = swingVolume + Volumes[BarsInProgress][i];
            if (Calculate != Calculate.OnBarClose)
                swingVolume = swingVolume + (Volumes[BarsInProgress][CurrentBars[BarsInProgress] -
                    swingLow.CurBar] - swingHigh.SignalBarVolume);
            if (swingProp.SwingVolumeType == SwingVolumeStyle.Relative)
                swingVolume = Math.Round(swingVolume / swingHigh.CurDuration, 0,
                    MidpointRounding.AwayFromZero);
            swingHigh.CurVolume = Convert.ToInt64(swingVolume);
            //=====================================================================================
            #endregion

            #region Visualize swing
            //=====================================================================================
            switch (swingProp.VisualizationType)
            {
                case VisualizationStyle.False:
                    break;
                case VisualizationStyle.Dots:
                    switch (swingHigh.CurRelation)
                    {
                        case 1:
                            higherHigh[CurrentBar - swingHigh.CurBar] = swingHigh.CurPrice;
                            break;
                        case -1:
                            lowerHigh[CurrentBar - swingHigh.CurBar] = swingHigh.CurPrice;
                            break;
                        case 0:
                            doubleTop[CurrentBar - swingHigh.CurBar] = swingHigh.CurPrice;
                            break;
                    }
                    break;
                case VisualizationStyle.Dots_ZigZag:
                    switch (swingHigh.CurRelation)
                    {
                        case 1:
                            higherHigh[CurrentBar - swingHigh.CurBar] = swingHigh.CurPrice;
                            break;
                        case -1:
                            lowerHigh[CurrentBar - swingHigh.CurBar] = swingHigh.CurPrice;
                            break;
                        case 0:
                            doubleTop[CurrentBar - swingHigh.CurBar] = swingHigh.CurPrice;
                            break;
                    }
                    Draw.Line(this, "ZigZagUp" + swingHigh.Counter,
                        swingProp.UseAutoScale, CurrentBar - swingLow.CurBar, swingLow.CurPrice,
                        CurrentBar - swingHigh.CurBar, swingHigh.CurPrice, swingProp.ZigZagColorUp,
                        swingProp.ZigZagStyle, swingProp.ZigZagWidth);
                    break;
                case VisualizationStyle.ZigZag:
                    Draw.Line(this, "ZigZagUp" + swingHigh.Counter,
                        swingProp.UseAutoScale, CurrentBar - swingLow.CurBar, swingLow.CurPrice,
                        CurrentBar - swingHigh.CurBar, swingHigh.CurPrice, swingProp.ZigZagColorUp,
                        swingProp.ZigZagStyle, swingProp.ZigZagWidth);
                    break;
                case VisualizationStyle.GannStyle:
                    for (int i = CurrentBar - swingCur.SwingSlopeChangeBar; i >= 0; i--)
                        gannSwing[i] = swingHigh.CurPrice;
                    break;
                case VisualizationStyle.ZigZagVolume:
                    if (swingHigh.CurVolume > swingLow.CurVolume)
                        Draw.Line(this, "ZigZagUp" + swingHigh.Counter,
                            swingProp.UseAutoScale, CurrentBar - swingLow.CurBar, 
                            swingLow.CurPrice, CurrentBar - swingHigh.CurBar, swingHigh.CurPrice, 
                            swingProp.ZigZagColorUp, swingProp.ZigZagStyle, swingProp.ZigZagWidth);
                    else
                        Draw.Line(this, "ZigZagUp" + swingHigh.Counter,
                            swingProp.UseAutoScale, CurrentBar - swingLow.CurBar,
                            swingLow.CurPrice, CurrentBar - swingHigh.CurBar, swingHigh.CurPrice,
                            swingProp.ZigZagColorDn, swingProp.ZigZagStyle, swingProp.ZigZagWidth);
                    break;
            }
            //=====================================================================================
            #endregion

            #region Swing value output
            //=====================================================================================
            string output = "";
            switch (swingProp.SwingLengthType)
            {
                case SwingLengthStyle.False:
                    break;
                case SwingLengthStyle.Ticks:
                    output = swingHigh.CurLength.ToString();
                    break;
                case SwingLengthStyle.Ticks_Price:
                    output = swingHigh.CurLength.ToString() + " / " +
                        swingHigh.CurPrice.ToString();
                    break;
                case SwingLengthStyle.Price_Ticks:
                    output = swingHigh.CurPrice.ToString() + " / " +
                        swingHigh.CurLength.ToString();
                    break;
                case SwingLengthStyle.Points:
                    output = (swingHigh.CurLength * TickSize).ToString();
                    break;
                case SwingLengthStyle.Points_Price:
                    output = (swingHigh.CurLength * TickSize).ToString() + " / " +
                        swingHigh.CurPrice.ToString();
                    break;
                case SwingLengthStyle.Price_Points:
                    output = swingHigh.CurPrice.ToString() + " / " +
                        (swingHigh.CurLength * TickSize).ToString();
                    break;
                case SwingLengthStyle.Price:
                    output = swingHigh.CurPrice.ToString();
                    break;
                case SwingLengthStyle.Percent:
                    output = (Math.Round((100.0 / swingLow.CurPrice * (swingHigh.CurLength *
                        TickSize)), 2, MidpointRounding.AwayFromZero)).ToString();
                    break;
            }
            string outputDuration = "";
            TimeSpan timeSpan;
            int hours, minutes, seconds = 0;
            switch (swingProp.SwingDurationType)
            {
                case SwingDurationStyle.False:
                    break;
                case SwingDurationStyle.Bars:
                    outputDuration = swingHigh.CurDuration.ToString();
                    break;
                case SwingDurationStyle.MMSS:
                    timeSpan = swingHigh.CurDateTime.Subtract(swingLow.CurDateTime);
                    minutes = timeSpan.Minutes;
                    seconds = timeSpan.Seconds;
                    if (minutes == 0)
                        outputDuration = "0:" + seconds.ToString();
                    else if (seconds == 0)
                        outputDuration = minutes + ":00";
                    else
                        outputDuration = minutes + ":" + seconds;
                    break;
                case SwingDurationStyle.HHMM:
                    timeSpan = swingHigh.CurDateTime.Subtract(swingLow.CurDateTime);
                    hours = timeSpan.Hours;
                    minutes = timeSpan.Minutes;
                    if (hours == 0)
                        outputDuration = "0:" + minutes.ToString();
                    else if (minutes == 0)
                        outputDuration = hours + ":00";
                    else
                        outputDuration = hours + ":" + minutes;
                    break;
                case SwingDurationStyle.SecondsTotal:
                    timeSpan = swingHigh.CurDateTime.Subtract(swingLow.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalSeconds, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
                case SwingDurationStyle.MinutesTotal:
                    timeSpan = swingHigh.CurDateTime.Subtract(swingLow.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalMinutes, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
                case SwingDurationStyle.HoursTotal:
                    timeSpan = swingHigh.CurDateTime.Subtract(swingLow.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalHours, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
                case SwingDurationStyle.Days:
                    timeSpan = swingHigh.CurDateTime.Subtract(swingLow.CurDateTime);
                    outputDuration = Math.Round(timeSpan.TotalDays, 1,
                        MidpointRounding.AwayFromZero).ToString();
                    break;
            }
            if (swingProp.SwingLengthType != SwingLengthStyle.False)
            {
                if (swingProp.SwingDurationType != SwingDurationStyle.False)
                    output = output + " / " + outputDuration;
            }
            else
                output = outputDuration;

            string swingLabel = null;
            Brush textColor = Brushes.Transparent;
            switch (swingHigh.CurRelation)
            {
                case 1:
                    swingLabel = "HH";
                    textColor = swingProp.TextColorHigherHigh;
                    break;
                case -1:
                    swingLabel = "LH";
                    textColor = swingProp.TextColorLowerHigh;
                    break;
                case 0:
                    swingLabel = "DT";
                    textColor = swingProp.TextColorDoubleTop;
                    break;
            }
            if (output != null)
                Draw.Text(this, "UpLength" + swingHigh.Counter,
                    swingProp.UseAutoScale, output.ToString(), CurrentBar - swingHigh.CurBar,
                    swingHigh.CurPrice, swingProp.TextOffsetLength + Convert.ToInt32(textFont.Size), textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            if (swingProp.ShowSwingLabel)
                Draw.Text(this, "UpLabel" + swingHigh.Counter, swingProp.UseAutoScale,
                    swingLabel, CurrentBar - swingHigh.CurBar, swingHigh.CurPrice,
                    swingProp.TextOffsetLabel + Convert.ToInt32(textFont.Size), textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            if (swingProp.ShowSwingPrice)
                Draw.Text(this, "UpPrice" + swingHigh.Counter, swingProp.UseAutoScale,
                    String.Format("{0:F" + decimalPlaces + "}", swingHigh.CurPrice),
                    CurrentBar - swingHigh.CurBar, swingHigh.CurPrice, swingProp.TextOffsetPrice + Convert.ToInt32(textFont.Size),
                    textColor, swingProp.TextFont, TextAlignment.Center, Brushes.Transparent,
                    Brushes.Transparent, 0);
            if (swingProp.ShowSwingPercent && swingHigh.CurPercent != 0)
                Draw.Text(this, "UpPerc" + swingHigh.Counter, swingProp.UseAutoScale,
                    String.Format("{0:F1}", swingHigh.CurPercent) + "%", CurrentBar - swingHigh.CurBar,
                    swingHigh.CurPrice, swingProp.TextOffsetPercent + Convert.ToInt32(textFont.Size), textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            if (swingProp.SwingTimeType != SwingTimeStyle.False)
            {
                string timeOutput = "";
                switch (swingProp.SwingTimeType)
                {
                    case SwingTimeStyle.False:
                        break;
                    case SwingTimeStyle.Integer:
                        timeOutput = swingHigh.CurTime.ToString();
                        break;
                    case SwingTimeStyle.HHMM:
                        timeOutput = string.Format("{0:t}",
                            Times[BarsInProgress][CurrentBars[BarsInProgress] - swingHigh.CurBar]);
                        break;
                    case SwingTimeStyle.HHMMSS:
                        timeOutput = string.Format("{0:T}",
                            Times[BarsInProgress][CurrentBars[BarsInProgress] - swingHigh.CurBar]);
                        break;
                    case SwingTimeStyle.DDMM:
                        timeOutput = string.Format("{0:dd.MM}",
                            Times[BarsInProgress][CurrentBars[BarsInProgress] - swingHigh.CurBar]);
                        break;
                }
                Draw.Text(this, "UpTime" + swingHigh.Counter, swingProp.UseAutoScale,
                    timeOutput, CurrentBar - swingHigh.CurBar, swingHigh.CurPrice,
                    swingProp.TextOffsetTime+ Convert.ToInt32(textFont.Size), textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            }
            if (swingProp.SwingVolumeType != SwingVolumeStyle.False)
                Draw.Text(this, "UpVolume" + swingHigh.Counter, swingProp.UseAutoScale,
                    TruncIntToStr(swingHigh.CurVolume), CurrentBar - swingHigh.CurBar,
                    swingHigh.CurPrice, swingProp.TextOffsetVolume + Convert.ToInt32(textFont.Size), textColor, swingProp.TextFont,
                    TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            //=========================================================================================
            #endregion

            SwingStruct up = new SwingStruct(swingHigh.CurPrice, swingHigh.CurBar,
                swingHigh.CurDateTime, swingHigh.CurDuration, swingHigh.CurLength,
                swingHigh.CurRelation, swingHigh.CurVolume);
            swingHighs.Add(up);
            swingHigh.ListCount = swingHighs.Count - 1;
        }
        //#########################################################################################
        #endregion
		
		#region Trunc integer to string
        //#########################################################################################
        /// <summary>
        /// Converts long integer numbers in a number-string format.
        /// </summary>
        protected string TruncIntToStr(long number)
        {
            long numberAbs = Math.Abs(number);
            string output = "";
            double convertedNumber = 0.0;
            if (numberAbs > 1000000000)
            {
                convertedNumber = Math.Round(number / 1000000000.0, 1,
                    MidpointRounding.AwayFromZero);
                output = convertedNumber.ToString() + "B";
            }
            else if (numberAbs > 1000000)
            {
                convertedNumber = Math.Round(number / 1000000.0, 1,
                    MidpointRounding.AwayFromZero);
                output = convertedNumber.ToString() + "M";
            }
            else if (numberAbs > 1000)
            {
                convertedNumber = Math.Round(number / 1000.0, 1,
                    MidpointRounding.AwayFromZero);
                output = convertedNumber.ToString() + "K";
            }
            else
                output = number.ToString();

            return output;
        }
        //#########################################################################################
        #endregion
		
		#region Properties
		// Plots ==================================================================================
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> DoubleBottom
        {
            get { return Values[0]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> LowerLow
        {
            get { return Values[1]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> HigherLow
        {
            get { return Values[2]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> DoubleTop
        {
            get { return Values[3]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> LowerHigh
        {
            get { return Values[4]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> HigherHigh
        {
            get { return Values[5]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> GannSwing
        {
            get { return Values[6]; }
        }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> TrendPlot
		{
			get { return Values[7]; }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Up Color", Description="Chop zone color.", Order=1, GroupName="Visualize Trend")]
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
		[Display(Name="Dn Color", Description="Chop zone color.", Order=2, GroupName="Visualize Trend")]
		public Brush DnColor
		{ get; set; }
		
		[Browsable(false)]
		public string DnColorSerializable
		{
			get { return Serialize.BrushToString(DnColor); }
			set { DnColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Text Font", Description = "Represents the text font for the swing value output.", Order = 1, GroupName = "Parameters")]
		public NinjaTrader.Gui.Tools.SimpleFont TextFont
		{
			get { return textFont; }
			set { textFont = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Swing type", Description = "Represents the swing type for the swings.", Order = 1, GroupName = "Parameters")]
		public SwingStyle SwingType
		{ get; set; }
		
		[Range(0.00000001, double.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "Swing size", Description = "Represents the swing size. e.g. 1 = small swings and 5 = bigger swings.", Order = 2, GroupName = "Parameters")]
		public double SwingSize
		{ get; set; }
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "Double top/-bottom strength", Description = "Represents the double top/-bottom strength. Increase the value to get more DB/DT.", Order = 3, GroupName = "Parameters")]
		public int DtbStrength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use close values", Description = "Indicates if high and low prices are used for the swing calculations or close values.", Order = 4, GroupName = "Parameters")]
		public bool UseCloseValues
		{ get; set; }
		
		#endregion
	}
	
	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceActionTest[] cachePriceActionTest;
		public PriceActionTest PriceActionTest(Brush upColor, Brush dnColor, NinjaTrader.Gui.Tools.SimpleFont textFont, SwingStyle swingType, double swingSize, int dtbStrength, bool useCloseValues)
		{
			return PriceActionTest(Input, upColor, dnColor, textFont, swingType, swingSize, dtbStrength, useCloseValues);
		}

		public PriceActionTest PriceActionTest(ISeries<double> input, Brush upColor, Brush dnColor, NinjaTrader.Gui.Tools.SimpleFont textFont, SwingStyle swingType, double swingSize, int dtbStrength, bool useCloseValues)
		{
			if (cachePriceActionTest != null)
				for (int idx = 0; idx < cachePriceActionTest.Length; idx++)
					if (cachePriceActionTest[idx] != null && cachePriceActionTest[idx].UpColor == upColor && cachePriceActionTest[idx].DnColor == dnColor && cachePriceActionTest[idx].TextFont == textFont && cachePriceActionTest[idx].SwingType == swingType && cachePriceActionTest[idx].SwingSize == swingSize && cachePriceActionTest[idx].DtbStrength == dtbStrength && cachePriceActionTest[idx].UseCloseValues == useCloseValues && cachePriceActionTest[idx].EqualsInput(input))
						return cachePriceActionTest[idx];
			return CacheIndicator<PriceActionTest>(new PriceActionTest(){ UpColor = upColor, DnColor = dnColor, TextFont = textFont, SwingType = swingType, SwingSize = swingSize, DtbStrength = dtbStrength, UseCloseValues = useCloseValues }, input, ref cachePriceActionTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceActionTest PriceActionTest(Brush upColor, Brush dnColor, NinjaTrader.Gui.Tools.SimpleFont textFont, SwingStyle swingType, double swingSize, int dtbStrength, bool useCloseValues)
		{
			return indicator.PriceActionTest(Input, upColor, dnColor, textFont, swingType, swingSize, dtbStrength, useCloseValues);
		}

		public Indicators.PriceActionTest PriceActionTest(ISeries<double> input , Brush upColor, Brush dnColor, NinjaTrader.Gui.Tools.SimpleFont textFont, SwingStyle swingType, double swingSize, int dtbStrength, bool useCloseValues)
		{
			return indicator.PriceActionTest(input, upColor, dnColor, textFont, swingType, swingSize, dtbStrength, useCloseValues);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceActionTest PriceActionTest(Brush upColor, Brush dnColor, NinjaTrader.Gui.Tools.SimpleFont textFont, SwingStyle swingType, double swingSize, int dtbStrength, bool useCloseValues)
		{
			return indicator.PriceActionTest(Input, upColor, dnColor, textFont, swingType, swingSize, dtbStrength, useCloseValues);
		}

		public Indicators.PriceActionTest PriceActionTest(ISeries<double> input , Brush upColor, Brush dnColor, NinjaTrader.Gui.Tools.SimpleFont textFont, SwingStyle swingType, double swingSize, int dtbStrength, bool useCloseValues)
		{
			return indicator.PriceActionTest(input, upColor, dnColor, textFont, swingType, swingSize, dtbStrength, useCloseValues);
		}
	}
}

#endregion
