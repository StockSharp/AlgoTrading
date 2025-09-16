using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomly buys or sells when no position is open.
/// Applies fixed take profit and stop loss.
/// </summary>
public class RandomTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private Random _random;
	private decimal _entryPrice;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
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
	/// Initializes a new instance of <see cref="RandomTraderStrategy"/>.
	/// </summary>
	public RandomTraderStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_random = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_random = new Random();

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			// No position - randomly choose direction
			if (_random.NextDouble() > 0.5)
			{
				BuyMarket(Volume);
			}
			else
			{
				SellMarket(Volume);
			}

			_entryPrice = price;
			return;
		}

		if (Position > 0)
		{
			// Long position exit logic
			if (price >= _entryPrice + TakeProfit || price <= _entryPrice - StopLoss)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			// Short position exit logic
			if (price <= _entryPrice - TakeProfit || price >= _entryPrice + StopLoss)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
			}
		}
	}
}
