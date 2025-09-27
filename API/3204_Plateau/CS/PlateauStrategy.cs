using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Money management mode for Plateau strategy.
/// </summary>
public enum PlateauMoneyManagementMode
{
	/// <summary>Use fixed lot size for every order.</summary>
	FixedLot,

	/// <summary>Risk a percentage of the account per trade.</summary>
	RiskPercent,
}

/// <summary>
/// Price source used for moving averages and Bollinger Bands.
/// Matches the input price options from the original MQL5 expert.
/// </summary>
public enum PlateauAppliedPrice
{
	/// <summary>Close price of the candle.</summary>
	Close,

	/// <summary>Open price of the candle.</summary>
	Open,

	/// <summary>High price of the candle.</summary>
	High,

	/// <summary>Low price of the candle.</summary>
	Low,

	/// <summary>Median price (high + low) / 2.</summary>
	Median,

	/// <summary>Typical price (high + low + close) / 3.</summary>
	Typical,

	/// <summary>Weighted price (high + low + close + close) / 4.</summary>
	Weighted,
}

/// <summary>
/// Moving average method equivalent to the MQL5 implementation.
/// </summary>
public enum PlateauMovingAverageMethod
{
	/// <summary>Simple moving average.</summary>
	Simple,

	/// <summary>Exponential moving average.</summary>
	Exponential,

	/// <summary>Smoothed moving average.</summary>
	Smoothed,

	/// <summary>Linear weighted moving average.</summary>
	LinearWeighted,
}

