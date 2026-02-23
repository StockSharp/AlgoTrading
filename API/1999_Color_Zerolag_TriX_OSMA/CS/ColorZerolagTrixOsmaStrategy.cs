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
	/// Strategy based on a zero-lag TRIX OSMA oscillator.
	/// Combines several TRIX values to detect trend reversals.
	/// </summary>
public class ColorZerolagTrixOsmaStrategy : Strategy
	{
	private readonly StrategyParam<int> _smoothing1;
	private readonly StrategyParam<int> _smoothing2;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<decimal> _factor4;
	private readonly StrategyParam<decimal> _factor5;
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<int> _period3;
	private readonly StrategyParam<int> _period4;
	private readonly StrategyParam<int> _period5;
	private readonly StrategyParam<DataType> _candleType;

	private TripleExponentialMovingAverage _tema1 = null!;
	private TripleExponentialMovingAverage _tema2 = null!;
	private TripleExponentialMovingAverage _tema3 = null!;
	private TripleExponentialMovingAverage _tema4 = null!;
	private TripleExponentialMovingAverage _tema5 = null!;
	private RateOfChange _roc1 = null!;
	private RateOfChange _roc2 = null!;
	private RateOfChange _roc3 = null!;
	private RateOfChange _roc4 = null!;
	private RateOfChange _roc5 = null!;

	private decimal _prevSlow;
	private decimal _prevOsma;
	private decimal _prev1;
	private decimal _prev2;
	private decimal _smoothConst1;
	private decimal _smoothConst2;
	private bool _initialized;

	/// <summary>
	/// Slow trend smoothing factor.
	/// </summary>
	public int Smoothing1
	{
	get => _smoothing1.Value;
	set => _smoothing1.Value = value;
}

	/// <summary>
	/// OSMA smoothing factor.
	/// </summary>
	public int Smoothing2
	{
	get => _smoothing2.Value;
	set => _smoothing2.Value = value;
}

	/// <summary>
	/// Weight for first TRIX component.
	/// </summary>
	public decimal Factor1
	{
	get => _factor1.Value;
	set => _factor1.Value = value;
}

	/// <summary>
	/// Weight for second TRIX component.
	/// </summary>
	public decimal Factor2
	{
	get => _factor2.Value;
	set => _factor2.Value = value;
}

	/// <summary>
	/// Weight for third TRIX component.
	/// </summary>
	public decimal Factor3
	{
	get => _factor3.Value;
	set => _factor3.Value = value;
}

	/// <summary>
	/// Weight for fourth TRIX component.
	/// </summary>
	public decimal Factor4
	{
	get => _factor4.Value;
	set => _factor4.Value = value;
}

	/// <summary>
	/// Weight for fifth TRIX component.
	/// </summary>
	public decimal Factor5
	{
	get => _factor5.Value;
	set => _factor5.Value = value;
}

	/// <summary>
	/// Period for first TRIX.
	/// </summary>
	public int Period1
	{
	get => _period1.Value;
	set => _period1.Value = value;
}

	/// <summary>
	/// Period for second TRIX.
	/// </summary>
	public int Period2
	{
	get => _period2.Value;
	set => _period2.Value = value;
}

	/// <summary>
	/// Period for third TRIX.
	/// </summary>
	public int Period3
	{
	get => _period3.Value;
	set => _period3.Value = value;
}

	/// <summary>
	/// Period for fourth TRIX.
	/// </summary>
	public int Period4
	{
	get => _period4.Value;
	set => _period4.Value = value;
}

	/// <summary>
	/// Period for fifth TRIX.
	/// </summary>
	public int Period5
	{
	get => _period5.Value;
	set => _period5.Value = value;
}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorZerolagTrixOsmaStrategy"/>.
	/// </summary>
	public ColorZerolagTrixOsmaStrategy()
	{
	_smoothing1 = Param(nameof(Smoothing1), 15).SetDisplay("Smoothing1", "Slow trend smoothing", "Indicator");
	_smoothing2 = Param(nameof(Smoothing2), 7).SetDisplay("Smoothing2", "OSMA smoothing", "Indicator");
	_factor1 = Param(nameof(Factor1), 0.05m).SetDisplay("Factor1", "Weight for first TRIX", "Indicator");
	_period1 = Param(nameof(Period1), 8).SetDisplay("Period1", "TRIX 1 period", "Indicator");
	_factor2 = Param(nameof(Factor2), 0.10m).SetDisplay("Factor2", "Weight for second TRIX", "Indicator");
	_period2 = Param(nameof(Period2), 21).SetDisplay("Period2", "TRIX 2 period", "Indicator");
	_factor3 = Param(nameof(Factor3), 0.16m).SetDisplay("Factor3", "Weight for third TRIX", "Indicator");
	_period3 = Param(nameof(Period3), 34).SetDisplay("Period3", "TRIX 3 period", "Indicator");
	_factor4 = Param(nameof(Factor4), 0.26m).SetDisplay("Factor4", "Weight for fourth TRIX", "Indicator");
	_period4 = Param(nameof(Period4), 55).SetDisplay("Period4", "TRIX 4 period", "Indicator");
	_factor5 = Param(nameof(Factor5), 0.43m).SetDisplay("Factor5", "Weight for fifth TRIX", "Indicator");
	_period5 = Param(nameof(Period5), 89).SetDisplay("Period5", "TRIX 5 period", "Indicator");
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();
	_prevSlow = 0m;
	_prevOsma = 0m;
	_prev1 = 0m;
	_prev2 = 0m;
	_initialized = false;
}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
	base.OnStarted2(time);

	_tema1 = new TripleExponentialMovingAverage { Length = Period1 };
	_tema2 = new TripleExponentialMovingAverage { Length = Period2 };
	_tema3 = new TripleExponentialMovingAverage { Length = Period3 };
	_tema4 = new TripleExponentialMovingAverage { Length = Period4 };
	_tema5 = new TripleExponentialMovingAverage { Length = Period5 };

	_roc1 = new RateOfChange { Length = 1 };
	_roc2 = new RateOfChange { Length = 1 };
	_roc3 = new RateOfChange { Length = 1 };
	_roc4 = new RateOfChange { Length = 1 };
	_roc5 = new RateOfChange { Length = 1 };

	_smoothConst1 = (Smoothing1 - 1m) / Smoothing1;
	_smoothConst2 = (Smoothing2 - 1m) / Smoothing2;

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();

}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsOnline)
	return;

	var time = candle.OpenTime;
	var tema1Val = _tema1.Process(candle.ClosePrice, time, true).ToDecimal();
	var tema2Val = _tema2.Process(candle.ClosePrice, time, true).ToDecimal();
	var tema3Val = _tema3.Process(candle.ClosePrice, time, true).ToDecimal();
	var tema4Val = _tema4.Process(candle.ClosePrice, time, true).ToDecimal();
	var tema5Val = _tema5.Process(candle.ClosePrice, time, true).ToDecimal();

	var t1Val = _roc1.Process(tema1Val, time, true).ToDecimal();
	var t2Val = _roc2.Process(tema2Val, time, true).ToDecimal();
	var t3Val = _roc3.Process(tema3Val, time, true).ToDecimal();
	var t4Val = _roc4.Process(tema4Val, time, true).ToDecimal();
	var t5Val = _roc5.Process(tema5Val, time, true).ToDecimal();

	if (!_roc1.IsFormed || !_roc2.IsFormed || !_roc3.IsFormed || !_roc4.IsFormed || !_roc5.IsFormed)
	return;

	var osc1 = Factor1 * t1Val;
	var osc2 = Factor2 * t2Val;
	var osc3 = Factor3 * t3Val;
	var osc4 = Factor4 * t4Val;
	var osc5 = Factor5 * t5Val;

	var fast = osc1 + osc2 + osc3 + osc4 + osc5;
	var slow = fast / Smoothing1 + _prevSlow * _smoothConst1;
	var osma = (fast - slow) / Smoothing2 + _prevOsma * _smoothConst2;

	if (!_initialized)
	{
	_prevSlow = slow;
	_prevOsma = osma;
	_prev1 = osma;
	_prev2 = osma;
	_initialized = true;
	return;
}

	var crossUp = _prev1 < _prev2 && osma > _prev1;
	var crossDown = _prev1 > _prev2 && osma < _prev1;

	if (crossUp && Position <= 0)
	{
	if (Position < 0)
	BuyMarket(Math.Abs(Position));
	BuyMarket();
}
	else if (crossDown && Position >= 0)
	{
	if (Position > 0)
	SellMarket(Position);
	SellMarket();
}

	_prevSlow = slow;
	_prevOsma = osma;
	_prev2 = _prev1;
	_prev1 = osma;
}
}
