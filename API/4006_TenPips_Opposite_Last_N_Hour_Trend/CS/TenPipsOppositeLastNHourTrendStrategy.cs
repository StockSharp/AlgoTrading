namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades once per day against the direction of the last N hourly candles.
/// Lot sizing mimics the martingale multipliers of the original MQL expert.
/// </summary>
public class TenPipsOppositeLastNHourTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<decimal> _maximumVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<int> _tradingHour;
	private readonly StrategyParam<int> _hoursToCheckTrend;
	private readonly StrategyParam<TimeSpan> _orderMaxAge;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _firstMultiplier;
	private readonly StrategyParam<decimal> _secondMultiplier;
	private readonly StrategyParam<decimal> _thirdMultiplier;
	private readonly StrategyParam<decimal> _fourthMultiplier;
	private readonly StrategyParam<decimal> _fifthMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<int> _tradingDayHours;
	private readonly List<decimal> _closedTradeProfits = new();
	private readonly List<decimal> _closeHistory = new();

	private decimal _pipSize;
	private DateTimeOffset? _lastBarTraded;
	private Sides? _entrySide;
	private decimal _entryVolume;
	private decimal? _entryPrice;
	private DateTimeOffset? _entryTime;
	private decimal? _trailingStopPrice;

	/// <summary>
	/// Fixed volume for market entries. When zero the strategy uses risk based sizing.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Minimum allowed volume after all adjustments.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed volume after all adjustments.
	/// </summary>
	public decimal MaximumVolume
	{
		get => _maximumVolume.Value;
		set => _maximumVolume.Value = value;
	}

	/// <summary>
	/// Fraction of account value risked when FixedVolume is zero.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open orders and positions.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when the strategy is allowed to open a trade.
	/// </summary>
	public int TradingHour
	{
		get => _tradingHour.Value;
		set => _tradingHour.Value = value;
	}

	/// <summary>
	/// Number of hours used to evaluate the opposite trend.
	/// </summary>
	public int HoursToCheckTrend
	{
		get => _hoursToCheckTrend.Value;
		set => _hoursToCheckTrend.Value = value;
	}

	/// <summary>
	/// Maximum allowed lifetime for an open position.
	/// </summary>
	public TimeSpan OrderMaxAge
	{
		get => _orderMaxAge.Value;
		set => _orderMaxAge.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied after the most recent losing trade.
	/// </summary>
	public decimal FirstMultiplier
	{
		get => _firstMultiplier.Value;
		set => _firstMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied when the last trade was profitable but the previous one lost.
	/// </summary>
	public decimal SecondMultiplier
	{
		get => _secondMultiplier.Value;
		set => _secondMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied when only the third most recent trade lost.
	/// </summary>
	public decimal ThirdMultiplier
	{
		get => _thirdMultiplier.Value;
		set => _thirdMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied when only the fourth most recent trade lost.
	/// </summary>
	public decimal FourthMultiplier
	{
		get => _fourthMultiplier.Value;
		set => _fourthMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied when only the fifth most recent trade lost.
	/// </summary>
	public decimal FifthMultiplier
	{
		get => _fifthMultiplier.Value;
		set => _fifthMultiplier.Value = value;
	}

	/// <summary>
	/// Type of candles processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TenPipsOppositeLastNHourTrendStrategy"/> class.
	/// </summary>
	public TenPipsOppositeLastNHourTrendStrategy()
	{
		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 1m, 0.1m);

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
		.SetDisplay("Minimum Volume", "Minimum allowed volume", "Risk");

		_maximumVolume = Param(nameof(MaximumVolume), 5m)
		.SetDisplay("Maximum Volume", "Maximum allowed volume", "Risk");

		_maximumRisk = Param(nameof(MaximumRisk), 0.05m)
		.SetDisplay("Maximum Risk", "Risk fraction when Fixed Volume is zero", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 0.2m, 0.01m);

		_maxOrders = Param(nameof(MaxOrders), 1)
		.SetDisplay("Max Orders", "Maximum simultaneous orders", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1, 3, 1);

		_tradingHour = Param(nameof(TradingHour), 7)
		.SetDisplay("Trading Hour", "Hour when entries are allowed", "Trading");

		_hoursToCheckTrend = Param(nameof(HoursToCheckTrend), 30)
		.SetDisplay("Hours To Check Trend", "Look-back hours for trend detection", "Trading")
		.SetGreaterThanZero();

		_orderMaxAge = Param(nameof(OrderMaxAge), TimeSpan.FromSeconds(75600))
		.SetDisplay("Order Max Age", "Maximum position lifetime", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
		.SetDisplay("Trailing Stop (pips)", "Trailing-stop distance in pips", "Risk");

		_firstMultiplier = Param(nameof(FirstMultiplier), 4m)
		.SetDisplay("First Multiplier", "Multiplier after the last loss", "Money Management");

		_secondMultiplier = Param(nameof(SecondMultiplier), 2m)
		.SetDisplay("Second Multiplier", "Multiplier if only the previous trade lost", "Money Management");

		_thirdMultiplier = Param(nameof(ThirdMultiplier), 5m)
		.SetDisplay("Third Multiplier", "Multiplier if only the third trade lost", "Money Management");

		_fourthMultiplier = Param(nameof(FourthMultiplier), 5m)
		.SetDisplay("Fourth Multiplier", "Multiplier if only the fourth trade lost", "Money Management");

		_fifthMultiplier = Param(nameof(FifthMultiplier), 1m)
		.SetDisplay("Fifth Multiplier", "Multiplier if only the fifth trade lost", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for analysis", "Trading");

		_tradingDayHours = new List<int>(24);
		for (var hour = 0; hour < 24; hour++)
		_tradingDayHours.Add(hour);
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

		_closedTradeProfits.Clear();
		_closeHistory.Clear();
		_lastBarTraded = null;
		_entrySide = null;
		_entryVolume = 0m;
		_entryPrice = null;
		_entryTime = null;
		_trailingStopPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateCloseHistory(candle.ClosePrice);

		if (Position != 0 && UpdateProtectiveLogic(candle))
		return;

		if (Position != 0 && CloseExpiredPosition(candle.CloseTime))
		return;

		if (!IsTradingHour(candle.CloseTime))
		{
			FlattenOutsideTradingHours();
			return;
		}

		if (!HasTrendSample())
		return;

		if (!CanOpenOnBar(candle.OpenTime))
		return;

		if (MaxOrders > 0 && ActiveOrders.Count >= MaxOrders)
		return;

		if (Position != 0 || ActiveOrders.Count != 0)
		return;

		var direction = DetermineDirection();
		if (direction == 0)
		return;

		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		if (direction > 0)
		{
			// Enter long against a bearish move in the look-back window.
			BuyMarket(volume);
		}
		else
		{
			// Enter short against a bullish move in the look-back window.
			SellMarket(volume);
		}

		_lastBarTraded = candle.OpenTime;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Trade == null)
		return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var time = trade.Trade.ServerTime;

		if (volume <= 0m || price <= 0m)
		return;

		if (_entrySide == null || _entrySide == trade.Order.Side)
		{
			RegisterEntryTrade(price, volume, trade.Order.Side, time);
		}
		else
		{
			RegisterExitTrade(price, volume, time);
		}
	}

	private void RegisterEntryTrade(decimal price, decimal volume, Sides side, DateTimeOffset time)
	{
		// Weighted-average entry price for pyramided fills.
		var totalVolume = _entryVolume + volume;
		if (totalVolume <= 0m)
		{
			_entryVolume = 0m;
			_entryPrice = null;
			_entrySide = null;
			_entryTime = null;
			_trailingStopPrice = null;
			return;
		}

		_entryPrice = _entryVolume > 0m && _entryPrice.HasValue
		? ((_entryPrice.Value * _entryVolume) + (price * volume)) / totalVolume
		: price;

		_entryVolume = totalVolume;
		_entrySide = side;
		_entryTime ??= time;

		var trailingDistance = GetTrailingDistance();
		if (TrailingStopPips > 0m && trailingDistance > 0m)
		{
			_trailingStopPrice = side == Sides.Buy
			? _entryPrice - trailingDistance
			: _entryPrice + trailingDistance;
		}
	}

	private void RegisterExitTrade(decimal price, decimal volume, DateTimeOffset time)
	{
		if (_entrySide == null || !_entryPrice.HasValue || _entryVolume <= 0m)
		return;

		var remaining = _entryVolume - volume;
		if (remaining < 0m)
		remaining = 0m;

		decimal profit = 0m;
		if (_entrySide == Sides.Buy)
		profit = (price - _entryPrice.Value) * volume;
		else if (_entrySide == Sides.Sell)
		profit = (_entryPrice.Value - price) * volume;

		AddClosedTradeProfit(profit);

		if (remaining == 0m)
		{
			ResetEntryState();
		}
		else
		{
			_entryVolume = remaining;
			_entryTime = time;
		}
	}

	private bool UpdateProtectiveLogic(ICandleMessage candle)
	{
		if (_entrySide == null || !_entryPrice.HasValue || _entryVolume <= 0m)
		return false;

		var pip = EnsurePipSize();
		if (pip <= 0m)
		return false;

		var stopLoss = StopLossPips * pip;
		var takeProfit = TakeProfitPips * pip;
		var trailingDistance = TrailingStopPips * pip;

		if (_entrySide == Sides.Buy)
		{
			if (StopLossPips > 0m && candle.LowPrice <= _entryPrice.Value - stopLoss)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}

			if (TakeProfitPips > 0m && candle.HighPrice >= _entryPrice.Value + takeProfit)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}

			if (TrailingStopPips > 0m && trailingDistance > 0m)
			{
				var candidate = candle.HighPrice - trailingDistance;
				if (candidate > (_trailingStopPrice ?? decimal.MinValue) && candle.HighPrice - _entryPrice.Value > trailingDistance)
				_trailingStopPrice = candidate;

				if (_trailingStopPrice.HasValue && candle.LowPrice <= _trailingStopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					return true;
				}
			}
		}
		else if (_entrySide == Sides.Sell)
		{
			if (StopLossPips > 0m && candle.HighPrice >= _entryPrice.Value + stopLoss)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (TakeProfitPips > 0m && candle.LowPrice <= _entryPrice.Value - takeProfit)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (TrailingStopPips > 0m && trailingDistance > 0m)
			{
				var candidate = candle.LowPrice + trailingDistance;
				if (!_trailingStopPrice.HasValue || candidate < _trailingStopPrice.Value)
				_trailingStopPrice = candidate;

				if (_trailingStopPrice.HasValue && candle.HighPrice >= _trailingStopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					return true;
				}
			}
		}

		return false;
	}

	private bool CloseExpiredPosition(DateTimeOffset time)
	{
		if (OrderMaxAge <= TimeSpan.Zero || _entryTime == null)
		return false;

		if (time - _entryTime < OrderMaxAge)
		return false;

		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
			return true;
		}

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			return true;
		}

		return false;
	}

	private bool IsTradingHour(DateTimeOffset time)
	{
		if (TradingHour < 0 || TradingHour > 23)
		return false;

		if (!_tradingDayHours.Contains(time.Hour))
		return false;

		return time.Hour == TradingHour;
	}

	private bool CanOpenOnBar(DateTimeOffset barOpenTime)
	{
		if (_lastBarTraded.HasValue && _lastBarTraded.Value == barOpenTime)
		return false;

		return true;
	}

	private void FlattenOutsideTradingHours()
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private bool HasTrendSample()
	{
		return HoursToCheckTrend > 0 && _closeHistory.Count >= HoursToCheckTrend;
	}

	private int DetermineDirection()
	{
		if (_closeHistory.Count == 0)
		return 0;

		var latestIndex = _closeHistory.Count - 1;
		var recentClose = _closeHistory[latestIndex];

		var olderIndex = _closeHistory.Count - HoursToCheckTrend;
		if (olderIndex < 0 || olderIndex >= _closeHistory.Count)
		return 0;

		var olderClose = _closeHistory[olderIndex];

		return olderClose > recentClose ? 1 : -1;
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		decimal baseVolume;

		if (FixedVolume > 0m)
		{
			baseVolume = FixedVolume;
		}
		else
		{
			var equity = Portfolio?.CurrentValue ?? 0m;
			if (equity > 0m && MaximumRisk > 0m)
			{
				baseVolume = RoundToOneDecimal(equity * MaximumRisk / 1000m);
			}
			else
			{
				baseVolume = Volume > 0m ? Volume : 1m;
			}
		}

		baseVolume = ApplyLossMultipliers(baseVolume);

		var equityCap = Portfolio?.CurrentValue ?? 0m;
		if (equityCap > 0m)
		{
			var cap = RoundToOneDecimal(equityCap / 1000m);
			if (cap > 0m && baseVolume > cap)
			baseVolume = cap;
		}

		if (baseVolume < MinimumVolume)
		baseVolume = MinimumVolume;
		else if (baseVolume > MaximumVolume)
		baseVolume = MaximumVolume;

		return AdjustVolume(baseVolume);
	}

	private decimal ApplyLossMultipliers(decimal volume)
	{
		if (_closedTradeProfits.Count == 0)
		return volume;

		var multipliers = new[]
		{
			FirstMultiplier,
			SecondMultiplier,
			ThirdMultiplier,
			FourthMultiplier,
			FifthMultiplier,
		};

		var count = _closedTradeProfits.Count;
		for (var i = 0; i < multipliers.Length; i++)
		{
			if (count <= i)
			break;

			var profit = _closedTradeProfits[count - 1 - i];
			if (profit < 0m)
			{
				volume *= multipliers[i];
				break;
			}

			if (profit > 0m)
			break;
		}

		return volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep;
			if (step is decimal stepValue && stepValue > 0m)
			volume = Math.Round(volume / stepValue, MidpointRounding.AwayFromZero) * stepValue;

			var min = security.VolumeMin;
			if (min is decimal minVolume && volume < minVolume)
			volume = minVolume;

			var max = security.VolumeMax;
			if (max is decimal maxVolume && maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;
		}

		return volume > 0m ? volume : 0m;
	}

	private void UpdateCloseHistory(decimal close)
	{
		if (close <= 0m)
		return;

		_closeHistory.Add(close);

		var maxLength = Math.Max(HoursToCheckTrend + 2, 64);
		while (_closeHistory.Count > maxLength)
		_closeHistory.RemoveAt(0);
	}

	private void AddClosedTradeProfit(decimal profit)
	{
		_closedTradeProfits.Add(profit);
		while (_closedTradeProfits.Count > 5)
		_closedTradeProfits.RemoveAt(0);
	}

	private void ResetEntryState()
	{
		_entrySide = null;
		_entryVolume = 0m;
		_entryPrice = null;
		_entryTime = null;
		_trailingStopPrice = null;
	}

	private void InitializePipSize()
	{
		var security = Security;
		if (security == null)
		{
			_pipSize = 0m;
			return;
		}

		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		if (security.Decimals is int decimals && (decimals == 3 || decimals == 5))
		_pipSize = step * 10m;
		else
		_pipSize = step;
	}

	private decimal EnsurePipSize()
	{
		if (_pipSize <= 0m)
		InitializePipSize();

		return _pipSize;
	}

	private decimal GetTrailingDistance()
	{
		var pip = EnsurePipSize();
		return pip > 0m ? TrailingStopPips * pip : 0m;
	}

	private static decimal RoundToOneDecimal(decimal value)
	{
		return Math.Round(value, 1, MidpointRounding.AwayFromZero);
	}
}
