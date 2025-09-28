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
/// Port of the MT4 "Gselector" expert adviser that studies sequences of direction changes
/// for several synthetic step sizes and evaluates the probability of a continuation move.
/// The strategy keeps probability statistics for every observed pattern, opens positions
/// when the continuation likelihood is high enough, and manages stops/takes in software to
/// emulate the behaviour of the original expert.
/// </summary>
public class GselectorPatternProbabilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _probabilityThreshold;
	private readonly StrategyParam<int> _baseDeltaPoints;
	private readonly StrategyParam<int> _deltaSteps;
	private readonly StrategyParam<int> _patternLength;
	private readonly StrategyParam<int> _stopLevels;
	private readonly StrategyParam<int> _stopDistancePoints;
	private readonly StrategyParam<decimal> _forgetFactor;
	private readonly StrategyParam<int> _minSamples;
	private readonly StrategyParam<decimal> _probabilityBuffer;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _useReinvest;
	private readonly StrategyParam<int> _volumeMode;
	private readonly StrategyParam<decimal> _percentPer10k;
	private readonly StrategyParam<decimal> _baseDeposit;
	private readonly StrategyParam<decimal> _depositStep;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _cooldownFactor;

	private List<decimal>[] _priceHistory = Array.Empty<List<decimal>>();
	private decimal[] _lastAnchor = Array.Empty<decimal>();
	private readonly Dictionary<PatternKey, PatternStatistics> _statistics = new();
	private readonly Dictionary<PatternKey, DateTimeOffset> _lastActivation = new();
	private readonly List<ActivePattern> _activePatterns = new();

	private DateTimeOffset? _lastBuyTime;
	private DateTimeOffset? _lastSellTime;
	private decimal _lastBuyProbability;
	private decimal _lastSellProbability;

	private decimal? _entryPrice;
	private int _entryDirection;
	private decimal _stopDistance;
	private decimal _takeDistance;
	private decimal _lastLongRequestPrice;
	private decimal _lastShortRequestPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="GselectorPatternProbabilityStrategy"/> class.
	/// </summary>
	public GselectorPatternProbabilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type");
		_probabilityThreshold = Param(nameof(ProbabilityThreshold), 0.8m)
		.SetDisplay("Minimum probability to open a trade");
		_baseDeltaPoints = Param(nameof(BaseDeltaPoints), 1)
		.SetDisplay("Base step (in points) used to build patterns");
		_deltaSteps = Param(nameof(DeltaSteps), 20)
		.SetDisplay("Number of delta multiples to analyse");
		_patternLength = Param(nameof(PatternLength), 10)
		.SetDisplay("Number of price comparisons per pattern");
		_stopLevels = Param(nameof(StopLevels), 1)
		.SetDisplay("Number of stop/take levels to evaluate");
		_stopDistancePoints = Param(nameof(StopDistancePoints), 25)
		.SetDisplay("Base stop distance in points");
		_forgetFactor = Param(nameof(ForgetFactor), 1.05m)
		.SetDisplay("Forgetting factor for probability statistics");
		_minSamples = Param(nameof(MinSamples), 10)
		.SetDisplay("Minimum completed observations before trading");
		_probabilityBuffer = Param(nameof(ProbabilityBuffer), 0.05m)
		.SetDisplay("Additional probability required to flip an opposite trade");
		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetDisplay("Base position volume");
		_useReinvest = Param(nameof(UseReinvest), true)
		.SetDisplay("Scale volume by portfolio changes");
		_volumeMode = Param(nameof(VolumeMode), 1)
		.SetDisplay("Lot calculation mode (0=fixed, 1=percent, 2=ladder, 3=linear)");
		_percentPer10k = Param(nameof(PercentPer10k), 3m)
		.SetDisplay("Volume percent per 10k of free equity");
		_baseDeposit = Param(nameof(BaseDeposit), 500m)
		.SetDisplay("Base deposit for ladder/linear volume modes");
		_depositStep = Param(nameof(DepositStep), 500m)
		.SetDisplay("Deposit step for ladder/linear volume modes");
		_maxVolume = Param(nameof(MaxVolume), 10000m)
		.SetDisplay("Maximum allowed volume");
		_cooldownFactor = Param(nameof(CooldownFactor), 2)
		.SetDisplay("Multiplier applied to timeframe for reactivation cooldown");
	}

	/// <summary>
	/// Data type of the candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum continuation probability required to enter a trade.
	/// </summary>
	public decimal ProbabilityThreshold
	{
		get => _probabilityThreshold.Value;
		set => _probabilityThreshold.Value = value;
	}

	/// <summary>
	/// Base distance in points used to build step-based price series.
	/// </summary>
	public int BaseDeltaPoints
	{
		get => _baseDeltaPoints.Value;
		set => _baseDeltaPoints.Value = value;
	}

	/// <summary>
	/// Number of delta multiples processed simultaneously.
	/// </summary>
	public int DeltaSteps
	{
		get => _deltaSteps.Value;
		set => _deltaSteps.Value = value;
	}

	/// <summary>
	/// Number of values stored in every synthetic series (pattern length).
	/// </summary>
	public int PatternLength
	{
		get => _patternLength.Value;
		set => _patternLength.Value = value;
	}

	/// <summary>
	/// Number of stop/take levels evaluated per pattern.
	/// </summary>
	public int StopLevels
	{
		get => _stopLevels.Value;
		set => _stopLevels.Value = value;
	}

	/// <summary>
	/// Base stop distance in points (level distance equals Base * level index).
	/// </summary>
	public int StopDistancePoints
	{
		get => _stopDistancePoints.Value;
		set => _stopDistancePoints.Value = value;
	}

	/// <summary>
	/// Forgetting factor applied to pattern statistics after each observation.
	/// </summary>
	public decimal ForgetFactor
	{
		get => _forgetFactor.Value;
		set => _forgetFactor.Value = value;
	}

	/// <summary>
	/// Minimum number of completed pattern observations before trading.
	/// </summary>
	public int MinSamples
	{
		get => _minSamples.Value;
		set => _minSamples.Value = value;
	}

	/// <summary>
	/// Extra probability advantage required to close opposite positions.
	/// </summary>
	public decimal ProbabilityBuffer
	{
		get => _probabilityBuffer.Value;
		set => _probabilityBuffer.Value = value;
	}

	/// <summary>
	/// Base order volume used when the fixed mode is selected.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Determines whether the fixed volume should be scaled with portfolio changes.
	/// </summary>
	public bool UseReinvest
	{
		get => _useReinvest.Value;
		set => _useReinvest.Value = value;
	}

	/// <summary>
	/// Selects the lot sizing model (0=fixed, 1=percent, 2=ladder, 3=linear).
	/// </summary>
	public int VolumeMode
	{
		get => _volumeMode.Value;
		set => _volumeMode.Value = value;
	}

	/// <summary>
	/// Percentage of free equity per 10 000 units used by the percent mode.
	/// </summary>
	public decimal PercentPer10k
	{
		get => _percentPer10k.Value;
		set => _percentPer10k.Value = value;
	}

	/// <summary>
	/// Base deposit used by ladder and linear sizing modes.
	/// </summary>
	public decimal BaseDeposit
	{
		get => _baseDeposit.Value;
		set => _baseDeposit.Value = value;
	}

	/// <summary>
	/// Deposit increment used in ladder/linear sizing calculations.
	/// </summary>
	public decimal DepositStep
	{
		get => _depositStep.Value;
		set => _depositStep.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the timeframe to throttle repeated pattern activations.
	/// </summary>
	public int CooldownFactor
	{
		get => _cooldownFactor.Value;
		set => _cooldownFactor.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceHistory = new List<decimal>[DeltaSteps];
		_lastAnchor = new decimal[DeltaSteps];

		for (var i = 0; i < DeltaSteps; i++)
		{
			_priceHistory[i] = new List<decimal>(PatternLength);
			_lastAnchor[i] = 0m;
		}

		_statistics.Clear();
		_lastActivation.Clear();
		_activePatterns.Clear();

		_lastBuyTime = null;
		_lastSellTime = null;
		_lastBuyProbability = 0m;
		_lastSellProbability = 0m;
		_entryPrice = null;
		_entryDirection = 0;
		_stopDistance = 0m;
		_takeDistance = 0m;
		_lastLongRequestPrice = 0m;
		_lastShortRequestPrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		for (var i = 0; i < _priceHistory.Length; i++)
		{
			_priceHistory[i]?.Clear();
			_lastAnchor[i] = 0m;
		}

		_statistics.Clear();
		_lastActivation.Clear();
		_activePatterns.Clear();
		_lastBuyTime = null;
		_lastSellTime = null;
		_lastBuyProbability = 0m;
		_lastSellProbability = 0m;
		_entryPrice = null;
		_entryDirection = 0;
		_stopDistance = 0m;
		_takeDistance = 0m;
		_lastLongRequestPrice = 0m;
		_lastShortRequestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_entryPrice = null;
			_entryDirection = 0;
			_stopDistance = 0m;
			_takeDistance = 0m;
			return;
		}

		if (_entryPrice is null)
		{
			_entryPrice = GetCurrentPrice();
			_entryDirection = Position > 0m ? 1 : -1;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateSyntheticSeries(candle);
		EvaluateActivePatterns(candle);
		ManageOpenPosition(candle);
		ProcessSignals(candle);
	}

	private void UpdateSyntheticSeries(ICandleMessage candle)
	{
		var price = candle.ClosePrice;
		var step = GetPointValue();

		for (var index = 0; index < _priceHistory.Length; index++)
		{
			var history = _priceHistory[index];
			if (history is null)
			continue;

			var requiredMove = step * BaseDeltaPoints * (index + 1);
			var anchor = _lastAnchor[index];

			if (anchor == 0m)
			{
				FillInitialHistory(history, price);
				_lastAnchor[index] = price;
				continue;
			}

			if (Math.Abs(price - anchor) <= Math.Max(step * 0.5m, requiredMove - step * 0.5m))
			continue;

			ShiftHistory(history, price);
			_lastAnchor[index] = price;

			var pattern = BuildPattern(history);
			if (pattern < 0)
			continue;

			ActivatePattern(index, pattern, candle);
		}
	}

	private void FillInitialHistory(List<decimal> history, decimal price)
	{
		history.Clear();
		for (var i = 0; i < PatternLength; i++)
		{
			history.Add(price);
		}
	}

	private void ShiftHistory(List<decimal> history, decimal price)
	{
		if (history.Count == 0)
		{
			FillInitialHistory(history, price);
			return;
		}

		history.Insert(0, price);

		if (history.Count > PatternLength)
		{
			history.RemoveAt(history.Count - 1);
		}
	}

	private int BuildPattern(List<decimal> history)
	{
		if (history.Count < PatternLength)
		return -1;

		var pattern = 0;
		var multiplier = 1;

		for (var i = 0; i < PatternLength - 1; i++)
		{
			var isDown = history[i] > history[i + 1];
			if (isDown)
			{
				pattern += multiplier;
			}

			multiplier <<= 1;
		}

		return pattern;
	}

	private void ActivatePattern(int deltaIndex, int pattern, ICandleMessage candle)
	{
		var timeFrame = CandleType.GetTimeFrame();
		var cooldown = TimeSpan.FromTicks(timeFrame.Ticks * Math.Max(1, CooldownFactor));

		for (var stopIndex = 0; stopIndex < StopLevels; stopIndex++)
		{
			var key = new PatternKey(deltaIndex, stopIndex, pattern);
			if (_lastActivation.TryGetValue(key, out var lastTime) && candle.CloseTime - lastTime < cooldown)
			continue;

			_lastActivation[key] = candle.CloseTime;
			_activePatterns.Add(new ActivePattern(key, candle.ClosePrice));
		}
	}

	private void EvaluateActivePatterns(ICandleMessage candle)
	{
		if (_activePatterns.Count == 0)
		return;

		var step = GetPointValue();

		for (var i = _activePatterns.Count - 1; i >= 0; i--)
		{
			var active = _activePatterns[i];
			var levelDistance = step * StopDistancePoints * (active.Key.StopIndex + 1);
			var upReached = candle.HighPrice - active.OriginPrice >= levelDistance;
			var downReached = active.OriginPrice - candle.LowPrice >= levelDistance;

			if (!upReached && !downReached)
			continue;

			var stats = GetStatistics(active.Key);

			if (upReached)
			{
				stats.Growth = stats.Growth / ForgetFactor + 1m;
				stats.Decline = stats.Decline / ForgetFactor;
			}
			else if (downReached)
			{
				stats.Decline = stats.Decline / ForgetFactor + 1m;
				stats.Growth = stats.Growth / ForgetFactor;
			}

			stats.Observations++;
			_statistics[active.Key] = stats;
			_activePatterns.RemoveAt(i);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m || _entryPrice is null)
		return;

		if (_entryDirection > 0)
		{
			if (_stopDistance > 0m && candle.LowPrice <= _entryPrice.Value - _stopDistance)
			{
				SellMarket(Position);
				return;
			}

			if (_takeDistance > 0m && candle.HighPrice >= _entryPrice.Value + _takeDistance)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (_entryDirection < 0)
		{
			if (_stopDistance > 0m && candle.HighPrice >= _entryPrice.Value + _stopDistance)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_takeDistance > 0m && candle.LowPrice <= _entryPrice.Value - _takeDistance)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}
	}

	private void ProcessSignals(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var step = GetPointValue();
		SignalCandidate? bestLong = null;
		SignalCandidate? bestShort = null;

		for (var deltaIndex = 0; deltaIndex < _priceHistory.Length; deltaIndex++)
		{
			var history = _priceHistory[deltaIndex];
			if (history is null || history.Count < PatternLength)
			continue;

			var pattern = BuildPattern(history);
			if (pattern < 0)
			continue;

			for (var stopIndex = 0; stopIndex < StopLevels; stopIndex++)
			{
				var key = new PatternKey(deltaIndex, stopIndex, pattern);
				if (!_statistics.TryGetValue(key, out var stats) || stats.Observations < MinSamples)
				continue;

				var upProbability = stats.Growth / (stats.Growth + stats.Decline + 0.0001m);
				var distance = step * StopDistancePoints * (stopIndex + 1);

				if (upProbability >= ProbabilityThreshold)
				{
					bestLong = ChooseBetter(bestLong, upProbability, distance, key);
				}

				var downProbability = stats.Decline / (stats.Growth + stats.Decline + 0.0001m);
				if (downProbability >= ProbabilityThreshold)
				{
					bestShort = ChooseBetter(bestShort, downProbability, distance, key);
				}
			}
		}

		if (bestLong.HasValue)
		{
			TryOpenLong(candle, bestLong.Value);
		}

		if (bestShort.HasValue)
		{
			TryOpenShort(candle, bestShort.Value);
		}
	}

	private void TryOpenLong(ICandleMessage candle, SignalCandidate candidate)
	{
		var timeFrame = CandleType.GetTimeFrame();
		var cooldown = TimeSpan.FromTicks(timeFrame.Ticks * Math.Max(1, CooldownFactor));

		if (_lastBuyTime.HasValue && candle.CloseTime - _lastBuyTime.Value < cooldown)
		return;

		if (Position > 0m)
		return;

		if (Position < 0m && candidate.Probability < _lastSellProbability + ProbabilityBuffer)
		return;

		var price = candle.ClosePrice;
		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (Math.Abs(price - _lastLongRequestPrice) <= GetPointValue() * 10m)
		return;

		BuyMarket(volume);

		_lastBuyTime = candle.CloseTime;
		_lastBuyProbability = candidate.Probability;
		_stopDistance = candidate.Distance;
		_takeDistance = candidate.Distance;
		_entryPrice = price;
		_entryDirection = 1;
		_lastLongRequestPrice = price;
	}

	private void TryOpenShort(ICandleMessage candle, SignalCandidate candidate)
	{
		var timeFrame = CandleType.GetTimeFrame();
		var cooldown = TimeSpan.FromTicks(timeFrame.Ticks * Math.Max(1, CooldownFactor));

		if (_lastSellTime.HasValue && candle.CloseTime - _lastSellTime.Value < cooldown)
		return;

		if (Position < 0m)
		return;

		if (Position > 0m && candidate.Probability < _lastBuyProbability + ProbabilityBuffer)
		return;

		var price = candle.ClosePrice;
		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		if (Position > 0m)
		{
			SellMarket(Position);
		}

		if (Math.Abs(price - _lastShortRequestPrice) <= GetPointValue() * 10m)
		return;

		SellMarket(volume);

		_lastSellTime = candle.CloseTime;
		_lastSellProbability = candidate.Probability;
		_stopDistance = candidate.Distance;
		_takeDistance = candidate.Distance;
		_entryPrice = price;
		_entryDirection = -1;
		_lastShortRequestPrice = price;
	}

	private decimal CalculateVolume()
	{
		var volume = FixedVolume;
		var portfolio = Portfolio;
		var freeEquity = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;

		if (VolumeMode == 0)
		{
			volume = FixedVolume;
		}
		else if (VolumeMode == 1)
		{
			var lots = freeEquity / 10000m * PercentPer10k / 10m;
			volume = Math.Max(0.1m, Math.Round(lots, 1));
		}
		else if (VolumeMode == 2)
		{
			var threshold = BaseDeposit;
			var lot = 0.1m;
			for (var step = 2m; step <= MaxVolume; step += 1m)
			{
				threshold += step * DepositStep;
				if (freeEquity < threshold)
				{
					lot = (step - 1m) / 10m;
					break;
				}
			}

			volume = lot;
		}
		else if (VolumeMode == 3)
		{
			var step = DepositStep <= 0m ? 1m : DepositStep;
			var adjusted = Math.Ceiling((freeEquity - BaseDeposit) / step) / 10m;
			volume = Math.Max(0.1m, adjusted);
		}

		if (!UseReinvest)
		{
			volume = FixedVolume;
		}

		volume = Math.Min(volume, MaxVolume);

		return Math.Round(Math.Max(volume, 0.1m), 2);
	}

	private decimal GetPointValue()
	{
		var security = Security;
		if (security?.PriceStep > 0m)
		return security.PriceStep;

		return 0.0001m;
	}

	private decimal GetCurrentPrice()
	{
		var lastTrade = Security?.LastTick;
		if (lastTrade != null)
		return lastTrade.Price;

		var bid = Security?.BestBid?.Price;
		if (bid.HasValue)
		return bid.Value;

		var ask = Security?.BestAsk?.Price;
		if (ask.HasValue)
		return ask.Value;

		return 0m;
	}

	private PatternStatistics GetStatistics(PatternKey key)
	{
		if (_statistics.TryGetValue(key, out var stats))
		return stats;

		stats = new PatternStatistics();
		_statistics[key] = stats;
		return stats;
	}

	private static SignalCandidate? ChooseBetter(SignalCandidate? current, decimal probability, decimal distance, PatternKey key)
	{
		if (current is null)
		return new SignalCandidate(probability, distance, key);

		return probability > current.Value.Probability
		? new SignalCandidate(probability, distance, key)
		: current;
	}

	private readonly struct PatternKey : IEquatable<PatternKey>
	{
		public PatternKey(int deltaIndex, int stopIndex, int pattern)
		{
			DeltaIndex = deltaIndex;
			StopIndex = stopIndex;
			Pattern = pattern;
		}

		public int DeltaIndex { get; }

		public int StopIndex { get; }

		public int Pattern { get; }

		public bool Equals(PatternKey other)
		{
			return DeltaIndex == other.DeltaIndex && StopIndex == other.StopIndex && Pattern == other.Pattern;
		}

		public override bool Equals(object obj)
		{
			return obj is PatternKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(DeltaIndex, StopIndex, Pattern);
		}
	}

	private sealed class PatternStatistics
	{
		public decimal Growth { get; set; }

		public decimal Decline { get; set; }

		public int Observations { get; set; }
	}

	private sealed class ActivePattern
	{
		public ActivePattern(PatternKey key, decimal originPrice)
		{
			Key = key;
			OriginPrice = originPrice;
		}

		public PatternKey Key { get; }

		public decimal OriginPrice { get; }
	}

	private readonly struct SignalCandidate
	{
		public SignalCandidate(decimal probability, decimal distance, PatternKey key)
		{
			Probability = probability;
			Distance = distance;
			Key = key;
		}

		public decimal Probability { get; }

		public decimal Distance { get; }

		public PatternKey Key { get; }
	}
}

