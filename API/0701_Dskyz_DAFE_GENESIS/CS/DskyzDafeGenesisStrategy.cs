using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified version of the "Dskyz (DAFE) GENESIS" strategy.
/// It trades on RSI conditions combined with EMA momentum and trend filters.
/// </summary>
public class DskyzDafeGenesisStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DskyzDafeGenesisStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 9)
			.SetDisplay("RSI Length", "RSI calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var smaFast = new SimpleMovingAverage { Length = 9 };
		var smaSlow = new SimpleMovingAverage { Length = 30 };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var emaFast = new ExponentialMovingAverage { Length = 8 };
		var emaSlow = new ExponentialMovingAverage { Length = 21 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(smaFast, smaSlow, rsi, emaFast, emaSlow, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, smaFast);
			DrawIndicator(area, smaSlow);
			DrawIndicator(area, rsi);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaFast, decimal smaSlow, decimal rsiValue, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trendUp = smaFast > smaSlow;
		var trendDown = smaFast < smaSlow;
		var momentumLong = emaFast > emaSlow;
		var momentumShort = emaFast < emaSlow;

		if (Position <= 0 && trendUp && rsiValue > 55 && momentumLong)
		{
			BuyMarket();
		}
		else if (Position >= 0 && trendDown && rsiValue < 45 && momentumShort)
		{
			SellMarket();
		}

		if (Position > 0 && momentumShort)
			SellMarket(Position);
		else if (Position < 0 && momentumLong)
			BuyMarket(Math.Abs(Position));
	}
}
