//
// Copyright (C) 2019, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
public class AChartReminder : Indicator
{
		// These are WPF Brushes which are pushed and exposed to the UI by default
		// And allow users to configure a custom value of their choice
		// We will later convert the user defined brush from the UI to SharpDX Brushes for rendering purposes
		private System.Windows.Media.Brush	areaBrush;
		private System.Windows.Media.Brush	textBrush;
		private System.Windows.Media.Brush	smallAreaBrush;
		private int							areaOpacity;
		private SMA							mySma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionSampleCustomRender;
				Name						= "A Chart Reminder";
				Calculate					= Calculate.OnBarClose;
				DisplayInDataBox			= false;
				IsOverlay					= true;
				IsChartOnly					= true;
				IsSuspendedWhileInactive	= true;
				ScaleJustification			= ScaleJustification.Right;
				AreaBrush = System.Windows.Media.Brushes.DodgerBlue;
				TextBrush = System.Windows.Media.Brushes.DodgerBlue;
				SmallAreaBrush = System.Windows.Media.Brushes.Crimson;
				AreaOpacity = 20;
				AddPlot(System.Windows.Media.Brushes.Crimson, NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameSampleCustomRender);
			}
			else if (State == State.DataLoaded)
			{
				mySma = SMA(20);
			}
			else if (State == State.Historical)
			{
				SetZOrder(-1); // default here is go below the bars and called in State.Historical
			}
		}

		protected override void OnBarUpdate()
		{
			Value[0] = mySma[0];
		}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		// Tip: This check is simply added to prevent the Indicator dialog menu from opening as a user clicks on the chart
		// The default behavior is to open the Indicator dialog menu if a user double clicks on the indicator
		// (i.e, the indicator falls within the RenderTarget "hit testing")
		// You can remove this check if you want the default behavior implemented
		if (!IsInHitTest)
		{
			// 1.2 - SharpDX Brush Resources
			// RenderTarget commands must use a special brush resource defined in the SharpDX.Direct2D1 namespace
			// These resources exist just like you will find in the WPF/Windows.System.Media namespace
			// such as SolidColorBrushes, LienarGraidentBrushes, RadialGradientBrushes, etc.
			// To begin, we will start with the most basic "Brush" type
			// Warning:  Brush objects must be disposed of after they have been used
			SharpDX.Direct2D1.Brush areaBrushDx;
			SharpDX.Direct2D1.Brush smallAreaBrushDx;
			SharpDX.Direct2D1.Brush textBrushDx;
			// for convenience, you can simply convert a WPF Brush to a DXBrush using the ToDxBrush() extension method provided by NinjaTrader
			// This is a common approach if you have a Brush property created e.g., on the UI you wish to use in custom rendering routines
			areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
			smallAreaBrushDx = smallAreaBrush.ToDxBrush(RenderTarget);
			textBrushDx = textBrush.ToDxBrush(RenderTarget);
			// 1.3 - Using The RenderTarget
			// before executing chart commands, you have the ability to describe how the RenderTarget should render
			// for example, we can store the existing RenderTarget AntialiasMode mode
			// then update the AntialiasMode to be the quality of non-text primitives are rendered
			SharpDX.Direct2D1.AntialiasMode oldAntialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			// 1.6 - Simple Text Rendering

			// For rendering custom text to the Chart, there are a few ways you can approach depending on your requirements
			// The most straight forward way is to "borrow" the existing chartControl font provided as a "SimpleFont" class
			// Using the chartControl LabelFont, your custom object will also change to the user defined properties allowing
			// your object to match different fonts if defined by user.

			// The code below will use the chartControl Properties Label Font if it exists,
			// or fall back to a default property if it cannot obtain that value
			NinjaTrader.Gui.Tools.SimpleFont simpleFont = chartControl.Properties.LabelFont ??  new NinjaTrader.Gui.Tools.SimpleFont("Arial", 12);

			// the advantage of using a SimpleFont is they are not only very easy to describe
			// but there is also a convenience method which can be used to convert the SimpleFont to a SharpDX.DirectWrite.TextFormat used to render to the chart
			// Warning:  TextFormat objects must be disposed of after they have been used
			SharpDX.DirectWrite.TextFormat textFormat1 = simpleFont.ToDirectWriteTextFormat();

			// Once you have the format of the font, you need to describe how the font needs to be laid out
			// Here we will create a new Vector2() which draws the font according to the to top left corner of the chart (offset by a few pixels)
			SharpDX.Vector2 upperTextPoint = new SharpDX.Vector2(ChartPanel.X + 10, ChartPanel.Y + 20);
			// Warning:  TextLayout objects must be disposed of after they have been used
			SharpDX.DirectWrite.TextLayout textLayout1 =
				new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory,
					NinjaTrader.Custom.Resource.SampleCustomPlotUpperLeftCorner, textFormat1, ChartPanel.X + ChartPanel.W,
					textFormat1.FontSize);

//			// With the format and layout of the text completed, we can now render the font to the chart
//			RenderTarget.DrawTextLayout(upperTextPoint, textLayout1, textBrushDx,
//				SharpDX.Direct2D1.DrawTextOptions.NoSnap);

//			// 1.7 - Advanced Text Rendering

//			// Font formatting and text layouts can get as complex as you need them to be
//			// This example shows how to use a complete custom font unrelated to the existing user-defined chart control settings
//			// Warning:  TextLayout and TextFormat objects must be disposed of after they have been used
			SharpDX.DirectWrite.TextFormat textFormat2 =
				new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Century Gothic", FontWeight.Bold,
					FontStyle.Italic, 32f);
			SharpDX.DirectWrite.TextLayout textLayout2 =
				new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory,
					NinjaTrader.Custom.Resource.SampleCustomPlotLowerRightCorner, textFormat2, 400, textFormat1.FontSize);

