using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Cross strategy: PSAR crossover.
/// Buys when close crosses above PSAR. Sells when close crosses below PSAR.
/// </summary>
public class ParabolicSarCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ParabolicSarCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar { Acceleration = 0.02m, AccelerationStep = 0.02m, AccelerationMax = 0.2m };

		decimal? prevSar = null;
		decimal? prevClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, (candle, sarVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (prevSar.HasValue && prevClose.HasValue)
				{
					var crossUp = prevClose.Value <= prevSar.Value && close > sarVal;
					var crossDown = prevClose.Value >= prevSar.Value && close < sarVal;

					if (crossUp && Position <= 0)
						BuyMarket();
					else if (crossDown && Position >= 0)
						SellMarket();
				}

				prevSar = sarVal;
				prevClose = close;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}
}
