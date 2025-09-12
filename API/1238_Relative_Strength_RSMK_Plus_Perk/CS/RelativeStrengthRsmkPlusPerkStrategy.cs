using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Markos Katsanos' Relative Strength (RSMK) indicator.
/// Buys when RSMK crosses above its signal line and sells when it crosses below.
/// </summary>
public class RelativeStrengthRsmkPlusPerkStrategy : Strategy
{
	private StrategyParam<int> _period;
	private StrategyParam<int> _smooth;
	private StrategyParam<int> _signalPeriod;
	private StrategyParam<Security> _indexSecurity;
	private StrategyParam<DataType> _candleType;

	private Momentum _momentum;
	private ExponentialMovingAverage _rsmkEma;
	private ExponentialMovingAverage _signalEma;

	private decimal _indexClose;
	private bool _indexReady;
	private bool _prevAbove;

	/// <summary>
	/// Momentum period for RSMK.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// EMA smoothing for RSMK.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Reference index security.
	/// </summary>
	public Security IndexSecurity
	{
		get => _indexSecurity.Value;
		set => _indexSecurity.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public RelativeStrengthRsmkPlusPerkStrategy()
	{
		_period = Param(nameof(Period), 90)
			.SetDisplay("RSMK Period", "Momentum period for RSMK", "RSMK")
			.SetCanOptimize(true)
			.SetOptimize(30, 150, 10);

		_smooth = Param(nameof(Smooth), 3)
			.SetDisplay("Smooth", "EMA smoothing for RSMK", "RSMK")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 20)
			.SetDisplay("Signal Period", "EMA period for signal line", "RSMK")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_indexSecurity = Param<Security>(nameof(IndexSecurity))
			.SetDisplay("Index Security", "Reference index security", "Data")
			.SetRequired();

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (IndexSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_indexClose = 0m;
		_indexReady = false;
		_prevAbove = false;
		_momentum?.Reset();
		_rsmkEma?.Reset();
		_signalEma?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_momentum = new Momentum { Length = Period };
		_rsmkEma = new ExponentialMovingAverage { Length = Smooth };
		_signalEma = new ExponentialMovingAverage { Length = SignalPeriod };

		var mainSub = SubscribeCandles(CandleType);
		mainSub
			.Bind(ProcessMainCandle)
			.Start();

		var indexSub = SubscribeCandles(CandleType, security: IndexSecurity);
		indexSub
			.Bind(ProcessIndexCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, _rsmkEma);
			DrawIndicator(area, _signalEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndexCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_indexClose = candle.ClosePrice;
		_indexReady = true;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !_indexReady)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_indexClose == 0m)
			return;

		var ratio = candle.ClosePrice / _indexClose;
		var ratioLog = (decimal)Math.Log((double)ratio);

		var momentumValue = _momentum.Process(candle.Time, ratioLog);
		if (!momentumValue.IsFinal)
			return;

		var rsmkValue = _rsmkEma.Process(candle.Time, momentumValue).GetValue<decimal>() * 100m;
		var signalValue = _signalEma.Process(candle.Time, rsmkValue).GetValue<decimal>();

		var isAbove = rsmkValue > signalValue;
		var crossedUp = isAbove && !_prevAbove;
		var crossedDown = !isAbove && _prevAbove;

		if (crossedUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossedDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevAbove = isAbove;
	}
}
