using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects divergences between price and RSI.
/// Buys on bullish divergence and sells on bearish divergence.
/// </summary>
public class DivergenceIndicatorAnyOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _lbLeft;
	private readonly StrategyParam<int> _lbRight;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal?[] _oscValues = [];
	private decimal?[] _highs = [];
	private decimal?[] _lows = [];
	
	private int _sinceLastPivotLow;
	private int _sinceLastPivotHigh;
	private decimal? _lastOscPivotLow;
	private decimal? _lastPricePivotLow;
	private decimal? _lastOscPivotHigh;
	private decimal? _lastPricePivotHigh;
	
	/// <summary>
	/// Bars to the left of pivot.
	/// </summary>
	public int LbLeft { get => _lbLeft.Value; set => _lbLeft.Value = value; }
	
	/// <summary>
	/// Bars to the right of pivot.
	/// </summary>
	public int LbRight { get => _lbRight.Value; set => _lbRight.Value = value; }
	
	/// <summary>
	/// Maximum bars between pivots.
	/// </summary>
	public int RangeUpper { get => _rangeUpper.Value; set => _rangeUpper.Value = value; }
	
	/// <summary>
	/// Minimum bars between pivots.
	/// </summary>
	public int RangeLower { get => _rangeLower.Value; set => _rangeLower.Value = value; }
	
	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of <see cref="DivergenceIndicatorAnyOscillatorStrategy"/>.
	/// </summary>
	public DivergenceIndicatorAnyOscillatorStrategy()
	{
		_lbLeft = Param(nameof(LbLeft), 5)
		.SetGreaterThanZero()
		.SetDisplay("Pivot Left", "Bars to the left", "Divergence");
		
		_lbRight = Param(nameof(LbRight), 5)
		.SetGreaterThanZero()
		.SetDisplay("Pivot Right", "Bars to the right", "Divergence");
		
		_rangeUpper = Param(nameof(RangeUpper), 60)
		.SetGreaterThanZero()
		.SetDisplay("Max Range", "Maximum bars between pivots", "Divergence");
		
		_rangeLower = Param(nameof(RangeLower), 5)
		.SetGreaterThanZero()
		.SetDisplay("Min Range", "Minimum bars between pivots", "Divergence");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "Oscillator")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 7);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		
		var len = LbLeft + LbRight + 1;
		_oscValues = new decimal?[len];
		_highs = new decimal?[len];
		_lows = new decimal?[len];
		_sinceLastPivotLow = RangeUpper + 1;
		_sinceLastPivotHigh = RangeUpper + 1;
		_lastOscPivotLow = null;
		_lastPricePivotLow = null;
		_lastOscPivotHigh = null;
		_lastPricePivotHigh = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(rsi, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal osc)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var len = _oscValues.Length;
		for (var i = 0; i < len - 1; i++)
		{
			_oscValues[i] = _oscValues[i + 1];
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}
		
		_oscValues[len - 1] = osc;
		_highs[len - 1] = candle.HighPrice;
		_lows[len - 1] = candle.LowPrice;
		
		_sinceLastPivotLow++;
		_sinceLastPivotHigh++;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_oscValues[0] is not decimal)
		return;
		
		var center = LbRight;
		var oscCenter = (decimal)_oscValues[center]!;
		var lowCenter = (decimal)_lows[center]!;
		var highCenter = (decimal)_highs[center]!;
		
		var isOscPivotLow = true;
		for (var i = 0; i < LbLeft; i++)
		{
			if (_oscValues[center - 1 - i] is decimal left && left <= oscCenter)
			isOscPivotLow = false;
		}
		for (var i = 1; i <= LbRight && isOscPivotLow; i++)
		{
			if (_oscValues[center + i] is decimal right && right <= oscCenter)
			isOscPivotLow = false;
		}
		
		var isPricePivotLow = true;
		for (var i = 0; i < LbLeft; i++)
		{
			if (_lows[center - 1 - i] is decimal left && left <= lowCenter)
			isPricePivotLow = false;
		}
		for (var i = 1; i <= LbRight && isPricePivotLow; i++)
		{
			if (_lows[center + i] is decimal right && right <= lowCenter)
			isPricePivotLow = false;
		}
		
		if (isOscPivotLow && isPricePivotLow)
		{
			if (_lastOscPivotLow is decimal lastOsc && _lastPricePivotLow is decimal lastPrice &&
			_sinceLastPivotLow >= RangeLower && _sinceLastPivotLow <= RangeUpper)
			{
				if (lowCenter < lastPrice && oscCenter > lastOsc && Position <= 0)
				BuyMarket();
				else if (lowCenter > lastPrice && oscCenter < lastOsc && Position >= 0)
				SellMarket();
			}
			
			_lastOscPivotLow = oscCenter;
			_lastPricePivotLow = lowCenter;
			_sinceLastPivotLow = 0;
		}
		
		var isOscPivotHigh = true;
		for (var i = 0; i < LbLeft; i++)
		{
			if (_oscValues[center - 1 - i] is decimal left && left >= oscCenter)
			isOscPivotHigh = false;
		}
		for (var i = 1; i <= LbRight && isOscPivotHigh; i++)
		{
			if (_oscValues[center + i] is decimal right && right >= oscCenter)
			isOscPivotHigh = false;
		}
		
		var isPricePivotHigh = true;
		for (var i = 0; i < LbLeft; i++)
		{
			if (_highs[center - 1 - i] is decimal left && left >= highCenter)
			isPricePivotHigh = false;
		}
		for (var i = 1; i <= LbRight && isPricePivotHigh; i++)
		{
			if (_highs[center + i] is decimal right && right >= highCenter)
			isPricePivotHigh = false;
		}
		
		if (isOscPivotHigh && isPricePivotHigh)
		{
			if (_lastOscPivotHigh is decimal lastOsc && _lastPricePivotHigh is decimal lastPrice &&
			_sinceLastPivotHigh >= RangeLower && _sinceLastPivotHigh <= RangeUpper)
			{
				if (highCenter > lastPrice && oscCenter < lastOsc && Position >= 0)
				SellMarket();
				else if (highCenter < lastPrice && oscCenter > lastOsc && Position <= 0)
				BuyMarket();
			}
			
			_lastOscPivotHigh = oscCenter;
			_lastPricePivotHigh = highCenter;
			_sinceLastPivotHigh = 0;
		}
	}
}
