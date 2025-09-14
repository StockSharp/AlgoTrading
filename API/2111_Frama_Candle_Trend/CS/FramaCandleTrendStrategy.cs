using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FrAMA candle trend-following strategy.
/// </summary>
public class FramaCandleTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _framaPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	
	private FractalAdaptiveMovingAverage _framaOpen;
	private FractalAdaptiveMovingAverage _framaClose;
	private readonly List<int> _colorHistory = new();
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public int FramaPeriod
	{
		get => _framaPeriod.Value;
		set => _framaPeriod.Value = value;
	}
	
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}
	
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}
	
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}
	
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}
	
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}
	
	public FramaCandleTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
		
		_framaPeriod = Param(nameof(FramaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("FrAMA Period", "Length of the Fractal Adaptive Moving Average", "Indicator");
		
		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqual(0)
		.SetDisplay("Signal Bar", "Offset of bar used for signal detection", "Indicator");
		
		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Enable Buy Open", "Allow opening long positions", "Trading");
		
		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Enable Sell Open", "Allow opening short positions", "Trading");
		
		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Enable Buy Close", "Allow closing long positions", "Trading");
		
		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Enable Sell Close", "Allow closing short positions", "Trading");
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
		
		_framaOpen = new FractalAdaptiveMovingAverage { Length = FramaPeriod };
		_framaClose = new FractalAdaptiveMovingAverage { Length = FramaPeriod };
		_colorHistory.Clear();
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _framaOpen);
			DrawIndicator(area, _framaClose);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var openValue = _framaOpen.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
		var closeValue = _framaClose.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		
		if (!openValue.IsFinal || !closeValue.IsFinal)
		return;
		
		var openMa = openValue.GetValue<decimal>();
		var closeMa = closeValue.GetValue<decimal>();
		
		var color = openMa < closeMa ? 2 : openMa > closeMa ? 0 : 1;
		_colorHistory.Insert(0, color);
		
		var required = SignalBar + 2;
		if (_colorHistory.Count < required)
		return;
		
		var current = _colorHistory[SignalBar];
		var prev = _colorHistory[SignalBar + 1];
		
		if (prev == 2)
		{
			if (SellClose && Position < 0)
			BuyMarket(Math.Abs(Position));
			
			if (BuyOpen && current < 2 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (prev == 0)
		{
			if (BuyClose && Position > 0)
			SellMarket(Math.Abs(Position));
			
			if (SellOpen && current > 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
