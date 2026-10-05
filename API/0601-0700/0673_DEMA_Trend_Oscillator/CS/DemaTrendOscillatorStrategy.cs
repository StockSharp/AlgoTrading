using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DEMA trend oscillator strategy.
/// The DEMA is normalized against its BaseLength SMA and standard deviation: the z-score is mapped to 0..100 with a logistic curve.
/// The bands are the DEMA plus and minus that standard deviation.
/// A long opens when the normalized value is above LongThreshold and the whole candle is above the upper band;
/// a short opens when it is below ShortThreshold and the whole candle is below the lower band.
/// The stop is the opposite band at entry, the target is RiskReward times that risk,
/// and an ATR trailing stop (AtrMultiplier times ATR) follows the price.
/// </summary>
public class DemaTrendOscillatorStrategy : Strategy
{
	private const int _atrLength = 14;

	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<int> _baseLength;
	private readonly StrategyParam<decimal> _longThreshold;
	private readonly StrategyParam<decimal> _shortThreshold;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _base;
	private StandardDeviation _stdDev;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _trailPrice;

	/// <summary>
	/// DEMA period.
	/// </summary>
	public int DemaPeriod { get => _demaPeriod.Value; set => _demaPeriod.Value = value; }

	/// <summary>
	/// Length of the SMA and standard deviation of the DEMA.
	/// </summary>
	public int BaseLength { get => _baseLength.Value; set => _baseLength.Value = value; }

	/// <summary>
	/// Normalized value above which longs are allowed.
	/// </summary>
	public decimal LongThreshold { get => _longThreshold.Value; set => _longThreshold.Value = value; }

	/// <summary>
	/// Normalized value below which shorts are allowed.
	/// </summary>
	public decimal ShortThreshold { get => _shortThreshold.Value; set => _shortThreshold.Value = value; }

	/// <summary>
	/// Take profit as a multiple of the band stop distance.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// ATR multiplier of the trailing stop.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DemaTrendOscillatorStrategy()
	{
		_demaPeriod = Param(nameof(DemaPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("DEMA Period", "DEMA period", "Indicators");

		_baseLength = Param(nameof(BaseLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Base Length", "Length of the SMA and standard deviation of the DEMA", "Indicators");

		_longThreshold = Param(nameof(LongThreshold), 55m)
			.SetDisplay("Long Threshold", "Normalized value above which longs are allowed", "Signals");

		_shortThreshold = Param(nameof(ShortThreshold), 45m)
			.SetDisplay("Short Threshold", "Normalized value below which shorts are allowed", "Signals");

		_riskReward = Param(nameof(RiskReward), 1.5m)
			.SetNotNegative()
			.SetDisplay("Risk Reward", "Take profit as a multiple of the band stop distance", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the trailing stop", "Risk");

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
		ClearStops();
	}

	private void ClearStops()
	{
		_stopPrice = null;
		_takePrice = null;
		_trailPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ClearStops();

		var dema = new DoubleExponentialMovingAverage { Length = DemaPeriod };
		var atr = new AverageTrueRange { Length = _atrLength };
		_base = new SimpleMovingAverage { Length = BaseLength };
		_stdDev = new StandardDeviation { Length = BaseLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(dema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, dema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue demaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!demaValue.IsFormed)
			return;

		var dema = demaValue.GetValue<decimal>();
		var baseValue = _base.Process(dema, candle.ServerTime, true);
		var sdValue = _stdDev.Process(dema, candle.ServerTime, true);

		if (!baseValue.IsFormed || !sdValue.IsFormed || !atrValue.IsFormed)
			return;

		var basis = baseValue.GetValue<decimal>();
		var sd = sdValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		if (sd <= 0m)
			return;

		var z = (double)((dema - basis) / sd);
		var normalized = (decimal)(100.0 / (1.0 + Math.Exp(-z)));
		var upper = dema + sd;
		var lower = dema - sd;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (AtrMultiplier > 0m)
			{
				var trail = close - atr * AtrMultiplier;
				_trailPrice = _trailPrice is decimal t ? Math.Max(t, trail) : trail;
			}

			if ((_stopPrice is decimal stop && candle.LowPrice <= stop)
				|| (_takePrice is decimal take && candle.HighPrice >= take)
				|| (_trailPrice is decimal trailStop && candle.LowPrice <= trailStop))
			{
				SellMarket(Position);
				ClearStops();
			}

			return;
		}

		if (Position < 0)
		{
			if (AtrMultiplier > 0m)
			{
				var trail = close + atr * AtrMultiplier;
				_trailPrice = _trailPrice is decimal t ? Math.Min(t, trail) : trail;
			}

			if ((_stopPrice is decimal stop && candle.HighPrice >= stop)
				|| (_takePrice is decimal take && candle.LowPrice <= take)
				|| (_trailPrice is decimal trailStop && candle.HighPrice >= trailStop))
			{
				BuyMarket(-Position);
				ClearStops();
			}

			return;
		}

		if (normalized > LongThreshold && candle.LowPrice > upper)
		{
			BuyMarket();
			_stopPrice = lower;
			_takePrice = RiskReward > 0m ? close + (close - lower) * RiskReward : null;
			_trailPrice = null;
		}
		else if (normalized < ShortThreshold && candle.HighPrice < lower)
		{
			SellMarket();
			_stopPrice = upper;
			_takePrice = RiskReward > 0m ? close - (upper - close) * RiskReward : null;
			_trailPrice = null;
		}
	}
}
