using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Moving Average slope mean reversion.
/// Buys when the slope is far below its average and starts turning up, sells when it is far above
/// and starts turning down. Exits when the slope returns to its average or the ATR stop is hit.
/// </summary>
public class HullMaSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevHull;
	private decimal? _prevSlope;
	private decimal _stopPrice;

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
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
	/// Initialize <see cref="HullMaSlopeMeanReversionStrategy"/>.
	/// </summary>
	public HullMaSlopeMeanReversionStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Hull Period", "Period of Hull Moving Average", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy");

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
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevHull = null;
		_prevSlope = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hull = new HullMovingAverage { Length = HullPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hull, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hull);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hullValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHull is not decimal prevHull)
		{
			_prevHull = hullValue;
			return;
		}

		_prevHull = hullValue;

		var slope = hullValue - prevHull;
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
		var stopDistance = AtrMultiplier * atrValue;

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
