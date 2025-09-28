using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BreakRevert Pro strategy converted from the MetaTrader 5 expert advisor.
/// The strategy blends breakout and mean-reversion logic using multi-timeframe candles.
/// </summary>
public class BreakRevertProStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _breakoutThreshold;
	private readonly StrategyParam<decimal> _meanReversionThreshold;
	private readonly StrategyParam<int> _tradeDelaySeconds;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _enableSafetyTrade;
	private readonly StrategyParam<int> _safetyTradeIntervalSeconds;
	private readonly StrategyParam<DataType> _candleType;

	private ISubscriptionHandler<ICandleMessage> _m1Subscription;
	private ISubscriptionHandler<ICandleMessage> _m15Subscription;
	private ISubscriptionHandler<ICandleMessage> _h1Subscription;

	private AverageTrueRange _m1Atr;
	private SimpleMovingAverage _m1TrendAverage;
	private SimpleMovingAverage _m15TrendAverage;
	private SimpleMovingAverage _h1TrendAverage;
	private SimpleMovingAverage _eventFrequency;
	private ExponentialMovingAverage _volatilityEma;

	private decimal _poissonProbability = 0.5m;
	private decimal _weibullProbability = 0.5m;
	private decimal _exponentialProbability = 0.5m;
	private decimal _m1Trend;
	private decimal _m15Trend;
	private decimal _h1Trend;
	private decimal _h1Volatility;
	private decimal? _previousM1Close;
	private decimal _latestAtr;
	private DateTimeOffset? _lastTradeTime;
	private DateTimeOffset? _lastSafetyCheck;
	private bool _safetyTradeSent;

	/// <summary>
	/// Initializes a new instance of the <see cref="BreakRevertProStrategy"/> class.
	/// </summary>
	public BreakRevertProStrategy()
	{
		_riskPerTrade = Param(nameof(RiskPerTrade), 1m)
		.SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
		.SetCanOptimize(true, 0.5m, 5m, 0.5m);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetRange(10, 60)
		.SetDisplay("Lookback", "Number of finished candles used for statistics", "Signals")
		.SetCanOptimize(true);

		_breakoutThreshold = Param(nameof(BreakoutThreshold), 0.4m)
		.SetDisplay("Breakout Threshold", "Minimum composite probability required for breakout entries", "Signals")
		.SetCanOptimize(true, 0.2m, 0.8m, 0.05m);

		_meanReversionThreshold = Param(nameof(MeanReversionThreshold), 0.4m)
		.SetDisplay("Reversion Threshold", "Maximum probability that still allows mean-reversion trades", "Signals")
		.SetCanOptimize(true, 0.2m, 0.8m, 0.05m);

		_tradeDelaySeconds = Param(nameof(TradeDelaySeconds), 600)
		.SetDisplay("Trade Delay", "Minimum delay between consecutive entries (seconds)", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetDisplay("Max Positions", "Maximum number of simultaneously open positions", "Risk");

		_enableSafetyTrade = Param(nameof(EnableSafetyTrade), true)
		.SetDisplay("Safety Trade", "Allow protective trades when validation requires at least one position", "Safety");

		_safetyTradeIntervalSeconds = Param(nameof(SafetyTradeIntervalSeconds), 900)
		.SetDisplay("Safety Interval", "Delay between safety trade checks (seconds)", "Safety");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Primary Candles", "Primary timeframe for signal generation", "Data");
	}

	/// <summary>
	/// Gets or sets the risk per trade in percent.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Gets or sets the number of candles used in rolling calculations.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the breakout probability threshold.
	/// </summary>
	public decimal BreakoutThreshold
	{
		get => _breakoutThreshold.Value;
		set => _breakoutThreshold.Value = value;
	}

	/// <summary>
	/// Gets or sets the mean-reversion probability threshold.
	/// </summary>
	public decimal MeanReversionThreshold
	{
		get => _meanReversionThreshold.Value;
		set => _meanReversionThreshold.Value = value;
	}

	/// <summary>
	/// Gets or sets the minimum delay between trades.
	/// </summary>
	public int TradeDelaySeconds
	{
		get => _tradeDelaySeconds.Value;
		set => _tradeDelaySeconds.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum simultaneous positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether safety trades are allowed.
	/// </summary>
	public bool EnableSafetyTrade
	{
		get => _enableSafetyTrade.Value;
		set => _enableSafetyTrade.Value = value;
	}

	/// <summary>
	/// Gets or sets the safety trade interval in seconds.
	/// </summary>
	public int SafetyTradeIntervalSeconds
	{
		get => _safetyTradeIntervalSeconds.Value;
		set => _safetyTradeIntervalSeconds.Value = value;
	}

	/// <summary>
	/// Gets or sets the primary candle type.
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

		_m1Subscription = null;
		_m15Subscription = null;
		_h1Subscription = null;

		_m1Atr = null;
		_m1TrendAverage = null;
		_m15TrendAverage = null;
		_h1TrendAverage = null;
		_eventFrequency = null;
		_volatilityEma = null;

		_poissonProbability = 0.5m;
		_weibullProbability = 0.5m;
		_exponentialProbability = 0.5m;
		_m1Trend = 0m;
		_m15Trend = 0m;
		_h1Trend = 0m;
		_h1Volatility = 0m;
		_previousM1Close = null;
		_latestAtr = 0m;
		_lastTradeTime = null;
		_lastSafetyCheck = null;
		_safetyTradeSent = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var lookback = Math.Max(1, LookbackPeriod);

		_m1Atr = new AverageTrueRange { Length = lookback };
		_m1TrendAverage = new SimpleMovingAverage { Length = lookback };
		_m15TrendAverage = new SimpleMovingAverage { Length = lookback };
		_h1TrendAverage = new SimpleMovingAverage { Length = lookback };
		_eventFrequency = new SimpleMovingAverage { Length = lookback };
		_volatilityEma = new ExponentialMovingAverage { Length = lookback };

		// Subscribe to the main one-minute flow.
		_m1Subscription = SubscribeCandles(CandleType);
		_m1Subscription
		.Bind(_m1Atr, ProcessPrimaryCandle)
		.Start();

		// Additional fifteen-minute stream provides mid-term trend confirmation.
		_m15Subscription = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		_m15Subscription
		.Bind(ProcessM15Candle)
		.Start();

		// Hourly candles track the broader context and volatility envelope.
		_h1Subscription = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		_h1Subscription
		.Bind(ProcessH1Candle)
		.Start();

		StartProtection();
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_latestAtr = atrValue;

		var close = candle.ClosePrice;
		var time = candle.CloseTime;
		var pip = GetPipSize();

		if (_m1TrendAverage is not null)
		{
			var trendValue = _m1TrendAverage.Process(close, time, true).ToDecimal();
			if (_m1TrendAverage.IsFormed)
			_m1Trend = close - trendValue;
		}

		if (_previousM1Close is decimal previousClose)
		{
			var move = Math.Abs(close - previousClose);
			var eventValue = move >= pip * 5m ? 1m : 0m;
			if (_eventFrequency is not null)
			{
				var avg = _eventFrequency.Process(eventValue, time, true).ToDecimal();
				if (_eventFrequency.IsFormed)
				_poissonProbability = Clamp(avg, 0m, 1m);
			}

			if (_volatilityEma is not null)
			{
				var ema = _volatilityEma.Process(move, time, true).ToDecimal();
				if (_volatilityEma.IsFormed)
				{
					var normalized = pip > 0m ? ema / (pip * 10m) : 0m;
					_exponentialProbability = Clamp(normalized, 0m, 1m);
				}
			}
		}

		_previousM1Close = close;

		var normalizedAtr = pip > 0m ? atrValue / (pip * 10m) : 0m;
		_weibullProbability = Clamp(normalizedAtr, 0m, 1m);

		EvaluateSignals(candle);
	}

	private void ProcessM15Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_m15TrendAverage is null)
		return;

		var close = candle.ClosePrice;
		var trend = _m15TrendAverage.Process(close, candle.CloseTime, true).ToDecimal();
		if (_m15TrendAverage.IsFormed)
		_m15Trend = close - trend;
	}

	private void ProcessH1Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_h1Volatility = candle.HighPrice - candle.LowPrice;

		if (_h1TrendAverage is null)
		return;

		var close = candle.ClosePrice;
		var trend = _h1TrendAverage.Process(close, candle.CloseTime, true).ToDecimal();
		if (_h1TrendAverage.IsFormed)
		_h1Trend = close - trend;
	}

	private void EvaluateSignals(ICandleMessage candle)
	{
		var now = candle.CloseTime;
		var pip = GetPipSize();

		if (_lastTradeTime is DateTimeOffset last && (now - last).TotalSeconds < TradeDelaySeconds)
		return;

		var tradeVolume = GetTradeVolume();
		if (tradeVolume <= 0m)
		return;

		var breakout = IsBreakoutSignal(pip);
		var reversion = IsMeanReversionSignal(pip);

		if (breakout && !HasReachedMaxExposure(1, tradeVolume))
		{
			EnterLong(tradeVolume);
			_lastTradeTime = now;
			_safetyTradeSent = false;
		}
		else if (reversion && !HasReachedMaxExposure(-1, tradeVolume))
		{
			EnterShort(tradeVolume);
			_lastTradeTime = now;
			_safetyTradeSent = false;
		}
		else
		{
			CheckSafetyTrade(now, tradeVolume);
		}
	}

	private bool IsBreakoutSignal(decimal pip)
	{
		var volatilityThreshold = Math.Max(pip * 10m, _latestAtr * 1.5m);
		var trendUp = _m1Trend > 0m && _m15Trend > 0m;
		var probabilityOk = _poissonProbability >= BreakoutThreshold && _weibullProbability >= MeanReversionThreshold;
		var momentumOk = _exponentialProbability >= BreakoutThreshold / 2m;
		var volatilityOk = _h1Volatility >= volatilityThreshold;
		return trendUp && probabilityOk && momentumOk && volatilityOk;
	}

	private bool IsMeanReversionSignal(decimal pip)
	{
		var flatThreshold = Math.Max(pip * 20m, _latestAtr);
		var probabilityOk = _weibullProbability <= MeanReversionThreshold && _poissonProbability <= BreakoutThreshold;
		var momentumCool = _exponentialProbability <= MeanReversionThreshold;
		var trendFlat = Math.Abs(_h1Trend) <= flatThreshold;
		return probabilityOk && momentumCool && trendFlat;
	}

	private void EnterLong(decimal volume)
	{
		var totalVolume = volume;

		if (Position < 0m)
		{
			totalVolume += Math.Abs(Position);
		}

		// Execute a market order to align with the breakout signal.
		BuyMarket(totalVolume);
	}

	private void EnterShort(decimal volume)
	{
		var totalVolume = volume;

		if (Position > 0m)
		{
			totalVolume += Math.Abs(Position);
		}

		// Execute a market order to capture the expected pullback.
		SellMarket(totalVolume);
	}

	private void CheckSafetyTrade(DateTimeOffset time, decimal volume)
	{
		if (!EnableSafetyTrade || _safetyTradeSent || Position != 0m)
		return;

		if (_lastSafetyCheck is DateTimeOffset last && (time - last).TotalSeconds < SafetyTradeIntervalSeconds)
		return;

		_lastSafetyCheck = time;

		var direction = _m1Trend + _m15Trend;
		if (direction > 0m)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_safetyTradeSent = true;
		_lastTradeTime = time;
	}

	private bool HasReachedMaxExposure(int direction, decimal tradeVolume)
	{
		if (MaxPositions <= 0 || tradeVolume <= 0m)
		return false;

		var limit = MaxPositions * tradeVolume;

		return direction switch
		{
			> 0 => Position >= limit,
			< 0 => -Position >= limit,
			_ => Math.Abs(Position) >= limit,
		};
	}

	private decimal GetTradeVolume()
	{
		if (Volume > 0m)
		return Volume;

		var stepVolume = Security?.StepVolume ?? 1m;
		var lotStep = Security?.VolumeStep ?? stepVolume;
		var minVolume = Security?.MinVolume ?? stepVolume;
		var maxVolume = Security?.MaxVolume ?? decimal.MaxValue;
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var atr = Math.Max(_latestAtr, GetPipSize());

		if (stepVolume <= 0m)
		stepVolume = 1m;

		if (lotStep <= 0m)
		lotStep = stepVolume;

		if (minVolume <= 0m)
		minVolume = stepVolume;

		if (balance <= 0m || atr <= 0m)
		return minVolume;

		var riskAmount = balance * RiskPerTrade / 100m;
		if (riskAmount <= 0m)
		return minVolume;

		var riskPerUnit = atr;
		var rawVolume = riskPerUnit > 0m ? riskAmount / riskPerUnit : minVolume;
		rawVolume = Math.Max(rawVolume, minVolume);

		var normalized = Math.Floor(rawVolume / lotStep) * lotStep;
		if (normalized <= 0m)
		normalized = minVolume;

		if (maxVolume > 0m && normalized > maxVolume)
		normalized = maxVolume;

		return normalized;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0m)
		return 0.0001m;

		return step.Value;
	}

	private static decimal Clamp(decimal value, decimal min, decimal max)
	{
		if (value < min)
		return min;

		return value > max ? max : value;
	}
}

