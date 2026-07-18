using System;
using System.Linq;
using System.Collections.Generic;
using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Leman Trend strategy using high/low differences smoothed by EMA.
/// Opens long when bullish pressure exceeds bearish, short otherwise.
/// </summary>
public class LeManTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _min;
	private readonly StrategyParam<int> _midle;
	private readonly StrategyParam<int> _max;
	private readonly StrategyParam<int> _periodEma;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highMin;
	private Highest _highMidle;
	private Highest _highMax;
	private Lowest _lowMin;
	private Lowest _lowMidle;
	private Lowest _lowMax;
	private ExponentialMovingAverage _bullsEma;
	private ExponentialMovingAverage _bearsEma;

	private decimal _prevBulls;
	private decimal _prevBears;

	public int Min
	{
		get => _min.Value;
		set => _min.Value = value;
	}

	public int Midle
	{
		get => _midle.Value;
		set => _midle.Value = value;
	}

	public int Max
	{
		get => _max.Value;
		set => _max.Value = value;
	}

	public int PeriodEma
	{
		get => _periodEma.Value;
		set => _periodEma.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LeManTrendStrategy()
	{
		_min = Param(nameof(Min), 13)
			.SetGreaterThanZero()
			.SetDisplay("Min Period", "Minimum lookback for highs/lows", "Indicator")
			.SetOptimize(5, 25, 1);

		_midle = Param(nameof(Midle), 21)
			.SetGreaterThanZero()
			.SetDisplay("Middle Period", "Middle lookback for highs/lows", "Indicator")
			.SetOptimize(10, 40, 1);

		_max = Param(nameof(Max), 34)
			.SetGreaterThanZero()
			.SetDisplay("Max Period", "Maximum lookback for highs/lows", "Indicator")
			.SetOptimize(20, 60, 1);

		_periodEma = Param(nameof(PeriodEma), 3)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Smoothing period for bulls/bears", "Indicator")
			.SetOptimize(2, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_highMin = default;
		_highMidle = default;
		_highMax = default;
		_lowMin = default;
		_lowMidle = default;
		_lowMax = default;
		_bullsEma = default;
		_bearsEma = default;
		_prevBulls = default;
		_prevBears = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highMin = new Highest { Length = Min };
		_highMidle = new Highest { Length = Midle };
		_highMax = new Highest { Length = Max };
		_lowMin = new Lowest { Length = Min };
		_lowMidle = new Lowest { Length = Midle };
		_lowMax = new Lowest { Length = Max };
		_bullsEma = new ExponentialMovingAverage { Length = PeriodEma };
		_bearsEma = new ExponentialMovingAverage { Length = PeriodEma };

		Indicators.Add(_highMin);
		Indicators.Add(_highMidle);
		Indicators.Add(_highMax);
		Indicators.Add(_lowMin);
		Indicators.Add(_lowMidle);
		Indicators.Add(_lowMax);
		Indicators.Add(_bullsEma);
		Indicators.Add(_bearsEma);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closeInput = new DecimalIndicatorValue(_highMin, candle.ClosePrice, candle.OpenTime) { IsFinal = true };

		var highMinVal = _highMin.Process(closeInput).ToDecimal();
		var highMidleVal = _highMidle.Process(new DecimalIndicatorValue(_highMidle, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var highMaxVal = _highMax.Process(new DecimalIndicatorValue(_highMax, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var lowMinVal = _lowMin.Process(new DecimalIndicatorValue(_lowMin, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var lowMidleVal = _lowMidle.Process(new DecimalIndicatorValue(_lowMidle, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var lowMaxVal = _lowMax.Process(new DecimalIndicatorValue(_lowMax, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_highMax.IsFormed || !_lowMax.IsFormed)
			return;

		var hh = (candle.HighPrice - highMinVal) + (candle.HighPrice - highMidleVal) + (candle.HighPrice - highMaxVal);
		var ll = (lowMinVal - candle.LowPrice) + (lowMidleVal - candle.LowPrice) + (lowMaxVal - candle.LowPrice);

		var bullsVal = _bullsEma.Process(new DecimalIndicatorValue(_bullsEma, hh, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var bearsVal = _bearsEma.Process(new DecimalIndicatorValue(_bearsEma, ll, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_bullsEma.IsFormed || !_bearsEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevBulls <= _prevBears && bullsVal > bearsVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevBulls >= _prevBears && bullsVal < bearsVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevBulls = bullsVal;
		_prevBears = bearsVal;
	}
}
