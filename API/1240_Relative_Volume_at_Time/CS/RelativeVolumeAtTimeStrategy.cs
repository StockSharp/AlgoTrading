using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on relative volume at a specific time of day.
/// </summary>
public class RelativeVolumeAtTimeStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _targetHour;
	private readonly StrategyParam<int> _targetMinute;

	private SimpleMovingAverage _volumeSma = null!;

	/// <summary>
	/// Number of candles for average volume calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Relative volume threshold to trigger trades.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
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
	/// Hour of day to evaluate volume.
	/// </summary>
	public int TargetHour
	{
		get => _targetHour.Value;
		set => _targetHour.Value = value;
	}

	/// <summary>
	/// Minute of hour to evaluate volume.
	/// </summary>
	public int TargetMinute
	{
		get => _targetMinute.Value;
		set => _targetMinute.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeVolumeAtTimeStrategy"/>.
	/// </summary>
	public RelativeVolumeAtTimeStrategy()
	{
		_period = Param(nameof(Period), 5)
			.SetGreaterThanZero()
			.SetDisplay("Volume Lookback", "Number of candles for average volume", "Parameters")
			.SetCanOptimize(true);

		_threshold = Param(nameof(Threshold), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Relative Volume Threshold", "Relative volume trigger level", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_targetHour = Param(nameof(TargetHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Target Hour", "Hour of day to evaluate volume", "General");

		_targetMinute = Param(nameof(TargetMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Target Minute", "Minute of hour to evaluate volume", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeSma = new SimpleMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(c => c.Volume, _volumeSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _volumeSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal avgVolume)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime.Hour != TargetHour || candle.OpenTime.Minute != TargetMinute)
			return;

		if (!_volumeSma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var relVol = avgVolume == 0m ? 0m : candle.Volume / avgVolume;

		if (relVol > Threshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (relVol < 1m && Position > 0)
		{
			SellMarket();
		}
	}
}
