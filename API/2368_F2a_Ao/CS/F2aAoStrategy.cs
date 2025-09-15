using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uses Awesome Oscillator filtered by SMA to follow trend direction.
/// Buys when filtered AO is above zero and trend candle is bullish.
/// Sells when filtered AO is below zero and trend candle is bearish.
/// </summary>
public class F2aAoStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _indicatorTimeFrame;
	private readonly StrategyParam<TimeSpan> _trendTimeFrame;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _filterLength;
	
	private decimal _trend;
	
	/// <summary>
	/// The timeframe used for oscillator calculation.
	/// </summary>
	public TimeSpan IndicatorTimeFrame
	{
		get => _indicatorTimeFrame.Value;
		set => _indicatorTimeFrame.Value = value;
	}
	
	/// <summary>
	/// The timeframe used to detect candle trend direction.
	/// </summary>
	public TimeSpan TrendTimeFrame
	{
		get => _trendTimeFrame.Value;
		set => _trendTimeFrame.Value = value;
	}
	
	/// <summary>
	/// Short period for Awesome Oscillator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}
	
	/// <summary>
	/// Long period for Awesome Oscillator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}
	
	/// <summary>
	/// SMA length used to filter the oscillator.
	/// </summary>
	public int FilterLength
	{
		get => _filterLength.Value;
		set => _filterLength.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public F2aAoStrategy()
	{
		_indicatorTimeFrame = Param(nameof(IndicatorTimeFrame), TimeSpan.FromHours(12))
		.SetDisplay("AO TimeFrame", "Time frame for oscillator", "General")
		.SetCanOptimize(true);
		
		_trendTimeFrame = Param(nameof(TrendTimeFrame), TimeSpan.FromDays(1))
		.SetDisplay("Trend TimeFrame", "Time frame for trend candle", "General")
		.SetCanOptimize(true);
		
		_fastPeriod = Param(nameof(FastPeriod), 13)
		.SetDisplay("AO Fast", "Fast period for Awesome Oscillator", "Awesome Oscillator")
		.SetCanOptimize(true);
		
		_slowPeriod = Param(nameof(SlowPeriod), 144)
		.SetDisplay("AO Slow", "Slow period for Awesome Oscillator", "Awesome Oscillator")
		.SetCanOptimize(true);
		
		_filterLength = Param(nameof(FilterLength), 3)
		.SetDisplay("Filter", "SMA length for AO filter", "Awesome Oscillator")
		.SetCanOptimize(true);
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, IndicatorTimeFrame.TimeFrame()), (Security, TrendTimeFrame.TimeFrame())];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_trend = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var ao = new AwesomeOscillator
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod
		};
		
		var filter = new SimpleMovingAverage { Length = FilterLength };
		
		var aoSubscription = SubscribeCandles(IndicatorTimeFrame);
		aoSubscription.Bind(ao, filter, ProcessOscillator).Start();
		
		var trendSubscription = SubscribeCandles(TrendTimeFrame);
		trendSubscription.Process(ProcessTrend).Start();
	}
	
	private void ProcessTrend(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_trend = candle.ClosePrice - candle.OpenPrice;
	}
	
	private void ProcessOscillator(ICandleMessage candle, decimal aoValue, decimal filteredAo)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (filteredAo > 0)
		{
			if (Position < 0)
			BuyMarket();
			
			if (_trend > 0 && Position <= 0)
			BuyMarket();
		}
		else if (filteredAo < 0)
		{
			if (Position > 0)
			SellMarket();
			
			if (_trend < 0 && Position >= 0)
			SellMarket();
		}
	}
}
