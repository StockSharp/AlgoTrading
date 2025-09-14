using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the MultiTrend Signal indicator.
/// Builds an adaptive channel and trades breakouts.
/// </summary>
public class ExpMultitrendSignalKvnStrategy : Strategy
{
	private readonly StrategyParam<decimal> _k;
	private readonly StrategyParam<decimal> _kStop;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _trend;

	/// <summary>
	/// Channel width coefficient.
	/// </summary>
	public decimal K
	{
		get => _k.Value;
		set => _k.Value = value;
	}

	/// <summary>
	/// Range multiplier used when switching direction.
	/// </summary>
	public decimal KStop
	{
		get => _kStop.Value;
		set => _kStop.Value = value;
	}

	/// <summary>
	/// Base period for swing calculation.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// ADX indicator period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Candles type used in calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ExpMultitrendSignalKvnStrategy()
	{
		_k = Param(nameof(K), 48m)
			.SetDisplay("K", "Percent of swing used for channel width", "Indicator")
			.SetCanOptimize(true);

		_kStop = Param(nameof(KStop), 0.5m)
			.SetDisplay("K Stop", "Multiplier for range added to breakout price", "Indicator")
			.SetCanOptimize(true);

		_kPeriod = Param(nameof(KPeriod), 150)
			.SetDisplay("K Period", "Base period for swing calculation", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period of ADX indicator", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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
		_trend = 0;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var maxHigh = new Max { Length = KPeriod };
		var minLow = new Min { Length = KPeriod };
		var rangeAvg = new SMA { Length = KPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(adx, (candle, adxValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var ssp = Math.Max(1, (int)Math.Ceiling(KPeriod / (double)adxValue));

				maxHigh.Length = ssp;
				minLow.Length = ssp;
				rangeAvg.Length = ssp;

				var maxVal = maxHigh.Process(candle.HighPrice);
				var minVal = minLow.Process(candle.LowPrice);
				var rangeVal = rangeAvg.Process(candle.HighPrice - candle.LowPrice);

				if (!maxVal.IsFinal || !minVal.IsFinal || !rangeVal.IsFinal)
					return;

				var ssMax = maxVal.GetValue<decimal>();
				var ssMin = minVal.GetValue<decimal>();
				var range = rangeVal.GetValue<decimal>();

				var swing = (ssMax - ssMin) * K / 100m;
				var smin = ssMin + swing;
				var smax = ssMax - swing;

				if (candle.ClosePrice > smax)
				{
					if (_trend <= 0 && Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));

					_trend = 1;
				}
				else if (candle.ClosePrice < smin)
				{
					if (_trend >= 0 && Position >= 0)
						SellMarket(Volume + Math.Abs(Position));

					_trend = -1;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, maxHigh);
			DrawIndicator(area, minLow);
		}
	}
}
