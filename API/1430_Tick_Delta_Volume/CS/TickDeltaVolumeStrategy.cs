using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on tick delta calculations.
/// </summary>
public class TickDeltaVolumeStrategy : Strategy
{
	public enum DeltaMode
	{
		/// <summary>Use volume delta.</summary>
		Volume,
		/// <summary>Use price delta.</summary>
		Price,
		/// <summary>Use product of price and volume delta.</summary>
		PriceVolume
	}

	private readonly StrategyParam<DeltaMode> _mode;
	private readonly StrategyParam<int> _length;

	private ExponentialMovingAverage _mean;
	private StandardDeviation _stdev;
	private decimal _prevPrice;

	/// <summary>
	/// Delta calculation mode.
	/// </summary>
	public DeltaMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Lookback length for mean and deviation.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TickDeltaVolumeStrategy"/>.
	/// </summary>
	public TickDeltaVolumeStrategy()
	{
		_mode = Param(nameof(Mode), DeltaMode.Volume)
		.SetDisplay("Mode", "Delta calculation mode", "General");

		_length = Param(nameof(Length), 10)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Lookback for average and deviation", "Indicators")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_mean = null;
		_stdev = null;
		_prevPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mean = new ExponentialMovingAverage { Length = Length };
		_stdev = new StandardDeviation { Length = Length };

		SubscribeTicks()
		.Bind(ProcessTick)
		.Start();
	}

	private void ProcessTick(ExecutionMessage trade)
	{
		var volumeDelta = trade.Volume;
		var priceDelta = trade.Price - _prevPrice;
		_prevPrice = trade.Price;

		var vpd = Mode switch
		{
			DeltaMode.Price => priceDelta,
			DeltaMode.PriceVolume => priceDelta * volumeDelta,
			_ => volumeDelta
		};

		var meanVal = _mean.Process(vpd, trade.ServerTime, true).ToDecimal();
		var stdVal = _stdev.Process(vpd, trade.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var threshold = meanVal + stdVal;

		if (vpd > threshold && Position <= 0)
			BuyMarket();
		else if (vpd < -threshold && Position >= 0)
			SellMarket();
	}
}