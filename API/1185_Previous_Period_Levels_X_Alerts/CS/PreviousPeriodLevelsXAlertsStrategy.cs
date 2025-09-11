using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that tracks previous period levels and logs SMA crosses.
/// </summary>
public class PreviousPeriodLevelsXAlertsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _referenceCandleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<bool> _useOpen;
	private readonly StrategyParam<bool> _useHigh;
	private readonly StrategyParam<bool> _useLow;
	private readonly StrategyParam<bool> _useClose;
	
	private decimal? _previousOpen;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _previousClose;
	private decimal? _lastSma;
	
	/// <summary>
	/// Base timeframe for SMA and trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Higher timeframe used to get previous OHLC levels.
	/// </summary>
	public DataType ReferenceCandleType
	{
		get => _referenceCandleType.Value;
		set => _referenceCandleType.Value = value;
	}
	
	/// <summary>
	/// SMA length for cross detection.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}
	
	/// <summary>
	/// Track previous open level.
	/// </summary>
	public bool UseOpen
	{
		get => _useOpen.Value;
		set => _useOpen.Value = value;
	}
	
	/// <summary>
	/// Track previous high level.
	/// </summary>
	public bool UseHigh
	{
		get => _useHigh.Value;
		set => _useHigh.Value = value;
	}
	
	/// <summary>
	/// Track previous low level.
	/// </summary>
	public bool UseLow
	{
		get => _useLow.Value;
		set => _useLow.Value = value;
	}
	
	/// <summary>
	/// Track previous close level.
	/// </summary>
	public bool UseClose
	{
		get => _useClose.Value;
		set => _useClose.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="PreviousPeriodLevelsXAlertsStrategy"/> class.
	/// </summary>
	public PreviousPeriodLevelsXAlertsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Base timeframe for SMA and trading", "General");
		
		_referenceCandleType = Param(nameof(ReferenceCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Reference Timeframe", "Higher timeframe used to get previous OHLC levels", "General");
		
		_smaLength = Param(nameof(SmaLength), 3)
		.SetDisplay("SMA Length", "Length of SMA for cross alerts", "Indicator")
		.SetRange(1, 100)
		.SetCanOptimize(true);
		
		_useOpen = Param(nameof(UseOpen), true)
		.SetDisplay("Use Open", "Track crosses of previous open level", "Levels");
		_useHigh = Param(nameof(UseHigh), true)
		.SetDisplay("Use High", "Track crosses of previous high level", "Levels");
		_useLow = Param(nameof(UseLow), true)
		.SetDisplay("Use Low", "Track crosses of previous low level", "Levels");
		_useClose = Param(nameof(UseClose), true)
		.SetDisplay("Use Close", "Track crosses of previous close level", "Levels");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, ReferenceCandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_previousOpen = null;
		_previousHigh = null;
		_previousLow = null;
		_previousClose = null;
		_lastSma = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var sma = new SimpleMovingAverage { Length = SmaLength };
		
		var baseSub = SubscribeCandles(CandleType);
		baseSub
		.Bind(sma, ProcessBaseCandle)
		.Start();
		
		var refSub = SubscribeCandles(ReferenceCandleType);
		refSub
		.Bind(ProcessReferenceCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessReferenceCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_previousOpen = candle.OpenPrice;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_previousClose = candle.ClosePrice;
	}
	
	private void ProcessBaseCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_lastSma is decimal prev)
		{
			if (UseOpen && _previousOpen is decimal open)
			CheckCross(prev, smaValue, open, "Open");
			if (UseHigh && _previousHigh is decimal high)
			CheckCross(prev, smaValue, high, "High");
			if (UseLow && _previousLow is decimal low)
			CheckCross(prev, smaValue, low, "Low");
			if (UseClose && _previousClose is decimal close)
			CheckCross(prev, smaValue, close, "Close");
		}
		
		_lastSma = smaValue;
	}
	
	private void CheckCross(decimal prev, decimal current, decimal level, string name)
	{
		if (prev < level && current > level)
		{
			LogInfo($"SMA crossed over previous {name} {level}");
		}
		else if (prev > level && current < level)
		{
			LogInfo($"SMA crossed under previous {name} {level}");
		}
	}
}
