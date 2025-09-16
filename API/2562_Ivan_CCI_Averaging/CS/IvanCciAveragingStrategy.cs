using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI based averaging strategy converted from the Ivan expert advisor.
/// </summary>
public class IvanCciAveragingStrategy : Strategy
{
	private readonly StrategyParam<bool> _useAveraging;
	private readonly StrategyParam<int> _stopLossMaPeriod;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _useZeroBar;
	private readonly StrategyParam<decimal> _reverseLevel;
	private readonly StrategyParam<decimal> _globalSignalLevel;
	private readonly StrategyParam<decimal> _minStopDistance;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _breakEvenDistance;
	private readonly StrategyParam<decimal> _profitProtectionFactor;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci100 = null!;
	private CommodityChannelIndex _cci13 = null!;
	private SmoothedMovingAverage _stopMa = null!;

	private decimal? _lastCci100;
	private decimal? _prevCci100;
	private decimal? _lastCci13;
	private decimal? _prevCci13;

	private bool _globalBuySignal;
	private bool _globalSellSignal;
	private bool _closeAll;

	private decimal? _initialBalance;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longStop;
	private decimal _shortStop;
	private bool _longBreakEvenActivated;
	private bool _shortBreakEvenActivated;
	private bool _hasLongEntry;
	private bool _hasShortEntry;

	/// <summary>
	/// Enables additional averaging entries when short CCI pulls back.
	/// </summary>
	public bool UseAveraging
	{
		get => _useAveraging.Value;
		set => _useAveraging.Value = value;
	}

	/// <summary>
	/// Period for the smoothed moving average used as stop reference.
	/// </summary>
	public int StopLossMaPeriod
	{
		get => _stopLossMaPeriod.Value;
		set => _stopLossMaPeriod.Value = value;
	}

	/// <summary>
	/// Portfolio risk percent used to size new entries.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Use the latest candle (zero bar) instead of the previous closed bar for signals.
	/// </summary>
	public bool UseZeroBar
	{
		get => _useZeroBar.Value;
		set => _useZeroBar.Value = value;
	}

	/// <summary>
	/// Reverse level for the long term CCI that triggers full liquidation.
	/// </summary>
	public decimal ReverseLevel
	{
		get => _reverseLevel.Value;
		set => _reverseLevel.Value = value;
	}

