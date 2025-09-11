using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy using profit percentage steps.
/// </summary>
public class GridTendenceV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Profit percentage for grid step.
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
	/// Initialize strategy parameters.
	/// </summary>
	public GridTendenceV1Strategy()
	{
		_percent = Param(nameof(Percent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Percent", "Profit percentage for grid step", "Common")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

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

		var close = candle.ClosePrice;

		if (Position == 0)
		{
			BuyMarket();
			_entryPrice = close;
			return;
		}

		var profitPercent = Position > 0
			? (close - _entryPrice) / _entryPrice * 100m
			: (_entryPrice - close) / _entryPrice * 100m;

		if (Position > 0)
		{
			if (profitPercent >= Percent)
			{
				SellMarket(Math.Abs(Position));
				BuyMarket();
				_entryPrice = close;
			}
			else if (profitPercent <= -Percent)
			{
				SellMarket(Math.Abs(Position));
				SellMarket();
				_entryPrice = close;
			}
		}
		else
		{
			if (profitPercent >= Percent)
			{
				BuyMarket(Math.Abs(Position));
				SellMarket();
				_entryPrice = close;
			}
			else if (profitPercent <= -Percent)
			{
				BuyMarket(Math.Abs(Position));
				BuyMarket();
				_entryPrice = close;
			}
		}
	}
}
