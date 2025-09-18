using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dealers Trade MACD strategy converted from the original MQL4 version (Dealers Trade v7.74).
/// </summary>
public class DealersTradeMacdMql4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _useRiskSizing;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _isStandardAccount;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _spacingPips;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<bool> _accountProtection;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<bool> _reverseCondition;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;

	private MovingAverageConvergenceDivergence _macd = null!;
	private readonly List<PositionState> _positions = new();
	private decimal? _previousMacd;
	private decimal _pipSize;
	private decimal _stepValue;

	/// <summary>
	/// Initializes a new instance of <see cref="DealersTradeMacdMql4Strategy"/>.
	/// </summary>
	public DealersTradeMacdMql4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for signals", "General");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetDisplay("Fixed Volume", "Lot size when risk sizing is disabled", "Risk");

		_useRiskSizing = Param(nameof(UseRiskSizing), true)
			.SetDisplay("Use Risk Sizing", "Enable balance based money management", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetDisplay("Risk Percent", "Percentage of equity used when sizing dynamically", "Risk");

		_isStandardAccount = Param(nameof(IsStandardAccount), true)
			.SetDisplay("Standard Account", "True for standard (1.0 lot) accounts, false for mini", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 5m)
			.SetDisplay("Max Volume", "Upper cap for any single order", "Risk")
			.SetGreaterThanZero();

		_lotMultiplier = Param(nameof(LotMultiplier), 1.5m)
			.SetDisplay("Lot Multiplier", "Multiplier applied to subsequent entries", "Money Management")
			.SetGreaterThanZero();

		_maxTrades = Param(nameof(MaxTrades), 5)
			.SetDisplay("Max Trades", "Maximum simultaneous positions", "Money Management")
			.SetGreaterThanZero();

		_spacingPips = Param(nameof(SpacingPips), 4)
			.SetDisplay("Spacing (pips)", "Minimum price movement before adding", "Money Management")
			.SetGreaterOrEqualZero();

		_ordersToProtect = Param(nameof(OrdersToProtect), 3)
			.SetDisplay("Orders To Protect", "Number of trades kept when protection triggers", "Money Management")
			.SetGreaterOrEqualZero();

		_accountProtection = Param(nameof(AccountProtection), true)
			.SetDisplay("Account Protection", "Close last trade once secure profit is reached", "Money Management");

		_secureProfit = Param(nameof(SecureProfit), 50m)
			.SetDisplay("Secure Profit", "Currency profit required to lock gains", "Money Management")
			.SetGreaterOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 30)
			.SetDisplay("Take Profit (pips)", "Take profit distance from entry", "Risk")
			.SetGreaterOrEqualZero();

		_stopLossPips = Param(nameof(StopLossPips), 90)
			.SetDisplay("Stop Loss (pips)", "Initial stop loss distance", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance applied after activation", "Risk")
			.SetGreaterOrEqualZero();

		_reverseCondition = Param(nameof(ReverseCondition), false)
			.SetDisplay("Reverse Condition", "Invert MACD slope interpretation", "General");

		_macdFast = Param(nameof(MacdFast), 14)
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
			.SetGreaterThanZero();

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
			.SetGreaterThanZero();

		_macdSignal = Param(nameof(MacdSignal), 1)
			.SetDisplay("MACD Signal", "Signal EMA length", "Indicators")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fixed order volume in lots.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Enables balance based position sizing.
	/// </summary>
	public bool UseRiskSizing
	{
		get => _useRiskSizing.Value;
		set => _useRiskSizing.Value = value;
	}

	/// <summary>
	/// Risk percentage applied when <see cref="UseRiskSizing"/> is true.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Indicates whether the account uses standard lot sizes.
	/// </summary>
	public bool IsStandardAccount
	{
		get => _isStandardAccount.Value;
		set => _isStandardAccount.Value = value;
	}

	/// <summary>
	/// Maximum volume allowed for a single order.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base size for subsequent entries.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
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
	/// Minimum spacing between entries expressed in pips.
	/// </summary>
	public int SpacingPips
	{
		get => _spacingPips.Value;
		set => _spacingPips.Value = value;
	}

	/// <summary>
	/// Number of orders that should remain protected before adding new exposure.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Enables the secure profit exit block.
	/// </summary>
	public bool AccountProtection
	{
		get => _accountProtection.Value;
		set => _accountProtection.Value = value;
	}

	/// <summary>
	/// Profit target used by the protection block.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Inverts the MACD slope interpretation.
	/// </summary>
	public bool ReverseCondition
	{
		get => _reverseCondition.Value;
		set => _reverseCondition.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD indicator.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD indicator.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA period for the MACD indicator.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
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
		_macd?.Reset();
		_positions.Clear();
		_previousMacd = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFast,
			Slow = MacdSlow,
			Signal = MacdSignal
		};

		_pipSize = GetPriceStep();
		_stepValue = Security?.StepPrice ?? 0m;
		if (_stepValue <= 0m)
			_stepValue = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal _, decimal __)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailingAndStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousMacd = macdValue;
			return;
		}

		var openTrades = _positions.Count;
		var allowNewTrade = openTrades < MaxTrades;

		if (_previousMacd is null)
		{
			_previousMacd = macdValue;
			return;
		}

		var direction = Math.Sign(macdValue - _previousMacd.Value);
		if (ReverseCondition)
			direction = -direction;

		if (AccountProtection && openTrades >= Math.Max(1, MaxTrades - OrdersToProtect))
		{
			var totalProfit = CalculateTotalProfit(candle.ClosePrice);
			if (totalProfit >= SecureProfit)
			{
				CloseLastPosition();
				_previousMacd = macdValue;
				return;
			}
		}

		if (allowNewTrade && direction > 0)
			TryOpen(Sides.Buy, candle);
		else if (allowNewTrade && direction < 0)
			TryOpen(Sides.Sell, candle);

		_previousMacd = macdValue;
	}

	private void TryOpen(Sides side, ICandleMessage candle)
	{
		var price = candle.ClosePrice;
		var spacing = SpacingPips * _pipSize;

		if (side == Sides.Buy)
		{
			var reference = GetReferencePrice(Sides.Buy);
			if (reference != 0m && reference - price < spacing)
				return;
		}
		else
		{
			var reference = GetReferencePrice(Sides.Sell);
			if (reference != 0m && price - reference < spacing)
				return;
		}

		var volume = CalculateVolume();
		if (volume <= 0m)
			return;

		var sameSideCount = CountPositions(side);
		if (sameSideCount > 0)
		{
			volume *= Pow(LotMultiplier, sameSideCount);
		}

		volume = NormalizeVolume(Math.Min(volume, MaxVolume));
		if (volume <= 0m)
			return;

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		var state = new PositionState
		{
			Side = side,
			Volume = volume,
			EntryPrice = price,
			StopPrice = stopDistance > 0m ? (side == Sides.Buy ? price - stopDistance : price + stopDistance) : (decimal?)null,
			TakeProfitPrice = takeDistance > 0m ? (side == Sides.Buy ? price + takeDistance : price - takeDistance) : (decimal?)null
		};

		_positions.Add(state);

	}

	private void UpdateTrailingAndStops(ICandleMessage candle)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		var activationDistance = (TrailingStopPips + SpacingPips) * _pipSize;

		for (var i = _positions.Count - 1; i >= 0; i--)
		{
			var state = _positions[i];

			if (state.Side == Sides.Buy)
			{
				if (state.TakeProfitPrice is decimal tp && candle.HighPrice >= tp)
				{
					SellMarket(state.Volume);
					_positions.RemoveAt(i);
					continue;
				}

				if (state.StopPrice is decimal sl && candle.LowPrice <= sl)
				{
					SellMarket(state.Volume);
					_positions.RemoveAt(i);
					continue;
				}

				if (TrailingStopPips > 0 && candle.ClosePrice - state.EntryPrice >= activationDistance)
				{
					var candidate = candle.ClosePrice - trailingDistance;
					if (state.StopPrice is null || state.StopPrice < candidate)
						state.StopPrice = candidate;
				}
			}
			else
			{
				if (state.TakeProfitPrice is decimal tp && candle.LowPrice <= tp)
				{
					BuyMarket(state.Volume);
					_positions.RemoveAt(i);
					continue;
				}

				if (state.StopPrice is decimal sl && candle.HighPrice >= sl)
				{
					BuyMarket(state.Volume);
					_positions.RemoveAt(i);
					continue;
				}

				if (TrailingStopPips > 0 && state.EntryPrice - candle.ClosePrice >= activationDistance)
				{
					var candidate = candle.ClosePrice + trailingDistance;
					if (state.StopPrice is null || state.StopPrice > candidate)
						state.StopPrice = candidate;
				}
			}
		}
	}

	private decimal CalculateVolume()
	{
		decimal baseVolume;

		if (UseRiskSizing)
		{
			if (Portfolio is null)
				return 0m;

			var balance = Portfolio.CurrentValue;
			if (balance <= 0m)
				return 0m;

			var rawLots = Math.Ceiling(balance * (RiskPercent / 100m) / 10000m);
			if (!IsStandardAccount)
				rawLots /= 10m;

			baseVolume = rawLots;
		}
		else
		{
			baseVolume = FixedVolume;
		}

		return baseVolume;
	}

	private decimal CalculateTotalProfit(decimal currentPrice)
	{
		decimal profit = 0m;

		foreach (var state in _positions)
		{
			var priceDifference = state.Side == Sides.Buy
				? currentPrice - state.EntryPrice
				: state.EntryPrice - currentPrice;

			var steps = _pipSize > 0m ? priceDifference / _pipSize : priceDifference;
			profit += steps * _stepValue * state.Volume;
		}

		return profit;
	}

	private void CloseLastPosition()
	{
		if (_positions.Count == 0)
			return;

		var index = _positions.Count - 1;
		var state = _positions[index];

		if (state.Side == Sides.Buy)
			SellMarket(state.Volume);
		else
			BuyMarket(state.Volume);

		_positions.RemoveAt(index);

	}

	private decimal GetReferencePrice(Sides side)
	{
		for (var i = _positions.Count - 1; i >= 0; i--)
		{
			var state = _positions[i];
			if (state.Side == side)
				return state.EntryPrice;
		}

		return 0m;
	}

	private int CountPositions(Sides side)
	{
		var count = 0;
		for (var i = 0; i < _positions.Count; i++)
		{
			if (_positions[i].Side == side)
				count++;
		}

		return count;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0)
				steps = 1;
			volume = steps * step;
		}
		else
		{
			volume = Math.Round(volume, 1, MidpointRounding.AwayFromZero);
			if (volume <= 0m)
				volume = 0.1m;
		}

		return volume;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		var decimals = Security?.Decimals ?? 0;
		if (decimals > 0)
			return (decimal)Math.Pow(10, -decimals);

		return 0.0001m;
	}

	private static decimal Pow(decimal value, int power)
	{
		if (power <= 0)
			return 1m;

		return (decimal)Math.Pow((double)value, power);
	}

	private sealed class PositionState
	{
		public Sides Side { get; set; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? StopPrice { get; set; }
		public decimal? TakeProfitPrice { get; set; }
	}
}
