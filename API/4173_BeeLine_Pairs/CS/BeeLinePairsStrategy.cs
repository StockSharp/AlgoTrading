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
/// Pair trading strategy converted from the BeeLine MQL5 expert advisor.
/// Trades the spread between the main instrument and the secondary symbol and
/// optionally executes a cross instrument instead of two legs.
/// </summary>
public class BeeLinePairsStrategy : Strategy
{
	private readonly StrategyParam<Security> _secondSecurity;
	private readonly StrategyParam<Security> _crossSecurity;
	private readonly StrategyParam<bool> _useDirectCrossRate;
	private readonly StrategyParam<int> _trainingRange;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _signalCorrection;
	private readonly StrategyParam<decimal> _distanceMultiplier;
	private readonly StrategyParam<int> _retrainInterval;
	private readonly StrategyParam<int> _maxDeals;
	private readonly StrategyParam<decimal> _closeCorrection;
	private readonly StrategyParam<int> _correlation;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _baseHigh;
	private Lowest _baseLow;
	private Highest _secondHigh;
	private Lowest _secondLow;
	private Highest _deviationMax;

	private readonly Dictionary<DateTimeOffset, (decimal High, decimal Low, decimal Close)> _baseCache = new();
	private readonly Dictionary<DateTimeOffset, (decimal High, decimal Low, decimal Close)> _secondCache = new();
	private readonly Queue<(DateTimeOffset Time, decimal BaseClose, decimal SecondClose)> _deviationHistory = new();

	private decimal _compaction = 1m;
	private decimal _baseHighCached;
	private decimal _baseLowCached;
	private decimal _secondLowCached;
	private decimal _maxDeviation;
	private int _barsSinceOptimization;
	private decimal _previousDeviation;
	private bool _hasPreviousDeviation;

