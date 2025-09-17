namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger band breakout strategy converted from the MetaTrader 4 expert "Crypto Analysis".
/// Combines volatility squeezes, momentum filters, and higher timeframe MACD confirmation.
/// </summary>
public class CryptoAnalysisStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailTarget;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private BollingerBands _bollinger = null!;
	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;
	private Momentum _momentumIndicator = null!;
	private MovingAverageConvergenceDivergence _macdIndicator = null!;

	private ICandleMessage? _previousCandle;
	private decimal? _previousLowerBand;
	private decimal? _previousUpperBand;

	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private bool _momentumReady;

	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _macdReady;

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _breakEvenActivated;
	private decimal _moneyTrailPeak;
	private decimal _initialEquity;
	private decimal _equityPeak;

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoAnalysisStrategy"/> class.
	/// </summary>
	public CryptoAnalysisStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base volume used for each market entry.", "Trading");

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
			.SetDisplay("Use money take profit", "Close positions when unrealized profit reaches MoneyTakeProfit.", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Money take profit", "Unrealized profit (in portfolio currency) that triggers an exit.", "Risk");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
			.SetDisplay("Use percent take profit", "Close positions once profit reaches PercentTakeProfit percent of the initial equity.", "Risk");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Percent take profit", "Profit target expressed as a percentage of the initial portfolio value.", "Risk");

		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
			.SetDisplay("Enable money trailing", "Protect accumulated profit using MoneyTrailTarget and MoneyTrailStop.", "Risk");

		_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Money trail target", "Profit level where the money-based trailing stop activates.", "Risk");

		_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Money trail stop", "Allowed profit giveback once the money trail is active.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop loss (pips)", "Protective stop distance in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take profit (pips)", "Fixed take profit distance in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing stop (pips)", "Pip distance of the trailing stop once activated.", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use break-even", "Automatically move the stop to break-even after BreakEvenTriggerPips.", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-even trigger (pips)", "Profit in pips required before break-even protection starts.", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-even offset (pips)", "Extra pips added beyond the entry price when locking in break-even.", "Risk");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum period", "Length of the momentum indicator used for confirmation.", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum buy threshold", "Absolute deviation from 100 required for long signals.", "Indicators");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum sell threshold", "Absolute deviation from 100 required for short signals.", "Indicators");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD fast", "Fast EMA length for the higher timeframe MACD filter.", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD slow", "Slow EMA length for the higher timeframe MACD filter.", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD signal", "Signal line length for the higher timeframe MACD filter.", "Indicators");

		_useEquityStop = Param(nameof(UseEquityStop), true)
			.SetDisplay("Use equity stop", "Enable portfolio level drawdown protection.", "Risk");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Equity risk (%)", "Maximum portfolio drawdown tolerated before closing the position.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary candles", "Timeframe used for the main Bollinger/MA logic.", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum candles", "Higher timeframe used for the momentum filter.", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD candles", "Trend filter timeframe used by the MACD confirmation.", "General");
	}

	/// <summary>
	/// Order volume used for new entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enables the money-based take profit.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Monetary take profit target.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables the percentage-based take profit.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Percentage-based take profit level.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enables the money trailing block.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Profit threshold that activates the money trail.
	/// </summary>
	public decimal MoneyTrailTarget
	{
		get => _moneyTrailTarget.Value;
		set => _moneyTrailTarget.Value = value;
	}

	/// <summary>
	/// Allowed drawdown once the money trail is active.
	/// </summary>
	public decimal MoneyTrailStop
	{
		get => _moneyTrailStop.Value;
		set => _moneyTrailStop.Value = value;
	}

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
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables break-even protection.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Break-even trigger distance in pips.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Break-even offset applied once triggered.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Length of the fast LWMA.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow LWMA.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation used for bullish momentum confirmation.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum deviation used for bearish momentum confirmation.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the higher timeframe MACD filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the higher timeframe MACD filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal length for the higher timeframe MACD filter.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Enables the equity stop block.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum allowed drawdown as a percentage of the equity peak.
	/// </summary>
	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the momentum confirmation.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, MomentumCandleType),
			(Security, MacdCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_previousLowerBand = null;
		_previousUpperBand = null;

		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
		_momentumReady = false;

		_macdMain = null;
		_macdSignal = null;
		_macdReady = false;

		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;
		_initialEquity = 0m;
		_equityPeak = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_bollinger = new BollingerBands
		{
			Length = 20,
			Width = 2m
		};

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = 14
		};

		_momentumIndicator = new Momentum
		{
			Length = MomentumPeriod
		};

		_macdIndicator = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		_initialEquity = GetPortfolioValue();
		_equityPeak = _initialEquity;

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_bollinger, _fastMa, _slowMa, _rsi, ProcessPrimaryCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(_momentumIndicator, ProcessMomentum)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.Bind(_macdIndicator, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = momentumValue;
		_momentumReady = _momentumIndicator.IsFormed;
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal _)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdMain = macdValue;
		_macdSignal = signalValue;
		_macdReady = _macdIndicator.IsFormed;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckEquityStop(candle);
		ApplyMoneyTargets(candle);

		var exited = UpdatePositionState(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(candle, lower, upper);
			return;
		}

		if (!IndicatorsReady())
		{
			UpdateHistory(candle, lower, upper);
			return;
		}

		if (!exited)
		{
			TryEnterLong(candle, lower, fastValue, slowValue, rsiValue);
			TryEnterShort(candle, upper, fastValue, slowValue, rsiValue);
		}

		UpdateHistory(candle, lower, upper);
	}

	private bool IndicatorsReady()
	{
		return _bollinger.IsFormed && _fastMa.IsFormed && _slowMa.IsFormed && _rsi.IsFormed && _momentumReady && _macdReady;
	}

	private void TryEnterLong(ICandleMessage candle, decimal currentLowerBand, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (Position > 0)
			return;

		if (_previousCandle is null || _previousLowerBand is null)
			return;

		if (_macdMain is not decimal macdMain || _macdSignal is not decimal macdSignal)
			return;

		var momentum = GetMomentumDeviation();
		if (momentum < MomentumBuyThreshold)
			return;

		var touchesBand = _previousCandle.LowPrice <= _previousLowerBand.Value;
		var maAligned = fastValue < slowValue;
		var rsiBullish = rsiValue > 50m;
		var macdBullish = macdMain > macdSignal;

		if (touchesBand && maAligned && rsiBullish && macdBullish)
			EnterLong(candle.ClosePrice);
	}

	private void TryEnterShort(ICandleMessage candle, decimal currentUpperBand, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (Position < 0)
			return;

		if (_previousCandle is null || _previousUpperBand is null)
			return;

		if (_macdMain is not decimal macdMain || _macdSignal is not decimal macdSignal)
			return;

		var momentum = GetMomentumDeviation();
		if (momentum < MomentumSellThreshold)
			return;

		var touchesBand = _previousCandle.HighPrice >= _previousUpperBand.Value;
		var maAligned = fastValue < slowValue;
		var rsiBearish = rsiValue < 50m;
		var macdBearish = macdMain < macdSignal;

		if (touchesBand && maAligned && rsiBearish && macdBearish)
			EnterShort(candle.ClosePrice);
	}

	private decimal GetMomentumDeviation()
	{
		if (!_momentumReady)
			return 0m;

		decimal deviation = 0m;

		if (_momentum1 is decimal m1)
			deviation = Math.Max(deviation, Math.Abs(m1 - 100m));

		if (_momentum2 is decimal m2)
			deviation = Math.Max(deviation, Math.Abs(m2 - 100m));

		if (_momentum3 is decimal m3)
			deviation = Math.Max(deviation, Math.Abs(m3 - 100m));

		return deviation;
	}

	private void EnterLong(decimal entryPrice)
	{
		var volume = GetAdjustedVolume(true);
		if (volume <= 0m)
			return;

		_entryPrice = entryPrice;
		_highestSinceEntry = entryPrice;
		_lowestSinceEntry = entryPrice;
		_stopPrice = StopLossPips > 0m ? entryPrice - GetPipDistance(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? entryPrice + GetPipDistance(TakeProfitPips) : null;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;

		BuyMarket(volume);
	}

	private void EnterShort(decimal entryPrice)
	{
		var volume = GetAdjustedVolume(false);
		if (volume <= 0m)
			return;

		_entryPrice = entryPrice;
		_highestSinceEntry = entryPrice;
		_lowestSinceEntry = entryPrice;
		_stopPrice = StopLossPips > 0m ? entryPrice + GetPipDistance(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? entryPrice - GetPipDistance(TakeProfitPips) : null;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;

		SellMarket(volume);
	}

	private decimal GetAdjustedVolume(bool isLong)
	{
		var baseVolume = OrderVolume;
		if (baseVolume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var volume = baseVolume;

		if (isLong && Position < 0m)
			volume += Math.Abs(Position);
		else if (!isLong && Position > 0m)
			volume += Position;

		var ratio = volume / step;
		var rounded = Math.Ceiling(ratio) * step;
		return rounded;
	}

	private bool UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			ApplyBreakEvenForLong();
			ApplyTrailingStopForLong();

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			ApplyBreakEvenForShort();
			ApplyTrailingStopForShort();

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void ApplyBreakEvenForLong()
	{
		if (!UseBreakEven || BreakEvenTriggerPips <= 0m || _entryPrice is not decimal entry)
			return;

		if (_breakEvenActivated)
			return;

		var triggerPrice = entry + GetPipDistance(BreakEvenTriggerPips);
		if (_highestSinceEntry >= triggerPrice)
		{
			var breakEvenPrice = entry + GetPipDistance(BreakEvenOffsetPips);
			_stopPrice = _stopPrice is decimal current ? Math.Max(current, breakEvenPrice) : breakEvenPrice;
			_breakEvenActivated = true;
		}
	}

	private void ApplyBreakEvenForShort()
	{
		if (!UseBreakEven || BreakEvenTriggerPips <= 0m || _entryPrice is not decimal entry)
			return;

		if (_breakEvenActivated)
			return;

		var triggerPrice = entry - GetPipDistance(BreakEvenTriggerPips);
		if (_lowestSinceEntry <= triggerPrice)
		{
			var breakEvenPrice = entry - GetPipDistance(BreakEvenOffsetPips);
			_stopPrice = _stopPrice is decimal current ? Math.Min(current, breakEvenPrice) : breakEvenPrice;
			_breakEvenActivated = true;
		}
	}

	private void ApplyTrailingStopForLong()
	{
		if (TrailingStopPips <= 0m)
			return;

		var candidate = _highestSinceEntry - GetPipDistance(TrailingStopPips);
		if (_stopPrice is not decimal current || candidate > current)
			_stopPrice = candidate;
	}

	private void ApplyTrailingStopForShort()
	{
		if (TrailingStopPips <= 0m)
			return;

		var candidate = _lowestSinceEntry + GetPipDistance(TrailingStopPips);
		if (_stopPrice is not decimal current || candidate < current)
			_stopPrice = candidate;
	}

	private void ApplyMoneyTargets(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			_moneyTrailPeak = 0m;
			return;
		}

		var unrealized = GetUnrealizedPnL(candle);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && unrealized >= MoneyTakeProfit)
		{
			ClosePosition();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialEquity > 0m)
		{
			var target = _initialEquity * PercentTakeProfit / 100m;
			if (unrealized >= target)
			{
				ClosePosition();
				return;
			}
		}

		if (EnableMoneyTrailing && MoneyTrailTarget > 0m && MoneyTrailStop > 0m)
		{
			if (unrealized >= MoneyTrailTarget)
			{
				_moneyTrailPeak = Math.Max(_moneyTrailPeak, unrealized);
				if (_moneyTrailPeak - unrealized >= MoneyTrailStop)
					ClosePosition();
			}
			else
			{
				_moneyTrailPeak = 0m;
			}
		}
	}

	private void CheckEquityStop(ICandleMessage candle)
	{
		if (!UseEquityStop || EquityRiskPercent <= 0m)
			return;

		var equity = GetPortfolioValue() + GetUnrealizedPnL(candle);
		_equityPeak = Math.Max(_equityPeak, equity);

		if (_equityPeak <= 0m)
			return;

		var drawdown = _equityPeak - equity;
		var threshold = _equityPeak * EquityRiskPercent / 100m;

		if (drawdown >= threshold && Position != 0m)
			ClosePosition();
	}

	private void UpdateHistory(ICandleMessage candle, decimal lower, decimal upper)
	{
		_previousCandle = candle;
		_previousLowerBand = lower;
		_previousUpperBand = upper;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;
	}

	private decimal GetPipDistance(decimal pips)
	{
		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		if (step == 0.00001m || step == 0.001m)
			step *= 10m;

		return step;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
			return portfolio.BeginValue;

		return 0m;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0m)
			return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
			return 0m;

		var diff = candle.ClosePrice - entry;
		return diff * Position;
	}
}
