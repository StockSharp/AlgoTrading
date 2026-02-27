namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Roulette Game strategy: random-like entries based on candle direction with SMA filter.
/// Buys when candle is bullish and close above SMA. Sells when bearish and below SMA.
/// </summary>
public class RouletteGameStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public RouletteGameStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;

		if (isBullish && candle.ClosePrice > sma && Position <= 0)
			BuyMarket();
		else if (!isBullish && candle.ClosePrice < sma && Position >= 0)
			SellMarket();
	}
}
