using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tomas Ratio strategy with multi-timeframe analysis.
/// </summary>
public class TomasRatioStrategyWithMultiTimeFrameAnalysisStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _deviationLength;
	private readonly StrategyParam<int> _pointsTarget;
	private readonly StrategyParam<bool> _useStdDev;

	private StandardDeviation _deviation;
	private SimpleMovingAverage _gainsAvg;
	private SimpleMovingAverage _lossesAvg;
	private SimpleMovingAverage _gainsWeightAvg;
	private SimpleMovingAverage _lossesWeightAvg;
	private SimpleMovingAverage _ma100;
	private ExponentialMovingAverage _ema720;
	private SimpleMovingAverage _buyPointsAvg;
	private SimpleMovingAverage _closePointsAvg;

	private decimal _prevHlc3;
	private decimal _prevSignal;
	private decimal _buySignalPoints;
	private decimal _buyPoints;
	private decimal _closePoints;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public int DeviationLength { get => _deviationLength.Value; set => _deviationLength.Value = value; }
	public int PointsTarget { get => _pointsTarget.Value; set => _pointsTarget.Value = value; }
	public bool UseStandardDeviation { get => _useStdDev.Value; set => _useStdDev.Value = value; }

	public TomasRatioStrategyWithMultiTimeFrameAnalysisStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_length = Param(nameof(Length), 720)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Monthly weight length", "Parameters")
			.SetCanOptimize(true);

		_deviationLength = Param(nameof(DeviationLength), 168)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Length", "Weekly deviation length", "Parameters")
			.SetCanOptimize(true);

		_pointsTarget = Param(nameof(PointsTarget), 100)
			.SetGreaterThanZero()
			.SetDisplay("Points Target", "Target points for entry", "Parameters")
			.SetCanOptimize(true);

		_useStdDev = Param(nameof(UseStandardDeviation), true)
			.SetDisplay("Use StdDev", "Enable standard deviation increment", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_deviation = default;
		_gainsAvg = default;
		_lossesAvg = default;
		_gainsWeightAvg = default;
		_lossesWeightAvg = default;
		_ma100 = default;
		_ema720 = default;
		_buyPointsAvg = default;
		_closePointsAvg = default;
		_prevHlc3 = default;
		_prevSignal = default;
		_buySignalPoints = default;
		_buyPoints = default;
		_closePoints = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_deviation = new StandardDeviation { Length = DeviationLength };
		_gainsAvg = new SimpleMovingAverage { Length = DeviationLength };
		_lossesAvg = new SimpleMovingAverage { Length = DeviationLength };
		_gainsWeightAvg = new SimpleMovingAverage { Length = Length };
		_lossesWeightAvg = new SimpleMovingAverage { Length = Length };
		_ma100 = new SimpleMovingAverage { Length = 100 };
		_ema720 = new ExponentialMovingAverage { Length = 720 };
		_buyPointsAvg = new SimpleMovingAverage { Length = 24 };
		_closePointsAvg = new SimpleMovingAverage { Length = 24 };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma100);
			DrawIndicator(area, _ema720);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var deviationValue = _deviation.Process(hlc3);
		var deviation = deviationValue.ToDecimal();
		var move = Math.Abs(hlc3 - _prevHlc3);
		var moveStrength = UseStandardDeviation && deviation != 0m ? 1m + move / deviation : 1m;

		var gainsWeight = hlc3 > _prevHlc3 ? moveStrength * moveStrength : 0m;
		var lossesWeight = hlc3 < _prevHlc3 ? moveStrength * moveStrength : 0m;
		var gains = hlc3 > _prevHlc3 ? 1m : 0m;
		var losses = hlc3 < _prevHlc3 ? 1m : 0m;

		var weightedGains = _gainsAvg.Process(gains).ToDecimal();
		var weightedLosses = _lossesAvg.Process(losses).ToDecimal();
		var dailyPositive = _gainsWeightAvg.Process(gainsWeight).ToDecimal() * Length / 7m;
		var dailyNegative = _lossesWeightAvg.Process(lossesWeight).ToDecimal() * Length / 7m;

		var averageGain = dailyPositive * weightedGains;
		var averageLoss = dailyNegative * weightedLosses;
		var signalLine = averageGain - averageLoss;

		var ma100 = _ma100.Process(signalLine).ToDecimal();
		var ema720 = _ema720.Process(candle.ClosePrice).ToDecimal();

		var inc = signalLine > _prevSignal ? signalLine : 0m;
		var dec = signalLine < ma100 ? Math.Max(signalLine, 5m) : 0m;

		_buyPoints = _buyPointsAvg.Process(inc).ToDecimal() * 24m;
		_closePoints = _closePointsAvg.Process(dec).ToDecimal() * 24m;

		if (signalLine > _prevSignal && _buyPoints > _closePoints)
			_buySignalPoints += signalLine;

		if (_buySignalPoints <= -100m)
			_buySignalPoints = -100m;

		if (_buySignalPoints >= PointsTarget && Position <= 0 && candle.ClosePrice > ema720)
		{
			BuyMarket();
			_buySignalPoints = 0m;
		}
		else if (_closePoints > _buyPoints && Position > 0)
		{
			SellMarket(Position);
		}

		_prevSignal = signalLine;
		_prevHlc3 = hlc3;
	}
}

