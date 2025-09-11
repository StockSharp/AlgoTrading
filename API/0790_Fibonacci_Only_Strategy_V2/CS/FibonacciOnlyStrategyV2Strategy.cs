using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades around 19% and 82.56% Fibonacci retracement levels.
/// </summary>
public class FibonacciOnlyStrategyV2Strategy : Strategy
{
	private readonly StrategyParam<bool> _useBreakStrategy;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useAtrForSl;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _initialized;

	/// <summary>
	/// Allow entries on Fibonacci level breakouts.
	/// </summary>
	public bool UseBreakStrategy
	{
		get => _useBreakStrategy.Value;
		set => _useBreakStrategy.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Use ATR for stop loss calculation.
	/// </summary>
	public bool UseAtrForSl
	{
		get => _useAtrForSl.Value;
		set => _useAtrForSl.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FibonacciOnlyStrategyV2Strategy()
	{
		_useBreakStrategy = Param(nameof(UseBreakStrategy), true)
			.SetDisplay("Use Break Strategy", "Allow entries on level breakouts", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk Management");

		_useAtrForSl = Param(nameof(UseAtrForSl), true)
			.SetDisplay("Use ATR for SL", "Calculate stop loss using ATR", "Risk Management");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop loss", "Risk Management");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk Management");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop %", "Trailing stop percentage", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_entryPrice = 0;
		_highestSinceEntry = 0;
		_lowestSinceEntry = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = 14 };
		var highest = new Highest { Length = 93 };
		var lowest = new Lowest { Length = 93 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fibHigh = highestValue;
		var fibLow = lowestValue;
		var fibDiff = fibHigh - fibLow;
		var fib19 = fibHigh - fibDiff * 0.19m;
		var fib8256 = fibHigh - fibDiff * 0.8256m;
		var fib19Reverse = fibLow + fibDiff * 0.19m;

		var fib19Touch = _prevLow > fib19 && candle.LowPrice <= fib19;
		var fib8256Touch = _prevHigh < fib8256 && candle.HighPrice >= fib8256;
		var fib19Break = candle.ClosePrice < fib19 && candle.OpenPrice > fib19;
		var fib8256Break = candle.ClosePrice > fib8256 && candle.OpenPrice < fib8256;
		var fib19ReverseTouch = _prevHigh < fib19Reverse && candle.HighPrice >= fib19Reverse;
		var fib19ReverseBreak = candle.ClosePrice > fib19Reverse && candle.OpenPrice < fib19Reverse;

		var bullConfirmation = candle.ClosePrice > candle.OpenPrice;
		var bearConfirmation = candle.ClosePrice < candle.OpenPrice;

		var longConditionFibTouch = (fib19Touch || fib19ReverseTouch) && bullConfirmation;
		var longConditionFibBreak = UseBreakStrategy && (fib19Break || fib19ReverseBreak) && bullConfirmation;
		var shortConditionFibTouch = fib19Touch && bearConfirmation;
		var shortConditionFibBreak = UseBreakStrategy && fib19Break && bearConfirmation;

		var longCondition = longConditionFibTouch || longConditionFibBreak;
		var shortCondition = shortConditionFibTouch || shortConditionFibBreak;

		if (longCondition && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_highestSinceEntry = candle.HighPrice;
			_lowestSinceEntry = candle.LowPrice;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_highestSinceEntry = candle.HighPrice;
			_lowestSinceEntry = candle.LowPrice;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			if (candle.HighPrice > _highestSinceEntry)
				_highestSinceEntry = candle.HighPrice;

			var stop = UseAtrForSl
				? _entryPrice - atrValue * AtrMultiplier
				: _entryPrice * (1 - StopLossPercent / 100m);

			if (UseTrailingStop)
			{
				var trail = _highestSinceEntry - (_highestSinceEntry - _entryPrice) * TrailingStopPercent / 100m;
				if (trail > stop)
					stop = trail;
			}

			if (candle.LowPrice <= stop)
				SellMarket(Volume + Position);
		}
		else if (Position < 0)
		{
			if (candle.LowPrice < _lowestSinceEntry)
				_lowestSinceEntry = candle.LowPrice;

			var stop = UseAtrForSl
				? _entryPrice + atrValue * AtrMultiplier
				: _entryPrice * (1 + StopLossPercent / 100m);

			if (UseTrailingStop)
			{
				var trail = _lowestSinceEntry + (_entryPrice - _lowestSinceEntry) * TrailingStopPercent / 100m;
				if (trail < stop)
					stop = trail;
			}

			if (candle.HighPrice >= stop)
				BuyMarket(Volume + Math.Abs(Position));
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
