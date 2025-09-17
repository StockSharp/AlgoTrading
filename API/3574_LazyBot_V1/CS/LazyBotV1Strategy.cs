using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy converted from the LazyBot MT5 expert advisor.
/// The strategy places buy and sell stop orders around the previous day's range
/// and keeps an adaptive trailing stop equal to the configured pip distance.
/// </summary>
public class LazyBotV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _botName;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _addPricePips;
	private readonly StrategyParam<decimal> _maxSpreadPips;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _useRiskPercent;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;

	private DateTime? _lastSignalDate;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal _pipSize;

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Name that is written into order comments.
	/// </summary>
	public string BotName
	{
		get => _botName.Value;
		set => _botName.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Additional offset from the previous day's extremes in pips.
	/// </summary>
	public decimal AddPricePips
	{
		get => _addPricePips.Value;
		set => _addPricePips.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in pips. Set to zero to disable the filter.
	/// </summary>
	public decimal MaxSpreadPips
	{
		get => _maxSpreadPips.Value;
		set => _maxSpreadPips.Value = value;
	}

	/// <summary>
	/// Enables the trading window filter.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Start hour of the trading window (server time).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour of the trading window (server time).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Switches between fixed and risk-based position sizing.
	/// </summary>
	public bool UseRiskPercent
	{
		get => _useRiskPercent.Value;
		set => _useRiskPercent.Value = value;
	}

	/// <summary>
	/// Risk percentage applied to the account equity when <see cref="UseRiskPercent"/> is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed trading volume. When zero the strategy falls back to <see cref="Strategy.Volume"/>.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LazyBotV1Strategy"/> class.
	/// </summary>
	public LazyBotV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used to detect the daily range", "General");

		_botName = Param(nameof(BotName), "LazyBot_V1")
		.SetDisplay("Bot Name", "Text stored in order comments", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Trailing stop distance measured in pips", "Risk Management")
		.SetNotNegative();

		_addPricePips = Param(nameof(AddPricePips), 0m)
		.SetDisplay("Breakout Offset (pips)", "Extra distance added to the previous day's high/low", "Signals");

		_maxSpreadPips = Param(nameof(MaxSpreadPips), 0m)
		.SetDisplay("Max Spread (pips)", "Maximum allowed spread before placing new orders", "Risk Management")
		.SetNotNegative();

		_useTradingHours = Param(nameof(UseTradingHours), true)
		.SetDisplay("Use Trading Hours", "Enable the start hour filter", "Timing");

		_startHour = Param(nameof(StartHour), 7)
		.SetDisplay("Start Hour", "Hour when pending orders can be placed", "Timing")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 22)
		.SetDisplay("End Hour", "Hour when pending orders are no longer created", "Timing")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_useRiskPercent = Param(nameof(UseRiskPercent), false)
		.SetDisplay("Use Risk %", "Turn on risk based position sizing", "Risk Management");

		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetDisplay("Risk %", "Percentage of equity used to compute the order volume", "Risk Management")
		.SetNotNegative();

		_fixedVolume = Param(nameof(FixedVolume), 0.01m)
		.SetDisplay("Fixed Volume", "Fixed order volume used when risk sizing is disabled", "Trading")
		.SetNotNegative();

		Volume = 0.01m;
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

		_lastSignalDate = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeLevel1();

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

		UpdateTrailingStops();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWeekday(CurrentTime))
		return;

		if (UseTradingHours && !IsWithinTradingHours(CurrentTime))
		return;

		var signalDate = candle.OpenTime.Date;
		if (_lastSignalDate != null && _lastSignalDate.Value == signalDate)
		return;

		if (!IsSpreadAcceptable())
		return;

		PlacePendingOrders(candle, signalDate);

		_lastSignalDate = signalDate;
	}

	private void PlacePendingOrders(ICandleMessage candle, DateTime signalDate)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		CancelPendingOrder(ref _buyStopOrder);
		CancelPendingOrder(ref _sellStopOrder);

		var addOffset = ConvertPipsToPrice(AddPricePips);
		var stopDistance = ConvertPipsToPrice(StopLossPips);

		var previousHigh = candle.HighPrice + addOffset;
		var previousLow = candle.LowPrice - addOffset;

		var buyPrice = ShrinkPrice(previousHigh);
		var sellPrice = ShrinkPrice(previousLow);

		if (buyPrice > 0m)
		{
			_buyStopOrder = BuyStop(volume, buyPrice);
			if (_buyStopOrder != null)
			_buyStopOrder.Comment = CreateOrderComment(signalDate);
		}

		if (sellPrice > 0m)
		{
			_sellStopOrder = SellStop(volume, sellPrice);
			if (_sellStopOrder != null)
			_sellStopOrder.Comment = CreateOrderComment(signalDate);
		}

		if (stopDistance <= 0m)
		return;

		// Reset trailing targets so they will be recalculated after a fill.
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = FixedVolume > 0m ? FixedVolume : (Volume > 0m ? Volume : 0m);

		if (!UseRiskPercent)
		return AdjustVolume(baseVolume);

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		if (stopDistance <= 0m)
		return AdjustVolume(baseVolume);

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
		return AdjustVolume(baseVolume);

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return AdjustVolume(baseVolume);

		var riskVolume = riskAmount / stopDistance;
		if (riskVolume <= 0m)
		return AdjustVolume(baseVolume);

		// Mirror the original behavior by never going below the base lot size.
		if (baseVolume > 0m && riskVolume < baseVolume)
		riskVolume = baseVolume;

		return AdjustVolume(riskVolume);
	}

	private void UpdateTrailingStops()
	{
		if (Position == 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
			return;
		}

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		if (stopDistance <= 0m)
		return;

		var bid = Security?.BestBid?.Price ?? 0m;
		var ask = Security?.BestAsk?.Price ?? 0m;

		if (Position > 0m)
		{
			var reference = bid > 0m ? bid : (Security?.LastTrade?.Price ?? 0m);
			if (reference <= 0m)
			return;

			var desiredStop = ShrinkPrice(reference - stopDistance);
			if (desiredStop <= 0m)
			return;

			if (_longStopPrice == null || desiredStop > _longStopPrice.Value)
			{
				SetStopLoss(stopDistance, reference, Position);
				_longStopPrice = desiredStop;
			}
		}
		else if (Position < 0m)
		{
			var reference = ask > 0m ? ask : (Security?.LastTrade?.Price ?? 0m);
			if (reference <= 0m)
			return;

			var desiredStop = ShrinkPrice(reference + stopDistance);
			if (desiredStop <= 0m)
			return;

			if (_shortStopPrice == null || desiredStop < _shortStopPrice.Value)
			{
				SetStopLoss(stopDistance, reference, Position);
				_shortStopPrice = desiredStop;
			}
		}
	}

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPips <= 0m)
		return true;

		var bid = Security?.BestBid?.Price ?? 0m;
		var ask = Security?.BestAsk?.Price ?? 0m;

		if (bid <= 0m || ask <= 0m || _pipSize <= 0m)
		return true;

		var spread = (ask - bid) / _pipSize;
		return spread <= MaxSpreadPips;
	}

	private bool IsWeekday(DateTimeOffset time)
	{
		return time.DayOfWeek != DayOfWeek.Saturday && time.DayOfWeek != DayOfWeek.Sunday;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;

		if (StartHour == EndHour)
		return hour >= StartHour;

		if (StartHour < EndHour)
		return hour >= StartHour && hour < EndHour;

		return hour >= StartHour || hour < EndHour;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
		return 0m;

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 0m;

		var decimals = GetDecimalPlaces(priceStep);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * factor;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);

		var bits = decimal.GetBits(value);
		var scale = (bits[3] >> 16) & 0xFF;
		return scale;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
		volume = maxVolume.Value;

		return volume;
	}

	private void CancelPendingOrder(ref Order? order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private string CreateOrderComment(DateTime signalDate)
	{
		var symbol = Security?.Id?.ToString() ?? string.Empty;
		var barsInfo = signalDate.ToString("yyyyMMdd");
		return string.IsNullOrEmpty(symbol)
		? BotName
		: $"{BotName};{symbol};{barsInfo}";
	}

	private decimal ShrinkPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position == 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
			return;
		}

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		if (stopDistance <= 0m)
		return;

		var reference = trade.Trade.Price;
		if (reference <= 0m)
		return;

		SetStopLoss(stopDistance, reference, Position);

		if (Position > 0m)
		{
			_longStopPrice = ShrinkPrice(reference - stopDistance);
			_shortStopPrice = null;
		}
		else if (Position < 0m)
		{
			_shortStopPrice = ShrinkPrice(reference + stopDistance);
			_longStopPrice = null;
		}
	}
}
