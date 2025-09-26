using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the VirtPOTestBed_Scalp expert advisor that emulates virtual pending orders
/// and converts the logic to StockSharp high level API.
/// </summary>
public class VirtPoTestBedScalpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _tickLevel;
	private readonly StrategyParam<decimal> _poThresholdPips;
	private readonly StrategyParam<decimal> _poTimeLimitMinutes;
	private readonly StrategyParam<decimal> _spreadMaxPips;
	private readonly StrategyParam<decimal> _volumeLimit;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<decimal> _volatilityLimit;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerLowerLimit;
	private readonly StrategyParam<decimal> _bollingerUpperLimit;
	private readonly StrategyParam<decimal> _lastBarLimitPips;
	private readonly StrategyParam<int> _smaFastPeriod;
	private readonly StrategyParam<int> _smaSlowPeriod;
	private readonly StrategyParam<decimal> _smaDifferencePips;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<int> _stochasticSmooth;
	private readonly StrategyParam<decimal> _stochasticSetLevel;
	private readonly StrategyParam<decimal> _stochasticGoLevel;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _dayNoTrade;
	private readonly StrategyParam<int> _day1;
	private readonly StrategyParam<int> _day2;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _openHours;
	private readonly StrategyParam<int> _fridayEndHour;
	private readonly StrategyParam<decimal> _closeTimeMinutes;
	private readonly StrategyParam<int> _profitType;

	private BollingerBands _bollinger = null!;
	private StochasticOscillator _stochastic = null!;
	private SimpleMovingAverage _fastSma = null!;
	private SimpleMovingAverage _slowSma = null!;
	private SimpleMovingAverage _volumeAverage = null!;
	private SimpleMovingAverage _volatilityAverage = null!;

	private decimal? _prevStochastic;
	private decimal? _lastStochastic;
	private ICandleMessage _previousCandle;

	private bool _pendingBuyActive;
	private bool _pendingSellActive;
	private decimal _pendingBuyPrice;
	private decimal _pendingSellPrice;
	private DateTimeOffset? _pendingBuyExpiration;
	private DateTimeOffset? _pendingSellExpiration;
	private decimal _pendingBuyStopDistance;
	private decimal _pendingSellStopDistance;
	private decimal _pendingBuyTakeDistance;
	private decimal _pendingSellTakeDistance;

	private decimal _entryPrice;
	private DateTimeOffset? _entryTime;
	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;
	private decimal _stopPrice;
	private decimal _takePrice;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal _lastTradePrice;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Use tick level evaluation for virtual orders.
	/// </summary>
	public bool TickLevel
	{
		get => _tickLevel.Value;
		set => _tickLevel.Value = value;
	}

	/// <summary>
	/// Pending order trigger threshold in pips.
	/// </summary>
	public decimal PoThresholdPips
	{
		get => _poThresholdPips.Value;
		set => _poThresholdPips.Value = value;
	}

	/// <summary>
	/// Pending order expiration in minutes.
	/// </summary>
	public decimal PoTimeLimitMinutes
	{
		get => _poTimeLimitMinutes.Value;
		set => _poTimeLimitMinutes.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in pips.
	/// </summary>
	public decimal SpreadMaxPips
	{
		get => _spreadMaxPips.Value;
		set => _spreadMaxPips.Value = value;
	}

	/// <summary>
	/// Minimum required average volume.
	/// </summary>
	public decimal VolumeLimit
	{
		get => _volumeLimit.Value;
		set => _volumeLimit.Value = value;
	}

	/// <summary>
	/// Period used to evaluate volatility.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute volatility in pips.
	/// </summary>
	public decimal VolatilityLimit
	{
		get => _volatilityLimit.Value;
		set => _volatilityLimit.Value = value;
	}

	/// <summary>
	/// Bollinger bands calculation period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Lower limit for Bollinger band width in pips.
	/// </summary>
	public decimal BollingerLowerLimit
	{
		get => _bollingerLowerLimit.Value;
		set => _bollingerLowerLimit.Value = value;
	}

	/// <summary>
	/// Upper limit for Bollinger band width in pips.
	/// </summary>
	public decimal BollingerUpperLimit
	{
		get => _bollingerUpperLimit.Value;
		set => _bollingerUpperLimit.Value = value;
	}

	/// <summary>
	/// Maximum allowable size of the previous bar body in pips.
	/// </summary>
	public decimal LastBarLimitPips
	{
		get => _lastBarLimitPips.Value;
		set => _lastBarLimitPips.Value = value;
	}

	/// <summary>
	/// Fast SMA period for trend detection.
	/// </summary>
	public int SmaFastPeriod
	{
		get => _smaFastPeriod.Value;
		set => _smaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period for trend detection.
	/// </summary>
	public int SmaSlowPeriod
	{
		get => _smaSlowPeriod.Value;
		set => _smaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Minimum SMA difference in pips to confirm trend direction.
	/// </summary>
	public decimal SmaDifferencePips
	{
		get => _smaDifferencePips.Value;
		set => _smaDifferencePips.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
	}

	/// <summary>
	/// Stochastic smoothing factor.
	/// </summary>
	public int StochasticSmooth
	{
		get => _stochasticSmooth.Value;
		set => _stochasticSmooth.Value = value;
	}

	/// <summary>
	/// Level used to arm virtual pending orders.
	/// </summary>
	public decimal StochasticSetLevel
	{
		get => _stochasticSetLevel.Value;
		set => _stochasticSetLevel.Value = value;
	}

	/// <summary>
	/// Level used to execute armed virtual pending orders.
	/// </summary>
	public decimal StochasticGoLevel
	{
		get => _stochasticGoLevel.Value;
		set => _stochasticGoLevel.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Disable trading on selected weekdays.
	/// </summary>
	public bool DayNoTrade
	{
		get => _dayNoTrade.Value;
		set => _dayNoTrade.Value = value;
	}

	/// <summary>
	/// First day of week when trading is disabled.
	/// </summary>
	public int Day1
	{
		get => _day1.Value;
		set => _day1.Value = value;
	}

	/// <summary>
	/// Second day of week when trading is disabled.
	/// </summary>
	public int Day2
	{
		get => _day2.Value;
		set => _day2.Value = value;
	}

	/// <summary>
	/// Trading window opening hour.
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// Trading window duration in hours.
	/// </summary>
	public int OpenHours
	{
		get => _openHours.Value;
		set => _openHours.Value = value;
	}

	/// <summary>
	/// Friday cut-off hour.
	/// </summary>
	public int FridayEndHour
	{
		get => _fridayEndHour.Value;
		set => _fridayEndHour.Value = value;
	}

	/// <summary>
	/// Maximum position lifetime in minutes.
	/// </summary>
	public decimal CloseTimeMinutes
	{
		get => _closeTimeMinutes.Value;
		set => _closeTimeMinutes.Value = value;
	}

	/// <summary>
	/// Profit filter used during time based exit.
	/// </summary>
	public int ProfitType
	{
		get => _profitType.Value;
		set => _profitType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="VirtPoTestBedScalpStrategy"/>.
	/// </summary>
	public VirtPoTestBedScalpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");

		_tickLevel = Param(nameof(TickLevel), true)
			.SetDisplay("Tick Level", "Execute virtual orders on each trade", "Execution");

		_poThresholdPips = Param(nameof(PoThresholdPips), 2m)
			.SetDisplay("PO Threshold (pips)", "Distance from bid to arm virtual stop", "Execution");

		_poTimeLimitMinutes = Param(nameof(PoTimeLimitMinutes), 15m)
			.SetDisplay("PO Lifetime (min)", "Virtual order expiration in minutes", "Execution");

		_spreadMaxPips = Param(nameof(SpreadMaxPips), 0.5m)
			.SetDisplay("Max Spread (pips)", "Maximum allowed spread", "Filters");

		_volumeLimit = Param(nameof(VolumeLimit), 90m)
			.SetDisplay("Volume Limit", "Minimum average tick volume", "Filters");

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Period", "Sample size for absolute volatility", "Filters");

		_volatilityLimit = Param(nameof(VolatilityLimit), 0.5m)
			.SetDisplay("Volatility Limit", "Minimum average absolute price change", "Filters");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period for Bollinger bandwidth", "Filters");

		_bollingerLowerLimit = Param(nameof(BollingerLowerLimit), 8m)
			.SetDisplay("Bollinger Lower", "Lower limit for band width", "Filters");

		_bollingerUpperLimit = Param(nameof(BollingerUpperLimit), 27m)
			.SetDisplay("Bollinger Upper", "Upper limit for band width", "Filters");

		_lastBarLimitPips = Param(nameof(LastBarLimitPips), 5m)
			.SetDisplay("Last Bar Limit", "Maximum body size for arming orders", "Filters");

		_smaFastPeriod = Param(nameof(SmaFastPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA length", "Trend");

		_smaSlowPeriod = Param(nameof(SmaSlowPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA length", "Trend");

		_smaDifferencePips = Param(nameof(SmaDifferencePips), 3m)
			.SetDisplay("SMA Difference", "Minimum SMA distance in pips", "Trend");

		_stochasticK = Param(nameof(StochasticK), 7)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K period", "Stochastic");

		_stochasticD = Param(nameof(StochasticD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D period", "Stochastic");

		_stochasticSmooth = Param(nameof(StochasticSmooth), 7)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smooth", "Smoothing", "Stochastic");

		_stochasticSetLevel = Param(nameof(StochasticSetLevel), 28m)
			.SetDisplay("Stochastic Set", "Level to arm virtual orders", "Stochastic");

		_stochasticGoLevel = Param(nameof(StochasticGoLevel), 46m)
			.SetDisplay("Stochastic Go", "Level to trigger market orders", "Stochastic");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Default trade volume", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 12m)
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 5m)
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_dayNoTrade = Param(nameof(DayNoTrade), true)
			.SetDisplay("Disable Days", "Enable weekday filters", "Schedule");

		_day1 = Param(nameof(Day1), 99)
			.SetDisplay("First No Trade Day", "Day of week to disable trading", "Schedule");

		_day2 = Param(nameof(Day2), 99)
			.SetDisplay("Second No Trade Day", "Day of week to disable trading", "Schedule");

		_entryHour = Param(nameof(EntryHour), 4)
			.SetDisplay("Entry Hour", "Trading window start hour", "Schedule");

		_openHours = Param(nameof(OpenHours), 18)
			.SetDisplay("Open Hours", "Trading window duration", "Schedule");

		_fridayEndHour = Param(nameof(FridayEndHour), 10)
			.SetDisplay("Friday Cut-off", "Hour to stop on Friday", "Schedule");

		_closeTimeMinutes = Param(nameof(CloseTimeMinutes), 25m)
			.SetDisplay("Max Lifetime", "Time based exit in minutes", "Risk");

		_profitType = Param(nameof(ProfitType), 0)
			.SetDisplay("Profit Filter", "0-all,1-profit,2-loss", "Risk");
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

		_bollinger?.Reset();
		_stochastic?.Reset();
		_fastSma?.Reset();
		_slowSma?.Reset();
		_volumeAverage.Reset();
		_volatilityAverage.Reset();

		_prevStochastic = null;
		_lastStochastic = null;
		_previousCandle = null;

		_pendingBuyActive = false;
		_pendingSellActive = false;
		_pendingBuyPrice = 0m;
		_pendingSellPrice = 0m;
		_pendingBuyExpiration = null;
		_pendingSellExpiration = null;
		_pendingBuyStopDistance = 0m;
		_pendingSellStopDistance = 0m;
		_pendingBuyTakeDistance = 0m;
		_pendingSellTakeDistance = 0m;

		_entryPrice = 0m;
		_entryTime = null;
		_highestSinceEntry = null;
		_lowestSinceEntry = null;
		_stopPrice = 0m;
		_takePrice = 0m;

		_lastBid = null;
		_lastAsk = null;
		_lastTradePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = 2m
		};

		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticK,
			DPeriod = StochasticD,
			Smooth = StochasticSmooth
		};

		_fastSma = new SimpleMovingAverage { Length = SmaFastPeriod };
		_slowSma = new SimpleMovingAverage { Length = SmaSlowPeriod };
		_volumeAverage = new SimpleMovingAverage { Length = 3 };
		_volatilityAverage = new SimpleMovingAverage { Length = VolatilityPeriod };

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.BindEx(_bollinger, _stochastic, _fastSma, _slowSma, ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_lastBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_lastAsk = askPrice;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price is null || price <= 0m)
			return;

		_lastTradePrice = price.Value;

		if (!TickLevel)
			return;

		var time = trade.ServerTime != default ? trade.ServerTime : trade.LocalTime;
		if (time == default)
			time = CurrentTime;

		EvaluatePendingOrders(time, price.Value);
		UpdateRiskManagement(time, price.Value, price.Value, price.Value);
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue bollingerValue,
		IIndicatorValue stochasticValue,
		IIndicatorValue fastSmaValue,
		IIndicatorValue slowSmaValue)
	{
		var pipSize = GetPipSize();
		var volumeAvg = _volumeAverage.Process(candle.TotalVolume ?? 0m).ToDecimal();
		var absMove = Math.Abs(candle.ClosePrice - candle.OpenPrice) / (pipSize == 0m ? 1m : pipSize);
		var volatilityAvg = _volatilityAverage.Process(absMove).ToDecimal();

		if (candle.State != CandleStates.Finished)
		{
			UpdateRiskManagement(candle.CloseTime, candle.ClosePrice, candle.HighPrice, candle.LowPrice);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			_prevStochastic = _lastStochastic;
			_lastStochastic = TryExtractStochastic(stochasticValue);
			return;
		}

		var bollinger = (BollingerBandsValue)bollingerValue;
		if (bollinger.UpBand is not decimal upperBand ||
			bollinger.LowBand is not decimal lowerBand)
		{
			_previousCandle = candle;
			_prevStochastic = _lastStochastic;
			_lastStochastic = TryExtractStochastic(stochasticValue);
			return;
		}

		var stochCurrent = TryExtractStochastic(stochasticValue);
		var fast = fastSmaValue.IsFinal ? fastSmaValue.ToDecimal() : (decimal?)null;
		var slow = slowSmaValue.IsFinal ? slowSmaValue.ToDecimal() : (decimal?)null;

		if (!stochCurrent.HasValue || !fast.HasValue || !slow.HasValue)
		{
			_previousCandle = candle;
			_prevStochastic = _lastStochastic;
			_lastStochastic = stochCurrent;
			return;
		}

		var prevCandle = _previousCandle;
		_previousCandle = candle;

		var prevStoch = _lastStochastic;
		var prevPrevStoch = _prevStochastic;
		_prevStochastic = prevStoch;
		_lastStochastic = stochCurrent;

		var spreadPips = GetSpreadPips(pipSize);
		var bbWidthPips = (upperBand - lowerBand) / (pipSize == 0m ? 1m : pipSize);

		var lastBarUp = prevCandle != null ? (prevCandle.HighPrice - prevCandle.OpenPrice) / (pipSize == 0m ? 1m : pipSize) : decimal.MaxValue;
		var lastBarDown = prevCandle != null ? (prevCandle.OpenPrice - prevCandle.LowPrice) / (pipSize == 0m ? 1m : pipSize) : decimal.MaxValue;

		var smaDiffPips = (fast.Value - slow.Value) / (pipSize == 0m ? 1m : pipSize);
		var trendUp = smaDiffPips > SmaDifferencePips;
		var trendDown = smaDiffPips < -SmaDifferencePips;

		if (Position == 0m && !_pendingBuyActive && !_pendingSellActive)
		{
			var filtersOk = spreadPips <= SpreadMaxPips &&
				volumeAvg > VolumeLimit &&
				volatilityAvg > VolatilityLimit &&
				bbWidthPips > BollingerLowerLimit &&
				bbWidthPips < BollingerUpperLimit &&
				IsWithinTradingWindow(candle.OpenTime);

			if (filtersOk && prevStoch.HasValue && prevPrevStoch.HasValue && (trendUp || trendDown))
			{
				if (lastBarUp < LastBarLimitPips &&
					prevPrevStoch.Value < StochasticSetLevel &&
					prevStoch.Value > StochasticSetLevel)
				{
					ArmPendingBuy(candle.CloseTime, pipSize);
				}

				if (lastBarDown < LastBarLimitPips &&
					prevPrevStoch.Value > 100m - StochasticSetLevel &&
					prevStoch.Value < 100m - StochasticSetLevel)
				{
					ArmPendingSell(candle.CloseTime, pipSize);
				}
			}
		}

		ExpireVirtualOrders(candle.CloseTime);

		if (!TickLevel)
		{
			EvaluatePendingOrders(candle.CloseTime, candle.ClosePrice);
		}

		UpdateRiskManagement(candle.CloseTime, candle.ClosePrice, candle.HighPrice, candle.LowPrice);
	}

	private static decimal? TryExtractStochastic(IIndicatorValue value)
	{
		if (value is StochasticOscillatorValue stoch)
			return stoch.K as decimal?;

		return value.IsFinal ? value.ToDecimal() : null;
	}

	private void ArmPendingBuy(DateTimeOffset time, decimal pipSize)
	{
		var bid = _lastBid ?? _lastTradePrice;
		if (bid <= 0m)
			return;

		_pendingBuyActive = true;
		_pendingBuyPrice = bid + PoThresholdPips * pipSize;
		_pendingBuyExpiration = time + TimeSpan.FromMinutes((double)PoTimeLimitMinutes);
		_pendingBuyStopDistance = StopLossPips * pipSize;
		_pendingBuyTakeDistance = TakeProfitPips * pipSize;
	}

	private void ArmPendingSell(DateTimeOffset time, decimal pipSize)
	{
		var bid = _lastBid ?? _lastTradePrice;
		if (bid <= 0m)
			return;

		_pendingSellActive = true;
		_pendingSellPrice = bid - PoThresholdPips * pipSize;
		_pendingSellExpiration = time + TimeSpan.FromMinutes((double)PoTimeLimitMinutes);
		_pendingSellStopDistance = StopLossPips * pipSize;
		_pendingSellTakeDistance = TakeProfitPips * pipSize;
	}

	private void ExpireVirtualOrders(DateTimeOffset time)
	{
		if (_pendingBuyActive && _pendingBuyExpiration is DateTimeOffset buyExp && time > buyExp)
		{
			_pendingBuyActive = false;
		}

		if (_pendingSellActive && _pendingSellExpiration is DateTimeOffset sellExp && time > sellExp)
		{
			_pendingSellActive = false;
		}
	}

	private void EvaluatePendingOrders(DateTimeOffset time, decimal price)
	{
		if (_pendingBuyActive && price >= _pendingBuyPrice && Position <= 0m)
		{
			_pendingBuyActive = false;
			ExecuteOrder(Sides.Buy, time, price, _pendingBuyStopDistance, _pendingBuyTakeDistance);
		}

		if (_pendingSellActive && price <= _pendingSellPrice && Position >= 0m)
		{
			_pendingSellActive = false;
			ExecuteOrder(Sides.Sell, time, price, _pendingSellStopDistance, _pendingSellTakeDistance);
		}
	}

	private void ExecuteOrder(Sides side, DateTimeOffset time, decimal price, decimal stopDistance, decimal takeDistance)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_entryPrice = price;
		_entryTime = time;
		_highestSinceEntry = price;
		_lowestSinceEntry = price;

		_stopPrice = stopDistance > 0m
			? (side == Sides.Buy ? price - stopDistance : price + stopDistance)
			: 0m;

		_takePrice = takeDistance > 0m
			? (side == Sides.Buy ? price + takeDistance : price - takeDistance)
			: 0m;
	}

	private void UpdateRiskManagement(DateTimeOffset time, decimal closePrice, decimal highPrice, decimal lowPrice)
	{
		if (Position > 0m)
		{
			_highestSinceEntry = _highestSinceEntry.HasValue ? Math.Max(_highestSinceEntry.Value, highPrice) : highPrice;
			ApplyTrailingStop(Sides.Buy);

			if (_takePrice > 0m && highPrice >= _takePrice)
			{
				SellMarket(Math.Abs(Position));
			}
			else if (_stopPrice > 0m && lowPrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
			}
			else
			{
				CheckTimeExit(time, closePrice);
			}
		}
		else if (Position < 0m)
		{
			_lowestSinceEntry = _lowestSinceEntry.HasValue ? Math.Min(_lowestSinceEntry.Value, lowPrice) : lowPrice;
			ApplyTrailingStop(Sides.Sell);

			if (_takePrice > 0m && lowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
			}
			else if (_stopPrice > 0m && highPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
			}
			else
			{
				CheckTimeExit(time, closePrice);
			}
		}
		else
		{
			_entryTime = null;
			_stopPrice = 0m;
			_takePrice = 0m;
			_highestSinceEntry = null;
			_lowestSinceEntry = null;
		}
	}

	private void ApplyTrailingStop(Sides side)
	{
		var pipSize = GetPipSize();
		var trailingDistance = TrailingStopPips * pipSize;
		if (trailingDistance <= 0m)
			return;

		if (side == Sides.Buy && _highestSinceEntry.HasValue)
		{
			var candidate = _highestSinceEntry.Value - trailingDistance;
			if (candidate > _stopPrice)
				_stopPrice = candidate;
		}
		else if (side == Sides.Sell && _lowestSinceEntry.HasValue)
		{
			var candidate = _lowestSinceEntry.Value + trailingDistance;
			if (_stopPrice == 0m || candidate < _stopPrice)
				_stopPrice = candidate;
		}
	}

	private void CheckTimeExit(DateTimeOffset time, decimal price)
	{
		if (_entryTime is null)
			return;

		if (CloseTimeMinutes <= 0m || CloseTimeMinutes >= 5000m)
			return;

		var lifetime = time - _entryTime.Value;
		if (lifetime.TotalMinutes < (double)CloseTimeMinutes)
			return;

		var profit = (price - _entryPrice) * Position;
		var shouldClose = ProfitType == 0 ||
			(ProfitType == 1 && profit >= 0m) ||
			(ProfitType == 2 && profit <= 0m);

		if (shouldClose)
		{
			ClosePosition();
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!DayNoTrade)
			return true;

		var day = (int)time.DayOfWeek;
		if (day == Day1 || day == Day2)
			return false;

		if (day == (int)DayOfWeek.Friday && time.Hour >= FridayEndHour)
			return false;

		var closeHour = (EntryHour + OpenHours) % 24;

		if (closeHour == EntryHour)
			return time.Hour == EntryHour;

		if (closeHour > EntryHour)
			return time.Hour >= EntryHour && time.Hour <= closeHour;

		return time.Hour >= EntryHour || time.Hour <= closeHour;
	}

	private decimal GetSpreadPips(decimal pipSize)
	{
		if (_lastBid is null || _lastAsk is null || pipSize <= 0m)
			return decimal.MaxValue;

		return (_lastAsk.Value - _lastBid.Value) / pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		var factor = GetStepFactor(step);
		return step * factor;
	}

	private static decimal GetStepFactor(decimal step)
	{
		var decimals = 0;
		var value = step;
		while (decimals < 10 && value != Math.Truncate(value))
		{
			value *= 10m;
			decimals++;
		}

		return decimals == 3 || decimals == 5 ? 10m : 1m;
	}
}
