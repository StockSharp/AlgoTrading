namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class FluctuateStrategy : Strategy
{
	private enum LotMode
	{
		FixedVolume,
		RiskPercent
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<bool> _multiplyLotCoefficient;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _maxTotalVolume;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _minEquityPercent;
	private readonly StrategyParam<bool> _closeAllAtStart;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<LotMode> _lotMode;
	private readonly StrategyParam<decimal> _volumeOrRisk;

	private readonly Queue<decimal> _closeHistory = new();

	private decimal _pipSize;

	private decimal _longVolume;
	private decimal _longAveragePrice;
	private decimal _longMaxPrice;
	private decimal? _longStopLossPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _longTrailingStop;

	private decimal _shortVolume;
	private decimal _shortAveragePrice;
	private decimal _shortMinPrice;
	private decimal? _shortStopLossPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _shortTrailingStop;

	private decimal _lastOpenedVolume;
	private decimal _lastOpenedPrice;

	private bool _scheduleBuyStop;
	private bool _scheduleSellStop;

	private Order? _pendingEntryOrder;
	private Sides? _pendingEntrySide;
	private decimal _pendingEntryVolume;

	public FluctuateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Primary timeframe used for signals.", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Additional move required before the trailing stop advances.", "Risk");

		_stepPips = Param(nameof(StepPips), 30)
		.SetNotNegative()
		.SetDisplay("Grid step (pips)", "Distance between opposite pending orders.", "Trade");

		_lotCoefficient = Param(nameof(LotCoefficient), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Lot coefficient", "Multiplier applied to the last opened volume when placing recovery orders.", "Money Management");

		_multiplyLotCoefficient = Param(nameof(MultiplyLotCoefficient), false)
		.SetDisplay("Multiply by total volume", "When enabled the new order volume is calculated from the total exposure instead of the last trade volume.", "Money Management");

		_maxPositions = Param(nameof(MaxPositions), 9)
		.SetGreaterThanZero()
		.SetDisplay("Max positions", "Maximum number of simultaneously open positions plus active pending orders.", "Risk");

		_maxTotalVolume = Param(nameof(MaxTotalVolume), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Max total volume", "Upper limit for the sum of open exposure and pending volume.", "Risk");

		_profitTarget = Param(nameof(ProfitTarget), 50m)
		.SetNotNegative()
		.SetDisplay("Profit target", "Closes every position when unrealized profit reaches this amount.", "Risk");

		_minEquityPercent = Param(nameof(MinEquityPercent), 30m)
		.SetNotNegative()
		.SetDisplay("Min equity %", "Trading pauses when equity falls below this percentage of the initial balance.", "Risk");

		_closeAllAtStart = Param(nameof(CloseAllAtStart), false)
		.SetDisplay("Close on start", "Close all positions and cancel orders when the strategy starts.", "General");

		_startHour = Param(nameof(StartHour), 10)
		.SetRange(0, 23)
		.SetDisplay("Start hour", "Hour when trading becomes active (inclusive).", "Schedule");

		_endHour = Param(nameof(EndHour), 20)
		.SetRange(0, 23)
		.SetDisplay("End hour", "Hour when trading stops accepting new signals (exclusive).", "Schedule");

		_lotMode = Param(nameof(PositionSizingMode), LotMode.FixedVolume)
		.SetDisplay("Sizing mode", "Select between fixed volume and risk-based position sizing.", "Money Management");

		_volumeOrRisk = Param(nameof(VolumeOrRisk), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume or risk %", "Fixed volume (lots) or risk percentage depending on the sizing mode.", "Money Management");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	public bool MultiplyLotCoefficient
	{
		get => _multiplyLotCoefficient.Value;
		set => _multiplyLotCoefficient.Value = value;
	}

	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	public decimal MaxTotalVolume
	{
		get => _maxTotalVolume.Value;
		set => _maxTotalVolume.Value = value;
	}

	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	public decimal MinEquityPercent
	{
		get => _minEquityPercent.Value;
		set => _minEquityPercent.Value = value;
	}

	public bool CloseAllAtStart
	{
		get => _closeAllAtStart.Value;
		set => _closeAllAtStart.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public LotMode PositionSizingMode
	{
		get => _lotMode.Value;
		set => _lotMode.Value = value;
	}

	public decimal VolumeOrRisk
	{
		get => _volumeOrRisk.Value;
		set => _volumeOrRisk.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsurePipSize();

		if (CloseAllAtStart)
			CloseAllPositionsAndOrders();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		EnsurePipSize();

		_closeHistory.Enqueue(candle.ClosePrice);
		while (_closeHistory.Count > 3)
			_closeHistory.Dequeue();

		UpdateTrailingStops(candle);
		if (CheckProtectiveExits(candle))
			return;

		var currentPrice = candle.ClosePrice;

		if (ProfitTarget > 0m)
		{
			var openProfit = CalculateOpenPnL(currentPrice);
			if (openProfit >= ProfitTarget)
			{
				CloseAllPositionsAndOrders();
				return;
			}
		}

		if (IsOutsideTradingHours(candle.OpenTime))
		{
			CancelEntryOrder();
			return;
		}

		if (MinEquityPercent > 0m && Portfolio != null)
		{
			var beginValue = Portfolio.BeginValue ?? 0m;
			var currentValue = Portfolio.CurrentValue ?? beginValue;
			if (beginValue > 0m)
			{
				var threshold = beginValue * MinEquityPercent / 100m;
				if (currentValue < threshold)
				{
					CancelEntryOrder();
					return;
				}
			}
		}

		ProcessEntryRequests(currentPrice);

		if (_longVolume > 0m || _shortVolume > 0m)
			return;

		CancelEntryOrder();
		_scheduleBuyStop = false;
		_scheduleSellStop = false;
		_lastOpenedVolume = 0m;
		_lastOpenedPrice = 0m;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_closeHistory.Count < 2)
			return;

		var closes = _closeHistory.ToArray();
		var lastClose = closes[^1];
		var prevClose = closes[^2];

		if (lastClose > prevClose)
		{
			var stopPrice = StopLossPips > 0 ? lastClose - GetPriceOffset(StopLossPips) : (decimal?)null;
			var volume = DetermineInitialVolume(Sides.Buy, lastClose, stopPrice);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (lastClose < prevClose)
		{
			var stopPrice = StopLossPips > 0 ? lastClose + GetPriceOffset(StopLossPips) : (decimal?)null;
			var volume = DetermineInitialVolume(Sides.Sell, lastClose, stopPrice);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void ProcessEntryRequests(decimal referencePrice)
	{
		if (_scheduleSellStop && _longVolume > 0m)
		{
			_scheduleSellStop = false;
			TryPlaceEntryStop(Sides.Sell, referencePrice);
		}

		if (_scheduleBuyStop && _shortVolume > 0m)
		{
			_scheduleBuyStop = false;
			TryPlaceEntryStop(Sides.Buy, referencePrice);
		}
	}

	private void TryPlaceEntryStop(Sides side, decimal referencePrice)
	{
		if (_lastOpenedPrice <= 0m)
			return;

		var step = GetPriceOffset(StepPips);
		if (step <= 0m)
			return;

		var spread = _pipSize > 0m ? _pipSize : step;
		var price = side == Sides.Buy
		? _lastOpenedPrice + step + spread
		: _lastOpenedPrice - step - spread;

		if (price <= 0m)
			return;

		if (side == Sides.Buy && price <= referencePrice)
			return;

		if (side == Sides.Sell && price >= referencePrice)
			return;

		var volume = CalculateNextVolume(side);
		if (volume <= 0m)
			return;

		if (!CanSubmitEntry(volume))
			return;

		CancelEntryOrder();

		var order = side == Sides.Buy
		? BuyStop(volume, price)
		: SellStop(volume, price);

		if (order != null)
		{
			_pendingEntryOrder = order;
			_pendingEntrySide = side;
			_pendingEntryVolume = volume;
		}
	}

	private decimal CalculateNextVolume(Sides side)
	{
		var baseVolume = _lastOpenedVolume > 0m ? _lastOpenedVolume : GetFallbackVolume(side);

		decimal volume;
		if (MultiplyLotCoefficient)
		{
			var totalExposure = _longVolume + _shortVolume;
			if (totalExposure <= 0m)
				totalExposure = baseVolume;
			volume = totalExposure * LotCoefficient;
		}
		else
		{
			volume = baseVolume * LotCoefficient;
		}

		return NormalizeVolume(volume);
	}

	private decimal GetFallbackVolume(Sides side)
	{
		var volume = DetermineInitialVolume(side, _lastOpenedPrice, null);
		if (volume > 0m)
			return volume;

		return NormalizeVolume(VolumeOrRisk);
	}

	private bool CanSubmitEntry(decimal volume)
	{
		if (volume <= 0m)
			return false;

		var positionCount = (_longVolume > 0m ? 1 : 0) + (_shortVolume > 0m ? 1 : 0);
		var pendingCount = _pendingEntryOrder != null && _pendingEntryOrder.State == OrderStates.Active ? 1 : 0;

		if (MaxPositions > 0 && positionCount + pendingCount >= MaxPositions)
			return false;

		if (MaxTotalVolume > 0m)
		{
			var pendingVolume = _pendingEntryOrder != null && _pendingEntryOrder.State == OrderStates.Active ? _pendingEntryVolume : 0m;
			var total = _longVolume + _shortVolume + pendingVolume;
			if (total + volume > MaxTotalVolume)
				return false;
		}

		return true;
	}

	private void CloseAllPositionsAndOrders()
	{
		if (_longVolume > 0m)
			SellMarket(_longVolume);

		if (_shortVolume > 0m)
			BuyMarket(_shortVolume);

		CancelEntryOrder();
	}

	private void CancelEntryOrder()
	{
		if (_pendingEntryOrder != null && _pendingEntryOrder.State == OrderStates.Active)
			CancelOrder(_pendingEntryOrder);

		_pendingEntryOrder = null;
		_pendingEntrySide = null;
		_pendingEntryVolume = 0m;
	}

	private void EnsurePipSize()
	{
		if (_pipSize > 0m)
			return;

		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
			_pipSize = step;
	}

	private decimal GetPriceOffset(int pips)
	{
		if (pips <= 0)
			return 0m;

		EnsurePipSize();
		return _pipSize > 0m ? pips * _pipSize : pips;
	}

	private decimal DetermineInitialVolume(Sides side, decimal entryPrice, decimal? stopPrice)
	{
		decimal volume = 0m;

		if (PositionSizingMode == LotMode.RiskPercent)
			volume = CalculateRiskVolume(entryPrice, stopPrice);

		if (volume <= 0m)
			volume = VolumeOrRisk;

		return NormalizeVolume(volume);
	}

	private decimal CalculateRiskVolume(decimal entryPrice, decimal? stopPrice)
	{
		if (stopPrice == null)
			return 0m;

		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var riskAmount = equity * (VolumeOrRisk / 100m);
		var stopDistance = Math.Abs(entryPrice - stopPrice.Value);
		if (stopDistance <= 0m)
			return 0m;

		return MoneyToVolume(riskAmount, stopDistance);
	}

	private decimal MoneyToVolume(decimal money, decimal stopDistance)
	{
		if (money <= 0m || stopDistance <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return money / stopDistance;

		var perUnitRisk = stopDistance / priceStep * stepPrice;
		if (perUnitRisk <= 0m)
			return 0m;

		return money / perUnitRisk;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(volume / volumeStep));
			volume = steps * volumeStep;
		}

		var minVolume = Security?.VolumeMin ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = Security?.VolumeMax ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal CalculateOpenPnL(decimal currentPrice)
	{
		var profit = 0m;

		if (_longVolume > 0m)
			profit += PriceToMoney(currentPrice - _longAveragePrice, _longVolume);

		if (_shortVolume > 0m)
			profit += PriceToMoney(_shortAveragePrice - currentPrice, _shortVolume);

		return profit;
	}

	private decimal PriceToMoney(decimal priceDiff, decimal volume)
	{
		if (priceDiff == 0m || volume <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return priceDiff * volume;

		return priceDiff / priceStep * stepPrice * volume;
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_longVolume > 0m)
		{
			_longMaxPrice = Math.Max(_longMaxPrice, candle.HighPrice);

			var trail = GetPriceOffset(TrailingStopPips);
			var step = GetPriceOffset(TrailingStepPips);

			if (trail > 0m && step > 0m)
			{
				if (_longMaxPrice - _longAveragePrice >= trail + step)
				{
					var candidate = _longMaxPrice - trail;
					if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
						_longTrailingStop = candidate;
				}

				if (_longTrailingStop.HasValue)
				{
					if (!_longStopLossPrice.HasValue || _longTrailingStop.Value > _longStopLossPrice.Value)
						_longStopLossPrice = _longTrailingStop.Value;
				}
			}
		}
		else
		{
			_longTrailingStop = null;
			_longMaxPrice = 0m;
		}

		if (_shortVolume > 0m)
		{
			_shortMinPrice = _shortMinPrice == 0m ? candle.LowPrice : Math.Min(_shortMinPrice, candle.LowPrice);

			var trail = GetPriceOffset(TrailingStopPips);
			var step = GetPriceOffset(TrailingStepPips);

			if (trail > 0m && step > 0m)
			{
				if (_shortAveragePrice - _shortMinPrice >= trail + step)
				{
					var candidate = _shortMinPrice + trail;
					if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
						_shortTrailingStop = candidate;
				}

				if (_shortTrailingStop.HasValue)
				{
					if (!_shortStopLossPrice.HasValue || _shortTrailingStop.Value < _shortStopLossPrice.Value)
						_shortStopLossPrice = _shortTrailingStop.Value;
				}
			}
		}
		else
		{
			_shortTrailingStop = null;
			_shortMinPrice = 0m;
		}
	}

	private bool CheckProtectiveExits(ICandleMessage candle)
	{
		if (_longVolume > 0m)
		{
			if (_longStopLossPrice.HasValue && candle.LowPrice <= _longStopLossPrice.Value)
			{
				SellMarket(_longVolume);
				return true;
			}

			if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				SellMarket(_longVolume);
				return true;
			}
		}

		if (_shortVolume > 0m)
		{
			if (_shortStopLossPrice.HasValue && candle.HighPrice >= _shortStopLossPrice.Value)
			{
				BuyMarket(_shortVolume);
				return true;
			}

			if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				BuyMarket(_shortVolume);
				return true;
			}
		}

		return false;
	}

	private bool IsOutsideTradingHours(DateTimeOffset time)
	{
		if (StartHour == EndHour)
			return false;

		var hour = time.Hour;

		if (StartHour < EndHour)
			return hour < StartHour || hour >= EndHour;

		return hour >= EndHour && hour < StartHour;
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order?.Security != Security)
			return;

		var order = trade.Order;
		var tradeInfo = trade.Trade;
		if (tradeInfo == null)
			return;

		var volume = tradeInfo.Volume;
		if (volume <= 0m)
			return;

		var price = tradeInfo.Price;

		if (_pendingEntryOrder != null && order == _pendingEntryOrder)
		{
			_pendingEntryOrder = null;
			_pendingEntrySide = null;
			_pendingEntryVolume = 0m;
		}

		if (order.Side == Sides.Buy)
		{
			ReduceShortExposure(ref volume);
			if (volume <= 0m)
				return;

			var newVolume = _longVolume + volume;
			_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
			_longVolume = newVolume;
			_longMaxPrice = Math.Max(_longMaxPrice, price);
			InitializeLongProtection();

			_lastOpenedVolume = volume;
			_lastOpenedPrice = price;
			_scheduleSellStop = true;
		}
		else if (order.Side == Sides.Sell)
		{
			ReduceLongExposure(ref volume);
			if (volume <= 0m)
				return;

			var newVolume = _shortVolume + volume;
			_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
			_shortVolume = newVolume;
			_shortMinPrice = _shortMinPrice == 0m ? price : Math.Min(_shortMinPrice, price);
			InitializeShortProtection();

			_lastOpenedVolume = volume;
			_lastOpenedPrice = price;
			_scheduleBuyStop = true;
		}
	}

	private void ReduceLongExposure(ref decimal volume)
	{
		if (_longVolume <= 0m || volume <= 0m)
			return;

		var closingVolume = Math.Min(_longVolume, volume);
		_longVolume -= closingVolume;
		volume -= closingVolume;

		if (_longVolume <= 0m)
			ResetLongState();
	}

	private void ReduceShortExposure(ref decimal volume)
	{
		if (_shortVolume <= 0m || volume <= 0m)
			return;

		var closingVolume = Math.Min(_shortVolume, volume);
		_shortVolume -= closingVolume;
		volume -= closingVolume;

		if (_shortVolume <= 0m)
			ResetShortState();
	}

	private void InitializeLongProtection()
	{
		var entryPrice = _longAveragePrice;
		_longMaxPrice = Math.Max(_longMaxPrice, entryPrice);
		_longTrailingStop = null;

		var stopOffset = GetPriceOffset(StopLossPips);
		_longStopLossPrice = stopOffset > 0m ? entryPrice - stopOffset : null;

		var takeOffset = GetPriceOffset(TakeProfitPips);
		_longTakeProfitPrice = takeOffset > 0m ? entryPrice + takeOffset : null;
	}

	private void InitializeShortProtection()
	{
		var entryPrice = _shortAveragePrice;
		_shortMinPrice = entryPrice;
		_shortTrailingStop = null;

		var stopOffset = GetPriceOffset(StopLossPips);
		_shortStopLossPrice = stopOffset > 0m ? entryPrice + stopOffset : null;

		var takeOffset = GetPriceOffset(TakeProfitPips);
		_shortTakeProfitPrice = takeOffset > 0m ? entryPrice - takeOffset : null;
	}

	private void ResetLongState()
	{
		_longVolume = 0m;
		_longAveragePrice = 0m;
		_longStopLossPrice = null;
		_longTakeProfitPrice = null;
		_longTrailingStop = null;
		_longMaxPrice = 0m;
	}

	private void ResetShortState()
	{
		_shortVolume = 0m;
		_shortAveragePrice = 0m;
		_shortStopLossPrice = null;
		_shortTakeProfitPrice = null;
		_shortTrailingStop = null;
		_shortMinPrice = 0m;
	}
}
