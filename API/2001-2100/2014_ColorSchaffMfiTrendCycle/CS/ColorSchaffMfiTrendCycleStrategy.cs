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
/// Strategy based on the Color Schaff MoneyFlowIndex Trend Cycle indicator.
/// </summary>
public class ColorSchaffMfiTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMfiPeriod;
	private readonly StrategyParam<int> _slowMfiPeriod;
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	
	private MoneyFlowIndex _fastMfi;
	private MoneyFlowIndex _slowMfi;
	private decimal[] _macd;
	private decimal[] _st;
	private int _index;
	private int _valuesCount;
	private bool _st1;
	private bool _st2;
	private decimal _prevStc;
	private int _prevColor;
	private int _cooldownRemaining;
	
	public int FastMfiPeriod { get => _fastMfiPeriod.Value; set => _fastMfiPeriod.Value = value; }
	public int SlowMfiPeriod { get => _slowMfiPeriod.Value; set => _slowMfiPeriod.Value = value; }
	public int CycleLength { get => _cycleLength.Value; set => _cycleLength.Value = value; }
	public int HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public int LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public ColorSchaffMfiTrendCycleStrategy()
	{
		_fastMfiPeriod = Param(nameof(FastMfiPeriod), 23)
		.SetDisplay("Fast MoneyFlowIndex", "Fast MoneyFlowIndex period", "Indicator");
		
		_slowMfiPeriod = Param(nameof(SlowMfiPeriod), 50)
		.SetDisplay("Slow MoneyFlowIndex", "Slow MoneyFlowIndex period", "Indicator");
		
		_cycleLength = Param(nameof(CycleLength), 10)
		.SetDisplay("Cycle Length", "Cycle length for STC", "Indicator");
		
		_highLevel = Param(nameof(HighLevel), 60)
		.SetDisplay("High Level", "Overbought threshold", "Indicator");
		
		_lowLevel = Param(nameof(LowLevel), -60)
		.SetDisplay("Low Level", "Oversold threshold", "Indicator");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Candles timeframe", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_fastMfi = null;
		_slowMfi = null;
		_macd = null;
		_st = null;
		_index = 0;
		_valuesCount = 0;
		_st1 = false;
		_st2 = false;
		_prevStc = 0m;
		_prevColor = 0;
		_cooldownRemaining = 0;
	}
	
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		
		_fastMfi = new MoneyFlowIndex { Length = FastMfiPeriod };
		_slowMfi = new MoneyFlowIndex { Length = SlowMfiPeriod };
		_macd = new decimal[CycleLength];
		_st = new decimal[CycleLength];
		_cooldownRemaining = 0;
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMfi, _slowMfi, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastMfi, decimal slowMfi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;
		
		var color = CalculateColor(fastMfi, slowMfi);
		
		if (_cooldownRemaining == 0 && _prevColor == 6 && color == 7 && Position <= 0)
		{
			if (Position < 0)
			BuyMarket();
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && _prevColor == 1 && color == 0 && Position >= 0)
		{
			if (Position > 0)
			SellMarket();
			SellMarket();
			_cooldownRemaining = SignalCooldownBars;
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
