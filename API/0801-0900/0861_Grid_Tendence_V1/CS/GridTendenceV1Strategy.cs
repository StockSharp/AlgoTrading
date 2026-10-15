using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid Tendence V1 strategy.
/// Always in the market, starting long. When the open position gains Percent percent from its entry price it is closed and
/// reopened in the same direction; when it loses Percent percent it is closed and a position in the opposite direction is opened.
/// </summary>
public class GridTendenceV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Profit or loss percent that triggers a reopen or a reversal.
	/// </summary>
	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
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
	public GridTendenceV1Strategy()
	{
		_percent = Param(nameof(Percent), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Percent", "Profit or loss percent that reopens or reverses the position", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position == 0)
		{
			// The first entry is always long.
			BuyMarket(Volume);
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
			if (Position > 0)
			{
				SellMarket(Position);
				BuyMarket(Volume);
			}
			else
			{
				BuyMarket(-Position);
				SellMarket(Volume);
			}

			_entryPrice = close;
		}
		else if (profit <= -Percent)
		{
			if (Position > 0)
				SellMarket(Volume + Math.Abs(Position));
			else
				BuyMarket(Volume + Math.Abs(Position));

			_entryPrice = close;
		}
	}
}
