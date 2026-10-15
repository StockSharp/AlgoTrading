using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands width mean reversion.
/// Enters when the band width is beyond its average by a standard deviation multiplier and starts
/// turning back toward the average: long on an extreme contraction, short on an extreme expansion.
/// Exits when the width returns to its average or the ATR stop is hit.
/// </summary>
public class BollingerWidthMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _widthLookbackPeriod;
	private readonly StrategyParam<decimal> _widthDeviationMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _widthAverage;
	private StandardDeviation _widthStdDev;
	private decimal? _prevWidth;
	private decimal _stopPrice;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	/// ATR period for the stop.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initialize <see cref="BollingerWidthMeanReversionStrategy"/>.
	/// </summary>
	public BollingerWidthMeanReversionStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviations for Bollinger Bands", "Indicators");

		_widthLookbackPeriod = Param(nameof(WidthLookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Width Lookback", "Period for width statistics", "Strategy");

		_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Width Deviation Multiplier", "Standard deviation multiplier for extreme width", "Strategy");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for the stop", "Risk Management");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop-loss distance in ATR multiples", "Risk Management");

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

		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerDeviation };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_widthAverage = new SimpleMovingAverage { Length = WidthLookbackPeriod };
		_widthStdDev = new StandardDeviation { Length = WidthLookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !atrValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var atr = atrValue.ToDecimal();
		var width = upper - lower;
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
		var stopDistance = AtrMultiplier * atr;

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
