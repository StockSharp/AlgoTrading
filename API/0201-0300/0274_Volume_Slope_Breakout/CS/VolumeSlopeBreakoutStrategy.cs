using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume slope breakout.
/// Enters when the slope of the smoothed volume exceeds its average by a standard deviation multiplier,
/// in the direction of the breakout candle. Exits when the slope returns to its average.
/// </summary>
public class VolumeSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeSmaPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevVolume;

	/// <summary>
	/// Period of the volume moving average.
	/// </summary>
	public int VolumeSMAPeriod
	{
		get => _volumeSmaPeriod.Value;
		set => _volumeSmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for slope statistics.
	/// </summary>
	public int SlopePeriod
	{
		get => _slopePeriod.Value;
		set => _slopePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
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
	/// Initialize <see cref="VolumeSlopeBreakoutStrategy"/>.
	/// </summary>
	public VolumeSlopeBreakoutStrategy()
	{
		_volumeSmaPeriod = Param(nameof(VolumeSMAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Period", "Period of the volume moving average", "Indicators");

		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Period for slope statistics", "Strategy");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Strategy");

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
		_volumeSma = null;
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevVolume = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumeSma = new SimpleMovingAverage { Length = VolumeSMAPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = SlopePeriod };
		_slopeStdDev = new StandardDeviation { Length = SlopePeriod };

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

		var volume = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_volumeSma.IsFormed)
			return;

		if (_prevVolume is not decimal prev)
		{
			_prevVolume = volume;
			return;
		}

		_prevVolume = volume;

		var slope = volume - prev;
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Volume has no direction, so the breakout candle decides the side.
		if (slope > avgSlope + BreakoutMultiplier * stdSlope)
		{
			if (candle.ClosePrice > candle.OpenPrice && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				return;
			}

			if (candle.ClosePrice < candle.OpenPrice && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				return;
			}
		}

		if (slope < avgSlope)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
		}
	}
}
