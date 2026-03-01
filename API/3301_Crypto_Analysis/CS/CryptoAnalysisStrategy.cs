using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crypto Analysis strategy: Bollinger Bands breakout + Momentum filter.
/// Buys when close breaks above upper BB and momentum > 100.
/// Sells when close breaks below lower BB and momentum < 100.
/// </summary>
public class CryptoAnalysisStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _momPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public int MomPeriod
	{
		get => _momPeriod.Value;
		set => _momPeriod.Value = value;
	}

	public CryptoAnalysisStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_momPeriod = Param(nameof(MomPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");
	}

	private decimal _momValue;

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod };
		var mom = new Momentum { Length = MomPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mom, OnMomentum);

		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void OnMomentum(ICandleMessage candle, decimal momValue)
	{
		_momValue = momValue;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bbVal;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;

		if (close > upper && _momValue > 100m && Position <= 0)
		{
			BuyMarket();
		}
		else if (close < lower && _momValue < 100m && Position >= 0)
		{
			SellMarket();
		}
	}
}
