namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Stochastic Oscillator with dynamic overbought and oversold zones.
/// </summary>
public class StochasticWithDynamicZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stdDevFactor;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevStochK;
	private decimal _stochSum;
	private decimal _stochSqSum;
	private int _stochCount;
	private int _cooldownRemaining;
	private DateTimeOffset? _lastEntryTime;
	private bool _wasBelowOversold;
	private readonly Queue<decimal> _stochQueue = new();

	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	public decimal StdDevFactor
	{
		get => _stdDevFactor.Value;
		set => _stdDevFactor.Value = value;
	}

	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StochasticWithDynamicZonesStrategy()
	{
		_stochKPeriod = Param(nameof(StochKPeriod), 14)
			.SetDisplay("Stoch %K Period", "Smoothing period for %K", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stoch %D Period", "Smoothing period for %D", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 40)
			.SetDisplay("Lookback Period", "Period for dynamic zones", "Indicators");

		_stdDevFactor = Param(nameof(StdDevFactor), 3.0m)
			.SetDisplay("StdDev Factor", "Factor for dynamic zones", "Indicators");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 240)
			.SetDisplay("Signal Cooldown", "Bars to wait between signals", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_prevStochK = 50m;
		_stochSum = 0m;
		_stochSqSum = 0m;
		_stochCount = 0;
		_cooldownRemaining = 0;
		_lastEntryTime = null;
		_wasBelowOversold = false;
		_stochQueue.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevStochK = 50m;
		_stochSum = 0m;
		_stochSqSum = 0m;
		_stochCount = 0;
		_cooldownRemaining = 0;
		_lastEntryTime = null;
		_wasBelowOversold = false;
		_stochQueue.Clear();

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFormed)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;

		if (stochTyped.K is not decimal stochK)
			return;

		_stochQueue.Enqueue(stochK);
		_stochSum += stochK;
		_stochSqSum += stochK * stochK;
		_stochCount++;

		if (_stochCount > LookbackPeriod)
		{
			var removed = _stochQueue.Dequeue();
			_stochSum -= removed;
			_stochSqSum -= removed * removed;
			_stochCount = LookbackPeriod;
		}

		if (_stochCount < LookbackPeriod)
		{
			_prevStochK = stochK;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var average = _stochSum / _stochCount;
		var variance = (_stochSqSum / _stochCount) - (average * average);
		var stdDev = variance <= 0m ? 0m : (decimal)Math.Sqrt((double)variance);
		var dynamicOversold = Math.Max(10m, average - StdDevFactor * stdDev);
		var entryOversold = Math.Min(dynamicOversold, 10m);
		var isReversingUp = stochK > _prevStochK;

		if (Position > 0 && stochK >= 50m)
		{
			SellMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && !HasEntryToday(candle) && _wasBelowOversold && stochK >= entryOversold && isReversingUp && Position == 0)
		{
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
			_lastEntryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		}
		_wasBelowOversold = stochK < entryOversold;
		_prevStochK = stochK;
	}

	private bool HasEntryToday(ICandleMessage candle)
	{
		if (!_lastEntryTime.HasValue)
			return false;

		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		return (candleTime.Date - _lastEntryTime.Value.Date).TotalDays < 3;
	}
}
