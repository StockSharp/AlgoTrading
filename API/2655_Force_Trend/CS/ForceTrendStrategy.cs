using System;
using System.Collections.Generic;
using StockSharp.Algo;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Force Trend strategy that mirrors the original MT5 expert advisor logic.
/// It reacts to ForceTrend indicator color changes to switch between long and short positions.
/// </summary>
public class ForceTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _previousForceValue;
	private decimal _previousIndicatorValue;
	private int?[] _directionHistory = Array.Empty<int?>();
	private int _historyCount;
	private int? _lastKnownDirection;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ForceTrendStrategy()
	{
		_length = Param(nameof(Length), 13)
			.SetDisplay("Length", "ForceTrend lookback length", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Number of finished bars to shift the signal", "Trading")
			.SetCanOptimize(true);

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
			.SetCanOptimize(true);

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
			.SetCanOptimize(true);

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading")
			.SetCanOptimize(true);

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for ForceTrend calculations", "General");
	}

	/// <summary>
	/// ForceTrend lookback length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Number of finished candles used to shift the trade signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening long positions when the ForceTrend becomes bullish.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions when the ForceTrend becomes bearish.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions on bearish ForceTrend signals.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions on bullish ForceTrend signals.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_previousForceValue = 0m;
		_previousIndicatorValue = 0m;
		_historyCount = 0;
		_lastKnownDirection = null;
		_directionHistory = new int?[Math.Max(SignalBar + 2, 2)];

		_highest = new Highest { Length = Length };
		_lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestValue = _highest.Process(candle).ToDecimal();
		var lowestValue = _lowest.Process(candle).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var range = highestValue - lowestValue;
		decimal forceValue;

		if (range != 0m)
		{
			var average = (candle.HighPrice + candle.LowPrice) / 2m;
			var normalized = (average - lowestValue) / range - 0.5m;
			forceValue = 0.66m * normalized + 0.67m * _previousForceValue;
		}
		else
		{
			forceValue = 0.67m * _previousForceValue - 0.33m;
		}

		forceValue = Math.Clamp(forceValue, -0.999m, 0.999m);

		decimal indicatorValue;
		var denominator = 1m - forceValue;

		if (denominator != 0m)
		{
			var ratio = (forceValue + 1m) / denominator;
			indicatorValue = (decimal)(Math.Log((double)ratio) / 2.0) + _previousIndicatorValue / 2m;
		}
		else
		{
			indicatorValue = _previousIndicatorValue / 2m + 0.5m;
		}

		_previousForceValue = forceValue;
		_previousIndicatorValue = indicatorValue;

		var direction = indicatorValue > 0m ? 1 : indicatorValue < 0m ? -1 : _lastKnownDirection ?? 0;
		if (direction != 0)
			_lastKnownDirection = direction;

		AddDirection(direction);

		var currentDirection = GetDirection(SignalBar);
		if (currentDirection is null)
			return;

		var previousDirection = GetDirection(SignalBar + 1);
		var bullish = currentDirection.Value > 0;
		var bearish = currentDirection.Value < 0;
		var bullishFlip = bullish && previousDirection.HasValue && previousDirection.Value <= 0;
		var bearishFlip = bearish && previousDirection.HasValue && previousDirection.Value >= 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullish)
		{
			var volumeToBuy = 0m;

			if (EnableShortExit && Position < 0m)
				volumeToBuy += Math.Abs(Position);

			if (EnableLongEntry && bullishFlip && Position <= 0m)
				volumeToBuy += Volume;

			if (volumeToBuy > 0m)
				BuyMarket(volumeToBuy);
		}
		else if (bearish)
		{
			var volumeToSell = 0m;

			if (EnableLongExit && Position > 0m)
				volumeToSell += Math.Abs(Position);

			if (EnableShortEntry && bearishFlip && Position >= 0m)
				volumeToSell += Volume;

			if (volumeToSell > 0m)
				SellMarket(volumeToSell);
		}
	}

	private void AddDirection(int direction)
	{
		if (_historyCount < _directionHistory.Length)
		{
			_directionHistory[_historyCount] = direction;
			_historyCount++;
		}
		else
		{
			for (var i = 1; i < _directionHistory.Length; i++)
				_directionHistory[i - 1] = _directionHistory[i];

			_directionHistory[^1] = direction;
		}
	}

	private int? GetDirection(int offset)
	{
		if (offset < 0)
			return null;

		var index = _historyCount - 1 - offset;
		if (index < 0)
			return null;

		return _directionHistory[index];
	}
}
