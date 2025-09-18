using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines an ATR based stop with a simplified Step Up/Down trend filter.
/// A long position is opened when both modules signal an upward trend.
/// A short position is opened when both modules signal a downward trend.
/// </summary>
public class ExpBuySellSideStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<bool> _closeByOppositeSignal;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevDiff;
	private decimal _prevUpper;
	private decimal _prevLower;
	private int _atrTrend;
	
	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	
	/// <summary>
	/// Multiplier applied to ATR for stop calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Fast SMA length used in trend filter.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}
	
	/// <summary>
	/// Slow SMA length used in trend filter.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}
	
	/// <summary>
	/// Close opposite position before opening new one.
	/// </summary>
	public bool CloseByOppositeSignal
	{
		get => _closeByOppositeSignal.Value;
		set => _closeByOppositeSignal.Value = value;
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
	/// Constructor.
	/// </summary>
	public ExpBuySellSideStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation length", "ATR")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 2.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR band multiplier", "ATR")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_fastPeriod = Param(nameof(FastPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA", "Fast moving average length", "Step Up/Down")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);
		
		_slowPeriod = Param(nameof(SlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA", "Slow moving average length", "Step Up/Down")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);
		
		_closeByOppositeSignal = Param(nameof(CloseByOppositeSignal), true)
		.SetDisplay("Close Opposite", "Close opposite position on signal", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevDiff = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_atrTrend = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		// Start position protection once.
		StartProtection();
		
		var atr = new ATR { Length = AtrPeriod };
		var fast = new SMA { Length = FastPeriod };
		var slow = new SMA { Length = SlowPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(fast, slow, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		// Step Up/Down filter using difference between fast and slow SMA.
		var diff = fast - slow;
		var stepSignal = 0;
		if (fast > slow && diff > _prevDiff)
		stepSignal = 1;
		else if (fast < slow && diff < _prevDiff)
		stepSignal = -1;
		
		_prevDiff = diff;
		
		// ATR based stop similar to SuperTrend.
		var upper = candle.HighPrice - AtrMultiplier * atr;
		var lower = candle.LowPrice + AtrMultiplier * atr;
		var atrSignal = 0;
		
		if (candle.ClosePrice > _prevUpper && _atrTrend <= 0)
		{
			_atrTrend = 1;
			atrSignal = 1;
		}
		else if (candle.ClosePrice < _prevLower && _atrTrend >= 0)
		{
			_atrTrend = -1;
			atrSignal = -1;
		}
		
		_prevUpper = upper;
		_prevLower = lower;
		
		var tradeSignal = 0;
		if (atrSignal == 1 && stepSignal == 1)
		tradeSignal = 1;
		else if (atrSignal == -1 && stepSignal == -1)
		tradeSignal = -1;
		
		switch (tradeSignal)
		{
		case 1:
			if (CloseByOppositeSignal && Position < 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			break;
		case -1:
			if (CloseByOppositeSignal && Position > 0)
			SellMarket(Volume + Math.Abs(Position));
			else if (Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
			break;
		}
	}
}
