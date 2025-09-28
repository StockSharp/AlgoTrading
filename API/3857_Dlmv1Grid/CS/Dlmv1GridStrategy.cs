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
/// Grid strategy converted from the MetaTrader expert advisor "DLM v1.4".
/// Implements martingale-style averaging with Fisher Transform based direction detection
/// and multiple account-protection blocks.
/// </summary>
public class Dlmv1GridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _gridDistancePips;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _accountType;
	private readonly StrategyParam<bool> _secureProfitProtection;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<bool> _equityProtection;
	private readonly StrategyParam<int> _equityProtectionPercent;
	private readonly StrategyParam<bool> _moneyProtection;
	private readonly StrategyParam<decimal> _moneyProtectionValue;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<int> _ordersLifeSeconds;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _useLimitOrders;
	private readonly StrategyParam<bool> _manualTrading;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fisherLength;
	private readonly StrategyParam<int> _signalSmoothing;
	private readonly StrategyParam<decimal> _defaultPipValue;

	private EhlersFisherTransform _fisher = null!;
	private SimpleMovingAverage _signalSma = null!;

	private decimal _pipSize;
	private decimal _pipValue;
	private decimal _openVolume;
	private decimal _averagePrice;
	private int _openTrades;
	private bool _isLongPosition;
	private decimal _lastEntryPrice;
	private decimal _lastEntryVolume;
	private bool _continueOpening;
	private Sides? _currentDirection;
	private DateTimeOffset? _lastEntryTime;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _initialEquity;
	private decimal _martingaleBaseVolume;
	private Order _pendingLimitOrder;
	private decimal _lastFisher;
	private decimal _lastSignal;

	/// <summary>
	/// Initializes a new instance of <see cref="Dlmv1GridStrategy"/>.
	/// </summary>
	public Dlmv1GridStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetDisplay("Take Profit (pips)", "Take profit distance applied to each entry", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetDisplay("Stop Loss (pips)", "Initial stop loss distance for every entry", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance that activates after the trigger", "Risk")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of martingale steps", "General")
			.SetCanOptimize(true);

		_gridDistancePips = Param(nameof(GridDistancePips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Distance (pips)", "Distance in pips between consecutive entries", "General")
			.SetCanOptimize(true);

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetDisplay("Lot Size", "Base lot size when money management is disabled", "Risk")
			.SetCanOptimize(true);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use Money Management", "Enable balance based position sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 12m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Risk percentage used for balance based sizing", "Risk")
			.SetCanOptimize(true);

		_accountType = Param(nameof(AccountType), 1)
			.SetDisplay("Account Type", "0=standard, 1=mini, 2=micro lot scaling", "Risk");

		_secureProfitProtection = Param(nameof(SecureProfitProtection), false)
			.SetDisplay("Secure Profit Protection", "Enable floating profit based basket protection", "Risk");

		_secureProfit = Param(nameof(SecureProfit), 20m)
			.SetDisplay("Secure Profit", "Floating profit (in currency units) required to close the basket", "Risk")
			.SetCanOptimize(true);

		_ordersToProtect = Param(nameof(OrdersToProtect), 3)
			.SetGreaterThanZero()
			.SetDisplay("Orders To Protect", "Number of last entries protected by secure profit", "Risk")
			.SetCanOptimize(true);

		_equityProtection = Param(nameof(EquityProtection), true)
			.SetDisplay("Equity Protection", "Close all trades when equity falls below the threshold", "Risk");

		_equityProtectionPercent = Param(nameof(EquityProtectionPercent), 90)
			.SetDisplay("Equity Percent", "Percentage of initial equity to protect", "Risk")
			.SetCanOptimize(true);

		_moneyProtection = Param(nameof(AccountMoneyProtection), false)
			.SetDisplay("Money Protection", "Close trades when drawdown in currency exceeds the threshold", "Risk");

		_moneyProtectionValue = Param(nameof(AccountMoneyProtectionValue), 3000m)
			.SetDisplay("Money Protection Value", "Drawdown in currency units that triggers the protection", "Risk")
			.SetCanOptimize(true);

		_tradeOnFriday = Param(nameof(TradeOnFriday), true)
			.SetDisplay("Trade On Friday", "Allow opening new baskets on Fridays", "Filters");

		_ordersLifeSeconds = Param(nameof(OrdersLifeSeconds), 0)
			.SetDisplay("Orders Lifetime (sec)", "Maximum lifetime for the most recent order", "Risk")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Reverse the Fisher Transform direction", "Filters");

		_useLimitOrders = Param(nameof(UseLimitOrders), true)
			.SetDisplay("Use Limit Orders", "Open averaging trades using limit orders instead of market", "Execution");

		_manualTrading = Param(nameof(ManualTrading), false)
			.SetDisplay("Manual Mode", "Disable automatic entries and manage trades manually", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals and management", "General");

		_fisherLength = Param(nameof(FisherLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Lookback for the Fisher Transform", "Filters")
			.SetCanOptimize(true);

		_signalSmoothing = Param(nameof(SignalSmoothing), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Smoothing", "SMA period applied to Fisher values", "Filters")
			.SetCanOptimize(true);

		_defaultPipValue = Param(nameof(DefaultPipValue), 5m)
			.SetDisplay("Default Pip Value", "Fallback pip value used for profit calculations", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Maximum number of averaging trades in the basket.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Distance between consecutive entries measured in pips.
	/// </summary>
	public decimal GridDistancePips
	{
		get => _gridDistancePips.Value;
		set => _gridDistancePips.Value = value;
	}

	/// <summary>
	/// Base lot size used when money management is disabled.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Enables balance based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage used to derive the base lot size.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Account type scaling (0=standard, 1=mini, 2=micro).
	/// </summary>
	public int AccountType
	{
		get => _accountType.Value;
		set => _accountType.Value = value;
	}

	/// <summary>
	/// Enables secure profit based protection for the basket.
	/// </summary>
	public bool SecureProfitProtection
	{
		get => _secureProfitProtection.Value;
		set => _secureProfitProtection.Value = value;
	}

	/// <summary>
	/// Floating profit threshold that triggers the protection block.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Number of orders that must be open before the secure profit check activates.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Enables equity based account protection.
	/// </summary>
	public bool EquityProtection
	{
		get => _equityProtection.Value;
		set => _equityProtection.Value = value;
	}

	/// <summary>
	/// Percentage of the initial equity to protect.
	/// </summary>
	public int EquityProtectionPercent
	{
		get => _equityProtectionPercent.Value;
		set => _equityProtectionPercent.Value = value;
	}

	/// <summary>
	/// Enables drawdown based money protection.
	/// </summary>
	public bool AccountMoneyProtection
	{
		get => _moneyProtection.Value;
		set => _moneyProtection.Value = value;
	}

	/// <summary>
	/// Drawdown in account currency that triggers the money protection block.
	/// </summary>
	public decimal AccountMoneyProtectionValue
	{
		get => _moneyProtectionValue.Value;
		set => _moneyProtectionValue.Value = value;
	}

	/// <summary>
	/// Allows opening new baskets on Fridays.
	/// </summary>
	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	/// <summary>
	/// Maximum lifetime (in seconds) for the most recent entry before protection closes the basket.
	/// </summary>
	public int OrdersLifeSeconds
	{
		get => _ordersLifeSeconds.Value;
		set => _ordersLifeSeconds.Value = value;
	}

	/// <summary>
	/// Reverses Fisher Transform signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Opens averaging trades using limit orders.
	/// </summary>
	public bool UseLimitOrders
	{
		get => _useLimitOrders.Value;
		set => _useLimitOrders.Value = value;
	}

	/// <summary>
	/// Disables automatic entries when true.
	/// </summary>
	public bool ManualTrading
	{
		get => _manualTrading.Value;
		set => _manualTrading.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculation and position management.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for the Fisher Transform indicator.
	/// </summary>
	public int FisherLength
	{
		get => _fisherLength.Value;
		set => _fisherLength.Value = value;
	}

	/// <summary>
	/// Period of the smoothing applied to Fisher values.
	/// </summary>
	public int SignalSmoothing
	{
		get => _signalSmoothing.Value;
		set => _signalSmoothing.Value = value;
	}

	/// <summary>
	/// Fallback pip value used for unrealized profit calculations.
	/// </summary>
	public decimal DefaultPipValue
	{
		get => _defaultPipValue.Value;
		set => _defaultPipValue.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openVolume = 0m;
		_averagePrice = 0m;
		_openTrades = 0;
		_isLongPosition = false;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_continueOpening = true;
		_currentDirection = null;
		_lastEntryTime = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_initialEquity = 0m;
		_martingaleBaseVolume = 0m;
		_pendingLimitOrder = null;
		_lastFisher = 0m;
		_lastSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		_pipValue = Security?.StepPrice ?? DefaultPipValue;
		_initialEquity = Portfolio?.CurrentValue ?? 0m;
		_martingaleBaseVolume = CalculateBaseVolume();

		_fisher = new EhlersFisherTransform { Length = FisherLength };
		_signalSma = new SimpleMovingAverage { Length = SignalSmoothing };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fisherValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var signalResult = _signalSma.Process(new DecimalIndicatorValue(_signalSma, fisherValue, candle.OpenTime));
		if (!signalResult.IsFinal || signalResult is not DecimalIndicatorValue { Value: var signal })
			return;

		_lastFisher = fisherValue;
		_lastSignal = signal;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TradeOnFriday && candle.CloseTime.DayOfWeek == DayOfWeek.Friday && _openTrades == 0)
		{
			_continueOpening = false;
			return;
		}

		if (!CheckAccountProtections(candle.CloseTime))
			return;

ManageOpenPosition(candle.ClosePrice);
		if (_openVolume > 0m)
		{
			ApplyTrailingStop(candle.ClosePrice);
		}

		if (ManualTrading)
			return;

		_continueOpening = _openTrades < MaxTrades;
		if (!_continueOpening)
			return;

		if (_openTrades == 0)
		{
			_currentDirection = DetermineDirection();
			if (_currentDirection.HasValue)
			{
				TryOpenPosition(_currentDirection.Value, candle.ClosePrice, candle.CloseTime);
			}
		}
		else if (_currentDirection.HasValue)
		{
			TryAddPosition(_currentDirection.Value, candle.ClosePrice, candle.CloseTime);
		}

		if (UseLimitOrders)
		{
			UpdateLimitOrder(candle.CloseTime);
		}
	}

	private bool CheckAccountProtections(DateTimeOffset time)
	{
		var portfolio = Portfolio;
		if (portfolio != null)
		{
			var equity = portfolio.CurrentValue;
			var drawdown = Math.Max(0m, _initialEquity - equity);

			if (AccountMoneyProtection && drawdown >= AccountMoneyProtectionValue)
			{
				LogInfo($"Money protection triggered. Drawdown={drawdown:F2}");
				CloseEntirePosition();
				_continueOpening = false;
				return false;
			}

			var protectionLevel = _initialEquity * EquityProtectionPercent / 100m;
			if (EquityProtection && equity <= protectionLevel)
			{
				LogInfo($"Equity protection triggered. Equity={equity:F2}");
				CloseEntirePosition();
				_continueOpening = false;
				return false;
			}
		}

		if (OrdersLifeSeconds > 0 && _lastEntryTime.HasValue)
		{
			var elapsed = (time - _lastEntryTime.Value).TotalSeconds;
			if (elapsed > OrdersLifeSeconds)
			{
				LogInfo($"Orders lifetime protection triggered after {elapsed:F0} seconds.");
				CloseEntirePosition();
				_continueOpening = false;
				return false;
			}
		}

		return true;
	}

private void ManageOpenPosition(decimal currentPrice)
	{
		if (_openVolume <= 0m)
			return;

		if (SecureProfitProtection && OrdersToProtect > 0 && _openTrades >= OrdersToProtect)
		{
			var profit = CalculateFloatingProfit(currentPrice);
			if (profit >= SecureProfit)
			{
				LogInfo($"Secure profit protection triggered. Profit={profit:F2}");
				CloseEntirePosition();
				_continueOpening = false;
				return;
			}
		}

		if (_stopLossPrice.HasValue)
		{
			if (_isLongPosition && currentPrice <= _stopLossPrice.Value)
			{
				SellMarket(_openVolume);
				return;
			}

			if (!_isLongPosition && currentPrice >= _stopLossPrice.Value)
			{
				BuyMarket(_openVolume);
				return;
			}
		}

		if (_takeProfitPrice.HasValue)
		{
			if (_isLongPosition && currentPrice >= _takeProfitPrice.Value)
			{
				SellMarket(_openVolume);
				return;
			}

			if (!_isLongPosition && currentPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(_openVolume);
			}
		}
	}

	private void ApplyTrailingStop(decimal currentPrice)
	{
		if (TrailingStopPips <= 0m || GridDistancePips <= 0m || _openVolume <= 0m)
			return;

		var trigger = ToPrice(TrailingStopPips + GridDistancePips);
		var trailingDistance = ToPrice(TrailingStopPips);

		if (_isLongPosition)
		{
			var distance = currentPrice - _averagePrice;
			if (distance >= trigger)
			{
				var candidate = AdjustPrice(currentPrice - trailingDistance);
				if (!_stopLossPrice.HasValue || candidate > _stopLossPrice.Value)
					_stopLossPrice = candidate;
			}
		}
		else
		{
			var distance = _averagePrice - currentPrice;
			if (distance >= trigger)
			{
				var candidate = AdjustPrice(currentPrice + trailingDistance);
				if (!_stopLossPrice.HasValue || candidate < _stopLossPrice.Value)
					_stopLossPrice = candidate;
			}
		}
	}

	private Sides? DetermineDirection()
	{
		if (!_signalSma.IsFormed)
			return null;

		if (_lastFisher > _lastSignal)
			return ReverseSignals ? Sides.Sell : Sides.Buy;

		if (_lastFisher < _lastSignal)
			return ReverseSignals ? Sides.Buy : Sides.Sell;

		return null;
	}

	private void TryOpenPosition(Sides direction, decimal price, DateTimeOffset time)
	{
		var volume = CalculateNextVolume();
		if (volume <= 0m)
			return;

		if (direction == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_lastEntryTime = time;
	}

	private void TryAddPosition(Sides direction, decimal price, DateTimeOffset time)
	{
		if (!UseLimitOrders)
		{
			var reference = _lastEntryPrice != 0m ? _lastEntryPrice : _averagePrice;
			var distance = Math.Abs(price - reference);
			if (distance < ToPrice(GridDistancePips))
				return;

			var volume = CalculateNextVolume();
			if (volume <= 0m)
				return;

			if (direction == Sides.Buy)
				BuyMarket(volume);
			else
				SellMarket(volume);

			_lastEntryTime = time;
		}
	}

	private void UpdateLimitOrder(DateTimeOffset time)
	{
		if (_openTrades == 0 || !_currentDirection.HasValue || !_continueOpening)
		{
			CancelLimitOrder();
			return;
		}

		var price = CalculateNextLimitPrice(_currentDirection.Value);
		if (!price.HasValue)
		{
			CancelLimitOrder();
			return;
		}

		var adjustedPrice = AdjustPrice(price.Value);

		if (_pendingLimitOrder != null && !_pendingLimitOrder.State.IsFinal())
		{
			if (_pendingLimitOrder.Price != adjustedPrice)
			{
				CancelLimitOrder();
			}
			else
			{
				return;
			}
		}

		var volume = CalculateNextVolume();
		if (volume <= 0m)
			return;

		_pendingLimitOrder = _currentDirection == Sides.Buy
			? BuyLimit(volume, adjustedPrice)
			: SellLimit(volume, adjustedPrice);

		_lastEntryTime = time;
	}

	private decimal? CalculateNextLimitPrice(Sides direction)
	{
		if (_openTrades <= 0)
			return null;

		var reference = _lastEntryPrice != 0m ? _lastEntryPrice : _averagePrice;
		if (reference == 0m)
			return null;

		var offset = ToPrice(GridDistancePips);
		if (offset <= 0m)
			return null;

		return direction == Sides.Buy ? reference - offset : reference + offset;
	}

	private void CancelLimitOrder()
	{
		if (_pendingLimitOrder != null && !_pendingLimitOrder.State.IsFinal())
		{
			CancelOrder(_pendingLimitOrder);
		}
	}

	private decimal CalculateBaseVolume()
	{
		var volume = LotSize;

		if (UseMoneyManagement)
		{
			var balance = Portfolio?.CurrentValue ?? 0m;
			if (balance > 0m)
			{
				var riskValue = Math.Ceiling(balance * RiskPercent / 10000m);
				volume = AccountType switch
				{
					0 => riskValue,
					1 => riskValue / 10m,
					2 => riskValue / 100m,
					_ => riskValue,
				};
			}
		}

		if (volume > 100m)
			volume = 100m;

		return AdjustVolume(volume);
	}

	private decimal CalculateNextVolume()
	{
		var volume = _martingaleBaseVolume > 0m ? _martingaleBaseVolume : CalculateBaseVolume();

		if (_openTrades > 0)
		{
			for (var i = 0; i < _openTrades; i++)
			{
				volume = MaxTrades > 12
					? Math.Round(volume * 1.5m, 2, MidpointRounding.AwayFromZero)
					: Math.Round(volume * 2m, 2, MidpointRounding.AwayFromZero);
			}
		}

		if (volume > 100m)
			volume = 100m;

		return AdjustVolume(volume);
	}

	private decimal AdjustPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private decimal AdjustVolume(decimal volume)
	{
		return Security?.ShrinkVolume(volume) ?? volume;
	}

	private decimal ToPrice(decimal pips)
	{
		return pips * _pipSize;
	}

	private decimal CalculateFloatingProfit(decimal currentPrice)
	{
		if (_openVolume <= 0m || _pipSize <= 0m)
			return 0m;

		var profitPips = _isLongPosition
			? (currentPrice - _averagePrice) / _pipSize * _openVolume
			: (_averagePrice - currentPrice) / _pipSize * _openVolume;

		return profitPips * _pipValue;
	}

	private void CloseEntirePosition()
	{
		if (_openVolume <= 0m)
			return;

		if (_isLongPosition)
			SellMarket(_openVolume);
		else
			BuyMarket(_openVolume);

		CancelLimitOrder();
	}

	private decimal? UpdateStopAfterEntry(bool isLong, decimal price)
	{
		if (StopLossPips <= 0m)
			return _stopLossPrice;

		var offset = ToPrice(StopLossPips);
		if (isLong)
		{
			var candidate = AdjustPrice(price - offset);
			return !_stopLossPrice.HasValue || candidate < _stopLossPrice.Value ? candidate : _stopLossPrice;
		}

		var candidateShort = AdjustPrice(price + offset);
		return !_stopLossPrice.HasValue || candidateShort > _stopLossPrice.Value ? candidateShort : _stopLossPrice;
	}

	private decimal? UpdateTakeProfitAfterEntry(bool isLong, decimal price)
	{
		if (TakeProfitPips <= 0m)
			return _takeProfitPrice;

		var offset = ToPrice(TakeProfitPips);
		if (isLong)
		{
			var candidate = AdjustPrice(price + offset);
			return !_takeProfitPrice.HasValue || candidate > _takeProfitPrice.Value ? candidate : _takeProfitPrice;
		}

		var candidateShort = AdjustPrice(price - offset);
		return !_takeProfitPrice.HasValue || candidateShort < _takeProfitPrice.Value ? candidateShort : _takeProfitPrice;
	}

	private void ResetPositionState()
	{
		_openVolume = 0m;
		_averagePrice = 0m;
		_openTrades = 0;
		_isLongPosition = false;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_continueOpening = true;
		_currentDirection = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		CancelLimitOrder();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		var side = trade.Order.Side;

		if (side == Sides.Buy)
		{
			if (_openVolume > 0m && !_isLongPosition)
			{
				HandlePositionReduction(volume);
				return;
			}

			var newVolume = _openVolume + volume;
			_averagePrice = newVolume == 0m ? 0m : (_averagePrice * _openVolume + price * volume) / newVolume;
			_openVolume = newVolume;
			_isLongPosition = true;
			_openTrades++;
			_lastEntryPrice = price;
			_lastEntryVolume = volume;
			_stopLossPrice = UpdateStopAfterEntry(true, price);
			_takeProfitPrice = UpdateTakeProfitAfterEntry(true, price);
		}
		else if (side == Sides.Sell)
		{
			if (_openVolume > 0m && _isLongPosition)
			{
				HandlePositionReduction(volume);
				return;
			}

			var newVolume = _openVolume + volume;
			_averagePrice = newVolume == 0m ? 0m : (_averagePrice * _openVolume + price * volume) / newVolume;
			_openVolume = newVolume;
			_isLongPosition = false;
			_openTrades++;
			_lastEntryPrice = price;
			_lastEntryVolume = volume;
			_stopLossPrice = UpdateStopAfterEntry(false, price);
			_takeProfitPrice = UpdateTakeProfitAfterEntry(false, price);
		}

		_continueOpening = _openTrades < MaxTrades;
		_martingaleBaseVolume = CalculateBaseVolume();
	}

	private void HandlePositionReduction(decimal volume)
	{
		var closingVolume = Math.Min(_openVolume, volume);
		_openVolume -= closingVolume;

		if (_openVolume <= 0m)
		{
			ResetPositionState();
			return;
		}

		if (_openTrades > 0)
			_openTrades--;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (_pendingLimitOrder != null && order == _pendingLimitOrder && order.State.IsFinal())
		{
			_pendingLimitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail, bool calcRisk)
	{
		base.OnOrderRegisterFailed(fail, calcRisk);

		if (_pendingLimitOrder != null && fail.Order == _pendingLimitOrder)
		{
			_pendingLimitOrder = null;
		}
	}
}

