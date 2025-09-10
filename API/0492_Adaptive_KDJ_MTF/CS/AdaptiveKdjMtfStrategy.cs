using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines KDJ signals from multiple timeframes.
/// </summary>
public class AdaptiveKdjMtfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _timeFrame1;
	private readonly StrategyParam<DataType> _timeFrame2;
	private readonly StrategyParam<DataType> _timeFrame3;
	private readonly StrategyParam<int> _kdjLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _weightOption;

	private decimal _k1, _d1, _j1;
	private decimal _k2, _d2, _j2;
	private decimal _k3, _d3, _j3;
	private decimal _prevSmoothK;
	private decimal _prevSmoothD;

	private readonly ExponentialMovingAverage _emaK = new();
	private readonly ExponentialMovingAverage _emaD = new();
	private readonly ExponentialMovingAverage _emaJ = new();
	private readonly ExponentialMovingAverage _emaTotal = new();
	private readonly SimpleMovingAverage _trendSma = new();

	public DataType TimeFrame1 { get => _timeFrame1.Value; set => _timeFrame1.Value = value; }
	public DataType TimeFrame2 { get => _timeFrame2.Value; set => _timeFrame2.Value = value; }
	public DataType TimeFrame3 { get => _timeFrame3.Value; set => _timeFrame3.Value = value; }
	public int KdjLength { get => _kdjLength.Value; set => _kdjLength.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int WeightOption { get => _weightOption.Value; set => _weightOption.Value = value; }

	public AdaptiveKdjMtfStrategy()
	{
		_timeFrame1 = Param(nameof(TimeFrame1), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("TimeFrame1", "First timeframe", "Timeframes");
		_timeFrame2 = Param(nameof(TimeFrame2), TimeSpan.FromMinutes(3).TimeFrame())
			.SetDisplay("TimeFrame2", "Second timeframe", "Timeframes");
		_timeFrame3 = Param(nameof(TimeFrame3), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("TimeFrame3", "Third timeframe", "Timeframes");

		_kdjLength = Param(nameof(KdjLength), 9)
			.SetDisplay("KDJ Length", "Base length for KDJ", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 3);

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetDisplay("Smoothing Length", "EMA smoothing length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 2);

		_trendLength = Param(nameof(TrendLength), 40)
			.SetDisplay("Trend Length", "Periods for trend definition", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 10);

		_weightOption = Param(nameof(WeightOption), 1)
			.SetDisplay("Weight Option", "Weight distribution between timeframes", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TimeFrame1), (Security, TimeFrame2), (Security, TimeFrame3)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_k1 = _d1 = _j1 = 0m;
		_k2 = _d2 = _j2 = 0m;
		_k3 = _d3 = _j3 = 0m;
		_prevSmoothK = 0m;
		_prevSmoothD = 0m;
		_emaK.Reset();
		_emaD.Reset();
		_emaJ.Reset();
		_emaTotal.Reset();
		_trendSma.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaK.Length = SmoothingLength;
		_emaD.Length = SmoothingLength;
		_emaJ.Length = SmoothingLength;
		_emaTotal.Length = SmoothingLength;
		_trendSma.Length = TrendLength;

		var stoch1 = new Stochastic { Length = KdjLength, KPeriod = 3, DPeriod = 3 };
		var stoch2 = new Stochastic { Length = KdjLength, KPeriod = 3, DPeriod = 3 };
		var stoch3 = new Stochastic { Length = KdjLength, KPeriod = 3, DPeriod = 3 };

		var sub1 = SubscribeCandles(TimeFrame1);
		var sub2 = SubscribeCandles(TimeFrame2);
		var sub3 = SubscribeCandles(TimeFrame3);

		sub1.BindEx(stoch1, ProcessTf1).Start();
		sub2.BindEx(stoch2, ProcessTf2).Start();
		sub3.BindEx(stoch3, ProcessTf3).Start();
	}

	private void ProcessTf1(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sv = (StochasticValue)stochValue;
		if (sv.K is not decimal k || sv.D is not decimal d)
			return;

		_k1 = k;
		_d1 = d;
		_j1 = 3m * k - 2m * d;
		ProcessCombined();
	}

	private void ProcessTf2(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sv = (StochasticValue)stochValue;
		if (sv.K is not decimal k || sv.D is not decimal d)
			return;

		_k2 = k;
		_d2 = d;
		_j2 = 3m * k - 2m * d;
		ProcessCombined();
	}

	private void ProcessTf3(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sv = (StochasticValue)stochValue;
		if (sv.K is not decimal k || sv.D is not decimal d)
			return;

		_k3 = k;
		_d3 = d;
		_j3 = 3m * k - 2m * d;
		ProcessCombined();
	}

	private void ProcessCombined()
	{
		var w1 = WeightOption switch
		{
			1 => 0.5m,
			2 => 0.4m,
			3 => 0.33m,
			4 => 0.2m,
			_ => 0.1m
		};
		var w2 = 0.33m;
		var w3 = 1m - (w1 + w2);

		var avgK = _k1 * w1 + _k2 * w2 + _k3 * w3;
		var avgD = _d1 * w1 + _d2 * w2 + _d3 * w3;
		var avgJ = _j1 * w1 + _j2 * w2 + _j3 * w3;

		var kVal = _emaK.Process(new DecimalIndicatorValue(_emaK, avgK));
		var dVal = _emaD.Process(new DecimalIndicatorValue(_emaD, avgD));
		var jVal = _emaJ.Process(new DecimalIndicatorValue(_emaJ, avgJ));
		var totalVal = _emaTotal.Process(new DecimalIndicatorValue(_emaTotal, (avgK + avgD + avgJ) / 3m));

		if (!kVal.IsFinal || !dVal.IsFinal || !jVal.IsFinal || !totalVal.IsFinal)
			return;

		var smoothK = kVal.ToDecimal();
		var smoothD = dVal.ToDecimal();
		var smoothJ = jVal.ToDecimal();
		var smoothTotal = totalVal.ToDecimal();

		var trendVal = _trendSma.Process(new DecimalIndicatorValue(_trendSma, smoothTotal));
		if (!trendVal.IsFinal)
			return;

		var trendAvg = trendVal.ToDecimal();
		var isUptrend = trendAvg > 60m;
		var isDowntrend = trendAvg < 40m;

		var buyLevel = isUptrend ? 40m : isDowntrend ? 15m : 25m;
		var sellLevel = isUptrend ? 85m : isDowntrend ? 60m : 75m;

		var crossUp = _prevSmoothK <= _prevSmoothD && smoothK > smoothD;
		var crossDown = _prevSmoothK >= _prevSmoothD && smoothK < smoothD;

		var buySignal = smoothJ < buyLevel && crossUp;
		var sellSignal = smoothJ > sellLevel && crossDown;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && buySignal)
			RegisterBuy();
		else if (Position >= 0 && sellSignal)
			RegisterSell();

		_prevSmoothK = smoothK;
		_prevSmoothD = smoothD;
	}
}
