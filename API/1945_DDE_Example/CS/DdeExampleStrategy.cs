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
	private readonly StrategyParam<string> _windowName;
	private readonly StrategyParam<int> _checkItemMessage;
	private readonly StrategyParam<int> _addItemMessage;
	private readonly StrategyParam<int> _setItemMessage;
	
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
	/// Target window name for DDE communication.
	/// </summary>
	public string WindowName
	{
		get => _windowName.Value;
		set => _windowName.Value = value;
	}

	/// <summary>
	/// Windows message code for checking item existence.
	/// </summary>
	public int CheckItemMessage
	{
		get => _checkItemMessage.Value;
		set => _checkItemMessage.Value = value;
	}

	/// <summary>
	/// Windows message code for adding new items.
	/// </summary>
	public int AddItemMessage
	{
		get => _addItemMessage.Value;
		set => _addItemMessage.Value = value;
	}

	/// <summary>
	/// Windows message code for setting item value.
	/// </summary>
	public int SetItemMessage
	{
		get => _setItemMessage.Value;
		set => _setItemMessage.Value = value;
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
		_windowName = Param(nameof(WindowName), "MT4.DDE.2")
		.SetDisplay("Window Name", "Target DDE window name", "DDE");
		_checkItemMessage = Param(nameof(CheckItemMessage), 0x0401)
		.SetDisplay("WM_CHECKITEM", "Message for item check", "DDE");
		_addItemMessage = Param(nameof(AddItemMessage), 0x0402)
		.SetDisplay("WM_ADDITEM", "Message for adding item", "DDE");
		_setItemMessage = Param(nameof(SetItemMessage), 0x0403)
		.SetDisplay("WM_SETITEM", "Message for updating item", "DDE");
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
	
	
	private bool CheckItem(string topic, string item)
	{
		var hWnd = FindWindowW(null, WindowName);
		if (hWnd == IntPtr.Zero)
		{
			LogError($"Cannot find {WindowName} window.");
			return false;
		}
		
		var atom = GlobalAddAtomW($"{topic}!{item}");
		if (atom == 0)
		{
			LogError($"Cannot create {topic}!{item} atom.");
			return false;
		}
		
		var ret = SendMessageW(hWnd, CheckItemMessage, (IntPtr)atom, IntPtr.Zero);
		GlobalDeleteAtom(atom);
		return HIWORD(ret) != 0;
	}
	
	private bool AddItem(string topic, string item)
	{
		var hWnd = FindWindowW(null, WindowName);
		if (hWnd == IntPtr.Zero)
		{
			LogError($"Cannot find {WindowName} window.");
			return false;
		}
		
		var atom = GlobalAddAtomW($"{topic}!{item}");
		if (atom == 0)
		{
			LogError($"Cannot create {topic}!{item} atom.");
			return false;
		}
		
		var ret = SendMessageW(hWnd, AddItemMessage, (IntPtr)atom, IntPtr.Zero);
		GlobalDeleteAtom(atom);
		return HIWORD(ret) != 0;
	}
	
	private bool SetItem(string topic, string item, string value)
	{
		var hWnd = FindWindowW(null, WindowName);
		if (hWnd == IntPtr.Zero)
		{
			LogError($"Cannot find {WindowName} window.");
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
		
		var ret = SendMessageW(hWnd, SetItemMessage, (IntPtr)itemAtom, (IntPtr)valueAtom);
		GlobalDeleteAtom(valueAtom);
		GlobalDeleteAtom(itemAtom);
		return HIWORD(ret) != 0;
	}
	
	private static int HIWORD(int value) => (value >> 16) & 0xFFFF;
}
