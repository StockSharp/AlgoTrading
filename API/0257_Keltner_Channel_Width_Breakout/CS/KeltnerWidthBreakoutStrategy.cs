namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades on Keltner Channel width breakouts.
/// When Keltner Channel width increases significantly above its average,
/// it enters position in the direction determined by price movement.
/// </summary>
public class KeltnerWidthBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _widthThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevWidth;
	private decimal _prevAvgWidth;
	private bool _hasPrev;

	/// <summary>
	/// EMA period for Keltner Channel.
	/// </summary>
	public int EMAPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for Keltner Channel.
	/// </summary>
	public int ATRPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner Channel.
	/// </summary>
	public decimal ATRMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Width threshold multiplier for breakout detection.
	/// </summary>
	public decimal WidthThreshold
	{
		get => _widthThreshold.Value;
		set => _widthThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="KeltnerWidthBreakoutStrategy"/>.
	/// </summary>
	public KeltnerWidthBreakoutStrategy()
	{
		_emaPeriod = Param(nameof(EMAPeriod), 20)
			.SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators");

		_atrPeriod = Param(nameof(ATRPeriod), 14)
			.SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators");

		_atrMultiplier = Param(nameof(ATRMultiplier), 2.0m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators");

		_widthThreshold = Param(nameof(WidthThreshold), 1.2m)
			.SetDisplay("Width Threshold", "Threshold multiplier for width breakout detection", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevWidth = 0;
		_prevAvgWidth = 0;
		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EMAPeriod };
		var atr = new AverageTrueRange { Length = ATRPeriod };

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

		// Calculate Keltner Channel boundaries
		var upperBand = emaValue + ATRMultiplier * atrValue;
		var lowerBand = emaValue - ATRMultiplier * atrValue;

		// Calculate Channel width
		var width = upperBand - lowerBand;

		if (!_hasPrev)
		{
			_prevWidth = width;
			_prevAvgWidth = width;
			_hasPrev = true;
			return;
		}

		// Simple exponential smoothing of width for average
		_prevAvgWidth = _prevAvgWidth * 0.9m + width * 0.1m;

		// Width breakout detection
		if (width > _prevAvgWidth * WidthThreshold)
		{
			// Determine direction based on price relative to EMA
			if (candle.ClosePrice > emaValue && Position <= 0)
			{
				BuyMarket();
			}
			else if (candle.ClosePrice < emaValue && Position >= 0)
			{
				SellMarket();
			}
		}
		// Exit when width contracts back
		else if (width < _prevAvgWidth * 0.8m)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}

		_prevWidth = width;
	}
}
