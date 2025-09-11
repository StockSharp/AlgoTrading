namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD Enhanced Strategy MTF with Stop Loss.
/// </summary>
public class MacdEnhancedMtfWithStopLossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _crossScore;
	private readonly StrategyParam<decimal> _indicatorScore;
	private readonly StrategyParam<decimal> _histogramScore;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<int> _stopLossPeriod;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private MovingAverageConvergenceDivergenceSignal _macdHtf;
	private AverageTrueRange _atr;
	private DataType _htfType;
	private decimal _calc;

	private readonly ScoreState _baseScore = new();
	private readonly ScoreState _htfScoreCalc = new();
	private decimal _htfScore;
	private decimal _prevResult;
	private int _posDir = 1;
	private decimal _tUp;
	private decimal _tDown;
	private decimal _stLine;
	private decimal _prevHtfClose;
	private bool _stInitialized;

	private sealed class ScoreState
	{
		public bool Initialized;
		public decimal PrevIndi;
		public decimal PrevSignal;
		public decimal PrevHist;
		public decimal PrevCountCross;

		public decimal Compute(decimal indi, decimal signal, decimal crossScore, decimal indiside, decimal histside)
		{
			var hist = indi - signal;

			if (!Initialized)
			{
				PrevIndi = indi;
				PrevSignal = signal;
				PrevHist = hist;
				Initialized = true;
				return 0m;
			}

			decimal analyse = 0m;

			analyse += indi > PrevIndi ? (hist > PrevHist ? indiside + histside : indiside)
				: (hist == PrevHist ? indiside : indiside - histside);

			analyse += indi < PrevIndi ? (hist < PrevHist ? -(indiside + histside) : -indiside)
				: (hist == PrevHist ? -indiside : -(indiside - histside));

			analyse += indi == PrevIndi ? (hist > PrevHist ? histside : (hist < PrevHist ? -histside : 0m)) : 0m;

			decimal countCross = 0m;
			if (indi >= signal && PrevIndi < PrevSignal)
				countCross = crossScore;
			else if (indi <= signal && PrevIndi > PrevSignal)
				countCross = -crossScore;

			countCross += PrevCountCross * 0.6m;
			analyse += countCross;

			PrevCountCross = countCross;
			PrevIndi = indi;
			PrevSignal = signal;
			PrevHist = hist;

			return analyse;
		}
	}

	public MacdEnhancedMtfWithStopLossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for calculation", "General");
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("MACD Fast Length", "Fast EMA period", "MACD");
		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("MACD Slow Length", "Slow EMA period", "MACD");
		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("MACD Signal Length", "Signal EMA period", "MACD");
		_crossScore = Param(nameof(CrossScore), 10m)
			.SetDisplay("Cross Score", "Score for MACD cross", "Logic");
		_indicatorScore = Param(nameof(IndicatorScore), 8m)
			.SetDisplay("Indicator Direction Score", "Score for MACD direction", "Logic");
		_histogramScore = Param(nameof(HistogramScore), 2m)
			.SetDisplay("Histogram Direction Score", "Score for histogram direction", "Logic");
		_stopLossFactor = Param(nameof(StopLossFactor), 1.2m)
			.SetDisplay("Stop Loss Factor", "ATR multiplier", "Stop");
		_stopLossPeriod = Param(nameof(StopLossPeriod), 10)
			.SetDisplay("Stop Loss Period", "ATR period", "Stop");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public decimal CrossScore { get => _crossScore.Value; set => _crossScore.Value = value; }
	public decimal IndicatorScore { get => _indicatorScore.Value; set => _indicatorScore.Value = value; }
	public decimal HistogramScore { get => _histogramScore.Value; set => _histogramScore.Value = value; }
	public decimal StopLossFactor { get => _stopLossFactor.Value; set => _stopLossFactor.Value = value; }
	public int StopLossPeriod { get => _stopLossPeriod.Value; set => _stopLossPeriod.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var tf = CandleType.TimeFrame ?? TimeSpan.FromMinutes(1);
		return [(Security, CandleType), (Security, GetHigherTimeFrame(tf))];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var timeFrame = CandleType.TimeFrame ?? TimeSpan.FromMinutes(1);
		_htfType = GetHigherTimeFrame(timeFrame);
		_calc = GetCalcValue(timeFrame);

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength },
		};

		_macdHtf = new()
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength },
		};

		_atr = new AverageTrueRange { Length = StopLossPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_macd, ProcessBase).Start();

		var htfSubscription = SubscribeCandles(_htfType);
		htfSubscription.BindEx(_macdHtf, _atr, ProcessHtf).Start();
	}

	private void ProcessBase(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine)
			return;
		if (macdTyped.Signal is not decimal signalLine)
			return;

		var anlys = _baseScore.Compute(macdLine, signalLine, CrossScore, IndicatorScore, HistogramScore);
		var result = (_htfScore * _calc + anlys) / (_calc + 1m);
		var longCondition = result != _prevResult && result > 0m;
		var shortCondition = result != _prevResult && result < 0m;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_posDir = 1;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_posDir = -1;
		}
		else if (Position > 0 && candle.ClosePrice < _stLine)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && candle.ClosePrice > _stLine)
		{
			BuyMarket(-Position);
		}

		_prevResult = result;
	}

	private void ProcessHtf(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine)
			return;
		if (macdTyped.Signal is not decimal signalLine)
			return;

		_htfScore = _htfScoreCalc.Compute(macdLine, signalLine, CrossScore, IndicatorScore, HistogramScore);

		var atr = atrValue.ToDecimal();
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var upt = hl2 - StopLossFactor * atr;
		var dnt = hl2 + StopLossFactor * atr;

		if (!_stInitialized)
		{
			_tUp = upt;
			_tDown = dnt;
			_stInitialized = true;
		}
		else
		{
			_tUp = _prevHtfClose > _tUp ? Math.Max(upt, _tUp) : upt;
			_tDown = _prevHtfClose < _tDown ? Math.Min(dnt, _tDown) : dnt;
		}

		_stLine = _posDir > 0 ? _tUp : _tDown;
		_prevHtfClose = candle.ClosePrice;
	}

	private static DataType GetHigherTimeFrame(TimeSpan tf)
	{
		if (tf == TimeSpan.FromMinutes(1))
			return TimeSpan.FromMinutes(5).TimeFrame();
		if (tf == TimeSpan.FromMinutes(3))
			return TimeSpan.FromMinutes(15).TimeFrame();
		if (tf == TimeSpan.FromMinutes(5))
			return TimeSpan.FromMinutes(15).TimeFrame();
		if (tf == TimeSpan.FromMinutes(15) || tf == TimeSpan.FromMinutes(30) || tf == TimeSpan.FromMinutes(45))
			return TimeSpan.FromMinutes(60).TimeFrame();
		if (tf == TimeSpan.FromMinutes(60) || tf == TimeSpan.FromMinutes(120) || tf == TimeSpan.FromMinutes(180))
			return TimeSpan.FromMinutes(240).TimeFrame();
		if (tf == TimeSpan.FromMinutes(240))
			return TimeSpan.FromDays(1).TimeFrame();
		if (tf == TimeSpan.FromDays(1))
			return TimeSpan.FromDays(7).TimeFrame();
		return TimeSpan.FromDays(7).TimeFrame();
	}

	private static decimal GetCalcValue(TimeSpan tf)
	{
		if (tf == TimeSpan.FromMinutes(1) || tf == TimeSpan.FromMinutes(3))
			return 5m;
		if (tf == TimeSpan.FromMinutes(5))
			return 3m;
		if (tf == TimeSpan.FromMinutes(15) || tf == TimeSpan.FromMinutes(30) || tf == TimeSpan.FromMinutes(45) || tf == TimeSpan.FromMinutes(60))
			return 4m;
		if (tf == TimeSpan.FromMinutes(120) || tf == TimeSpan.FromMinutes(180))
			return 3m;
		if (tf == TimeSpan.FromMinutes(240))
			return 6m;
		if (tf == TimeSpan.FromDays(1))
			return 5m;
		return 1m;
	}
}
