using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the classic "10points 3" MetaTrader 4 grid expert advisor to the StockSharp high level API.
/// The strategy evaluates the MACD slope to decide between long and short grids and manages exits through
/// initial stops, trailing stops and an optional equity protection target.
/// </summary>
public class TenPoints3Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _gridSpacingPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialStopPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<bool> _accountProtectionEnabled;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPerTenThousand;
	private readonly StrategyParam<bool> _isStandardAccount;
	private readonly StrategyParam<decimal> _maxVolumeCap;

	private decimal? _previousMacd;
	private int _gridOrders;
	private decimal _gridAveragePrice;
	private decimal _gridNetVolume;
	private Sides? _gridDirection;
	private decimal _lastEntryPrice;
	private decimal? _trailingStopPrice;
	private bool _awaitingFlat;

	/// <summary>
	/// Initializes strategy parameters with defaults close to the original expert advisor.
	/// </summary>
	public TenPoints3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for MACD evaluation and execution.", "Data");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for the MACD indicator.", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for the MACD indicator.", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing period for MACD.", "Indicators");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial order volume before money management adjustments.", "Trading");

		_gridSpacingPoints = Param(nameof(GridSpacingPoints), 15m)
			.SetNotNegative()
			.SetDisplay("Grid Spacing", "Minimum distance in points before adding a new order to the grid.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Distance in points used for the fixed take profit target.", "Risk");

		_initialStopPoints = Param(nameof(InitialStopPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Initial Stop", "Distance in points for the initial protective stop.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Distance in points kept when the trailing stop activates.", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 9)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of simultaneous grid entries per direction.", "Trading");

		_ordersToProtect = Param(nameof(OrdersToProtect), 3)
			.SetNotNegative()
			.SetDisplay("Orders To Protect", "Number of open trades required before equity protection can trigger.", "Risk");

		_secureProfit = Param(nameof(SecureProfit), 8m)
			.SetNotNegative()
			.SetDisplay("Secure Profit", "Unrealized profit in points*volume required to close all trades.", "Risk");

		_accountProtectionEnabled = Param(nameof(AccountProtectionEnabled), true)
			.SetDisplay("Account Protection", "Close the entire grid once the secure profit target is reached.", "Risk");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert the buy/sell logic derived from the MACD slope.", "Trading");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Money Management", "Recalculate the base volume from portfolio value and risk.", "Money management");

		_riskPerTenThousand = Param(nameof(RiskPerTenThousand), 12m)
			.SetNotNegative()
			.SetDisplay("Risk (1/10000)", "Risk amount per 10,000 units of balance when money management is enabled.", "Money management");

		_isStandardAccount = Param(nameof(IsStandardAccount), true)
			.SetDisplay("Standard Account", "Controls lot rounding when money management is active.", "Money management");

		_maxVolumeCap = Param(nameof(MaxVolumeCap), 100m)
			.SetNotNegative()
			.SetDisplay("Max Volume", "Safety cap for the calculated position size.", "Trading");
	}

	/// <summary>
	/// Candle type used for MACD and trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD indicator.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD indicator.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal smoothing period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Base volume for the first grid order.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Grid spacing measured in points.
	/// </summary>
	public decimal GridSpacingPoints
	{
		get => _gridSpacingPoints.Value;
		set => _gridSpacingPoints.Value = value;
	}

	/// <summary>
	/// Fixed take profit distance measured in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance measured in points.
	/// </summary>
	public decimal InitialStopPoints
	{
		get => _initialStopPoints.Value;
		set => _initialStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of grid trades per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Open orders required before enabling the equity protection exit.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Unrealized profit threshold (points multiplied by volume) required to flatten the grid.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Enables the secure profit close out logic.
	/// </summary>
	public bool AccountProtectionEnabled
	{
		get => _accountProtectionEnabled.Value;
		set => _accountProtectionEnabled.Value = value;
	}

	/// <summary>
	/// Inverts buy and sell conditions.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Enables dynamic lot calculation.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk portion per 10,000 units of balance when dynamic sizing is enabled.
	/// </summary>
	public decimal RiskPerTenThousand
	{
		get => _riskPerTenThousand.Value;
		set => _riskPerTenThousand.Value = value;
	}

	/// <summary>
	/// Indicates if the portfolio uses standard lots (true) or mini lots (false) in the original EA logic.
	/// </summary>
	public bool IsStandardAccount
	{
		get => _isStandardAccount.Value;
		set => _isStandardAccount.Value = value;
	}

	/// <summary>
	/// Maximum allowed position size after all martingale multipliers are applied.
	/// </summary>
	public decimal MaxVolumeCap
	{
		get => _maxVolumeCap.Value;
		set => _maxVolumeCap.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ManageExistingPosition(candle))
			return;

		if (_awaitingFlat)
			return;

		if (AccountProtectionEnabled && OrdersToProtect > 0 && _gridOrders >= OrdersToProtect)
		{
			var unrealized = CalculateUnrealizedProfit(candle.ClosePrice);
			if (unrealized >= SecureProfit)
			{
				FlattenPosition();
				return;
			}
		}

		if (!macdValue.IsFinal)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdMain = macdTyped.Macd;

		if (macdMain is not decimal currentMacd)
			return;

		var previousMacd = _previousMacd;
		_previousMacd = currentMacd;
		if (previousMacd is null)
			return;

		var signalSide = DetermineSignal(currentMacd, previousMacd.Value);
		if (signalSide is null)
			return;

		if (ReverseSignals)
			signalSide = signalSide == Sides.Buy ? Sides.Sell : Sides.Buy;

		if (_gridOrders >= MaxTrades)
			return;

		if (_gridOrders > 0)
		{
			if (_gridDirection != signalSide)
				return;

			if (!IsSpacingSatisfied(signalSide.Value, candle.ClosePrice))
				return;
		}

		TryOpenPosition(signalSide.Value, candle.ClosePrice);
	}

	private bool ManageExistingPosition(ICandleMessage candle)
	{
		if (_gridOrders == 0)
			return false;

		var direction = _gridDirection;
		if (direction is null)
			return false;

		var step = GetPriceStep();
		var absVolume = Math.Abs(_gridNetVolume);
		if (absVolume <= 0m)
			return false;

		var exitTriggered = false;

		if (direction == Sides.Buy)
		{
			if (InitialStopPoints > 0m)
			{
				var stop = _gridAveragePrice - InitialStopPoints * step;
				if (candle.LowPrice <= stop)
					exitTriggered = true;
			}

			if (!exitTriggered && TakeProfitPoints > 0m)
			{
				var target = _gridAveragePrice + TakeProfitPoints * step;
				if (candle.HighPrice >= target)
					exitTriggered = true;
			}

			if (!exitTriggered && TrailingStopPoints > 0m)
			{
				var triggerDistance = (TrailingStopPoints + GridSpacingPoints) * step;
				if (candle.ClosePrice - _gridAveragePrice >= triggerDistance)
				{
					var newStop = candle.ClosePrice - TrailingStopPoints * step;
					if (!_trailingStopPrice.HasValue || newStop > _trailingStopPrice.Value)
						_trailingStopPrice = newStop;
				}

				if (_trailingStopPrice.HasValue && candle.LowPrice <= _trailingStopPrice.Value)
					exitTriggered = true;
			}
		}
		else
		{
			if (InitialStopPoints > 0m)
			{
				var stop = _gridAveragePrice + InitialStopPoints * step;
				if (candle.HighPrice >= stop)
					exitTriggered = true;
			}

			if (!exitTriggered && TakeProfitPoints > 0m)
			{
				var target = _gridAveragePrice - TakeProfitPoints * step;
				if (candle.LowPrice <= target)
					exitTriggered = true;
			}

			if (!exitTriggered && TrailingStopPoints > 0m)
			{
				var triggerDistance = (TrailingStopPoints + GridSpacingPoints) * step;
				if (_gridAveragePrice - candle.ClosePrice >= triggerDistance)
				{
					var newStop = candle.ClosePrice + TrailingStopPoints * step;
					if (!_trailingStopPrice.HasValue || newStop < _trailingStopPrice.Value)
						_trailingStopPrice = newStop;
				}

				if (_trailingStopPrice.HasValue && candle.HighPrice >= _trailingStopPrice.Value)
					exitTriggered = true;
			}
		}

		if (!exitTriggered)
			return false;

		FlattenPosition();
		return true;
	}

	private void FlattenPosition()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Math.Abs(_gridNetVolume);
		if (volume <= 0m)
			volume = Math.Abs(Position);

		if (volume <= 0m)
		{
			ResetPositionState();
			return;
		}

		if (_gridDirection == Sides.Buy)
			SellMarket(volume);
		else if (_gridDirection == Sides.Sell)
			BuyMarket(volume);

		_awaitingFlat = true;
	}

	private void TryOpenPosition(Sides side, decimal referencePrice)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume(_gridOrders);
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_lastEntryPrice = referencePrice;
	}

	private bool IsSpacingSatisfied(Sides side, decimal currentPrice)
	{
		var spacing = GridSpacingPoints * GetPriceStep();
		if (spacing <= 0m)
			return true;

		if (_lastEntryPrice <= 0m)
			return true;

		return side == Sides.Buy
			? _lastEntryPrice - currentPrice >= spacing
			: currentPrice - _lastEntryPrice >= spacing;
	}

	private decimal CalculateOrderVolume(int openOrders)
	{
		var volume = UseMoneyManagement ? CalculateManagedVolume() : BaseVolume;

		if (openOrders > 0)
		{
			var multiplierBase = MaxTrades > 12 ? 1.5 : 2.0;
			var multiplier = (decimal)Math.Pow(multiplierBase, openOrders);
			volume *= multiplier;
		}

		var minVolume = Security?.MinVolume ?? 0.01m;
		var maxVolume = Security?.MaxVolume ?? 0m;
		if (MaxVolumeCap > 0m && (maxVolume <= 0m || MaxVolumeCap < maxVolume))
			maxVolume = MaxVolumeCap;

		if (volume < minVolume)
			volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		var step = Security?.VolumeStep ?? 0.01m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
				steps = 1m;
			volume = steps * step;
		}

		return volume;
	}

	private decimal CalculateManagedVolume()
	{
		if (Portfolio is not Portfolio portfolio)
			return BaseVolume;

		var balance = portfolio.CurrentValue;
		if (balance <= 0m || RiskPerTenThousand <= 0m)
			return BaseVolume;

		var lots = Math.Ceiling(balance * RiskPerTenThousand / 10000m);
		if (!IsStandardAccount)
			lots /= 10m;

		return lots <= 0m ? BaseVolume : lots;
	}

	private decimal CalculateUnrealizedProfit(decimal currentPrice)
	{
		if (_gridOrders == 0)
			return 0m;

		var step = GetPriceStep();
		if (step <= 0m)
			return 0m;

		var diff = _gridDirection == Sides.Buy
			? currentPrice - _gridAveragePrice
			: _gridAveragePrice - currentPrice;

		return diff / step * Math.Abs(_gridNetVolume);
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private Sides? DetermineSignal(decimal currentMacd, decimal previousMacd)
	{
		if (currentMacd > previousMacd)
			return Sides.Buy;

		if (currentMacd < previousMacd)
			return Sides.Sell;

		return null;
	}

	private void ResetPositionState()
	{
		_gridNetVolume = 0m;
		_gridAveragePrice = 0m;
		_gridOrders = 0;
		_gridDirection = null;
		_lastEntryPrice = 0m;
		_trailingStopPrice = null;
		_awaitingFlat = false;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		var execution = trade.Trade;
		if (order == null || execution == null)
			return;

		var tradePrice = execution.Price;
		var tradeVolume = trade.Volume;
		if (tradeVolume <= 0m)
			return;

		var direction = order.Side;
		var sign = direction == Sides.Buy ? 1m : -1m;
		var previousNet = _gridNetVolume;
		var newNet = previousNet + sign * tradeVolume;

		if (newNet == 0m)
		{
			ResetPositionState();
			return;
		}

		if (previousNet == 0m || Math.Sign(previousNet) != Math.Sign(newNet))
		{
			_gridAveragePrice = tradePrice;
			_gridOrders = 1;
			_trailingStopPrice = null;
		}
		else if (Math.Sign(sign) == Math.Sign(newNet))
		{
			var prevAbs = Math.Abs(previousNet);
			var newAbs = Math.Abs(newNet);
			_gridAveragePrice = (_gridAveragePrice * prevAbs + tradePrice * tradeVolume) / newAbs;
			_gridOrders++;
		}
		else
		{
			if (_gridOrders > 1)
				_gridOrders--;
			else
				_gridOrders = 1;
		}

		_gridNetVolume = newNet;
		_gridDirection = newNet > 0m ? Sides.Buy : Sides.Sell;
		_lastEntryPrice = tradePrice;
		_awaitingFlat = false;
	}
}
