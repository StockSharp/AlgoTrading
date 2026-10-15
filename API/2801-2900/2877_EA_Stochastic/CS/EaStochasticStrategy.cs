using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EA Stochastic strategy (simplified). Uses RSI as oscillator proxy.
/// </summary>
public class EaStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;

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

	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	public EaStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_upperLevel = Param(nameof(UpperLevel), 70m)
			.SetDisplay("Upper Level", "Overbought level", "Levels");

		_lowerLevel = Param(nameof(LowerLevel), 30m)
			.SetDisplay("Lower Level", "Oversold level", "Levels");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		decimal prevRsi = 50;
		bool hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, (ICandleMessage candle, decimal rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevRsi = rsiValue;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevRsi = rsiValue;
					return;
				}

				// Cross up from oversold
				if (prevRsi < LowerLevel && rsiValue >= LowerLevel && Position <= 0)
				{
					BuyMarket();
				}
				// Cross down from overbought
				else if (prevRsi > UpperLevel && rsiValue <= UpperLevel && Position >= 0)
				{
					SellMarket();
				}

				prevRsi = rsiValue;
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
