using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Defines how the strategy manages simultaneous entries.
/// </summary>
public enum SmallInsideBarOpenMode
{
	/// <summary>
	/// Open a new position on every signal without forcing opposite positions to close.
	/// </summary>
	AnySignal,

	/// <summary>
	/// Close opposite positions first and allow adding to the current swing direction.
	/// </summary>
	SwingWithRefill,

	/// <summary>
	/// Maintain a single position in the market and ignore additional entries while it is active.
	/// </summary>
	SingleSwing
}

/// <summary>
/// Implements the "Small Inside Bar" pattern strategy converted from MetaTrader 5.
/// The strategy searches for an inside bar with a small range compared to the mother bar
/// and opens positions following the direction of the pattern conditions.
/// </summary>
public class SmallInsideBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _rangeRatioThreshold;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<SmallInsideBarOpenMode> _openMode;

	private ICandleMessage _previousCandle;
	private ICandleMessage _twoBackCandle;

	/// <summary>
	/// Type of candles used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum ratio between the mother bar range and the inside bar range.
	/// </summary>
	public decimal RangeRatioThreshold
	{
		get => _rangeRatioThreshold.Value;
		set => _rangeRatioThreshold.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Reverse long and short signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Mode for handling position entries.
	/// </summary>
	public SmallInsideBarOpenMode OpenMode
	{
		get => _openMode.Value;
		set => _openMode.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SmallInsideBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for pattern detection", "General");

		_rangeRatioThreshold = Param(nameof(RangeRatioThreshold), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Range Ratio", "Minimum mother-to-inside bar range ratio", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 3m, 0.25m);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow bullish trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow bearish trades", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short signals", "Trading");

		_openMode = Param(nameof(OpenMode), SmallInsideBarOpenMode.SwingWithRefill)
			.SetDisplay("Open Mode", "Position management mode", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_twoBackCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousCandle == null)
		{
			_previousCandle = candle;
			return;
		}

		if (_twoBackCandle == null)
		{
			_twoBackCandle = _previousCandle;
			_previousCandle = candle;
			return;
		}

		var insideHigh = _previousCandle.HighPrice;
		var insideLow = _previousCandle.LowPrice;
		var motherHigh = _twoBackCandle.HighPrice;
		var motherLow = _twoBackCandle.LowPrice;

		if (insideHigh <= insideLow || motherHigh <= motherLow)
		{
			ShiftHistory(candle);
			return;
		}

		if (!(insideHigh < motherHigh && insideLow > motherLow))
		{
			ShiftHistory(candle);
			return;
		}

		var insideRange = insideHigh - insideLow;
		var motherRange = motherHigh - motherLow;
		var ratio = insideRange == 0 ? decimal.MaxValue : motherRange / insideRange;

		if (ratio <= RangeRatioThreshold)
		{
			ShiftHistory(candle);
			return;
		}

		var midpoint = (motherHigh + motherLow) / 2m;

		var bullishInside = _previousCandle.ClosePrice > _previousCandle.OpenPrice && insideHigh < midpoint && _twoBackCandle.ClosePrice < _twoBackCandle.OpenPrice;
		var bearishInside = _previousCandle.ClosePrice < _previousCandle.OpenPrice && insideLow < midpoint && _twoBackCandle.ClosePrice > _twoBackCandle.OpenPrice;

		if (ReverseSignals)
		{
			(bullishInside, bearishInside) = (bearishInside, bullishInside);
		}

		var shouldOpenLong = bullishInside && EnableLong;
		var shouldOpenShort = bearishInside && EnableShort;

		if (shouldOpenLong)
		{
			var volume = CalculateOrderVolume(true);

			if (volume > 0)
				BuyMarket(volume);
		}

		if (shouldOpenShort)
		{
			var volume = CalculateOrderVolume(false);

			if (volume > 0)
				SellMarket(volume);
		}

		ShiftHistory(candle);
	}

	private decimal CalculateOrderVolume(bool isLong)
	{
		var baseVolume = Volume;

		if (baseVolume <= 0)
			return 0;

		var position = Position;

		if (isLong)
		{
			if (OpenMode == SmallInsideBarOpenMode.SingleSwing && position > 0)
				return 0;

			if (position < 0 && OpenMode != SmallInsideBarOpenMode.AnySignal)
				baseVolume += Math.Abs(position);
		}
		else
		{
			if (OpenMode == SmallInsideBarOpenMode.SingleSwing && position < 0)
				return 0;

			if (position > 0 && OpenMode != SmallInsideBarOpenMode.AnySignal)
				baseVolume += Math.Abs(position);
		}

		return baseVolume;
	}

	private void ShiftHistory(ICandleMessage candle)
	{
		// Keep track of the last two finished candles for pattern evaluation.
		_twoBackCandle = _previousCandle;
		_previousCandle = candle;
	}
}
