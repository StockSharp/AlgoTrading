using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MostasHar15 Pivot strategy (simplified).
/// Uses daily pivot levels with RSI filter for entries.
/// </summary>
public class MostasHar15PivotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<int> _rsiLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public MostasHar15PivotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Trading candles", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Daily Candle Type", "Higher timeframe for pivots", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		decimal pivotHigh = 0;
		decimal pivotLow = 0;
		decimal pivotMid = 0;
		bool hasPivot = false;

		// Subscribe to higher timeframe for pivot levels
		var dailySub = SubscribeCandles(DailyCandleType);
		dailySub
			.Bind((ICandleMessage candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				pivotMid = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
				pivotHigh = 2m * pivotMid - candle.LowPrice;
				pivotLow = 2m * pivotMid - candle.HighPrice;
				hasPivot = true;
			})
			.Start();

		// Subscribe to trading timeframe with RSI
		var tradeSub = SubscribeCandles(CandleType);
		tradeSub
			.Bind(rsi, (ICandleMessage candle, decimal rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!hasPivot)
					return;

				var close = candle.ClosePrice;

				// Long: price above pivot, RSI momentum confirms
				if (close > pivotMid && rsiVal > 50 && Position <= 0)
					BuyMarket();
				// Short: price below pivot, RSI confirms weakness
				else if (close < pivotMid && rsiVal < 50 && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradeSub);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}
