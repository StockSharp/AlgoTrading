using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope MA Short: EMA band reversion with ATR stops.
/// </summary>
public class EnvelopeMaShortStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _bandPercent;

	private decimal _entryPrice;

	public EnvelopeMaShortStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA period.", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
		_bandPercent = Param(nameof(BandPercent), 1.0m)
			.SetDisplay("Band %", "Band width percent.", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal BandPercent { get => _bandPercent.Value; set => _bandPercent.Value = value; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, ema); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (atrVal <= 0 || emaVal <= 0) return;
		var close = candle.ClosePrice;
		var factor = BandPercent / 100m;
		var upper = emaVal * (1m + factor);
		var lower = emaVal * (1m - factor);

		if (Position > 0)
		{
			if (close >= emaVal || close <= _entryPrice - atrVal * 1.5m) { SellMarket(); _entryPrice = 0; }
		}
		else if (Position < 0)
		{
			if (close <= emaVal || close >= _entryPrice + atrVal * 1.5m) { BuyMarket(); _entryPrice = 0; }
		}

		if (Position == 0)
		{
			if (close < lower) { _entryPrice = close; BuyMarket(); }
			else if (close > upper) { _entryPrice = close; SellMarket(); }
		}
	}
}
