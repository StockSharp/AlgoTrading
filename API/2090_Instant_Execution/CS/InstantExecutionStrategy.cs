using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that instantly opens a position and manages it with
/// take profit, stop loss and trailing stop rules.
/// </summary>
public class InstantExecutionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStart;
	private readonly StrategyParam<decimal> _trailingSize;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Sides> _direction;

	private decimal _entryPrice;
	private decimal? _trailingStop;

	/// <summary>
	/// Target profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Minimum profit before trailing starts.
	/// </summary>
	public decimal TrailingStart
	{
		get => _trailingStart.Value;
		set => _trailingStart.Value = value;
	}

	/// <summary>
	/// Distance for trailing stop.
	/// </summary>
	public decimal TrailingSize
	{
		get => _trailingSize.Value;
		set => _trailingSize.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Direction of the initial order.
	/// </summary>
	public Sides Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public InstantExecutionStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 70m)
			.SetDisplay("Take Profit", "Target profit in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 10m);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_trailingStart = Param(nameof(TrailingStart), 5m)
			.SetDisplay("Trailing Start", "Minimum profit before trailing", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_trailingSize = Param(nameof(TrailingSize), 5m)
			.SetDisplay("Trailing Size", "Distance for trailing stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_direction = Param(nameof(Direction), Sides.Buy)
			.SetDisplay("Direction", "Initial order direction", "General");
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
		_entryPrice = 0m;
		_trailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			_entryPrice = candle.ClosePrice;
			if (Direction == Sides.Buy)
				BuyMarket(Volume);
			else
				SellMarket(Volume);
			return;
		}

		if (Position > 0)
			ProcessLong(candle);
		else if (Position < 0)
			ProcessShort(candle);
	}

	private void ProcessLong(ICandleMessage candle)
	{
		var price = candle.ClosePrice;

		if (TakeProfit > 0m && price - _entryPrice >= TakeProfit)
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_trailingStop = null;
			return;
		}

		if (StopLoss > 0m && _entryPrice - price >= StopLoss)
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_trailingStop = null;
			return;
		}

		if (TrailingStart <= 0m)
			return;

		if (_trailingStop is null)
		{
			if (price - _entryPrice >= TrailingStart)
				_trailingStop = price - TrailingSize;
			return;
		}

		var newStop = price - TrailingSize;
		if (newStop > _trailingStop)
			_trailingStop = newStop;

		if (price <= _trailingStop)
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_trailingStop = null;
		}
	}

	private void ProcessShort(ICandleMessage candle)
	{
		var price = candle.ClosePrice;

		if (TakeProfit > 0m && _entryPrice - price >= TakeProfit)
		{
			BuyMarket(-Position);
			_entryPrice = 0m;
			_trailingStop = null;
			return;
		}

		if (StopLoss > 0m && price - _entryPrice >= StopLoss)
		{
			BuyMarket(-Position);
			_entryPrice = 0m;
			_trailingStop = null;
			return;
		}

		if (TrailingStart <= 0m)
			return;

		if (_trailingStop is null)
		{
			if (_entryPrice - price >= TrailingStart)
				_trailingStop = price + TrailingSize;
			return;
		}

		var newStop = price + TrailingSize;
		if (newStop < _trailingStop)
			_trailingStop = newStop;

		if (price >= _trailingStop)
		{
			BuyMarket(-Position);
			_entryPrice = 0m;
			_trailingStop = null;
		}
	}
}
