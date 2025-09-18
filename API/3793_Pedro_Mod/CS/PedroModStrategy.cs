namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mean reversion grid derived from the Pedroxxmod MetaTrader 4 expert advisor.
/// Places an initial reference price, then opens contrarian trades when price deviates by a configurable gap.
/// </summary>
public class PedroModStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _gapPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _reEntryGap;
	private readonly StrategyParam<bool> _moneyManagement;
	private readonly StrategyParam<int> _maxLots;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _startYear;

	private decimal _pointValue;
	private decimal? _entryPrice;
	private decimal? _reEntryPrice;
	private TradeSide _lastDirection;
	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _previousPosition;

	private readonly List<decimal> _longTrades = new();
	private readonly List<decimal> _shortTrades = new();

	/// <summary>
	/// Fixed volume used when money management is disabled.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in MetaTrader pips.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in MetaTrader pips.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Gap between the reference price and the first entry in MetaTrader pips.
	/// </summary>
	public int Gap
	{
		get => _gapPips.Value;
		set => _gapPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open trades.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Distance between averaging trades in MetaTrader pips.
	/// </summary>
	public int ReEntryGap
	{
		get => _reEntryGap.Value;
		set => _reEntryGap.Value = value;
	}

	/// <summary>
	/// Enables automatic volume calculation based on portfolio equity.
	/// </summary>
	public bool MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	/// <summary>
	/// Maximum number of lots when money management is enabled.
	/// </summary>
	public int MaxLots
	{
		get => _maxLots.Value;
		set => _maxLots.Value = value;
	}

	/// <summary>
	/// First trading hour (inclusive) in exchange server time.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last trading hour (inclusive) in exchange server time.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Year from which trading is allowed.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="PedroModStrategy"/>.
	/// </summary>
	public PedroModStrategy()
	{
		Volume = 1m;

		_lots = Param(nameof(Lots), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Lot size when money management is disabled.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_stopLoss = Param(nameof(StopLoss), 30)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in MetaTrader pips.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_takeProfit = Param(nameof(TakeProfit), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Profit target distance in MetaTrader pips.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_gapPips = Param(nameof(Gap), 5)
			.SetGreaterThanZero()
			.SetDisplay("Entry Gap (pips)", "Required movement before opening the first trade.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of simultaneously open trades.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_reEntryGap = Param(nameof(ReEntryGap), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Re-entry Gap (pips)", "Distance between averaging trades in MetaTrader pips.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_moneyManagement = Param(nameof(MoneyManagement), true)
			.SetDisplay("Use Money Management", "Toggle automatic volume based on equity.", "Risk");

		_maxLots = Param(nameof(MaxLots), 50)
			.SetGreaterThanZero()
			.SetDisplay("Max Lots", "Upper cap for automatically calculated volume.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_startHour = Param(nameof(StartHour), 1)
			.SetDisplay("Start Hour", "First trading hour (server time).", "Schedule")
			.SetCanOptimize(true)
			.SetOptimize(0, 12, 1);

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Last trading hour (server time).", "Schedule")
			.SetCanOptimize(true)
			.SetOptimize(12, 23, 1);

		_startYear = Param(nameof(StartYear), 2006)
			.SetDisplay("Start Year", "The first calendar year when trading is enabled.", "Schedule")
			.SetCanOptimize(true)
			.SetOptimize(2000, 2030, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointValue = 0m;
		_entryPrice = null;
		_reEntryPrice = null;
		_lastDirection = TradeSide.None;
		_bestBid = 0m;
		_bestAsk = 0m;
		_previousPosition = 0m;
		_longTrades.Clear();
		_shortTrades.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		UpdateOpenTrades(delta);
		_previousPosition = Position;

		if (Position == 0m)
		{
			_lastDirection = TradeSide.None;
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		// Update cached best bid/ask values from the Level1 message.
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAsk = ask;

		var serverTime = message.ServerTime != default ? message.ServerTime : CurrentTime;
		if (serverTime == default)
			return;

		// Enforce the calendar start restriction inherited from the MQL expert.
		if (StartYear > 0 && serverTime.Year < StartYear)
		{
			ResetEntryState();
			return;
		}

		var hour = serverTime.Hour;
		if (hour < StartHour || hour > EndHour)
		{
			// Reset the trigger prices outside of the trading window.
			ResetEntryState();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			// Do not evaluate signals without a live connection or permission to trade.
			return;
		}

		if (_bestAsk <= 0m || _bestBid <= 0m)
			return;

		var openTrades = GetOpenTradeCount();
		if (openTrades >= MaxTrades)
			return;

		ExecuteTradingLogic(openTrades);
	}

	private void ExecuteTradingLogic(int openTrades)
	{
		// The original expert keeps a reference price when no trades are open.
		if (_entryPrice == null)
		{
			if (openTrades == 0)
			{
				_entryPrice = _bestAsk;
				// When a fresh session starts no averaging is allowed yet.
				_reEntryPrice = null;
				_lastDirection = TradeSide.None;
			}
			else
			{
				HandleReEntry(openTrades);
			}

			return;
		}

		var gapDistance = Gap * _pointValue;
		if (gapDistance <= 0m)
		{
			// No valid distance means we cannot evaluate the breakout condition.
			return;
		}

		var entry = _entryPrice.Value;
		if (_bestAsk >= entry + gapDistance)
		{
			// Price moved up far enough to trigger a contrarian sell order.
			if (TryOpenSell())
			{
				_lastDirection = TradeSide.Sell;
				_reEntryPrice = _bestAsk;
				_entryPrice = null;
			}
			return;
		}

		if (_bestAsk <= entry - gapDistance)
		{
			// Price dropped enough to trigger a contrarian buy order.
			if (TryOpenBuy())
			{
				_lastDirection = TradeSide.Buy;
				_reEntryPrice = _bestAsk;
				_entryPrice = null;
			}
			return;
		}
	}

	private void HandleReEntry(int openTrades)
	{
		// Averaging logic works only after the first trade has been opened.
		if (_reEntryPrice == null)
			return;

		var reEntryDistance = ReEntryGap * _pointValue;
		if (reEntryDistance < 0m)
			return;

		var reference = _reEntryPrice.Value;
		var nextCount = openTrades + 1;

		if (_lastDirection == TradeSide.Buy)
		{
			// Keep adding to the long basket while the price stays near the reference.
			if (_bestAsk <= reference + reEntryDistance && TryOpenBuy())
			{
				_reEntryPrice = nextCount < MaxTrades ? _bestAsk : null;
			}
		}
		else if (_lastDirection == TradeSide.Sell)
		{
			// Mirror logic for the short basket.
			if (_bestAsk >= reference - reEntryDistance && TryOpenSell())
			{
				_reEntryPrice = nextCount < MaxTrades ? _bestAsk : null;
			}
		}
	}

	private bool TryOpenBuy()
	{
		// Determine the trade volume and submit a market buy order.
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return false;

		var executionPrice = _bestAsk;
		if (executionPrice <= 0m)
			return false;

		var resultingPosition = Position + volume;
		BuyMarket(volume);

		if (TakeProfit > 0)
			SetTakeProfit(TakeProfit, executionPrice, resultingPosition);

		if (StopLoss > 0)
			SetStopLoss(StopLoss, executionPrice, resultingPosition);

		return true;
	}

	private bool TryOpenSell()
	{
		// Determine the trade volume and submit a market sell order.
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return false;

		var executionPrice = _bestBid;
		if (executionPrice <= 0m)
			return false;

		var resultingPosition = Position - volume;
		SellMarket(volume);

		if (TakeProfit > 0)
			SetTakeProfit(TakeProfit, executionPrice, resultingPosition);

		if (StopLoss > 0)
			SetStopLoss(StopLoss, executionPrice, resultingPosition);

		return true;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = MoneyManagement ? CalculateManagedVolume() : Lots;
		return NormalizeVolume(volume);
	}

	private decimal CalculateManagedVolume()
	{
		// Replicates the MetaTrader formula: floor(equity / 20000) constrained to [1, MaxLots].
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return Lots;

		var rawLots = Math.Floor(equity / 20000m);
		if (rawLots < 1m)
			rawLots = 1m;

		var maxLots = Math.Max(1, MaxLots);
		if (rawLots > maxLots)
			rawLots = maxLots;

		return rawLots;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume != null && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private void UpdateOpenTrades(decimal delta)
	{
		// Track how many synthetic trades remain open to emulate the hedging-friendly terminal.
		if (delta == 0m)
			return;

		var previous = _previousPosition;
		var current = Position;

		if (delta > 0m)
		{
			var closedShort = Math.Min(delta, Math.Max(0m, -previous));
			if (closedShort > 0m)
				ReduceTrades(_shortTrades, closedShort);

			var openedLong = delta - closedShort;
			if (openedLong > 0m)
				_longTrades.Add(openedLong);
		}
		else
		{
			var absDelta = Math.Abs(delta);
			var closedLong = Math.Min(absDelta, Math.Max(0m, previous));
			if (closedLong > 0m)
				ReduceTrades(_longTrades, closedLong);

			var openedShort = absDelta - closedLong;
			if (openedShort > 0m)
				_shortTrades.Add(openedShort);
		}
	}

	private static void ReduceTrades(List<decimal> trades, decimal volumeToRemove)
	{
		// Remove closed exposure starting from the oldest entries (FIFO).
		for (var i = 0; i < trades.Count && volumeToRemove > 0m; )
		{
			var tradeVolume = trades[i];
			if (tradeVolume <= volumeToRemove)
			{
				volumeToRemove -= tradeVolume;
				trades.RemoveAt(i);
				continue;
			}

			trades[i] = tradeVolume - volumeToRemove;
			volumeToRemove = 0m;
			break;
		}
	}

	private int GetOpenTradeCount()
	{
		return _longTrades.Count + _shortTrades.Count;
	}

	private decimal CalculatePointValue()
	{
		// Convert MetaTrader pip distances to actual price movements.
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;

		return step * multiplier;
	}

	private void ResetEntryState()
	{
		_entryPrice = null;
		_reEntryPrice = null;
		_lastDirection = TradeSide.None;
	}

	private enum TradeSide
	{
		None,
		Buy,
		Sell,
	}
}