	/// <summary>
	/// Threshold for the long term CCI global signal.
	/// </summary>
	public decimal GlobalSignalLevel
	{
		get => _globalSignalLevel.Value;
		set => _globalSignalLevel.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and stop when entering a position.
	/// </summary>
	public decimal MinStopDistance
	{
		get => _minStopDistance.Value;
		set => _minStopDistance.Value = value;
	}

	/// <summary>
	/// Minimum improvement required before trailing the stop with the MA.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Profit distance that moves the stop to break-even. Zero disables break-even.
	/// </summary>
	public decimal BreakEvenDistance
	{
		get => _breakEvenDistance.Value;
		set => _breakEvenDistance.Value = value;
	}

	/// <summary>
	/// Equity multiple that forces liquidation of all positions.
	/// </summary>
	public decimal ProfitProtectionFactor
	{
		get => _profitProtectionFactor.Value;
		set => _profitProtectionFactor.Value = value;
	}

	/// <summary>
	/// Minimum trading volume used when risk based sizing is not available.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IvanCciAveragingStrategy"/> class.
	/// </summary>
	public IvanCciAveragingStrategy()
	{
		_useAveraging = Param(nameof(UseAveraging), true)
			.SetDisplay("Use Averaging", "Allow additional averaging entries", "Signals");

		_stopLossMaPeriod = Param(nameof(StopLossMaPeriod), 36)
			.SetGreaterThanZero()
			.SetDisplay("Stop MA Period", "Length of SMMA for stop placement", "Stops");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Portfolio percent risked per trade", "Risk");

		_useZeroBar = Param(nameof(UseZeroBar), true)
			.SetDisplay("Use Zero Bar", "Use current bar values instead of previous", "Signals");

		_reverseLevel = Param(nameof(ReverseLevel), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Reverse Level", "CCI level that closes all trades", "Signals");

		_globalSignalLevel = Param(nameof(GlobalSignalLevel), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Global Level", "CCI level that creates global signal", "Signals");

		_minStopDistance = Param(nameof(MinStopDistance), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Min Stop Distance", "Minimum price gap between entry and stop", "Stops");

		_trailingStep = Param(nameof(TrailingStep), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Minimum MA progress before trailing", "Stops");

		_breakEvenDistance = Param(nameof(BreakEvenDistance), 0.0005m)
			.SetDisplay("BreakEven Distance", "Distance to move stop to entry", "Stops");

		_profitProtectionFactor = Param(nameof(ProfitProtectionFactor), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Protection", "Equity multiple to flatten positions", "Risk");

		_minimumVolume = Param(nameof(MinimumVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Fallback trade volume", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators used for signal and stop logic.
		_cci100 = new CommodityChannelIndex { Length = 100 };
		_cci13 = new CommodityChannelIndex { Length = 13 };
		_stopMa = new SmoothedMovingAverage { Length = StopLossMaPeriod };

		// Reset state variables for a new run.
		_lastCci100 = null;
		_prevCci100 = null;
		_lastCci13 = null;
		_prevCci13 = null;
		_globalBuySignal = false;
		_globalSellSignal = false;
		_closeAll = false;
		_hasLongEntry = false;
		_hasShortEntry = false;
		_longBreakEvenActivated = false;
		_shortBreakEvenActivated = false;
		_longStop = 0m;
		_shortStop = 0m;

		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci100, _cci13, _stopMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci100Value, decimal cci13Value, decimal stopMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update profit protection flag based on equity growth.
		var equity = Portfolio?.CurrentValue ?? 0m;
		if (ProfitProtectionFactor > 1m && _initialBalance.HasValue && _initialBalance.Value > 0m)
		{
			if (equity >= _initialBalance.Value * ProfitProtectionFactor)
				_closeAll = true;
		}

		// Ensure indicators are ready before using their values.
		if (!_cci100.IsFormed || !_cci13.IsFormed || !_stopMa.IsFormed)
		{
			UpdateHistory(cci100Value, cci13Value);
			return;
		}

		decimal? currentCci;
		decimal? previousCci;
		decimal? shortCci;

		// Recreate the zero/first bar selection logic from MQL.
		if (UseZeroBar)
		{
			currentCci = cci100Value;
			previousCci = _lastCci100;
			shortCci = cci13Value;
		}
		else
		{
			currentCci = _lastCci100;
			previousCci = _prevCci100;
			shortCci = _lastCci13;
		}

		if (currentCci is null || previousCci is null || shortCci is null)
		{
			UpdateHistory(cci100Value, cci13Value);
			return;
		}

		// Detect reverse conditions that require flattening the book.
		if ((previousCci.Value > ReverseLevel && currentCci.Value < ReverseLevel) ||
			(previousCci.Value < -ReverseLevel && currentCci.Value > -ReverseLevel))
		{
			_globalBuySignal = false;
			_globalSellSignal = false;
			_closeAll = true;
		}
		else if (!_closeAll)
		{
			// Generate global signals and optional averaging entries.
			if (currentCci.Value > GlobalSignalLevel && !_globalBuySignal)
			{
				_globalBuySignal = true;
				_globalSellSignal = false;
				TryEnterLong(candle, stopMaValue);
			}
			else if (currentCci.Value < -GlobalSignalLevel && !_globalSellSignal)
			{
				_globalBuySignal = false;
				_globalSellSignal = true;
				TryEnterShort(candle, stopMaValue);
			}
			else if (UseAveraging)
			{
				if (_globalBuySignal && shortCci.Value < -GlobalSignalLevel)
					TryEnterLong(candle, stopMaValue);
				else if (_globalSellSignal && shortCci.Value > GlobalSignalLevel)
					TryEnterShort(candle, stopMaValue);
			}
		}

		ManagePositions(candle, stopMaValue);

		if (_closeAll)
		{
			ClosePosition();
			_closeAll = false;
		}

		UpdateHistory(cci100Value, cci13Value);
	}

	private void ManagePositions(ICandleMessage candle, decimal stopMaValue)
	{
		if (Position > 0 && _hasLongEntry)
		{
			// Move the long stop to break-even when profit reaches the target distance.
			if (BreakEvenDistance > 0m && !_longBreakEvenActivated && candle.ClosePrice >= _longEntryPrice + BreakEvenDistance)
			{
				_longStop = _longEntryPrice;
				_longBreakEvenActivated = true;
			}

			// Trail the stop with the smoothed moving average if it keeps rising.
			if (stopMaValue < candle.ClosePrice)
			{
				if (stopMaValue - TrailingStep > _longStop)
					_longStop = stopMaValue;
			}

			if (_longStop > 0m && candle.ClosePrice <= _longStop)
			{
				SellMarket(Position);
				_hasLongEntry = false;
				_longBreakEvenActivated = false;
			}
		}
		else if (Position < 0 && _hasShortEntry)
		{
			// Move the short stop to break-even when profit reaches the target distance.
			if (BreakEvenDistance > 0m && !_shortBreakEvenActivated && candle.ClosePrice <= _shortEntryPrice - BreakEvenDistance)
			{
				_shortStop = _shortEntryPrice;
				_shortBreakEvenActivated = true;
			}

			// Trail the stop with the smoothed moving average if it keeps falling.
			if (stopMaValue > candle.ClosePrice)
			{
				if (_shortStop == 0m || stopMaValue + TrailingStep < _shortStop)
					_shortStop = stopMaValue;
			}

			if (_shortStop > 0m && candle.ClosePrice >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
				_hasShortEntry = false;
				_shortBreakEvenActivated = false;
			}
		}
	}

	private void TryEnterLong(ICandleMessage candle, decimal stopMaValue)
	{
		if (stopMaValue >= candle.ClosePrice)
			return;

		var distance = candle.ClosePrice - stopMaValue;
		if (distance < MinStopDistance)
			return;

		var volume = CalculateVolume(candle.ClosePrice, stopMaValue);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_longEntryPrice = candle.ClosePrice;
		_longStop = stopMaValue;
		_longBreakEvenActivated = false;
		_hasLongEntry = true;
		_hasShortEntry = false;
	}

	private void TryEnterShort(ICandleMessage candle, decimal stopMaValue)
	{
		if (stopMaValue <= candle.ClosePrice)
			return;

		var distance = stopMaValue - candle.ClosePrice;
		if (distance < MinStopDistance)
			return;

		var volume = CalculateVolume(candle.ClosePrice, stopMaValue);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortStop = stopMaValue;
		_shortBreakEvenActivated = false;
		_hasShortEntry = true;
		_hasLongEntry = false;
	}

	private decimal CalculateVolume(decimal entryPrice, decimal stopPrice)
	{
		var minimum = MinimumVolume > 0m ? MinimumVolume : 0m;

		if (entryPrice <= 0m || stopPrice <= 0m)
			return minimum;

		var riskPerUnit = Math.Abs(entryPrice - stopPrice);
		if (riskPerUnit <= 0m)
			return minimum;

		if (RiskPercent <= 0m)
			return minimum;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return minimum;

		var riskCapital = equity * RiskPercent / 100m;
		if (riskCapital <= 0m)
			return minimum;

		var volume = riskCapital / riskPerUnit;
		if (volume <= 0m)
			return minimum;

		return Math.Max(minimum, volume);
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_hasLongEntry = false;
		_hasShortEntry = false;
		_longBreakEvenActivated = false;
		_shortBreakEvenActivated = false;
		_longStop = 0m;
		_shortStop = 0m;
	}

	private void UpdateHistory(decimal cci100Value, decimal cci13Value)
	{
		_prevCci100 = _lastCci100;
		_lastCci100 = cci100Value;
		_prevCci13 = _lastCci13;
		_lastCci13 = cci13Value;
	}
}
