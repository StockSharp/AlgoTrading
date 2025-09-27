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

using StockSharp.Algo;

/// <summary>
/// Breakout strategy converted from the MetaTrader Cidomo expert advisor.
/// It places paired stop orders around a recent range and manages trades with trailing stops.
/// </summary>
public class CidomoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<int> _barsCount;
	private readonly StrategyParam<CidomoMoneyManagementModes> _moneyManagementMode;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _timeWindowSeconds;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private Order _buyStopOrder;
	private Order _sellStopOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="CidomoStrategy"/> class.
	/// </summary>
	public CidomoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for range detection", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Profit target distance expressed in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 35m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Distance used by the trailing stop mechanism", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimum progress before the trailing stop is advanced", "Risk Management");

		_indentPips = Param(nameof(IndentPips), 3m)
			.SetNotNegative()
			.SetDisplay("Indent (pips)", "Indent added above/below the recent range before placing stop orders", "General");

		_barsCount = Param(nameof(BarsCount), 15)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Number of completed candles used for the breakout range", "General");

		_moneyManagementMode = Param(nameof(MoneyManagement), CidomoMoneyManagementModes.RiskPercent)
			.SetDisplay("Money Management", "Volume calculation mode", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Risk Percent", "Risk percentage applied when money management uses risk-based sizing", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Fixed order volume used when money management is set to fixed size", "Trading");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Enable the time-of-day filter", "Filters");

		_startHour = Param(nameof(StartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour component of the trading window", "Filters");

		_startMinute = Param(nameof(StartMinute), 58)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Minute component of the trading window", "Filters");

		_timeWindowSeconds = Param(nameof(TimeWindowSeconds), 30)
			.SetGreaterThanZero()
			.SetDisplay("Time Window (sec)", "Tolerance around the candle close for order placement", "Filters");
	}

	/// <summary>
	/// Candle type used for the breakout calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum move required before advancing the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Indent in pips added to breakout levels.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Number of candles considered for the breakout range.
	/// </summary>
	public int BarsCount
	{
		get => _barsCount.Value;
		set => _barsCount.Value = value;
	}

	/// <summary>
	/// Money management mode.
	/// </summary>
	public CidomoMoneyManagementModes MoneyManagement
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Risk percentage used when money management is set to risk-based sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed order volume applied when money management uses a constant size.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Enables the time-of-day filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Hour component of the time filter window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Minute component of the time filter window.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Allowed time difference between candle completion and range evaluation in seconds.
	/// </summary>
	public int TimeWindowSeconds
	{
		get => _timeWindowSeconds.Value;
		set => _timeWindowSeconds.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Volume = TradeVolume;

		_buyStopOrder = null;
		_sellStopOrder = null;

		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;

		ResetPositionTracking();

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_pipSize = CalculatePipSize();

		_highest = new Highest { Length = BarsCount };
		_lowest = new Lowest { Length = BarsCount };

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		_highest.Length = BarsCount;
		_lowest.Length = BarsCount;

		var highValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.OpenTime));
		var lowValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.OpenTime));

		UpdatePositionManagement(candle);

		if (Position != 0m)
			return;

		if (HasActivePendingOrders())
			return;

		if (!highValue.IsFinal || !lowValue.IsFinal)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTimeWindow(candle.OpenTime))
			return;

		var highest = highValue.GetValue<decimal>();
		var lowest = lowValue.GetValue<decimal>();

		var indentDistance = IndentPips * _pipSize;
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		var volume = CalculateEntryVolume(stopDistance);
		if (volume <= 0m)
			return;

		var buyPrice = RoundPrice(highest + indentDistance);
		var sellPrice = RoundPrice(lowest - indentDistance);

		if (buyPrice > 0m)
		{
			_pendingLongStop = stopDistance > 0m ? RoundPrice(buyPrice - stopDistance) : null;
			_pendingLongTake = takeDistance > 0m ? RoundPrice(buyPrice + takeDistance) : null;

			_buyStopOrder = BuyStop(volume, buyPrice);
		}

		if (sellPrice > 0m)
		{
			_pendingShortStop = stopDistance > 0m ? RoundPrice(sellPrice + stopDistance) : null;
			_pendingShortTake = takeDistance > 0m ? RoundPrice(sellPrice - takeDistance) : null;

			_sellStopOrder = SellStop(volume, sellPrice);
		}
	}

	private void UpdatePositionManagement(ICandleMessage candle)
	{
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m)
		{
			if (_longEntryPrice is null)
				_longEntryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;

			if (_longStopPrice is null)
				_longStopPrice = _pendingLongStop ?? (stopDistance > 0m && _longEntryPrice.HasValue ? RoundPrice(_longEntryPrice.Value - stopDistance) : null);

			if (_longTakeProfit is null)
				_longTakeProfit = _pendingLongTake ?? (takeDistance > 0m && _longEntryPrice.HasValue ? RoundPrice(_longEntryPrice.Value + takeDistance) : null);

			_pendingLongStop = null;
			_pendingLongTake = null;

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionTracking();
				return;
			}

			if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionTracking();
				return;
			}

			if (trailingDistance > 0m && _longEntryPrice.HasValue)
			{
				var profit = candle.ClosePrice - _longEntryPrice.Value;
				var threshold = trailingDistance + trailingStep;

				if (profit >= threshold)
				{
					var candidate = RoundPrice(candle.ClosePrice - trailingDistance);
					if (!_longStopPrice.HasValue || _longStopPrice.Value < candle.ClosePrice - threshold)
						_longStopPrice = candidate;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_shortEntryPrice is null)
				_shortEntryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;

			if (_shortStopPrice is null)
				_shortStopPrice = _pendingShortStop ?? (stopDistance > 0m && _shortEntryPrice.HasValue ? RoundPrice(_shortEntryPrice.Value + stopDistance) : null);

			if (_shortTakeProfit is null)
				_shortTakeProfit = _pendingShortTake ?? (takeDistance > 0m && _shortEntryPrice.HasValue ? RoundPrice(_shortEntryPrice.Value - takeDistance) : null);

			_pendingShortStop = null;
			_pendingShortTake = null;

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionTracking();
				return;
			}

			if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionTracking();
				return;
			}

			if (trailingDistance > 0m && _shortEntryPrice.HasValue)
			{
				var profit = _shortEntryPrice.Value - candle.ClosePrice;
				var threshold = trailingDistance + trailingStep;

				if (profit >= threshold)
				{
					var candidate = RoundPrice(candle.ClosePrice + trailingDistance);
					if (!_shortStopPrice.HasValue || _shortStopPrice.Value > candle.ClosePrice + threshold)
						_shortStopPrice = candidate;
				}
			}
		}
		else
		{
			ResetPositionTracking();
		}
	}

	private bool HasActivePendingOrders()
	{
		return (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active) ||
			(_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active);
	}

	private bool IsWithinTimeWindow(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var start = new TimeSpan(StartHour, StartMinute, 0);
		var current = time.TimeOfDay;
		var diff = Math.Abs((current - start).TotalSeconds);

		if (diff <= TimeWindowSeconds)
			return true;

		var daySeconds = TimeSpan.FromDays(1).TotalSeconds;
		var altDiff = daySeconds - diff;
		return altDiff <= TimeWindowSeconds;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private decimal CalculateEntryVolume(decimal stopDistance)
	{
		if (MoneyManagement == CidomoMoneyManagementModes.RiskPercent)
		{
			if (stopDistance <= 0m)
				return TradeVolume;

			var portfolioValue = Portfolio?.CurrentValue ?? 0m;
			if (portfolioValue <= 0m)
				return TradeVolume;

			var riskCapital = portfolioValue * RiskPercent / 100m;
			if (riskCapital <= 0m)
				return TradeVolume;

			var rawVolume = riskCapital / stopDistance;
			var step = Security?.VolumeStep ?? 1m;
			if (step <= 0m)
				step = 1m;

			var minVolume = Security?.MinVolume ?? step;
			if (minVolume <= 0m)
				minVolume = step;

			var maxVolume = Security?.MaxVolume ?? decimal.MaxValue;

			var rounded = Math.Floor(rawVolume / step) * step;
			if (rounded <= 0m)
				rounded = step;

			if (rounded < minVolume)
				rounded = minVolume;

			if (rounded > maxVolume)
				rounded = maxVolume;

			return rounded;
		}

		return TradeVolume;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		var rounded = Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
		return rounded;
	}

	private void ResetPositionTracking()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order == null)
			return;

		var tradePrice = trade.Trade?.Price ?? order.Price;

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			_longEntryPrice = tradePrice ?? _longEntryPrice;
			_longStopPrice = _pendingLongStop;
			_longTakeProfit = _pendingLongTake;
			_pendingLongStop = null;
			_pendingLongTake = null;

			if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
				CancelOrder(_sellStopOrder);

			_buyStopOrder = null;
		}
		else if (_sellStopOrder != null && order == _sellStopOrder)
		{
			_shortEntryPrice = tradePrice ?? _shortEntryPrice;
			_shortStopPrice = _pendingShortStop;
			_shortTakeProfit = _pendingShortTake;
			_pendingShortStop = null;
			_pendingShortTake = null;

			if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
				CancelOrder(_buyStopOrder);

			_sellStopOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			if (order.State == OrderStates.Canceled || order.State == OrderStates.Failed)
			{
				_buyStopOrder = null;
				_pendingLongStop = null;
				_pendingLongTake = null;
			}
		}

		if (_sellStopOrder != null && order == _sellStopOrder)
		{
			if (order.State == OrderStates.Canceled || order.State == OrderStates.Failed)
			{
				_sellStopOrder = null;
				_pendingShortStop = null;
				_pendingShortTake = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			ResetPositionTracking();
	}

	public enum CidomoMoneyManagementModes
	{
		/// <summary>
		/// Always trade the fixed volume specified by <see cref="CidomoStrategy.TradeVolume"/>.
		/// </summary>
		FixedVolume,

		/// <summary>
		/// Scale the order size so that the configured risk percentage is lost when the stop-loss is hit.
		/// </summary>
		RiskPercent
	}
}
