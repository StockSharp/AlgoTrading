using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three Signal Directional Trend Strategy - combines MACD, Stochastic, and MA ROC signals.
/// </summary>
public class ThreeSignalDirectionalTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _avgLength;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _avgRocLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdAvgLength;

	private SimpleMovingAverage _avg;
	private RateOfChange _roc;
	private SimpleMovingAverage _avgRoc;
	private StochasticOscillator _stochastic;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal _prevMacdAvg;
	private bool _macdInitialized;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// SMA length for MA speed.
	/// </summary>
	public int AvgLength
	{
		get => _avgLength.Value;
		set => _avgLength.Value = value;
	}

	/// <summary>
	/// ROC length for MA speed.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// SMA length of ROC.
	/// </summary>
	public int AvgRocLength
	{
		get => _avgRocLength.Value;
		set => _avgRocLength.Value = value;
	}

	/// <summary>
	/// Stochastic length.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Stochastic smoothing length.
	/// </summary>
	public int SmoothK
	{
		get => _smoothK.Value;
		set => _smoothK.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdAvgLength
	{
		get => _macdAvgLength.Value;
		set => _macdAvgLength.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ThreeSignalDirectionalTrendStrategy"/>.
	/// </summary>
	public ThreeSignalDirectionalTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_avgLength = Param(nameof(AvgLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Average Length", "SMA length for MA speed", "MA Speed")
		.SetCanOptimize(true);

		_rocLength = Param(nameof(RocLength), 1)
		.SetGreaterThanZero()
		.SetDisplay("ROC Length", "ROC length for MA speed", "MA Speed")
		.SetCanOptimize(true);

		_avgRocLength = Param(nameof(AvgRocLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Avg ROC Length", "SMA length of ROC", "MA Speed")
		.SetCanOptimize(true);

		_stochLength = Param(nameof(StochLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Stoch Length", "Stochastic length", "Stochastic")
		.SetCanOptimize(true);

		_smoothK = Param(nameof(SmoothK), 3)
		.SetGreaterThanZero()
		.SetDisplay("Smooth K", "Stochastic smoothing", "Stochastic")
		.SetCanOptimize(true);

		_overbought = Param(nameof(Overbought), 80m)
		.SetDisplay("Overbought", "Stochastic overbought level", "Stochastic")
		.SetCanOptimize(true);

		_oversold = Param(nameof(Oversold), 20m)
		.SetDisplay("Oversold", "Stochastic oversold level", "Stochastic")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast Length", "Fast EMA length", "MACD")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow Length", "Slow EMA length", "MACD")
		.SetCanOptimize(true);

		_macdAvgLength = Param(nameof(MacdAvgLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Avg Length", "Signal EMA length", "MACD")
		.SetCanOptimize(true);
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
		_prevMacdAvg = 0m;
		_macdInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_avg = new SimpleMovingAverage { Length = AvgLength };
		_roc = new RateOfChange { Length = RocLength };
		_avgRoc = new SimpleMovingAverage { Length = AvgRocLength };

		_stochastic = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = SmoothK }
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdAvgLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([_stochastic, _macd], ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var avgValue = _avg.Process(candle);
		var rocValue = _roc.Process(avgValue.ToDecimal(), candle.ServerTime, true);
		var avgRocValue = _avgRoc.Process(rocValue, candle.ServerTime, true);

		if (!_stochastic.IsFormed || !_macd.IsFormed || !_avgRoc.IsFormed)
		return;

		var stochTyped = (StochasticOscillatorValue)values[0];
		if (stochTyped.D is not decimal kValue)
		return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[1];
		if (macdTyped.Signal is not decimal macdAvg)
		return;

		var avgRoc = avgRocValue.ToDecimal();

		var longCount = 0;
		var shortCount = 0;

		if (_macdInitialized)
		{
			if (macdAvg > _prevMacdAvg)
			longCount++;
			else if (macdAvg < _prevMacdAvg)
			shortCount++;
		}
		else
		{
			_macdInitialized = true;
		}

		_prevMacdAvg = macdAvg;

		if (kValue <= Oversold)
		longCount++;
		else if (kValue >= Overbought)
		shortCount++;

		if (avgRoc > 0)
		longCount++;
		else if (avgRoc < 0)
		shortCount++;

		if (longCount >= 2 && Position <= 0)
		BuyMarket();
		else if (shortCount >= 2 && Position >= 0)
		SellMarket();
	}
}
