using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the "ScalperEMAEASimple" MetaTrader expert advisor.
/// Combines EMA trend detection, stochastic momentum, and an ADX filter to scalp retracements.
/// Includes configurable stop-loss and trailing-stop mechanics expressed in pips.
/// </summary>
public class ScalperEmaSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _stochasticOversold;
	private readonly StrategyParam<decimal> _stochasticOverbought;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<int> _conditionWindowBars;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingDistancePips;
	private readonly StrategyParam<decimal> _trailingActivationPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _previousFastEma;
	private decimal? _previousSlowEma;
	private decimal? _previousStochastic;
	private long _barIndex;
	private long _lastBuySignalIndex;
	private long _lastSellSignalIndex;
	private long _lastRetracementIndex;
	private long _lastBreakIndex;
	private long _lastStochasticIndex;
	private bool _lastStochasticCrossUp;
	private readonly decimal[] _distanceHistory = new decimal[3];
	private int _storedDistances;


	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Base lookback period for the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Stochastic %K smoothing period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold used for long signals.
	/// </summary>
	public decimal StochasticOversold
	{
		get => _stochasticOversold.Value;
		set => _stochasticOversold.Value = value;
	}

	/// <summary>
	/// Overbought threshold used for short signals.
	/// </summary>
	public decimal StochasticOverbought
	{
		get => _stochasticOverbought.Value;
		set => _stochasticOverbought.Value = value;
	}

	/// <summary>
	/// ADX upper bound. Trend strength must stay below this level to allow trades.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between consecutive signals of the same direction.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Number of bars during which confirmation conditions must stay valid.
	/// </summary>
	public int ConditionWindowBars
	{
		get => _conditionWindowBars.Value;
		set => _conditionWindowBars.Value = value;
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
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingDistancePips
	{
		get => _trailingDistancePips.Value;
		set => _trailingDistancePips.Value = value;
	}

	/// <summary>
	/// Profit in pips required before the trailing stop activates.
	/// </summary>
	public decimal TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Candle type used for the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScalperEmaSimpleStrategy"/>.
	/// </summary>
	public ScalperEmaSimpleStrategy()
	{

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 39)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Length of the fast EMA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(20, 80, 5);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 740)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Length of the slow EMA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(400, 900, 40);

		_stochasticLength = Param(nameof(StochasticLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Length", "Base lookback for the stochastic", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(3, 12, 1);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Smoothing period for the %K line", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Smoothing period for the %D line", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);

		_stochasticOversold = Param(nameof(StochasticOversold), 20m)
		.SetDisplay("Stochastic Oversold", "Level that confirms bullish momentum", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(10m, 30m, 5m);

		_stochasticOverbought = Param(nameof(StochasticOverbought), 80m)
		.SetDisplay("Stochastic Overbought", "Level that confirms bearish momentum", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(70m, 90m, 5m);

		_adxThreshold = Param(nameof(AdxThreshold), 37m)
		.SetDisplay("ADX Threshold", "Maximum ADX value allowed to trade", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(20m, 50m, 5m);

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 4)
		.SetGreaterThanZero()
		.SetDisplay("Signal Cooldown", "Minimum bars between signals", "Logic");

		_conditionWindowBars = Param(nameof(ConditionWindowBars), 3)
		.SetGreaterThanZero()
		.SetDisplay("Condition Window", "Bars during which confirmations must align", "Logic");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_trailingDistancePips = Param(nameof(TrailingDistancePips), 840m)
		.SetNotNegative()
		.SetDisplay("Trailing Distance", "Trailing stop distance in pips", "Risk");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 10m)
		.SetNotNegative()
		.SetDisplay("Trailing Activation", "Profit in pips required to arm the trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");
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

		_pipSize = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_previousFastEma = null;
		_previousSlowEma = null;
		_previousStochastic = null;
		_barIndex = 0;
		_lastBuySignalIndex = long.MinValue;
		_lastSellSignalIndex = long.MinValue;
		_lastRetracementIndex = long.MinValue;
		_lastBreakIndex = long.MinValue;
		_lastStochasticIndex = long.MinValue;
		_lastStochasticCrossUp = false;
		Array.Clear(_distanceHistory, 0, _distanceHistory.Length);
		_storedDistances = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new EMA { Length = FastEmaPeriod };
		var slowEma = new EMA { Length = SlowEmaPeriod };
		var stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod
		};
		var adx = new AverageDirectionalIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(fastEma, slowEma, stochastic, adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, stochastic);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue fastEmaValue,
	IIndicatorValue slowEmaValue,
	IIndicatorValue stochasticValue,
	IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!fastEmaValue.IsFinal || !slowEmaValue.IsFinal || !stochasticValue.IsFinal || !adxValue.IsFinal)
		return;

		var fastEma = fastEmaValue.GetValue<decimal>();
		var slowEma = slowEmaValue.GetValue<decimal>();

		if (stochasticValue is not StochasticOscillatorValue stochastic || stochastic.K is not decimal stochK)
		return;

		if (adxValue is not AverageDirectionalIndexValue adxData || adxData.MovingAverage is not decimal adx)
		return;

		_pipSize ??= CalculatePipSize();

		// Update bar index to track signal windows.
		_barIndex++;

		var candleColorUp = candle.ClosePrice >= candle.OpenPrice;
		var distancePrice = candle.ClosePrice > slowEma ? candle.LowPrice : candle.HighPrice;
		var distanceFromSlowEma = Math.Abs(distancePrice - slowEma);
		var barSize = candle.HighPrice - candle.LowPrice;

		UpdateDistance(distanceFromSlowEma);
		var hasRetracement = distanceFromSlowEma < barSize && IsLowerThanHistory(distanceFromSlowEma);
		if (hasRetracement)
		{
			_lastRetracementIndex = _barIndex;
		}

		var crossedFast = candleColorUp
		? candle.OpenPrice < fastEma && candle.ClosePrice > fastEma
		: candle.OpenPrice > fastEma && candle.ClosePrice < fastEma;
		var emaTrendBreak = _previousFastEma is decimal prevFast && _previousSlowEma is decimal prevSlow
		&& ((prevFast < prevSlow && fastEma > slowEma) || (prevFast > prevSlow && fastEma < slowEma));
		if (crossedFast || emaTrendBreak)
		{
			_lastBreakIndex = _barIndex;
		}

		var crossedOversold = _previousStochastic is decimal prevStoch && prevStoch <= StochasticOversold && stochK >= StochasticOversold;
		var crossedOverbought = _previousStochastic is decimal prevStochDown && prevStochDown >= StochasticOverbought && stochK <= StochasticOverbought;
		if (crossedOversold)
		{
			_lastStochasticIndex = _barIndex;
			_lastStochasticCrossUp = true;
		}
		else if (crossedOverbought)
		{
			_lastStochasticIndex = _barIndex;
			_lastStochasticCrossUp = false;
		}

		var windowSatisfied = _barIndex - _lastRetracementIndex <= ConditionWindowBars
		&& _barIndex - _lastBreakIndex <= ConditionWindowBars
		&& _barIndex - _lastStochasticIndex <= ConditionWindowBars;

		var allowLong = candle.OpenPrice > slowEma && candle.ClosePrice > slowEma && fastEma > slowEma;
		var allowShort = candle.OpenPrice < slowEma && candle.ClosePrice < slowEma && fastEma < slowEma;

		var canBuy = allowLong && windowSatisfied && _lastStochasticCrossUp && adx < AdxThreshold && _barIndex - _lastBuySignalIndex > SignalCooldownBars;
		var canSell = allowShort && windowSatisfied && !_lastStochasticCrossUp && adx < AdxThreshold && _barIndex - _lastSellSignalIndex > SignalCooldownBars;

		if (canBuy && Position <= 0)
		{
			EnterLong(candle.ClosePrice);
			_lastBuySignalIndex = _barIndex;
		}
		else if (canSell && Position >= 0)
		{
			EnterShort(candle.ClosePrice);
			_lastSellSignalIndex = _barIndex;
		}

		if (Position > 0 && canSell)
		{
			ClosePosition();
		}
		else if (Position < 0 && canBuy)
		{
			ClosePosition();
		}

		UpdateTrailingStop(candle.ClosePrice);

		_previousFastEma = fastEma;
		_previousSlowEma = slowEma;
		_previousStochastic = stochK;
	}

	private void EnterLong(decimal price)
	{
		var volumeToBuy = Volume + Math.Max(Position, 0m);
		var resultingPosition = Position + volumeToBuy;
		BuyMarket(volumeToBuy);
		_longEntryPrice = price;
		_shortEntryPrice = null;

		ApplyStopLoss(price, resultingPosition);
	}

	private void EnterShort(decimal price)
	{
		var volumeToSell = Volume + Math.Max(-Position, 0m);
		var resultingPosition = Position - volumeToSell;
		SellMarket(volumeToSell);
		_shortEntryPrice = price;
		_longEntryPrice = null;

		ApplyStopLoss(price, resultingPosition);
	}

	private void ApplyStopLoss(decimal referencePrice, decimal resultingPosition)
	{
		var stopDistance = ConvertPipsToPrice(StopLossPips);
		if (stopDistance <= 0m)
		return;

		SetStopLoss(stopDistance, referencePrice, resultingPosition);
	}

	private void UpdateTrailingStop(decimal price)
	{
		var trailingDistance = ConvertPipsToPrice(TrailingDistancePips);
		var activation = ConvertPipsToPrice(TrailingActivationPips);

		if (trailingDistance <= 0m || activation < 0m)
		return;

		if (Position > 0 && _longEntryPrice is decimal entry)
		{
			var gain = price - entry;
			if (gain >= activation)
			{
				SetStopLoss(trailingDistance, price, Position);
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal entryShort)
		{
			var gain = entryShort - price;
			if (gain >= activation)
			{
				SetStopLoss(trailingDistance, price, Position);
			}
		}
	}

	private void UpdateDistance(decimal distance)
	{
		if (_storedDistances < _distanceHistory.Length)
		{
			_distanceHistory[_storedDistances++] = distance;
		}
		else
		{
			for (var i = _distanceHistory.Length - 1; i > 0; i--)
			{
				_distanceHistory[i] = _distanceHistory[i - 1];
			}
			_distanceHistory[0] = distance;
		}
	}

	private bool IsLowerThanHistory(decimal distance)
	{
		if (_storedDistances < _distanceHistory.Length)
		return false;

		for (var i = 0; i < _distanceHistory.Length; i++)
		{
			if (distance >= _distanceHistory[i])
			return false;
		}

		return true;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		var pipSize = _pipSize ?? CalculatePipSize();
		return pipSize * pips;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		var decimals = GetDecimalPlaces(priceStep);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * factor;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		if (value == 0m)
		return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
