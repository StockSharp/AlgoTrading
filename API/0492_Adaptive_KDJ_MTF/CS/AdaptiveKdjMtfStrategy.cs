namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that combines KDJ (Stochastic-based) signals from multiple timeframes.
/// Uses weighted combination of K, D values from 3 timeframes with smoothing.
/// </summary>
public class AdaptiveKdjMtfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _timeFrame1;
	private readonly StrategyParam<DataType> _timeFrame2;
	private readonly StrategyParam<DataType> _timeFrame3;
	private readonly StrategyParam<int> _kdjLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;

	private decimal _k1, _d1, _j1;
	private decimal _k2, _d2, _j2;
	private decimal _k3, _d3, _j3;
	private decimal _prevSmoothK;
	private decimal _prevSmoothD;
	private decimal _smoothK;
	private decimal _smoothD;
	private int _processCount;

	public DataType TimeFrame1 { get => _timeFrame1.Value; set => _timeFrame1.Value = value; }
	public DataType TimeFrame2 { get => _timeFrame2.Value; set => _timeFrame2.Value = value; }
	public DataType TimeFrame3 { get => _timeFrame3.Value; set => _timeFrame3.Value = value; }
	public int KdjLength { get => _kdjLength.Value; set => _kdjLength.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }

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
			.SetOptimize(3, 15, 3);

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetDisplay("Smoothing Length", "EMA smoothing length", "Parameters")
			.SetOptimize(3, 15, 2);

		_buyLevel = Param(nameof(BuyLevel), 30m)
			.SetDisplay("Buy Level", "J value threshold for buy signal", "Parameters")
			.SetOptimize(15m, 40m, 5m);

		_sellLevel = Param(nameof(SellLevel), 70m)
			.SetDisplay("Sell Level", "J value threshold for sell signal", "Parameters")
			.SetOptimize(60m, 85m, 5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TimeFrame1), (Security, TimeFrame2), (Security, TimeFrame3)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_k1 = _d1 = _j1 = 50m;
		_k2 = _d2 = _j2 = 50m;
		_k3 = _d3 = _j3 = 50m;
		_prevSmoothK = 50m;
		_prevSmoothD = 50m;
		_smoothK = 50m;
		_smoothD = 50m;
		_processCount = 0;

		var stoch1 = new StochasticOscillator();
		stoch1.K.Length = KdjLength;
		stoch1.D.Length = 3;

		var stoch2 = new StochasticOscillator();
		stoch2.K.Length = KdjLength;
		stoch2.D.Length = 3;

		var stoch3 = new StochasticOscillator();
		stoch3.K.Length = KdjLength;
		stoch3.D.Length = 3;

		var sub1 = SubscribeCandles(TimeFrame1);
		var sub2 = SubscribeCandles(TimeFrame2);
		var sub3 = SubscribeCandles(TimeFrame3);

		sub1.BindEx(stoch1, ProcessTf1).Start();
		sub2.BindEx(stoch2, ProcessTf2).Start();
		sub3.BindEx(stoch3, ProcessTf3).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub1);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTf1(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sv = (IStochasticOscillatorValue)stochValue;
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

		var sv = (IStochasticOscillatorValue)stochValue;
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

		var sv = (IStochasticOscillatorValue)stochValue;
		if (sv.K is not decimal k || sv.D is not decimal d)
			return;

		_k3 = k;
		_d3 = d;
		_j3 = 3m * k - 2m * d;
		ProcessCombined();
	}

	private void ProcessCombined()
	{
		_processCount++;

		// Weighted average of K, D, J values from 3 timeframes
		var avgK = _k1 * 0.5m + _k2 * 0.3m + _k3 * 0.2m;
		var avgD = _d1 * 0.5m + _d2 * 0.3m + _d3 * 0.2m;
		var avgJ = _j1 * 0.5m + _j2 * 0.3m + _j3 * 0.2m;

		// Simple EMA smoothing
		var alpha = 2m / (SmoothingLength + 1m);
		_smoothK = alpha * avgK + (1m - alpha) * _smoothK;
		_smoothD = alpha * avgD + (1m - alpha) * _smoothD;
		var smoothJ = alpha * avgJ + (1m - alpha) * avgJ;

		// Need some warmup
		if (_processCount < SmoothingLength * 3)
		{
			_prevSmoothK = _smoothK;
			_prevSmoothD = _smoothD;
			return;
		}

		var crossUp = _prevSmoothK <= _prevSmoothD && _smoothK > _smoothD;
		var crossDown = _prevSmoothK >= _prevSmoothD && _smoothK < _smoothD;

		var buySignal = smoothJ < BuyLevel && crossUp;
		var sellSignal = smoothJ > SellLevel && crossDown;

		if (Position <= 0 && buySignal)
			BuyMarket();
		else if (Position >= 0 && sellSignal)
			SellMarket();

		_prevSmoothK = _smoothK;
		_prevSmoothD = _smoothD;
	}
}
