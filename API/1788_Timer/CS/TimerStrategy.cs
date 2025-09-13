using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Timer-based breakout strategy.
/// Recalculates buy and sell levels every specified number of seconds
/// using ATR and places market orders when price crosses these levels.
/// </summary>
public class TimerStrategy : Strategy
{
	private readonly StrategyParam<int> _waitSeconds;
	private readonly StrategyParam<decimal> _pipDistance;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;

	private decimal _buyLevel;
	private decimal _sellLevel;
	private DateTimeOffset _lastMoveTime;
	private ATR _atr;

	/// <summary>
	/// Seconds between level recalculations.
	/// </summary>
	public int WaitSeconds
	{
		get => _waitSeconds.Value;
		set => _waitSeconds.Value = value;
	}

	/// <summary>
	/// Additional distance from price in points.
	/// </summary>
	public decimal PipDistance
	{
		get => _pipDistance.Value;
		set => _pipDistance.Value = value;
	}

	/// <summary>
	/// ATR indicator period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable trading only during specified hours.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Trading start time.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading stop time.
	/// </summary>
	public TimeSpan StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TimerStrategy()
	{
		_waitSeconds = Param(nameof(WaitSeconds), 3)
			.SetGreaterThanZero()
			.SetDisplay("Wait Seconds", "Seconds before levels are recalculated", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_pipDistance = Param(nameof(PipDistance), 0m)
			.SetDisplay("Pip Distance", "Additional distance in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 50m, 5m);

		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_takeProfit = Param(nameof(TakeProfit), 150m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 300m, 50m);

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_trailingStop = Param(nameof(TrailingStop), 10m)
			.SetDisplay("Trailing Stop", "Trailing stop in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_volume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_useTradingHours = Param(nameof(UseTradingHours), false)
			.SetDisplay("Use Trading Hours", "Enable time filter", "Trading Hours");

		_startTime = Param(nameof(StartTime), new TimeSpan(6, 0, 0))
			.SetDisplay("Start Time", "Trading start time", "Trading Hours");

		_stopTime = Param(nameof(StopTime), new TimeSpan(22, 0, 0))
			.SetDisplay("Stop Time", "Trading stop time", "Trading Hours");
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
		_buyLevel = 0m;
		_sellLevel = 0m;
		_lastMoveTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_atr = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			trailingStop: new Unit(TrailingStop, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (UseTradingHours)
		{
			var t = candle.OpenTime.TimeOfDay;
			if (t < StartTime || t >= StopTime)
				return;
		}

		if ((candle.OpenTime - _lastMoveTime).TotalSeconds >= WaitSeconds)
			MoveLevels(candle.ClosePrice, atrValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && candle.ClosePrice > _buyLevel)
		{
			BuyMarket(Volume);
		}
		else if (Position >= 0 && candle.ClosePrice < _sellLevel)
		{
			SellMarket(Volume);
		}
	}

	private void MoveLevels(decimal price, decimal atrValue)
	{
		var distance = PipDistance * Security.PriceStep;
		_buyLevel = price + distance + atrValue;
		_sellLevel = price - distance - atrValue;
		_lastMoveTime = CurrentTime;
	}
}
