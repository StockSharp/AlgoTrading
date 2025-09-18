using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid-based martingale strategy converted from the MetaTrader "Terminator" expert advisor.
/// </summary>
public class TerminatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _initialStopPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _entryDistancePips;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<bool> _useAccountProtection;
	private readonly StrategyParam<bool> _protectUsingBalance;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _manualTrading;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _isStandardAccount;
	private readonly StrategyParam<decimal> _eurUsdPipValue;
	private readonly StrategyParam<decimal> _gbpUsdPipValue;
	private readonly StrategyParam<decimal> _usdChfPipValue;
	private readonly StrategyParam<decimal> _usdJpyPipValue;
	private readonly StrategyParam<decimal> _defaultPipValue;
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<int> _startMonth;
	private readonly StrategyParam<int> _endYear;
	private readonly StrategyParam<int> _endMonth;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _previousMacd;
	private decimal? _previousPreviousMacd;
	private decimal _openVolume;
	private decimal _averagePrice;
	private int _openTrades;
	private bool _isLongPosition;
	private decimal _lastEntryPrice;
	private decimal _lastEntryVolume;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _pipSize;
	private decimal _pipValue;
	private bool _continueOpening;
	private Sides? _currentDirection;
	private decimal _martingaleBaseVolume;

	/// <summary>
	/// Initializes a new instance of <see cref="TerminatorStrategy"/>.
	/// </summary>
	public TerminatorStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 38m)
			.SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
			.SetCanOptimize(true);

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetDisplay("Base Lot Size", "Fixed lot size used when money management is disabled", "Risk")
			.SetCanOptimize(true);

		_initialStopPips = Param(nameof(InitialStopPips), 0m)
			.SetDisplay("Initial Stop (pips)", "Initial protective stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance that activates after the threshold", "Risk")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of simultaneously open martingale trades", "General")
			.SetCanOptimize(true);

		_entryDistancePips = Param(nameof(EntryDistancePips), 18m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Distance (pips)", "Minimum adverse movement required before adding a new position", "General")
			.SetCanOptimize(true);

		_secureProfit = Param(nameof(SecureProfit), 10m)
			.SetDisplay("Secure Profit", "Floating profit in currency units required to protect the account", "Risk")
			.SetCanOptimize(true);

		_useAccountProtection = Param(nameof(UseAccountProtection), true)
			.SetDisplay("Use Account Protection", "Enable partial liquidation when floating profit exceeds the threshold", "Risk");

		_protectUsingBalance = Param(nameof(ProtectUsingBalance), false)
			.SetDisplay("Protect Using Balance", "Use the current account value instead of Secure Profit as the protection threshold", "Risk");

		_ordersToProtect = Param(nameof(OrdersToProtect), 3)
			.SetGreaterThanZero()
			.SetDisplay("Orders To Protect", "Number of final trades protected by the secure profit rule", "Risk")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Reverse the MACD slope interpretation", "Filters");

		_manualTrading = Param(nameof(ManualTrading), false)
			.SetDisplay("Manual Trading", "Disable automatic entries while keeping trade management active", "General");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use Money Management", "Enable balance-based position sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Risk percentage used to derive the base lot size", "Risk")
			.SetCanOptimize(true);

		_isStandardAccount = Param(nameof(IsStandardAccount), false)
			.SetDisplay("Standard Account", "Use standard lot calculations instead of mini account scaling", "Risk");

		_eurUsdPipValue = Param(nameof(EurUsdPipValue), 10m)
			.SetDisplay("EURUSD Pip Value", "Monetary value of one pip for EURUSD", "Currency")
			.SetCanOptimize(true);

		_gbpUsdPipValue = Param(nameof(GbpUsdPipValue), 10m)
			.SetDisplay("GBPUSD Pip Value", "Monetary value of one pip for GBPUSD", "Currency")
			.SetCanOptimize(true);

		_usdChfPipValue = Param(nameof(UsdChfPipValue), 8.7m)
			.SetDisplay("USDCHF Pip Value", "Monetary value of one pip for USDCHF", "Currency")
			.SetCanOptimize(true);

		_usdJpyPipValue = Param(nameof(UsdJpyPipValue), 9.715m)
			.SetDisplay("USDJPY Pip Value", "Monetary value of one pip for USDJPY", "Currency")
			.SetCanOptimize(true);

		_defaultPipValue = Param(nameof(DefaultPipValue), 5m)
			.SetDisplay("Default Pip Value", "Fallback pip value used for other symbols", "Currency")
			.SetCanOptimize(true);

		_startYear = Param(nameof(StartYear), 2005)
			.SetDisplay("Start Year", "First year when new trades are allowed", "Schedule")
			.SetCanOptimize(true);

		_startMonth = Param(nameof(StartMonth), 1)
			.SetDisplay("Start Month", "First month when new trades are allowed", "Schedule")
			.SetCanOptimize(true);

		_endYear = Param(nameof(EndYear), 2030)
			.SetDisplay("End Year", "Last year when new trades are allowed", "Schedule")
			.SetCanOptimize(true);

		_endMonth = Param(nameof(EndMonth), 12)
			.SetDisplay("End Month", "Last month when new trades are allowed", "Schedule")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");

		_macdFastLength = Param(nameof(MacdFastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period used in MACD", "Filters")
			.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period used in MACD", "Filters")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period used in MACD", "Filters")
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
	/// Fixed lot size when money management is disabled.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Initial protective stop distance in pips.
	/// </summary>
	public decimal InitialStopPips
	{
		get => _initialStopPips.Value;
		set => _initialStopPips.Value = value;
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
	/// Maximum number of averaging trades allowed.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Minimum adverse move required to add a new position.
	/// </summary>
	public decimal EntryDistancePips
	{
		get => _entryDistancePips.Value;
		set => _entryDistancePips.Value = value;
	}

	/// <summary>
	/// Floating profit threshold used by the protection routine.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Enable or disable the account protection block.
	/// </summary>
	public bool UseAccountProtection
	{
		get => _useAccountProtection.Value;
		set => _useAccountProtection.Value = value;
	}

	/// <summary>
	/// Use the portfolio value instead of the SecureProfit parameter when protecting.
	/// </summary>
	public bool ProtectUsingBalance
	{
		get => _protectUsingBalance.Value;
		set => _protectUsingBalance.Value = value;
	}

	/// <summary>
	/// Number of last trades considered when calculating secure profit.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Reverse the MACD slope interpretation.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Disable automatic entries while still managing open positions.
	/// </summary>
	public bool ManualTrading
	{
		get => _manualTrading.Value;
		set => _manualTrading.Value = value;
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
	/// Risk percentage used when money management is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Indicates whether the account is standard (true) or mini (false).
	/// </summary>
	public bool IsStandardAccount
	{
		get => _isStandardAccount.Value;
		set => _isStandardAccount.Value = value;
	}

	/// <summary>
	/// Pip value for EURUSD.
	/// </summary>
	public decimal EurUsdPipValue
	{
		get => _eurUsdPipValue.Value;
		set => _eurUsdPipValue.Value = value;
	}

	/// <summary>
	/// Pip value for GBPUSD.
	/// </summary>
	public decimal GbpUsdPipValue
	{
		get => _gbpUsdPipValue.Value;
		set => _gbpUsdPipValue.Value = value;
	}

	/// <summary>
	/// Pip value for USDCHF.
	/// </summary>
	public decimal UsdChfPipValue
	{
		get => _usdChfPipValue.Value;
		set => _usdChfPipValue.Value = value;
	}

	/// <summary>
	/// Pip value for USDJPY.
	/// </summary>
	public decimal UsdJpyPipValue
	{
		get => _usdJpyPipValue.Value;
		set => _usdJpyPipValue.Value = value;
	}

	/// <summary>
	/// Default pip value used for other symbols.
	/// </summary>
	public decimal DefaultPipValue
	{
		get => _defaultPipValue.Value;
		set => _defaultPipValue.Value = value;
	}

	/// <summary>
	/// First year when new trades are allowed.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// First month when new trades are allowed.
	/// </summary>
	public int StartMonth
	{
		get => _startMonth.Value;
		set => _startMonth.Value = value;
	}

	/// <summary>
	/// Last year when new trades are allowed.
	/// </summary>
	public int EndYear
	{
		get => _endYear.Value;
		set => _endYear.Value = value;
	}

	/// <summary>
	/// Last month when new trades are allowed.
	/// </summary>
	public int EndMonth
	{
		get => _endMonth.Value;
		set => _endMonth.Value = value;
	}

	/// <summary>
	/// Timeframe used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the MACD indicator.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the MACD indicator.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length of the MACD indicator.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousMacd = null;
		_previousPreviousMacd = null;
		_openVolume = 0m;
		_averagePrice = 0m;
		_openTrades = 0;
		_isLongPosition = false;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_pipSize = 0m;
		_pipValue = 0m;
		_continueOpening = true;
		_currentDirection = null;
		_martingaleBaseVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Determine pip size for price to pip conversions.
		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		// Cache pip value for floating profit calculations.
		_pipValue = DeterminePipValue();
		_martingaleBaseVolume = CalculateBaseVolume();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		// Enable built-in position protection monitoring.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		var macdMain = macdValue.Macd;
		var previousMacd = _previousMacd;
		var previousPreviousMacd = _previousPreviousMacd;

		_previousPreviousMacd = previousMacd;
		_previousMacd = macdMain;

		var time = candle.CloseTime;
		if (!IsTradingWindowOpen(time))
			return;

		var currentPrice = candle.ClosePrice;

		// Manage existing basket before looking for new entries.
		if (_openTrades > 0)
		{
			ManageOpenPosition(currentPrice);
			if (_openTrades == 0)
				return;
		}

		_continueOpening = _openTrades < MaxTrades;
		if (!_continueOpening)
			return;

		// Respect manual mode by skipping automatic entries.
		if (ManualTrading)
			return;

		if (_openTrades == 0)
		{
			_currentDirection = DetermineDirection(previousMacd, previousPreviousMacd);
			if (_currentDirection.HasValue)
				TryOpenPosition(_currentDirection.Value, currentPrice);
		}
		else if (_currentDirection.HasValue)
		{
			TryAddPosition(_currentDirection.Value, currentPrice);
		}
	}

	private void ManageOpenPosition(decimal currentPrice)
	{
		if (_openVolume <= 0m)
			return;

		// Exit immediately if price hits the protective stop.
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

		// Take profit closes the entire basket.
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
				return;
			}
		}

		if (TrailingStopPips > 0m)
			UpdateTrailingStop(currentPrice);

		if (UseAccountProtection && _openTrades >= Math.Max(1, MaxTrades - OrdersToProtect))
		{
			var profit = CalculateFloatingProfit(currentPrice);
			var threshold = ProtectUsingBalance ? (Portfolio?.CurrentValue ?? 0m) : SecureProfit;
			if (profit >= threshold && _lastEntryVolume > 0m)
			{
				if (_isLongPosition)
					SellMarket(_lastEntryVolume);
				else
					BuyMarket(_lastEntryVolume);
				_continueOpening = false;
			}
		}
	}

	private void UpdateTrailingStop(decimal currentPrice)
	{
		var trailingDistance = ToPrice(TrailingStopPips);
		var threshold = trailingDistance + ToPrice(EntryDistancePips);

		if (_isLongPosition)
		{
			var profit = currentPrice - _averagePrice;
			if (profit >= threshold)
			{
				var newStop = currentPrice - trailingDistance;
				if (!_stopLossPrice.HasValue || newStop > _stopLossPrice.Value)
					_stopLossPrice = newStop;
			}
		}
		else
		{
			var profit = _averagePrice - currentPrice;
			if (profit >= threshold)
			{
				var newStop = currentPrice + trailingDistance;
				if (!_stopLossPrice.HasValue || newStop < _stopLossPrice.Value)
					_stopLossPrice = newStop;
			}
		}
	}

	private void TryOpenPosition(Sides direction, decimal currentPrice)
	{
		var volume = CalculateNextVolume();
		if (volume <= 0m)
			return;

		if (direction == Sides.Buy)
			BuyMarket(volume);
		else if (direction == Sides.Sell)
			SellMarket(volume);
	}

	private void TryAddPosition(Sides direction, decimal currentPrice)
	{
		var distance = ToPrice(EntryDistancePips);
		var canAdd = direction == Sides.Buy
			? (_lastEntryPrice - currentPrice) >= distance
			: (currentPrice - _lastEntryPrice) >= distance;

		if (!canAdd)
			return;

		TryOpenPosition(direction, currentPrice);
	}

	private Sides? DetermineDirection(decimal? macdPrev, decimal? macdPrevPrev)
	{
		if (!macdPrev.HasValue || !macdPrevPrev.HasValue)
			return null;

		var isBullish = macdPrev.Value > macdPrevPrev.Value;
		var isBearish = macdPrev.Value < macdPrevPrev.Value;

		if (!isBullish && !isBearish)
			return null;

		if (ReverseSignals)
			return isBullish ? Sides.Sell : Sides.Buy;

		return isBullish ? Sides.Buy : Sides.Sell;
	}

	private bool IsTradingWindowOpen(DateTimeOffset time)
	{
		if (_openTrades > 0)
			return true;

		if (time.Year < StartYear)
			return false;
		if (time.Year == StartYear && time.Month < StartMonth)
			return false;
		if (time.Year > EndYear)
			return false;
		if (time.Year == EndYear && time.Month > EndMonth)
			return false;
		return true;
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

	private decimal CalculateBaseVolume()
	{
		var volume = LotSize;

		if (UseMoneyManagement)
		{
			var balance = Portfolio?.CurrentValue ?? 0m;
			if (balance > 0m)
			{
				var riskValue = balance * RiskPercent / 100m;
				var rounded = Math.Ceiling(riskValue);
				volume = IsStandardAccount ? rounded : rounded / 10m;
			}
		}

		if (volume > 100m)
			volume = 100m;

		return volume;
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

		return volume;
	}

	private decimal DeterminePipValue()
	{
		var code = Security?.Code?.ToUpperInvariant();
		return code switch
		{
			"EURUSD" => EurUsdPipValue,
			"GBPUSD" => GbpUsdPipValue,
			"USDCHF" => UsdChfPipValue,
			"USDJPY" => UsdJpyPipValue,
			_ => DefaultPipValue,
		};
	}

	private decimal ToPrice(decimal pips)
	{
		return pips * _pipSize;
	}

	private void ResetPositionState()
	{
		_openVolume = 0m;
		_averagePrice = 0m;
		_openTrades = 0;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_continueOpening = true;
		_currentDirection = null;
	}

	private decimal? UpdateStopAfterEntry(bool isLong, decimal price)
	{
		if (InitialStopPips <= 0m)
			return _stopLossPrice;

		var stopOffset = ToPrice(InitialStopPips);
		if (isLong)
		{
			var candidate = price - stopOffset;
			return !_stopLossPrice.HasValue || candidate < _stopLossPrice.Value ? candidate : _stopLossPrice;
		}

		var candidateShort = price + stopOffset;
		return !_stopLossPrice.HasValue || candidateShort > _stopLossPrice.Value ? candidateShort : _stopLossPrice;
	}

	private decimal? UpdateTakeProfitAfterEntry(bool isLong, decimal price)
	{
		if (TakeProfitPips <= 0m)
			return _takeProfitPrice;

		var takeOffset = ToPrice(TakeProfitPips);
		if (isLong)
		{
			var candidate = price + takeOffset;
			return !_takeProfitPrice.HasValue || candidate > _takeProfitPrice.Value ? candidate : _takeProfitPrice;
		}

		var candidateShort = price - takeOffset;
		return !_takeProfitPrice.HasValue || candidateShort < _takeProfitPrice.Value ? candidateShort : _takeProfitPrice;
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
			_martingaleBaseVolume = CalculateBaseVolume();
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
			_martingaleBaseVolume = CalculateBaseVolume();
		}

		_continueOpening = _openTrades < MaxTrades;
	}

	private void HandlePositionReduction(decimal volume)
	{
		var closingVolume = Math.Min(_openVolume, volume);
		_openVolume -= closingVolume;
		if (_openVolume <= 0m)
			ResetPositionState();
		else if (_openTrades > 0)
			_openTrades--;
	}
}
