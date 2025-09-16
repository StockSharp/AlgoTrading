using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the NTK 07 MetaTrader strategy that trades stop orders around a recent range.
/// </summary>
public class Ntk07RangeTraderStrategy : Strategy
{
	public enum TradeModeOption
	{
		EdgesOfRange,
		CenterOfRange,
	}

	private readonly StrategyParam<decimal> _entryVolume;
	private readonly StrategyParam<decimal> _totalVolumeLimit;
	private readonly StrategyParam<decimal> _netStepPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<bool> _trailHighLow;
	private readonly StrategyParam<bool> _trailMa;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<int> _rangeBars;
	private readonly StrategyParam<TradeModeOption> _tradeMode;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _movingAverage;
	private Highest _rangeHighIndicator;
	private Lowest _rangeLowIndicator;

	private Order _entryBuyStop;
	private Order _entrySellStop;
	private Order _longStopOrder;
	private Order _longTakeProfitOrder;
	private Order _shortStopOrder;
	private Order _shortTakeProfitOrder;

	private ICandleMessage _previousCandle;
	private decimal _priceStep;

	/// <summary>
	/// Base volume used for each entry order.
	/// </summary>
	public decimal EntryVolume
	{
		get => _entryVolume.Value;
		set => _entryVolume.Value = value;
	}

	/// <summary>
	/// Maximum total exposure allowed for the strategy. Set to zero for unlimited exposure.
	/// </summary>
	public decimal TotalVolumeLimit
	{
		get => _totalVolumeLimit.Value;
		set => _totalVolumeLimit.Value = value;
	}

