using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pattern learning strategy converted from the original FutureMA and FutureMACD experts.
/// Builds an in-memory database of repeated indicator sequences and trades breakouts from historical fractal distances.
/// </summary>
public class FuturePatternMemoryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<PatternSource> _source;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _analysisBars;
	private readonly StrategyParam<int> _fractalDepth;
	private readonly StrategyParam<int> _minimumMatches;
	private readonly StrategyParam<decimal> _minimumTakeProfit;
	private readonly StrategyParam<int> _normalizationFactor;
	private readonly StrategyParam<decimal> _forgettingFactor;
	private readonly StrategyParam<decimal> _statisticalTakeRatio;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<bool> _manualMode;
	private readonly StrategyParam<bool> _allowAddOn;
	private readonly StrategyParam<decimal> _volume;

	private SmoothedMovingAverage _fastSmma = null!;
	private SmoothedMovingAverage _slowSmma = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _pointValue;

	private readonly Queue<int> _patternWindow = new();
	private readonly Queue<CandleSnapshot> _fractalWindow = new();
	private readonly Dictionary<string, PatternStats> _patterns = new();

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	private decimal? _pendingLongStopDistance;
	private decimal? _pendingLongTakeDistance;
	private decimal? _pendingShortStopDistance;
	private decimal? _pendingShortTakeDistance;

	/// <summary>
	/// Initializes a new instance of the <see cref="FuturePatternMemoryStrategy"/> class.
	/// </summary>
	public FuturePatternMemoryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for pattern detection", "General");

		_source = Param(nameof(Source), PatternSource.MaSpread)
		.SetDisplay("Pattern Source", "Indicator used to build pattern signatures", "Pattern Memory");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMMA", "Length of the fast smoothed MA", "MA Source");

		_slowMaLength = Param(nameof(SlowMaLength), 24)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMMA", "Length of the slow smoothed MA", "MA Source");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD Source");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD Source");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period for MACD", "MACD Source");

		_analysisBars = Param(nameof(AnalysisBars), 8)
		.SetGreaterThanZero()
		.SetDisplay("Pattern Length", "Bars used to describe a pattern", "Pattern Memory");

		_fractalDepth = Param(nameof(FractalDepth), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fractal Depth", "Bars inspected for swing extremes", "Pattern Memory");

		_minimumMatches = Param(nameof(MinimumMatches), 5)
		.SetNotNegative()
		.SetDisplay("Minimum Matches", "Required occurrences before trading", "Pattern Memory");

		_minimumTakeProfit = Param(nameof(MinimumTakeProfit), 30m)
		.SetNotNegative()
		.SetDisplay("Minimum TP (pts)", "Ignore signals below this expected reward", "Execution");

		_normalizationFactor = Param(nameof(NormalizationFactor), 10)
		.SetGreaterThanZero()
		.SetDisplay("Normalization", "Scaling factor for indicator differences", "Pattern Memory");

		_forgettingFactor = Param(nameof(ForgettingFactor), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Memory Weight", "Strength of the new observation", "Pattern Memory");

		_statisticalTakeRatio = Param(nameof(StatisticalTakeRatio), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("TP Ratio", "Fraction of expected swing used for take profit", "Execution");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), false)
		.SetDisplay("Trailing Stop", "Enable adaptive trailing management", "Risk");

		_manualMode = Param(nameof(ManualMode), false)
		.SetDisplay("Manual Mode", "Disable automated orders", "Execution");

		_allowAddOn = Param(nameof(AllowAddOn), true)
		.SetDisplay("Allow Add-on", "Permit scaling in on repeated signals", "Execution");

		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume in lots", "Execution");
	}

	/// <summary>
	/// Candle series processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicator used to build the pattern signature.
	/// </summary>
	public PatternSource Source
	{
		get => _source.Value;
		set => _source.Value = value;
	}

	/// <summary>
	/// Fast smoothed moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow smoothed moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
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
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Number of bars used to form a pattern signature.
	/// </summary>
	public int AnalysisBars
	{
		get => _analysisBars.Value;
		set => _analysisBars.Value = value;
	}

	/// <summary>
	/// Number of historical bars used to measure the swing range.
	/// </summary>
	public int FractalDepth
	{
		get => _fractalDepth.Value;
		set => _fractalDepth.Value = value;
	}

	/// <summary>
	/// Required number of recorded occurrences before trading.
	/// </summary>
	public int MinimumMatches
	{
		get => _minimumMatches.Value;
		set => _minimumMatches.Value = value;
	}

	/// <summary>
	/// Minimum take profit in points before executing a trade.
	/// </summary>
	public decimal MinimumTakeProfit
	{
		get => _minimumTakeProfit.Value;
		set => _minimumTakeProfit.Value = value;
	}

	/// <summary>
	/// Indicator scaling factor used when hashing the pattern.
	/// </summary>
	public int NormalizationFactor
	{
		get => _normalizationFactor.Value;
		set => _normalizationFactor.Value = value;
	}

	/// <summary>
	/// Weight applied to new observations in the pattern memory.
	/// </summary>
	public decimal ForgettingFactor
	{
		get => _forgettingFactor.Value;
		set => _forgettingFactor.Value = value;
	}

	/// <summary>
	/// Ratio between measured swing distance and the target take profit.
	/// </summary>
	public decimal StatisticalTakeRatio
	{
		get => _statisticalTakeRatio.Value;
		set => _statisticalTakeRatio.Value = value;
	}

	/// <summary>
	/// Enables adaptive trailing of open positions.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// When true the strategy stops issuing automatic orders.
	/// </summary>
	public bool ManualMode
	{
		get => _manualMode.Value;
		set => _manualMode.Value = value;
	}

	/// <summary>
	/// Allow adding to an existing position when the pattern repeats.
	/// </summary>
	public bool AllowAddOn
	{
		get => _allowAddOn.Value;
		set => _allowAddOn.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_patternWindow.Clear();
		_fractalWindow.Clear();
		_patterns.Clear();
		ResetPositionTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSmma = new SmoothedMovingAverage { Length = FastMaLength };
		_slowSmma = new SmoothedMovingAverage { Length = SlowMaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		_pointValue = Security?.MinPriceStep ?? Security?.PriceStep ?? 0m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
			{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage active orders before recording the new observation.
		UpdatePositionManagement(candle);

		if (!TryGetNormalizedDifference(candle, out var normalizedValue))
			{
			UpdateFractalWindow(candle);
			return;
		}

		// Store normalized difference for the current bar.
		_patternWindow.Enqueue(normalizedValue);
		while (_patternWindow.Count > AnalysisBars)
			_patternWindow.Dequeue();

		if (_patternWindow.Count == AnalysisBars && TryGetFractalDistances(out var fractalUp, out var fractalDown))
			{
			var key = BuildPatternKey(_patternWindow);

			if (!_patterns.TryGetValue(key, out var stats))
				{
				stats = new PatternStats();
				_patterns.Add(key, stats);
			}

			// Update the weighted memory with the latest swing measurement.
			UpdatePatternStats(stats, fractalUp, fractalDown);

			// Evaluate whether the refreshed statistics justify a new order.
			EvaluateEntry(stats, candle);
		}

		UpdateFractalWindow(candle);
	}

	private bool TryGetNormalizedDifference(ICandleMessage candle, out int normalized)
	{
		normalized = 0;

		var point = _pointValue > 0m ? _pointValue : 1m;

		decimal rawDiff;

		switch (Source)
		{
		case PatternSource.MaSpread:
			{
				var median = (candle.HighPrice + candle.LowPrice) / 2m;
				var fastValue = _fastSmma.Process(median, candle.OpenTime, true);
				var slowValue = _slowSmma.Process(median, candle.OpenTime, true);

				if (!_fastSmma.IsFormed || !_slowSmma.IsFormed)
					return false;

				rawDiff = slowValue.ToDecimal() - fastValue.ToDecimal();
				break;
			}

		case PatternSource.MacdHistogram:
			{
				var macdValue = (MovingAverageConvergenceDivergenceSignalValue)_macd.Process(candle.ClosePrice, candle.OpenTime, true);

				if (!_macd.IsFormed)
					return false;

				rawDiff = macdValue.Macd - macdValue.Signal;
				break;
			}

		default:
			return false;
		}

		var scaled = NormalizationFactor * rawDiff / (100m * point);
		normalized = (int)Math.Round((double)scaled, MidpointRounding.AwayFromZero);
		return true;
	}

	private bool TryGetFractalDistances(out decimal fractalUp, out decimal fractalDown)
	{
		fractalUp = 0m;
		fractalDown = 0m;

		if (_fractalWindow.Count < FractalDepth)
			return false;

		var point = _pointValue > 0m ? _pointValue : 1m;

		var enumerator = _fractalWindow.GetEnumerator();
		if (!enumerator.MoveNext())
			return false;

		var oldest = enumerator.Current;
		var maxHigh = oldest.High;
		var minLow = oldest.Low;

		while (enumerator.MoveNext())
			{
			var snapshot = enumerator.Current;

			if (snapshot.High > maxHigh)
				maxHigh = snapshot.High;

			if (snapshot.Low < minLow)
				minLow = snapshot.Low;
		}

		fractalUp = (maxHigh - oldest.Open) / point;
		fractalDown = (oldest.Open - minLow) / point;
		return true;
	}

	private void UpdatePatternStats(PatternStats stats, decimal fractalUp, decimal fractalDown)
	{
		var memory = ForgettingFactor;

		if (fractalUp >= fractalDown)
			{
			stats.BuyMatches++;
			stats.BuyTakeProfit = ApplyWeightedAverage(stats.BuyTakeProfit, fractalUp, memory);
			stats.SellTakeProfit = ApplyWeightedAverage(stats.SellTakeProfit, -fractalUp, memory);
		}

		if (fractalDown >= fractalUp)
			{
			stats.SellMatches++;
			stats.SellTakeProfit = ApplyWeightedAverage(stats.SellTakeProfit, fractalDown, memory);
			stats.BuyTakeProfit = ApplyWeightedAverage(stats.BuyTakeProfit, -fractalDown, memory);
		}
	}

	private static decimal ApplyWeightedAverage(decimal current, decimal sample, decimal memory)
	{
		return (current + sample * memory) / (1m + memory);
	}

	private void EvaluateEntry(PatternStats stats, ICandleMessage candle)
	{
		if (ManualMode)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = _pointValue > 0m ? _pointValue : 1m;
		var buyStrength = stats.BuyTakeProfit;
		var sellStrength = stats.SellTakeProfit;

		var canBuy = buyStrength >= sellStrength && stats.BuyMatches > MinimumMatches && buyStrength > MinimumTakeProfit;
		var canSell = sellStrength >= buyStrength && stats.SellMatches > MinimumMatches && sellStrength > MinimumTakeProfit;

		if (canBuy)
			{
			var stopDistance = buyStrength * step;
			var takeDistance = buyStrength * StatisticalTakeRatio * step;

			if (stopDistance > 0m && takeDistance > 0m)
				EnterLong(candle, stopDistance, takeDistance);
		}
		else if (canSell)
			{
			var stopDistance = sellStrength * step;
			var takeDistance = sellStrength * StatisticalTakeRatio * step;

			if (stopDistance > 0m && takeDistance > 0m)
				EnterShort(candle, stopDistance, takeDistance);
		}
	}

	private void EnterLong(ICandleMessage candle, decimal stopDistance, decimal takeDistance)
	{
		if (Volume <= 0m)
			return;

		var volume = Volume;

		if (Position > 0m)
			{
			if (!AllowAddOn)
				return;
		}
		else if (Position < 0m)
			{
			volume += Math.Abs(Position);
			_pendingShortStopDistance = null;
			_pendingShortTakeDistance = null;
			_shortStop = null;
			_shortTarget = null;
		}

		if (volume <= 0m)
			return;

		_pendingLongStopDistance = stopDistance;
		_pendingLongTakeDistance = takeDistance;

		// Issue a market order to align the position with the bullish expectation.
		BuyMarket(volume);
	}

	private void EnterShort(ICandleMessage candle, decimal stopDistance, decimal takeDistance)
	{
		if (Volume <= 0m)
			return;

		var volume = Volume;

		if (Position < 0m)
			{
			if (!AllowAddOn)
				return;
		}
		else if (Position > 0m)
			{
			volume += Position;
			_pendingLongStopDistance = null;
			_pendingLongTakeDistance = null;
			_longStop = null;
			_longTarget = null;
		}

		if (volume <= 0m)
			return;

		_pendingShortStopDistance = stopDistance;
		_pendingShortTakeDistance = takeDistance;

		// Issue a market order to align the position with the bearish expectation.
		SellMarket(volume);
	}

	private void UpdatePositionManagement(ICandleMessage candle)
	{
		if (Position > 0m)
			{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
				{
				SellMarket(Position);
				ResetPositionTargets();
				return;
			}

			if (_longTarget is decimal target && candle.HighPrice >= target)
				{
				SellMarket(Position);
				ResetPositionTargets();
				return;
			}

			if (EnableTrailingStop && _longTarget is decimal take && PositionPrice is decimal entry)
				{
				var trailingStep = (take - entry) / 4m;
				if (trailingStep > 0m && candle.ClosePrice > entry + trailingStep)
					{
					var candidate = candle.ClosePrice - trailingStep;
					if (_longStop is not decimal current || candidate > current)
						_longStop = candidate;
				}
			}
		}
		else if (Position < 0m)
			{
			var absPosition = Math.Abs(Position);

			if (_shortStop is decimal stop && candle.HighPrice >= stop)
				{
				BuyMarket(absPosition);
				ResetPositionTargets();
				return;
			}

			if (_shortTarget is decimal target && candle.LowPrice <= target)
				{
				BuyMarket(absPosition);
				ResetPositionTargets();
				return;
			}

			if (EnableTrailingStop && _shortTarget is decimal take && PositionPrice is decimal entry)
				{
				var trailingStep = (entry - take) / 4m;
				if (trailingStep > 0m && candle.ClosePrice < entry - trailingStep)
					{
					var candidate = candle.ClosePrice + trailingStep;
					if (_shortStop is not decimal current || candidate < current)
						_shortStop = candidate;
				}
			}
		}
	}

	private void UpdateFractalWindow(ICandleMessage candle)
	{
		_fractalWindow.Enqueue(new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice));
		while (_fractalWindow.Count > FractalDepth)
			_fractalWindow.Dequeue();
	}

	private static string BuildPatternKey(IEnumerable<int> values)
	{
		var builder = new StringBuilder();
		var first = true;
		foreach (var value in values)
		{
			if (!first)
				builder.Append('_');

			builder.Append(value);
			first = false;
		}

		return builder.ToString();
	}

	private void ResetPositionTargets()
	{
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_pendingLongStopDistance = null;
		_pendingLongTakeDistance = null;
		_pendingShortStopDistance = null;
		_pendingShortTakeDistance = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
			{
			if (PositionPrice is decimal entry)
				{
				if (_pendingLongStopDistance is decimal stop && _pendingLongTakeDistance is decimal take)
					{
					_longStop = entry - stop;
					_longTarget = entry + take;
					_pendingLongStopDistance = null;
					_pendingLongTakeDistance = null;
				}
			}

			_shortStop = null;
			_shortTarget = null;
			_pendingShortStopDistance = null;
			_pendingShortTakeDistance = null;
		}
		else if (Position < 0m)
			{
			if (PositionPrice is decimal entry)
				{
				if (_pendingShortStopDistance is decimal stop && _pendingShortTakeDistance is decimal take)
					{
					_shortStop = entry + stop;
					_shortTarget = entry - take;
					_pendingShortStopDistance = null;
					_pendingShortTakeDistance = null;
				}
			}

			_longStop = null;
			_longTarget = null;
			_pendingLongStopDistance = null;
			_pendingLongTakeDistance = null;
		}
		else
		{
			ResetPositionTargets();
		}
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low)
		{
			Open = open;
			High = high;
			Low = low;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
	}

	private sealed class PatternStats
	{
		public int BuyMatches;
		public decimal BuyTakeProfit;
		public int SellMatches;
		public decimal SellTakeProfit;
	}

	/// <summary>
	/// Data source used to build pattern hashes.
	/// </summary>
	public enum PatternSource
	{
		/// <summary>
		/// Use the spread between slow and fast SMMAs.
		/// </summary>
		MaSpread,

		/// <summary>
		/// Use the MACD histogram (main minus signal).
		/// </summary>
		MacdHistogram
	}
}
