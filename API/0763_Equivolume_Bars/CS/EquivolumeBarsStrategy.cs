using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on volume ratio from equivolume bars.
/// Enters long when volume spike occurs on bullish candle.
/// Enters short when volume spike occurs on bearish candle.
/// Closes position when volume returns below threshold or candle reverses.
/// </summary>
public class EquivolumeBarsStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private decimal _prevVolumeSum;

	/// <summary>
	/// Number of bars for volume sum.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Ratio threshold of current volume to previous sum.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EquivolumeBarsStrategy()
	{
		_lookback = Param(nameof(Lookback), 60)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Volume lookback period", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 120, 20);

		_volumeThreshold = Param(nameof(VolumeThreshold), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Threshold", "Ratio threshold for high volume", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.2m, 0.05m);

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

		_volumeSma?.Reset();
		_prevVolumeSum = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeSma = new SimpleMovingAverage { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription.ForEach(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentAvg = _volumeSma.Process(candle.TotalVolume).ToDecimal();

		if (!_volumeSma.IsFormed)
			return;

		var currentSum = currentAvg * Lookback;
		var ratio = _prevVolumeSum == 0 ? 0 : candle.TotalVolume / _prevVolumeSum;

		var isBull = candle.ClosePrice >= candle.OpenPrice;
		var isBear = candle.ClosePrice < candle.OpenPrice;
		var highVolume = ratio > VolumeThreshold;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (highVolume && isBull && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (highVolume && isBear && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0 && (!highVolume || isBear))
				SellMarket(Math.Abs(Position));
			else if (Position < 0 && (!highVolume || isBull))
				BuyMarket(Math.Abs(Position));
		}

		_prevVolumeSum = currentSum;
	}
}
