using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on price crossing a moving average with StdDev-based bounds.
/// </summary>
public class MultiRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal RiskMultiplier { get => _riskMultiplier.Value; set => _riskMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiRegressionStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "SMA and StdDev period", "Regression");
		_riskMultiplier = Param(nameof(RiskMultiplier), 2m)
			.SetDisplay("Risk Multiplier", "StdDev multiplier for bounds", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Common");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var prevClose = 0m;
		var prevSma = 0m;
		var initialized = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, std, (candle, smaVal, stdVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var price = candle.ClosePrice;

				if (!initialized)
				{
					prevClose = price;
					prevSma = smaVal;
					initialized = true;
					return;
				}

				var upperBound = smaVal + stdVal * RiskMultiplier;
				var lowerBound = smaVal - stdVal * RiskMultiplier;

				// Cross above SMA => buy
				if (prevClose <= prevSma && price > smaVal && Position <= 0)
					BuyMarket();
				// Cross below SMA => sell if long
				else if (prevClose >= prevSma && price < smaVal && Position > 0)
					SellMarket();

				// Exit at bounds
				if (Position > 0 && price >= upperBound)
					SellMarket();

				prevClose = price;
				prevSma = smaVal;
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
