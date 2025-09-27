using System;
using System.Collections.Generic;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Cleaner strategy converted from MetaTrader 5 implementation.
/// The logic opens trades when the MACD main line rises or falls during three consecutive closed candles.
/// </summary>
public class MacdCleanerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;

	private decimal _pipSize;
	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _longHighest;
	private decimal _shortLowest;
	private decimal? _longTrailingLevel;
	private decimal? _shortTrailingLevel;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Initializes a new instance of <see cref="MacdCleanerStrategy"/>.
	/// </summary>
	public MacdCleanerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for MACD evaluation", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order size used for entries", "Trading")
			.SetGreaterThan(0m);

		_stopLossPips = Param(nameof(StopLossPips), 35m)
			.SetDisplay("Stop Loss (pips)", "Distance to the protective stop", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
			.SetDisplay("Take Profit (pips)", "Distance to the profit target", "Risk")
			.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetNotNegative();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Additional distance required before trailing moves", "Risk")
			.SetNotNegative();

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 15)
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
			.SetGreaterThan(0);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 33)
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
			.SetGreaterThan(0);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 11)
			.SetDisplay("MACD Signal", "Signal EMA length", "Indicators")
			.SetGreaterThan(0);
	}

	/// <summary>
	/// Primary candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order volume submitted with new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Fast moving average length used in the MACD calculation.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length used in the MACD calculation.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal moving average length used in the MACD calculation.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
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

		ResetState();

		var step = Security?.PriceStep ?? 0m;
		var decimals = Security?.Decimals ?? 0;
		_pipSize = step;
		if (decimals == 3 || decimals == 5)
		{
			_pipSize *= 10m;
		}

		if (_pipSize <= 0m)
		{
			_pipSize = step > 0m ? step : 1m;
		}

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var exitTriggered = ManagePosition(candle);

		UpdateMacdSeries(macd);

		if (exitTriggered)
			return;

		if (_macdPrev3 is not decimal older || _macdPrev2 is not decimal previous || _macdPrev1 is not decimal current)
			return;

		if (older <= previous && previous <= current)
		{
			TryOpenLong(candle);
		}
		else if (older >= previous && previous >= current)
		{
			TryOpenShort(candle);
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		var exitTriggered = false;

		if (Position > 0m)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return false;

			var entryPrice = Position.AveragePrice ?? _longEntryPrice ?? candle.ClosePrice;
			_longEntryPrice ??= entryPrice;

			if (_longHighest <= 0m)
				_longHighest = entryPrice;

			if (_longExitRequested)
				return true;

			var trailingDistance = ToPipPrice(TrailingStopPips);
			var trailingStep = ToPipPrice(TrailingStepPips);

			if (candle.HighPrice > _longHighest)
			{
				var progress = candle.HighPrice - _longHighest;
				_longHighest = candle.HighPrice;

				if (trailingDistance > 0m && (trailingStep <= 0m || progress >= trailingStep))
				{
					var candidate = candle.HighPrice - trailingDistance;
					if (!_longTrailingLevel.HasValue || candidate > _longTrailingLevel.Value)
						_longTrailingLevel = candidate;
				}
			}

			if (trailingDistance > 0m && _longTrailingLevel is decimal trailing && candle.LowPrice <= trailing)
			{
				SellMarket(volume);
				_longExitRequested = true;
				exitTriggered = true;
			}

			var stopLossDistance = ToPipPrice(StopLossPips);
			if (!exitTriggered && stopLossDistance > 0m && candle.LowPrice <= entryPrice - stopLossDistance)
			{
				SellMarket(volume);
				_longExitRequested = true;
				exitTriggered = true;
			}

			var takeProfitDistance = ToPipPrice(TakeProfitPips);
			if (!exitTriggered && takeProfitDistance > 0m && candle.HighPrice >= entryPrice + takeProfitDistance)
			{
				SellMarket(volume);
				_longExitRequested = true;
				exitTriggered = true;
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return false;

			var entryPrice = Position.AveragePrice ?? _shortEntryPrice ?? candle.ClosePrice;
			_shortEntryPrice ??= entryPrice;

			if (_shortLowest <= 0m || _shortLowest > entryPrice)
				_shortLowest = entryPrice;

			if (_shortExitRequested)
				return true;

			var trailingDistance = ToPipPrice(TrailingStopPips);
			var trailingStep = ToPipPrice(TrailingStepPips);

			if (candle.LowPrice < _shortLowest)
			{
				var progress = _shortLowest - candle.LowPrice;
				_shortLowest = candle.LowPrice;

				if (trailingDistance > 0m && (trailingStep <= 0m || progress >= trailingStep))
				{
					var candidate = candle.LowPrice + trailingDistance;
					if (!_shortTrailingLevel.HasValue || candidate < _shortTrailingLevel.Value)
						_shortTrailingLevel = candidate;
				}
			}

			if (trailingDistance > 0m && _shortTrailingLevel is decimal trailing && candle.HighPrice >= trailing)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				exitTriggered = true;
			}

			var stopLossDistance = ToPipPrice(StopLossPips);
			if (!exitTriggered && stopLossDistance > 0m && candle.HighPrice >= entryPrice + stopLossDistance)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				exitTriggered = true;
			}

			var takeProfitDistance = ToPipPrice(TakeProfitPips);
			if (!exitTriggered && takeProfitDistance > 0m && candle.LowPrice <= entryPrice - takeProfitDistance)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				exitTriggered = true;
			}
		}
		else
		{
			ResetPositionState();
		}

		return exitTriggered;
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = TradeVolume;
		if (Position < 0m)
			volume += Math.Abs(Position);
		else if (Position > 0m)
			return;

		if (volume <= 0m)
			return;

		_longEntryPrice = candle.ClosePrice;
		_longHighest = Math.Max(_longEntryPrice.Value, candle.HighPrice);
		_longTrailingLevel = null;
		_longExitRequested = false;
		_shortExitRequested = false;

		BuyMarket(volume);
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = TradeVolume;
		if (Position > 0m)
			volume += Math.Abs(Position);
		else if (Position < 0m)
			return;

		if (volume <= 0m)
			return;

		_shortEntryPrice = candle.ClosePrice;
		_shortLowest = Math.Min(_shortEntryPrice.Value, candle.LowPrice);
		_shortTrailingLevel = null;
		_shortExitRequested = false;
		_longExitRequested = false;

		SellMarket(volume);
	}

	private void UpdateMacdSeries(decimal value)
	{
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = value;
	}

	private decimal ToPipPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var size = _pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m;
		return size > 0m ? pips * size : 0m;
	}

	private void ResetState()
	{
		_macdPrev1 = null;
		_macdPrev2 = null;
		_macdPrev3 = null;
		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longHighest = 0m;
		_shortLowest = 0m;
		_longTrailingLevel = null;
		_shortTrailingLevel = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}
}
