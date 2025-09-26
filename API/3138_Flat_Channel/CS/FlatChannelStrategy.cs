using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flat channel breakout strategy translated from the "Flat Channel (barabashkakvn's edition)" MQL5 expert advisor.
/// Detects a volatility squeeze via a smoothed standard deviation, builds a horizontal channel, and places stop entries beyond its borders.
/// Pending orders inherit dynamic or fixed stop-loss/take-profit levels and are followed by an optional trailing stop once the position is live.
/// </summary>
public class FlatChannelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _pipSizeParam;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _flatBars;
	private readonly StrategyParam<int> _channelLookback;
	private readonly StrategyParam<decimal> _channelMinPips;
	private readonly StrategyParam<decimal> _channelMaxPips;
	private readonly StrategyParam<decimal> _dynamicStopMultiplier;
	private readonly StrategyParam<decimal> _dynamicTakeMultiplier;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useBuy;
	private readonly StrategyParam<bool> _useSell;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation? _stdDev;
	private SimpleMovingAverage _stdDevSma;
	private Highest? _highest;
	private Lowest? _lowest;

	private decimal _pipSize;
	private decimal? _lastStdValue;
	private int _flatSequence;
	private bool _flatPatternActive;
	private bool _allowBuyStop;
	private bool _allowSellStop;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private decimal? _plannedLongStop;
	private decimal? _plannedLongTake;
	private decimal? _plannedShortStop;
	private decimal? _plannedShortTake;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Trade volume per pending order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Override for pip size. Leave at zero to auto-detect from the security.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSizeParam.Value;
		set => _pipSizeParam.Value = value;
	}

	/// <summary>
	/// Length of the base standard deviation calculation.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Moving average length used to smooth the volatility series.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Number of consecutive decreasing volatility values required to confirm a flat.
	/// </summary>
	public int FlatBars
	{
		get => _flatBars.Value;
		set => _flatBars.Value = value;
	}

	/// <summary>
	/// Candle count used to build the channel extremes once a flat is detected.
	/// </summary>
	public int ChannelLookback
	{
		get => _channelLookback.Value;
		set => _channelLookback.Value = value;
	}

	/// <summary>
	/// Minimum channel height in pips (0 disables the filter).
	/// </summary>
	public decimal ChannelMinPips
	{
		get => _channelMinPips.Value;
		set => _channelMinPips.Value = value;
	}

	/// <summary>
	/// Maximum channel height in pips (0 disables the filter).
	/// </summary>
	public decimal ChannelMaxPips
	{
		get => _channelMaxPips.Value;
		set => _channelMaxPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to channel height when calculating dynamic stop-loss levels.
	/// </summary>
	public decimal DynamicStopMultiplier
	{
		get => _dynamicStopMultiplier.Value;
		set => _dynamicStopMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to channel height when calculating dynamic take-profit levels.
	/// </summary>
	public decimal DynamicTakeMultiplier
	{
		get => _dynamicTakeMultiplier.Value;
		set => _dynamicTakeMultiplier.Value = value;
	}

	/// <summary>
	/// Fixed stop-loss distance in pips. When zero the dynamic multiplier is used instead.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Fixed take-profit distance in pips. When zero the dynamic multiplier is used instead.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Additional offset (in pips) added above/below the channel when placing the stop orders.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Set to zero to disable trailing.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal step (in pips) required to move the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables long-side trades.
	/// </summary>
	public bool UseBuy
	{
		get => _useBuy.Value;
		set => _useBuy.Value = value;
	}

	/// <summary>
	/// Enables short-side trades.
	/// </summary>
	public bool UseSell
	{
		get => _useSell.Value;
		set => _useSell.Value = value;
	}

	/// <summary>
	/// Maximum number of base volumes allowed in the aggregate position.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Enables the trading session filter.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Session start hour (inclusive) in exchange time.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour (exclusive) in exchange time.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candle series used for all calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FlatChannelStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume per pending entry", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_pipSizeParam = Param(nameof(PipSize), 0.0001m)
		.SetDisplay("Pip Size", "Custom pip size override", "Market")
		.SetCanOptimize(false);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 46)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Period", "Base standard deviation length", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(20, 80, 5);

		_smoothingLength = Param(nameof(SmoothingLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Moving average applied to the volatility", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_flatBars = Param(nameof(FlatBars), 3)
		.SetGreaterThanZero()
		.SetDisplay("Flat Bars", "Consecutive bars with shrinking volatility", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(2, 6, 1);

		_channelLookback = Param(nameof(ChannelLookback), 5)
		.SetGreaterThanZero()
		.SetDisplay("Channel Lookback", "Candles used to compute channel extremes", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 1);

		_channelMinPips = Param(nameof(ChannelMinPips), 15m)
		.SetDisplay("Min Channel", "Minimum channel height in pips", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_channelMaxPips = Param(nameof(ChannelMaxPips), 105m)
		.SetDisplay("Max Channel", "Maximum channel height in pips", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(60m, 200m, 10m);

		_dynamicStopMultiplier = Param(nameof(DynamicStopMultiplier), 1m)
		.SetDisplay("Stop Mult", "Channel height multiplier for dynamic stops", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.25m);

		_dynamicTakeMultiplier = Param(nameof(DynamicTakeMultiplier), 1m)
		.SetDisplay("Take Mult", "Channel height multiplier for dynamic targets", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.25m);

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetDisplay("Stop Loss", "Fixed stop-loss distance in pips", "Risk")
		.SetCanOptimize(false);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetDisplay("Take Profit", "Fixed take-profit distance in pips", "Risk")
		.SetCanOptimize(false);

		_indentPips = Param(nameof(IndentPips), 0m)
		.SetDisplay("Indent", "Additional offset in pips when placing stops", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(0m, 20m, 2m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 30m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step", "Minimal trailing step in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 2m);

		_useBuy = Param(nameof(UseBuy), true)
		.SetDisplay("Use Buy", "Enable long-side entries", "General");

		_useSell = Param(nameof(UseSell), true)
		.SetDisplay("Use Sell", "Enable short-side entries", "General");

		_maxPositions = Param(nameof(MaxPositions), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum aggregated lots (TradeVolume multiplier)", "Risk")
		.SetCanOptimize(false);

		_useTradingHours = Param(nameof(UseTradingHours), true)
		.SetDisplay("Use Trading Hours", "Enable intraday time filter", "Session");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Session start hour", "Session")
		.SetCanOptimize(false);

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Session end hour", "Session")
		.SetCanOptimize(false);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
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

		_stdDev = null;
		_stdDevSma = null;
		_highest = null;
		_lowest = null;

		_pipSize = 0m;
		_lastStdValue = null;
		_flatSequence = 0;
		_flatPatternActive = false;
		_allowBuyStop = false;
		_allowSellStop = false;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_plannedLongStop = null;
		_plannedLongTake = null;
		_plannedShortStop = null;
		_plannedShortTake = null;

		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (ChannelMinPips > 0m && ChannelMaxPips > 0m && ChannelMinPips >= ChannelMaxPips)
			throw new InvalidOperationException("ChannelMinPips must be less than ChannelMaxPips when both are positive.");

		if (UseTradingHours && StartHour == EndHour)
			throw new InvalidOperationException("StartHour and EndHour must differ when the time filter is enabled.");

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("TrailingStepPips must be positive when trailing is active.");

		_pipSize = ResolvePipSize();

		_stdDev = new StandardDeviation { Length = StdDevPeriod };
		_stdDevSma = SmoothingLength > 1 ? new SimpleMovingAverage { Length = SmoothingLength } : null;

		var lookback = Math.Max(ChannelLookback, FlatBars + 1);
		_highest = new Highest { Length = lookback };
		_lowest = new Lowest { Length = lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPosition(candle);

		if (Position != 0)
			return;

		if (_stdDev is null || _highest is null || _lowest is null)
			return;

		var stdValue = _stdDev.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!stdValue.IsFormed)
			return;

		var currentStd = stdValue.ToDecimal();
		if (_stdDevSma != null)
		{
			var smoothValue = _stdDevSma.Process(currentStd, candle.OpenTime, true);
			if (!smoothValue.IsFormed)
				return;

			currentStd = smoothValue.ToDecimal();
		}

		UpdateFlatState(currentStd);

		if (!_flatPatternActive)
		{
			ResetPendingState();
			return;
		}

		var highestValue = _highest.Process(candle.HighPrice, candle.OpenTime, true);
		var lowestValue = _lowest.Process(candle.LowPrice, candle.OpenTime, true);

		if (!highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		var highest = highestValue.ToDecimal();
		var lowest = lowestValue.ToDecimal();
		if (highest <= lowest)
		{
			ResetPendingState();
			return;
		}

		var channelHeight = highest - lowest;
		var minHeight = ConvertPips(ChannelMinPips);
		var maxHeight = ConvertPips(ChannelMaxPips);

		var heightOk = (minHeight <= 0m || channelHeight >= minHeight) && (maxHeight <= 0m || channelHeight <= maxHeight);
		if (!heightOk)
		{
			ResetPendingState();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTradingHours && !IsWithinTradingHours(candle.OpenTime))
			return;

		var indent = ConvertPips(IndentPips);
		var buyPrice = ShrinkPrice(highest + indent);
		var sellPrice = ShrinkPrice(lowest - indent);

		var plannedLongStop = CalculateStopPrice(true, buyPrice, highest, channelHeight);
		var plannedLongTake = CalculateTakePrice(true, buyPrice, highest, channelHeight);
		var plannedShortStop = CalculateStopPrice(false, sellPrice, lowest, channelHeight);
		var plannedShortTake = CalculateTakePrice(false, sellPrice, lowest, channelHeight);

		var maxExposure = TradeVolume * MaxPositions;
		var absPos = Math.Abs(Position);

		if (UseBuy && _allowBuyStop && _buyStopOrder == null && absPos < maxExposure)
		{
			_plannedLongStop = plannedLongStop;
			_plannedLongTake = plannedLongTake;

			_buyStopOrder = BuyStop(TradeVolume, buyPrice);
			_allowBuyStop = false;
			LogInfo($"Buy stop placed at {buyPrice:F5} (channel high {highest:F5}).");
		}

		if (UseSell && _allowSellStop && _sellStopOrder == null && absPos < maxExposure)
		{
			_plannedShortStop = plannedShortStop;
			_plannedShortTake = plannedShortTake;

			_sellStopOrder = SellStop(TradeVolume, sellPrice);
			_allowSellStop = false;
			LogInfo($"Sell stop placed at {sellPrice:F5} (channel low {lowest:F5}).");
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		UpdateTrailingStop(candle);

		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}
	}

	private void UpdateFlatState(decimal currentStd)
	{
		if (_lastStdValue is decimal prevStd && currentStd <= prevStd)
		{
			_flatSequence++;
		}
		else
		{
			_flatSequence = 0;
		}

		_lastStdValue = currentStd;

		var wasActive = _flatPatternActive;
		_flatPatternActive = _flatSequence >= FlatBars;

		if (_flatPatternActive && !wasActive)
		{
			_allowBuyStop = UseBuy;
			_allowSellStop = UseSell;
		}
		else if (!_flatPatternActive)
		{
			ResetPendingState();
		}
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _entryPrice is null)
			return;

		var trailingDistance = ConvertPips(TrailingStopPips);
		if (trailingDistance <= 0m)
			return;

		var trailingStep = ConvertPips(TrailingStepPips);
		if (Position > 0)
		{
			var profit = candle.ClosePrice - _entryPrice.Value;
			if (profit <= trailingDistance)
				return;

			var newStop = ShrinkPrice(candle.ClosePrice - trailingDistance);
			if (_stopPrice == null || newStop - _stopPrice.Value >= trailingStep)
				_stopPrice = newStop;
		}
		else if (Position < 0)
		{
			var profit = _entryPrice.Value - candle.ClosePrice;
			if (profit <= trailingDistance)
				return;

			var newStop = ShrinkPrice(candle.ClosePrice + trailingDistance);
			if (_stopPrice == null || _stopPrice.Value - newStop >= trailingStep)
				_stopPrice = newStop;
		}
	}

	private void ResetPendingState()
	{
		CancelPendingOrder(ref _buyStopOrder);
		CancelPendingOrder(ref _sellStopOrder);

		_plannedLongStop = null;
		_plannedLongTake = null;
		_plannedShortStop = null;
		_plannedShortTake = null;

		_allowBuyStop = false;
		_allowSellStop = false;
	}

	private decimal? CalculateStopPrice(bool isLong, decimal orderPrice, decimal boundary, decimal channelHeight)
	{
		decimal? candidate = null;

		if (StopLossPips > 0m)
		{
			var distance = ConvertPips(StopLossPips);
			candidate = isLong ? orderPrice - distance : orderPrice + distance;
		}
		else if (DynamicStopMultiplier > 0m)
		{
			var offset = channelHeight * DynamicStopMultiplier;
			candidate = isLong ? boundary - offset : boundary + offset;
		}

		if (!candidate.HasValue || candidate.Value <= 0m)
			return null;

		var price = ShrinkPrice(candidate.Value);

		if (isLong && price >= orderPrice)
			return null;

		if (!isLong && price <= orderPrice)
			return null;

		return price;
	}

	private decimal? CalculateTakePrice(bool isLong, decimal orderPrice, decimal boundary, decimal channelHeight)
	{
		decimal? candidate = null;

		if (TakeProfitPips > 0m)
		{
			var distance = ConvertPips(TakeProfitPips);
			candidate = isLong ? orderPrice + distance : orderPrice - distance;
		}
		else if (DynamicTakeMultiplier > 0m)
		{
			var offset = channelHeight * DynamicTakeMultiplier;
			candidate = isLong ? boundary + offset : boundary - offset;
		}

		if (!candidate.HasValue || candidate.Value <= 0m)
			return null;

		var price = ShrinkPrice(candidate.Value);

		if (isLong && price <= orderPrice)
			return null;

		if (!isLong && price >= orderPrice)
			return null;

		return price;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		if (!UseTradingHours)
			return true;

		var start = StartHour;
		var end = EndHour;
		var hour = time.Hour;

		if (start < end)
			return hour >= start && hour < end;

		return hour >= start || hour < end;
	}

	private decimal ConvertPips(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var size = _pipSize;
		if (size <= 0m)
			size = 0.0001m;

		return pips * size;
	}

	private decimal ShrinkPrice(decimal price)
	{
		var security = Security;
		return security?.ShrinkPrice(price) ?? price;
	}

	private decimal ResolvePipSize()
	{
		if (PipSize > 0m)
			return PipSize;

		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		{
			var decimals = security.Decimals;
			var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
			return step * adjust;
		}

		return 0.0001m;
	}

	private void CancelPendingOrder(ref Order order)
	{
		if (order == null)
			return;

		CancelOrder(order);
		order = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		if (trade.Order == _buyStopOrder)
		{
			_buyStopOrder = null;
			CancelPendingOrder(ref _sellStopOrder);

			_entryPrice = trade.Trade.Price;
			_stopPrice = _plannedLongStop;
			_takeProfitPrice = _plannedLongTake;

			_plannedLongStop = null;
			_plannedLongTake = null;
			_plannedShortStop = null;
			_plannedShortTake = null;

			_allowBuyStop = false;
			_allowSellStop = false;
		}
		else if (trade.Order == _sellStopOrder)
		{
			_sellStopOrder = null;
			CancelPendingOrder(ref _buyStopOrder);

			_entryPrice = trade.Trade.Price;
			_stopPrice = _plannedShortStop;
			_takeProfitPrice = _plannedShortTake;

			_plannedLongStop = null;
			_plannedLongTake = null;
			_plannedShortStop = null;
			_plannedShortTake = null;

			_allowBuyStop = false;
			_allowSellStop = false;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}
