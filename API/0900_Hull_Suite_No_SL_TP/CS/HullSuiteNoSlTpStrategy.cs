using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Suite strategy with optional HMA variants.
/// Enters long when hull rises, short when it falls.
/// </summary>
public class HullSuiteNoSlTpStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<HullMode> _mode;
	private readonly StrategyParam<DataType> _candleType;
	
	private HullMovingAverage _hma;
	private ExponentialMovingAverage _emaHalf;
	private ExponentialMovingAverage _emaFull;
	private ExponentialMovingAverage _emaFinal;
	private WeightedMovingAverage _wmaThird;
	private WeightedMovingAverage _wmaHalf;
	private WeightedMovingAverage _wmaFull;
	private WeightedMovingAverage _wmaFinal;
	
	private decimal? _prev1;
	private decimal? _prev2;
	
	/// <summary>
	/// Hull length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}
	
	/// <summary>
	/// Hull variation.
	/// </summary>
	public HullMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="HullSuiteNoSlTpStrategy"/>.
	/// </summary>
	public HullSuiteNoSlTpStrategy()
	{
		_length = Param(nameof(Length), 55)
		.SetDisplay("Hull Length", "Period for Hull calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 5);
		
		_mode = Param(nameof(Mode), HullMode.Hma)
		.SetDisplay("Hull Mode", "Hull variation", "Indicators");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe of data for strategy", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_hma?.Reset();
		_emaHalf?.Reset();
		_emaFull?.Reset();
		_emaFinal?.Reset();
		_wmaThird?.Reset();
		_wmaHalf?.Reset();
		_wmaFull?.Reset();
		_wmaFinal?.Reset();
		
		_prev1 = null;
		_prev2 = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		
		switch (Mode)
		{
		case HullMode.Hma:
		_hma = new HullMovingAverage { Length = Length };
		subscription.Bind(_hma, ProcessHma).Start();
		break;
		
	case HullMode.Ehma:
	_emaHalf = new ExponentialMovingAverage { Length = Math.Max(1, Length / 2) };
	_emaFull = new ExponentialMovingAverage { Length = Length };
	_emaFinal = new ExponentialMovingAverage { Length = (int)Math.Round(Math.Sqrt(Length)) };
	subscription.BindEx(_emaHalf, _emaFull, ProcessEhma).Start();
	break;
	
case HullMode.Thma:
_wmaThird = new WeightedMovingAverage { Length = Math.Max(1, Length / 3) };
_wmaHalf = new WeightedMovingAverage { Length = Math.Max(1, Length / 2) };
_wmaFull = new WeightedMovingAverage { Length = Length };
_wmaFinal = new WeightedMovingAverage { Length = Length };
subscription.BindEx(_wmaThird, _wmaHalf, _wmaFull, ProcessThma).Start();
break;
}

var area = CreateChartArea();
if (area != null)
{
	DrawCandles(area, subscription);
	if (_hma != null)
	DrawIndicator(area, _hma);
	else if (_emaFinal != null)
	DrawIndicator(area, _emaFinal);
	else if (_wmaFinal != null)
	DrawIndicator(area, _wmaFinal);
	DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessHma(ICandleMessage candle, decimal hull)
{
	if (candle.State != CandleStates.Finished)
	return;
	
	ProcessHull(candle, hull);
}

private void ProcessEhma(ICandleMessage candle, IIndicatorValue emaHalfValue, IIndicatorValue emaFullValue)
{
	if (candle.State != CandleStates.Finished)
	return;
	
	var emaHalf = emaHalfValue.ToDecimal();
	var emaFull = emaFullValue.ToDecimal();
	var input = 2m * emaHalf - emaFull;
	
	var value = _emaFinal.Process(input, candle.ServerTime, true).ToDecimal();
	ProcessHull(candle, value);
}

private void ProcessThma(ICandleMessage candle, IIndicatorValue wmaThirdValue, IIndicatorValue wmaHalfValue, IIndicatorValue wmaFullValue)
{
	if (candle.State != CandleStates.Finished)
	return;
	
	var wmaThird = wmaThirdValue.ToDecimal();
	var wmaHalf = wmaHalfValue.ToDecimal();
	var wmaFull = wmaFullValue.ToDecimal();
	var input = 3m * wmaThird - wmaHalf - wmaFull;
	
	var value = _wmaFinal.Process(input, candle.ServerTime, true).ToDecimal();
	ProcessHull(candle, value);
}

private void ProcessHull(ICandleMessage candle, decimal hull)
{
	if (!_prev1.HasValue)
	{
		_prev1 = hull;
		return;
	}
	
	if (!_prev2.HasValue)
	{
		_prev2 = _prev1;
		_prev1 = hull;
		return;
	}
	
	var isBull = hull > _prev2.Value;
	var isBear = hull < _prev2.Value;
	
	if (IsFormedAndOnlineAndAllowTrading())
	{
		if (isBull && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (isBear && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
	}
	
	_prev2 = _prev1;
	_prev1 = hull;
}

/// <summary>
/// Hull modes.
/// </summary>
public enum HullMode
{
	/// <summary>Traditional Hull Moving Average.</summary>
	Hma,
	/// <summary>Exponential Hull Moving Average.</summary>
	Ehma,
	/// <summary>Triple Hull Moving Average.</summary>
	Thma
}
}
