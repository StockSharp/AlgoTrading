
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
/// Stellar Lite ICT strategy that combines Silver Bullet and 2022 model setups.
/// The strategy reads ICT style order flow concepts on finished candles
/// and places partial take profits with adaptive stop management.
/// </summary>
public class StellarLiteIctEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframeType;
	private readonly StrategyParam<int> _higherMaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _liquidityLookback;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<decimal> _tp1Ratio;
	private readonly StrategyParam<decimal> _tp2Ratio;
	private readonly StrategyParam<decimal> _tp3Ratio;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<bool> _moveToBreakEven;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<bool> _useSilverBullet;
	private readonly StrategyParam<bool> _use2022Model;
	private readonly StrategyParam<bool> _useOteEntry;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _oteLowerLevel;

	private SimpleMovingAverage _higherMa = null!;
	private AverageTrueRange _atr = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal? _lastHtfMa;
	private decimal? _previousHtfMa;
	private Sides? _currentBias;
	private decimal _priceStep;
	private decimal _volumeStep;

	private readonly ICandleMessage[] _history = new ICandleMessage[20];
	private int _historyCount;
	private decimal _latestHighest;
	private decimal _latestLowest;
	private decimal _latestAtr;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _tp1;
	private decimal? _tp2;
	private decimal? _tp3;
	private decimal _initialVolume;
	private bool _tp1Hit;
	private bool _tp2Hit;
	private bool _tp3Hit;
	private bool _trailingActive;

	/// <summary>
	/// Initializes a new instance of the <see cref="StellarLiteIctEaStrategy"/>.
	/// </summary>
	public StellarLiteIctEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Entry Candle", "Primary timeframe used for entries", "General");

		_higherTimeframeType = Param(nameof(HigherTimeframeType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher Timeframe", "Timeframe used for directional bias", "General");

		_higherMaPeriod = Param(nameof(HigherMaPeriod), 200)
			.SetDisplay("Higher MA Period", "Moving average length for higher timeframe bias", "Bias")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Average True Range lookback", "Volatility")
			.SetCanOptimize(true);

		_liquidityLookback = Param(nameof(LiquidityLookback), 120)
			.SetDisplay("Liquidity Lookback", "Number of candles to detect liquidity pools", "Structure")
			.SetCanOptimize(true);

		_atrThreshold = Param(nameof(AtrThreshold), 0.5m)
			.SetDisplay("ATR Threshold", "Maximum candle range relative to ATR", "Structure")
			.SetCanOptimize(true);

		_tp1Ratio = Param(nameof(Tp1Ratio), 1m)
			.SetDisplay("TP1 Risk Reward", "Risk reward multiplier for the first target", "Targets")
			.SetCanOptimize(true);

		_tp2Ratio = Param(nameof(Tp2Ratio), 2m)
			.SetDisplay("TP2 Risk Reward", "Risk reward multiplier for the second target", "Targets")
			.SetCanOptimize(true);

		_tp3Ratio = Param(nameof(Tp3Ratio), 3m)
			.SetDisplay("TP3 Risk Reward", "Risk reward multiplier for the final target", "Targets")
			.SetCanOptimize(true);

		_tp1Percent = Param(nameof(Tp1Percent), 50m)
			.SetDisplay("TP1 Close %", "Percentage of volume closed at the first target", "Targets")
			.SetCanOptimize(true);

		_tp2Percent = Param(nameof(Tp2Percent), 25m)
			.SetDisplay("TP2 Close %", "Percentage of volume closed at the second target", "Targets")
			.SetCanOptimize(true);

		_tp3Percent = Param(nameof(Tp3Percent), 25m)
			.SetDisplay("TP3 Close %", "Percentage of volume closed at the final target", "Targets")
			.SetCanOptimize(true);

		_moveToBreakEven = Param(nameof(MoveToBreakEven), true)
			.SetDisplay("Break Even After TP1", "Move the stop to break even after the first partial", "Protection");

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 1m)
			.SetDisplay("Break Even Offset", "Additional price steps added to the break even stop", "Protection")
			.SetCanOptimize(true);

		_trailingDistance = Param(nameof(TrailingDistance), 10m)
			.SetDisplay("Trailing Distance", "Price steps used after TP2 for trailing stop", "Protection")
			.SetCanOptimize(true);

		_useSilverBullet = Param(nameof(UseSilverBullet), true)
			.SetDisplay("Use Silver Bullet", "Enable the Silver Bullet setup", "Structure");

		_use2022Model = Param(nameof(Use2022Model), true)
			.SetDisplay("Use 2022 Model", "Enable the 2022 model setup", "Structure");

		_useOteEntry = Param(nameof(UseOteEntry), true)
			.SetDisplay("Use OTE Entry", "Place entries inside the optimal trade entry zone", "Structure");

		_riskPercent = Param(nameof(RiskPercent), 0.25m)
			.SetDisplay("Risk %", "Risk percentage of account equity used to size trades", "Risk")
			.SetCanOptimize(true);

		_oteLowerLevel = Param(nameof(OteLowerLevel), 0.618m)
			.SetDisplay("OTE Lower", "Lower Fibonacci level used for the entry", "Structure")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Primary candle type used to generate entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type that provides directional bias.
	/// </summary>
	public DataType HigherTimeframeType
	{
		get => _higherTimeframeType.Value;
		set => _higherTimeframeType.Value = value;
	}

	/// <summary>
	/// Higher timeframe moving average period.
	/// </summary>
	public int HigherMaPeriod
	{
		get => _higherMaPeriod.Value;
		set => _higherMaPeriod.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to search for liquidity pools.
	/// </summary>
	public int LiquidityLookback
	{
		get => _liquidityLookback.Value;
		set => _liquidityLookback.Value = value;
	}

	/// <summary>
	/// Maximum allowed candle range relative to ATR to confirm consolidation.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for the first target.
	/// </summary>
	public decimal Tp1Ratio
	{
		get => _tp1Ratio.Value;
		set => _tp1Ratio.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for the second target.
	/// </summary>
	public decimal Tp2Ratio
	{
		get => _tp2Ratio.Value;
		set => _tp2Ratio.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for the third target.
	/// </summary>
	public decimal Tp3Ratio
	{
		get => _tp3Ratio.Value;
		set => _tp3Ratio.Value = value;
	}

	/// <summary>
	/// Percentage of the position closed at TP1.
	/// </summary>
	public decimal Tp1Percent
	{
		get => _tp1Percent.Value;
		set => _tp1Percent.Value = value;
	}

	/// <summary>
	/// Percentage of the position closed at TP2.
	/// </summary>
	public decimal Tp2Percent
	{
		get => _tp2Percent.Value;
		set => _tp2Percent.Value = value;
	}

	/// <summary>
	/// Percentage of the position closed at TP3.
	/// </summary>
	public decimal Tp3Percent
	{
		get => _tp3Percent.Value;
		set => _tp3Percent.Value = value;
	}

	/// <summary>
	/// Enables moving the stop to break even after TP1.
	/// </summary>
	public bool MoveToBreakEven
	{
		get => _moveToBreakEven.Value;
		set => _moveToBreakEven.Value = value;
	}

	/// <summary>
	/// Additional price steps added to the break even stop.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Distance in price steps for the trailing stop activated after TP2.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Enables the Silver Bullet setup.
	/// </summary>
	public bool UseSilverBullet
	{
		get => _useSilverBullet.Value;
		set => _useSilverBullet.Value = value;
	}

	/// <summary>
	/// Enables the 2022 model setup.
	/// </summary>
	public bool Use2022Model
	{
		get => _use2022Model.Value;
		set => _use2022Model.Value = value;
	}

	/// <summary>
	/// Enables the optimal trade entry calculation.
	/// </summary>
	public bool UseOteEntry
	{
		get => _useOteEntry.Value;
		set => _useOteEntry.Value = value;
	}

	/// <summary>
	/// Risk percentage used for dynamic position sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Lower bound of the OTE retracement window.
	/// </summary>
	public decimal OteLowerLevel
	{
		get => _oteLowerLevel.Value;
		set => _oteLowerLevel.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType), (Security, HigherTimeframeType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherMa = null!;
		_atr = null!;
		_highest = null!;
		_lowest = null!;

		_lastHtfMa = null;
		_previousHtfMa = null;
		_currentBias = null;
		_priceStep = 1m;
		_volumeStep = 1m;

		Array.Clear(_history, 0, _history.Length);
		_historyCount = 0;
		_latestHighest = 0m;
		_latestLowest = 0m;
		_latestAtr = 0m;

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security;
		_priceStep = security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_volumeStep = security?.VolumeStep ?? 1m;
		if (_volumeStep <= 0m)
			_volumeStep = 1m;

		_higherMa = new SimpleMovingAverage
		{
			Length = HigherMaPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_highest = new Highest
		{
			Length = LiquidityLookback
		};

		_lowest = new Lowest
		{
			Length = LiquidityLookback
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(ProcessMainCandle)
			.Start();

		var higherSubscription = SubscribeCandles(HigherTimeframeType);
		higherSubscription
			.Bind(_higherMa, ProcessHigherCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousHtfMa = _lastHtfMa;
		_lastHtfMa = maValue;

		if (_previousHtfMa is not decimal prev || _lastHtfMa is not decimal current)
			return;

		if (candle.ClosePrice > current && current > prev)
		{
			_currentBias = Sides.Buy;
		}
		else if (candle.ClosePrice < current && current < prev)
		{
			_currentBias = Sides.Sell;
		}
		else
		{
			_currentBias = null;
		}
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestValue = _highest.Process(candle);
		var lowestValue = _lowest.Process(candle);
		var atrValue = _atr.Process(candle);

		StoreCandle(candle);

		ManageActivePosition(candle);

		if (!_highest.IsFormed || !_lowest.IsFormed || !atrValue.IsFinal)
			return;

		_latestHighest = highestValue.ToDecimal();
		_latestLowest = lowestValue.ToDecimal();
		_latestAtr = atrValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (_currentBias is not Sides bias)
			return;

		if (!TryBuildSetup(candle, bias, out var setup))
			return;

		if (!TryCalculateVolume(setup.Stop, setup.Entry, out var volume))
			return;

		_initialVolume = volume;
		_entryPrice = setup.Entry;
		_stopPrice = setup.Stop;
		_tp1 = setup.Tp1;
		_tp2 = setup.Tp2;
		_tp3 = setup.Tp3;
		_tp1Hit = false;
		_tp2Hit = false;
		_tp3Hit = false;
		_trailingActive = false;

		if (bias == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetPositionState();
			return;
		}

		if (_stopPrice is decimal stop)
		{
			if (Position > 0 && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
			if (Position < 0 && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
		}

		if (_tp3Hit)
			return;

		var trailingSteps = TrailingDistance * _priceStep;
		if (_trailingActive && trailingSteps > 0m && _stopPrice is decimal trailingStop)
		{
			if (Position > 0)
			{
				var candidate = candle.ClosePrice - trailingSteps;
				if (candidate > trailingStop)
					_stopPrice = candidate;
			}
			else if (Position < 0)
			{
				var candidate = candle.ClosePrice + trailingSteps;
				if (candidate < trailingStop)
					_stopPrice = candidate;
			}
		}

		TryHandleTarget(candle, _tp1, ref _tp1Hit, Tp1Percent, () =>
		{
			if (!MoveToBreakEven || _entryPrice is not decimal entry)
				return;
			var offset = BreakEvenOffset * _priceStep;
			_stopPrice = Position > 0 ? entry + offset : entry - offset;
		});

		TryHandleTarget(candle, _tp2, ref _tp2Hit, Tp2Percent, () =>
		{
			if (TrailingDistance <= 0m || _stopPrice is not decimal currentStop)
				return;
			_trailingActive = true;
			_stopPrice = currentStop;
		});

		TryHandleTarget(candle, _tp3, ref _tp3Hit, Tp3Percent, () =>
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
			ResetPositionState();
		});
	}

	private void TryHandleTarget(ICandleMessage candle, decimal? targetPrice, ref bool targetHit, decimal percent, Action onSuccess)
	{
		if (targetHit)
			return;

		if (targetPrice is not decimal target)
			return;

		var hit = Position > 0
			? candle.HighPrice >= target
			: candle.LowPrice <= target;

		if (!hit)
			return;

		if (percent > 0m && TryGetPartialVolume(percent, out var volume))
		{
			if (Position > 0)
				SellMarket(Math.Min(volume, Math.Abs(Position)));
			else if (Position < 0)
				BuyMarket(Math.Min(volume, Math.Abs(Position)));
		}

		targetHit = true;
		onSuccess();
	}

	private bool TryBuildSetup(ICandleMessage candle, Sides bias, out TradeSetup setup)
	{
		if (UseSilverBullet && TryBuildSilverBulletSetup(candle, bias, out setup))
			return true;

		if (Use2022Model && TryBuild2022Setup(candle, bias, out setup))
			return true;

		setup = default;
		return false;
	}

	private bool TryBuildSilverBulletSetup(ICandleMessage candle, Sides bias, out TradeSetup setup)
	{
		setup = default;
		if (!TryCollectStructureSignals(candle, bias, bias, out var entryZone))
			return false;

		setup = CreateTradeSetup(bias, entryZone.Entry, entryZone.Stop);
		return setup.IsValid;
	}

	private bool TryBuild2022Setup(ICandleMessage candle, Sides bias, out TradeSetup setup)
	{
		setup = default;
		var opposite = bias == Sides.Buy ? Sides.Sell : Sides.Buy;
		if (!TryCollectStructureSignals(candle, bias, opposite, out var entryZone))
			return false;

		setup = CreateTradeSetup(bias, entryZone.Entry, entryZone.Stop);
		return setup.IsValid;
	}

	private bool TryCollectStructureSignals(ICandleMessage candle, Sides executionBias, Sides liquidityBias, out EntryZone zone)
	{
		zone = default;

		if (_historyCount < 3)
			return false;

		var liquidityLevel = liquidityBias == Sides.Sell ? _latestHighest : _latestLowest;
		if (!CheckLiquiditySweep(liquidityBias, liquidityLevel))
			return false;

		if (!CheckMarketStructureShift(executionBias))
			return false;

		if (!TryFindFairValueGap(out var gapHigh, out var gapLow))
			return false;

		if (!CheckNdOg(candle))
			return false;

		var lower = Math.Min(gapHigh, gapLow);
		var upper = Math.Max(gapHigh, gapLow);
		if (candle.ClosePrice < lower || candle.ClosePrice > upper)
			return false;

		var entry = CalculateEntryPrice(lower, upper, executionBias);
		var stop = FindProtectiveStopLoss(executionBias);
		if (entry == stop)
			return false;

		zone = new EntryZone(entry, stop);
		return true;
	}

	private TradeSetup CreateTradeSetup(Sides bias, decimal entry, decimal stop)
	{
		var distance = Math.Abs(entry - stop);
		if (distance <= 0m)
			return default;

		return new TradeSetup
		{
			IsValid = true,
			Side = bias,
			Entry = entry,
			Stop = stop,
			Tp1 = CalculateTarget(entry, stop, Tp1Ratio, bias),
			Tp2 = CalculateTarget(entry, stop, Tp2Ratio, bias),
			Tp3 = CalculateTarget(entry, stop, Tp3Ratio, bias)
		};
	}

	private bool CheckLiquiditySweep(Sides bias, decimal liquidityLevel)
	{
		if (_historyCount < 2 || liquidityLevel == 0m)
			return false;

		var current = _history[0];
		var previous = _history[1];
		if (current == null || previous == null)
			return false;

		return bias == Sides.Buy
			? current.ClosePrice < liquidityLevel && previous.LowPrice <= liquidityLevel
			: current.ClosePrice > liquidityLevel && previous.HighPrice >= liquidityLevel;
	}

	private bool CheckMarketStructureShift(Sides bias)
	{
		if (_historyCount < 3)
			return false;

		var current = _history[0];
		var previous = _history[1];
		var anchor = _history[2];
		if (current == null || previous == null || anchor == null)
			return false;

		return bias == Sides.Buy
			? current.ClosePrice > previous.HighPrice && previous.ClosePrice < anchor.OpenPrice
			: current.ClosePrice < previous.LowPrice && previous.ClosePrice > anchor.OpenPrice;
	}

	private bool TryFindFairValueGap(out decimal gapHigh, out decimal gapLow)
	{
		gapHigh = 0m;
		gapLow = 0m;
		var limit = Math.Min(_historyCount, 10);
		for (var i = 2; i < limit; i++)
		{
			var older = _history[i];
			var mid = _history[i - 1];
			var anchor = _history[i - 2];
			if (older == null || mid == null || anchor == null)
				continue;

			if (older.HighPrice < anchor.LowPrice && mid.ClosePrice > older.HighPrice)
			{
				gapHigh = anchor.LowPrice;
				gapLow = older.HighPrice;
				return true;
			}

			if (older.LowPrice > anchor.HighPrice && mid.ClosePrice < older.LowPrice)
			{
				gapHigh = older.LowPrice;
				gapLow = anchor.HighPrice;
				return true;
			}
		}

		return false;
	}

	private bool CheckNdOg(ICandleMessage candle)
	{
		if (_latestAtr <= 0m)
			return false;

		var range = candle.HighPrice - candle.LowPrice;
		return range <= _latestAtr * AtrThreshold;
	}

	private decimal CalculateEntryPrice(decimal lower, decimal upper, Sides bias)
	{
		var range = upper - lower;
		if (range <= 0m)
			return lower;

		if (!UseOteEntry)
			return lower + range / 2m;

		return bias == Sides.Buy
			? lower + range * OteLowerLevel
			: upper - range * OteLowerLevel;
	}

	private decimal FindProtectiveStopLoss(Sides bias)
	{
		var depth = Math.Min(_historyCount, 10);
		decimal? level = null;
		for (var i = 0; i < depth; i++)
		{
			var candle = _history[i];
			if (candle == null)
				continue;

			if (bias == Sides.Buy)
			{
				if (level is null || candle.LowPrice < level)
					level = candle.LowPrice;
			}
			else
			{
				if (level is null || candle.HighPrice > level)
					level = candle.HighPrice;
			}
		}

		if (level is null)
			return 0m;

		return bias == Sides.Buy ? level.Value - _priceStep : level.Value + _priceStep;
	}

	private decimal CalculateTarget(decimal entry, decimal stop, decimal ratio, Sides bias)
	{
		var distance = Math.Abs(entry - stop);
		var move = distance * ratio;
		return bias == Sides.Buy ? entry + move : entry - move;
	}

	private bool TryCalculateVolume(decimal stop, decimal entry, out decimal volume)
	{
		volume = 0m;
		var security = Security;
		if (security == null)
			return false;

		var distance = Math.Abs(entry - stop);
		if (distance <= 0m)
			return false;

		var priceStep = _priceStep;
		var stepValue = security.StepPrice ?? priceStep;
		if (priceStep <= 0m)
			priceStep = 1m;
		if (stepValue <= 0m)
			stepValue = priceStep;

		var baseVolume = Volume;
		if (baseVolume <= 0m)
			baseVolume = _volumeStep;

		var risk = RiskPercent;
		if (risk > 0m && Portfolio is not null)
		{
			var equity = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
			if (equity > 0m)
			{
				var riskAmount = equity * (risk / 100m);
				var riskPerUnit = (distance / priceStep) * stepValue;
				if (riskPerUnit > 0m)
				{
					baseVolume = riskAmount / riskPerUnit;
				}
			}
		}

		var volumeStep = _volumeStep;
		var normalized = Math.Floor(baseVolume / volumeStep) * volumeStep;
		if (normalized < volumeStep)
			normalized = volumeStep;

		var maxVolume = security.MaxVolume ?? normalized;
		if (normalized > maxVolume)
			normalized = maxVolume;

		volume = normalized;
		return volume > 0m;
	}

	private bool TryGetPartialVolume(decimal percent, out decimal volume)
	{
		volume = 0m;
		if (_initialVolume <= 0m)
			return false;

		var security = Security;
		if (security == null)
			return false;

		var step = _volumeStep;
		var raw = Math.Abs(_initialVolume) * percent / 100m;
		var normalized = Math.Floor(raw / step) * step;
		if (normalized < step)
			return false;

		var available = Math.Abs(Position);
		if (normalized > available)
			normalized = available;

		if (normalized <= 0m)
			return false;

		volume = normalized;
		return true;
	}

	private void StoreCandle(ICandleMessage candle)
	{
		for (var i = _history.Length - 1; i > 0; i--)
		{
			_history[i] = _history[i - 1];
		}

		_history[0] = candle;
		if (_historyCount < _history.Length)
			_historyCount++;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_tp1 = null;
		_tp2 = null;
		_tp3 = null;
		_initialVolume = 0m;
		_tp1Hit = false;
		_tp2Hit = false;
		_tp3Hit = false;
		_trailingActive = false;
	}

	private readonly struct TradeSetup
	{
		public bool IsValid { get; init; }
		public Sides Side { get; init; }
		public decimal Entry { get; init; }
		public decimal Stop { get; init; }
		public decimal Tp1 { get; init; }
		public decimal Tp2 { get; init; }
		public decimal Tp3 { get; init; }
	}

	private readonly record struct EntryZone(decimal Entry, decimal Stop);
}

