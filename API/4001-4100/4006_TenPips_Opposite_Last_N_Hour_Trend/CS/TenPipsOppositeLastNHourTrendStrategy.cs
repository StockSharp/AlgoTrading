namespace StockSharp.Samples.Strategies;

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
	private readonly TenPipsTradeEpisode _episode = new();

	private decimal _pipSize;
	private DateTime? _lastTradeDate;
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
		
		.SetOptimize(0m, 1m, 0.1m);

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
		.SetDisplay("Minimum Volume", "Minimum allowed volume", "Risk");

		_maximumVolume = Param(nameof(MaximumVolume), 5m)
		.SetDisplay("Maximum Volume", "Maximum allowed volume", "Risk");

		_maximumRisk = Param(nameof(MaximumRisk), 0.05m)
		.SetDisplay("Maximum Risk", "Risk fraction when Fixed Volume is zero", "Risk")
		
		.SetOptimize(0m, 0.2m, 0.01m);

		_maxOrders = Param(nameof(MaxOrders), 1)
		.SetDisplay("Max Orders", "Maximum simultaneous orders", "Trading")
		
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
		_episode.Reset();
		_lastTradeDate = null;
		_trailingStopPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		StartProtection(null, null);
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

		if (!CanOpenOnDay(candle.CloseTime))
		return;

		if (Position != 0)
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

		_lastTradeDate = candle.CloseTime.Date;
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

		if (!_episode.IsOpen || _episode.Side == trade.Order.Side)
		{
			RegisterEntryTrade(price, volume, trade.Order.Side, time);
		}
		else
		{
			RegisterExitTrade(price, volume);
		}
	}

	private void RegisterEntryTrade(decimal price, decimal volume, Sides side, DateTimeOffset time)
	{
		_episode.RegisterEntry(price, volume, side, time);

		var trailingDistance = GetTrailingDistance();
		if (TrailingStopPips > 0m && trailingDistance > 0m)
		{
			_trailingStopPrice = _episode.Side == Sides.Buy
			? _episode.EntryPrice - trailingDistance
			: _episode.EntryPrice + trailingDistance;
		}
	}

	private void RegisterExitTrade(decimal price, decimal volume)
	{
		var closedProfit = _episode.RegisterExit(price, volume);
		if (closedProfit.HasValue)
		{
			AddClosedTradeProfit(closedProfit.Value);
			_trailingStopPrice = null;
		}
	}

	private bool UpdateProtectiveLogic(ICandleMessage candle)
	{
		if (!_episode.IsOpen)
		return false;

		var entrySide = _episode.Side.Value;
		var entryPrice = _episode.EntryPrice.Value;

		var pip = EnsurePipSize();
		if (pip <= 0m)
		return false;

		var stopLoss = StopLossPips * pip;
		var takeProfit = TakeProfitPips * pip;
		var trailingDistance = TrailingStopPips * pip;

		if (entrySide == Sides.Buy)
		{
			if (StopLossPips > 0m && candle.LowPrice <= entryPrice - stopLoss)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}

			if (TakeProfitPips > 0m && candle.HighPrice >= entryPrice + takeProfit)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}

			if (TrailingStopPips > 0m && trailingDistance > 0m)
			{
				// The candidate derived below becomes active on the next candle.
				if (_trailingStopPrice.HasValue && candle.LowPrice <= _trailingStopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					return true;
				}

				var candidate = candle.HighPrice - trailingDistance;
				if (candidate > (_trailingStopPrice ?? decimal.MinValue) && candle.HighPrice - entryPrice > trailingDistance)
					_trailingStopPrice = candidate;
			}
		}
		else if (entrySide == Sides.Sell)
		{
			if (StopLossPips > 0m && candle.HighPrice >= entryPrice + stopLoss)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (TakeProfitPips > 0m && candle.LowPrice <= entryPrice - takeProfit)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (TrailingStopPips > 0m && trailingDistance > 0m)
			{
				// The candidate derived below becomes active on the next candle.
				if (_trailingStopPrice.HasValue && candle.HighPrice >= _trailingStopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					return true;
				}

				var candidate = candle.LowPrice + trailingDistance;
				if (!_trailingStopPrice.HasValue || candidate < _trailingStopPrice.Value)
					_trailingStopPrice = candidate;
			}
		}

		return false;
	}

	private bool CloseExpiredPosition(DateTimeOffset time)
	{
		var entryTime = _episode.EntryTime;
		if (OrderMaxAge <= TimeSpan.Zero || !entryTime.HasValue)
		return false;

		if (time - entryTime.Value < OrderMaxAge)
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

	private bool CanOpenOnDay(DateTimeOffset tradingTime)
	{
		if (_lastTradeDate.HasValue && _lastTradeDate.Value == tradingTime.Date)
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
		return HoursToCheckTrend > 0 && _closeHistory.Count > HoursToCheckTrend;
	}

	private int DetermineDirection()
		=> DetermineDirection(_closeHistory, HoursToCheckTrend);

	internal static int DetermineDirection(IReadOnlyList<decimal> closeHistory, int hoursToCheckTrend)
	{
		if (closeHistory == null || hoursToCheckTrend <= 0 || closeHistory.Count <= hoursToCheckTrend)
		return 0;

		var latestIndex = closeHistory.Count - 1;
		var recentClose = closeHistory[latestIndex];

		var olderIndex = latestIndex - hoursToCheckTrend;
		if (olderIndex < 0 || olderIndex >= closeHistory.Count)
		return 0;

		var olderClose = closeHistory[olderIndex];

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
		=> ApplyLossMultipliers(volume, _closedTradeProfits,
		[
			FirstMultiplier,
			SecondMultiplier,
			ThirdMultiplier,
			FourthMultiplier,
			FifthMultiplier,
		]);

	internal static decimal ApplyLossMultipliers(decimal volume, IReadOnlyList<decimal> closedTradeProfits, IReadOnlyList<decimal> multipliers)
	{
		if (closedTradeProfits == null || multipliers == null || closedTradeProfits.Count == 0)
		return volume;

		var count = closedTradeProfits.Count;
		for (var i = 0; i < multipliers.Count; i++)
		{
			if (count <= i)
			break;

			var profit = closedTradeProfits[count - 1 - i];
			if (profit < 0m)
			{
				volume *= multipliers[i];
				break;
			}
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

			if (volume < 0.01m)
			volume = 0.01m;
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

	private void InitializePipSize()
	{
		var security = Security;
		if (security == null)
		{
			_pipSize = 0m;
			return;
		}

		var step = security.PriceStep ?? 0m;
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

internal sealed class TenPipsTradeEpisode : IEquatable<TenPipsTradeEpisode>
{
	public Sides? Side { get; private set; }
	public decimal Volume { get; private set; }
	public decimal? EntryPrice { get; private set; }
	public DateTimeOffset? EntryTime { get; private set; }
	public bool IsOpen => Side.HasValue && EntryPrice.HasValue && Volume > 0m;

	private decimal _realizedProfit;

	public void RegisterEntry(decimal price, decimal volume, Sides side, DateTimeOffset time)
	{
		if (price <= 0m)
			throw new ArgumentOutOfRangeException(nameof(price));
		if (volume <= 0m)
			throw new ArgumentOutOfRangeException(nameof(volume));
		if (IsOpen && Side != side)
			throw new InvalidOperationException("An opposite fill must close the active trade episode.");

		var totalVolume = Volume + volume;
		EntryPrice = IsOpen
			? ((EntryPrice.Value * Volume) + (price * volume)) / totalVolume
			: price;
		Volume = totalVolume;
		Side = side;
		EntryTime ??= time;
	}

	public decimal? RegisterExit(decimal price, decimal volume)
	{
		if (!IsOpen || price <= 0m || volume <= 0m)
			return null;

		var closedVolume = Math.Min(volume, Volume);
		_realizedProfit += Side == Sides.Buy
			? (price - EntryPrice.Value) * closedVolume
			: (EntryPrice.Value - price) * closedVolume;
		Volume -= closedVolume;

		if (Volume > 0m)
			return null;

		var closedProfit = _realizedProfit;
		Reset();
		return closedProfit;
	}

	public void Reset()
	{
		Side = null;
		Volume = 0m;
		EntryPrice = null;
		EntryTime = null;
		_realizedProfit = 0m;
	}

	public bool Equals(TenPipsTradeEpisode other)
		=> other != null
		&& Side == other.Side
		&& Volume == other.Volume
		&& EntryPrice == other.EntryPrice
		&& EntryTime == other.EntryTime
		&& _realizedProfit == other._realizedProfit;

	public override bool Equals(object obj)
		=> Equals(obj as TenPipsTradeEpisode);

	public override int GetHashCode()
		=> HashCode.Combine(Side, Volume, EntryPrice, EntryTime, _realizedProfit);
}
