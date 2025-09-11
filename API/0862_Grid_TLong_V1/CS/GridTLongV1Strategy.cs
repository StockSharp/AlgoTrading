using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid TLong V1 strategy.
/// </summary>
public class GridTLongV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<bool> _useLimitOrders;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Grid step in percent.
	/// </summary>
	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	/// <summary>
	/// Use limit orders instead of market orders.
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
	/// Initialize strategy parameters.
	/// </summary>
	public GridTLongV1Strategy()
	{
		_percent = Param(nameof(Percent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Percent", "Grid step in percent", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_useLimitOrders = Param(nameof(UseLimitOrders), false)
			.SetDisplay("Use Limit Orders", "Open positions with limit orders", "General");

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
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		if (Position == 0)
		{
			OpenLong(price);
			return;
		}

		var profitPercent = Position > 0
			? (price - _entryPrice) / _entryPrice * 100m
			: (_entryPrice - price) / _entryPrice * 100m;

		if (Position > 0)
		{
			if (profitPercent >= Percent)
			{
				ClosePosition();
				OpenLong(price);
			}
			else if (profitPercent <= -Percent)
			{
				ClosePosition();
				OpenShort(price);
			}
		}
		else
		{
			if (profitPercent >= Percent)
			{
				ClosePosition();
				OpenShort(price);
			}
			else if (profitPercent <= -Percent)
			{
				ClosePosition();
				OpenLong(price);
			}
		}
	}

	private void OpenLong(decimal price)
	{
		if (UseLimitOrders)
			BuyLimit(price);
		else
			BuyMarket();

		_entryPrice = price;
	}

	private void OpenShort(decimal price)
	{
		if (UseLimitOrders)
			SellLimit(price);
		else
			SellMarket();

		_entryPrice = price;
	}
}
