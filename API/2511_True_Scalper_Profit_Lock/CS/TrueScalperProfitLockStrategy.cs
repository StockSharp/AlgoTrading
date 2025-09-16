using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// True Scalper Profit Lock strategy converted from MetaTrader 5.
/// Combines short-term exponential moving averages with RSI filters, profit locking and abandon logic.
/// </summary>
public class TrueScalperProfitLockStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<bool> _useRsiMethodA;
	private readonly StrategyParam<bool> _useRsiMethodB;
	private readonly StrategyParam<bool> _useAbandonMethodA;
	private readonly StrategyParam<bool> _useAbandonMethodB;
	private readonly StrategyParam<int> _abandonBars;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _useProfitLock;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenPoints;
	private readonly StrategyParam<bool> _liveTrading;
	private readonly StrategyParam<bool> _isMiniAccount;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _previousRsi;
	private decimal _currentVolume;
	private bool _isLongPosition;
	private bool _pendingReverseToBuy;
	private bool _pendingReverseToSell;
	private int _barsSinceEntry;
	private DateTimeOffset? _lastCandleTime;
	private bool _breakEvenApplied;

	/// <summary>
	/// Base order size expressed in lots.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI decision threshold.
	/// </summary>
	public decimal RsiThreshold
	{
		get => _rsiThreshold.Value;
		set => _rsiThreshold.Value = value;
	}

	/// <summary>
	/// Enable RSI crossing logic.
	/// </summary>
	public bool UseRsiMethodA
	{
		get => _useRsiMethodA.Value;
		set => _useRsiMethodA.Value = value;
	}

	/// <summary>
	/// Enable RSI polarity logic.
	/// </summary>
	public bool UseRsiMethodB
	{
		get => _useRsiMethodB.Value;
		set => _useRsiMethodB.Value = value;
	}

	/// <summary>
	/// Force reverse direction after abandon timeout.
	/// </summary>
	public bool UseAbandonMethodA
	{
		get => _useAbandonMethodA.Value;
		set => _useAbandonMethodA.Value = value;
	}

	/// <summary>
	/// Close the trade after abandon timeout without forcing direction.
	/// </summary>
	public bool UseAbandonMethodB
	{
		get => _useAbandonMethodB.Value;
		set => _useAbandonMethodB.Value = value;
	}

	/// <summary>
	/// Number of finished candles before abandon logic triggers.
	/// </summary>
	public int AbandonBars
	{
		get => _abandonBars.Value;
		set => _abandonBars.Value = value;
	}

	/// <summary>
	/// Enable balance based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage used in money management.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Enable break even stop adjustment.
	/// </summary>
	public bool UseProfitLock
	{
		get => _useProfitLock.Value;
		set => _useProfitLock.Value = value;
	}

	/// <summary>
	/// Profit distance that triggers break even move.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Stop offset applied once break even activates.
	/// </summary>
	public decimal BreakEvenPoints
	{
		get => _breakEvenPoints.Value;
		set => _breakEvenPoints.Value = value;
	}

	/// <summary>
	/// Use live trading sizing adjustments.
	/// </summary>
	public bool LiveTrading
	{
		get => _liveTrading.Value;
		set => _liveTrading.Value = value;
	}

	/// <summary>
	/// Treat account as mini when applying live adjustments.
	/// </summary>
	public bool IsMiniAccount
	{
		get => _isMiniAccount.Value;
		set => _isMiniAccount.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous trades allowed by the original logic.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrueScalperProfitLockStrategy"/> class.
	/// </summary>
	public TrueScalperProfitLockStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lots", "Base trade volume", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 44m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit distance in steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 80m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 90m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss distance in steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50m, 150m, 10m);

		_fastPeriod = Param(nameof(FastPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Signals");

		_slowPeriod = Param(nameof(SlowPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Signals");

		_rsiLength = Param(nameof(RsiLength), 2)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI calculation length", "Signals");

		_rsiThreshold = Param(nameof(RsiThreshold), 50m)
		.SetDisplay("RSI Threshold", "RSI boundary for polarity", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(40m, 60m, 5m);

		_useRsiMethodA = Param(nameof(UseRsiMethodA), false)
		.SetDisplay("RSI Method A", "Use RSI crossing logic", "Signals");

		_useRsiMethodB = Param(nameof(UseRsiMethodB), true)
		.SetDisplay("RSI Method B", "Use RSI polarity logic", "Signals");

		_useAbandonMethodA = Param(nameof(UseAbandonMethodA), true)
		.SetDisplay("Abandon Method A", "Force reverse after timeout", "Management");

		_useAbandonMethodB = Param(nameof(UseAbandonMethodB), false)
		.SetDisplay("Abandon Method B", "Only close after timeout", "Management");

		_abandonBars = Param(nameof(AbandonBars), 101)
		.SetGreaterThanZero()
		.SetDisplay("Abandon Bars", "Bars before abandon logic", "Management");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Money Management", "Enable balance based sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Risk percentage per trade", "Risk");

		_useProfitLock = Param(nameof(UseProfitLock), true)
		.SetDisplay("Use Profit Lock", "Move stop to break even", "Risk");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 25m)
		.SetGreaterThanZero()
		.SetDisplay("BreakEven Trigger", "Profit distance before break even", "Risk");

		_breakEvenPoints = Param(nameof(BreakEvenPoints), 3m)
		.SetGreaterThanZero()
		.SetDisplay("BreakEven Offset", "Offset applied at break even", "Risk");

		_liveTrading = Param(nameof(LiveTrading), false)
		.SetDisplay("Live Trading", "Apply live sizing adjustments", "Risk");

		_isMiniAccount = Param(nameof(IsMiniAccount), false)
		.SetDisplay("Mini Account", "Treat account as mini", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum simultaneous trades", "Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for processing", "General");
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

		var fastEma = new EMA { Length = FastPeriod };
		var slowEma = new EMA { Length = SlowPeriod };
		var rsi = new RSI { Length = RsiLength };

		SubscribeCandles(CandleType)
		.Bind(fastEma, slowEma, rsi, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateBarCounter(candle);

		var step = GetPriceStep();

		ApplyAbandonLogic();

		if (Position != 0)
		{
			ApplyProfitLock(step, candle);

			if (TryExitByTargets(candle))
			{
				_pendingReverseToBuy = false;
				_pendingReverseToSell = false;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousRsi = rsi;
			return;
		}

		var (rsiPositive, rsiNegative) = EvaluateRsiSignals(rsi);

		var buySignal = fastEma > slowEma + step && rsiNegative;
		var sellSignal = fastEma < slowEma - step && rsiPositive;

		TryEnterPosition(candle, step, buySignal, sellSignal);

		_previousRsi = rsi;
	}

	private void UpdateBarCounter(ICandleMessage candle)
	{
		if (_lastCandleTime == candle.OpenTime)
		return;

		if (Position != 0 && _lastCandleTime != null)
		_barsSinceEntry++;
		else if (Position == 0)
		_barsSinceEntry = 0;

		_lastCandleTime = candle.OpenTime;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0)
		step = 0.0001m;

		return step;
	}

	private void ApplyAbandonLogic()
	{
		if (Position == 0 || AbandonBars <= 0)
		return;

		if (_barsSinceEntry < AbandonBars)
		return;

		if (UseAbandonMethodA)
		{
			if (_isLongPosition && Position > 0)
			{
				ClosePosition();
				ResetTradeState();
				_pendingReverseToSell = true;
				_pendingReverseToBuy = false;
			}
			else if (!_isLongPosition && Position < 0)
			{
				ClosePosition();
				ResetTradeState();
				_pendingReverseToBuy = true;
				_pendingReverseToSell = false;
			}
		}
		else if (UseAbandonMethodB)
		{
			ClosePosition();
			ResetTradeState();
			_pendingReverseToBuy = false;
			_pendingReverseToSell = false;
		}
	}

	private void ApplyProfitLock(decimal step, ICandleMessage candle)
	{
		if (!UseProfitLock || _entryPrice is not decimal entry || _stopLossPrice is not decimal stop)
		return;

		if (_isLongPosition && Position > 0)
		{
			if (!_breakEvenApplied && stop < entry && BreakEvenTriggerPoints > 0m && candle.HighPrice >= entry + step * BreakEvenTriggerPoints)
			{
				_stopLossPrice = entry + step * BreakEvenPoints;
				_breakEvenApplied = true;
			}
		}
		else if (!_isLongPosition && Position < 0)
		{
			if (!_breakEvenApplied && stop > entry && BreakEvenTriggerPoints > 0m && candle.LowPrice <= entry - step * BreakEvenTriggerPoints)
			{
				_stopLossPrice = entry - step * BreakEvenPoints;
				_breakEvenApplied = true;
			}
		}
	}

	private bool TryExitByTargets(ICandleMessage candle)
	{
		if (_entryPrice is null || _stopLossPrice is null || _takeProfitPrice is null)
		return false;

		if (_isLongPosition && Position > 0)
		{
			if (candle.HighPrice >= _takeProfitPrice)
			{
				ClosePosition();
				ResetTradeState();
				return true;
			}

			if (candle.LowPrice <= _stopLossPrice)
			{
				ClosePosition();
				ResetTradeState();
				return true;
			}
		}
		else if (!_isLongPosition && Position < 0)
		{
			if (candle.LowPrice <= _takeProfitPrice)
			{
				ClosePosition();
				ResetTradeState();
				return true;
			}

			if (candle.HighPrice >= _stopLossPrice)
			{
				ClosePosition();
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private (bool positive, bool negative) EvaluateRsiSignals(decimal currentRsi)
	{
		var positive = false;
		var negative = false;

		if (UseRsiMethodA && _previousRsi is decimal prev)
		{
			if (currentRsi > RsiThreshold && prev < RsiThreshold)
			{
				positive = true;
				negative = false;
			}
			else if (currentRsi < RsiThreshold && prev > RsiThreshold)
			{
				positive = false;
				negative = true;
			}
		}

		if (UseRsiMethodB)
		{
			if (currentRsi > RsiThreshold)
			{
				positive = true;
				negative = false;
			}
			else if (currentRsi < RsiThreshold)
			{
				positive = false;
				negative = true;
			}
		}

		return (positive, negative);
	}

	private void TryEnterPosition(ICandleMessage candle, decimal step, bool buySignal, bool sellSignal)
	{
		if (MaxPositions <= 0)
		return;

		var volume = CalculateEntryVolume();

		if (volume <= 0)
		return;

		if ((_pendingReverseToBuy || buySignal) && Position <= 0)
		{
			var totalVolume = volume + (Position < 0 ? Math.Abs(Position) : 0m);

			if (totalVolume <= 0)
			return;

			BuyMarket(totalVolume);
			InitializeTradeState(candle, step, volume, true);
			_pendingReverseToBuy = false;
			_pendingReverseToSell = false;
		}
		else if ((_pendingReverseToSell || sellSignal) && Position >= 0)
		{
			var totalVolume = volume + (Position > 0 ? Math.Abs(Position) : 0m);

			if (totalVolume <= 0)
			return;

			SellMarket(totalVolume);
			InitializeTradeState(candle, step, volume, false);
			_pendingReverseToBuy = false;
			_pendingReverseToSell = false;
		}
	}

	private decimal CalculateEntryVolume()
	{
		var volume = InitialVolume;

		if (UseMoneyManagement)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

			if (balance > 0)
			{
				var managed = Math.Ceiling(balance * RiskPercent / 10000m) / 10m;

				if (managed < 0.1m)
				managed = InitialVolume;

				if (managed > 1m)
				managed = Math.Ceiling(managed);

				if (LiveTrading)
				{
					if (IsMiniAccount)
					managed *= 10m;
					else if (managed < 1m)
					managed = 1m;
				}

				if (managed > 100m)
				managed = 100m;

				volume = managed;
			}
		}

		return Math.Max(volume, 0m);
	}

	private void InitializeTradeState(ICandleMessage candle, decimal step, decimal volume, bool isLong)
	{
		_isLongPosition = isLong;
		_entryPrice = candle.ClosePrice;
		_currentVolume = volume;
		_breakEvenApplied = false;
		_barsSinceEntry = 0;
		_lastCandleTime = candle.OpenTime;

		if (isLong)
		{
			_stopLossPrice = _entryPrice - step * StopLossPoints;
			_takeProfitPrice = _entryPrice + step * TakeProfitPoints;
		}
		else
		{
			_stopLossPrice = _entryPrice + step * StopLossPoints;
			_takeProfitPrice = _entryPrice - step * TakeProfitPoints;
		}
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_currentVolume = 0m;
		_breakEvenApplied = false;
		_barsSinceEntry = 0;
	}
}
