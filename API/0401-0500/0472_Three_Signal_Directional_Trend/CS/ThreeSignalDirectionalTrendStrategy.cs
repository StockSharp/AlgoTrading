namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Three Signal Directional Trend Strategy.
/// Three indicators vote: the MACD signal line rising or falling, the Stochastic %K (StochLength, smoothed over SmoothK) below
/// Oversold or above Overbought, and the rate of change over RocLength bars of the AvgLength SMA, averaged over AvgRocLength
/// bars, above or below zero. At least two long votes open a long and at least two short votes open a short, reversing
/// an opposite position.
/// </summary>
public class ThreeSignalDirectionalTrendStrategy : Strategy
{
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
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _kSmooth;
	private SimpleMovingAverage _rocAverage;
	private readonly List<decimal> _maHistory = [];
	private decimal? _prevSignal;

	/// <summary>
	/// SMA period of the rate of change.
	/// </summary>
	public int AvgLength
	{
		get => _avgLength.Value;
		set => _avgLength.Value = value;
	}

	/// <summary>
	/// Bars of the rate of change.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// Averaging period of the rate of change.
	/// </summary>
	public int AvgRocLength
	{
		get => _avgRocLength.Value;
		set => _avgRocLength.Value = value;
	}

	/// <summary>
	/// Stochastic lookback.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Smoothing of %K.
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
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdAvgLength
	{
		get => _macdAvgLength.Value;
		set => _macdAvgLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public ThreeSignalDirectionalTrendStrategy()
	{
		_avgLength = Param(nameof(AvgLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Average Length", "SMA period of the rate of change", "ROC");

		_rocLength = Param(nameof(RocLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length", "Bars of the rate of change", "ROC");

		_avgRocLength = Param(nameof(AvgRocLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Average ROC Length", "Averaging period of the rate of change", "ROC");

		_stochLength = Param(nameof(StochLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Stochastic lookback", "Stochastic");

		_smoothK = Param(nameof(SmoothK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth K", "Smoothing of %K", "Stochastic");

		_overbought = Param(nameof(Overbought), 80m)
			.SetDisplay("Overbought", "Stochastic overbought level", "Stochastic");

		_oversold = Param(nameof(Oversold), 20m)
			.SetDisplay("Oversold", "Stochastic oversold level", "Stochastic");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "MACD");

		_macdAvgLength = Param(nameof(MacdAvgLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line period", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_maHistory.Clear();
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_maHistory.Clear();
		_prevSignal = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdAvgLength },
		};
		var stochK = new StochasticK { Length = StochLength };
		var sma = new SimpleMovingAverage { Length = AvgLength };

		_kSmooth = new SimpleMovingAverage { Length = SmoothK };
		_rocAverage = new SimpleMovingAverage { Length = AvgRocLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, stochK, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;

		decimal? k = null;
		if (stochValue.IsFormed)
		{
			var smoothed = _kSmooth.Process(stochValue.GetValue<decimal>(), time, true);
			if (_kSmooth.IsFormed)
				k = smoothed.ToDecimal();
		}

		decimal? avgRoc = null;
		if (smaValue.IsFormed)
		{
			var ma = smaValue.GetValue<decimal>();
			_maHistory.Add(ma);
			if (_maHistory.Count > RocLength + 1)
				_maHistory.RemoveAt(0);

			if (_maHistory.Count == RocLength + 1 && _maHistory[0] != 0m)
			{
				var roc = (ma - _maHistory[0]) / _maHistory[0] * 100m;
				var averaged = _rocAverage.Process(roc, time, true);
				if (_rocAverage.IsFormed)
					avgRoc = averaged.ToDecimal();
			}
		}

		decimal? signal = null;
		if (macdValue.IsFormed && macdValue is IMovingAverageConvergenceDivergenceSignalValue { Signal: decimal s })
			signal = s;

		var prevSignal = _prevSignal;
		if (signal is not null)
			_prevSignal = signal;

		if (k is not decimal kValue || avgRoc is not decimal rocValue || signal is not decimal signalValue || prevSignal is not decimal previous)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longVotes = (signalValue > previous ? 1 : 0) + (kValue < Oversold ? 1 : 0) + (rocValue > 0m ? 1 : 0);
		var shortVotes = (signalValue < previous ? 1 : 0) + (kValue > Overbought ? 1 : 0) + (rocValue < 0m ? 1 : 0);

		if (longVotes >= 2 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortVotes >= 2 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
