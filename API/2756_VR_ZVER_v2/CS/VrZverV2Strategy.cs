using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the VR-ZVER v2 expert advisor with triple EMA confirmation and stochastic/RSI filters.
/// </summary>
public class VrZverV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _breakevenPips;
	private readonly StrategyParam<bool> _allowLongs;
	private readonly StrategyParam<bool> _allowShorts;
	private readonly StrategyParam<bool> _useMovingAverageFilter;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _verySlowMaPeriod;
	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSmooth;
	private readonly StrategyParam<decimal> _stochasticUpperLevel;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;

	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;
	private ExponentialMovingAverage _verySlowMa = null!;
	private StochasticOscillator _stochastic = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _trailingStop;
	private bool _breakevenActivated;

	public VrZverV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for signal generation", "General");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetDisplay("Fixed Volume", "Use fixed volume when greater than zero", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 10m)
		.SetDisplay("Risk %", "Risk percentage used when fixed volume is zero", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Full stop distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 70m)
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)", "Additional distance before trailing updates", "Risk");

		_breakevenPips = Param(nameof(BreakevenPips), 20m)
		.SetDisplay("Breakeven (pips)", "Move stop to entry after this profit", "Risk");

		_allowLongs = Param(nameof(AllowLongs), true)
		.SetDisplay("Allow Longs", "Permit buy trades", "General");

		_allowShorts = Param(nameof(AllowShorts), true)
		.SetDisplay("Allow Shorts", "Permit sell trades", "General");

		_useMovingAverageFilter = Param(nameof(UseMovingAverageFilter), true)
		.SetDisplay("Use MA Filter", "Require triple EMA alignment", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Length of the slow EMA", "Indicators");

		_verySlowMaPeriod = Param(nameof(VerySlowMaPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Very Slow EMA", "Length of the very slow EMA", "Indicators");

		_useStochastic = Param(nameof(UseStochastic), true)
		.SetDisplay("Use Stochastic", "Enable stochastic confirmation", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 42)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Number of periods for %K", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Smoothing period for %D", "Indicators");

		_stochasticSmooth = Param(nameof(StochasticSmooth), 7)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Smooth", "Final smoothing for stochastic", "Indicators");

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 60m)
		.SetDisplay("Stochastic Upper", "Upper threshold for short signals", "Indicators");

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 40m)
		.SetDisplay("Stochastic Lower", "Lower threshold for long signals", "Indicators");

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI filter", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of the RSI", "Indicators");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 60m)
		.SetDisplay("RSI Upper", "Upper threshold for short entries", "Indicators");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 40m)
		.SetDisplay("RSI Lower", "Lower threshold for long entries", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public decimal BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	public bool AllowLongs
	{
		get => _allowLongs.Value;
		set => _allowLongs.Value = value;
	}

	public bool AllowShorts
	{
		get => _allowShorts.Value;
		set => _allowShorts.Value = value;
	}

	public bool UseMovingAverageFilter
	{
		get => _useMovingAverageFilter.Value;
		set => _useMovingAverageFilter.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int VerySlowMaPeriod
	{
		get => _verySlowMaPeriod.Value;
		set => _verySlowMaPeriod.Value = value;
	}

	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	public int StochasticSmooth
	{
		get => _stochasticSmooth.Value;
		set => _stochasticSmooth.Value = value;
	}

	public decimal StochasticUpperLevel
	{
		get => _stochasticUpperLevel.Value;
		set => _stochasticUpperLevel.Value = value;
	}

	public decimal StochasticLowerLevel
	{
		get => _stochasticLowerLevel.Value;
		set => _stochasticLowerLevel.Value = value;
	}

	public bool UseRsi
	{
		get => _useRsi.Value;
		set => _useRsi.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare pip size once the security is available.
		_pipSize = CalculatePipSize();
		// Clear any leftover state from previous runs.
		ResetTradeState();

		// Instantiate indicators with the configured lengths.
		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_verySlowMa = new ExponentialMovingAverage { Length = VerySlowMaPeriod };
		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Smooth = StochasticSmooth,
		};
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		// Subscribe to candle updates and bind indicators to a single handler.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _verySlowMa, _stochastic, _rsi, ProcessCandle)
		.Start();

		// Draw indicators and trades when a chart area is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _verySlowMa);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal verySlowMaValue, IIndicatorValue stochasticValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Skip processing when the strategy is not ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseMovingAverageFilter && (!_fastMa.IsFormed || !_slowMa.IsFormed || !_verySlowMa.IsFormed))
		return;

		if (UseStochastic && !_stochastic.IsFormed)
		return;

		if (UseRsi && !_rsi.IsFormed)
		return;

		// Manage the active position before evaluating new signals.
		UpdateRiskManagement(candle);

		// Aggregate votes from all enabled filters.
		var filters = 0;
		var upVotes = 0;
		var downVotes = 0;

		if (UseMovingAverageFilter)
		{
			filters++;

			if (fastMaValue > slowMaValue && slowMaValue > verySlowMaValue)
			upVotes++;
		else if (fastMaValue < slowMaValue && slowMaValue < verySlowMaValue)
			downVotes++;
		}

		if (UseStochastic)
		{
			var stoch = (StochasticOscillatorValue)stochasticValue;
			if (stoch.K is not decimal stochK || stoch.D is not decimal stochD)
			return;

			filters++;

			if (stochD < stochK && StochasticLowerLevel > stochK)
			upVotes++;
			if (stochD > stochK && StochasticUpperLevel < stochK)
			downVotes++;
		}

		if (UseRsi)
		{
			filters++;

			if (rsiValue < RsiLowerLevel)
			upVotes++;
			if (rsiValue > RsiUpperLevel)
			downVotes++;
		}

		if (filters == 0)
		return;

		var longSignal = AllowLongs && upVotes == filters;
		var shortSignal = AllowShorts && downVotes == filters;

		// Only open a new trade when there is no active position.
		if (Position == 0)
		{
			if (longSignal)
			TryEnterLong(candle);
			else if (shortSignal)
			TryEnterShort(candle);
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var volume = CalculateEntryVolume();
		if (volume <= 0m)
		return;

		// Enter a long position at market price.
		BuyMarket(volume);

		// Store trade prices for later risk management.
		_entryPrice = candle.ClosePrice;
		_breakevenActivated = false;
		_trailingStop = null;

		var stopOffset = StopLossPips > 0m ? StopLossPips * _pipSize / 1.5m : 0m;
		var takeOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		_stopPrice = stopOffset > 0m ? _entryPrice - stopOffset : null;
		_takePrice = takeOffset > 0m ? _entryPrice + takeOffset : null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var volume = CalculateEntryVolume();
		if (volume <= 0m)
		return;

		// Enter a short position at market price.
		SellMarket(volume);

		// Store trade prices for later risk management.
		_entryPrice = candle.ClosePrice;
		_breakevenActivated = false;
		_trailingStop = null;

		var stopOffset = StopLossPips > 0m ? StopLossPips * _pipSize / 1.5m : 0m;
		var takeOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		_stopPrice = stopOffset > 0m ? _entryPrice + stopOffset : null;
		_takePrice = takeOffset > 0m ? _entryPrice - takeOffset : null;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		// Manage long positions first.
		if (Position > 0)
		{
			HandleBreakevenLong(candle);
			HandleTrailingLong(candle);

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
			else if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		// Manage short positions in the same fashion.
		else if (Position < 0)
		{
			HandleBreakevenShort(candle);
			HandleTrailingShort(candle);

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
			else if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		// Reset helper state when flat.
		else
		{
			ResetTradeState();
		}
	}

	// Move the stop to breakeven for long trades once profit reaches the threshold.
	private void HandleBreakevenLong(ICandleMessage candle)
	{
		if (_breakevenActivated || BreakevenPips <= 0m)
		return;

		var trigger = _entryPrice + BreakevenPips * _pipSize;
		if (candle.HighPrice >= trigger)
		{
			_breakevenActivated = true;
			UpdateLongStop(_entryPrice);
		}
	}

	// Move the stop to breakeven for short trades once profit reaches the threshold.
	private void HandleBreakevenShort(ICandleMessage candle)
	{
		if (_breakevenActivated || BreakevenPips <= 0m)
		return;

		var trigger = _entryPrice - BreakevenPips * _pipSize;
		if (candle.LowPrice <= trigger)
		{
			_breakevenActivated = true;
			UpdateShortStop(_entryPrice);
		}
	}

	// Update trailing logic for long trades using distance and step thresholds.
	private void HandleTrailingLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m)
		return;

		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
		return;

		var step = TrailingStepPips * _pipSize;
		var desiredStop = candle.ClosePrice - distance;

		if (_trailingStop is null)
		{
			var activationPrice = _entryPrice + distance + step;
			if (candle.HighPrice >= activationPrice)
			{
				_trailingStop = desiredStop;
				UpdateLongStop(desiredStop);
			}
		}
		else if (desiredStop > _trailingStop.Value + step)
		{
			_trailingStop = desiredStop;
			UpdateLongStop(desiredStop);
		}
	}

	// Update trailing logic for short trades using distance and step thresholds.
	private void HandleTrailingShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m)
		return;

		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
		return;

		var step = TrailingStepPips * _pipSize;
		var desiredStop = candle.ClosePrice + distance;

		if (_trailingStop is null)
		{
			var activationPrice = _entryPrice - distance - step;
			if (candle.LowPrice <= activationPrice)
			{
				_trailingStop = desiredStop;
				UpdateShortStop(desiredStop);
			}
		}
		else if (desiredStop < _trailingStop.Value - step)
		{
			_trailingStop = desiredStop;
			UpdateShortStop(desiredStop);
		}
	}

	// Ensure the long stop can only move upward.
	private void UpdateLongStop(decimal newLevel)
	{
		if (_stopPrice is null || newLevel > _stopPrice.Value)
		_stopPrice = newLevel;
	}

	// Ensure the short stop can only move downward.
	private void UpdateShortStop(decimal newLevel)
	{
		if (_stopPrice is null || newLevel < _stopPrice.Value)
		_stopPrice = newLevel;
	}

	// Determine trade size using either fixed volume or risk-based sizing.
	private decimal CalculateEntryVolume()
	{
		if (FixedVolume > 0m)
		return AdjustVolume(FixedVolume);

		var stopOffset = StopLossPips > 0m ? StopLossPips * _pipSize / 1.5m : 0m;
		if (stopOffset <= 0m)
		return AdjustVolume(Volume);

		var riskVolume = GetRiskVolume(stopOffset);
		return AdjustVolume(riskVolume);
	}

	// Translate the configured risk percentage into lots based on stop distance.
	private decimal GetRiskVolume(decimal stopOffset)
	{
		if (stopOffset <= 0m)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var lossPerUnit = stopOffset / priceStep * stepPrice;
		if (lossPerUnit <= 0m)
		return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
		return 0m;

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return 0m;

		return riskAmount / lossPerUnit;
	}

	// Normalize the requested volume to instrument constraints.
	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.VolumeMin ?? step;
		var max = security.VolumeMax ?? decimal.MaxValue;

		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			var adjusted = steps * step;

			if (min <= 0m)
			min = step;

			if (adjusted < min)
			return 0m;

			return Math.Min(adjusted, max);
		}

		if (min > 0m && volume < min)
		return 0m;

		return Math.Min(volume, max);
	}

	// Mimic the MetaTrader pip conversion used in the original script.
	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		return step < 0.01m ? step * 10m : step;
	}

	// Clear cached state values when no position is active.
	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_trailingStop = null;
		_breakevenActivated = false;
	}
}
