using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume slope mean reversion.
/// The slope of the smoothed volume is compared with its average: buys when the slope is far below the average
/// and starts turning up, sells when it is far above and starts turning down.
/// Exits when the slope returns to its average.
/// </summary>
public class VolumeSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeMaPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevVolume;
	private decimal? _prevSlope;

	/// <summary>
	/// Period of the volume moving average.
	/// </summary>
	public int VolumeMaPeriod
	{
		get => _volumeMaPeriod.Value;
		set => _volumeMaPeriod.Value = value;
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
	/// Initialize <see cref="VolumeSlopeMeanReversionStrategy"/>.
	/// </summary>
	public VolumeSlopeMeanReversionStrategy()
	{
		_volumeMaPeriod = Param(nameof(VolumeMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Period", "Period of the volume moving average", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
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
		_volumeMa = null;
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevVolume = null;
		_prevSlope = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumeMa = new SimpleMovingAverage { Length = VolumeMaPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
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
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volume = _volumeMa.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_volumeMa.IsFormed)
			return;

		if (_prevVolume is not decimal prevVolume)
		{
			_prevVolume = volume;
			return;
		}

		_prevVolume = volume;

		var slope = volume - prevVolume;
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
