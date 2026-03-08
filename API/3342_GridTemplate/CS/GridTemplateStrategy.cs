namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Grid Template strategy: places trades at regular grid intervals.
/// Buys when price drops by grid step, sells when price rises by grid step.
/// </summary>
public class GridTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _gridStepPercent;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal GridStepPercent
	{
		get => _gridStepPercent.Value;
		set => _gridStepPercent.Value = value;
	}

	public GridTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_gridStepPercent = Param(nameof(GridStepPercent), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step %", "Price change percentage for grid level", "Grid");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };

		decimal? lastTradePrice = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (!lastTradePrice.HasValue)
				{
					lastTradePrice = close;
					return;
				}

				var step = lastTradePrice.Value * GridStepPercent / 100m;

				if (close <= lastTradePrice.Value - step)
				{
					BuyMarket();
					lastTradePrice = close;
				}
				else if (close >= lastTradePrice.Value + step)
				{
					SellMarket();
					lastTradePrice = close;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
