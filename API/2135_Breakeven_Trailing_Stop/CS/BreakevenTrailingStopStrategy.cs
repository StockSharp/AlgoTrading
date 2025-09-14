using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that moves stop-loss to breakeven and then trails it as price advances.
/// </summary>
public class BreakevenTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _breakevenPlus;
	private readonly StrategyParam<decimal> _breakevenStep;
	private readonly StrategyParam<decimal> _trailingPlus;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _breakevenReached;

	/// <summary>
	/// Profit distance in points before moving stop to breakeven.
	/// </summary>
	public decimal BreakevenPlus
	{
		get => _breakevenPlus.Value;
		set => _breakevenPlus.Value = value;
	}

	/// <summary>
	/// Stop offset after breakeven activation.
	/// </summary>
	public decimal BreakevenStep
	{
		get => _breakevenStep.Value;
		set => _breakevenStep.Value = value;
	}

	/// <summary>
	/// Profit distance in points required before trailing.
	/// </summary>
	public decimal TrailingPlus
	{
		get => _trailingPlus.Value;
		set => _trailingPlus.Value = value;
	}

	/// <summary>
	/// Distance between current price and trailing stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
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
		_breakevenPlus = Param(nameof(BreakevenPlus), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Plus", "Profit in points before breakeven", "Trading");

		_breakevenStep = Param(nameof(BreakevenStep), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Step", "Stop offset after breakeven", "Trading");

		_trailingPlus = Param(nameof(TrailingPlus), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Plus", "Profit above stop before trailing", "Trading");

		_trailingStep = Param(nameof(TrailingStep), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Distance from price to stop", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for updates", "General");
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position == 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice;
			_breakevenReached = false;
			BuyMarket();
			return;
		}

		if (Position > 0)
		{
			if (!_breakevenReached)
			{
				if (candle.ClosePrice >= _entryPrice + BreakevenPlus * step)
				{
					_stopPrice = _entryPrice + BreakevenStep * step;
					_breakevenReached = true;
				}
			}
			else
			{
				if (candle.ClosePrice >= _stopPrice + TrailingPlus * step)
				{
					var newStop = candle.ClosePrice - TrailingStep * step;
					if (newStop > _stopPrice)
						_stopPrice = newStop;
				}
			}

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (!_breakevenReached)
			{
				if (candle.ClosePrice <= _entryPrice - BreakevenPlus * step)
				{
					_stopPrice = _entryPrice - BreakevenStep * step;
					_breakevenReached = true;
				}
			}
			else
			{
				if (candle.ClosePrice <= _stopPrice - TrailingPlus * step)
				{
					var newStop = candle.ClosePrice + TrailingStep * step;
					if (newStop < _stopPrice)
						_stopPrice = newStop;
				}
			}

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
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
