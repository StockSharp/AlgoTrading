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
/// FX Fish based grid strategy inspired by the DLMv expert advisor.
/// </summary>
public class DlmvFxFishGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _distancePips;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<int> _timeLiveSeconds;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _setLimitOrders;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<int> _calculatePeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<AppliedPriceMode> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private SimpleMovingAverage _fishAverage = null!;

	private decimal _previousValue;
	private decimal _previousFish;
	private decimal _previousFishAverage;
	private decimal _pipSize;
	private decimal _lastEntryPrice;
	private DateTimeOffset? _lastEntryTime;
	private Order _buyLimitOrder;
	private Order _sellLimitOrder;

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop size in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous trades in one direction. Zero disables the cap.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Minimum distance between consecutive entries in pips.
	/// </summary>
	public int DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Allow trading on Fridays.
	/// </summary>
	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	/// <summary>
	/// Maximum lifetime for open exposure in seconds. Zero disables the timer.
	/// </summary>
	public int TimeLiveSeconds
	{
		get => _timeLiveSeconds.Value;
		set => _timeLiveSeconds.Value = value;
	}

	/// <summary>
	/// Reverse generated signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Enable automatic limit orders at a distance.
	/// </summary>
	public bool SetLimitOrders
	{
		get => _setLimitOrders.Value;
		set => _setLimitOrders.Value = value;
	}

	/// <summary>
	/// Close opposite positions before opening new trades.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Lookback length for high/low range.
	/// </summary>
	public int CalculatePeriod
	{
		get => _calculatePeriod.Value;
		set => _calculatePeriod.Value = value;
	}

	/// <summary>
	/// Moving average period applied to the Fisher output.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Price source used in the Fisher transform.
	/// </summary>
	public AppliedPriceMode AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DlmvFxFishGridStrategy"/>.
	/// </summary>
	public DlmvFxFishGridStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Volume", "Order volume in lots", "Trading")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 1)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step", "Trailing step distance in pips", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 5)
		.SetDisplay("Max Trades", "Maximum simultaneous trades", "Risk");

		_distancePips = Param(nameof(DistancePips), 15)
		.SetDisplay("Distance", "Distance between entries in pips", "Entries");

		_tradeOnFriday = Param(nameof(TradeOnFriday), true)
		.SetDisplay("Trade On Friday", "Allow trading on Fridays", "Risk");

		_timeLiveSeconds = Param(nameof(TimeLiveSeconds), 0)
		.SetDisplay("Time Live", "Maximum lifetime for positions in seconds", "Risk");

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse", "Reverse trade direction", "Entries");

		_setLimitOrders = Param(nameof(SetLimitOrders), true)
		.SetDisplay("Use Limit Orders", "Place additional limit orders", "Entries");

		_closeOpposite = Param(nameof(CloseOpposite), true)
		.SetDisplay("Close Opposite", "Close opposite exposure on new signals", "Risk");

		_calculatePeriod = Param(nameof(CalculatePeriod), 10)
		.SetDisplay("Fisher Range", "Lookback period for highs and lows", "Indicator")
		.SetGreaterThanZero();

		_maPeriod = Param(nameof(MaPeriod), 9)
		.SetDisplay("Fisher MA", "Moving average period for Fisher", "Indicator")
		.SetGreaterThanZero();

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceMode.Median)
		.SetDisplay("Applied Price", "Price source for Fisher", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for calculations", "General");
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

		_previousValue = 0m;
		_previousFish = 0m;
		_previousFishAverage = 0m;
		_lastEntryPrice = 0m;
		_lastEntryTime = null;
		_buyLimitOrder = null;
		_sellLimitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_pipSize = (Security?.PriceStep ?? 1m) * 10m;

		StartProtection(
		takeProfit: TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
		stopLoss: StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
		trailingStop: TrailingStopPips > 0 ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null,
		trailingStep: TrailingStepPips > 0 ? new Unit(TrailingStepPips * _pipSize, UnitTypes.Absolute) : null,
		useMarketOrders: true);

		_highest = new Highest
		{
			Length = CalculatePeriod,
			CandlePrice = CandlePrice.High
		};

		_lowest = new Lowest
		{
			Length = CalculatePeriod,
			CandlePrice = CandlePrice.Low
		};

		_fishAverage = new SimpleMovingAverage
		{
			Length = MaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			var oscillatorArea = CreateChartArea();
			DrawIndicator(oscillatorArea, _fishAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_previousValue = 0m;
			_previousFish = 0m;
			_previousFishAverage = 0m;
			return;
		}

		var range = highest - lowest;
		var price = GetAppliedPrice(candle);

		var normalized = range != 0m ? (price - lowest) / range : 0.5m;
		var value = range != 0m
		? 0.66m * (normalized - 0.5m) + 0.67m * _previousValue
		: 0.67m * _previousValue;
		value = Math.Min(Math.Max(value, -0.999m), 0.999m);

		var ratio = (double)((1m + value) / (1m - value));
		var fish = 0.5m * (decimal)Math.Log(ratio) + 0.5m * _previousFish;

		var avgValue = _fishAverage.Process(new DecimalIndicatorValue(_fishAverage, fish, candle.CloseTime));
		if (!avgValue.IsFinal || avgValue is not DecimalIndicatorValue fishAverageValue)
		{
			_previousValue = value;
			_previousFish = fish;
			return;
		}

		var fishAverage = fishAverageValue.Value;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousValue = value;
			_previousFish = fish;
			_previousFishAverage = fishAverage;
			return;
		}

		if (!TradeOnFriday && candle.CloseTime.DayOfWeek == DayOfWeek.Friday)
		{
			CancelAllLimits();

			if (Position != 0m)
			ClosePosition();

			_previousValue = value;
			_previousFish = fish;
			_previousFishAverage = fishAverage;
			return;
		}

		if (TimeLiveSeconds > 0 && _lastEntryTime != null)
		{
			var lifetime = candle.CloseTime - _lastEntryTime.Value;
			if (lifetime.TotalSeconds >= TimeLiveSeconds)
			{
				CancelAllLimits();

				if (Position != 0m)
				ClosePosition();

				_lastEntryTime = null;
				_lastEntryPrice = 0m;

				_previousValue = value;
				_previousFish = fish;
				_previousFishAverage = fishAverage;
				return;
			}
		}

		if (!SetLimitOrders)
		CancelAllLimits();

		if (_buyLimitOrder != null && _buyLimitOrder.State != OrderStates.Active)
		_buyLimitOrder = null;

		if (_sellLimitOrder != null && _sellLimitOrder.State != OrderStates.Active)
		_sellLimitOrder = null;

		var distance = DistancePips > 0 ? DistancePips * _pipSize : 0m;

		var signal = 0;
		if (fish < 0m && _previousFish < 0m && fish >= fishAverage && _previousFish <= _previousFishAverage)
		signal = 2;
		else if (fish > 0m && _previousFish > 0m && fish <= fishAverage && _previousFish >= _previousFishAverage)
		signal = 1;

		if (ReverseSignals)
		{
			if (signal == 1)
			signal = 2;
			else if (signal == 2)
			signal = 1;
		}

		if (signal == 1)
		HandleSellSignal(candle, distance);
		else if (signal == 2)
		HandleBuySignal(candle, distance);

		_previousValue = value;
		_previousFish = fish;
		_previousFishAverage = fishAverage;
	}

	private void HandleBuySignal(ICandleMessage candle, decimal distance)
	{
		CancelLimitOrder(ref _sellLimitOrder);

		if (Position < 0m)
		{
			if (!CloseOpposite)
			return;

			var volumeToBuy = Math.Abs(Position) + OrderVolume;
			BuyMarket(volumeToBuy);
			_lastEntryPrice = candle.ClosePrice;
			_lastEntryTime = candle.CloseTime;
			return;
		}

		var maxTrades = MaxTrades;
		var openCount = GetOpenCount();

		if (maxTrades > 0 && openCount >= maxTrades && Position >= 0m)
		return;

		var needDistance = distance > 0m && _lastEntryPrice != 0m;
		var distanceSatisfied = !needDistance || Math.Abs(candle.ClosePrice - _lastEntryPrice) >= distance;

		if (Position <= 0m)
		{
			if (!needDistance || distanceSatisfied || _lastEntryPrice == 0m)
			{
				BuyMarket(OrderVolume);
				_lastEntryPrice = candle.ClosePrice;
				_lastEntryTime = candle.CloseTime;
			}
		}
		else if (distanceSatisfied)
		{
			BuyMarket(OrderVolume);
			_lastEntryPrice = candle.ClosePrice;
			_lastEntryTime = candle.CloseTime;
		}

		if (SetLimitOrders && distance > 0m && (maxTrades == 0 || openCount < maxTrades))
		{
			if (_buyLimitOrder == null)
			{
				var price = candle.ClosePrice - distance;
				if (price > 0m)
				_buyLimitOrder = BuyLimit(OrderVolume, price);
			}
		}
	}

	private void HandleSellSignal(ICandleMessage candle, decimal distance)
	{
		CancelLimitOrder(ref _buyLimitOrder);

		if (Position > 0m)
		{
			if (!CloseOpposite)
			return;

			var volumeToSell = Math.Abs(Position) + OrderVolume;
			SellMarket(volumeToSell);
			_lastEntryPrice = candle.ClosePrice;
			_lastEntryTime = candle.CloseTime;
			return;
		}

		var maxTrades = MaxTrades;
		var openCount = GetOpenCount();

		if (maxTrades > 0 && openCount >= maxTrades && Position <= 0m)
		return;

		var needDistance = distance > 0m && _lastEntryPrice != 0m;
		var distanceSatisfied = !needDistance || Math.Abs(candle.ClosePrice - _lastEntryPrice) >= distance;

		if (Position >= 0m)
		{
			if (!needDistance || distanceSatisfied || _lastEntryPrice == 0m)
			{
				SellMarket(OrderVolume);
				_lastEntryPrice = candle.ClosePrice;
				_lastEntryTime = candle.CloseTime;
			}
		}
		else if (distanceSatisfied)
		{
			SellMarket(OrderVolume);
			_lastEntryPrice = candle.ClosePrice;
			_lastEntryTime = candle.CloseTime;
		}

		if (SetLimitOrders && distance > 0m && (maxTrades == 0 || openCount < maxTrades))
		{
			if (_sellLimitOrder == null)
			{
				var price = candle.ClosePrice + distance;
				if (price > 0m)
				_sellLimitOrder = SellLimit(OrderVolume, price);
			}
		}
	}

	private void CancelLimitOrder(ref Order order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private void CancelAllLimits()
	{
		CancelLimitOrder(ref _buyLimitOrder);
		CancelLimitOrder(ref _sellLimitOrder);
	}

	private int GetOpenCount()
	{
		if (OrderVolume <= 0m)
		return 0;

		return (int)Math.Round(Math.Abs(Position) / OrderVolume, MidpointRounding.AwayFromZero);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_lastEntryPrice = 0m;
			_lastEntryTime = null;
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceMode.Close => candle.ClosePrice,
			AppliedPriceMode.Open => candle.OpenPrice,
			AppliedPriceMode.High => candle.HighPrice,
			AppliedPriceMode.Low => candle.LowPrice,
			AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.OpenPrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	/// <summary>
	/// Price source options for the Fisher transform.
	/// </summary>
	public enum AppliedPriceMode
	{
		/// <summary>
		/// Close price.
		/// </summary>
		Close,

		/// <summary>
		/// Open price.
		/// </summary>
		Open,

		/// <summary>
		/// High price.
		/// </summary>
		High,

		/// <summary>
		/// Low price.
		/// </summary>
		Low,

		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (high + low + close) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted price (high + low + close + open) / 4.
		/// </summary>
		Weighted
	}
}