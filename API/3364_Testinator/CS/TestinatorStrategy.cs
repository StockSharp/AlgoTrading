using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor "Testinator v1.30a".
/// Builds a configurable binary rule set that combines up to nine indicators
/// for entries and exits, manages a basket of long positions, and applies ATR based
/// protective levels together with an optional trailing stop.
/// </summary>
public class TestinatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _buySequence;
	private readonly StrategyParam<int> _closeBuySequence;
	private readonly StrategyParam<int> _maxBuys;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<int> _tradeStartHour;
	private readonly StrategyParam<int> _tradeDurationHours;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _startTrailPips;
	private readonly StrategyParam<decimal> _trailStepPips;
	private readonly StrategyParam<decimal> _takeRatio;
	private readonly StrategyParam<decimal> _stopRatio;
	private readonly StrategyParam<decimal> _startTrailRatio;
	private readonly StrategyParam<decimal> _trailStepRatio;
	private readonly StrategyParam<decimal> _rsiEntryLevel;
	private readonly StrategyParam<int> _rsiEntryPeriod;
	private readonly StrategyParam<decimal> _rsiCloseLevel;
	private readonly StrategyParam<int> _rsiClosePeriod;
	private readonly StrategyParam<int> _bollingerCloseLength;
	private readonly StrategyParam<decimal> _bollingerCloseDeviation;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private ExponentialMovingAverage _emaShort = null!;
	private ExponentialMovingAverage _emaLong = null!;
	private BollingerBands _entryBands = null!;
	private BollingerBands _closeBands = null!;
	private AverageDirectionalIndex _adx = null!;
	private StochasticOscillator _stochastic = null!;
	private WilliamsR _williams = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private IchimokuKinkoHyo _ichimoku = null!;
	private RelativeStrengthIndex _rsiEntry = null!;
	private RelativeStrengthIndex _rsiClose = null!;
	private AverageTrueRange _dailyAtr = null!;

	private decimal _pipSize;
	private decimal _currentVolume;
	private decimal _averageEntryPrice;
	private decimal? _lastEntryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _dailyAtrPips;

	private readonly List<ICandleMessage> _recentCandles = new();
	private readonly List<BollingerSnapshot> _entryBandHistory = new();
	private readonly List<BollingerSnapshot> _closeBandHistory = new();
	private readonly List<decimal> _rsiEntryHistory = new();
	private readonly List<decimal> _rsiCloseHistory = new();

	private record struct BollingerSnapshot(decimal? Middle, decimal? Upper, decimal? Lower);

	/// <summary>
	/// Trading volume for every market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Bit mask describing which entry tests must succeed.
	/// </summary>
	public int BuySequence
	{
		get => _buySequence.Value;
		set => _buySequence.Value = value;
	}

	/// <summary>
	/// Bit mask describing which exit tests must succeed.
	/// </summary>
	public int CloseBuySequence
	{
		get => _closeBuySequence.Value;
		set => _closeBuySequence.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open buy trades.
	/// </summary>
	public int MaxBuys
	{
		get => _maxBuys.Value;
		set => _maxBuys.Value = value;
	}

	/// <summary>
	/// Minimum distance between consecutive buys expressed in pips.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Eastern European Time hour when trading becomes active.
	/// </summary>
	public int TradeStartHour
	{
		get => _tradeStartHour.Value;
		set => _tradeStartHour.Value = value;
	}

	/// <summary>
	/// Duration of the trading window in hours.
	/// </summary>
	public int TradeDurationHours
	{
		get => _tradeDurationHours.Value;
		set => _tradeDurationHours.Value = value;
	}

	/// <summary>
	/// Fixed take profit expressed in pips (-1 disables, 0 uses ATR ratio).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Fixed stop loss expressed in pips (-1 disables, 0 uses ATR ratio).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Price move in pips required before the trailing stop activates (-1 disables, 0 uses ATR ratio).
	/// </summary>
	public decimal StartTrailPips
	{
		get => _startTrailPips.Value;
		set => _startTrailPips.Value = value;
	}

	/// <summary>
	/// Trailing step size in pips (-1 disables, 0 uses ATR ratio).
	/// </summary>
	public decimal TrailStepPips
	{
		get => _trailStepPips.Value;
		set => _trailStepPips.Value = value;
	}

	/// <summary>
	/// Ratio of the daily ATR used when the fixed take profit is zero.
	/// </summary>
	public decimal TakeRatio
	{
		get => _takeRatio.Value;
		set => _takeRatio.Value = value;
	}

	/// <summary>
	/// Ratio of the daily ATR used when the fixed stop loss is zero.
	/// </summary>
	public decimal StopRatio
	{
		get => _stopRatio.Value;
		set => _stopRatio.Value = value;
	}

	/// <summary>
	/// Ratio of the daily ATR used when the trailing start is zero.
	/// </summary>
	public decimal StartTrailRatio
	{
		get => _startTrailRatio.Value;
		set => _startTrailRatio.Value = value;
	}

	/// <summary>
	/// Ratio of the daily ATR used when the trailing step is zero.
	/// </summary>
	public decimal TrailStepRatio
	{
		get => _trailStepRatio.Value;
		set => _trailStepRatio.Value = value;
	}

	/// <summary>
	/// RSI threshold used by entry test 256.
	/// </summary>
	public decimal RsiEntryLevel
	{
		get => _rsiEntryLevel.Value;
		set => _rsiEntryLevel.Value = value;
	}

	/// <summary>
	/// RSI period for entry calculations.
	/// </summary>
	public int RsiEntryPeriod
	{
		get => _rsiEntryPeriod.Value;
		set => _rsiEntryPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold used by exit test 256.
	/// </summary>
	public decimal RsiCloseLevel
	{
		get => _rsiCloseLevel.Value;
		set => _rsiCloseLevel.Value = value;
	}

	/// <summary>
	/// RSI period for exit calculations.
	/// </summary>
	public int RsiClosePeriod
	{
		get => _rsiClosePeriod.Value;
		set => _rsiClosePeriod.Value = value;
	}

	/// <summary>
	/// Bollinger period used by exit test 4.
	/// </summary>
	public int BollingerCloseLength
	{
		get => _bollingerCloseLength.Value;
		set => _bollingerCloseLength.Value = value;
	}

	/// <summary>
	/// Bollinger deviation used by exit test 4.
	/// </summary>
	public decimal BollingerCloseDeviation
	{
		get => _bollingerCloseDeviation.Value;
		set => _bollingerCloseDeviation.Value = value;
	}

	/// <summary>
	/// Candle type used for the primary calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TestinatorStrategy"/>.
	/// </summary>
	public TestinatorStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume used for each market order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_buySequence = Param(nameof(BuySequence), 256)
		.SetDisplay("Buy Sequence", "Bit mask of enabled entry checks", "Logic");

		_closeBuySequence = Param(nameof(CloseBuySequence), 276)
		.SetDisplay("Close Sequence", "Bit mask of enabled exit checks", "Logic");

		_maxBuys = Param(nameof(MaxBuys), 3)
		.SetGreaterThanZero()
		.SetDisplay("Max Buys", "Maximum number of concurrent buy trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1, 6, 1);

		_stepPips = Param(nameof(StepPips), 15)
		.SetNotNegative()
		.SetDisplay("Step (pips)", "Minimum spacing between sequential buys", "Risk");

		_tradeStartHour = Param(nameof(TradeStartHour), 16)
		.SetDisplay("Trading start hour", "Session opening hour in EET", "Session")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_tradeDurationHours = Param(nameof(TradeDurationHours), 2)
		.SetGreaterThanZero()
		.SetDisplay("Trading duration", "Session duration in hours", "Session")
		.SetCanOptimize(true)
		.SetOptimize(1, 12, 1);

		_takeProfitPips = Param(nameof(TakeProfitPips), -1m)
		.SetDisplay("Take profit (pips)", "Fixed take profit in pips (-1 disables, 0 uses ATR ratio)", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), -1m)
		.SetDisplay("Stop loss (pips)", "Fixed stop loss in pips (-1 disables, 0 uses ATR ratio)", "Risk");

		_startTrailPips = Param(nameof(StartTrailPips), -1m)
		.SetDisplay("Trail start (pips)", "Price move in pips before the trailing stop activates", "Risk");

		_trailStepPips = Param(nameof(TrailStepPips), -1m)
		.SetDisplay("Trail step (pips)", "Trailing stop step in pips", "Risk");

		_takeRatio = Param(nameof(TakeRatio), 0m)
		.SetDisplay("Take ratio", "ATR multiplier used when take profit is zero", "Risk");

		_stopRatio = Param(nameof(StopRatio), 0m)
		.SetDisplay("Stop ratio", "ATR multiplier used when stop loss is zero", "Risk");

		_startTrailRatio = Param(nameof(StartTrailRatio), 0m)
		.SetDisplay("Trail start ratio", "ATR multiplier used when trail start is zero", "Risk");

		_trailStepRatio = Param(nameof(TrailStepRatio), 0m)
		.SetDisplay("Trail step ratio", "ATR multiplier used when trail step is zero", "Risk");

		_rsiEntryLevel = Param(nameof(RsiEntryLevel), 70m)
		.SetDisplay("RSI entry level", "Threshold for entry test 256", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(55m, 80m, 5m);

		_rsiEntryPeriod = Param(nameof(RsiEntryPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI entry period", "Period for entry RSI", "Indicators");

		_rsiCloseLevel = Param(nameof(RsiCloseLevel), 40m)
		.SetDisplay("RSI close level", "Threshold for exit test 256", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20m, 60m, 5m);

		_rsiClosePeriod = Param(nameof(RsiClosePeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("RSI close period", "Period for exit RSI", "Indicators");

		_bollingerCloseLength = Param(nameof(BollingerCloseLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Exit Bollinger length", "Period of the exit Bollinger bands", "Indicators");

		_bollingerCloseDeviation = Param(nameof(BollingerCloseDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Exit Bollinger deviation", "Deviation multiplier for the exit Bollinger bands", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle type", "Primary candle type used by the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var dailyType = TimeSpan.FromDays(1).TimeFrame();
		return
		[
		(Security, CandleType),
		(Security, dailyType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentVolume = 0m;
		_averageEntryPrice = 0m;
		_lastEntryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_dailyAtrPips = null;
		_recentCandles.Clear();
		_entryBandHistory.Clear();
		_closeBandHistory.Clear();
		_rsiEntryHistory.Clear();
		_rsiCloseHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;
		_currentVolume = 0m;
		Volume = TradeVolume;

		_sma = new SimpleMovingAverage { Length = 14 };
		_emaShort = new ExponentialMovingAverage { Length = 12 };
		_emaLong = new ExponentialMovingAverage { Length = 50 };
		_entryBands = new BollingerBands { Length = 20, Width = 2m };
		_closeBands = new BollingerBands { Length = BollingerCloseLength, Width = BollingerCloseDeviation };
		_adx = new AverageDirectionalIndex { Length = 14 };
		_stochastic = new StochasticOscillator { KPeriod = 16, DPeriod = 4, Smooth = 8 };
		_williams = new WilliamsR { Length = 14 };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = 12,
			LongLength = 26,
			SignalLength = 9
		};
		_ichimoku = new IchimokuKinkoHyo
		{
			TenkanSenLength = 9,
			KijunSenLength = 26,
			SenkouSpanBLength = 52
		};
		_rsiEntry = new RelativeStrengthIndex { Length = RsiEntryPeriod };
		_rsiClose = new RelativeStrengthIndex { Length = RsiClosePeriod };
		_dailyAtr = new AverageTrueRange { Length = 15 };

		var dailyType = TimeSpan.FromDays(1).TimeFrame();
		var dailySubscription = SubscribeCandles(dailyType);
		dailySubscription
		.BindEx(_dailyAtr, ProcessDailyCandle)
		.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(
		_sma,
		_emaShort,
		_emaLong,
		_entryBands,
		_closeBands,
		_adx,
		_stochastic,
		_williams,
		_macd,
		_ichimoku,
		_rsiEntry,
		_rsiClose,
		ProcessMainCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _emaShort);
			DrawIndicator(area, _emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_dailyAtr.IsFormed)
		return;

		var atr = atrValue.ToDecimal();
		if (_pipSize > 0)
		_dailyAtrPips = atr / _pipSize;
	}

	private void ProcessMainCandle(
	ICandleMessage candle,
	IIndicatorValue smaValue,
	IIndicatorValue emaShortValue,
	IIndicatorValue emaLongValue,
	IIndicatorValue entryBandsValue,
	IIndicatorValue closeBandsValue,
	IIndicatorValue adxValue,
	IIndicatorValue stochasticValue,
	IIndicatorValue williamsValue,
	IIndicatorValue macdValue,
	IIndicatorValue ichimokuValue,
	IIndicatorValue rsiEntryValue,
	IIndicatorValue rsiCloseValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Wait for all indicators to deliver valid values before processing the bar.
		if (!_sma.IsFormed || !_emaShort.IsFormed || !_emaLong.IsFormed || !_entryBands.IsFormed || !_closeBands.IsFormed || !_adx.IsFormed || !_stochastic.IsFormed || !_williams.IsFormed || !_macd.IsFormed || !_ichimoku.IsFormed || !_rsiEntry.IsFormed || !_rsiClose.IsFormed)
		{
			UpdateIndicatorHistory(entryBandsValue, closeBandsValue, rsiEntryValue, rsiCloseValue, candle);
			return;
		}

		var emaShort = emaShortValue.ToDecimal();
		var sma = smaValue.ToDecimal();
		var emaLong = emaLongValue.ToDecimal();

		var entryBands = (BollingerBandsValue)entryBandsValue;
		var closeBands = (BollingerBandsValue)closeBandsValue;
		var adx = (AverageDirectionalIndexValue)adxValue;
		var stochastic = (StochasticOscillatorValue)stochasticValue;
		var macd = (MovingAverageConvergenceDivergenceValue)macdValue;
		var ichimoku = (IchimokuKinkoHyoValue)ichimokuValue;

		var williams = williamsValue.ToDecimal();
		var rsiEntry = rsiEntryValue.ToDecimal();
		var rsiClose = rsiCloseValue.ToDecimal();

		var previousCandle1 = GetCandleFromHistory(0);
		var previousCandle2 = GetCandleFromHistory(1);
		var previousCandle3 = GetCandleFromHistory(2);

		var previousEntryBands = GetEntryBandsFromHistory(0);
		var previousCloseBands = GetCloseBandsFromHistory(0);
		var previousRsiEntry = GetRsiEntryFromHistory(0);

		var withinSession = IsWithinTradingWindow(candle.CloseTime);

		// Update dynamic protection before making any new decisions.
		UpdateTrailingStop(candle.ClosePrice);
		CheckProtectiveLevels(candle.ClosePrice);

		if (!withinSession)
		{
			if (_currentVolume > 0 && candle.ClosePrice > _averageEntryPrice)
			CloseLongPosition("Session exit");

			UpdateIndicatorHistory(entryBandsValue, closeBandsValue, rsiEntryValue, rsiCloseValue, candle);
			return;
		}

		var buyTrades = _currentVolume > 0 && TradeVolume > 0 ? (int)Math.Round(_currentVolume / TradeVolume) : 0;
		var spacingOk = _lastEntryPrice is null || StepPips <= 0 || candle.ClosePrice - _lastEntryPrice.Value >= StepPips * _pipSize;

		var buySignal = EvaluateBuySequence(
		BuySequence,
		emaShort,
		sma,
		emaLong,
		entryBands,
		adx,
		stochastic,
		williams,
		macd,
		ichimoku,
		rsiEntry,
		previousRsiEntry,
		previousCandle1,
		previousCandle2,
		previousCandle3,
		previousEntryBands);

		var closeSignal = EvaluateCloseSequence(
		CloseBuySequence,
		emaShort,
		sma,
		emaLong,
		closeBands,
		adx,
		stochastic,
		williams,
		macd,
		ichimoku,
		rsiClose,
		previousCandle1,
		previousCandle2,
		previousCandle3,
		previousCloseBands);

		// Either extend the basket or close it depending on the signal masks.
		if (buySignal && spacingOk && buyTrades < MaxBuys)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (_currentVolume > 0 && closeSignal)
		{
			CloseLongPosition("Sequence exit");
		}

		UpdateIndicatorHistory(entryBandsValue, closeBandsValue, rsiEntryValue, rsiCloseValue, candle);
	}

	private void EnterLong(decimal price)
	{
		if (TradeVolume <= 0)
		return;

		// Send the market order that extends the long basket.
		BuyMarket(TradeVolume);

		var previousVolume = _currentVolume;
		var newVolume = previousVolume + TradeVolume;
		_averageEntryPrice = previousVolume <= 0 ? price : (previousVolume * _averageEntryPrice + TradeVolume * price) / newVolume;
		_currentVolume = newVolume;
		_lastEntryPrice = price;
		RecalculateProtectiveLevels();
	}

	private void CloseLongPosition(string reason)
	{
		if (_currentVolume <= 0)
		return;

		// Exit every open buy position in a single market order.
		SellMarket(_currentVolume);

		_currentVolume = 0m;
		_averageEntryPrice = 0m;
		_lastEntryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void UpdateIndicatorHistory(IIndicatorValue entryBandsValue, IIndicatorValue closeBandsValue, IIndicatorValue rsiEntryValue, IIndicatorValue rsiCloseValue, ICandleMessage candle)
	{
		// Store the latest indicator snapshots so the next bar can access previous values.
		var entryBands = (BollingerBandsValue)entryBandsValue;
		var entryMiddle = entryBands.MovingAverage is decimal entryMiddleValue ? entryMiddleValue : (decimal?)null;
		var entryUpper = entryBands.UpBand is decimal entryUpperValue ? entryUpperValue : (decimal?)null;
		var entryLower = entryBands.LowBand is decimal entryLowerValue ? entryLowerValue : (decimal?)null;
		_entryBandHistory.Insert(0, new BollingerSnapshot(entryMiddle, entryUpper, entryLower));
		TrimHistory(_entryBandHistory, 4);

		var closeBands = (BollingerBandsValue)closeBandsValue;
		var closeMiddle = closeBands.MovingAverage is decimal closeMiddleValue ? closeMiddleValue : (decimal?)null;
		var closeUpper = closeBands.UpBand is decimal closeUpperValue ? closeUpperValue : (decimal?)null;
		var closeLower = closeBands.LowBand is decimal closeLowerValue ? closeLowerValue : (decimal?)null;
		_closeBandHistory.Insert(0, new BollingerSnapshot(closeMiddle, closeUpper, closeLower));
		TrimHistory(_closeBandHistory, 4);

		_rsiEntryHistory.Insert(0, rsiEntryValue.ToDecimal());
		TrimHistory(_rsiEntryHistory, 4);

		_rsiCloseHistory.Insert(0, rsiCloseValue.ToDecimal());
		TrimHistory(_rsiCloseHistory, 4);

		_recentCandles.Insert(0, candle);
		TrimHistory(_recentCandles, 4);
	}

	private static void TrimHistory<T>(List<T> list, int maxCount)
	{
		if (list.Count > maxCount)
		list.RemoveRange(maxCount, list.Count - maxCount);
	}

	private ICandleMessage? GetCandleFromHistory(int index)
	{
		return index < _recentCandles.Count ? _recentCandles[index] : null;
	}

	private BollingerSnapshot? GetEntryBandsFromHistory(int index)
	{
		return index < _entryBandHistory.Count ? _entryBandHistory[index] : null;
	}

	private BollingerSnapshot? GetCloseBandsFromHistory(int index)
	{
		return index < _closeBandHistory.Count ? _closeBandHistory[index] : null;
	}

	private decimal? GetRsiEntryFromHistory(int index)
	{
		return index < _rsiEntryHistory.Count ? _rsiEntryHistory[index] : null;
	}

	private bool EvaluateBuySequence(
	int sequence,
	decimal emaShort,
	decimal sma,
	decimal emaLong,
	BollingerBandsValue entryBands,
	AverageDirectionalIndexValue adx,
	StochasticOscillatorValue stochastic,
	decimal williams,
	MovingAverageConvergenceDivergenceValue macd,
	IchimokuKinkoHyoValue ichimoku,
	decimal rsiEntry,
	decimal? previousRsiEntry,
	ICandleMessage? previousCandle1,
	ICandleMessage? previousCandle2,
	ICandleMessage? previousCandle3,
	BollingerSnapshot? previousEntryBands)
	{
		if (sequence <= 0)
		return true;

		for (var bit = 1; bit <= sequence; bit <<= 1)
		{
			if ((sequence & bit) == 0)
			continue;

			switch (bit)
			{
			case 1:
				if (!(emaShort > sma))
				return false;
				break;
			case 2:
				if (previousCandle1 is null || previousCandle2 is null || previousCandle3 is null)
				return false;
				if (!(emaLong < previousCandle1.LowPrice && emaLong < previousCandle2.LowPrice && emaLong < previousCandle3.LowPrice))
				return false;
				break;
			case 4:
				if (previousCandle1 is null || previousEntryBands is null || previousEntryBands.Value.Lower is not decimal lowerBand)
				return false;
				if (!(previousCandle1.LowPrice < lowerBand))
				return false;
				break;
			case 8:
				if (adx.MovingAverage is not decimal adxValue || adx.MinusDi is not decimal minusDi || adx.PlusDi is not decimal plusDi)
				return false;
				if (!(adxValue > minusDi && plusDi > minusDi))
				return false;
				break;
			case 16:
				if (stochastic.K is not decimal kValue || stochastic.D is not decimal dValue)
				return false;
				if (!(kValue > dValue && dValue > 80m))
				return false;
				break;
			case 32:
				if (!(williams > -20m))
				return false;
				break;
			case 64:
				if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
				return false;
				if (!(macdLine > signalLine))
				return false;
				break;
			case 128:
				if (ichimoku.SenkouSpanA is not decimal spanA || ichimoku.SenkouSpanB is not decimal spanB || ichimoku.Tenkan is not decimal tenkan || ichimoku.Kijun is not decimal kijun || previousCandle1 is null)
				return false;
				if (!(spanA > spanB && tenkan > kijun && previousCandle1.LowPrice > spanA))
				return false;
				break;
			case 256:
				if (previousRsiEntry is null)
				return false;
				if (!(rsiEntry > RsiEntryLevel && rsiEntry > previousRsiEntry.Value))
				return false;
				break;
			}
		}

		return true;
	}

	private bool EvaluateCloseSequence(
	int sequence,
	decimal emaShort,
	decimal sma,
	decimal emaLong,
	BollingerBandsValue closeBands,
	AverageDirectionalIndexValue adx,
	StochasticOscillatorValue stochastic,
	decimal williams,
	MovingAverageConvergenceDivergenceValue macd,
	IchimokuKinkoHyoValue ichimoku,
	decimal rsiClose,
	ICandleMessage? previousCandle1,
	ICandleMessage? previousCandle2,
	ICandleMessage? previousCandle3,
	BollingerSnapshot? previousCloseBands)
	{
		if (sequence <= 0)
		return true;

		for (var bit = 1; bit <= sequence; bit <<= 1)
		{
			if ((sequence & bit) == 0)
			continue;

			switch (bit)
			{
			case 1:
				if (!(sma > emaShort))
				return false;
				break;
			case 2:
				if (previousCandle1 is null || previousCandle2 is null || previousCandle3 is null)
				return false;
				if (!(emaLong > previousCandle1.HighPrice && emaLong > previousCandle2.HighPrice && emaLong > previousCandle3.HighPrice))
				return false;
				break;
			case 4:
				if (previousCandle1 is null || previousCloseBands is null || previousCloseBands.Value.Upper is not decimal upperBand)
				return false;
				if (!(previousCandle1.HighPrice > upperBand))
				return false;
				break;
			case 8:
				if (adx.MinusDi is not decimal minusDi || adx.PlusDi is not decimal plusDi)
				return false;
				if (!(minusDi > plusDi))
				return false;
				break;
			case 16:
				if (stochastic.D is not decimal dValue)
				return false;
				if (!(dValue < 80m))
				return false;
				break;
			case 32:
				if (!(williams < -80m))
				return false;
				break;
			case 64:
				if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
				return false;
				if (!(macdLine < signalLine))
				return false;
				break;
			case 128:
				if (ichimoku.SenkouSpanA is not decimal spanA || ichimoku.SenkouSpanB is not decimal spanB)
				return false;
				if (!(spanB > spanA))
				return false;
				break;
			case 256:
				if (!(rsiClose < RsiCloseLevel))
				return false;
				break;
			}
		}

		return true;
	}

	private void RecalculateProtectiveLevels()
	{
		if (_currentVolume <= 0 || _pipSize <= 0)
		{
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		var stopPips = GetStopDistance();
		_stopPrice = stopPips is null ? null : _averageEntryPrice - stopPips.Value * _pipSize;

		var takePips = GetTakeDistance();
		_takePrice = takePips is null ? null : _averageEntryPrice + takePips.Value * _pipSize;
	}

	private void CheckProtectiveLevels(decimal closePrice)
	{
		if (_currentVolume <= 0)
		return;

		if (_stopPrice is decimal stop && closePrice <= stop)
		{
			CloseLongPosition("Stop loss");
			return;
		}

		if (_takePrice is decimal take && closePrice >= take)
		CloseLongPosition("Take profit");
	}

	private void UpdateTrailingStop(decimal closePrice)
	{
		if (_currentVolume <= 0 || _pipSize <= 0)
		return;

		var stopPips = GetStopDistance();
		var stepPips = GetTrailStepDistance();

		if (stopPips is null || stepPips is null)
		return;

		var startPips = GetTrailStartDistance();
		if (startPips is not null && closePrice - _averageEntryPrice < startPips.Value * _pipSize)
		return;

		var desiredStop = closePrice - stopPips.Value * _pipSize;
		if (_stopPrice is null)
		{
			_stopPrice = desiredStop;
			return;
		}

		if (desiredStop > _stopPrice.Value + stepPips.Value * _pipSize)
		_stopPrice = desiredStop;
	}

	private decimal? GetStopDistance()
	{
		if (StopLossPips < 0)
		return null;

		if (StopLossPips == 0)
		{
			if (_dailyAtrPips is null || StopRatio <= 0)
			return null;

			return _dailyAtrPips.Value * StopRatio;
		}

		return StopLossPips;
	}

	private decimal? GetTakeDistance()
	{
		if (TakeProfitPips < 0)
		return null;

		if (TakeProfitPips == 0)
		{
			if (_dailyAtrPips is null || TakeRatio <= 0)
			return null;

			return _dailyAtrPips.Value * TakeRatio;
		}

		return TakeProfitPips;
	}

	private decimal? GetTrailStartDistance()
	{
		if (StartTrailPips < 0)
		return null;

		if (StartTrailPips == 0)
		{
			if (_dailyAtrPips is null || StartTrailRatio <= 0)
			return null;

			return _dailyAtrPips.Value * StartTrailRatio;
		}

		return StartTrailPips;
	}

	private decimal? GetTrailStepDistance()
	{
		if (TrailStepPips < 0)
		return null;

		if (TrailStepPips == 0)
		{
			if (_dailyAtrPips is null || TrailStepRatio <= 0)
			return null;

			return _dailyAtrPips.Value * TrailStepRatio;
		}

		return TrailStepPips;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		var endHour = TradeStartHour + TradeDurationHours - 1;
		return hour >= TradeStartHour && hour <= endHour;
	}
}