	/// <summary>
	/// Initializes a new instance of the <see cref="BeeLinePairsStrategy"/> class.
	/// </summary>
	public BeeLinePairsStrategy()
	{
		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Secondary Security", "Instrument paired with the main security", "General")
			.SetRequired();

		_crossSecurity = Param<Security>(nameof(CrossSecurity))
			.SetDisplay("Cross Security", "Optional cross instrument traded instead of two legs", "General");

		_useDirectCrossRate = Param(nameof(UseDirectCrossRate), true)
			.SetDisplay("Direct Cross Rate", "If true trade the cross directly, otherwise invert signals", "General");

		_trainingRange = Param(nameof(TrainingRange), 640)
			.SetGreaterThanZero()
			.SetDisplay("Training Range", "Number of candles used to re-estimate price compression", "Optimization")
			.SetCanOptimize(true)
			.SetOptimize(200, 1000, 100);

		_profitPercent = Param(nameof(ProfitPercent), 3m)
			.SetDisplay("Profit %", "Close all positions once portfolio profit reaches this percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_signalCorrection = Param(nameof(SignalCorrection), 0.7m)
			.SetDisplay("Signal Correction", "Multiplier applied to the maximum deviation threshold", "Optimization");

		_distanceMultiplier = Param(nameof(DistanceMultiplier), 1.2m)
			.SetDisplay("Distance Multiplier", "Multiplier defining how many bars are inspected for the signal range", "Optimization");

		_retrainInterval = Param(nameof(RetrainInterval), 120)
			.SetDisplay("Retrain Interval", "Number of finished candles before recomputing compression", "Optimization")
			.SetCanOptimize(true)
			.SetOptimize(60, 240, 30);

		_maxDeals = Param(nameof(MaxDeals), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Deals", "Maximum scaling factor applied to the base volume", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_closeCorrection = Param(nameof(CloseCorrection), 0.618034m)
			.SetDisplay("Close Correction", "Close positions when deviation shrinks below this fraction of the signal", "Risk");

		_correlation = Param(nameof(Correlation), 1)
			.SetDisplay("Correlation", "Use 1 for positively correlated symbols or -1 for inverse pairs", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "Data");
	}

	/// <summary>
	/// Secondary instrument paired with the main security.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Optional cross instrument replacing the two-leg execution.
	/// </summary>
	public Security CrossSecurity
	{
		get => _crossSecurity.Value;
		set => _crossSecurity.Value = value;
	}

	/// <summary>
	/// Trade the cross directly when true, otherwise invert signals.
	/// </summary>
	public bool UseDirectCrossRate
	{
		get => _useDirectCrossRate.Value;
		set => _useDirectCrossRate.Value = value;
	}

	/// <summary>
	/// Number of candles used to calibrate compression and signal width.
	/// </summary>
	public int TrainingRange
	{
		get => _trainingRange.Value;
		set => _trainingRange.Value = value;
	}

	/// <summary>
	/// Profit percentage that triggers a full liquidation.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the detected maximum deviation.
	/// </summary>
	public decimal SignalCorrection
	{
		get => _signalCorrection.Value;
		set => _signalCorrection.Value = value;
	}

	/// <summary>
	/// Multiplier defining how many bars are inspected when searching the signal range.
	/// </summary>
	public decimal DistanceMultiplier
	{
		get => _distanceMultiplier.Value;
		set => _distanceMultiplier.Value = value;
	}

	/// <summary>
	/// Number of finished candles before recalculating compression parameters.
	/// </summary>
	public int RetrainInterval
	{
		get => _retrainInterval.Value;
		set => _retrainInterval.Value = value;
	}

	/// <summary>
	/// Maximum scaling factor applied to the base trade volume.
	/// </summary>
	public int MaxDeals
	{
		get => _maxDeals.Value;
		set => _maxDeals.Value = value;
	}

	/// <summary>
	/// Fraction of the signal width that triggers a safety exit.
	/// </summary>
	public decimal CloseCorrection
	{
		get => _closeCorrection.Value;
		set => _closeCorrection.Value = value;
	}

	/// <summary>
	/// Correlation sign between the instruments (1 or -1).
	/// </summary>
	public int Correlation
	{
		get => _correlation.Value;
		set => _correlation.Value = value;
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (SecondSecurity != null)
			yield return (SecondSecurity, CandleType);

		if (CrossSecurity != null)
			yield return (CrossSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_baseHigh = null;
		_baseLow = null;
		_secondHigh = null;
		_secondLow = null;
		_deviationMax = null;

		_baseCache.Clear();
		_secondCache.Clear();
		_deviationHistory.Clear();

		_compaction = 1m;
		_baseHighCached = 0m;
		_baseLowCached = 0m;
		_secondLowCached = 0m;
		_maxDeviation = 0m;
		_barsSinceOptimization = 0;
		_previousDeviation = 0m;
		_hasPreviousDeviation = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SecondSecurity == null)
			throw new InvalidOperationException("Secondary security must be specified before starting the strategy.");

		_baseHigh = new Highest { Length = Math.Max(1, TrainingRange) };
		_baseLow = new Lowest { Length = Math.Max(1, TrainingRange) };
		_secondHigh = new Highest { Length = Math.Max(1, TrainingRange) };
		_secondLow = new Lowest { Length = Math.Max(1, TrainingRange) };
		_deviationMax = new Highest { Length = GetDeviationWindow() };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription
			.Bind(ProcessBaseCandle)
			.Start();

		var secondSubscription = SubscribeCandles(CandleType, security: SecondSecurity);
		secondSubscription
			.Bind(ProcessSecondCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawCandles(area, secondSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_baseHigh.Process(candle.HighPrice);
		_baseLow.Process(candle.LowPrice);

		_baseCache[candle.OpenTime] = (candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		TryProcessTime(candle.OpenTime);
	}

	private void ProcessSecondCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_secondHigh.Process(candle.HighPrice);
		_secondLow.Process(candle.LowPrice);

		_secondCache[candle.OpenTime] = (candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		TryProcessTime(candle.OpenTime);
	}

	private void TryProcessTime(DateTimeOffset openTime)
	{
		if (!_baseCache.TryGetValue(openTime, out var baseData))
			return;

		if (!_secondCache.TryGetValue(openTime, out var secondData))
			return;

		_baseCache.Remove(openTime);
		_secondCache.Remove(openTime);

		ProcessCombined(openTime, baseData, secondData);
	}

	private void ProcessCombined(DateTimeOffset time, (decimal High, decimal Low, decimal Close) baseData, (decimal High, decimal Low, decimal Close) secondData)
	{
		if (!_baseHigh.IsFormed || !_baseLow.IsFormed || !_secondHigh.IsFormed || !_secondLow.IsFormed)
			return;

		if (_barsSinceOptimization == 0 && _maxDeviation == 0m)
			PerformOptimization(time);
		else if (RetrainInterval > 0 && ++_barsSinceOptimization >= RetrainInterval)
			PerformOptimization(time);

		var deviation = CalculateDeviation(baseData.Close, secondData.Close);
		if (deviation == null)
			return;

		_deviationHistory.Enqueue((time, baseData.Close, secondData.Close));
		TrimDeviationHistory();

		_deviationMax.Process(Math.Abs(deviation.Value));
		if (_deviationMax.IsFormed)
			_maxDeviation = _deviationMax.GetCurrentValue<decimal>() * SignalCorrection;

		var previous = _previousDeviation;
		var hasPrevious = _hasPreviousDeviation;
		_previousDeviation = deviation.Value;
		_hasPreviousDeviation = true;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_maxDeviation <= 0m)
			return;

		if (ShouldClosePositions(deviation.Value))
		{
			ClosePairPositions();
			return;
		}

		if (!hasPrevious)
			return;

		TryOpenPositions(deviation.Value, previous);
	}

	private void PerformOptimization(DateTimeOffset time)
	{
		_baseHighCached = _baseHigh.GetCurrentValue<decimal>();
		_baseLowCached = _baseLow.GetCurrentValue<decimal>();
		var secondHigh = _secondHigh.GetCurrentValue<decimal>();
		_secondLowCached = _secondLow.GetCurrentValue<decimal>();

		var secondRange = secondHigh - _secondLowCached;
		var baseRange = _baseHighCached - _baseLowCached;
		_compaction = secondRange == 0m ? 1m : (baseRange / secondRange);

		_deviationMax.Length = GetDeviationWindow();
		_deviationMax.Reset();

		RecalculateMaxDeviation();

		_barsSinceOptimization = 0;
	}

	private void RecalculateMaxDeviation()
	{
		if (_deviationHistory.Count == 0)
		{
			_maxDeviation = 0m;
			return;
		}

		foreach (var entry in _deviationHistory)
		{
			var deviation = CalculateDeviation(entry.BaseClose, entry.SecondClose);
			if (deviation == null)
				continue;

			_deviationMax.Process(Math.Abs(deviation.Value));
		}

		if (_deviationMax.IsFormed)
			_maxDeviation = _deviationMax.GetCurrentValue<decimal>() * SignalCorrection;
	}

	private void TrimDeviationHistory()
	{
		var maxItems = GetDeviationWindow();
		while (_deviationHistory.Count > maxItems)
			_deviationHistory.Dequeue();
	}

	private int GetDeviationWindow()
	{
		var multiplier = Math.Max(1m, DistanceMultiplier);
		var window = (int)Math.Ceiling(TrainingRange * multiplier);
		return Math.Max(window, 1);
	}

	private decimal? CalculateDeviation(decimal baseClose, decimal secondClose)
	{
		if (_compaction <= 0m)
			return null;

		var aligned = _compaction * (secondClose - _secondLowCached) + _baseLowCached;

		if (Correlation < 0)
		{
			aligned = _baseHighCached - (aligned - _baseLowCached);
		}

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		return (baseClose - aligned) / step;
	}

	private bool ShouldClosePositions(decimal deviation)
	{
		var basePosition = GetPositionVolume(Security);
		var secondPosition = GetPositionVolume(SecondSecurity);
		var crossPosition = GetPositionVolume(CrossSecurity);

		if (CrossSecurity != null)
		{
			if (crossPosition > 0m)
				return UseDirectCrossRate ? deviation > 0m : deviation < 0m;

			if (crossPosition < 0m)
				return UseDirectCrossRate ? deviation < 0m : deviation > 0m;
		}
		else
		{
			if (Correlation > 0)
			{
				if ((basePosition > 0m || secondPosition < 0m) && deviation > 0m)
					return true;

				if ((basePosition < 0m || secondPosition > 0m) && deviation < 0m)
					return true;
			}
			else if (Correlation < 0)
			{
				if ((basePosition > 0m || secondPosition > 0m) && deviation > 0m)
					return true;

				if ((basePosition < 0m || secondPosition < 0m) && deviation < 0m)
					return true;
			}
		}

		if (CloseCorrection > 0m && _maxDeviation > 0m && Math.Abs(deviation) < CloseCorrection * _maxDeviation)
			return true;

		if (ProfitPercent > 0m && Portfolio != null)
		{
			var begin = Portfolio.BeginValue ?? Portfolio.CurrentValue ?? 0m;
			var current = Portfolio.CurrentValue ?? begin;
			var targetProfit = begin * ProfitPercent / 100m;
			if (targetProfit > 0m && current - begin >= targetProfit)
				return true;
		}

		return false;
	}

	private void ClosePairPositions()
	{
		CloseSecurityPosition(Security);

		if (CrossSecurity != null)
		{
			CloseSecurityPosition(CrossSecurity);
			return;
		}

		CloseSecurityPosition(SecondSecurity);
	}

	private void CloseSecurityPosition(Security security)
	{
		if (security == null)
			return;

		var position = GetPositionVolume(security);
		if (position > 0m)
			SellMarket(position, security);
		else if (position < 0m)
			BuyMarket(Math.Abs(position), security);
	}

	private void TryOpenPositions(decimal deviation, decimal previousDeviation)
	{
		if (Math.Abs(deviation) < _maxDeviation)
			return;

		if (Math.Abs(previousDeviation) < Math.Abs(deviation))
			return;

		var multiplier = (int)Math.Round(Math.Abs(deviation) / _maxDeviation);
		multiplier = Math.Max(1, Math.Min(MaxDeals, multiplier));

		var targetBaseVolume = multiplier * GetBaseLot();
		if (targetBaseVolume <= 0m)
			return;

		if (CrossSecurity != null)
		{
			OpenCrossPosition(deviation, targetBaseVolume);
			return;
		}

		var baseVolume = GetAdditionalVolume(Security, targetBaseVolume);
		if (baseVolume <= 0m)
			return;

		var targetSecondVolume = NormalizeVolume(targetBaseVolume * GetSecondaryVolumeRatio(), SecondSecurity);
		var secondVolume = GetAdditionalVolume(SecondSecurity, targetSecondVolume);

		var sellBase = deviation > 0m;

		if (sellBase)
		{
			SellMarket(baseVolume, Security);

			if (secondVolume > 0m)
			{
				if (Correlation > 0)
					BuyMarket(secondVolume, SecondSecurity);
				else
					SellMarket(secondVolume, SecondSecurity);
			}
		}
		else
		{
			BuyMarket(baseVolume, Security);

			if (secondVolume > 0m)
			{
				if (Correlation > 0)
					SellMarket(secondVolume, SecondSecurity);
				else
					BuyMarket(secondVolume, SecondSecurity);
			}
		}
	}

	private void OpenCrossPosition(decimal deviation, decimal targetVolume)
	{
		var crossVolume = GetAdditionalVolume(CrossSecurity, targetVolume);
		if (crossVolume <= 0m)
			return;

		var sellCross = UseDirectCrossRate ? deviation > 0m : deviation < 0m;
		if (sellCross)
			SellMarket(crossVolume, CrossSecurity);
		else
			BuyMarket(crossVolume, CrossSecurity);
	}

	private decimal GetBaseLot()
	{
		var volume = Volume;
		if (volume <= 0m)
			volume = Security?.MinVolume ?? 1m;

		return NormalizeVolume(volume, Security);
	}

	private decimal GetSecondaryVolumeRatio()
	{
		var firstTick = GetTickValue(Security);
		var secondTick = GetTickValue(SecondSecurity);

		if (secondTick <= 0m)
			return 1m;

		return _compaction * firstTick / secondTick;
	}

	private decimal GetTickValue(Security security)
	{
		if (security?.StepPrice is { } stepPrice && stepPrice > 0m)
			return stepPrice;

		return 1m;
	}

	private decimal GetAdditionalVolume(Security security, decimal targetVolume)
	{
		if (security == null)
			return 0m;

		var current = Math.Abs(GetPositionVolume(security));
		var volume = targetVolume - current;
		if (volume <= 0m)
			return 0m;

		return NormalizeVolume(volume, security);
	}

	private decimal NormalizeVolume(decimal volume, Security security)
	{
		if (security == null)
			return volume;

		if (volume <= 0m)
			return 0m;

		if (security.VolumeStep is { } step && step > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(volume / step));
			volume = steps * step;
		}

		if (security.MinVolume is { } min && min > 0m && volume < min)
			volume = min;

		if (security.MaxVolume is { } max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private decimal GetPositionVolume(Security security)
	{
		return security == null ? 0m : GetPositionValue(security, Portfolio) ?? 0m;
	}
}
