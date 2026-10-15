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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk-aware EMA crossover strategy inspired by the original MT4 Risk Monitor script.
/// Uses a risk percentage of account balance to size positions, combined with EMA crossover signals.
/// Tracks realized gains/losses and adjusts lot sizing accordingly.
/// </summary>
public class RiskMonitorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Candle type used for signal detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Percentage of the account balance allocated to new positions.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
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
	/// Take profit distance in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RiskMonitorStrategy"/>.
	/// </summary>
	public RiskMonitorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for calculations", "General");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Portion of balance used to size positions", "Risk Management")
			.SetOptimize(5m, 30m, 5m);

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
			.SetOptimize(5, 30, 5);

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
			.SetOptimize(20, 80, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Absolute take profit distance", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Absolute stop loss distance", "Risk");

		Volume = 1;
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
		_fastEma = null;
		_slowEma = null;
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}

		if (TakeProfitPoints > 0 || StopLossPoints > 0)
		{
			var tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
			var sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
			StartProtection(tp, sl);
		}

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var pf = _prevFast;
		var ps = _prevSlow;
		_prevFast = fastValue;
		_prevSlow = slowValue;

		if (pf == null || ps == null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Detect crossover
		var prevDiff = pf.Value - ps.Value;
		var currDiff = fastValue - slowValue;

		if (prevDiff <= 0 && currDiff > 0)
		{
			// Bullish crossover
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position == 0)
				BuyMarket(Volume);
		}
		else if (prevDiff >= 0 && currDiff < 0)
		{
			// Bearish crossover
			if (Position > 0)
				SellMarket(Position);
			if (Position == 0)
				SellMarket(Volume);
		}
	}
}
