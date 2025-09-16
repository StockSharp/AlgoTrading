using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Schaff MFI Trend Cycle indicator.
/// </summary>
public class ColorSchaffMfiTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMfiPeriod;
	private readonly StrategyParam<int> _slowMfiPeriod;
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;
	
	private MFI _fastMfi;
	private MFI _slowMfi;
	private decimal[] _macd;
	private decimal[] _st;
	private int _index;
	private int _valuesCount;
	private bool _st1;
	private bool _st2;
	private decimal _prevStc;
	private int _prevColor;
	
	public int FastMfiPeriod { get => _fastMfiPeriod.Value; set => _fastMfiPeriod.Value = value; }
	public int SlowMfiPeriod { get => _slowMfiPeriod.Value; set => _slowMfiPeriod.Value = value; }
	public int CycleLength { get => _cycleLength.Value; set => _cycleLength.Value = value; }
	public int HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public int LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public ColorSchaffMfiTrendCycleStrategy()
	{
		_fastMfiPeriod = Param(nameof(FastMfiPeriod), 23)
		.SetDisplay("Fast MFI", "Fast MFI period", "Indicator");
		
		_slowMfiPeriod = Param(nameof(SlowMfiPeriod), 50)
		.SetDisplay("Slow MFI", "Slow MFI period", "Indicator");
		
		_cycleLength = Param(nameof(CycleLength), 10)
		.SetDisplay("Cycle Length", "Cycle length for STC", "Indicator");
		
		_highLevel = Param(nameof(HighLevel), 60)
		.SetDisplay("High Level", "Overbought threshold", "Indicator");
		
		_lowLevel = Param(nameof(LowLevel), -60)
		.SetDisplay("Low Level", "Oversold threshold", "Indicator");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles timeframe", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastMfi = new MFI { Length = FastMfiPeriod };
		_slowMfi = new MFI { Length = SlowMfiPeriod };
		_macd = new decimal[CycleLength];
		_st = new decimal[CycleLength];
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMfi, _slowMfi, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastMfi, decimal slowMfi)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var color = CalculateColor(fastMfi, slowMfi);
		
		if (_prevColor > 5)
		{
			if (Position < 0)
			BuyMarket();
			if (color < 6 && Position <= 0)
			BuyMarket();
		}
		else if (_prevColor < 2)
		{
			if (Position > 0)
			SellMarket();
			if (color > 1 && Position >= 0)
			SellMarket();
		}
		
		_prevColor = color;
	}
	
	private int CalculateColor(decimal fastMfi, decimal slowMfi)
	{
		var diff = fastMfi - slowMfi;
		_macd[_index] = diff;
		
		var count = _valuesCount < CycleLength ? _valuesCount + 1 : CycleLength;
		GetMinMax(_macd, count, out var llv, out var hhv);
		
		var prevIndex = (_index - 1 + CycleLength) % CycleLength;
		var stPrev = _st[prevIndex];
		var st = hhv != llv ? (diff - llv) / (hhv - llv) * 100m : stPrev;
		if (_st1 && _valuesCount > 0)
		st = 0.5m * (st - stPrev) + stPrev;
		_st1 = true;
		_st[_index] = st;
		
		GetMinMax(_st, count, out llv, out hhv);
		var stcPrev = _prevStc;
		var stc = hhv != llv ? (st - llv) / (hhv - llv) * 200m - 100m : stcPrev;
		if (_st2 && _valuesCount > 0)
		stc = 0.5m * (stc - stcPrev) + stcPrev;
		_st2 = true;
		
		var dStc = stc - stcPrev;
		_prevStc = stc;
		
		_index = (_index + 1) % CycleLength;
		if (_valuesCount < CycleLength)
		_valuesCount++;
		
		int color;
		
		if (stc > 0)
		{
			if (stc > HighLevel)
			color = dStc >= 0 ? 7 : 6;
			else
			color = dStc >= 0 ? 5 : 4;
		}
		else
		{
			if (stc < LowLevel)
			color = dStc < 0 ? 0 : 1;
			else
			color = dStc < 0 ? 2 : 3;
		}
		
		return color;
	}
	
	private static void GetMinMax(decimal[] buffer, int count, out decimal min, out decimal max)
	{
		min = buffer[0];
		max = buffer[0];
		
		for (var i = 1; i < count; i++)
		{
			var val = buffer[i];
			if (val < min)
			min = val;
			if (val > max)
			max = val;
		}
	}
}
