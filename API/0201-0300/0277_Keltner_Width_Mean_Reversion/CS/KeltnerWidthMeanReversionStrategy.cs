using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel width mean reversion.
/// Enters when the channel width is beyond its average by a standard deviation multiplier and starts
/// turning back toward the average: long on an extreme contraction, short on an extreme expansion.
/// Exits when the width returns to its average or the ATR stop is hit.
/// </summary>
public class KeltnerWidthMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _widthLookbackPeriod;
	private readonly StrategyParam<decimal> _widthDeviationMultiplier;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _widthAverage;
	private StandardDeviation _widthStdDev;
	private decimal? _prevWidth;
	private decimal _stopPrice;

	/// <summary>
	/// EMA period for the channel middle line.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for the channel bands and the stop.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the channel bands.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Lookback period for width statistics.
	/// </summary>
	public int WidthLookbackPeriod
	{
		get => _widthLookbackPeriod.Value;
		set => _widthLookbackPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for extreme width.
	/// </summary>
	public decimal WidthDeviationMultiplier
	{
		get => _widthDeviationMultiplier.Value;
		set => _widthDeviationMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="KeltnerWidthMeanReversionStrategy"/>.
	/// </summary>
	public KeltnerWidthMeanReversionStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for Keltner Channel", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for Keltner Channel", "Indicators");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Multiplier", "ATR multiplier for Keltner Channel", "Indicators");

		_widthLookbackPeriod = Param(nameof(WidthLookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Width Lookback", "Period for width statistics", "Strategy");

		_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Width Deviation Multiplier", "Standard deviation multiplier for extreme width", "Strategy");

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2.0m)
			.SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Stop-loss distance in ATR multiples", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_widthAverage = null;
		_widthStdDev = null;
		_prevWidth = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_widthAverage = new SimpleMovingAverage { Length = WidthLookbackPeriod };
		_widthStdDev = new StandardDeviation { Length = WidthLookbackPeriod };

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

		// Width of the channel: (EMA + k*ATR) - (EMA - k*ATR).
		var width = 2m * KeltnerMultiplier * atrValue;
		var avgWidth = _widthAverage.Process(width, candle.ServerTime, true).ToDecimal();
		var stdWidth = _widthStdDev.Process(width, candle.ServerTime, true).ToDecimal();

		var prevWidth = _prevWidth;
		_prevWidth = width;

		if (!_widthAverage.IsFormed || !_widthStdDev.IsFormed || prevWidth is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStop(candle))
			return;

		var close = candle.ClosePrice;
		var stopDistance = AtrStopMultiplier * atrValue;

		// Extreme reading that has started to turn back toward the average.
		if (width < avgWidth - WidthDeviationMultiplier * stdWidth && width > prev && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = stopDistance > 0 ? close - stopDistance : 0m;
		}
		else if (width > avgWidth + WidthDeviationMultiplier * stdWidth && width < prev && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = stopDistance > 0 ? close + stopDistance : 0m;
		}
		else if ((Position > 0 && width >= avgWidth) || (Position < 0 && width <= avgWidth))
		{
			ExitPosition();
		}
	}

	private bool CheckStop(ICandleMessage candle)
	{
		if (_stopPrice == 0m)
			return false;

		if ((Position > 0 && candle.LowPrice <= _stopPrice) || (Position < 0 && candle.HighPrice >= _stopPrice))
		{
			ExitPosition();
			return true;
		}

		return false;
	}

	private void ExitPosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		_stopPrice = 0m;
	}
}
