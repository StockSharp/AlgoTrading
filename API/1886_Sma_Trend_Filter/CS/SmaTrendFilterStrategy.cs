using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe SMA trend filter strategy.
/// </summary>
public class SmaTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _openLevel;
	private readonly StrategyParam<int> _closeLevel;
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;
	
	private readonly int[] _periods = { 5, 8, 13, 21, 34 };
	private readonly Sma[][] _smas = new Sma[3][];
	private readonly decimal[][] _previous = new decimal[3][];
	private readonly decimal[] _uitog = new decimal[3];
	private readonly decimal[] _ditog = new decimal[3];
	private int _signal;
	
	/// <summary>
	/// Signal threshold to open position.
	/// </summary>
	public int OpenLevel
	{
		get => _openLevel.Value;
		set => _openLevel.Value = value;
}

/// <summary>
/// Signal threshold to close position.
/// </summary>
public int CloseLevel
{
	get => _closeLevel.Value;
	set => _closeLevel.Value = value;
}

/// <summary>
/// Primary timeframe for calculations.
/// </summary>
public DataType CandleType1
{
	get => _candleType1.Value;
	set => _candleType1.Value = value;
}

/// <summary>
/// Secondary timeframe for calculations.
/// </summary>
public DataType CandleType2
{
	get => _candleType2.Value;
	set => _candleType2.Value = value;
}

/// <summary>
/// Tertiary timeframe for calculations.
/// </summary>
public DataType CandleType3
{
	get => _candleType3.Value;
	set => _candleType3.Value = value;
}

/// <summary>
/// Initializes a new instance of the strategy.
/// </summary>
public SmaTrendFilterStrategy()
{
	_openLevel = Param(nameof(OpenLevel), 0)
	.SetDisplay("Open Level", "Signal threshold to open position", "Trading");
	_closeLevel = Param(nameof(CloseLevel), 0)
	.SetDisplay("Close Level", "Signal threshold to close position", "Trading");
	_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Candle Type 1", "Primary timeframe", "General");
	_candleType2 = Param(nameof(CandleType2), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("Candle Type 2", "Secondary timeframe", "General");
	_candleType3 = Param(nameof(CandleType3), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Candle Type 3", "Tertiary timeframe", "General");
	
	for (var i = 0; i < 3; i++)
	{
		_smas[i] = new Sma[_periods.Length];
		_previous[i] = new decimal[_periods.Length];
		for (var j = 0; j < _periods.Length; j++)
		_smas[i][j] = new Sma { Length = _periods[j] };
}
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);
	
	var sub1 = SubscribeCandles(CandleType1);
	sub1.Bind(_smas[0][0], _smas[0][1], _smas[0][2], _smas[0][3], _smas[0][4], ProcessTf1).Start();
	
	var sub2 = SubscribeCandles(CandleType2);
	sub2.Bind(_smas[1][0], _smas[1][1], _smas[1][2], _smas[1][3], _smas[1][4], ProcessTf2).Start();
	
	var sub3 = SubscribeCandles(CandleType3);
	sub3.Bind(_smas[2][0], _smas[2][1], _smas[2][2], _smas[2][3], _smas[2][4], ProcessTf3).Start();
}

private void ProcessTf1(ICandleMessage candle, decimal sma5, decimal sma8, decimal sma13, decimal sma21, decimal sma34)
=> ProcessTf(0, candle, new[] { sma5, sma8, sma13, sma21, sma34 });

private void ProcessTf2(ICandleMessage candle, decimal sma5, decimal sma8, decimal sma13, decimal sma21, decimal sma34)
=> ProcessTf(1, candle, new[] { sma5, sma8, sma13, sma21, sma34 });

private void ProcessTf3(ICandleMessage candle, decimal sma5, decimal sma8, decimal sma13, decimal sma21, decimal sma34)
=> ProcessTf(2, candle, new[] { sma5, sma8, sma13, sma21, sma34 });

private void ProcessTf(int index, ICandleMessage candle, decimal[] values)
{
	if (candle.State != CandleStates.Finished)
	return;
	
	var up = 0;
	var down = 0;
	
	for (var i = 0; i < _periods.Length; i++)
	{
		var val = values[i];
		
		if (val == 0m)
		return;
		
		var prev = _previous[index][i];
		
		if (prev == 0m)
		{
			_previous[index][i] = val;
			return;
	}
	
	if (val > prev)
	up++;
	else if (val < prev)
	down++;
	
	_previous[index][i] = val;
}

_uitog[index] = up / (decimal)_periods.Length * 100m;
_ditog[index] = down / (decimal)_periods.Length * 100m;

EvaluateSignal();
}

private void EvaluateSignal()
{
	_signal = 0;
	
	if (_uitog[0] >= 75m && _uitog[1] >= 75m && _uitog[2] >= 75m)
	_signal = 2;
	else if (_ditog[0] >= 75m && _ditog[1] >= 75m && _ditog[2] >= 75m)
	_signal = -2;
	else if (_uitog[0] > 50m && _uitog[1] > 50m && _uitog[2] > 50m)
	_signal = 1;
	else if (_ditog[0] > 50m && _ditog[1] > 50m && _ditog[2] > 50m)
	_signal = -1;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	var openBuy = _signal > OpenLevel;
	var openSell = _signal < -OpenLevel;
	var closeBuy = _signal < -CloseLevel;
	var closeSell = _signal > CloseLevel;
	
	if (Position > 0 && closeBuy)
	SellMarket();
	
	if (Position < 0 && closeSell)
	BuyMarket();
	
	if (openBuy && Position <= 0 && !closeBuy)
	BuyMarket();
	
	if (openSell && Position >= 0 && !closeSell)
	SellMarket();
}
}
