using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci-only strategy with optional breakout entries, ATR stop loss, trailing stop and stacked take profits.
/// </summary>
public class FibonacciOnlyStrategy : Strategy
{
	private readonly StrategyParam<bool> _useBreakStrategy;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useAtrForSl;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private AverageTrueRange _atr;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _entryPrice;
	private decimal _entryVolume;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _trailingLong;
	private decimal _trailingShort;
	private int _tpCount;

	/// <summary>
	/// Enable breakout entries.
	/// </summary>
	public bool UseBreakStrategy { get => _useBreakStrategy.Value; set => _useBreakStrategy.Value = value; }

	/// <summary>
	/// Percentage stop loss for positions.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Use ATR-based stop loss instead of percentage.
	/// </summary>
	public bool UseAtrForSl { get => _useAtrForSl.Value; set => _useAtrForSl.Value = value; }

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent { get => _trailingStopPercent.Value; set => _trailingStopPercent.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FibonacciOnlyStrategy()
	{
		_useBreakStrategy = Param(nameof(UseBreakStrategy), true)
			.SetDisplay("Use Break Strategy", "Enable breakout entries", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk");

		_useAtrForSl = Param(nameof(UseAtrForSl), true)
			.SetDisplay("Use ATR for Stop Loss", "Use ATR-based stop loss", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop loss", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevHigh = 0m;
		_prevLow = 0m;
		_entryPrice = 0m;
		_entryVolume = 0m;
		_longStop = 0m;
		_shortStop = 0m;
		_trailingLong = 0m;
		_trailingShort = decimal.MaxValue;
		_tpCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = 100 };
		_lowest = new Lowest { Length = 100 };
		_atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { _highest, _lowest, _atr }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || !_highest.IsFormed || !_lowest.IsFormed || !_atr.IsFormed)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		var fibHigh = values[0].ToDecimal();
		var fibLow = values[1].ToDecimal();
		var atr = values[2].ToDecimal();
		var diff = fibHigh - fibLow;
		var fib19 = fibHigh - diff * 0.19m;
		var fib8256 = fibHigh - diff * 0.8256m;
		var fib19Reverse = fibLow + diff * 0.19m;

		var fib19Touch = _prevLow > fib19 && candle.LowPrice <= fib19;
		var fib8256Touch = _prevHigh < fib8256 && candle.HighPrice >= fib8256;
		var fib19Break = candle.ClosePrice < fib19 && candle.OpenPrice > fib19;
		var fib8256Break = candle.ClosePrice > fib8256 && candle.OpenPrice < fib8256;
		var fib19ReverseTouch = _prevHigh < fib19Reverse && candle.HighPrice >= fib19Reverse;
		var fib19ReverseBreak = candle.ClosePrice > fib19Reverse && candle.OpenPrice < fib19Reverse;

		var bullConfirmation = candle.ClosePrice > candle.OpenPrice;
		var bearConfirmation = candle.ClosePrice < candle.OpenPrice;

		var longCondition = (fib19Touch || fib19ReverseTouch) && bullConfirmation;
		var longBreak = UseBreakStrategy && (fib19Break || fib19ReverseBreak) && bullConfirmation;
		var shortCondition = fib8256Touch && bearConfirmation;
		var shortBreak = UseBreakStrategy && fib8256Break && bearConfirmation;

		var volume = Volume + Math.Abs(Position);

		if ((longCondition || longBreak) && Position <= 0 && volume > 0m)
		{
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_entryVolume = volume;
			_tpCount = 0;
			_longStop = UseAtrForSl ? _entryPrice - atr * AtrMultiplier : _entryPrice * (1m - StopLossPercent / 100m);
			_trailingLong = candle.HighPrice - (candle.HighPrice - _entryPrice) * TrailingStopPercent / 100m;
		}
		else if ((shortCondition || shortBreak) && Position >= 0 && volume > 0m)
		{
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_entryVolume = volume;
			_tpCount = 0;
			_shortStop = UseAtrForSl ? _entryPrice + atr * AtrMultiplier : _entryPrice * (1m + StopLossPercent / 100m);
			_trailingShort = candle.LowPrice + (_entryPrice - candle.LowPrice) * TrailingStopPercent / 100m;
		}

		if (Position > 0)
		{
			if (UseTrailingStop)
			{
				var newTrail = candle.HighPrice - (candle.HighPrice - _entryPrice) * TrailingStopPercent / 100m;
				_trailingLong = Math.Max(_trailingLong, newTrail);
			}

			var stop = UseTrailingStop ? Math.Max(_trailingLong, _longStop) : _longStop;
			if (candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
			}
			else
			{
				while (_tpCount < 7)
				{
					var target = _entryPrice * (1m + (_tpCount + 1) * 0.005m);
					if (candle.HighPrice >= target)
					{
						var part = _entryVolume / 7m;
						SellMarket(Math.Min(part, Math.Abs(Position)));
						_tpCount++;
					}
					else
						break;
				}
			}
		}
		else if (Position < 0)
		{
			if (UseTrailingStop)
			{
				var newTrail = candle.LowPrice + (_entryPrice - candle.LowPrice) * TrailingStopPercent / 100m;
				_trailingShort = Math.Min(_trailingShort, newTrail);
			}

			var stop = UseTrailingStop ? Math.Min(_trailingShort, _shortStop) : _shortStop;
			if (candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
			}
			else
			{
				while (_tpCount < 7)
				{
					var target = _entryPrice * (1m - (_tpCount + 1) * 0.005m);
					if (candle.LowPrice <= target)
					{
						var part = _entryVolume / 7m;
						BuyMarket(Math.Min(part, Math.Abs(Position)));
						_tpCount++;
					}
					else
						break;
				}
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
