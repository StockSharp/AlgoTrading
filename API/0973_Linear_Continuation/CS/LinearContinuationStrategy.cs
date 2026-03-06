using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average continuation strategy.
/// Opens positions when MA ordering indicates directional continuation.
/// </summary>
public class LinearContinuationStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _ma3Period;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<bool> _aggressiveMode;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma1;
	private IIndicator _ma2;
	private IIndicator _ma3;
	private int _lastTrend;
	private int _barsFromSignal;

	/// <summary>
	/// Moving average type (SMA or EMA).
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// First moving average period.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Second moving average period.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Third moving average period.
	/// </summary>
	public int Ma3Period
	{
		get => _ma3Period.Value;
		set => _ma3Period.Value = value;
	}

	/// <summary>
	/// Minimum spread between fast and slow MA as percent of close price.
	/// </summary>
	public decimal MinSpreadPercent
	{
		get => _minSpreadPercent.Value;
		set => _minSpreadPercent.Value = value;
	}

	/// <summary>
	/// Minimum bars between new entry signals.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Use aggressive mode for continuation calculation.
	/// </summary>
	public bool AggressiveMode
	{
		get => _aggressiveMode.Value;
		set => _aggressiveMode.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public LinearContinuationStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
			.SetDisplay("MA Type", "Moving average type", "General");

		_ma1Period = Param(nameof(Ma1Period), 120)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Period for MA1", "General")
			
			.SetOptimize(60, 240, 10);

		_ma2Period = Param(nameof(Ma2Period), 55)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Period for MA2", "General")
			
			.SetOptimize(20, 140, 5);

		_ma3Period = Param(nameof(Ma3Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA3 Period", "Period for MA3", "General")
			
			.SetOptimize(8, 80, 4);

		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Min Spread %", "Minimal fast/slow MA spread in percent", "General")
			
			.SetOptimize(0.01m, 0.10m, 0.01m);

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Minimum bars between signals", "General")
			
			.SetOptimize(5, 30, 1);

		_aggressiveMode = Param(nameof(AggressiveMode), true)
			.SetDisplay("Aggressive Mode", "Use aggressive continuation", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_ma1 = null;
		_ma2 = null;
		_ma3 = null;
		_lastTrend = 0;
		_barsFromSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma1 = CreateMa(MaType, Ma1Period);
		_ma2 = CreateMa(MaType, Ma2Period);
		_ma3 = CreateMa(MaType, Ma3Period);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_ma1, _ma2, _ma3, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawIndicator(area, _ma3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Value, decimal ma2Value, decimal ma3Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_ma1 is null || _ma2 is null || _ma3 is null)
			return;

		if (!_ma1.IsFormed || !_ma2.IsFormed || !_ma3.IsFormed)
			return;

		_barsFromSignal++;

		var isBullishContinuation = ma3Value > ma2Value && ma2Value > ma1Value;
		var isBearishContinuation = ma3Value < ma2Value && ma2Value < ma1Value;

		if (!isBullishContinuation && !isBearishContinuation)
			return;

		var closePrice = candle.ClosePrice;
		if (closePrice <= 0)
			return;

		var spreadPercent = Math.Abs(ma3Value - ma1Value) / closePrice * 100m;
		var minSpread = AggressiveMode ? MinSpreadPercent : MinSpreadPercent * 1.5m;
		if (spreadPercent < minSpread)
			return;

		var cooldownBars = AggressiveMode ? SignalCooldownBars : SignalCooldownBars + GetBr(Ma2Period);
		if (_barsFromSignal < cooldownBars)
			return;

		var trend = isBullishContinuation ? 1 : -1;
		if (trend == _lastTrend)
			return;

		if (trend > 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_lastTrend = trend;
			_barsFromSignal = 0;
			return;
		}

		if (trend < 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_lastTrend = trend;
			_barsFromSignal = 0;
		}
	}

	private int GetBr(int period)
	{
		return AggressiveMode ? 1 : (int)Math.Round(period / 4.669m) + 1;
	}

	private static IIndicator CreateMa(MovingAverageTypes type, int length)
	{
		return type switch
		{
			MovingAverageTypes.Simple => new SMA { Length = length },
			MovingAverageTypes.Exponential => new EMA { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypes
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
	}
}