/// <summary>
/// Converted version of the Plateau expert advisor.
/// The strategy monitors fast and slow moving averages together with the lower Bollinger Band.
/// When a bullish crossover occurs below the lower band a long position is opened, while a bearish crossover above the lower band triggers a short entry.
/// Optional stop loss, take profit and trailing stop are applied in pips, and position sizing can rely on fixed lots or risk percentage.
/// </summary>
public class PlateauStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<PlateauMoneyManagementMode> _moneyMode;
	private readonly StrategyParam<decimal> _moneyValue;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<PlateauMovingAverageMethod> _maMethod;
	private readonly StrategyParam<PlateauAppliedPrice> _maAppliedPrice;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<int> _bandsShift;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<PlateauAppliedPrice> _bandsAppliedPrice;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _printLog;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _historyCapacity;

	private IIndicator _fastMa = null!;
	private IIndicator _slowMa = null!;
	private BollingerBands _bollinger = null!;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();
	private readonly List<decimal> _lowerBandHistory = new();
	private readonly List<decimal> _closeHistory = new();

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	private decimal? _entryPrice;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Minimal trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Money management mode (fixed lot or risk percentage).
	/// </summary>
	public PlateauMoneyManagementMode MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Fixed lot size or risk percentage depending on <see cref="MoneyMode"/>.
	/// </summary>
	public decimal MoneyValue
	{
		get => _moneyValue.Value;
		set => _moneyValue.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to both moving averages.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Method used to calculate the moving averages.
	/// </summary>
	public PlateauMovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the moving averages.
	/// </summary>
	public PlateauAppliedPrice MaAppliedPrice
	{
		get => _maAppliedPrice.Value;
		set => _maAppliedPrice.Value = value;
	}

	/// <summary>
	/// Bollinger Bands averaging period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to Bollinger Bands values.
	/// </summary>
	public int BandsShift
	{
		get => _bandsShift.Value;
		set => _bandsShift.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	/// <summary>
	/// Applied price used for Bollinger Bands calculations.
	/// </summary>
	public PlateauAppliedPrice BandsAppliedPrice
	{
		get => _bandsAppliedPrice.Value;
		set => _bandsAppliedPrice.Value = value;
	}

	/// <summary>
	/// Reverse signal logic flag.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close opposite positions before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Enable verbose logging similar to the original script.
	/// </summary>
	public bool PrintLog
	{
		get => _printLog.Value;
		set => _printLog.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum number of historical indicator values retained for signal confirmation.
	/// </summary>
	public int HistoryCapacity
	{
		get => _historyCapacity.Value;
		set => _historyCapacity.Value = value;
	}

	/// <summary>
	/// Create Plateau strategy with default parameters matching the original expert.
	/// </summary>
	public PlateauStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 140m)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step", "Minimal trailing step in pips", "Risk")
			.SetCanOptimize(true);

		_moneyMode = Param(nameof(MoneyMode), PlateauMoneyManagementMode.RiskPercent)
			.SetDisplay("Money Mode", "Choose between fixed lot or risk percent", "Money Management");

		_moneyValue = Param(nameof(MoneyValue), 3m)
			.SetDisplay("Lot / Risk", "Fixed lot when Money Mode=FixedLot or risk percent when Money Mode=RiskPercent", "Money Management")
			.SetCanOptimize(true);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 9)
			.SetDisplay("Fast MA", "Fast moving average period", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 24)
			.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 0)
			.SetDisplay("MA Shift", "Horizontal shift applied to moving averages", "Indicators");

		_maMethod = Param(nameof(MaMethod), PlateauMovingAverageMethod.LinearWeighted)
			.SetDisplay("MA Method", "Moving average smoothing method", "Indicators");

		_maAppliedPrice = Param(nameof(MaAppliedPrice), PlateauAppliedPrice.Typical)
			.SetDisplay("MA Price", "Applied price for moving averages", "Indicators");

		_bandsPeriod = Param(nameof(BandsPeriod), 150)
			.SetDisplay("Bands Period", "Bollinger Bands averaging period", "Indicators")
			.SetCanOptimize(true);

		_bandsShift = Param(nameof(BandsShift), 0)
			.SetDisplay("Bands Shift", "Horizontal shift applied to Bollinger Bands", "Indicators");

		_bandsDeviation = Param(nameof(BandsDeviation), 1m)
			.SetDisplay("Bands Deviation", "Bollinger Bands deviation multiplier", "Indicators")
			.SetCanOptimize(true);

		_bandsAppliedPrice = Param(nameof(BandsAppliedPrice), PlateauAppliedPrice.Typical)
			.SetDisplay("Bands Price", "Applied price for Bollinger Bands", "Indicators");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse", "Invert trading signals", "General");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close Opposite", "Close opposite exposure before opening a new trade", "General");

		_printLog = Param(nameof(PrintLog), false)
			.SetDisplay("Verbose Log", "Print diagnostic messages", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Data series used for calculations", "General");
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

		_fastHistory.Clear();
		_slowHistory.Clear();
		_lowerBandHistory.Clear();
		_closeHistory.Clear();

		_entryPrice = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_fastMa = CreateMovingAverage(MaMethod, FastMaPeriod);
		_slowMa = CreateMovingAverage(MaMethod, SlowMaPeriod);
		_bollinger = new BollingerBands
		{
			Length = BandsPeriod,
			Width = BandsDeviation
		};

		_pipSize = CalculatePipSize();
		_stopLossOffset = StopLossPips * _pipSize;
		_takeProfitOffset = TakeProfitPips * _pipSize;
		_trailingStopOffset = TrailingStopPips * _pipSize;
		_trailingStepOffset = TrailingStepPips * _pipSize;

		if (MoneyMode == PlateauMoneyManagementMode.FixedLot && MoneyValue > 0m)
			Volume = MoneyValue;

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_fastMa is IIndicator fastIndicator)
				DrawIndicator(area, fastIndicator);
			if (_slowMa is IIndicator slowIndicator)
				DrawIndicator(area, slowIndicator);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage existing protection before looking for new opportunities.
		ManageActivePosition(candle);

		var maInput = GetAppliedPrice(candle, MaAppliedPrice);
		var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, maInput, candle.OpenTime)).ToNullableDecimal();
		var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, maInput, candle.OpenTime)).ToNullableDecimal();

		var bandsInput = GetAppliedPrice(candle, BandsAppliedPrice);
		var bandValue = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, bandsInput, candle.OpenTime));

		if (fastValue is not decimal fast || slowValue is not decimal slow)
		{
			UpdateHistory(fastValue, slowValue, bandValue.LowBand, candle.ClosePrice);
			return;
		}

		if (bandValue.LowBand is not decimal lowerBand)
		{
			UpdateHistory(fast, slow, bandValue.LowBand, candle.ClosePrice);
			return;
		}

		UpdateHistory(fast, slow, lowerBand, candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var (buySignal, sellSignal) = EvaluateSignals();

		if (ReverseSignals)
		{
			(buySignal, sellSignal) = (sellSignal, buySignal);
		}

		if (buySignal)
			TryEnterLong(candle.ClosePrice);
		else if (sellSignal)
			TryEnterShort(candle.ClosePrice);
	}

	private (bool buy, bool sell) EvaluateSignals()
	{
	var fastPrev1 = GetHistoryValue(_fastHistory, 1 + MaShift);
	var fastPrev2 = GetHistoryValue(_fastHistory, 2 + MaShift);
	var slowPrev1 = GetHistoryValue(_slowHistory, 1 + MaShift);
	var slowPrev2 = GetHistoryValue(_slowHistory, 2 + MaShift);
	var lowerPrev1 = GetHistoryValue(_lowerBandHistory, 1 + BandsShift);
	var closePrev1 = GetHistoryValue(_closeHistory, 1);

	if (fastPrev1 is null || fastPrev2 is null || slowPrev1 is null || slowPrev2 is null || lowerPrev1 is null || closePrev1 is null)
	return (false, false);

	var buySignal = fastPrev2 < slowPrev2 && fastPrev1 > slowPrev1 && closePrev1 < lowerPrev1;
	var sellSignal = fastPrev2 > slowPrev2 && fastPrev1 < slowPrev1 && closePrev1 > lowerPrev1;

	return (buySignal, sellSignal);
	}

	private void TryEnterLong(decimal entryPrice)
	{
	if (Position > 0m)
	return;

	var volume = CalculateOrderVolume();
	if (volume <= 0m)
	return;

	if (CloseOpposite && Position < 0m)
	BuyMarket(-Position);

	BuyMarket(volume);
	SetProtectionLevels(entryPrice, true);
	}

	private void TryEnterShort(decimal entryPrice)
	{
	if (Position < 0m)
	return;

	var volume = CalculateOrderVolume();
	if (volume <= 0m)
	return;

	if (CloseOpposite && Position > 0m)
	SellMarket(Position);

	SellMarket(volume);
	SetProtectionLevels(entryPrice, false);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
	if (Position > 0m)
	{
	if (_activeStopPrice is decimal stop && candle.LowPrice <= stop)
	{
	SellMarket(Position);
	ResetProtection();
	return;
	}

	if (_activeTakePrice is decimal take && candle.HighPrice >= take)
	{
	SellMarket(Position);
	ResetProtection();
	return;
	}

	UpdateTrailingForLong(candle.ClosePrice);
	}
	else if (Position < 0m)
	{
	var volume = Math.Abs(Position);

	if (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
	{
	BuyMarket(volume);
	ResetProtection();
	return;
	}

	if (_activeTakePrice is decimal take && candle.LowPrice <= take)
	{
	BuyMarket(volume);
	ResetProtection();
	return;
	}

	UpdateTrailingForShort(candle.ClosePrice);
	}
	else
	{
	ResetProtection();
	}
	}

	private void UpdateTrailingForLong(decimal price)
	{
	if (_entryPrice is not decimal entry || _trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
	return;

	var distance = price - entry;
	if (distance <= _trailingStopOffset + _trailingStepOffset)
	return;

	var newStop = price - _trailingStopOffset;
	if (_activeStopPrice is decimal currentStop && newStop - currentStop < _trailingStepOffset)
	return;

	_activeStopPrice = newStop;

	if (PrintLog)
	LogInfo($"Trailing long stop adjusted to {newStop:0.#####}");
	}

	private void UpdateTrailingForShort(decimal price)
	{
	if (_entryPrice is not decimal entry || _trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
	return;

	var distance = entry - price;
	if (distance <= _trailingStopOffset + _trailingStepOffset)
	return;

	var newStop = price + _trailingStopOffset;
	if (_activeStopPrice is decimal currentStop && currentStop - newStop < _trailingStepOffset)
	return;

	_activeStopPrice = newStop;

	if (PrintLog)
	LogInfo($"Trailing short stop adjusted to {newStop:0.#####}");
	}

	private void SetProtectionLevels(decimal entryPrice, bool isLong)
	{
	_entryPrice = entryPrice;

	if (isLong)
	{
	_activeStopPrice = _stopLossOffset > 0m ? entryPrice - _stopLossOffset : null;
	_activeTakePrice = _takeProfitOffset > 0m ? entryPrice + _takeProfitOffset : null;
	}
	else
	{
	_activeStopPrice = _stopLossOffset > 0m ? entryPrice + _stopLossOffset : null;
	_activeTakePrice = _takeProfitOffset > 0m ? entryPrice - _takeProfitOffset : null;
	}

	if (PrintLog)
	LogInfo($"Entry at {entryPrice:0.#####}, stop={_activeStopPrice?.ToString("0.#####") ?? "n/a"}, take={_activeTakePrice?.ToString("0.#####") ?? "n/a"}");
	}

	private void ResetProtection()
	{
	_entryPrice = null;
	_activeStopPrice = null;
	_activeTakePrice = null;
	}

	private decimal CalculateOrderVolume()
	{
	if (MoneyMode == PlateauMoneyManagementMode.FixedLot)
	return MoneyValue;

	if (MoneyMode != PlateauMoneyManagementMode.RiskPercent)
	return Volume;

	if (MoneyValue <= 0m || _stopLossOffset <= 0m)
	return Volume;

	var portfolio = Portfolio;
	if (portfolio is null)
	return Volume;

	var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
	if (equity <= 0m)
	return Volume;

	var priceStep = Security?.PriceStep ?? 0m;
	var stepPrice = Security?.StepPrice ?? 0m;

	decimal perUnitRisk;
	if (priceStep > 0m && stepPrice > 0m)
	{
	perUnitRisk = _stopLossOffset / priceStep * stepPrice;
	}
	else
	{
	perUnitRisk = _stopLossOffset;
	}

	if (perUnitRisk <= 0m)
	return Volume;

	var riskAmount = equity * MoneyValue / 100m;
	if (riskAmount <= 0m)
	return Volume;

	var rawVolume = riskAmount / perUnitRisk;
	var volumeStep = Security?.VolumeStep ?? 0m;

	if (volumeStep > 0m)
	{
	var steps = Math.Max(1m, Math.Floor(rawVolume / volumeStep));
	return steps * volumeStep;
	}

	return Math.Max(rawVolume, 0m);
	}

	private void UpdateHistory(decimal? fast, decimal? slow, decimal? lowerBand, decimal closePrice)
	{
	void AddValue(List<decimal> list, decimal? value)
	{
	if (value is not decimal decimalValue)
	return;

	list.Insert(0, decimalValue);
	if (list.Count > HistoryCapacity)
	list.RemoveAt(list.Count - 1);
	}

	AddValue(_fastHistory, fast);
	AddValue(_slowHistory, slow);
	AddValue(_lowerBandHistory, lowerBand);

	_closeHistory.Insert(0, closePrice);
	if (_closeHistory.Count > HistoryCapacity)
	_closeHistory.RemoveAt(_closeHistory.Count - 1);
	}

	private static decimal? GetHistoryValue(List<decimal> list, int index)
	{
	if (index < 0 || index >= list.Count)
	return null;

	return list[index];
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	return 0.0001m;

	var decimals = GetDecimalPlaces(step);
	if (decimals == 3 || decimals == 5)
	return step * 10m;

	return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
	var bits = decimal.GetBits(value);
	return (bits[3] >> 16) & 0xFF;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, PlateauAppliedPrice price)
	{
	return price switch
	{
	PlateauAppliedPrice.Close => candle.ClosePrice,
	PlateauAppliedPrice.Open => candle.OpenPrice,
	PlateauAppliedPrice.High => candle.HighPrice,
	PlateauAppliedPrice.Low => candle.LowPrice,
	PlateauAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
	PlateauAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
	PlateauAppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
	_ => candle.ClosePrice,
	};
	}

	private static IIndicator CreateMovingAverage(PlateauMovingAverageMethod method, int period)
	{
	return method switch
	{
	PlateauMovingAverageMethod.Simple => new SimpleMovingAverage { Length = Math.Max(1, period) },
	PlateauMovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = Math.Max(1, period) },
	PlateauMovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = Math.Max(1, period) },
	PlateauMovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = Math.Max(1, period) },
	_ => new WeightedMovingAverage { Length = Math.Max(1, period) },
	};
	}
}
