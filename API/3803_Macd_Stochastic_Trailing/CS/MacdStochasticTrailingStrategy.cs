
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from MetaTrader that combines MACD and Stochastic filters
/// across multiple timeframes and manages trailing stops.
/// </summary>
public class MacdStochasticTrailingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longStopLoss;
	private readonly StrategyParam<decimal> _shortStopLoss;
	private readonly StrategyParam<decimal> _longTrailingStop;
	private readonly StrategyParam<decimal> _shortTrailingStop;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<DataType> _stochasticCandleType;
	private readonly StrategyParam<DataType> _entryCandleType;

	private MovingAverageConvergenceDivergence _bullishMacd;
	private MovingAverageConvergenceDivergence _bearishMacd;
	private StochasticOscillator _entryStochastic;
	private StochasticOscillator _exitStochastic;

	private decimal? _bullishMacdCurrent;
	private decimal? _bullishMacdPrevious;
	private decimal? _bearishMacdCurrent;
	private decimal? _bearishMacdPrevious;
	private decimal? _entryStochasticCurrent;
	private decimal? _entryStochasticPrevious;
	private decimal? _exitStochasticCurrent;
	private decimal? _m1PreviousHigh;
	private decimal? _m1PreviousLow;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdStochasticTrailingStrategy"/> class.
	/// </summary>
	public MacdStochasticTrailingStrategy()
	{
		_longStopLoss = Param(nameof(LongStopLoss), 17m)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss", "Distance for the long stop loss in points", "Risk")
			.SetCanOptimize(true);

		_shortStopLoss = Param(nameof(ShortStopLoss), 40m)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss", "Distance for the short stop loss in points", "Risk")
			.SetCanOptimize(true);

		_longTrailingStop = Param(nameof(LongTrailingStop), 88m)
			.SetNotNegative()
			.SetDisplay("Long Trailing", "Trailing distance for long positions in points", "Risk")
			.SetCanOptimize(true);

		_shortTrailingStop = Param(nameof(ShortTrailingStop), 76m)
			.SetNotNegative()
			.SetDisplay("Short Trailing", "Trailing distance for short positions in points", "Risk")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Order Volume", "Base volume used when opening positions", "Trading")
			.SetCanOptimize(true);

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("MACD Candles", "Timeframe used to calculate MACD filters", "Data");

		_stochasticCandleType = Param(nameof(StochasticCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Stochastic Candles", "Timeframe used for Stochastic oscillators", "Data");

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Entry Candles", "Timeframe that drives price confirmation and trailing", "Data");
	}

	/// <summary>
	/// Distance for the long stop loss in points.
	/// </summary>
	public decimal LongStopLoss
	{
		get => _longStopLoss.Value;
		set => _longStopLoss.Value = value;
	}

	/// <summary>
	/// Distance for the short stop loss in points.
	/// </summary>
	public decimal ShortStopLoss
	{
		get => _shortStopLoss.Value;
		set => _shortStopLoss.Value = value;
	}

	/// <summary>
	/// Trailing distance used for long positions.
	/// </summary>
	public decimal LongTrailingStop
	{
		get => _longTrailingStop.Value;
		set => _longTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing distance used for short positions.
	/// </summary>
	public decimal ShortTrailingStop
	{
		get => _shortTrailingStop.Value;
		set => _shortTrailingStop.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for both MACD indicators (default H1).
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used by the Stochastic oscillators (default M15).
	/// </summary>
	public DataType StochasticCandleType
	{
		get => _stochasticCandleType.Value;
		set => _stochasticCandleType.Value = value;
	}

	/// <summary>
	/// Candle type that provides price confirmation (default M1).
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, MacdCandleType),
			(Security, StochasticCandleType),
			(Security, EntryCandleType),
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bullishMacdCurrent = null;
		_bullishMacdPrevious = null;
		_bearishMacdCurrent = null;
		_bearishMacdPrevious = null;
		_entryStochasticCurrent = null;
		_entryStochasticPrevious = null;
		_exitStochasticCurrent = null;
		_m1PreviousHigh = null;
		_m1PreviousLow = null;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		// Configure indicators that emulate the MetaTrader setup.
		_bullishMacd = new MovingAverageConvergenceDivergence
		{
			FastLength = 22,
			SlowLength = 27,
			SignalLength = 9
		};

		_bearishMacd = new MovingAverageConvergenceDivergence
		{
			FastLength = 19,
			SlowLength = 77,
			SignalLength = 9
		};

		_entryStochastic = new StochasticOscillator
		{
			Length = 5,
			K = { Length = 11 },
			D = { Length = 3 }
		};

		_exitStochastic = new StochasticOscillator
		{
			Length = 9,
			K = { Length = 19 },
			D = { Length = 3 }
		};

		// Subscribe to hourly candles for MACD comparisons.
		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.Bind(_bullishMacd, ProcessBullishMacdValues)
			.Bind(_bearishMacd, ProcessBearishMacdValues)
			.Start();

		// Subscribe to 15-minute candles for Stochastic filters.
		var stochasticSubscription = SubscribeCandles(StochasticCandleType);
		stochasticSubscription
			.BindEx(_entryStochastic, ProcessEntryStochastic)
			.BindEx(_exitStochastic, ProcessExitStochastic)
			.Start();

		// Subscribe to 1-minute candles for price confirmation and trailing logic.
		var entrySubscription = SubscribeCandles(EntryCandleType);
		entrySubscription
			.Bind(ProcessEntryCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawIndicator(area, _bullishMacd);
			DrawIndicator(area, _bearishMacd);
			DrawIndicator(area, _entryStochastic);
			DrawIndicator(area, _exitStochastic);
			DrawOwnTrades(area);
		}
	}
	private void ProcessBullishMacdValues(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bullishMacdPrevious = _bullishMacdCurrent;
		_bullishMacdCurrent = macdValue;
	}

	private void ProcessBearishMacdValues(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bearishMacdPrevious = _bearishMacdCurrent;
		_bearishMacdCurrent = macdValue;
	}

	private void ProcessEntryStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is not decimal k)
			return;

		_entryStochasticPrevious = _entryStochasticCurrent;
		_entryStochasticCurrent = k;
	}

	private void ProcessExitStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is not decimal k)
			return;

		_exitStochasticCurrent = k;
	}

	private void ProcessEntryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage open positions before checking for new entries.
		if (UpdateStopsAndMaybeExit(candle))
		{
			_m1PreviousHigh = candle.HighPrice;
			_m1PreviousLow = candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_m1PreviousHigh = candle.HighPrice;
			_m1PreviousLow = candle.LowPrice;
			return;
		}

		if (Position == 0)
		{
			var opened = TryOpenLong(candle) || TryOpenShort(candle);
			if (opened)
			{
				_m1PreviousHigh = candle.HighPrice;
				_m1PreviousLow = candle.LowPrice;
				return;
			}
		}

		_m1PreviousHigh = candle.HighPrice;
		_m1PreviousLow = candle.LowPrice;
	}

	private bool TryOpenLong(ICandleMessage candle)
	{
		if (_bullishMacdCurrent is not decimal macd || _bullishMacdPrevious is not decimal macdPrev)
			return false;

		if (_entryStochasticCurrent is not decimal stoch || _entryStochasticPrevious is not decimal stochPrev)
			return false;

		if (_m1PreviousHigh is not decimal prevHigh)
			return false;

		var bullishMacdRising = macd > macdPrev;
		var macdBelowZero = macd < 0m;
		var stochasticOversold = stoch < 26m;
		var stochasticAscending = stoch > stochPrev;
		var priceBreakout = candle.ClosePrice > prevHigh;

		if (bullishMacdRising && macdBelowZero && stochasticOversold && stochasticAscending && priceBreakout)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_longEntryPrice = candle.ClosePrice;
			_longStopPrice = LongStopLoss > 0m ? candle.ClosePrice - GetPointValue(LongStopLoss) : null;
			ResetShortState();
			return true;
		}

		return false;
	}

	private bool TryOpenShort(ICandleMessage candle)
	{
		if (_bearishMacdCurrent is not decimal macd || _bearishMacdPrevious is not decimal macdPrev)
			return false;

		if (_exitStochasticCurrent is not decimal stoch)
			return false;

		if (_m1PreviousLow is not decimal prevLow)
			return false;

		var bearishMacdFalling = macd < macdPrev;
		var macdAboveZeroPreviously = macdPrev > 0m;
		var stochasticOverbought = stoch > 70m;
		var priceBreakdown = candle.ClosePrice < prevLow;

		if (bearishMacdFalling && macdAboveZeroPreviously && stochasticOverbought && priceBreakdown)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_shortEntryPrice = candle.ClosePrice;
			_shortStopPrice = ShortStopLoss > 0m ? candle.ClosePrice + GetPointValue(ShortStopLoss) : null;
			ResetLongState();
			return true;
		}

		return false;
	}

	private bool UpdateStopsAndMaybeExit(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longEntryPrice is not decimal entry)
			{
				ResetLongState();
				return false;
			}

			var trailDistance = LongTrailingStop > 0m ? GetPointValue(LongTrailingStop) : 0m;
			if (trailDistance > 0m)
			{
				var candidate = candle.ClosePrice - trailDistance;
				if (candle.ClosePrice - entry > trailDistance && (_longStopPrice is null || candidate > _longStopPrice))
				{
					_longStopPrice = candidate;
				}
			}

			var stopLevel = _longStopPrice ?? (LongStopLoss > 0m ? entry - GetPointValue(LongStopLoss) : (decimal?)null);
			if (stopLevel.HasValue && candle.LowPrice <= stopLevel.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_shortEntryPrice is not decimal entry)
			{
				ResetShortState();
				return false;
			}

			var trailDistance = ShortTrailingStop > 0m ? GetPointValue(ShortTrailingStop) : 0m;
			if (trailDistance > 0m)
			{
				var candidate = candle.ClosePrice + trailDistance;
				if (entry - candle.ClosePrice > trailDistance && (_shortStopPrice is null || candidate < _shortStopPrice))
				{
					_shortStopPrice = candidate;
				}
			}

			var stopLevel = _shortStopPrice ?? (ShortStopLoss > 0m ? entry + GetPointValue(ShortStopLoss) : (decimal?)null);
			if (stopLevel.HasValue && candle.HighPrice >= stopLevel.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		return false;
	}

	private decimal GetPointValue(decimal points)
	{
		var step = Security?.PriceStep ?? 0.0001m;
		return points * step;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
	}
}