	/// <summary>
	/// Distance of stop orders from the market in price steps.
	/// </summary>
	public decimal NetStepPoints
	{
		get => _netStepPoints.Value;
		set => _netStepPoints.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Additional volume multiplier used when pyramiding into an existing position.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Enables trailing based on previous candle extremes.
	/// </summary>
	public bool UseTrailingAtHighLow
	{
		get => _trailHighLow.Value;
		set => _trailHighLow.Value = value;
	}

	/// <summary>
	/// Enables trailing based on a moving average.
	/// </summary>
	public bool UseTrailingMa
	{
		get => _trailMa.Value;
		set => _trailMa.Value = value;
	}

	/// <summary>
	/// Inclusive starting hour for trading (platform time).
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Inclusive ending hour for trading (platform time).
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Number of completed candles that define the reference range. Set to zero to disable range filtering.
	/// </summary>
	public int RangeBars
	{
		get => _rangeBars.Value;
		set => _rangeBars.Value = value;
	}

	/// <summary>
	/// Range interaction mode for entry logic.
	/// </summary>
	public TradeModeOption TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Length of the moving average used for trailing stops.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public Ntk07RangeTraderStrategy()
	{
		_entryVolume = Param(nameof(EntryVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Entry Volume", "Base volume for each entry order", "Risk");

		_totalVolumeLimit = Param(nameof(TotalVolumeLimit), 7m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Total Volume Limit", "Maximum aggregated volume (0 disables the limit)", "Risk");

		_netStepPoints = Param(nameof(NetStepPoints), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Net Step", "Offset for stop entries measured in price steps", "Entries");

		_stopLossPoints = Param(nameof(StopLossPoints), 11m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Initial stop distance measured in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Take-profit distance measured in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 8m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop", "Distance used for trailing calculations in price steps", "Risk");

		_lotMultiplier = Param(nameof(LotMultiplier), 1.7m)
		.SetGreaterOrEqual(1m)
		.SetDisplay("Lot Multiplier", "Volume multiplier when pyramiding", "Risk");

		_trailHighLow = Param(nameof(UseTrailingAtHighLow), true)
		.SetDisplay("Trail High/Low", "Use previous candle extremes for trailing", "Risk");

		_trailMa = Param(nameof(UseTrailingMa), false)
		.SetDisplay("Trail Moving Average", "Use moving average value for trailing", "Risk");

		_tradingStartHour = Param(nameof(TradingStartHour), 0)
		.SetDisplay("Trading Start Hour", "Trading window opening hour", "Sessions");

		_tradingEndHour = Param(nameof(TradingEndHour), 23)
		.SetDisplay("Trading End Hour", "Trading window closing hour", "Sessions");

		_rangeBars = Param(nameof(RangeBars), 0)
		.SetGreaterOrEqual(0)
		.SetDisplay("Range Bars", "Number of completed candles used for the range", "Entries");

		_tradeMode = Param(nameof(TradeMode), TradeModeOption.EdgesOfRange)
		.SetDisplay("Trade Mode", "How price interacts with the range before placing orders", "Entries");

		_maPeriod = Param(nameof(MovingAveragePeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length for trailing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_entryBuyStop = null;
		_entrySellStop = null;
		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;
		_previousCandle = null;
		_movingAverage = null;
		_rangeHighIndicator = null;
		_rangeLowIndicator = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TradingStartHour < 0 || TradingStartHour > 23)
		throw new InvalidOperationException("TradingStartHour must be between 0 and 23.");

		if (TradingEndHour < 0 || TradingEndHour > 23)
		throw new InvalidOperationException("TradingEndHour must be between 0 and 23.");

		if (TradingStartHour >= TradingEndHour)
		throw new InvalidOperationException("TradingStartHour must be strictly less than TradingEndHour.");

		if (UseTrailingAtHighLow && UseTrailingMa)
		throw new InvalidOperationException("Only one trailing mode can be enabled at a time.");

		_priceStep = Security?.PriceStep ?? 1m;
		_movingAverage = new SimpleMovingAverage { Length = MovingAveragePeriod };

		var subscription = SubscribeCandles(CandleType);

		if (RangeBars > 0)
		{
			_rangeHighIndicator = new Highest { Length = Math.Max(2, RangeBars) };
			_rangeLowIndicator = new Lowest { Length = Math.Max(2, RangeBars) };

			subscription
			.Bind(_movingAverage, _rangeHighIndicator, _rangeLowIndicator, ProcessCandleWithRange)
			.Start();
		}
		else
		{
			subscription
			.Bind(_movingAverage, ProcessCandleWithoutRange)
			.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			if (UseTrailingMa)
			DrawIndicator(area, _movingAverage);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandleWithoutRange(ICandleMessage candle, decimal maValue)
	{
		ProcessCandleInternal(candle, maValue, null, null);
	}

	private void ProcessCandleWithRange(ICandleMessage candle, decimal maValue, decimal rangeHigh, decimal rangeLow)
	{
		var highValue = _rangeHighIndicator != null && _rangeHighIndicator.IsFormed ? rangeHigh : (decimal?)null;
		var lowValue = _rangeLowIndicator != null && _rangeLowIndicator.IsFormed ? rangeLow : (decimal?)null;

		ProcessCandleInternal(candle, maValue, highValue, lowValue);
	}

	private void ProcessCandleInternal(ICandleMessage candle, decimal maValue, decimal? rangeHigh, decimal? rangeLow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			return;
		}

		if (candle.CloseTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
		{
			_previousCandle = candle;
			return;
		}

		var hour = candle.CloseTime.Hour;
		if (hour < TradingStartHour || hour > TradingEndHour)
		{
			_previousCandle = candle;
			return;
		}

		var netOffset = ToPrice(NetStepPoints);
		if (netOffset <= 0m)
		{
			_previousCandle = candle;
			return;
		}

		if (Position == 0)
		{
			HandleFlatState(candle, netOffset, rangeHigh, rangeLow);
		}
		else if (Position > 0)
		{
			HandleLongState(candle, maValue, netOffset);
		}
		else
		{
			HandleShortState(candle, maValue, netOffset);
		}

		_previousCandle = candle;
	}

	private void HandleFlatState(ICandleMessage candle, decimal netOffset, decimal? rangeHigh, decimal? rangeLow)
	{
		CancelAndReset(ref _longStopOrder);
		CancelAndReset(ref _longTakeProfitOrder);
		CancelAndReset(ref _shortStopOrder);
		CancelAndReset(ref _shortTakeProfitOrder);

		var allowEntries = true;

		if (rangeHigh.HasValue && rangeLow.HasValue && rangeHigh.Value > rangeLow.Value)
		{
			allowEntries = TradeMode switch
			{
				TradeModeOption.EdgesOfRange => candle.ClosePrice >= rangeHigh.Value || candle.ClosePrice <= rangeLow.Value,
				TradeModeOption.CenterOfRange => Math.Abs(candle.ClosePrice - ((rangeHigh.Value + rangeLow.Value) / 2m)) <= _priceStep,
				_ => true,
			};
		}

		if (!allowEntries)
		{
			CancelAndReset(ref _entryBuyStop);
			CancelAndReset(ref _entrySellStop);
			return;
		}

		var volume = EntryVolume;
		if (volume <= 0m)
		{
			CancelAndReset(ref _entryBuyStop);
			CancelAndReset(ref _entrySellStop);
			return;
		}

		var limit = TotalVolumeLimit;
		if (limit > 0m)
		{
			var cap = limit / 2m;
			if (cap <= 0m)
			{
				CancelAndReset(ref _entryBuyStop);
				CancelAndReset(ref _entrySellStop);
				return;
			}

			volume = Math.Min(volume, cap);
		}

		var buyPrice = RoundPrice(candle.ClosePrice + netOffset);
		var sellPrice = RoundPrice(candle.ClosePrice - netOffset);

		if (buyPrice <= 0m || sellPrice <= 0m)
		{
			CancelAndReset(ref _entryBuyStop);
			CancelAndReset(ref _entrySellStop);
			return;
		}

		UpdateEntryOrder(ref _entryBuyStop, volume, buyPrice, true);
		UpdateEntryOrder(ref _entrySellStop, volume, sellPrice, false);
	}

	private void HandleLongState(ICandleMessage candle, decimal maValue, decimal netOffset)
	{
		CancelAndReset(ref _entrySellStop);

		var currentVolume = Math.Abs(Position);
		if (currentVolume <= 0m)
		return;

		var stopLossOffset = ToPrice(StopLossPoints);
		var takeProfitOffset = ToPrice(TakeProfitPoints);
		var trailingOffset = ToPrice(TrailingStopPoints);

		var stopPrice = CalculateLongStop(candle, maValue, stopLossOffset, trailingOffset);
		var takePrice = CalculateLongTakeProfit(takeProfitOffset);

		UpdateExitOrder(ref _longStopOrder, true, currentVolume, stopPrice, true);
		UpdateExitOrder(ref _longTakeProfitOrder, true, currentVolume, takePrice, false);

		HandlePyramiding(ref _entryBuyStop, true, candle.ClosePrice + netOffset, currentVolume);
	}

	private void HandleShortState(ICandleMessage candle, decimal maValue, decimal netOffset)
	{
		CancelAndReset(ref _entryBuyStop);

		var currentVolume = Math.Abs(Position);
		if (currentVolume <= 0m)
		return;

		var stopLossOffset = ToPrice(StopLossPoints);
		var takeProfitOffset = ToPrice(TakeProfitPoints);
		var trailingOffset = ToPrice(TrailingStopPoints);

		var stopPrice = CalculateShortStop(candle, maValue, stopLossOffset, trailingOffset);
		var takePrice = CalculateShortTakeProfit(takeProfitOffset);

		UpdateExitOrder(ref _shortStopOrder, false, currentVolume, stopPrice, true);
		UpdateExitOrder(ref _shortTakeProfitOrder, false, currentVolume, takePrice, false);

		HandlePyramiding(ref _entrySellStop, false, candle.ClosePrice - netOffset, currentVolume);
	}

	private void HandlePyramiding(ref Order entryOrder, bool isLong, decimal rawPrice, decimal currentVolume)
	{
		if (LotMultiplier <= 1m)
		{
			CancelAndReset(ref entryOrder);
			return;
		}

		var limit = TotalVolumeLimit;
		decimal remaining;

		if (limit > 0m)
		{
			remaining = limit - currentVolume;
			if (remaining <= 0m)
			{
				CancelAndReset(ref entryOrder);
				return;
			}
		}
		else
		{
			remaining = EntryVolume * LotMultiplier;
		}

		var additionalVolume = Math.Min(EntryVolume * LotMultiplier, remaining);
		if (additionalVolume <= 0m)
		{
			CancelAndReset(ref entryOrder);
			return;
		}

		var price = RoundPrice(rawPrice);
		if (price <= 0m)
		{
			CancelAndReset(ref entryOrder);
			return;
		}

		UpdateEntryOrder(ref entryOrder, additionalVolume, price, isLong);
	}

	private decimal? CalculateLongStop(ICandleMessage candle, decimal maValue, decimal stopLossOffset, decimal trailingOffset)
	{
		decimal? stopPrice = null;

		if (stopLossOffset > 0m && PositionPrice != 0m)
		stopPrice = PositionPrice - stopLossOffset;

		if (UseTrailingAtHighLow && _previousCandle != null)
		{
			var candidate = _previousCandle.LowPrice;
			if (candidate > 0m)
			stopPrice = stopPrice is null ? candidate : Math.Max(stopPrice.Value, candidate);
		}
		else if (UseTrailingMa && maValue > 0m)
		{
			stopPrice = stopPrice is null ? maValue : Math.Max(stopPrice.Value, maValue);
		}
		else if (TrailingStopPoints > 0m && trailingOffset > 0m)
		{
			var candidate = candle.ClosePrice - trailingOffset;
			stopPrice = stopPrice is null ? candidate : Math.Max(stopPrice.Value, candidate);
		}

		if (stopPrice is decimal value)
		{
			var maxStop = candle.ClosePrice - _priceStep;
			stopPrice = Math.Min(value, maxStop);
			stopPrice = Math.Max(stopPrice.Value, 0m);
		}

		return stopPrice;
	}

	private decimal? CalculateShortStop(ICandleMessage candle, decimal maValue, decimal stopLossOffset, decimal trailingOffset)
	{
		decimal? stopPrice = null;

		if (stopLossOffset > 0m && PositionPrice != 0m)
		stopPrice = PositionPrice + stopLossOffset;

		if (UseTrailingAtHighLow && _previousCandle != null)
		{
			var candidate = _previousCandle.HighPrice;
			if (candidate > 0m)
			stopPrice = stopPrice is null ? candidate : Math.Min(stopPrice.Value, candidate);
		}
		else if (UseTrailingMa && maValue > 0m)
		{
			stopPrice = stopPrice is null ? maValue : Math.Min(stopPrice.Value, maValue);
		}
		else if (TrailingStopPoints > 0m && trailingOffset > 0m)
		{
			var candidate = candle.ClosePrice + trailingOffset;
			stopPrice = stopPrice is null ? candidate : Math.Min(stopPrice.Value, candidate);
		}

		if (stopPrice is decimal value)
		{
			var minStop = candle.ClosePrice + _priceStep;
			stopPrice = Math.Max(value, minStop);
		}

		return stopPrice;
	}

	private decimal? CalculateLongTakeProfit(decimal takeOffset)
	{
		if (takeOffset <= 0m || PositionPrice == 0m)
		return null;

		return PositionPrice + takeOffset;
	}

	private decimal? CalculateShortTakeProfit(decimal takeOffset)
	{
		if (takeOffset <= 0m || PositionPrice == 0m)
		return null;

		return PositionPrice - takeOffset;
	}

	private void UpdateEntryOrder(ref Order order, decimal volume, decimal price, bool isBuy)
	{
		if (volume <= 0m)
		{
			CancelAndReset(ref order);
			return;
		}

		if (order != null)
		{
			if (order.State == OrderStates.Active && order.Volume == volume && order.Price == price)
			return;

			CancelAndReset(ref order);
		}

		order = isBuy
		? BuyStop(volume, price)
		: SellStop(volume, price);
	}

	private void UpdateExitOrder(ref Order order, bool isLong, decimal volume, decimal? price, bool isStop)
	{
		if (volume <= 0m || price is not decimal target || target <= 0m)
		{
			CancelAndReset(ref order);
			return;
		}

		if (order != null)
		{
			if (order.State == OrderStates.Active && order.Volume == volume && order.Price == target)
			return;

			CancelAndReset(ref order);
		}

		order = isStop
		? (isLong ? SellStop(volume, target) : BuyStop(volume, target))
		: (isLong ? SellLimit(volume, target) : BuyLimit(volume, target));
	}

	private void CancelAndReset(ref Order order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private decimal ToPrice(decimal points)
	{
		if (points <= 0m)
		return 0m;

		var step = _priceStep > 0m ? _priceStep : 1m;
		return points * step;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = _priceStep > 0m ? _priceStep : 1m;
		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			CancelAndReset(ref _entrySellStop);
		}
		else if (Position < 0)
		{
			CancelAndReset(ref _entryBuyStop);
		}
		else
		{
			CancelAndReset(ref _entryBuyStop);
			CancelAndReset(ref _entrySellStop);
			CancelAndReset(ref _longStopOrder);
			CancelAndReset(ref _longTakeProfitOrder);
			CancelAndReset(ref _shortStopOrder);
			CancelAndReset(ref _shortTakeProfitOrder);
		}
	}
}