//			// the textLayout object provides a way to measure the described font through a "Metrics" object
//			// This allows you to create new vectors on the chart which are entirely dependent on the "text" that is being rendered
//			// For example, we can create a rectangle that surrounds our font based off the textLayout which would dynamically change if the text used in the layout changed dynamically
			SharpDX.Vector2 lowerTextPoint = new SharpDX.Vector2(ChartPanel.W - textLayout2.Metrics.Width - 5,
				ChartPanel.Y + (ChartPanel.H - textLayout2.Metrics.Height));
			SharpDX.RectangleF rect1 = new SharpDX.RectangleF(lowerTextPoint.X, lowerTextPoint.Y, textLayout2.Metrics.Width,
				textLayout2.Metrics.Height);

//			// We can draw the Rectangle based on the TextLayout used above
			RenderTarget.FillRectangle(rect1, smallAreaBrushDx);
//			RenderTarget.DrawRectangle(rect1, smallAreaBrushDx, 2);

			// And render the advanced text layout using the DrawTextLayout() method
			// Note:  When drawing the same text repeatedly, using the DrawTextLayout() method is more efficient than using the DrawText()
			// because the text doesn't need to be formatted and the layout processed with each call
RenderTarget.DrawTextLayout(lowerTextPoint, textLayout2, textBrushDx, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

			// 1.8 - Cleanup
			// This concludes all of the rendering concepts used in the sample
			// However - there are some final clean up processes we should always provided before we are done

			// If changed, do not forget to set the AntialiasMode back to the default value as described above as a best practice
			RenderTarget.AntialiasMode = oldAntialiasMode;

			// We also need to make sure to dispose of every device dependent resource on each render pass
			// Failure to dispose of these resources will eventually result in unnecessary amounts of memory being used on the chart
			// Although the effects might not be obvious as first, if you see issues related to memory increasing over time
			// Objects such as these should be inspected first
			areaBrushDx.Dispose();
	//		customDXBrush.Dispose();
	//		gradientStopCollection.Dispose();
			//radialGradientBrush.Dispose();
			smallAreaBrushDx.Dispose();
			textBrushDx.Dispose();
			textFormat1.Dispose();
			textFormat2.Dispose();
			textLayout1.Dispose();
			textLayout2.Dispose();
		}
	}

	#region Properties
	[XmlIgnore]
	[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral")]
	public System.Windows.Media.Brush AreaBrush
	{
		get { return areaBrush; }
		set
		{
			areaBrush = value;
			if (areaBrush != null)
			{
				if (areaBrush.IsFrozen)
					areaBrush = areaBrush.Clone();
				areaBrush.Opacity = areaOpacity / 100d;
				areaBrush.Freeze();
			}
		}
	}

	[Browsable(false)]
	public string AreaBrushSerialize
	{
		get { return Serialize.BrushToString(AreaBrush); }
		set { AreaBrush = Serialize.StringToBrush(value); }
	}

	[Range(0, 100)]
	[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral")]
	public int AreaOpacity
	{
		get { return areaOpacity; }
		set
		{
			areaOpacity = Math.Max(0, Math.Min(100, value));
			if (areaBrush != null)
			{
				System.Windows.Media.Brush newBrush		= areaBrush.Clone();
				newBrush.Opacity	= areaOpacity / 100d;
				newBrush.Freeze();
				areaBrush			= newBrush;
			}
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Custom.Resource), Name = "SmallAreaColor", GroupName = "NinjaScriptGeneral")]
	public System.Windows.Media.Brush SmallAreaBrush
	{
		get { return smallAreaBrush; }
		set { smallAreaBrush = value; }
	}

	[Browsable(false)]
	public string SmallAreaBrushSerialize
	{
		get { return Serialize.BrushToString(SmallAreaBrush); }
		set { SmallAreaBrush = Serialize.StringToBrush(value); }
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> TestPlot
	{
		get { return Values[0]; }
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Custom.Resource), Name = "TextColor", GroupName = "NinjaScriptGeneral")]
	public System.Windows.Media.Brush TextBrush
	{
		get { return textBrush; }
		set { textBrush = value; }
	}

	[Browsable(false)]
	public string TextBrushSerialize
	{
		get { return Serialize.BrushToString(TextBrush); }
		set { TextBrush = Serialize.StringToBrush(value); }
	}
	#endregion
}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private AChartReminder[] cacheAChartReminder;
		public AChartReminder AChartReminder()
		{
			return AChartReminder(Input);
		}

		public AChartReminder AChartReminder(ISeries<double> input)
		{
			if (cacheAChartReminder != null)
				for (int idx = 0; idx < cacheAChartReminder.Length; idx++)
					if (cacheAChartReminder[idx] != null &&  cacheAChartReminder[idx].EqualsInput(input))
						return cacheAChartReminder[idx];
			return CacheIndicator<AChartReminder>(new AChartReminder(), input, ref cacheAChartReminder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.AChartReminder AChartReminder()
		{
			return indicator.AChartReminder(Input);
		}

		public Indicators.AChartReminder AChartReminder(ISeries<double> input )
		{
			return indicator.AChartReminder(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.AChartReminder AChartReminder()
		{
			return indicator.AChartReminder(Input);
		}

		public Indicators.AChartReminder AChartReminder(ISeries<double> input )
		{
			return indicator.AChartReminder(input);
		}
	}
}

#endregion
