namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Keltner Width Mean Reversion Strategy.
/// Trades based on mean reversion of Keltner Channel width.
/// </summary>
public class KeltnerWidthMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<decimal> _widthDeviationMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _widthAvg;
	private bool _hasPrev;

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	public decimal WidthDeviationMultiplier
	{
		get => _widthDeviationMultiplier.Value;
		set => _widthDeviationMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public KeltnerWidthMeanReversionStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2.0m)
			.SetDisplay("Keltner Multiplier", "Multiplier for Keltner Channel bands", "Indicators");

		_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 1.5m)
			.SetDisplay("Width Dev Multiplier", "Multiplier for width deviation threshold", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_widthAvg = 0;
		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var upperBand = emaValue + KeltnerMultiplier * atrValue;
		var lowerBand = emaValue - KeltnerMultiplier * atrValue;
		var width = upperBand - lowerBand;

		if (!_hasPrev)
		{
			_widthAvg = width;
			_hasPrev = true;
			return;
		}

		// Exponential smoothing of width average
		_widthAvg = _widthAvg * 0.9m + width * 0.1m;

		var lowerThreshold = _widthAvg * (1m - WidthDeviationMultiplier * 0.1m);
		var upperThreshold = _widthAvg * (1m + WidthDeviationMultiplier * 0.1m);

		// Mean reversion: compressed width -> expect expansion
		if (width < lowerThreshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (width > upperThreshold && Position >= 0)
		{
			SellMarket();
		}
		// Exit when width returns to average
		else if (width > _widthAvg && Position > 0)
		{
			SellMarket();
		}
		else if (width < _widthAvg && Position < 0)
		{
			BuyMarket();
		}
	}
}
