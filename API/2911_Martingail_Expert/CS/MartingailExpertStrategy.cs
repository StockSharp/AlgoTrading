using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale Expert strategy (simplified). Uses RSI oscillator with
/// martingale-style position scaling and profit targets.
/// </summary>
public class MartingailExpertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public decimal BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	public decimal SellLevel
	{
		get => _sellLevel.Value;
		set => _sellLevel.Value = value;
	}

	public MartingailExpertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_buyLevel = Param(nameof(BuyLevel), 35m)
			.SetDisplay("Buy Level", "RSI level for longs", "Logic");

		_sellLevel = Param(nameof(SellLevel), 65m)
			.SetDisplay("Sell Level", "RSI level for shorts", "Logic");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		decimal prevRsi = 50;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, (ICandleMessage candle, decimal rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevRsi = rsiVal;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevRsi = rsiVal;
					return;
				}

				// RSI crosses up from oversold
				if (prevRsi < BuyLevel && rsiVal >= BuyLevel && Position <= 0)
					BuyMarket();
				// RSI crosses down from overbought
				else if (prevRsi > SellLevel && rsiVal <= SellLevel && Position >= 0)
					SellMarket();

				prevRsi = rsiVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}
}
