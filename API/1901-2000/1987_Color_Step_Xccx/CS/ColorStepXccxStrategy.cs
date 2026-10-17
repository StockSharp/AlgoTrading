using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Normalized ColorStepXCCX step channel with JMA price smoothing and T3 deviation
/// smoothing. Long when fast steps below slow, short when fast steps above slow.
/// Unlike a conventional EMA crossover, the step parameters affect BOTH lines.
/// </summary>
public class ColorStepXccxStrategy : Strategy
{
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _mPeriod;
	private readonly StrategyParam<decimal> _stepSizeFast;
	private readonly StrategyParam<decimal> _stepSizeSlow;
	private readonly StrategyParam<DataType> _candleType;
	private JurikMovingAverage _priceAverage;
	private T3MovingAverage _signedDeviationAverage;
	private T3MovingAverage _absoluteDeviationAverage;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal _fastMin;
	private decimal _fastMax;
	private decimal _slowMin;
	private decimal _slowMax;
	private int _fastTrend;
	private int _slowTrend;
	private bool _hasStepBaseline;

	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public int MPeriod { get => _mPeriod.Value; set => _mPeriod.Value = value; }
	public decimal StepSizeFast { get => _stepSizeFast.Value; set => _stepSizeFast.Value = value; }
	public decimal StepSizeSlow { get => _stepSizeSlow.Value; set => _stepSizeSlow.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorStepXccxStrategy()
	{
		_dPeriod = Param(nameof(DPeriod), 30).SetGreaterThanZero()
			.SetDisplay("D Period", "JMA price smoothing period", "Indicators");
		_mPeriod = Param(nameof(MPeriod), 7).SetGreaterThanZero()
			.SetDisplay("M Period", "T3 normalized deviation smoothing period", "Indicators");
		_stepSizeFast = Param(nameof(StepSizeFast), 5m).SetGreaterThanZero()
			.SetDisplay("Fast Step", "Normalized oscillator fast step", "Indicators");
		_stepSizeSlow = Param(nameof(StepSizeSlow), 30m).SetGreaterThanZero()
			.SetDisplay("Slow Step", "Normalized oscillator slow step", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_priceAverage = null;
		_signedDeviationAverage = null;
		_absoluteDeviationAverage = null;
		_previousFast = _previousSlow = null;
		_hasStepBaseline = false;
		_fastTrend = _slowTrend = 0;
		_fastMin = _fastMax = _slowMin = _slowMax = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousFast = _previousSlow = null;
		_hasStepBaseline = false;
		_fastTrend = _slowTrend = 0;
		_priceAverage = new JurikMovingAverage { Length = DPeriod, Phase = 100 };
		// The original T3 smoothing phase (15) represents a factor of 0.15.
		_signedDeviationAverage = new T3MovingAverage { Length = MPeriod, VolumeFactor = 0.15m };
		_absoluteDeviationAverage = new T3MovingAverage { Length = MPeriod, VolumeFactor = 0.15m };
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// MQL applied-price default is PRICE_TYPICAL, not closing price.
		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var baseValue = _priceAverage.Process(new DecimalIndicatorValue(_priceAverage, typical, candle.OpenTime) { IsFinal = true });
		if (!_priceAverage.IsFormed)
			return;

		var deviation = typical - baseValue.GetValue<decimal>();
		var up = _signedDeviationAverage.Process(new DecimalIndicatorValue(_signedDeviationAverage, deviation, candle.OpenTime) { IsFinal = true });
		var down = _absoluteDeviationAverage.Process(new DecimalIndicatorValue(_absoluteDeviationAverage, Math.Abs(deviation), candle.OpenTime) { IsFinal = true });
		if (!_signedDeviationAverage.IsFormed || !_absoluteDeviationAverage.IsFormed)
			return;

		var numerator = up.GetValue<decimal>();
		var denominator = down.GetValue<decimal>();
		var oscillator = numerator != 0m && denominator != 0m ? 100m * numerator / denominator : 0m;
		ProcessNormalized(oscillator);
	}

	private void ProcessNormalized(decimal oscillator)
	{
		var fastStep = StepSizeFast;
		var slowStep = StepSizeSlow;
		var fastMax = oscillator + 2m * fastStep;
		var fastMin = oscillator - 2m * fastStep;
		var slowMax = oscillator + 2m * slowStep;
		var slowMin = oscillator - 2m * slowStep;

		if (!_hasStepBaseline)
		{
			_fastMin = fastMin;
			_fastMax = fastMax;
			_slowMin = slowMin;
			_slowMax = slowMax;
			_previousFast = _previousSlow = oscillator;
			_hasStepBaseline = true;
			return;
		}

		if (oscillator > _fastMax) _fastTrend = 1;
		else if (oscillator < _fastMin) _fastTrend = -1;
		if (_fastTrend > 0 && fastMin < _fastMin) fastMin = _fastMin;
		if (_fastTrend < 0 && fastMax > _fastMax) fastMax = _fastMax;

		if (oscillator > _slowMax) _slowTrend = 1;
		else if (oscillator < _slowMin) _slowTrend = -1;
		if (_slowTrend > 0 && slowMin < _slowMin) slowMin = _slowMin;
		if (_slowTrend < 0 && slowMax > _slowMax) slowMax = _slowMax;

		var fast = _fastTrend > 0 ? fastMin + fastStep : fastMax - fastStep;
		var slow = _slowTrend > 0 ? slowMin + slowStep : slowMax - slowStep;
		var prevFast = _previousFast.Value;
		var prevSlow = _previousSlow.Value;

		_fastMin = fastMin; _fastMax = fastMax;
		_slowMin = slowMin; _slowMax = slowMax;
		_previousFast = fast; _previousSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var position = Position;
		if (prevFast > prevSlow && fast < slow && position <= 0m)
		{
			if (position < 0m) BuyMarket(-position);
			BuyMarket(Volume);
		}
		else if (prevFast < prevSlow && fast > slow && position >= 0m)
		{
			if (position > 0m) SellMarket(position);
			SellMarket(Volume);
		}
	}
}
