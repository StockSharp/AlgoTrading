
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the "1 MINUTE SCALPER" MetaTrader expert advisor.
/// </summary>
public class OneMinuteScalperStrategy : Strategy
{
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailTarget;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _breakEvenTriggerSteps;
	private readonly StrategyParam<decimal> _breakEvenOffsetSteps;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private LinearWeightedMovingAverage _lwma3;
	private LinearWeightedMovingAverage _lwma5;
	private LinearWeightedMovingAverage _lwma8;
	private LinearWeightedMovingAverage _lwma10;
	private LinearWeightedMovingAverage _lwma12;
	private LinearWeightedMovingAverage _lwma15;
	private LinearWeightedMovingAverage _lwma30;
	private LinearWeightedMovingAverage _lwma35;
	private LinearWeightedMovingAverage _lwma40;
	private LinearWeightedMovingAverage _lwma45;
	private LinearWeightedMovingAverage _lwma50;
	private LinearWeightedMovingAverage _lwma55;
	private LinearWeightedMovingAverage _lwma200;
	private LinearWeightedMovingAverage _fastTrendMa;
	private LinearWeightedMovingAverage _slowTrendMa;
	private Momentum _momentumIndicator;
	private MovingAverageConvergenceDivergence _macdIndicator;

	private decimal _tickSize;
	private decimal? _prevHigh1;
	private decimal? _prevLow1;
	private decimal? _prevHigh2;
	private decimal? _prevLow2;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _breakEvenActivated;
	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private bool _momentumReady;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _macdReady;
	private decimal _moneyTrailPeak;
	private decimal _equityPeak;
	private decimal _initialEquity;

	/// <summary>
	/// Enable the money-based take profit.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Profit target in account currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable the percentage-based take profit.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Percentage of the initial equity used as a floating profit target.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable the floating profit trailing in account currency.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Profit level that activates the money trailing logic.
	/// </summary>
	public decimal MoneyTrailTarget
	{
		get => _moneyTrailTarget.Value;
		set => _moneyTrailTarget.Value = value;
	}

	/// <summary>
	/// Allowed pullback from the floating profit peak before closing the position.
	/// </summary>
	public decimal MoneyTrailStop
	{
		get => _moneyTrailStop.Value;
		set => _moneyTrailStop.Value = value;
	}

	/// <summary>
	/// Enable the equity drawdown stop.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum allowed equity drawdown in percent.
	/// </summary>
	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Fast LWMA length used by the trend filter.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA length used by the trend filter.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation from the neutral momentum level required for long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum deviation from the neutral momentum level required for short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Momentum indicator length on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal line length for the MACD filter.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Profit distance required before moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenTriggerSteps
	{
		get => _breakEvenTriggerSteps.Value;
		set => _breakEvenTriggerSteps.Value = value;
	}

