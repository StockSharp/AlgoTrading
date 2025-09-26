using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sidus EMA + RSI strategy converted from the original MetaTrader 4 expert advisor.
/// The strategy waits for a fast EMA to cross a slow EMA and confirms the move with RSI relative to the 50 level.
/// Orders are executed on completed candles only and duplicate entries on the same signal candle are prevented.
/// </summary>
public class SidusEmaRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _fastEma;
	private ExponentialMovingAverage? _slowEma;
	private RelativeStrengthIndex? _rsi;

	private readonly Queue<(DateTimeOffset time, decimal fast, decimal slow, decimal rsi)> _history = new();

	private decimal _pointValue;
	private DateTimeOffset? _lastSignalTime;

	/// <summary>
	/// Take-profit distance expressed in price points (PriceStep multiples).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points (PriceStep multiples).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trade volume sent to the exchange.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// RSI period used for the confirmation filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed candles to shift the signal evaluation (equivalent of the MT4 <c>shif</c> input).
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Candle type consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults derived from the original expert advisor.
	/// </summary>
	public SidusEmaRsiStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 80m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Take Profit (points)", "Take-profit distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 160m, 20m);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 10m);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume expressed in lots or contracts", "Trading");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(14, 28, 2);

		_signalShift = Param(nameof(SignalShift), 1)
			.SetNotNegative()
			.SetDisplay("Signal Shift", "Number of closed candles used for signal evaluation", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candle series", "General");
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

		_history.Clear();
		_fastEma = null;
		_slowEma = null;
		_rsi = null;
		_pointValue = 0m;
		_lastSignalTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = ResolvePointValue();
		Volume = TradeVolume;

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _rsi, ProcessCandle)
			.Start();

		var takeDistance = TakeProfitPoints * _pointValue;
		var stopDistance = StopLossPoints * _pointValue;
		Unit takeProfitUnit = takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : null;
		Unit stopLossUnit = stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : null;
		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_fastEma is null || _slowEma is null || _rsi is null)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_rsi.IsFormed)
			return;

		_history.Enqueue((candle.OpenTime, fastValue, slowValue, rsiValue));

		var maxNeeded = SignalShift + 2;
		while (_history.Count > maxNeeded)
			_history.Dequeue();

		if (_history.Count <= SignalShift + 1)
			return;

		var snapshot = _history.ToArray();
		var currentIndex = snapshot.Length - SignalShift - 1;
		if (currentIndex <= 0)
			return;

		var current = snapshot[currentIndex];
		var previous = snapshot[currentIndex - 1];

		var bullish = previous.fast <= previous.slow && current.fast > current.slow && current.rsi > 50m;
		var bearish = previous.fast >= previous.slow && current.fast < current.slow && current.rsi < 50m;
		var signalTime = current.time;

		if (bullish)
		{
			if (Position < 0)
			{
				LogInfo($"Closing short position due to bullish crossover at {candle.ClosePrice}.");
				ClosePosition();
				return;
			}

			if (Position == 0 && _lastSignalTime != signalTime)
			{
				LogInfo($"Opening long position at {candle.ClosePrice}. Fast EMA {current.fast}, Slow EMA {current.slow}, RSI {current.rsi}.");
				BuyMarket(Volume);
				_lastSignalTime = signalTime;
			}

			return;
		}

		if (bearish)
		{
			if (Position > 0)
			{
				LogInfo($"Closing long position due to bearish crossover at {candle.ClosePrice}.");
				ClosePosition();
				return;
			}

			if (Position == 0 && _lastSignalTime != signalTime)
			{
				LogInfo($"Opening short position at {candle.ClosePrice}. Fast EMA {current.fast}, Slow EMA {current.slow}, RSI {current.rsi}.");
				SellMarket(Volume);
				_lastSignalTime = signalTime;
			}
		}
	}

	private decimal ResolvePointValue()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep is null || priceStep == 0m)
			return 1m;

		return priceStep.Value;
	}
}
