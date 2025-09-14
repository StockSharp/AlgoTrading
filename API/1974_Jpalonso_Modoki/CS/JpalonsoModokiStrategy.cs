namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on price position relative to SMA envelopes.
/// Buys when price is below the middle or above it, and sells in opposite situations.
/// </summary>
public class JpalonsoModokiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<Unit> _stopLoss;
	
	private SimpleMovingAverage _sma;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="JpalonsoModokiStrategy"/> class.
	/// </summary>
	public JpalonsoModokiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_smaPeriod = Param(nameof(SmaPeriod), 200)
		.SetDisplay("SMA Period", "Length of the moving average", "Envelopes");
		
		_deviation = Param(nameof(Deviation), 0.35m)
		.SetDisplay("Deviation %", "Envelope deviation from SMA in percent", "Envelopes");
		
		_takeProfit = Param(nameof(TakeProfit), new Unit(127, UnitTypes.Point))
		.SetDisplay("Take Profit", "Take profit in points", "Risk Management");
		
		_stopLoss = Param(nameof(StopLoss), new Unit(77, UnitTypes.Point))
		.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management");
	}
	
	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Moving average period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}
	
	/// <summary>
	/// Envelope deviation in percent.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}
	
	/// <summary>
	/// Take profit in points.
	/// </summary>
	public Unit TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection(takeProfit: TakeProfit, stopLoss: StopLoss);
		
		_sma = new SimpleMovingAverage { Length = SmaPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}
	
	/// <summary>
	/// Process incoming candles and generate trade signals.
	/// </summary>
	/// <param name="candle">Current candle.</param>
	/// <param name="ma">SMA value.</param>
	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		// Only finished candles are processed
		if (candle.State != CandleStates.Finished)
		return;
		
		// Wait until the indicator has enough data
		if (!_sma.IsFormed)
		return;
		
		var upper = ma * (1 + Deviation / 100m);
		var lower = ma * (1 - Deviation / 100m);
		var close = candle.ClosePrice;
		
		var buy = close <= lower || (close < upper && close > ma);
		var sell = close >= upper || (close > lower && close < ma);
		
		if (buy && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sell && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}

