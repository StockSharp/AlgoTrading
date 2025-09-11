using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ChartPatternsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private decimal _prevHigh;
	private decimal _prevLow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ChartPatternsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0m;
		_prevLow = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		var initialized = false;

		subscription
			.Bind(candle =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!initialized)
				{
					_prevHigh = candle.HighPrice;
					_prevLow = candle.LowPrice;
					initialized = true;
					return;
				}

				var isAscending = candle.HighPrice > _prevHigh && candle.LowPrice > _prevLow;
				var isDescending = candle.HighPrice < _prevHigh && candle.LowPrice < _prevLow;

				if (isAscending && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (isDescending && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
				}

				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
