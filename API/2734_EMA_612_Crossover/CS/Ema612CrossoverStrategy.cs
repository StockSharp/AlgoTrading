using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA 6/12 crossover strategy with trailing stop management.
/// </summary>
public class Ema612CrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<decimal> _trailingStopOffset;
	private readonly StrategyParam<decimal> _trailingStepOffset;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in absolute price units.
	/// </summary>
	public decimal TrailingStopOffset
	{
		get => _trailingStopOffset.Value;
		set => _trailingStopOffset.Value = value;
	}

	/// <summary>
	/// Additional distance required to move the trailing stop.
	/// </summary>
	public decimal TrailingStepOffset
	{
		get => _trailingStepOffset.Value;
		set => _trailingStepOffset.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Ema612CrossoverStrategy"/>.
	/// </summary>
	public Ema612CrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle resolution", "General");
		_fastPeriod = Param(nameof(FastPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA length", "Moving Averages");
		_slowPeriod = Param(nameof(SlowPeriod), 54)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA length", "Moving Averages");
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0.001m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Target distance in price units", "Risk");
		_trailingStopOffset = Param(nameof(TrailingStopOffset), 0.005m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");
		_trailingStepOffset = Param(nameof(TrailingStepOffset), 0.0005m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Additional profit required to tighten stop", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetPositionState();
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SlowPeriod <= FastPeriod)
			throw new InvalidOperationException("Slow period must be greater than fast period.");

		_fastSma = new SimpleMovingAverage { Length = FastPeriod };
		_slowSma = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastSma.IsFormed || !_slowSma.IsFormed)
			return;

		var bullishCross = false;
		var bearishCross = false;

		if (_prevFast.HasValue && _prevSlow.HasValue)
		{
			// Detect crossovers using previous candle values.
			bullishCross = _prevSlow > _prevFast && slow < fast;
			bearishCross = _prevSlow < _prevFast && slow > fast;
		}

		HandleExistingPosition(candle, bullishCross, bearishCross);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (Position == 0)
		{
			if (bullishCross)
			{
				// Slow MA crossed below the fast MA - go long.
				EnterLong(candle);
			}
			else if (bearishCross)
			{
				// Slow MA crossed above the fast MA - go short.
				EnterShort(candle);
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private void HandleExistingPosition(ICandleMessage candle, bool bullishCross, bool bearishCross)
	{
		if (Position > 0)
		{
			// Update trailing stop for the long position before evaluating exits.
			UpdateLongTrailing(candle);

			var exit = bearishCross;
			if (!exit && _takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				// Price reached the take profit objective.
				exit = true;
			}

			if (!exit && _stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				// Price retraced to the trailing stop.
				exit = true;
			}

			if (exit)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			// Update trailing stop for the short position before evaluating exits.
			UpdateShortTrailing(candle);

			var exit = bullishCross;
			if (!exit && _takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				// Price reached the take profit objective for the short trade.
				exit = true;
			}

			if (!exit && _stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				// Price rallied back to the trailing stop level.
				exit = true;
			}

			if (exit)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_takeProfitPrice = TakeProfitOffset > 0m ? candle.ClosePrice + TakeProfitOffset : null;
		_stopPrice = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_takeProfitPrice = TakeProfitOffset > 0m ? candle.ClosePrice - TakeProfitOffset : null;
		_stopPrice = null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (TrailingStopOffset <= 0m || !_entryPrice.HasValue)
			return;

		var gain = candle.ClosePrice - _entryPrice.Value;
		var triggerDistance = TrailingStopOffset + TrailingStepOffset;

		if (gain <= triggerDistance)
			return;

		var candidate = candle.ClosePrice - TrailingStopOffset;
		var minAdvance = TrailingStepOffset <= 0m ? 0m : TrailingStepOffset;

		if (!_stopPrice.HasValue || candidate - _stopPrice.Value > minAdvance)
		{
			// Move stop loss closer only when price progressed enough.
			_stopPrice = candidate;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (TrailingStopOffset <= 0m || !_entryPrice.HasValue)
			return;

		var gain = _entryPrice.Value - candle.ClosePrice;
		var triggerDistance = TrailingStopOffset + TrailingStepOffset;

		if (gain <= triggerDistance)
			return;

		var candidate = candle.ClosePrice + TrailingStopOffset;
		var minAdvance = TrailingStepOffset <= 0m ? 0m : TrailingStepOffset;

		if (!_stopPrice.HasValue || _stopPrice.Value - candidate > minAdvance)
		{
			// Move stop loss for the short only after sufficient favorable movement.
			_stopPrice = candidate;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}
