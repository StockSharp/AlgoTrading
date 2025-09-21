using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades Heiken Ashi engulfing patterns confirmed by moving averages and RSI filters.
/// </summary>
public class HeikenAshiEngulfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradeDirectionOption> _directionParam;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _buyStopLossPips;
	private readonly StrategyParam<decimal> _buyTakeProfitPips;
	private readonly StrategyParam<int> _buyBaselinePeriod;
	private readonly StrategyParam<MaMethod> _buyBaselineMethod;
	private readonly StrategyParam<int> _buyFastPeriod;
	private readonly StrategyParam<MaMethod> _buyFastMethod;
	private readonly StrategyParam<int> _buySlowPeriod;
	private readonly StrategyParam<MaMethod> _buySlowMethod;
	private readonly StrategyParam<int> _buyPrimaryRsiPeriod;
	private readonly StrategyParam<int> _buyPrimaryShift;
	private readonly StrategyParam<int> _buyPrimaryWindow;
	private readonly StrategyParam<int> _buyPrimaryExceptions;
	private readonly StrategyParam<decimal> _buyPrimaryUpper;
	private readonly StrategyParam<decimal> _buyPrimaryLower;
	private readonly StrategyParam<int> _buySecondaryRsiPeriod;
	private readonly StrategyParam<int> _buySecondaryShift;
	private readonly StrategyParam<int> _buySecondaryWindow;
	private readonly StrategyParam<int> _buySecondaryExceptions;
	private readonly StrategyParam<decimal> _buySecondaryUpper;
	private readonly StrategyParam<decimal> _buySecondaryLower;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<decimal> _sellStopLossPips;
	private readonly StrategyParam<decimal> _sellTakeProfitPips;
	private readonly StrategyParam<int> _sellBaselinePeriod;
	private readonly StrategyParam<MaMethod> _sellBaselineMethod;
	private readonly StrategyParam<int> _sellFastPeriod;
	private readonly StrategyParam<MaMethod> _sellFastMethod;
	private readonly StrategyParam<int> _sellSlowPeriod;
	private readonly StrategyParam<MaMethod> _sellSlowMethod;
	private readonly StrategyParam<int> _sellPrimaryRsiPeriod;
	private readonly StrategyParam<int> _sellPrimaryShift;
	private readonly StrategyParam<int> _sellPrimaryWindow;
	private readonly StrategyParam<int> _sellPrimaryExceptions;
	private readonly StrategyParam<decimal> _sellPrimaryUpper;
	private readonly StrategyParam<decimal> _sellPrimaryLower;
	private readonly StrategyParam<int> _sellSecondaryRsiPeriod;
	private readonly StrategyParam<int> _sellSecondaryShift;
	private readonly StrategyParam<int> _sellSecondaryWindow;
	private readonly StrategyParam<int> _sellSecondaryExceptions;
	private readonly StrategyParam<decimal> _sellSecondaryUpper;
	private readonly StrategyParam<decimal> _sellSecondaryLower;
	private readonly StrategyParam<string> _alertTitle;
	private readonly StrategyParam<bool> _sendNotification;

	private LengthIndicator<decimal>? _buyBaselineMa;
	private LengthIndicator<decimal>? _buyFastMa;
	private LengthIndicator<decimal>? _buySlowMa;
	private RelativeStrengthIndex? _buyPrimaryRsi;
	private RelativeStrengthIndex? _buySecondaryRsi;
	private LengthIndicator<decimal>? _sellBaselineMa;
	private LengthIndicator<decimal>? _sellFastMa;
	private LengthIndicator<decimal>? _sellSlowMa;
	private RelativeStrengthIndex? _sellPrimaryRsi;
	private RelativeStrengthIndex? _sellSecondaryRsi;

	private readonly List<CandleSnapshot> _candles = new();
	private readonly List<HeikenAshiSnapshot> _heiken = new();
	private readonly List<decimal?> _buyBaselineValues = new();
	private readonly List<decimal?> _buyFastValues = new();
	private readonly List<decimal?> _buySlowValues = new();
	private readonly List<decimal?> _buyPrimaryRsiValues = new();
	private readonly List<decimal?> _buySecondaryRsiValues = new();
	private readonly List<decimal?> _sellBaselineValues = new();
	private readonly List<decimal?> _sellFastValues = new();
	private readonly List<decimal?> _sellSlowValues = new();
	private readonly List<decimal?> _sellPrimaryRsiValues = new();
	private readonly List<decimal?> _sellSecondaryRsiValues = new();

	private decimal _pipSize;
	private HeikenAshiSnapshot? _previousHeiken;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeikenAshiEngulfStrategy"/> class.
	/// </summary>
	public HeikenAshiEngulfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for all indicators and signal calculations.", "Data");

		_directionParam = Param(nameof(Direction), TradeDirectionOption.Both)
		.SetDisplay("Trade Direction", "Which side of the engulfing setup should be executed.", "Trading");

		_buyVolume = Param(nameof(BuyVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Volume", "Position size for bullish signals (lots).", "Trading");

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Buy Stop Loss (pips)", "Protective stop distance for long trades.", "Risk");

		_buyTakeProfitPips = Param(nameof(BuyTakeProfitPips), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Buy Take Profit (pips)", "Profit target distance for long trades.", "Risk");

		_buyBaselinePeriod = Param(nameof(BuyBaselinePeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Buy Baseline MA Period", "Length of the moving average compared with the bullish Heiken Ashi candle.", "Filters");

		_buyBaselineMethod = Param(nameof(BuyBaselineMethod), MaMethod.Exponential)
		.SetDisplay("Buy Baseline MA Method", "Type of moving average used against the Heiken Ashi candle.", "Filters");

		_buyFastPeriod = Param(nameof(BuyFastPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Buy Fast MA Period", "Length of the fast trend filter moving average.", "Filters");

		_buyFastMethod = Param(nameof(BuyFastMethod), MaMethod.Exponential)
		.SetDisplay("Buy Fast MA Method", "Moving average method for the fast trend filter.", "Filters");

		_buySlowPeriod = Param(nameof(BuySlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Buy Slow MA Period", "Length of the slow trend filter moving average.", "Filters");

		_buySlowMethod = Param(nameof(BuySlowMethod), MaMethod.Exponential)
		.SetDisplay("Buy Slow MA Method", "Moving average method for the slow trend filter.", "Filters");

		_buyPrimaryRsiPeriod = Param(nameof(BuyPrimaryRsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Buy Primary RSI Period", "Length of the first RSI window that must stay inside the limits.", "Filters");

		_buyPrimaryShift = Param(nameof(BuyPrimaryRsiShift), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Buy Primary RSI Shift", "Base shift applied before counting the checked candles.", "Filters");

		_buyPrimaryWindow = Param(nameof(BuyPrimaryRsiWindow), 2)
		.SetGreaterThanZero()
		.SetDisplay("Buy Primary RSI Window", "Number of candles checked by the primary RSI filter.", "Filters");

		_buyPrimaryExceptions = Param(nameof(BuyPrimaryRsiExceptions), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Buy Primary RSI Exceptions", "How many candles are allowed to violate the limits.", "Filters");

		_buyPrimaryUpper = Param(nameof(BuyPrimaryRsiUpper), 100m)
		.SetDisplay("Buy Primary RSI Upper", "Upper bound for the primary RSI filter.", "Filters");

		_buyPrimaryLower = Param(nameof(BuyPrimaryRsiLower), 0m)
		.SetDisplay("Buy Primary RSI Lower", "Lower bound for the primary RSI filter.", "Filters");

		_buySecondaryRsiPeriod = Param(nameof(BuySecondaryRsiPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Buy Secondary RSI Period", "Length of the second RSI window that must stay inside the limits.", "Filters");

		_buySecondaryShift = Param(nameof(BuySecondaryRsiShift), 2)
		.SetGreaterOrEqualZero()
		.SetDisplay("Buy Secondary RSI Shift", "Base shift applied before counting the checked candles for the second RSI.", "Filters");

		_buySecondaryWindow = Param(nameof(BuySecondaryRsiWindow), 3)
		.SetGreaterThanZero()
		.SetDisplay("Buy Secondary RSI Window", "Number of candles checked by the secondary RSI filter.", "Filters");

		_buySecondaryExceptions = Param(nameof(BuySecondaryRsiExceptions), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Buy Secondary RSI Exceptions", "How many candles may fall outside the secondary RSI bounds.", "Filters");

		_buySecondaryUpper = Param(nameof(BuySecondaryRsiUpper), 100m)
		.SetDisplay("Buy Secondary RSI Upper", "Upper bound for the secondary RSI filter.", "Filters");

		_buySecondaryLower = Param(nameof(BuySecondaryRsiLower), 0m)
		.SetDisplay("Buy Secondary RSI Lower", "Lower bound for the secondary RSI filter.", "Filters");

		_sellVolume = Param(nameof(SellVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Volume", "Position size for bearish signals (lots).", "Trading");

		_sellStopLossPips = Param(nameof(SellStopLossPips), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sell Stop Loss (pips)", "Protective stop distance for short trades.", "Risk");

		_sellTakeProfitPips = Param(nameof(SellTakeProfitPips), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sell Take Profit (pips)", "Profit target distance for short trades.", "Risk");

		_sellBaselinePeriod = Param(nameof(SellBaselinePeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Sell Baseline MA Period", "Length of the moving average compared with the bearish Heiken Ashi candle.", "Filters");

		_sellBaselineMethod = Param(nameof(SellBaselineMethod), MaMethod.Exponential)
		.SetDisplay("Sell Baseline MA Method", "Type of moving average used against the bearish Heiken Ashi candle.", "Filters");

		_sellFastPeriod = Param(nameof(SellFastPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Sell Fast MA Period", "Length of the fast trend filter moving average for shorts.", "Filters");

		_sellFastMethod = Param(nameof(SellFastMethod), MaMethod.Exponential)
		.SetDisplay("Sell Fast MA Method", "Moving average method for the fast bearish trend filter.", "Filters");

		_sellSlowPeriod = Param(nameof(SellSlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Sell Slow MA Period", "Length of the slow trend filter moving average for shorts.", "Filters");

		_sellSlowMethod = Param(nameof(SellSlowMethod), MaMethod.Exponential)
		.SetDisplay("Sell Slow MA Method", "Moving average method for the slow bearish trend filter.", "Filters");

		_sellPrimaryRsiPeriod = Param(nameof(SellPrimaryRsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Sell Primary RSI Period", "Length of the first RSI window used by the bearish setup.", "Filters");

		_sellPrimaryShift = Param(nameof(SellPrimaryRsiShift), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sell Primary RSI Shift", "Base shift applied before counting the checked candles for the first RSI.", "Filters");

		_sellPrimaryWindow = Param(nameof(SellPrimaryRsiWindow), 2)
		.SetGreaterThanZero()
		.SetDisplay("Sell Primary RSI Window", "Number of candles checked by the primary bearish RSI filter.", "Filters");

		_sellPrimaryExceptions = Param(nameof(SellPrimaryRsiExceptions), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sell Primary RSI Exceptions", "Allowed violations inside the primary bearish RSI window.", "Filters");

		_sellPrimaryUpper = Param(nameof(SellPrimaryRsiUpper), 100m)
		.SetDisplay("Sell Primary RSI Upper", "Upper bound for the primary bearish RSI filter.", "Filters");

		_sellPrimaryLower = Param(nameof(SellPrimaryRsiLower), 0m)
		.SetDisplay("Sell Primary RSI Lower", "Lower bound for the primary bearish RSI filter.", "Filters");

		_sellSecondaryRsiPeriod = Param(nameof(SellSecondaryRsiPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Sell Secondary RSI Period", "Length of the second RSI window used by the bearish setup.", "Filters");

		_sellSecondaryShift = Param(nameof(SellSecondaryRsiShift), 2)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sell Secondary RSI Shift", "Base shift applied before counting the checked candles for the second RSI.", "Filters");

		_sellSecondaryWindow = Param(nameof(SellSecondaryRsiWindow), 3)
		.SetGreaterThanZero()
		.SetDisplay("Sell Secondary RSI Window", "Number of candles checked by the secondary bearish RSI filter.", "Filters");

		_sellSecondaryExceptions = Param(nameof(SellSecondaryRsiExceptions), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sell Secondary RSI Exceptions", "Allowed violations inside the secondary bearish RSI window.", "Filters");

		_sellSecondaryUpper = Param(nameof(SellSecondaryRsiUpper), 100m)
		.SetDisplay("Sell Secondary RSI Upper", "Upper bound for the secondary bearish RSI filter.", "Filters");

		_sellSecondaryLower = Param(nameof(SellSecondaryRsiLower), 0m)
		.SetDisplay("Sell Secondary RSI Lower", "Lower bound for the secondary bearish RSI filter.", "Filters");

		_alertTitle = Param(nameof(AlertTitle), "Alert Message")
		.SetDisplay("Alert Title", "Text shown in the log when a trade is opened.", "Notifications");

		_sendNotification = Param(nameof(SendNotification), true)
		.SetDisplay("Send Notification", "Whether to create an info log entry on each new trade.", "Notifications");
	}

	/// <summary>
	/// Primary candle type used for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Direction handled by the strategy.
	/// </summary>
	public TradeDirectionOption Direction
	{
		get => _directionParam.Value;
		set => _directionParam.Value = value;
	}

	/// <summary>
	/// Buy position size in lots.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Buy stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public decimal BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Buy take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public decimal BuyTakeProfitPips
	{
		get => _buyTakeProfitPips.Value;
		set => _buyTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Sell position size in lots.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Sell stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public decimal SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Sell take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public decimal SellTakeProfitPips
	{
		get => _sellTakeProfitPips.Value;
		set => _sellTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Message written when a trade is opened.
	/// </summary>
	public string AlertTitle
	{
		get => _alertTitle.Value;
		set => _alertTitle.Value = value;
	}

	/// <summary>
	/// Enables trade notifications in the log.
	/// </summary>
	public bool SendNotification
	{
		get => _sendNotification.Value;
		set => _sendNotification.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyBaselineMa = null;
		_buyFastMa = null;
		_buySlowMa = null;
		_buyPrimaryRsi = null;
		_buySecondaryRsi = null;
		_sellBaselineMa = null;
		_sellFastMa = null;
		_sellSlowMa = null;
		_sellPrimaryRsi = null;
		_sellSecondaryRsi = null;

		_candles.Clear();
		_heiken.Clear();
		_buyBaselineValues.Clear();
		_buyFastValues.Clear();
		_buySlowValues.Clear();
		_buyPrimaryRsiValues.Clear();
		_buySecondaryRsiValues.Clear();
		_sellBaselineValues.Clear();
		_sellFastValues.Clear();
		_sellSlowValues.Clear();
		_sellPrimaryRsiValues.Clear();
		_sellSecondaryRsiValues.Clear();

		_pipSize = 0m;
		_previousHeiken = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_buyBaselineMa = CreateMovingAverage(BuyBaselineMethod, BuyBaselinePeriod);
		_buyFastMa = CreateMovingAverage(BuyFastMethod, BuyFastPeriod);
		_buySlowMa = CreateMovingAverage(BuySlowMethod, BuySlowPeriod);
		_buyPrimaryRsi = new RelativeStrengthIndex { Length = BuyPrimaryRsiPeriod, CandlePrice = CandlePrice.Close };
		_buySecondaryRsi = new RelativeStrengthIndex { Length = BuySecondaryRsiPeriod, CandlePrice = CandlePrice.Close };

		_sellBaselineMa = CreateMovingAverage(SellBaselineMethod, SellBaselinePeriod);
		_sellFastMa = CreateMovingAverage(SellFastMethod, SellFastPeriod);
		_sellSlowMa = CreateMovingAverage(SellSlowMethod, SellSlowPeriod);
		_sellPrimaryRsi = new RelativeStrengthIndex { Length = SellPrimaryRsiPeriod, CandlePrice = CandlePrice.Close };
		_sellSecondaryRsi = new RelativeStrengthIndex { Length = SellSecondaryRsiPeriod, CandlePrice = CandlePrice.Close };

		_pipSize = CalculatePipSize();

		StartProtection();

		SubscribeCandles(CandleType)
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var snapshot = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_candles.Add(snapshot);

		var heiken = ComputeHeikenAshi(candle);
		_heiken.Add(heiken);
		_previousHeiken = heiken;

		ProcessIndicator(_buyBaselineMa, candle, _buyBaselineValues);
		ProcessIndicator(_buyFastMa, candle, _buyFastValues);
		ProcessIndicator(_buySlowMa, candle, _buySlowValues);
		ProcessIndicator(_buyPrimaryRsi, candle, _buyPrimaryRsiValues);
		ProcessIndicator(_buySecondaryRsi, candle, _buySecondaryRsiValues);
		ProcessIndicator(_sellBaselineMa, candle, _sellBaselineValues);
		ProcessIndicator(_sellFastMa, candle, _sellFastValues);
		ProcessIndicator(_sellSlowMa, candle, _sellSlowValues);
		ProcessIndicator(_sellPrimaryRsi, candle, _sellPrimaryRsiValues);
		ProcessIndicator(_sellSecondaryRsi, candle, _sellSecondaryRsiValues);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Direction != TradeDirectionOption.SellOnly && TryEnterLong(candle))
		return;

		if (Direction != TradeDirectionOption.BuyOnly)
		TryEnterShort(candle);
	}

	private bool TryEnterLong(ICandleMessage candle)
	{
		if (Position != 0m)
		return false;

		if (!_buyBaselineMa?.IsFormed ?? true)
		return false;

		if (!_buyFastMa?.IsFormed ?? true)
		return false;

		if (!_buySlowMa?.IsFormed ?? true)
		return false;

		if (!_buyPrimaryRsi?.IsFormed ?? true)
		return false;

		if (!_buySecondaryRsi?.IsFormed ?? true)
		return false;

		var haCurrent = GetHeiken(1);
		var haPrevious = GetHeiken(2);
		if (haCurrent == null || haPrevious == null)
		return false;

		if (!(haCurrent.Value.Close > haCurrent.Value.Open))
		return false;

		if (!(haPrevious.Value.Close < haPrevious.Value.Open))
		return false;

		var candleCurrent = GetCandle(1);
		var candlePrevious = GetCandle(2);
		if (candleCurrent == null || candlePrevious == null)
		return false;

		if (!(candleCurrent.Value.Close > candlePrevious.Value.High))
		return false;

		if (!(candlePrevious.Value.Close < candlePrevious.Value.Open))
		return false;

		var baseline = GetIndicatorValue(_buyBaselineValues, BuyBaselineShiftTotal);
		if (baseline == null)
		return false;

		if (!(haCurrent.Value.Close > baseline.Value))
		return false;

		var fast = GetIndicatorValue(_buyFastValues, BuyTrendShiftTotal);
		var slow = GetIndicatorValue(_buySlowValues, BuyTrendShiftTotal);
		if (fast == null || slow == null)
		return false;

		if (!(fast.Value > slow.Value))
		return false;

		if (!CheckIndicatorWithinLimits(_buyPrimaryRsiValues, BuyPrimaryShift, BuyPrimaryWindow, BuyPrimaryExceptions, BuyPrimaryUpper, BuyPrimaryLower))
		return false;

		if (!CheckIndicatorWithinLimits(_buySecondaryRsiValues, BuySecondaryShift, BuySecondaryWindow, BuySecondaryExceptions, BuySecondaryUpper, BuySecondaryLower))
		return false;

		var volume = NormalizeVolume(BuyVolume);
		if (volume <= 0m)
		return false;

		BuyMarket(volume);

		ApplyProtection(true, candle.ClosePrice, volume);

		if (SendNotification)
		LogInfo($"{AlertTitle}: buy {volume} at {candle.ClosePrice:0.#####}");

		return true;
	}

	private bool TryEnterShort(ICandleMessage candle)
	{
		if (Position != 0m)
		return false;

		if (!_sellBaselineMa?.IsFormed ?? true)
		return false;

		if (!_sellFastMa?.IsFormed ?? true)
		return false;

		if (!_sellSlowMa?.IsFormed ?? true)
		return false;

		if (!_sellPrimaryRsi?.IsFormed ?? true)
		return false;

		if (!_sellSecondaryRsi?.IsFormed ?? true)
		return false;

		var haCurrent = GetHeiken(1);
		var haPrevious = GetHeiken(2);
		if (haCurrent == null || haPrevious == null)
		return false;

		if (!(haCurrent.Value.Close < haCurrent.Value.Open))
		return false;

		if (!(haPrevious.Value.Close > haPrevious.Value.Open))
		return false;

		var candleCurrent = GetCandle(1);
		var candlePrevious = GetCandle(2);
		if (candleCurrent == null || candlePrevious == null)
		return false;

		if (!(candleCurrent.Value.Close < candlePrevious.Value.Low))
		return false;

		if (!(candlePrevious.Value.Close > candlePrevious.Value.Open))
		return false;

		var baseline = GetIndicatorValue(_sellBaselineValues, SellBaselineShiftTotal);
		if (baseline == null)
		return false;

		if (!(haCurrent.Value.Close < baseline.Value))
		return false;

		var fast = GetIndicatorValue(_sellFastValues, SellTrendShiftTotal);
		var slow = GetIndicatorValue(_sellSlowValues, SellTrendShiftTotal);
		if (fast == null || slow == null)
		return false;

		if (!(fast.Value < slow.Value))
		return false;

		if (!CheckIndicatorWithinLimits(_sellPrimaryRsiValues, SellPrimaryShift, SellPrimaryWindow, SellPrimaryExceptions, SellPrimaryUpper, SellPrimaryLower))
		return false;

		if (!CheckIndicatorWithinLimits(_sellSecondaryRsiValues, SellSecondaryShift, SellSecondaryWindow, SellSecondaryExceptions, SellSecondaryUpper, SellSecondaryLower))
		return false;

		var volume = NormalizeVolume(SellVolume);
		if (volume <= 0m)
		return false;

		SellMarket(volume);

		ApplyProtection(false, candle.ClosePrice, volume);

		if (SendNotification)
		LogInfo($"{AlertTitle}: sell {volume} at {candle.ClosePrice:0.#####}");

		return true;
	}

	private void ApplyProtection(bool isLong, decimal referencePrice, decimal volume)
	{
		var stopDistance = ConvertPipsToPrice(isLong ? BuyStopLossPips : SellStopLossPips);
		var takeDistance = ConvertPipsToPrice(isLong ? BuyTakeProfitPips : SellTakeProfitPips);
		var resultingPosition = isLong ? Position + volume : Position - volume;

		if (stopDistance > 0m)
		SetStopLoss(stopDistance, referencePrice, resultingPosition);

		if (takeDistance > 0m)
		SetTakeProfit(takeDistance, referencePrice, resultingPosition);
	}

	private static void ProcessIndicator(IIndicator? indicator, ICandleMessage candle, List<decimal?> storage)
	{
		if (indicator == null)
		{
			storage.Add(null);
			return;
		}

		var value = indicator.Process(candle);
		storage.Add(value.IsFinal ? value.ToNullableDecimal() : null);
	}

	private HeikenAshiSnapshot ComputeHeikenAshi(ICandleMessage candle)
	{
		if (_previousHeiken == null)
		{
			var initialClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
			var initialOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			var initialHigh = Math.Max(candle.HighPrice, Math.Max(initialOpen, initialClose));
			var initialLow = Math.Min(candle.LowPrice, Math.Min(initialOpen, initialClose));
			return new HeikenAshiSnapshot(initialOpen, initialHigh, initialLow, initialClose);
		}

		var haOpen = (_previousHeiken.Value.Open + _previousHeiken.Value.Close) / 2m;
		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
		return new HeikenAshiSnapshot(haOpen, haHigh, haLow, haClose);
	}

	private HeikenAshiSnapshot? GetHeiken(int totalShift)
	{
		var index = GetEffectiveIndex(_heiken.Count, totalShift);
		return index >= 0 ? _heiken[index] : null;
	}

	private CandleSnapshot? GetCandle(int totalShift)
	{
		var index = GetEffectiveIndex(_candles.Count, totalShift);
		return index >= 0 ? _candles[index] : null;
	}

	private static int GetEffectiveIndex(int count, int totalShift)
	{
		if (totalShift <= 0)
		return -1;

		var effectiveShift = totalShift - 1;
		var index = count - 1 - effectiveShift;
		return index >= 0 && index < count ? index : -1;
	}

	private static decimal? GetIndicatorValue(IReadOnlyList<decimal?> values, int totalShift)
	{
		var index = GetEffectiveIndex(values.Count, totalShift);
		return index >= 0 ? values[index] : null;
	}

	private bool CheckIndicatorWithinLimits(IReadOnlyList<decimal?> values, int candlesShift, int candlesPeriod, int exceptions, decimal upper, decimal lower)
	{
		var start = candlesShift;
		var end = candlesShift + candlesPeriod - 1;
		var fails = 0;

		for (var i = start; i <= end; i++)
		{
			var totalShift = 1 + i;
			var value = GetIndicatorValue(values, totalShift);
			if (value == null)
			return false;

			if (value.Value > upper || value.Value < lower)
			{
				if (exceptions <= 0)
				return false;

				fails++;
				if (fails > exceptions)
				return false;
			}
		}

		return true;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
		return 0m;

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 1m;

		var step = security.PriceStep ?? 1m;
		var multiplier = security.Decimals is 3 or 5 ? 10m : 1m;
		return step * multiplier;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int period)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = period, CandlePrice = CandlePrice.Close },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = period, CandlePrice = CandlePrice.Close },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = period, CandlePrice = CandlePrice.Close },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = period, CandlePrice = CandlePrice.Close },
			_ => new ExponentialMovingAverage { Length = period, CandlePrice = CandlePrice.Close }
		};
	}

	private int BuyBaselinePeriod => _buyBaselinePeriod.Value;
	private MaMethod BuyBaselineMethod => _buyBaselineMethod.Value;
	private int BuyFastPeriod => _buyFastPeriod.Value;
	private MaMethod BuyFastMethod => _buyFastMethod.Value;
	private int BuySlowPeriod => _buySlowPeriod.Value;
	private MaMethod BuySlowMethod => _buySlowMethod.Value;
	private int BuyPrimaryRsiPeriod => _buyPrimaryRsiPeriod.Value;
	private int BuyPrimaryShift => _buyPrimaryShift.Value;
	private int BuyPrimaryWindow => _buyPrimaryWindow.Value;
	private int BuyPrimaryExceptions => _buyPrimaryExceptions.Value;
	private decimal BuyPrimaryUpper => _buyPrimaryUpper.Value;
	private decimal BuyPrimaryLower => _buyPrimaryLower.Value;
	private int BuySecondaryRsiPeriod => _buySecondaryRsiPeriod.Value;
	private int BuySecondaryShift => _buySecondaryShift.Value;
	private int BuySecondaryWindow => _buySecondaryWindow.Value;
	private int BuySecondaryExceptions => _buySecondaryExceptions.Value;
	private decimal BuySecondaryUpper => _buySecondaryUpper.Value;
	private decimal BuySecondaryLower => _buySecondaryLower.Value;
	private int SellBaselinePeriod => _sellBaselinePeriod.Value;
	private MaMethod SellBaselineMethod => _sellBaselineMethod.Value;
	private int SellFastPeriod => _sellFastPeriod.Value;
	private MaMethod SellFastMethod => _sellFastMethod.Value;
	private int SellSlowPeriod => _sellSlowPeriod.Value;
	private MaMethod SellSlowMethod => _sellSlowMethod.Value;
	private int SellPrimaryRsiPeriod => _sellPrimaryRsiPeriod.Value;
	private int SellPrimaryShift => _sellPrimaryShift.Value;
	private int SellPrimaryWindow => _sellPrimaryWindow.Value;
	private int SellPrimaryExceptions => _sellPrimaryExceptions.Value;
	private decimal SellPrimaryUpper => _sellPrimaryUpper.Value;
	private decimal SellPrimaryLower => _sellPrimaryLower.Value;
	private int SellSecondaryRsiPeriod => _sellSecondaryRsiPeriod.Value;
	private int SellSecondaryShift => _sellSecondaryShift.Value;
	private int SellSecondaryWindow => _sellSecondaryWindow.Value;
	private int SellSecondaryExceptions => _sellSecondaryExceptions.Value;
	private decimal SellSecondaryUpper => _sellSecondaryUpper.Value;
	private decimal SellSecondaryLower => _sellSecondaryLower.Value;

	private int BuyBaselineShiftTotal => 1;
	private int BuyTrendShiftTotal => 1;
	private int SellBaselineShiftTotal => 1;
	private int SellTrendShiftTotal => 1;

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	private readonly struct HeikenAshiSnapshot
	{
		public HeikenAshiSnapshot(decimal open, decimal high, decimal low, decimal close)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	/// <summary>
	/// Moving average methods supported by MetaTrader.
	/// </summary>
	public enum MaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	/// <summary>
	/// Trade direction options.
	/// </summary>
	public enum TradeDirectionOption
	{
		BuyOnly,
		SellOnly,
		Both
	}
}
