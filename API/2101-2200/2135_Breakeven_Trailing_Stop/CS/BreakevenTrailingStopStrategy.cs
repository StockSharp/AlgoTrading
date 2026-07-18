using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that moves stop-loss to breakeven and then trails it as price advances.
/// Uses EMA crossover for entry signals with breakeven + trailing stop management.
/// </summary>
public class BreakevenTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _breakevenPercent;
	private readonly StrategyParam<decimal> _breakevenOffset;
	private readonly StrategyParam<decimal> _trailingActivation;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _breakevenReached;

	/// <summary>
	/// Profit percent before moving stop to breakeven.
	/// </summary>
	public decimal BreakevenPercent
	{
		get => _breakevenPercent.Value;
		set => _breakevenPercent.Value = value;
	}

	/// <summary>
	/// Stop offset percent above entry after breakeven activation.
	/// </summary>
	public decimal BreakevenOffset
	{
		get => _breakevenOffset.Value;
		set => _breakevenOffset.Value = value;
	}

	/// <summary>
	/// Profit percent above breakeven stop required before trailing starts.
	/// </summary>
	public decimal TrailingActivation
	{
		get => _trailingActivation.Value;
		set => _trailingActivation.Value = value;
	}

	/// <summary>
	/// Distance percent between current price and trailing stop.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Fast EMA period for entry signals.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for entry signals.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for price updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BreakevenTrailingStopStrategy()
	{
		_breakevenPercent = Param(nameof(BreakevenPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven %", "Profit percent before breakeven", "Trading");

		_breakevenOffset = Param(nameof(BreakevenOffset), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Offset", "Stop offset percent after breakeven", "Trading");

		_trailingActivation = Param(nameof(TrailingActivation), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Activation", "Profit percent above stop before trailing", "Trading");

		_trailingDistance = Param(nameof(TrailingDistance), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Distance", "Percent from price to trailing stop", "Trading");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for updates", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (Position == 0)
		{
			_breakevenReached = false;

			if (fastEma > slowEma)
			{
				_entryPrice = close;
				_stopPrice = close * (1m - 1m / 100m);
				BuyMarket();
			}
			else if (fastEma < slowEma)
			{
				_entryPrice = close;
				_stopPrice = close * (1m + 1m / 100m);
				SellMarket();
			}

			return;
		}

		if (Position > 0)
		{
			if (!_breakevenReached)
			{
				if (close >= _entryPrice * (1m + BreakevenPercent / 100m))
				{
					_stopPrice = _entryPrice * (1m + BreakevenOffset / 100m);
					_breakevenReached = true;
				}
			}
			else
			{
				var trailingStop = close * (1m - TrailingDistance / 100m);
				if (close >= _stopPrice * (1m + TrailingActivation / 100m) && trailingStop > _stopPrice)
				{
					_stopPrice = trailingStop;
				}
			}

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
			}
			else if (fastEma < slowEma)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (!_breakevenReached)
			{
				if (close <= _entryPrice * (1m - BreakevenPercent / 100m))
				{
					_stopPrice = _entryPrice * (1m - BreakevenOffset / 100m);
					_breakevenReached = true;
				}
			}
			else
			{
				var trailingStop = close * (1m + TrailingDistance / 100m);
				if (close <= _stopPrice * (1m - TrailingActivation / 100m) && trailingStop < _stopPrice)
				{
					_stopPrice = trailingStop;
				}
			}

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
			}
			else if (fastEma > slowEma)
			{
				BuyMarket();
			}
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_breakevenReached = false;
	}
}
