//+----------------------------------------------------------------------------------------------+
//| Copyright © <2020>  <LizardIndicators.com - powered by AlderLab UG>
//
//| This program is free software: you can redistribute it and/or modify
//| it under the terms of the GNU General Public License as published by
//| the Free Software Foundation, either version 3 of the License, or
//| any later version.
//|
//| This program is distributed in the hope that it will be useful,
//| but WITHOUT ANY WARRANTY; without even the implied warranty of
//| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//| GNU General Public License for more details.
//|
//| By installing this software you confirm acceptance of the GNU
//| General Public License terms. You may find a copy of the license
//| here; http://www.gnu.org/licenses/
//+----------------------------------------------------------------------------------------------+

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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators.LizardIndicators
{
	/// <summary>
	/// The Double Smoothed Stochastics by Walter Bressert is based on the Stochastics indicator by George Lane. The Double Smoothed Stochastics is derived from a modified Slow Stochastics with a %D line obtained
	/// from exponential smoothing instead of simple smoothing. The modified Slow Stochastics is then applied twice. This version of the Double Smoothed Stochastics comes with a signal line added.
	/// </summary>
	[Gui.CategoryOrder("Input Parameters", 1000100)]
	[Gui.CategoryOrder("Threshold Values", 1000200)]
	[Gui.CategoryOrder("Display Options", 1000300)]
	[Gui.CategoryOrder("%D Plot", 8000100)]
	[Gui.CategoryOrder("%K Plot", 8000200)]
	[Gui.CategoryOrder("Version", 8000300)]
	public class amaDSSBressert : Indicator
	{
		private int								periodK							= 10;
		private int								smooth							= 3;	
		private int								periodD							= 7;
		private int								displacement					= 0;
		private int								totalBarsRequiredToPlot			= 0;
		private double							k1								= 0.0;
		private double							oneMinusK1						= 0.0;
		private double							k2								= 0.0;
		private double							oneMinusK2						= 0.0;
		private double							den								= 0.0;
		private double							num								= 0.0;
		private double							minLow0							= 0.0;
		private double							maxHigh0						= 0.0;
		private double							minK0							= 0.0;
		private double							maxK0							= 0.0;
		private double							upperlineValue					= 80;
		private double							midlineValue					= 50;
		private double							lowerlineValue					= 20;
		private bool							showD							= true;
		private bool							showK							= true;
		private bool							showDShading					= true;
		private bool							showKShading					= true;
		private bool							applyDShading					= true;
		private bool							applyKShading					= true;
		private bool							calculateFromPriceData			= true;
		private System.Windows.Media.Brush		dOverboughtBrush				= Brushes.DarkSlateGray;
		private System.Windows.Media.Brush		dOversoldBrush					= Brushes.DarkSlateGray;
		private System.Windows.Media.Brush		dNeutralBrush					= Brushes.DarkSlateGray;
		private System.Windows.Media.Brush		kOverboughtBrush				= Brushes.DarkSlateBlue;
		private System.Windows.Media.Brush		kOversoldBrush					= Brushes.DarkSlateBlue;
		private System.Windows.Media.Brush		kNeutralBrush					= Brushes.DarkSlateBlue;
		private System.Windows.Media.Brush		dOverboughtAreaBrush			= Brushes.Navy;
		private System.Windows.Media.Brush		dOversoldAreaBrush				= Brushes.DarkRed;
		private System.Windows.Media.Brush		kOverboughtAreaBrush			= Brushes.RoyalBlue;
		private System.Windows.Media.Brush		kOversoldAreaBrush				= Brushes.Red;
		private System.Windows.Media.Brush		dOverboughtAreaBrushOpaque		= null;
		private System.Windows.Media.Brush		dOversoldAreaBrushOpaque		= null;
		private System.Windows.Media.Brush		kOverboughtAreaBrushOpaque		= null;
		private System.Windows.Media.Brush		kOversoldAreaBrushOpaque		= null;
		private System.Windows.Media.Brush		upperlineBrush					= Brushes.MidnightBlue;
		private System.Windows.Media.Brush		midlineBrush					= Brushes.DarkSlateGray;
		private System.Windows.Media.Brush		lowerlineBrush					= Brushes.DarkRed;
		private	SharpDX.Direct2D1.Brush 		dOverboughtBrushDX;
		private	SharpDX.Direct2D1.Brush 		dOversoldBrushDX;
		private	SharpDX.Direct2D1.Brush 		dNeutralBrushDX;
		private	SharpDX.Direct2D1.Brush 		kOverboughtBrushDX;
		private	SharpDX.Direct2D1.Brush 		kOversoldBrushDX;
		private	SharpDX.Direct2D1.Brush 		kNeutralBrushDX;
		private	SharpDX.Direct2D1.Brush 		dOverboughtAreaBrushDX;
		private	SharpDX.Direct2D1.Brush 		dOversoldAreaBrushDX;
		private	SharpDX.Direct2D1.Brush 		kOverboughtAreaBrushDX;
		private	SharpDX.Direct2D1.Brush 		kOversoldAreaBrushDX;
		private	SharpDX.Direct2D1.Brush 		upperlineBrushDX;
		private	SharpDX.Direct2D1.Brush 		midlineBrushDX;
		private	SharpDX.Direct2D1.Brush 		lowerlineBrushDX;
		private PlotStyle						plot0Style						= PlotStyle.Line;
		private DashStyleHelper					dash0Style						= DashStyleHelper.Dash;
		private int								plot0Width						= 2;
		private PlotStyle						plot1Style						= PlotStyle.Line;
		private DashStyleHelper					dash1Style						= DashStyleHelper.Solid;
		private int								plot1Width						= 2;
		private DashStyleHelper					upperlineStyle					= DashStyleHelper.Solid;
		private int								upperlineWidth					= 1;
		private DashStyleHelper					midlineStyle					= DashStyleHelper.Solid;
		private int								midlineWidth					= 1;
		private DashStyleHelper					lowerlineStyle					= DashStyleHelper.Solid;
		private int								lowerlineWidth					= 1;
		private int								dAreaOpacity					= 100;
		private int								kAreaOpacity					= 80;
		private string							versionString					= "v 2.2  -  April 17, 2020";
		private Series<double>					fastK;
		private Series<double>					emaFastK;
		private Series<double>					rawDSS;
		private Series<double>					signalline;
		private Series<double>					dTrend;
		private Series<double>					kTrend;
		private MIN								minLow;
		private MAX								maxHigh;
		private MIN								minK;
		private MAX								maxK;
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "\r\nThe Double Smoothed Stochastics by Walter Bressert is based on the Stochastics indicator by George Lane. The Double Smoothed Stochastics is derived from a modified"
											+ " Slow Stochastics with a %D line obtained from exponential smoothing instead of simple smoothing. The modified Slow Stochastics is then applied twice." 
											+ " This version of the Double Smoothed Stochastics comes with a signal line added." ;
				Name						= "amaDSSBressert";
				IsSuspendedWhileInactive	= true;
				ArePlotsConfigurable		= false;
				AreLinesConfigurable		= false;
				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "Stochastics D");
				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "Stochastics K");
				AddLine(new Stroke(Brushes.Gray, 1), 80, "Upper");
				AddLine(new Stroke(Brushes.Gray, 1), 50, "Middle");
				AddLine(new Stroke(Brushes.Gray, 1), 20, "Lower");
			}
			else if (State == State.Configure)
			{
				displacement = Displacement;
				BarsRequiredToPlot = Math.Max(2*periodK + 2*periodD + 6*smooth, -displacement);
				totalBarsRequiredToPlot = Math.Max(0, BarsRequiredToPlot + displacement);
				if(showD)
				{	
					Plots[0].PlotStyle = plot0Style;
					Plots[0].DashStyleHelper = dash0Style;
					Plots[0].Width = plot0Width;
				}	
				if(showK)
				{	
					Plots[1].PlotStyle = plot1Style;
					Plots[1].DashStyleHelper = dash1Style;
					Plots[1].Width = plot1Width;
				}	
				Lines[0].Value = upperlineValue;
				Lines[0].Brush = upperlineBrush;
				Lines[0].DashStyleHelper = upperlineStyle;
				Lines[0].Width = upperlineWidth;
				Lines[1].Value = midlineValue;
				Lines[1].Brush = midlineBrush;
				Lines[1].DashStyleHelper = midlineStyle;
				Lines[1].Width = midlineWidth;
				Lines[2].Value = lowerlineValue;
				Lines[2].Brush = lowerlineBrush;
				Lines[2].DashStyleHelper = lowerlineStyle;
				Lines[2].Width = lowerlineWidth;
				dOverboughtAreaBrushOpaque = dOverboughtAreaBrush.Clone(); 
				dOverboughtAreaBrushOpaque.Opacity = (float)dAreaOpacity/100.0;
				dOverboughtAreaBrushOpaque.Freeze();
				dOversoldAreaBrushOpaque = dOversoldAreaBrush.Clone(); 
				dOversoldAreaBrushOpaque.Opacity = (float) dAreaOpacity/100.0;
				dOversoldAreaBrushOpaque.Freeze();
				kOverboughtAreaBrushOpaque = kOverboughtAreaBrush.Clone(); 
				kOverboughtAreaBrushOpaque.Opacity = (float) kAreaOpacity/100.0;
				kOverboughtAreaBrushOpaque.Freeze();
				kOversoldAreaBrushOpaque = kOversoldAreaBrush.Clone(); 
				kOversoldAreaBrushOpaque.Opacity = (float) kAreaOpacity/100.0;
				kOversoldAreaBrushOpaque.Freeze();
			}
			else if (State == State.DataLoaded)
			{	
				fastK = new Series<double>(this, smooth < 255 ? MaximumBarsLookBack.TwoHundredFiftySix : MaximumBarsLookBack.Infinite);
				emaFastK = new Series<double>(this, MaximumBarsLookBack.Infinite);
				rawDSS	= new Series<double>(this, smooth < 255 ? MaximumBarsLookBack.TwoHundredFiftySix : MaximumBarsLookBack.Infinite);
				signalline	= new Series<double>(this, smooth < 255 ? MaximumBarsLookBack.TwoHundredFiftySix : MaximumBarsLookBack.Infinite);
				dTrend	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				kTrend	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				if(Input is PriceSeries)
				{	
					calculateFromPriceData = true;
					minLow = MIN(Low, periodK);
					maxHigh = MAX(High, periodK);
				}	
				else
				{	
					calculateFromPriceData = false;
					minLow = MIN(Input, periodK);
					maxHigh = MAX(Input, periodK);
				}
				minK = MIN(emaFastK, periodK);
				maxK = MAX(emaFastK, periodK);
			}
			else if (State == State.Historical)
			{
				k1 = 2.0 / (1 + smooth);
				oneMinusK1 = 1 - k1;
				k2 = 2.0 / (1 + periodD);
				oneMinusK2 = 1 - k2;
				applyKShading = showK && showKShading && kAreaOpacity > 0;
				applyDShading = showD && showDShading && dAreaOpacity > 0;
			}	
		}

		protected override void OnBarUpdate()
		{
			if (calculateFromPriceData)
			{
				if(IsFirstTickOfBar)
				{
					maxHigh0 = maxHigh[0]; 
					minLow0 = minLow[0];
				}
				else
				{	
					maxHigh0 = Math.Max(maxHigh0, High[0]); 
					minLow0 = Math.Min(minLow0, Low[0]);
				}	
				num	= Close[0] - minLow0;
			}	
			else
			{
				if(IsFirstTickOfBar)
				{
					maxHigh0 = maxHigh[0]; 
					minLow0 = minLow[0];
				}
				else
				{	
					maxHigh0 = Math.Max(maxHigh0, Input[0]); 
					minLow0 = Math.Min(minLow0, Input[0]);
				}	
				num	= Input[0] - minLow0;
			}	
			den	= maxHigh0 - minLow0;
			if(CurrentBar == 0)
			{
				if (den.ApproxCompare(0) == 0)
					fastK[0] = 50;
				else
					fastK[0] = num/den;
				emaFastK[0] = fastK[0];
			}	
			else
			{	
				if (den.ApproxCompare(0) == 0)
					fastK[0] = fastK[1];
				else
					fastK[0] = Math.Min(100, Math.Max(0, 100 * num/den));
				emaFastK[0] = k1 * fastK[0] + oneMinusK1 * emaFastK[1];
			}
			if(IsFirstTickOfBar)
			{
				maxK0 = maxK[0];
				minK0 = minK[0];
			}
			else
			{
				maxK0 = Math.Max(maxK0, emaFastK[0]); 
				minK0 = Math.Min(minK0, emaFastK[0]);
			}
			num = emaFastK[0] - minK0; 
			den = maxK0 - minK0;
			
			if (den.ApproxCompare(0) == 0)
				rawDSS[0] = CurrentBar == 0 ? 50 : rawDSS[1];
			else
				rawDSS[0] = Math.Min(100, Math.Max(0, 100 * num/den));
			
			if(CurrentBar == 0)
			{	
				K[0] = rawDSS[0];
				signalline[0] = K[0];
			}	
			else
			{	
				K[0] = k1 * rawDSS[0] + oneMinusK1 * K[1];
				signalline[0] = k2 * K[0] + oneMinusK2 * signalline[1];
			}	
			D[0] = Math.Min(100.0, Math.Max (0.0, signalline[0]));
			
			if(showD)
			{
				if(D[0] > upperlineValue)
				{	
					dTrend[0] = 1.0;
					PlotBrushes[0][0] = dOverboughtBrush;
				}	
				else if(D[0] < lowerlineValue)
				{	
					dTrend[0] = -1.0;
					PlotBrushes[0][0] = dOversoldBrush;
				}	
				else
				{	
					dTrend[0] = 0.0;
					PlotBrushes[0][0] = dNeutralBrush;
				}	
			}
			else
			{	
				if(D[0] > upperlineValue)
					dTrend[0] = 1.0;
				else if(D[0] < lowerlineValue)
					dTrend[0] = -1.0;
				else
					dTrend[0] = 0.0;
				PlotBrushes[0][0] = Brushes.Transparent;
			}
			if(showK)
			{
				if(K[0] > upperlineValue)
				{	
					kTrend[0] = 1.0;
					PlotBrushes[1][0] = kOverboughtBrush;
				}	
				else if(K[0] < lowerlineValue)
				{	
					kTrend[0] = -1.0;
					PlotBrushes[1][0] = kOversoldBrush;
				}	
				else
				{	
					kTrend[0] = 0.0;
					PlotBrushes[1][0] = kNeutralBrush;
				}	
			}
			else
			{	
				if(K[0] > upperlineValue)
					kTrend[0] = 1.0;
				else if(K[0] < lowerlineValue)
					kTrend[0] = -1.0;
				else
					kTrend[0] = 0.0;
				PlotBrushes[1][0] = Brushes.Transparent;
			}	
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> D
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> K
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> DTrend
		{
			get { return dTrend; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> KTrend
		{
			get { return kTrend; }
		}
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period K", GroupName = "Input Parameters", Order = 0)]
		public int PeriodK
		{	
            get { return periodK; }
            set { periodK = value; }
		}

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth K", GroupName = "Input Parameters", Order = 1)]
		public int Smooth
		{	
            get { return smooth; }
            set { smooth = value; }
		}
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period D", GroupName = "Input Parameters", Order = 2)]
		public int PeriodD
		{	
            get { return periodD; }
            set { periodD = value; }
		}
		
		[Range(50, 100), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stochastic overbought", Description = "Sets the overbought level for the two Stochastics indicators", GroupName = "Threshold Values", Order = 0)]
		public double UpperlineValue
		{	
            get { return upperlineValue; }
            set { upperlineValue = value; }
		}
		
		[Range(0, 50), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stochastic oversold", Description = "Sets the oversold level for the two Stochastics indicators", GroupName = "Threshold Values", Order = 1)]
		public double LowerlineValue
		{	
            get { return lowerlineValue; }
            set { lowerlineValue = value; }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show %D line", Description = "Displays %D line", GroupName = "Display Options", Order = 0)]
      	public bool ShowD
        {
            get { return showD; }
            set { showD = value; }
        }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show %D line shading", Description = "Displays %D line shading", GroupName = "Display Options", Order = 1)]
      	public bool ShowDShading
        {
            get { return showDShading; }
            set { showDShading = value; }
        }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show %K line", Description = "Displays %K line", GroupName = "Display Options", Order = 2)]
      	public bool ShowK
        {
            get { return showK; }
            set { showK = value; }
        }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Show %K line shading", Description = "Displays %K line shading", GroupName = "Display Options", Order = 3)]
      	public bool ShowKShading
        {
            get { return showKShading; }
            set { showKShading = value; }
        }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%D line overbought", Description = "Sets the color for the %D line when overbought", GroupName = "%D Plot", Order = 0)]
		public System.Windows.Media.Brush DOverboughtBrush
		{ 
			get {return dOverboughtBrush;}
			set {dOverboughtBrush = value;}
		}

		[Browsable(false)]
		public string DOverboughtBrushSerializable
		{
			get { return Serialize.BrushToString(dOverboughtBrush); }
			set { dOverboughtBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%D line neutral", Description = "Sets the color for the %D line when neutral", GroupName = "%D Plot", Order = 1)]
		public System.Windows.Media.Brush DNeutralBrush
		{ 
			get {return dNeutralBrush;}
			set {dNeutralBrush = value;}
		}

		[Browsable(false)]
		public string DNeutralBrushSerializable
		{
			get { return Serialize.BrushToString(dNeutralBrush); }
			set { dNeutralBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%D line oversold", Description = "Sets the color for the %D line when oversold", GroupName = "%D Plot", Order = 2)]
		public System.Windows.Media.Brush DOversoldBrush
		{ 
			get {return dOversoldBrush;}
			set {dOversoldBrush = value;}
		}

		[Browsable(false)]
		public string DOversoldBrushSerializable
		{
			get { return Serialize.BrushToString(dOversoldBrush); }
			set { dOversoldBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Dash style %D line", Description = "Sets the dash style for the %D line", GroupName = "%D Plot", Order = 3)]
		public DashStyleHelper Dash0Style
		{
			get { return dash0Style; }
			set { dash0Style = value; }
		}
		
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Plot width %D line", Description = "Sets the plot width for the %D line", GroupName = "%D Plot", Order = 5)]
		public int Plot0Width
		{	
            get { return plot0Width; }
            set { plot0Width = value; }
		}
			
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%D line overbought area", Description = "Sets the color for the %D line area when overbought", GroupName = "%D Plot", Order = 5)]
		public System.Windows.Media.Brush DOverboughtAreaBrush
		{ 
			get {return dOverboughtAreaBrush;}
			set {dOverboughtAreaBrush = value;}
		}

		[Browsable(false)]
		public string DOverboughtAreaBrushSerializable
		{
			get { return Serialize.BrushToString(dOverboughtAreaBrush); }
			set { dOverboughtAreaBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%D line oversold area", Description = "Sets the color for the %D line area when oversold", GroupName = "%D Plot", Order = 6)]
		public System.Windows.Media.Brush DOversoldAreaBrush
		{ 
			get {return dOversoldAreaBrush;}
			set {dOversoldAreaBrush = value;}
		}

		[Browsable(false)]
		public string DOversoldAreaBrushSerializable
		{
			get { return Serialize.BrushToString(dOversoldAreaBrush); }
			set { dOversoldAreaBrush = Serialize.StringToBrush(value); }
		}
		
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area opacity", Description = "Sets the opacity for the overbought and oversold areas", GroupName = "%D Plot", Order = 7)]
		public int DAreaOpacity
		{	
            get { return dAreaOpacity; }
            set { dAreaOpacity = value; }
		}
					
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%K line overbought", Description = "Sets the color for the %K line when overbought", GroupName = "%K Plot", Order = 0)]
		public System.Windows.Media.Brush KOverboughtBrush
		{ 
			get {return kOverboughtBrush;}
			set {kOverboughtBrush = value;}
		}

		[Browsable(false)]
		public string KOverboughtBrushSerializable
		{
			get { return Serialize.BrushToString(kOverboughtBrush); }
			set { kOverboughtBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%K line neutral", Description = "Sets the color for the %K line when neutral", GroupName = "%K Plot", Order = 1)]
		public System.Windows.Media.Brush KNeutralBrush
		{ 
			get {return kNeutralBrush;}
			set {kNeutralBrush = value;}
		}

		[Browsable(false)]
		public string KNeutralBrushSerializable
		{
			get { return Serialize.BrushToString(kNeutralBrush); }
			set { kNeutralBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%K line oversold", Description = "Sets the color for the %K line when oversold", GroupName = "%K Plot", Order = 2)]
		public System.Windows.Media.Brush KOversoldBrush
		{ 
			get {return kOversoldBrush;}
			set {kOversoldBrush = value;}
		}

		[Browsable(false)]
		public string KOversoldBrushSerializable
		{
			get { return Serialize.BrushToString(kOversoldBrush); }
			set { kOversoldBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Dash style %K line", Description = "Sets the dash style for the %K line", GroupName = "%K Plot", Order = 3)]
		public DashStyleHelper Dash1Style
		{
			get { return dash1Style; }
			set { dash1Style = value; }
		}
		
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Plot width %K line", Description = "Sets the plot width for the %K line", GroupName = "%K Plot", Order = 4)]
		public int Plot1Width
		{	
            get { return plot1Width; }
            set { plot1Width = value; }
		}
			
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%K line overbought area", Description = "Sets the color for the %K line area when overbought", GroupName = "%K Plot", Order = 5)]
		public System.Windows.Media.Brush KOverboughtAreaBrush
		{ 
			get {return kOverboughtAreaBrush;}
			set {kOverboughtAreaBrush = value;}
		}

		[Browsable(false)]
		public string KOverboughtAreaBrushSerializable
		{
			get { return Serialize.BrushToString(kOverboughtAreaBrush); }
			set { kOverboughtAreaBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "%K line oversold area", Description = "Sets the color for the %K line area when oversold", GroupName = "%K Plot", Order = 6)]
		public System.Windows.Media.Brush KOversoldAreaBrush
		{ 
			get {return kOversoldAreaBrush;}
			set {kOversoldAreaBrush = value;}
		}

		[Browsable(false)]
		public string KOversoldAreaBrushSerializable
		{
			get { return Serialize.BrushToString(kOversoldAreaBrush); }
			set { kOversoldAreaBrush = Serialize.StringToBrush(value); }
		}
		
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Area opacity", Description = "Sets the opacity for the overbought and oversold areas", GroupName = "%K Plot", Order = 7)]
		public int KAreaOpacity
		{	
            get { return kAreaOpacity; }
            set { kAreaOpacity = value; }
		}		
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Upper line", Description = "Sets the color for the %D line area when oversold", GroupName = "Lines", Order = 0)]
		public System.Windows.Media.Brush UpperlineBrush
		{ 
			get {return upperlineBrush;}
			set {upperlineBrush = value;}
		}

		[Browsable(false)]
		public string UpperlineBrushSerializable
		{
			get { return Serialize.BrushToString(upperlineBrush); }
			set { upperlineBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Dash style upper line", Description = "Sets the dash style for the upper line", GroupName = "Lines", Order = 1)]
		public DashStyleHelper UpperlineStyle
		{
			get { return upperlineStyle; }
			set { upperlineStyle = value; }
		}
		
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Plot width upper line", Description = "Sets the plot width for the upper line", GroupName = "Lines", Order = 2)]
		public int UpperlineWidth
		{	
            get { return upperlineWidth; }
            set { upperlineWidth = value; }
		}
			
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Midline", Description = "Sets the color for the %D line area when oversold", GroupName = "Lines", Order = 3)]
		public System.Windows.Media.Brush MidlineBrush
		{ 
			get {return midlineBrush;}
			set {midlineBrush = value;}
		}

		[Browsable(false)]
		public string MidlineBrushSerializable
		{
			get { return Serialize.BrushToString(midlineBrush); }
			set { midlineBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Dash style midline", Description = "Sets the dash style for the midline", GroupName = "Lines", Order = 4)]
		public DashStyleHelper MidlineStyle
		{
			get { return midlineStyle; }
			set { midlineStyle = value; }
		}
		
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Plot width midline", Description = "Sets the plot width for the midline", GroupName = "Lines", Order = 5)]
		public int MidlineWidth
		{	
            get { return midlineWidth; }
            set { midlineWidth = value; }
		}
			
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Lower line", Description = "Sets the color for the %D line area when oversold", GroupName = "Lines", Order = 6)]
		public System.Windows.Media.Brush LowerlineBrush
		{ 
			get {return lowerlineBrush;}
			set {lowerlineBrush = value;}
		}

		[Browsable(false)]
		public string LowerlineBrushSerializable
		{
			get { return Serialize.BrushToString(lowerlineBrush); }
			set { lowerlineBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Dash style lower line", Description = "Sets the dash style for the lower line", GroupName = "Lines", Order = 7)]
		public DashStyleHelper LowerlineStyle
		{
			get { return lowerlineStyle; }
			set { lowerlineStyle = value; }
		}
		
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Plot width lower line", Description = "Sets the plot width for the lower line", GroupName = "Lines", Order = 8)]
		public int LowerlineWidth
		{	
            get { return lowerlineWidth; }
            set { lowerlineWidth = value; }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Release and date", Description = "Release and date", GroupName = "Version", Order = 0)]
		public string VersionString
		{	
            get { return versionString; }
            set { ; }
		}
		#endregion
		
		#region Miscellaneous
		
		public override void OnRenderTargetChanged()
		{
			if (dOverboughtBrushDX != null)
				dOverboughtBrushDX.Dispose();
			if (dOversoldBrushDX != null)
				dOversoldBrushDX.Dispose();
			if (dNeutralBrushDX != null)
				dNeutralBrushDX.Dispose();
			if (kOverboughtBrushDX != null)
				kOverboughtBrushDX.Dispose();
			if (kOversoldBrushDX != null)
				kOversoldBrushDX.Dispose();
			if (kNeutralBrushDX != null)
				kNeutralBrushDX.Dispose();
			if (dOverboughtAreaBrushDX != null)
				dOverboughtAreaBrushDX.Dispose();
			if (dOversoldAreaBrushDX != null)
				dOversoldAreaBrushDX.Dispose();
			if (kOverboughtAreaBrushDX != null)
				kOverboughtAreaBrushDX.Dispose();
			if (kOversoldAreaBrushDX != null)
				kOversoldAreaBrushDX.Dispose();
			if (upperlineBrushDX != null)
				upperlineBrushDX.Dispose();
			if (midlineBrushDX != null)
				midlineBrushDX.Dispose();
			if (lowerlineBrushDX != null)
				lowerlineBrushDX.Dispose();
			
			if (RenderTarget != null)
			{
				try
				{
					dOverboughtBrushDX = dOverboughtBrush.ToDxBrush(RenderTarget);
					dOversoldBrushDX = dOversoldBrush.ToDxBrush(RenderTarget);
					dNeutralBrushDX = dNeutralBrush.ToDxBrush(RenderTarget);
					kOverboughtBrushDX = kOverboughtBrush.ToDxBrush(RenderTarget);
					kOversoldBrushDX = kOversoldBrush.ToDxBrush(RenderTarget);
					kNeutralBrushDX = kNeutralBrush.ToDxBrush(RenderTarget);
					dOverboughtAreaBrushDX = dOverboughtAreaBrushOpaque.ToDxBrush(RenderTarget);
					dOversoldAreaBrushDX = dOversoldAreaBrushOpaque.ToDxBrush(RenderTarget);
					kOverboughtAreaBrushDX = kOverboughtAreaBrushOpaque.ToDxBrush(RenderTarget);
					kOversoldAreaBrushDX = kOversoldAreaBrushOpaque.ToDxBrush(RenderTarget);
					upperlineBrushDX = upperlineBrush.ToDxBrush(RenderTarget);
					midlineBrushDX = midlineBrush.ToDxBrush(RenderTarget);
					lowerlineBrushDX = lowerlineBrush.ToDxBrush(RenderTarget);
				}
				catch (Exception e) { }
			}
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (Bars == null || ChartControl == null || ChartBars.ToIndex < BarsRequiredToPlot || !IsVisible) return;
			int	lastBarPainted = ChartBars.ToIndex;
			if(lastBarPainted  < 0 || BarsArray[0].Count < lastBarPainted) return;
			SharpDX.Direct2D1.AntialiasMode oldAntialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
	        ChartPanel panel = chartControl.ChartPanels[ChartPanel.PanelIndex];
			
			bool nonEquidistant 	= (chartControl.BarSpacingType == BarSpacingType.TimeBased || chartControl.BarSpacingType == BarSpacingType.EquidistantMulti);
			int lastBarCounted		= Close.Count - 1;
			int	lastBarOnUpdate		= lastBarCounted - (Calculate == Calculate.OnBarClose ? 1 : 0);
			int	lastBarIndex		= Math.Min(lastBarPainted, lastBarOnUpdate);
			int firstBarPainted	 	= ChartBars.FromIndex;
			int firstBarIndex  	 	= Math.Max(totalBarsRequiredToPlot, firstBarPainted); 
			int lastIndex			= 0;
			int x 					= 0;
			int y0	 				= 0;
			int y1 					= 0;
			int yUpper 				= 0;
			int yMid 				= 0;
			int yLower 				= 0;
			int y 					= 0;
			int t					= 0;
			int lastX 				= 0;
			int lastY1 				= 0;
			int lastY2 				= 0;
			int lastY 				= 0;
			int sign 				= 0;
			int lastSign 			= 0;
			int startBar 			= 0;
			int priorStartBar 		= 0;
			int returnBar 			= 0;
			int count	 			= 0;
			double barWidth			= 0;
			bool firstLoop 			= true;
			SharpDX.Vector2 startPointDX;
			SharpDX.Vector2 endPointDX;
			Vector2[] plotArray 	= new Vector2[2 * (lastBarIndex - firstBarIndex + Math.Max(0, displacement) + 1)]; 
			
			if(lastBarIndex + displacement > firstBarIndex)
			{	
				if (displacement >= 0)
					lastIndex = lastBarIndex + displacement;
				else
					lastIndex = Math.Min(lastBarIndex, lastBarOnUpdate + displacement);
				if(nonEquidistant && lastIndex > lastBarOnUpdate)
					barWidth = Convert.ToDouble(ChartControl.GetXByBarIndex(ChartBars, lastBarPainted) - ChartControl.GetXByBarIndex(ChartBars, lastBarPainted - displacement))/displacement;
				lastY1	= chartScale.GetYByValue(D.GetValueAt(lastIndex - displacement));
				lastY2	= chartScale.GetYByValue(K.GetValueAt(lastIndex - displacement));
				yUpper  = chartScale.GetYByValue(upperlineValue);
				yMid  = chartScale.GetYByValue(midlineValue);
				yLower  = chartScale.GetYByValue(lowerlineValue);
				
				//Lines
				startPointDX = new SharpDX.Vector2((float)ChartPanel.X, (float)yUpper);
				endPointDX	= new SharpDX.Vector2((float)ChartPanel.X + ChartPanel.W, (float)yUpper);
				RenderTarget.DrawLine(startPointDX, endPointDX, upperlineBrushDX, upperlineWidth, Lines[0].StrokeStyle);
				startPointDX = new SharpDX.Vector2((float)ChartPanel.X, (float)yMid);
				endPointDX	= new SharpDX.Vector2((float)ChartPanel.X + ChartPanel.W, (float)yMid);
				RenderTarget.DrawLine(startPointDX, endPointDX, midlineBrushDX, midlineWidth, Lines[1].StrokeStyle);
				startPointDX = new SharpDX.Vector2((float)ChartPanel.X, (float)yLower);
				endPointDX	= new SharpDX.Vector2((float)ChartPanel.X + ChartPanel.W, (float)yLower);
				RenderTarget.DrawLine(startPointDX, endPointDX, lowerlineBrushDX, lowerlineWidth, Lines[2].StrokeStyle);
				
				// %D Line Shading
				if(applyDShading)
				{	
					lastY = lastY1;
					lastSign = (int) DTrend.GetValueAt(lastIndex - displacement);
					startBar = lastIndex;
					firstLoop = true;
					do
					{
						SharpDX.Direct2D1.PathGeometry 	pathF;
						SharpDX.Direct2D1.GeometrySink 	sinkF;
						pathF = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
						using (pathF)
						{
							if(firstLoop)
							{	
								count = 0;						
								if(nonEquidistant && displacement > 0 && startBar > lastBarCounted)
									x = ChartControl.GetXByBarIndex(ChartBars, lastBarCounted) + (int)((startBar - lastBarCounted) * barWidth);
								else
									x = ChartControl.GetXByBarIndex(ChartBars, startBar);
								if (lastSign == 1)
									plotArray[count] = new Vector2(x, yUpper);
								else if(lastSign == -1)
									plotArray[count] = new Vector2(x, yLower);
							}	
							else
							{	
								count = 0;
								plotArray[count] = new Vector2(lastX, lastY);
							}
							for (int idx = startBar; idx >= firstBarIndex; idx --)	
							{
								if(nonEquidistant && displacement > 0 && idx > lastBarCounted)
									x = ChartControl.GetXByBarIndex(ChartBars, lastBarCounted) + (int)((idx - lastBarCounted) * barWidth);
								else
									x = ChartControl.GetXByBarIndex(ChartBars, idx);
								y = chartScale.GetYByValue(D.GetValueAt(idx - displacement));   
								t = (int) DTrend.GetValueAt(idx - displacement);
								count = count + 1;
								if(t == lastSign)
								{
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = t;
									lastX = x;
									lastY = y;
								}	
								else if (t == 0 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 0 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
								else if (t == 1 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
							}
							if(sign == 0)
							{	
								firstLoop = false;
								continue;
							}	
							if(startBar == firstBarIndex)
							{
								count = count + 1;
								if(sign == 1)
									plotArray[count] = new Vector2(lastX, yUpper);
								else if(sign == -1)
									plotArray[count] = new Vector2(lastX, yLower);
							}	
							sinkF = pathF.Open();
							sinkF.BeginFigure(plotArray[0], FigureBegin.Filled);
							for (int i = 1; i <= count; i++)
								sinkF.AddLine(plotArray[i]);
							sinkF.EndFigure(FigureEnd.Closed);
			        		sinkF.Close();
							RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
							if(sign == 1)
		 						RenderTarget.FillGeometry(pathF, dOverboughtAreaBrushDX);
							else if(sign == -1) 
		 						RenderTarget.FillGeometry(pathF, dOversoldAreaBrushDX);
							RenderTarget.AntialiasMode = oldAntialiasMode;
						}
						pathF.Dispose();
						sinkF.Dispose();
						firstLoop = false;
					}	
					while (startBar > firstBarIndex && D.IsValidDataPointAt(startBar - displacement));
				}
				
				// Stochastics %K Line Shading
				if(applyKShading)
				{	
					lastY = lastY2;
					lastSign = (int) KTrend.GetValueAt(lastIndex - displacement);
					startBar = lastIndex;
					firstLoop = true;
					do
					{
						SharpDX.Direct2D1.PathGeometry 	pathF;
						SharpDX.Direct2D1.GeometrySink 	sinkF;
						pathF = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
						using (pathF)
						{
							if(firstLoop)
							{	
								count = 0;						
								if(nonEquidistant && displacement > 0 && startBar > lastBarCounted)
									x = ChartControl.GetXByBarIndex(ChartBars, lastBarCounted) + (int)((startBar - lastBarCounted) * barWidth);
								else
									x = ChartControl.GetXByBarIndex(ChartBars, startBar);
								if (lastSign == 1)
									plotArray[count] = new Vector2(x, yUpper);
								else if(lastSign == -1)
									plotArray[count] = new Vector2(x, yLower);
							}	
							else
							{	
								count = 0;
								plotArray[count] = new Vector2(lastX, lastY);
							}
							for (int idx = startBar; idx >= firstBarIndex; idx --)	
							{
								if(nonEquidistant && displacement > 0 && idx > lastBarCounted)
									x = ChartControl.GetXByBarIndex(ChartBars, lastBarCounted) + (int)((idx - lastBarCounted) * barWidth);
								else
									x = ChartControl.GetXByBarIndex(ChartBars, idx);
								y = chartScale.GetYByValue(K.GetValueAt(idx - displacement));   
								t = (int) KTrend.GetValueAt(idx - displacement);
								count = count + 1;
								if(t == lastSign)
								{
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = t;
									lastX = x;
									lastY = y;
								}	
								else if (t == 0 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 0 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									startBar = idx;
									sign = lastSign;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
								else if (t == 1 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
							}
							if(sign == 0)
							{	
								firstLoop = false;
								continue;
							}	
							if(startBar == firstBarIndex)
							{
								count = count + 1;
								if(sign == 1)
									plotArray[count] = new Vector2(lastX, yUpper);
								else if(sign == -1)
									plotArray[count] = new Vector2(lastX, yLower);
							}	
							sinkF = pathF.Open();
							sinkF.BeginFigure(plotArray[0], FigureBegin.Filled);
							for (int i = 1; i <= count; i++)
								sinkF.AddLine(plotArray[i]);
							sinkF.EndFigure(FigureEnd.Closed);
			        		sinkF.Close();
							RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
							if(sign == 1)
		 						RenderTarget.FillGeometry(pathF, kOverboughtAreaBrushDX);
							else if(sign == -1) 
		 						RenderTarget.FillGeometry(pathF, kOversoldAreaBrushDX);
							RenderTarget.AntialiasMode = oldAntialiasMode;
						}
						pathF.Dispose();
						sinkF.Dispose();
						firstLoop = false;
					}	
					while (startBar > firstBarIndex && K.IsValidDataPointAt(startBar - displacement));
				}
				
				// Stochastics %D Line
				if(showD)
				{	
					lastY = lastY1;
					lastSign = 5;
					startBar = lastIndex;
					firstLoop = true;
					do
					{
						SharpDX.Direct2D1.PathGeometry 	pathE;
						SharpDX.Direct2D1.GeometrySink 	sinkE;
						pathE = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
						using (pathE)
						{
							count = 0;
							plotArray[count] = new Vector2(lastX, lastY);
							for (int idx = startBar; idx >= firstBarIndex; idx --)	
							{
								if(nonEquidistant && displacement > 0 && idx > lastBarCounted)
									x = ChartControl.GetXByBarIndex(ChartBars, lastBarCounted) + (int)((idx - lastBarCounted) * barWidth);
								else
									x = ChartControl.GetXByBarIndex(ChartBars, idx);
								y = chartScale.GetYByValue(D.GetValueAt(idx - displacement));   
								t = (int) DTrend.GetValueAt(idx - displacement);
								count = count + 1;
								if(t == lastSign)
								{
									plotArray[count] = new Vector2(x,y);
									sign = t;
									startBar = idx;
									lastX = x;
									lastY = y;
								}	
								else if(lastSign == 5)
								{	
									plotArray[count] = new Vector2(x,y);
									sign = 5;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}	
								else if (t == 0 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 0 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
								else if (t == 1 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
							}
							sinkE = pathE.Open();
							if(firstLoop)
								sinkE.BeginFigure(plotArray[1], FigureBegin.Filled);
							else
								sinkE.BeginFigure(plotArray[0], FigureBegin.Filled);
							for (int i = 1; i <= count; i++)
								sinkE.AddLine(plotArray[i]);
							sinkE.EndFigure(FigureEnd.Open);
			        		sinkE.Close();
							RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
							if(sign == 1)
		 						RenderTarget.DrawGeometry(pathE, dOverboughtBrushDX, Plots[0].Width, Plots[0].StrokeStyle);
							else if(sign == 0) 
		 						RenderTarget.DrawGeometry(pathE, dNeutralBrushDX, Plots[0].Width, Plots[0].StrokeStyle);
							else if(sign == -1) 
		 						RenderTarget.DrawGeometry(pathE, dOversoldBrushDX, Plots[0].Width, Plots[0].StrokeStyle);
							RenderTarget.AntialiasMode = oldAntialiasMode;
						}
						pathE.Dispose();
						sinkE.Dispose();
						firstLoop = false;
					}	
					while (startBar > firstBarIndex && D.IsValidDataPointAt(startBar - displacement));
				}

				// Stochastics %K Line
				if(showK)
				{	
					lastY = lastY2;
					lastSign = 5;
					startBar = lastIndex;
					firstLoop = true;
					do
					{
						SharpDX.Direct2D1.PathGeometry 	pathE;
						SharpDX.Direct2D1.GeometrySink 	sinkE;
						pathE = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
						using (pathE)
						{
							count = 0;
							plotArray[count] = new Vector2(lastX, lastY);
							for (int idx = startBar; idx >= firstBarIndex; idx --)	
							{
								if(nonEquidistant && displacement > 0 && idx > lastBarCounted)
									x = ChartControl.GetXByBarIndex(ChartBars, lastBarCounted) + (int)((idx - lastBarCounted) * barWidth);
								else
									x = ChartControl.GetXByBarIndex(ChartBars, idx);
								y = chartScale.GetYByValue(K.GetValueAt(idx - displacement));   
								t = (int) KTrend.GetValueAt(idx - displacement);
								count = count + 1;
								if(t == lastSign)
								{
									plotArray[count] = new Vector2(x,y);
									sign = t;
									startBar = idx;
									lastX = x;
									lastY = y;
								}	
								else if(lastSign == 5)
								{	
									plotArray[count] = new Vector2(x,y);
									sign = 5;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}	
								else if (t == 0 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 0 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == 1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 0)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = t;
									break;
								}
								else if (t == -1 && lastSign == 1)
								{	
									double lastDiff = Convert.ToDouble(yUpper - lastY);
									double diff = Convert.ToDouble(yUpper - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yUpper;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
								else if (t == 1 && lastSign == -1)
								{	
									double lastDiff = Convert.ToDouble(yLower - lastY);
									double diff = Convert.ToDouble(yLower - y);
									double denominator = lastDiff - diff;
									if(denominator.ApproxCompare(0) == 0)
										x = lastX - Convert.ToInt32((lastX - x) * 0.5);
									else	
										x = lastX - Convert.ToInt32((lastX - x) * lastDiff / denominator);
									y = yLower;
									plotArray[count] = new Vector2(x,y);
									sign = lastSign;
									startBar = idx;
									lastX = x;
									lastY = y;
									lastSign = 0;
									break;
								}
							}
							sinkE = pathE.Open();
							if(firstLoop)
								sinkE.BeginFigure(plotArray[1], FigureBegin.Filled);
							else
								sinkE.BeginFigure(plotArray[0], FigureBegin.Filled);
							for (int i = 1; i <= count; i++)
								sinkE.AddLine(plotArray[i]);
							sinkE.EndFigure(FigureEnd.Open);
			        		sinkE.Close();
							RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
							if(sign == 1)
		 						RenderTarget.DrawGeometry(pathE, kOverboughtBrushDX, Plots[1].Width, Plots[1].StrokeStyle);
							else if(sign == 0) 
		 						RenderTarget.DrawGeometry(pathE, kNeutralBrushDX, Plots[1].Width, Plots[1].StrokeStyle);
							else if(sign == -1) 
		 						RenderTarget.DrawGeometry(pathE, kOversoldBrushDX, Plots[1].Width, Plots[1].StrokeStyle);
							RenderTarget.AntialiasMode = oldAntialiasMode;
						}
						pathE.Dispose();
						sinkE.Dispose();
						firstLoop = false;
					}	
					while (startBar > firstBarIndex && K.IsValidDataPointAt(startBar - displacement));
				}			
			}
		}		
		#endregion		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private LizardIndicators.amaDSSBressert[] cacheamaDSSBressert;
		public LizardIndicators.amaDSSBressert amaDSSBressert(int periodK, int smooth, int periodD, double upperlineValue, double lowerlineValue)
		{
			return amaDSSBressert(Input, periodK, smooth, periodD, upperlineValue, lowerlineValue);
		}

		public LizardIndicators.amaDSSBressert amaDSSBressert(ISeries<double> input, int periodK, int smooth, int periodD, double upperlineValue, double lowerlineValue)
		{
			if (cacheamaDSSBressert != null)
				for (int idx = 0; idx < cacheamaDSSBressert.Length; idx++)
					if (cacheamaDSSBressert[idx] != null && cacheamaDSSBressert[idx].PeriodK == periodK && cacheamaDSSBressert[idx].Smooth == smooth && cacheamaDSSBressert[idx].PeriodD == periodD && cacheamaDSSBressert[idx].UpperlineValue == upperlineValue && cacheamaDSSBressert[idx].LowerlineValue == lowerlineValue && cacheamaDSSBressert[idx].EqualsInput(input))
						return cacheamaDSSBressert[idx];
			return CacheIndicator<LizardIndicators.amaDSSBressert>(new LizardIndicators.amaDSSBressert(){ PeriodK = periodK, Smooth = smooth, PeriodD = periodD, UpperlineValue = upperlineValue, LowerlineValue = lowerlineValue }, input, ref cacheamaDSSBressert);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.LizardIndicators.amaDSSBressert amaDSSBressert(int periodK, int smooth, int periodD, double upperlineValue, double lowerlineValue)
		{
			return indicator.amaDSSBressert(Input, periodK, smooth, periodD, upperlineValue, lowerlineValue);
		}

		public Indicators.LizardIndicators.amaDSSBressert amaDSSBressert(ISeries<double> input , int periodK, int smooth, int periodD, double upperlineValue, double lowerlineValue)
		{
			return indicator.amaDSSBressert(input, periodK, smooth, periodD, upperlineValue, lowerlineValue);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.LizardIndicators.amaDSSBressert amaDSSBressert(int periodK, int smooth, int periodD, double upperlineValue, double lowerlineValue)
		{
			return indicator.amaDSSBressert(Input, periodK, smooth, periodD, upperlineValue, lowerlineValue);
		}

		public Indicators.LizardIndicators.amaDSSBressert amaDSSBressert(ISeries<double> input , int periodK, int smooth, int periodD, double upperlineValue, double lowerlineValue)
		{
			return indicator.amaDSSBressert(input, periodK, smooth, periodD, upperlineValue, lowerlineValue);
		}
	}
}

#endregion
