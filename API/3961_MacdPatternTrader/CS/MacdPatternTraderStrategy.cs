namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the MetaTrader 4 expert advisor MacdPatternTraderv04cb.
/// Detects bearish and bullish double-top/double-bottom patterns on the MACD main line.
/// The second swing needs to be weaker than the first one before a market order is sent.
/// Fixed protective orders replicate the original 100/300 pip stop-loss and take-profit distances.
/// </summary>
public class MacdPatternTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _triggerLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _previousMacd;
	private decimal? _previousMacd2;
	private decimal? _firstPeak;
	private decimal? _firstTrough;
	private bool _sellPatternArmed;
	private bool _buyPatternArmed;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacdPatternTraderStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow moving average length used by MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal EMA", "Signal smoothing period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(1, 9, 1);

		_triggerLevel = Param(nameof(TriggerLevel), 0.0045m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Trigger Level", "Absolute MACD level that arms the pattern logic", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.01m, 0.0005m);

		_stopLossPips = Param(nameof(StopLossPips), 100m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop-Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(50m, 200m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take-Profit (pips)", "Take-profit distance expressed in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(150m, 500m, 10m);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order volume used for new entries", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 1m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for MACD calculations", "General");
	}

/// <summary>
/// Fast EMA length used by MACD.
/// </summary>
public int FastPeriod
{
	get => _fastPeriod.Value;
	set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow EMA length used by MACD.
/// </summary>
public int SlowPeriod
{
	get => _slowPeriod.Value;
	set => _slowPeriod.Value = value;
}

/// <summary>
/// Signal EMA length used by MACD.
/// </summary>
public int SignalPeriod
{
	get => _signalPeriod.Value;
	set => _signalPeriod.Value = value;
}

/// <summary>
/// Absolute MACD level that enables pattern tracking.
/// </summary>
public decimal TriggerLevel
{
	get => _triggerLevel.Value;
	set => _triggerLevel.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in pips.
/// </summary>
public decimal StopLossPips
{
	get => _stopLossPips.Value;
	set => _stopLossPips.Value = value;
}

/// <summary>
/// Take-profit distance expressed in pips.
/// </summary>
public decimal TakeProfitPips
{
	get => _takeProfitPips.Value;
	set => _takeProfitPips.Value = value;
}

/// <summary>
/// Trading volume used for new market orders.
/// </summary>
public decimal TradeVolume
{
	get => _tradeVolume.Value;
	set => _tradeVolume.Value = value;
}

/// <summary>
/// Candle type used for indicator calculations.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
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

	_previousMacd = null;
	_previousMacd2 = null;
	_firstPeak = null;
	_firstTrough = null;
	_sellPatternArmed = false;
	_buyPatternArmed = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	Volume = TradeVolume;

	_macd = new MovingAverageConvergenceDivergenceSignal
	{
		Macd =
		{
			ShortMa = { Length = FastPeriod },
			LongMa = { Length = SlowPeriod },
		},
	SignalMa = { Length = SignalPeriod },
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_macd, ProcessCandle)
.Start();

StartProtection(CreateTakeProfitUnit(), CreateStopLossUnit(), useMarketOrders: true);
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceSignalValue typed)
	return;

	if (typed.Macd is not decimal macdLine)
	return;

	var previous = _previousMacd;
	var previous2 = _previousMacd2;

	if (previous.HasValue && previous2.HasValue)
	{
		ProcessSellPattern(macdLine, previous.Value, previous2.Value);
		ProcessBuyPattern(macdLine, previous.Value, previous2.Value);
	}

_previousMacd2 = previous;
_previousMacd = macdLine;
}

private void ProcessSellPattern(decimal current, decimal previous, decimal previous2)
{
	if (current > TriggerLevel && current < previous && previous > previous2)
	{
		if (!_sellPatternArmed)
		{
			_firstPeak = previous;
			_sellPatternArmed = true;
		}
	else if (_firstPeak is decimal referencePeak && previous < referencePeak)
	{
		EnterShort();
		ResetSellPattern();
	}
}
else if (current < TriggerLevel)
{
	ResetSellPattern();
}
}

private void ProcessBuyPattern(decimal current, decimal previous, decimal previous2)
{
	var negativeTrigger = -TriggerLevel;

	if (current < negativeTrigger && current > previous && previous < previous2)
	{
		if (!_buyPatternArmed)
		{
			_firstTrough = previous;
			_buyPatternArmed = true;
		}
	else if (_firstTrough is decimal referenceTrough && previous > referenceTrough)
	{
		EnterLong();
		ResetBuyPattern();
	}
}
else if (current > negativeTrigger)
{
	ResetBuyPattern();
}
}

private void EnterShort()
{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var volume = Volume + Math.Max(0m, Position);
	if (volume <= 0m)
	return;

	SellMarket(volume);
}

private void EnterLong()
{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var volume = Volume + Math.Max(0m, -Position);
	if (volume <= 0m)
	return;

	BuyMarket(volume);
}

private void ResetSellPattern()
{
	_sellPatternArmed = false;
	_firstPeak = null;
}

private void ResetBuyPattern()
{
	_buyPatternArmed = false;
	_firstTrough = null;
}

private Unit CreateStopLossUnit()
{
	return CreateUnitFromPips(StopLossPips);
}

private Unit CreateTakeProfitUnit()
{
	return CreateUnitFromPips(TakeProfitPips);
}

private Unit CreateUnitFromPips(decimal pips)
{
	if (pips <= 0m)
	return null;

	var security = Security;
	if (security?.Step is not decimal priceStep || priceStep <= 0m)
	return null;

	var pipSize = GetPipSize(security);
	if (pipSize <= 0m)
	return null;

	var steps = pips * pipSize / priceStep;
	return new Unit(steps, UnitTypes.Step);
}

private static decimal GetPipSize(Security security)
{
	if (security.Step is not decimal step || step <= 0m)
	return 0m;

	return security.Decimals is 3 or 5 ? step * 10m : step;
}
}
