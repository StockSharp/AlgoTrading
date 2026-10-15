using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX slope mean reversion.
/// Buys when the ADX slope is far below its average and starts turning up, sells when it is far above
/// and starts turning down. Exits when the slope returns to its average.
/// </summary>
public class AdxSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevAdx;
	private decimal? _prevSlope;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
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
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initialize <see cref="AdxSlopeMeanReversionStrategy"/>.
	/// </summary>
	public AdxSlopeMeanReversionStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management");

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
		_prevAdx = null;
		_prevSlope = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
				DrawIndicator(adxArea, adx);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFormed)
			return;

		if (((AverageDirectionalIndexValue)adxValue).MovingAverage is not decimal adx)
			return;

		if (_prevAdx is not decimal prevAdx)
		{
			_prevAdx = adx;
			return;
		}

		_prevAdx = adx;

		var slope = adx - prevAdx;
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		var prevSlope = _prevSlope;
		_prevSlope = slope;

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed || prevSlope is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Extreme reading that has started to turn back toward the average.
		if (slope < avgSlope - DeviationMultiplier * stdSlope && slope > prev && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (slope > avgSlope + DeviationMultiplier * stdSlope && slope < prev && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && slope >= avgSlope)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && slope <= avgSlope)
		{
			BuyMarket(-Position);
		}
	}
}
