using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR slope mean reversion.
/// Buys when the ATR slope is far below its average and starts turning up, sells when it is far above
/// and starts turning down. Exits when the slope returns to its average or the ATR stop is hit.
/// </summary>
public class AtrSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<int> _stopLossMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevAtr;
	private decimal? _prevSlope;
	private decimal _stopPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for slope statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for extreme slope.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public int StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
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
	/// Initialize <see cref="AtrSlopeMeanReversionStrategy"/>.
	/// </summary>
	public AtrSlopeMeanReversionStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of ATR", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 2)
			.SetNotNegative()
			.SetDisplay("Stop Loss Multiplier", "Stop-loss distance in ATR multiples", "Risk Management");

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
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevAtr = null;
		_prevSlope = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var atrArea = CreateChartArea();
			if (atrArea != null)
				DrawIndicator(atrArea, atr);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevAtr is not decimal prevAtr)
		{
			_prevAtr = atrValue;
			return;
		}

		_prevAtr = atrValue;

		var slope = atrValue - prevAtr;
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		var prevSlope = _prevSlope;
		_prevSlope = slope;

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed || prevSlope is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStop(candle))
			return;

		var close = candle.ClosePrice;
		var stopDistance = StopLossMultiplier * atrValue;

		// Extreme reading that has started to turn back toward the average.
		if (slope < avgSlope - DeviationMultiplier * stdSlope && slope > prev && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = stopDistance > 0 ? close - stopDistance : 0m;
		}
		else if (slope > avgSlope + DeviationMultiplier * stdSlope && slope < prev && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = stopDistance > 0 ? close + stopDistance : 0m;
		}
		else if ((Position > 0 && slope >= avgSlope) || (Position < 0 && slope <= avgSlope))
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
