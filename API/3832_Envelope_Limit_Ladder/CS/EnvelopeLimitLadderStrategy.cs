using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope based ladder strategy converted from MetaTrader advisor "E_2_12_5min.mq4".
/// Places up to three buy and three sell limit orders around the moving average envelope.
/// Pending orders are cancelled at the session end and filled positions are managed with
/// individual stop-loss and take-profit orders including optional MA based trailing.
/// </summary>
public class EnvelopeLimitLadderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _envelopeCandleType;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<EnvelopeMaMethod> _maMethod;
	private readonly StrategyParam<decimal> _envelopeDeviation;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<decimal> _firstTakeProfitPoints;
	private readonly StrategyParam<decimal> _secondTakeProfitPoints;
	private readonly StrategyParam<decimal> _thirdTakeProfitPoints;
	private readonly StrategyParam<bool> _useOppositeEnvelopeTrailing;
	private readonly StrategyParam<decimal> _orderVolume;

	private readonly EntrySlot[] _longSlots;
	private readonly EntrySlot[] _shortSlots;

	private IIndicator _envelopeMa = default!;
	private decimal? _basis;
	private decimal? _upperBand;
	private decimal? _lowerBand;
	private decimal? _previousClose;

	/// <summary>
	/// Trading candle type used for signal detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used for calculating the envelope.
	/// </summary>
	public DataType EnvelopeCandleType
	{
		get => _envelopeCandleType.Value;
		set => _envelopeCandleType.Value = value;
	}

	/// <summary>
	/// Moving average period for the envelope.
	/// </summary>
	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	/// <summary>
	/// Moving average method matching the MetaTrader <c>MODE_*</c> options.
	/// </summary>
	public EnvelopeMaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Envelope deviation expressed in percent (0.05 = 0.05%).
	/// </summary>
	public decimal EnvelopeDeviation
	{
		get => _envelopeDeviation.Value;
		set => _envelopeDeviation.Value = value;
	}

	/// <summary>
	/// Inclusive start hour when pending orders may be created.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Exclusive end hour when pending orders are cancelled.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// First take-profit offset in MetaTrader points.
	/// </summary>
	public decimal FirstTakeProfitPoints
	{
		get => _firstTakeProfitPoints.Value;
		set => _firstTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Second take-profit offset in MetaTrader points.
	/// </summary>
	public decimal SecondTakeProfitPoints
	{
		get => _secondTakeProfitPoints.Value;
		set => _secondTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Third take-profit offset in MetaTrader points.
	/// </summary>
	public decimal ThirdTakeProfitPoints
	{
		get => _thirdTakeProfitPoints.Value;
		set => _thirdTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Use the opposite envelope band for trailing instead of the moving average.
	/// </summary>
	public bool UseOppositeEnvelopeTrailing
	{
		get => _useOppositeEnvelopeTrailing.Value;
		set => _useOppositeEnvelopeTrailing.Value = value;
	}

	/// <summary>
	/// Volume of each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EnvelopeLimitLadderStrategy"/>.
	/// </summary>
	public EnvelopeLimitLadderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Signal candles", "Trading timeframe for signal detection.", "General");

		_envelopeCandleType = Param(nameof(EnvelopeCandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Envelope candles", "Timeframe used to calculate the envelope.", "General");

		_envelopePeriod = Param(nameof(EnvelopePeriod), 144)
		.SetGreaterThanZero()
		.SetDisplay("Envelope period", "Moving average period of the envelope.", "Indicator");

		_maMethod = Param(nameof(MaMethod), EnvelopeMaMethod.Ema)
		.SetDisplay("MA method", "Moving average method for the envelope.", "Indicator");

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.05m)
		.SetGreaterThanZero()
		.SetDisplay("Envelope deviation %", "Envelope deviation in percent (0.05 = 0.05%).", "Indicator");

		_tradingStartHour = Param(nameof(TradingStartHour), 0)
		.SetDisplay("Start hour", "Hour when pending orders may start appearing.", "Trading window");

		_tradingEndHour = Param(nameof(TradingEndHour), 17)
		.SetDisplay("End hour", "Hour when pending orders are cancelled.", "Trading window");

		_firstTakeProfitPoints = Param(nameof(FirstTakeProfitPoints), 8m)
		.SetGreaterThanZero()
		.SetDisplay("TP1 points", "First take-profit distance in points.", "Targets");

		_secondTakeProfitPoints = Param(nameof(SecondTakeProfitPoints), 13m)
		.SetGreaterThanZero()
		.SetDisplay("TP2 points", "Second take-profit distance in points.", "Targets");

		_thirdTakeProfitPoints = Param(nameof(ThirdTakeProfitPoints), 21m)
		.SetGreaterThanZero()
		.SetDisplay("TP3 points", "Third take-profit distance in points.", "Targets");

		_useOppositeEnvelopeTrailing = Param(nameof(UseOppositeEnvelopeTrailing), true)
		.SetDisplay("Use envelope trailing", "Use opposite envelope for trailing stops (false = moving average).", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order volume", "Volume for each pending order.", "General");

		_longSlots =
		[
		new EntrySlot(true),
		new EntrySlot(true),
		new EntrySlot(true)
	];

		_shortSlots =
		[
		new EntrySlot(false),
		new EntrySlot(false),
		new EntrySlot(false)
	];
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, EnvelopeCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_basis = null;
		_upperBand = null;
		_lowerBand = null;
		_previousClose = null;

		foreach (var slot in _longSlots)
		slot.Reset();

		foreach (var slot in _shortSlots)
		slot.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_envelopeMa = CreateMovingAverage();

		var envelopeSubscription = SubscribeCandles(EnvelopeCandleType);
		envelopeSubscription
		.Bind(_envelopeMa, ProcessEnvelopeCandle)
		.Start();

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
		.Bind(ProcessTradingCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _envelopeMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessEnvelopeCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var deviationFactor = EnvelopeDeviation / 100m;

		var alignedMa = AlignPrice(maValue);
		var upper = AlignPrice(alignedMa * (1 + deviationFactor));
		var lower = AlignPrice(alignedMa * (1 - deviationFactor));

		// Cache the envelope values so that the trading subscription can reuse them.
		_basis = alignedMa;
		_upperBand = upper;
		_lowerBand = lower;
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_basis is not decimal ma || _upperBand is not decimal upper || _lowerBand is not decimal lower)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		// Always keep protective stops in sync with the latest envelope values.
		ApplyTrailingAdjustments(candle.ClosePrice, ma, upper, lower);

		// Remove stale pending orders once the configured session end is reached.
		CancelExpiredPendingOrders(candle.OpenTime.Hour);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var previousClose = _previousClose;
		_previousClose = candle.ClosePrice;

		if (previousClose is null)
		return;

		var hour = candle.OpenTime.Hour;
		if (!IsWithinTradingWindow(hour))
		return;

		var volume = AlignVolume(OrderVolume);
		if (volume <= 0m)
		return;

		// Place pending orders only when the previous candle closed between MA and the closest band.
		if (previousClose.Value > ma && previousClose.Value < upper)
		EnsurePendingSlots(_longSlots, true, volume, ma, upper, lower);

		if (previousClose.Value < ma && previousClose.Value > lower)
		EnsurePendingSlots(_shortSlots, false, volume, ma, upper, lower);
	}

	private void EnsurePendingSlots(EntrySlot[] slots, bool isLong, decimal volume, decimal ma, decimal upper, decimal lower)
	{
		var entryPrice = AlignPrice(ma);
		var stopPrice = AlignPrice(isLong ? lower : upper);

		var tpOffsets = new[]
		{
			FirstTakeProfitPoints,
			SecondTakeProfitPoints,
			ThirdTakeProfitPoints
		};

		for (var i = 0; i < slots.Length; i++)
		{
			var slot = slots[i];

			if (!slot.CanPlaceNewEntry())
			continue;

			var tpPrice = isLong
			? AlignPrice(upper + PointsToPrice(tpOffsets[i]))
			: AlignPrice(lower - PointsToPrice(tpOffsets[i]));

			// Register a limit order at the midline with individual take-profit distance.
			var order = isLong
			? BuyLimit(volume, entryPrice)
			: SellLimit(volume, entryPrice);

			if (order is null)
			continue;

			slot.AssignEntry(order, entryPrice, stopPrice, tpPrice, volume);
		}
	}

	private void ApplyTrailingAdjustments(decimal closePrice, decimal ma, decimal upper, decimal lower)
	{
		foreach (var slot in _longSlots)
		{
			if (!slot.HasOpenPosition)
			continue;

			var candidateStop = UseOppositeEnvelopeTrailing ? lower : ma;

			if (closePrice <= slot.EntryPrice)
			continue;

			if (closePrice <= upper)
			continue;

			// Tighten the long stop only after price breaks above the upper band.
			slot.TryMoveStop(candidateStop, this);
		}

		foreach (var slot in _shortSlots)
		{
			if (!slot.HasOpenPosition)
			continue;

			var candidateStop = UseOppositeEnvelopeTrailing ? upper : ma;

			if (closePrice >= slot.EntryPrice)
			continue;

			if (closePrice >= lower)
			continue;

			// Tighten the short stop only after price breaks below the lower band.
			slot.TryMoveStop(candidateStop, this);
		}
	}

	private void CancelExpiredPendingOrders(int hour)
	{
		if (hour < TradingEndHour)
		return;

		foreach (var slot in _longSlots)
		slot.CancelEntryIfActive(this);

		foreach (var slot in _shortSlots)
		slot.CancelEntryIfActive(this);
	}

	private bool IsWithinTradingWindow(int hour)
	{
		return hour > TradingStartHour && hour < TradingEndHour;
	}

	private IIndicator CreateMovingAverage()
	{
		return MaMethod switch
		{
			EnvelopeMaMethod.Sma => new SimpleMovingAverage { Length = EnvelopePeriod },
			EnvelopeMaMethod.Ema => new ExponentialMovingAverage { Length = EnvelopePeriod },
			EnvelopeMaMethod.Smma => new SmoothedMovingAverage { Length = EnvelopePeriod },
			EnvelopeMaMethod.Lwma => new WeightedMovingAverage { Length = EnvelopePeriod },
			_ => new SimpleMovingAverage { Length = EnvelopePeriod }
		};
	}

	private decimal PointsToPrice(decimal points)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return points;

		return points * step;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security is null)
		return volume;

		var minVolume = security.MinVolume ?? 0m;
		var step = security.VolumeStep ?? 0m;

		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume.Value;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		foreach (var slot in _longSlots)
		{
			if (slot.HandleTrade(trade, this))
			return;
		}

		foreach (var slot in _shortSlots)
		{
			if (slot.HandleTrade(trade, this))
			return;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		foreach (var slot in _longSlots)
		{
			if (slot.HandleOrderChanged(order, this))
			return;
		}

		foreach (var slot in _shortSlots)
		{
			if (slot.HandleOrderChanged(order, this))
			return;
		}
	}

	private static bool IsOrderActive(Order order)
	{
		return order is not null && (order.State == OrderStates.Active || order.State == OrderStates.Pending);
	}

	private static bool IsFinalState(OrderStates state)
	{
		return state == OrderStates.Done || state == OrderStates.Failed || state == OrderStates.Canceled || state == OrderStates.Stopped;
	}

	private sealed class EntrySlot
	{
		private readonly bool _isLong;

		public EntrySlot(bool isLong)
		{
			_isLong = isLong;
		}

		public Order EntryOrder { get; private set; }
		public Order StopOrder { get; private set; }
		public Order TakeOrder { get; private set; }
		public decimal EntryPrice { get; private set; }
		public decimal StopPrice { get; private set; }
		public decimal TakePrice { get; private set; }
		public decimal Volume { get; private set; }
		public decimal FilledVolume { get; private set; }
		public bool HasOpenPosition => FilledVolume > 0m;

		public bool CanPlaceNewEntry()
		{
			return !HasOpenPosition && !IsOrderActive(EntryOrder);
		}

		public void AssignEntry(Order order, decimal entryPrice, decimal stopPrice, decimal takePrice, decimal volume)
		{
			EntryOrder = order;
			EntryPrice = entryPrice;
			StopPrice = stopPrice;
			TakePrice = takePrice;
			Volume = volume;
			FilledVolume = 0m;
		}

		public void CancelEntryIfActive(Strategy strategy)
		{
			if (EntryOrder is not null && IsOrderActive(EntryOrder))
			{
				strategy.CancelOrder(EntryOrder);
				EntryOrder = null;
			}
		}

		public bool HandleTrade(MyTrade trade, Strategy strategy)
		{
			if (EntryOrder == trade.Order)
			{
				FilledVolume += trade.Trade.Volume;

				if (trade.Order.Balance == 0m)
				RegisterProtection(strategy);

				return true;
			}

			if (StopOrder == trade.Order)
			{
				FilledVolume -= trade.Trade.Volume;
				if (trade.Order.Balance == 0m)
				CompleteExit(strategy, cancelRemaining: true);

				return true;
			}

			if (TakeOrder == trade.Order)
			{
				FilledVolume -= trade.Trade.Volume;
				if (trade.Order.Balance == 0m)
				CompleteExit(strategy, cancelRemaining: true);

				return true;
			}

			return false;
		}

		public bool HandleOrderChanged(Order order, Strategy strategy)
		{
			if (EntryOrder == order)
			{
				if (IsFinalState(order.State))
				{
					if (order.Balance > 0m)
					{
						EntryOrder = null;
						FilledVolume = 0m;
					}
					else
					{
						EntryOrder = null;
					}
				}

				return true;
			}

			if (StopOrder == order)
			{
				if (IsFinalState(order.State) && order.State != OrderStates.Active)
				{
					if (order.State == OrderStates.Done)
					CompleteExit(strategy, cancelRemaining: true);
					else
					StopOrder = null;
				}

				return true;
			}

			if (TakeOrder == order)
			{
				if (IsFinalState(order.State) && order.State != OrderStates.Active)
				{
					if (order.State == OrderStates.Done)
					CompleteExit(strategy, cancelRemaining: true);
					else
					TakeOrder = null;
				}

				return true;
			}

			return false;
		}

		public void TryMoveStop(decimal candidateStop, Strategy strategy)
		{
			if (!HasOpenPosition || StopOrder is null)
			return;

			if (_isLong)
			{
				if (candidateStop <= StopPrice)
				return;
			}
			else
			{
				if (candidateStop >= StopPrice)
				return;
			}

			StopPrice = candidateStop;

			if (IsOrderActive(StopOrder))
			strategy.CancelOrder(StopOrder);

			StopOrder = _isLong
			? strategy.SellStop(FilledVolume, candidateStop)
			: strategy.BuyStop(FilledVolume, candidateStop);
		}

		private void RegisterProtection(Strategy strategy)
		{
			if (FilledVolume <= 0m)
			return;

			if (StopPrice > 0m)
			{
				StopOrder = _isLong
				? strategy.SellStop(FilledVolume, StopPrice)
				: strategy.BuyStop(FilledVolume, StopPrice);
			}

			if (TakePrice > 0m)
			{
				TakeOrder = _isLong
				? strategy.SellLimit(FilledVolume, TakePrice)
				: strategy.BuyLimit(FilledVolume, TakePrice);
			}
		}

		private void CompleteExit(Strategy strategy, bool cancelRemaining)
		{
			if (cancelRemaining)
			{
				if (StopOrder is not null && IsOrderActive(StopOrder))
				strategy.CancelOrder(StopOrder);

				if (TakeOrder is not null && IsOrderActive(TakeOrder))
				strategy.CancelOrder(TakeOrder);
			}

			Reset();
		}

		public void Reset()
		{
			EntryOrder = null;
			StopOrder = null;
			TakeOrder = null;
			FilledVolume = 0m;
			Volume = 0m;
			EntryPrice = 0m;
			StopPrice = 0m;
			TakePrice = 0m;
		}
	}

	private enum EnvelopeMaMethod
	{
		Sma = 0,
		Ema = 1,
		Smma = 2,
		Lwma = 3
	}
}
