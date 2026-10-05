using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid TLong V1 strategy.
/// Always keeps a position, starting long. Once the position gains Percent percent from its entry price it is closed and restarted
/// in the same direction; once it loses Percent percent it is reversed. With UseLimitOrders the orders are limit orders at the
/// candle close instead of market orders.
/// </summary>
public class GridTLongV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<bool> _useLimitOrders;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Grid step in percent of the entry price.
	/// </summary>
	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	/// <summary>
	/// Use limit orders at the candle close instead of market orders.
	/// </summary>
	public bool UseLimitOrders
	{
		get => _useLimitOrders.Value;
		set => _useLimitOrders.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GridTLongV1Strategy()
	{
		_percent = Param(nameof(Percent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Percent", "Grid step in percent of the entry price", "Trading");

		_useLimitOrders = Param(nameof(UseLimitOrders), false)
			.SetDisplay("Use Limit Orders", "Use limit orders at the candle close instead of market orders", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void Buy(decimal volume, decimal price)
	{
		if (UseLimitOrders)
			BuyLimit(price, volume);
		else
			BuyMarket(volume);
	}

	private void Sell(decimal volume, decimal price)
	{
		if (UseLimitOrders)
			SellLimit(price, volume);
		else
			SellMarket(volume);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Unfilled limit orders from the previous bar are replaced by the current decision.
		if (UseLimitOrders)
			CancelActiveOrders();

		if (Position == 0)
		{
			Buy(Volume, close);
			_entryPrice = close;
			return;
		}

		if (_entryPrice <= 0m)
		{
			_entryPrice = close;
			return;
		}

		var change = (close - _entryPrice) / _entryPrice * 100m;
		var profit = Position > 0 ? change : -change;

		if (profit >= Percent)
		{
			// Restart the position in the same direction at the new grid level.
			if (Position > 0)
			{
				Sell(Position, close);
				Buy(Volume, close);
			}
			else
			{
				Buy(-Position, close);
				Sell(Volume, close);
			}

			_entryPrice = close;
		}
		else if (profit <= -Percent)
		{
			if (Position > 0)
				Sell(Volume + Math.Abs(Position), close);
			else
				Buy(Volume + Math.Abs(Position), close);

			_entryPrice = close;
		}
	}
}
