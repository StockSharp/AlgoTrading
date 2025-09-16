using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Invest System 4.5 strategy converted from MetaTrader.
/// Trades in the direction of the previous 4-hour candle within the first minutes of the new session.
/// </summary>
public class InvestSystem45Strategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _entryWindowMinutes;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<decimal> _baseLot;

	private decimal _pipSize;
	private decimal _minBalance;
	private decimal _maxBalance;
	private int _lotStage;
	private bool _planBActive;

	private decimal _stageLot1;
	private decimal _stageLot2;
	private decimal _stageLot3;
	private decimal _stageLot4;
	private decimal _lotOption1;
	private decimal _lotOption2;
	private decimal _currentVolume;

	private bool _needsPostTradeAdjustment;
	private bool _hasOpenPosition;
	private decimal _pnlAtEntry;
	private decimal _lastTradePnL;

	private int _trendDirection;
	private DateTimeOffset? _entryWindowStart;
	private DateTimeOffset? _entryWindowEnd;
	private bool _entryWindowActive;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minutes allowed for entries after a new trend candle opens.
	/// </summary>
	public int EntryWindowMinutes
	{
		get => _entryWindowMinutes.Value;
		set => _entryWindowMinutes.Value = value;
	}

	/// <summary>
	/// Candle type that drives entry timing.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle used to define trade direction.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Base lot size used to derive martingale steps.
	/// </summary>
	public decimal BaseLot
	{
		get => _baseLot.Value;
		set => _baseLot.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="InvestSystem45Strategy"/>.
	/// </summary>
	public InvestSystem45Strategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 240)
			.SetGreaterOrEqual(0)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(120, 360, 20);

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetGreaterOrEqual(0)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 10);

		_entryWindowMinutes = Param(nameof(EntryWindowMinutes), 15)
			.SetGreaterThanZero()
			.SetDisplay("Entry Window", "Minutes after 4H open when entries are allowed", "Timing")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Signal Candles", "Candles used to time entries", "Timing");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Trend Candles", "Higher timeframe candles for direction", "Timing");

		_baseLot = Param(nameof(BaseLot), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Lot", "Starting lot size before scaling", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.3m, 0.05m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, SignalCandleType);

		if (!SignalCandleType.Equals(TrendCandleType))
			yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		_pipSize = CalculatePipSize();
		// Recreate lot options according to current stage and plan mode.
		RecalculateLotOptions();

		// Configure default protective orders matching pip distances.
		StartProtection(
			takeProfit: new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * _pipSize, UnitTypes.Absolute));

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription.ForEach(ProcessTrendCandle).Start();

		var entrySubscription = SubscribeCandles(SignalCandleType);
		entrySubscription.ForEach(ProcessEntryCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0m)
		{
			// Record entry state to compute realized PnL later.
			if (!_hasOpenPosition)
			{
				_hasOpenPosition = true;
				_needsPostTradeAdjustment = true;
				_pnlAtEntry = PnL;
			}

			_entryWindowActive = false;
			return;
		}

		if (!_hasOpenPosition)
			return;

		_hasOpenPosition = false;
		_lastTradePnL = PnL - _pnlAtEntry;
		// Mirror MetaTrader profit calculation for Plan B rules.
		LogInfo($"Position closed with PnL {_lastTradePnL:F2}");

		HandlePostTradeAdjustment();
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store direction from the last completed 4H candle.
		if (candle.ClosePrice > candle.OpenPrice)
		{
			_trendDirection = 1;
			LogInfo("Latest 4H candle bullish. Next trade will look for buys.");
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			_trendDirection = -1;
			LogInfo("Latest 4H candle bearish. Next trade will look for sells.");
		}

		_entryWindowStart = candle.CloseTime;
		_entryWindowEnd = _entryWindowStart?.AddMinutes(EntryWindowMinutes);
		// Open a new entry window immediately at the next candle open.
		_entryWindowActive = true;
	}

	private void ProcessEntryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update balance-dependent scaling before evaluating signals.
		UpdateBalanceState();

		if (!_entryWindowActive || !_entryWindowStart.HasValue || !_entryWindowEnd.HasValue)
			return;

		var openTime = candle.OpenTime;
		if (openTime < _entryWindowStart.Value)
			return;

		if (openTime > _entryWindowEnd.Value)
		{
			_entryWindowActive = false;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_trendDirection == 0)
			return;

		if (Position != 0m)
			return;

		// Lazy initialize volume when strategy is ready.
		if (_currentVolume <= 0m)
			_currentVolume = _lotOption1;

		if (_currentVolume <= 0m)
			return;

		if (_trendDirection > 0)
		{
			LogInfo($"Opening long position with volume {_currentVolume} at {candle.CloseTime:O}.");
			BuyMarket(_currentVolume);
		}
		else
		{
			LogInfo($"Opening short position with volume {_currentVolume} at {candle.CloseTime:O}.");
			SellMarket(_currentVolume);
		}

		// Allow only one trade per 4H candle similar to MetaTrader logic.
		_entryWindowActive = false;
	}

	private void HandlePostTradeAdjustment()
	{
		if (!_needsPostTradeAdjustment)
			return;

		_needsPostTradeAdjustment = false;

		// Apply lot escalation rules after each closed trade.
		UpdateBalanceState();

		if (_lastTradePnL < 0m)
		{
			if (_currentVolume == _lotOption2 && !_planBActive)
			{
				_planBActive = true;
				RecalculateLotOptions();
				LogInfo("Plan B activated after loss with aggressive lot size.");
			}
			else if (_currentVolume == _lotOption1)
			{
				_currentVolume = _lotOption2;
				LogInfo($"Switching to larger lot {_currentVolume} after loss.");
			}
			else
			{
				_currentVolume = _lotOption2;
				LogInfo($"Adjusting lot to {_currentVolume} after loss.");
			}
		}
		else if (_lastTradePnL > 0m)
		{
			_currentVolume = _lotOption1;
			LogInfo($"Resetting lot to {_currentVolume} after profit.");
		}
	}

	private void UpdateBalanceState()
	{
		var balance = Portfolio?.CurrentValue;
		if (balance is null || balance.Value <= 0m)
			return;

		if (_minBalance <= 0m)
		{
			_minBalance = balance.Value;
			_maxBalance = balance.Value;
		}

		if (balance.Value > _maxBalance)
		{
			_maxBalance = balance.Value;
			if (_planBActive)
			{
				_planBActive = false;
				RecalculateLotOptions();
				LogInfo("Plan B disabled after reaching new balance high.");
			}
		}

		var newStage = 1;
		if (_minBalance > 0m)
		{
			// Check for equity milestones to scale base lots.
			for (var stage = 6; stage >= 2; stage--)
			{
				if (balance.Value > _minBalance * stage)
				{
					newStage = stage;
					break;
				}
			}
		}

		if (newStage != _lotStage)
		{
			_lotStage = newStage;
			RecalculateLotOptions();
			LogInfo($"Lot stage updated to {_lotStage} for balance {balance.Value:F2}.");
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;

		if (decimals == 3 || decimals == 5)
			step *= 10m;

		return step;
	}

	private void ResetState()
	{
		_pipSize = 0m;
		_minBalance = 0m;
		_maxBalance = 0m;
		_lotStage = 1;
		_planBActive = false;
		_stageLot1 = 0m;
		_stageLot2 = 0m;
		_stageLot3 = 0m;
		_stageLot4 = 0m;
		_lotOption1 = 0m;
		_lotOption2 = 0m;
		_currentVolume = 0m;
		_needsPostTradeAdjustment = false;
		_hasOpenPosition = false;
		_pnlAtEntry = 0m;
		_lastTradePnL = 0m;
		_trendDirection = 0;
		_entryWindowStart = null;
		_entryWindowEnd = null;
		_entryWindowActive = false;
	}

	private void RecalculateLotOptions()
	{
		var baseLot = BaseLot * _lotStage;

		_stageLot1 = baseLot;
		_stageLot2 = baseLot * 2m;
		_stageLot3 = baseLot * 7m;
		_stageLot4 = baseLot * 14m;

		// Stage-specific lot multipliers replicate the original configuration.
		if (_planBActive)
		{
			_lotOption1 = _stageLot2;
			_lotOption2 = _stageLot4;
		}
		else
		{
			_lotOption1 = _stageLot1;
			_lotOption2 = _stageLot3;
		}

		if (_currentVolume <= 0m)
			_currentVolume = _lotOption1;
	}
}
