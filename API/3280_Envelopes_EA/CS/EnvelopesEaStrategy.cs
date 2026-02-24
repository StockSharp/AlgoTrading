using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelopes EA strategy: mean reversion with envelope bands.
/// Buys when close drops below lower envelope band.
/// Sells when close rises above upper envelope band.
/// Exits when price returns to the middle EMA.
/// </summary>
public class EnvelopesEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _deviationPercent;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public decimal DeviationPercent
	{
		get => _deviationPercent.Value;
		set => _deviationPercent.Value = value;
	}

	public EnvelopesEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Envelope center EMA period", "Indicators");

		_deviationPercent = Param(nameof(DeviationPercent), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation %", "Envelope deviation percentage", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var upperBand = emaValue * (1m + DeviationPercent / 100m);
		var lowerBand = emaValue * (1m - DeviationPercent / 100m);

		// Mean reversion: buy at lower band, sell at upper band
		if (close < lowerBand && Position <= 0)
		{
			BuyMarket();
		}
		else if (close > upperBand && Position >= 0)
		{
			SellMarket();
		}
	}
}
