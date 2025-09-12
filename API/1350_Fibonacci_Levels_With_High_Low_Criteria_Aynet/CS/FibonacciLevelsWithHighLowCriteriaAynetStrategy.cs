using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci levels strategy using high/low criteria from AYNET.
/// Buys when price closes above previous lowest low and a Fibonacci level.
/// Sells when price closes below previous highest high and the same level.
/// </summary>
public class FibonacciLevelsWithHighLowCriteriaAynetStrategy : Strategy
{
	private readonly StrategyParam<int> _lowestLookback;
	private readonly StrategyParam<int> _highestLookback;
	private readonly StrategyParam<decimal> _fibLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe;
	private readonly StrategyParam<bool> _useCurrentHtf;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _currentOpen;
	private decimal _currentHigh;
	private decimal _currentLow;
	private decimal _lastOpen;
	private decimal _lastHigh;
	private decimal _lastLow;
	private bool _htfReady;

	private decimal _prevHighCriteria;
	private decimal _prevLowCriteria;

	/// <summary>
	/// Lookback for lowest price.
	/// </summary>
	public int LowestLookback
	{
		get => _lowestLookback.Value;
		set => _lowestLookback.Value = value;
	}

	/// <summary>
	/// Lookback for highest price.
	/// </summary>
	public int HighestLookback
	{
		get => _highestLookback.Value;
		set => _highestLookback.Value = value;
	}

	/// <summary>
	/// Fibonacci level used for signals.
	/// </summary>
	public decimal FibLevel
	{
		get => _fibLevel.Value;
		set => _fibLevel.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe for Fibonacci calculation.
	/// </summary>
	public DataType HigherTimeframe
	{
		get => _higherTimeframe.Value;
		set => _higherTimeframe.Value = value;
	}

	/// <summary>
	/// Use current higher timeframe candle instead of last.
	/// </summary>
	public bool UseCurrentHtf
	{
		get => _useCurrentHtf.Value;
		set => _useCurrentHtf.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FibonacciLevelsWithHighLowCriteriaAynetStrategy"/>.
	/// </summary>
	public FibonacciLevelsWithHighLowCriteriaAynetStrategy()
	{
		_lowestLookback = Param(nameof(LowestLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Lookback", "Lookback bars for lowest price", "Criteria")
			.SetCanOptimize(true);

		_highestLookback = Param(nameof(HighestLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Highest Lookback", "Lookback bars for highest price", "Criteria")
			.SetCanOptimize(true);

		_fibLevel = Param(nameof(FibLevel), 0.5m)
			.SetDisplay("Fibonacci Level", "Fibonacci level for signals", "Fibonacci")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_higherTimeframe = Param(nameof(HigherTimeframe), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Higher Timeframe", "Timeframe for Fibonacci levels", "General");

		_useCurrentHtf = Param(nameof(UseCurrentHtf), false)
			.SetDisplay("Use Current HTF", "Use current higher timeframe candle", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeframe)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = default;
		_lowest = default;
		_currentOpen = default;
		_currentHigh = default;
		_currentLow = default;
		_lastOpen = default;
		_lastHigh = default;
		_lastLow = default;
		_htfReady = false;
		_prevHighCriteria = default;
		_prevLowCriteria = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = HighestLookback };
		_lowest = new Lowest { Length = LowestLookback };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(_highest, _lowest, ProcessCandle).Start();

		SubscribeCandles(HigherTimeframe)
			.Bind(ProcessHigher)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessHigher(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastOpen = _currentOpen;
		_lastHigh = _currentHigh;
		_lastLow = _currentLow;

		_currentOpen = candle.OpenPrice;
		_currentHigh = candle.HighPrice;
		_currentLow = candle.LowPrice;

		_htfReady = true;
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_htfReady)
		{
			_prevHighCriteria = highestValue;
			_prevLowCriteria = lowestValue;
			return;
		}

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevHighCriteria = highestValue;
			_prevLowCriteria = lowestValue;
			return;
		}

		var highCriteria = _prevHighCriteria;
		var lowCriteria = _prevLowCriteria;

		_prevHighCriteria = highestValue;
		_prevLowCriteria = lowestValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var open = UseCurrentHtf ? _currentOpen : _lastOpen;
		var high = UseCurrentHtf ? _currentHigh : _lastHigh;
		var low = UseCurrentHtf ? _currentLow : _lastLow;

		var fib = open + (high - low) * FibLevel;

		var buy = candle.ClosePrice > lowCriteria && candle.ClosePrice > fib;
		var sell = candle.ClosePrice < highCriteria && candle.ClosePrice < fib;

		if (buy && Position <= 0)
			BuyMarket();
		else if (sell && Position >= 0)
			SellMarket();
	}
}
