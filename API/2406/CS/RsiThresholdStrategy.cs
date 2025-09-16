using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TrendMode
{
	Direct,
	Against,
}

/// <summary>
/// Simple RSI threshold strategy converted from MQL/16855.
/// Buys or sells when RSI crosses predefined levels.
/// </summary>
public class RsiThresholdStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _prevRsi;
	
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public TrendMode Trend { get => _trend.Value; set => _trend.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public RsiThresholdStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Period for RSI calculation", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);
		
		_highLevel = Param(nameof(HighLevel), 60m)
		.SetDisplay("RSI High Level", "Overbought level", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(50m, 80m, 5m);
		
		_lowLevel = Param(nameof(LowLevel), 40m)
		.SetDisplay("RSI Low Level", "Oversold level", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(20m, 50m, 5m);
		
		_trend = Param(nameof(Trend), TrendMode.Direct)
		.SetDisplay("Trend Mode", "Trading direction relative to RSI crossings", "General");
		
		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Enable Buy Entry", "Allow opening long positions", "General");
		
		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Enable Sell Entry", "Allow opening short positions", "General");
		
		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Enable Buy Exit", "Allow closing long positions", "General");
		
		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Enable Sell Exit", "Allow closing short positions", "General");
		
		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management");
		
		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit in price units", "Risk Management");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(rsi, ProcessCandle)
		.Start();
		
		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) // only process completed candles
		return;
		
		if (_prevRsi is null)
		{
			// store first RSI value to detect crossings
			_prevRsi = rsi;
			return;
		}
		
		if (Trend == TrendMode.Direct)
		{
			if (_prevRsi <= LowLevel && rsi > LowLevel)
			// RSI crossed above oversold level
			{
				if (SellClose && Position < 0) // close short positions
				BuyMarket();
				
				if (BuyOpen && Position <= 0) // open long
				BuyMarket();
			}
			
			if (_prevRsi >= HighLevel && rsi < HighLevel)
			// RSI crossed below overbought level
			{
				if (BuyClose && Position > 0) // close long positions
				SellMarket();
				
				if (SellOpen && Position >= 0) // open short
				SellMarket();
			}
		}
else
	{
		if (_prevRsi <= LowLevel && rsi > LowLevel)
			// RSI crossed above oversold level
		{
			if (BuyClose && Position > 0) // close long positions
			SellMarket();
			
			if (SellOpen && Position >= 0) // open short
			SellMarket();
		}
		
		if (_prevRsi >= HighLevel && rsi < HighLevel)
			// RSI crossed below overbought level
		{
			if (SellClose && Position < 0) // close short positions
			BuyMarket();
			
			if (BuyOpen && Position <= 0) // open long
			BuyMarket();
		}
	}
	
	_prevRsi = rsi; // remember last RSI value
}
}
