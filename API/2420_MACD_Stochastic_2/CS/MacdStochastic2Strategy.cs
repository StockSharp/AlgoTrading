using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD and stochastic based swing strategy with pip-sized risk management.
/// </summary>
public class MacdStochastic2Strategy : Strategy
{
	private const decimal OversoldThreshold = 20m;
	private const decimal OverboughtThreshold = 80m;

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossBuyPips;
	private readonly StrategyParam<int> _stopLossSellPips;
	private readonly StrategyParam<int> _takeProfitBuyPips;
	private readonly StrategyParam<int> _takeProfitSellPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _longTrail;
	private decimal _longEntryPrice;

	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _shortTrail;
	private decimal _shortEntryPrice;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;

	private decimal _pipSize;

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Long stop-loss distance in pips.
	/// </summary>
	public int StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Short stop-loss distance in pips.
	/// </summary>
	public int StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Long take-profit distance in pips.
	/// </summary>
	public int TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Short take-profit distance in pips.
	/// </summary>
	public int TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum pip advance required to move the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal smoothing length.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic lookback period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K slowing period.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MacdStochastic2Strategy"/>.
	/// </summary>
	public MacdStochastic2Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default order volume", "Trading")
			.SetCanOptimize(true);

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 50)
			.SetDisplay("Stop Loss Buy (pips)", "Stop loss distance for long trades", "Risk")
			.SetCanOptimize(true);

		_stopLossSellPips = Param(nameof(StopLossSellPips), 50)
			.SetDisplay("Stop Loss Sell (pips)", "Stop loss distance for short trades", "Risk")
			.SetCanOptimize(true);

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 50)
			.SetDisplay("Take Profit Buy (pips)", "Take profit distance for long trades", "Risk")
			.SetCanOptimize(true);

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 50)
			.SetDisplay("Take Profit Sell (pips)", "Take profit distance for short trades", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance applied after profits", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimum advance to update the trailing stop", "Risk")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing length for MACD", "MACD")
			.SetCanOptimize(true);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Lookback period for %K", "Stochastic")
			.SetCanOptimize(true);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Smoothing period for %D", "Stochastic")
			.SetCanOptimize(true);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Slowing period applied to %K", "Stochastic")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for indicator calculations", "General");
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
		ResetRiskState();
		_macdPrev1 = null;
		_macdPrev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		Volume = TradeVolume;
		_pipSize = CalculatePipSize();

		// Initialize indicators that replicate the MetaTrader strategy.
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			return step * 10m;
		return step;
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histogramValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal)
		{
			UpdateMacdHistory(macdValue);
			return;
		}

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal percentK)
		{
			UpdateMacdHistory(macdValue);
			return;
		}

		// Manage existing trades before looking for new signals.
	ManagePositions(candle);

	if (IsFormedAndOnlineAndAllowTrading() && _macdPrev1.HasValue && _macdPrev2.HasValue)
	{
		var macd0 = macdValue;
		var macd1 = _macdPrev1.Value;
		var macd2 = _macdPrev2.Value;

		var longSignal = macd0 < 0m && macd1 < 0m && macd2 < 0m &&
			macd0 > macd1 && macd1 < macd2 &&
			percentK < OversoldThreshold;

		var shortSignal = macd0 > 0m && macd1 > 0m && macd2 > 0m &&
			macd0 < macd1 && macd1 > macd2 &&
			percentK > OverboughtThreshold;

		if (longSignal && Position <= 0)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (shortSignal && Position >= 0)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	UpdateMacdHistory(macdValue);
	}

	private void ManagePositions(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var newTrail = CalculateLongTrail(candle);
			var effectiveStop = CombineLongStops(newTrail);

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (effectiveStop.HasValue && candle.LowPrice <= effectiveStop.Value)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (newTrail.HasValue)
				_longTrail = _longTrail.HasValue ? Math.Max(_longTrail.Value, newTrail.Value) : newTrail;
		}
		else if (Position < 0)
		{
			var newTrail = CalculateShortTrail(candle);
			var effectiveStop = CombineShortStops(newTrail);

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}

			if (effectiveStop.HasValue && candle.HighPrice >= effectiveStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}

			if (newTrail.HasValue)
				_shortTrail = _shortTrail.HasValue ? Math.Min(_shortTrail.Value, newTrail.Value) : newTrail;
		}
	}

	private decimal? CalculateLongTrail(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _longEntryPrice <= 0m)
			return null;

		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
			return null;

		var move = candle.ClosePrice - _longEntryPrice;
		if (move <= distance)
			return null;

		var newStop = candle.ClosePrice - distance;
		var reference = _longTrail ?? _longStop;
		var step = TrailingStepPips * _pipSize;
		if (reference.HasValue && newStop - reference.Value < step)
			return null;

		return newStop;
	}

	private decimal? CalculateShortTrail(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _shortEntryPrice <= 0m)
			return null;

		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
			return null;

		var move = _shortEntryPrice - candle.ClosePrice;
		if (move <= distance)
			return null;

		var newStop = candle.ClosePrice + distance;
		var reference = _shortTrail ?? _shortStop;
		var step = TrailingStepPips * _pipSize;
		if (reference.HasValue && reference.Value - newStop < step)
			return null;

		return newStop;
	}

	private decimal? CombineLongStops(decimal? newTrail)
	{
		decimal? stop = _longStop;
		if (_longTrail.HasValue)
			stop = stop.HasValue ? Math.Max(stop.Value, _longTrail.Value) : _longTrail;
		if (newTrail.HasValue)
			stop = stop.HasValue ? Math.Max(stop.Value, newTrail.Value) : newTrail;
		return stop;
	}

	private decimal? CombineShortStops(decimal? newTrail)
	{
		decimal? stop = _shortStop;
		if (_shortTrail.HasValue)
			stop = stop.HasValue ? Math.Min(stop.Value, _shortTrail.Value) : _shortTrail;
		if (newTrail.HasValue)
			stop = stop.HasValue ? Math.Min(stop.Value, newTrail.Value) : newTrail;
		return stop;
	}

	private void EnterLong(decimal entryPrice)
	{
		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}

		if (Position != 0)
			return;

		BuyMarket(TradeVolume);
		_longEntryPrice = entryPrice;
		_longStop = StopLossBuyPips > 0 ? entryPrice - StopLossBuyPips * _pipSize : null;
		_longTake = TakeProfitBuyPips > 0 ? entryPrice + TakeProfitBuyPips * _pipSize : null;
		_longTrail = _longStop;
	}

	private void EnterShort(decimal entryPrice)
	{
		if (Position > 0)
		{
			SellMarket(Position);
			ResetLongState();
		}

		if (Position != 0)
			return;

		SellMarket(TradeVolume);
		_shortEntryPrice = entryPrice;
		_shortStop = StopLossSellPips > 0 ? entryPrice + StopLossSellPips * _pipSize : null;
		_shortTake = TakeProfitSellPips > 0 ? entryPrice - TakeProfitSellPips * _pipSize : null;
		_shortTrail = _shortStop;
	}

	private void UpdateMacdHistory(decimal macdValue)
	{
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdValue;
	}

	private void ResetRiskState()
	{
		ResetLongState();
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longStop = null;
		_longTake = null;
		_longTrail = null;
		_longEntryPrice = 0m;
	}

	private void ResetShortState()
	{
		_shortStop = null;
		_shortTake = null;
		_shortTrail = null;
		_shortEntryPrice = 0m;
	}
}