	/// <summary>
	/// Additional step offset applied when the stop moves to break-even.
	/// </summary>
	public decimal BreakEvenOffsetSteps
	{
		get => _breakEvenOffsetSteps.Value;
		set => _breakEvenOffsetSteps.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used by the momentum filter.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used by the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OneMinuteScalperStrategy"/> class.
	/// </summary>
	public OneMinuteScalperStrategy()
	{
		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
			.SetDisplay("Use Money Take Profit", "Enable money-based profit target", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 10m)
			.SetNotNegative()
			.SetDisplay("Money Take Profit", "Profit target in account currency", "Risk");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
			.SetDisplay("Use Percent Take Profit", "Enable percentage-based profit target", "Risk");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
			.SetNotNegative()
			.SetDisplay("Percent Take Profit", "Profit target relative to initial equity", "Risk");

		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
			.SetDisplay("Enable Money Trailing", "Activate floating profit trailing", "Risk");

		_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
			.SetNotNegative()
			.SetDisplay("Money Trail Target", "Profit level that activates money trailing", "Risk");

		_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
			.SetNotNegative()
			.SetDisplay("Money Trail Stop", "Allowed pullback from peak profit", "Risk");

		_useEquityStop = Param(nameof(UseEquityStop), true)
			.SetDisplay("Use Equity Stop", "Enable drawdown-based exit", "Risk");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Equity Risk %", "Maximum drawdown as percent of peak equity", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Fast LWMA length", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Slow LWMA length", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Buy Threshold", "Deviation from 100 needed for longs", "Indicators");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Sell Threshold", "Deviation from 100 needed for shorts", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum indicator", "Indicators");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line length", "Indicators");

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Steps", "Stop loss distance in steps", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit Steps", "Take profit distance in steps", "Risk");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
			.SetNotNegative()
			.SetDisplay("Trailing Steps", "Trailing stop distance in steps", "Risk");

		_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 30m)
			.SetNotNegative()
			.SetDisplay("BreakEven Trigger", "Steps before moving to break-even", "Risk");

		_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 30m)
			.SetNotNegative()
			.SetDisplay("BreakEven Offset", "Extra steps applied at break-even", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Primary Candles", "Working timeframe", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Momentum Candles", "Higher timeframe for momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candles", "Timeframe for MACD trend filter", "General");
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

		_prevHigh1 = null;
		_prevLow1 = null;
		_prevHigh2 = null;
		_prevLow2 = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
		_momentumReady = false;
		_macdMain = null;
		_macdSignal = null;
		_macdReady = false;
		_moneyTrailPeak = 0m;
		_equityPeak = 0m;
		_initialEquity = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 0m;

		_lwma3 = new LinearWeightedMovingAverage { Length = 3 };
		_lwma5 = new LinearWeightedMovingAverage { Length = 5 };
		_lwma8 = new LinearWeightedMovingAverage { Length = 8 };
		_lwma10 = new LinearWeightedMovingAverage { Length = 10 };
		_lwma12 = new LinearWeightedMovingAverage { Length = 12 };
		_lwma15 = new LinearWeightedMovingAverage { Length = 15 };
		_lwma30 = new LinearWeightedMovingAverage { Length = 30 };
		_lwma35 = new LinearWeightedMovingAverage { Length = 35 };
		_lwma40 = new LinearWeightedMovingAverage { Length = 40 };
		_lwma45 = new LinearWeightedMovingAverage { Length = 45 };
		_lwma50 = new LinearWeightedMovingAverage { Length = 50 };
		_lwma55 = new LinearWeightedMovingAverage { Length = 55 };
		_lwma200 = new LinearWeightedMovingAverage { Length = 200 };
		_fastTrendMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowTrendMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };
		_momentumIndicator = new Momentum { Length = MomentumPeriod };
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
			.Bind(
				_lwma3,
				_lwma5,
				_lwma8,
				_lwma10,
				_lwma12,
				_lwma15,
				_lwma30,
				_lwma35,
				_lwma40,
				_lwma45,
				_lwma50,
				_lwma55,
				_lwma200,
				_fastTrendMa,
				_slowTrendMa,
				ProcessMainCandle)
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
			DrawIndicator(area, _fastTrendMa);
			DrawIndicator(area, _slowTrendMa);
			DrawIndicator(area, _lwma55);
			DrawIndicator(area, _lwma200);
			DrawOwnTrades(area);
		}
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

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdMain = macdValue;
		_macdSignal = signalValue;
		_macdReady = _macdIndicator.IsFormed;
	}

	private void ProcessMainCandle(
		ICandleMessage candle,
		decimal ema3,
		decimal ema5,
		decimal ema8,
		decimal ema10,
		decimal ema12,
		decimal ema15,
		decimal ema30,
		decimal ema35,
		decimal ema40,
		decimal ema45,
		decimal ema50,
		decimal ema55,
		decimal ema200,
		decimal fastValue,
		decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckEquityStop(candle);
		ApplyMoneyTargets(candle);

		var exited = UpdatePositionState(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(candle);
			return;
		}

		if (!IndicatorsReady())
		{
			UpdateHistory(candle);
			return;
		}

		if (!exited)
		{
			if (CanEnterLong(candle, ema3, ema5, ema8, ema10, ema12, ema15, ema30, ema35, ema40, ema45, ema50, ema55, ema200, fastValue, slowValue))
			{
				EnterLong(candle);
			}
			else if (CanEnterShort(candle, ema3, ema5, ema8, ema10, ema12, ema15, ema30, ema35, ema40, ema45, ema50, ema55, ema200, fastValue, slowValue))
			{
				EnterShort(candle);
			}
		}

		UpdateHistory(candle);
	}

	private bool IndicatorsReady()
	{
		foreach (var ma in GetTrendIndicators())
		{
			if (!ma.IsFormed)
				return false;
		}

		return _fastTrendMa.IsFormed && _slowTrendMa.IsFormed && _momentumReady && _macdReady;
	}

	private IEnumerable<LinearWeightedMovingAverage> GetTrendIndicators()
	{
		yield return _lwma3;
		yield return _lwma5;
		yield return _lwma8;
		yield return _lwma10;
		yield return _lwma12;
		yield return _lwma15;
		yield return _lwma30;
		yield return _lwma35;
		yield return _lwma40;
		yield return _lwma45;
		yield return _lwma50;
		yield return _lwma55;
		yield return _lwma200;
	}

	private bool CanEnterLong(
		ICandleMessage candle,
		decimal ema3,
		decimal ema5,
		decimal ema8,
		decimal ema10,
		decimal ema12,
		decimal ema15,
		decimal ema30,
		decimal ema35,
		decimal ema40,
		decimal ema45,
		decimal ema50,
		decimal ema55,
		decimal ema200,
		decimal fastValue,
		decimal slowValue)
	{
		if (Volume <= 0m)
			return false;

		if (Position > 0)
			return false;

		if (_prevHigh1 is not decimal prevHigh1 || _prevLow2 is not decimal prevLow2)
			return false;

		if (!HasMomentumSignal(MomentumBuyThreshold))
			return false;

		if (!HasMacdBullish())
			return false;

		if (!(fastValue > slowValue))
			return false;

		if (!(ema3 > ema5 && ema5 > ema8 && ema8 > ema10 && ema10 > ema12 && ema12 > ema15 && ema15 > ema30 && ema30 > ema35 && ema35 > ema40 && ema40 > ema45 && ema45 > ema50 && ema50 > ema55 && ema55 > ema200))
			return false;

		if (!(prevLow2 < prevHigh1))
			return false;

		return true;
	}

	private bool CanEnterShort(
		ICandleMessage candle,
		decimal ema3,
		decimal ema5,
		decimal ema8,
		decimal ema10,
		decimal ema12,
		decimal ema15,
		decimal ema30,
		decimal ema35,
		decimal ema40,
		decimal ema45,
		decimal ema50,
		decimal ema55,
		decimal ema200,
		decimal fastValue,
		decimal slowValue)
	{
		if (Volume <= 0m)
			return false;

		if (Position < 0)
			return false;

		if (_prevLow1 is not decimal prevLow1 || _prevHigh2 is not decimal prevHigh2)
			return false;

		if (!HasMomentumSignal(MomentumSellThreshold))
			return false;

		if (!HasMacdBearish())
			return false;

		if (!(fastValue < slowValue))
			return false;

		if (!(ema3 < ema5 && ema5 < ema8 && ema8 < ema10 && ema10 < ema12 && ema12 < ema15 && ema15 < ema30 && ema30 < ema35 && ema35 < ema40 && ema40 < ema45 && ema45 < ema50 && ema50 < ema55 && ema55 < ema200))
			return false;

		if (!(prevLow1 < prevHigh2))
			return false;

		return true;
	}

	private bool HasMomentumSignal(decimal threshold)
	{
		if (!_momentumReady || threshold <= 0m)
			return false;

		bool Check(decimal? value) => value is decimal actual && Math.Abs(100m - actual) >= threshold;

		return Check(_momentum1) || Check(_momentum2) || Check(_momentum3);
	}

	private bool HasMacdBullish()
	{
		return _macdMain is decimal macd && _macdSignal is decimal signal && macd > signal;
	}

	private bool HasMacdBearish()
	{
		return _macdMain is decimal macd && _macdSignal is decimal signal && macd < signal;
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);

		_entryPrice = candle.ClosePrice;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_stopPrice = StopLossSteps > 0m ? _entryPrice - GetStepValue(StopLossSteps) : null;
		_takeProfitPrice = TakeProfitSteps > 0m ? _entryPrice + GetStepValue(TakeProfitSteps) : null;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;

		BuyMarket(volume);
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);

		_entryPrice = candle.ClosePrice;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_stopPrice = StopLossSteps > 0m ? _entryPrice + GetStepValue(StopLossSteps) : null;
		_takeProfitPrice = TakeProfitSteps > 0m ? _entryPrice - GetStepValue(TakeProfitSteps) : null;
		_breakEvenActivated = false;
		_moneyTrailPeak = 0m;

		SellMarket(volume);
	}

	private bool UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			if (!_breakEvenActivated && BreakEvenTriggerSteps > 0m && _entryPrice is decimal entry)
			{
				var trigger = entry + GetStepValue(BreakEvenTriggerSteps);
				if (candle.HighPrice >= trigger)
				{
					var newStop = entry + GetStepValue(BreakEvenOffsetSteps);
					_stopPrice = _stopPrice is decimal currentStop ? Math.Max(currentStop, newStop) : newStop;
					_breakEvenActivated = true;
				}
			}

			if (TrailingStopSteps > 0m)
			{
				var candidate = _highestSinceEntry - GetStepValue(TrailingStopSteps);
				if (_stopPrice is not decimal current || candidate > current)
					_stopPrice = candidate;
			}

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

			if (!_breakEvenActivated && BreakEvenTriggerSteps > 0m && _entryPrice is decimal entry)
			{
				var trigger = entry - GetStepValue(BreakEvenTriggerSteps);
				if (candle.LowPrice <= trigger)
				{
					var newStop = entry - GetStepValue(BreakEvenOffsetSteps);
					_stopPrice = _stopPrice is decimal currentStop ? Math.Min(currentStop, newStop) : newStop;
					_breakEvenActivated = true;
				}
			}

			if (TrailingStopSteps > 0m)
			{
				var candidate = _lowestSinceEntry + GetStepValue(TrailingStopSteps);
				if (_stopPrice is not decimal current || candidate < current)
					_stopPrice = candidate;
			}

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

	private void UpdateHistory(ICandleMessage candle)
	{
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
	}

	private void ApplyMoneyTargets(ICandleMessage candle)
	{
		if (Position == 0)
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

		var drawdown = _equityPeak - equity;
		var threshold = _equityPeak * EquityRiskPercent / 100m;

		if (drawdown >= threshold && Position != 0)
			ClosePosition();
	}

	private decimal GetStepValue(decimal steps)
	{
		var size = _tickSize == 0m ? 1m : _tickSize;
		return steps * size;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0)
			return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
			return 0m;

		var diff = candle.ClosePrice - entry;
		return diff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
			return portfolio.BeginValue;

		return _initialEquity;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		ResetPositionState();
	}
}
