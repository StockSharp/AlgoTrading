using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates sending EMA values through Windows DDE interface.
/// </summary>
public class DdeExampleStrategy : Strategy
{
	// Indicator parameters
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	
	/// <summary>
	/// EMA calculation period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	
	/// <summary>
	/// Candle type used for indicator calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public DdeExampleStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Period of the EMA", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for processing", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ema = new EMA { Length = EmaLength };
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(ema, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!CheckItem("A", "B"))
		{
			if (!AddItem("A", "B"))
			return;
		}
		
		if (!CheckItem("COMPANY", "Value"))
		AddItem("COMPANY", "Value");
		
		if (!CheckItem("TIME", "Value"))
		AddItem("TIME", "Value");
		
		SetItem("COMPANY", "Value", "StockSharp");
		SetItem("TIME", "Value", candle.CloseTime.ToString());
		SetItem("A", "B", $"EMA({EmaLength}): {emaValue:F6}");
	}
	
	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);
	
	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
	
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern ushort GlobalAddAtomW(string lpString);
	
	[DllImport("kernel32.dll")]
	private static extern ushort GlobalDeleteAtom(ushort nAtom);
	
	private const string WndName = "MT4.DDE.2";
	private const int WM_CHECKITEM = 0x0401;
	private const int WM_ADDITEM = 0x0402;
	private const int WM_SETITEM = 0x0403;
	
	private bool CheckItem(string topic, string item)
	{
		var hWnd = FindWindowW(null, WndName);
		if (hWnd == IntPtr.Zero)
		{
			LogError($"Cannot find {WndName} window.");
			return false;
		}
		
		var atom = GlobalAddAtomW($"{topic}!{item}");
		if (atom == 0)
		{
			LogError($"Cannot create {topic}!{item} atom.");
			return false;
		}
		
		var ret = SendMessageW(hWnd, WM_CHECKITEM, (IntPtr)atom, IntPtr.Zero);
		GlobalDeleteAtom(atom);
		return HIWORD(ret) != 0;
	}
	
	private bool AddItem(string topic, string item)
	{
		var hWnd = FindWindowW(null, WndName);
		if (hWnd == IntPtr.Zero)
		{
			LogError($"Cannot find {WndName} window.");
			return false;
		}
		
		var atom = GlobalAddAtomW($"{topic}!{item}");
		if (atom == 0)
		{
			LogError($"Cannot create {topic}!{item} atom.");
			return false;
		}
		
		var ret = SendMessageW(hWnd, WM_ADDITEM, (IntPtr)atom, IntPtr.Zero);
		GlobalDeleteAtom(atom);
		return HIWORD(ret) != 0;
	}
	
	private bool SetItem(string topic, string item, string value)
	{
		var hWnd = FindWindowW(null, WndName);
		if (hWnd == IntPtr.Zero)
		{
			LogError($"Cannot find {WndName} window.");
			return false;
		}
		
		var itemAtom = GlobalAddAtomW($"{topic}!{item}");
		if (itemAtom == 0)
		{
			LogError($"Cannot create {topic}!{item} atom.");
			return false;
		}
		
		var valueAtom = GlobalAddAtomW(value);
		if (valueAtom == 0)
		{
			LogError($"Cannot create {value} atom.");
			GlobalDeleteAtom(itemAtom);
			return false;
		}
		
		var ret = SendMessageW(hWnd, WM_SETITEM, (IntPtr)itemAtom, (IntPtr)valueAtom);
		GlobalDeleteAtom(valueAtom);
		GlobalDeleteAtom(itemAtom);
		return HIWORD(ret) != 0;
	}
	
	private static int HIWORD(int value) => (value >> 16) & 0xFFFF;
}
