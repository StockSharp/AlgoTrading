using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Supertrend indicator flips.
/// Detects when Supertrend direction changes and trades accordingly.
/// </summary>
public class TradingViewSupertrendFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<bool> _useVolumeFilter;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeAverage;
	private bool _prevIsUpTrend;
	private bool _hasPrevValues;

	/// <summary>
	/// Period for Supertrend calculation.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend calculation.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	public int VolumeAvgPeriod { get => _volumeAvgPeriod.Value; set => _volumeAvgPeriod.Value = value; }
	public bool UseVolumeFilter { get => _useVolumeFilter.Value; set => _useVolumeFilter.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the TradingView Supertrend Flip strategy.
	/// </summary>
	public TradingViewSupertrendFlipStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "Period for Supertrend calculation", "Indicators")
			.SetOptimize(7, 14, 1);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend", "Indicators")
			.SetOptimize(3.0m, 5.0m, 0.5m);

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20).SetGreaterThanZero();
		_useVolumeFilter = Param(nameof(UseVolumeFilter), true);

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
		_volumeAverage = null;
		_prevIsUpTrend = default;
		_hasPrevValues = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_volumeAverage = new SimpleMovingAverage { Length = VolumeAvgPeriod };

		var supertrend = new SuperTrend
		{
			Length = SupertrendPeriod,
			Multiplier = SupertrendMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle, allowEmpty: true)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Feed volume during Supertrend warm-up, using the current bar in the 20-bar SMA.
		var volumeValue = _volumeAverage.Process(new DecimalIndicatorValue(_volumeAverage, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		if (!supertrendValue.IsFormed || supertrendValue.IsEmpty || !IsFormedAndOnlineAndAllowTrading())
			return;

		// The native direction preserves inclusive flips when the close equals a band.
		var isUpTrend = ((SuperTrendIndicatorValue)supertrendValue).IsUpTrend;
		var confirmed = !UseVolumeFilter || (_volumeAverage.IsFormed && candle.TotalVolume > volumeValue.GetValue<decimal>());

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevIsUpTrend = isUpTrend;
			return;
		}

		// Detect flip
		var isFlippedBullish = isUpTrend && !_prevIsUpTrend;
		var isFlippedBearish = !isUpTrend && _prevIsUpTrend;

		_prevIsUpTrend = isUpTrend;

		if (isFlippedBullish && Position <= 0)
		{
			if (confirmed)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0m)
				BuyMarket(Math.Abs(Position));
		}
		else if (isFlippedBearish && Position >= 0)
		{
			if (confirmed)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0m)
				SellMarket(Math.Abs(Position));
		}
	}
}
