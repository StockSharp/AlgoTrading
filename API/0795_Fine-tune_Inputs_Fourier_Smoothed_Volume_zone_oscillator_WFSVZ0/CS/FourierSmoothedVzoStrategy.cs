using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a Fourier smoothed Volume Zone Oscillator.
/// Buys when the oscillator is rising above the threshold and
/// sells when it falls below the negative threshold.
/// </summary>
public class FourierSmoothedVzoStrategy : Strategy
{
	private readonly StrategyParam<int> _vzoLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _emaNum;
	private decimal _emaDen;
	private decimal _emaVzo;
	private decimal _prevClose;
	private decimal _prevVzo;
	private bool _isFirst = true;
	
	/// <summary>
	/// VZO EMA length.
	/// </summary>
	public int VzoLength
	{
	get => _vzoLength.Value;
	set => _vzoLength.Value = value;
	}
	
	/// <summary>
	/// Smoothing length for VZO.
	/// </summary>
	public int SmoothLength
	{
	get => _smoothLength.Value;
	set => _smoothLength.Value = value;
	}
	
	/// <summary>
	/// Threshold for VZO.
	/// </summary>
	public decimal Threshold
	{
	get => _threshold.Value;
	set => _threshold.Value = value;
	}
	
	/// <summary>
	/// Close position when no signal.
	/// </summary>
	public bool CloseAllPositions
	{
	get => _closeAll.Value;
	set => _closeAll.Value = value;
	}
	
	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public FourierSmoothedVzoStrategy()
	{
	_vzoLength = Param(nameof(VzoLength), 2)
	.SetGreaterThanZero()
	.SetDisplay("VZO Length", "Length for VZO EMA", "Indicators")
	.SetCanOptimize(true);
	
	_smoothLength = Param(nameof(SmoothLength), 2)
	.SetGreaterThanZero()
	.SetDisplay("Smooth Length", "Length for smoothing", "Indicators")
	.SetCanOptimize(true);
	
	_threshold = Param(nameof(Threshold), 0m)
	.SetDisplay("Threshold", "VZO threshold", "General")
	.SetCanOptimize(true);
	
	_closeAll = Param(nameof(CloseAllPositions), true)
	.SetDisplay("Close All", "Close position when no signal", "General");
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
	
	_emaNum = default;
	_emaDen = default;
	_emaVzo = default;
	_prevClose = default;
	_prevVzo = default;
	_isFirst = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	SubscribeCandles(CandleType)
	.Bind(ProcessCandle)
	.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	var direction = 0m;
	if (!_isFirst)
	{
	if (candle.ClosePrice > _prevClose)
	direction = 1m;
	else if (candle.ClosePrice < _prevClose)
	direction = -1m;
	}
	else
	{
	_isFirst = false;
	}
	
	var alphaVzo = 2m / (VzoLength + 1m);
	_emaNum += alphaVzo * (direction * candle.TotalVolume - _emaNum);
	_emaDen += alphaVzo * (candle.TotalVolume - _emaDen);
	
	if (_emaDen == 0m)
	{
	_prevClose = candle.ClosePrice;
	return;
	}
	
	var vzo = 100m * _emaNum / _emaDen;
	
	var alphaSmooth = 2m / (SmoothLength + 1m);
	_emaVzo += alphaSmooth * (vzo - _emaVzo);
	
	var green = SmoothLength < 2 ? vzo > _prevVzo : vzo > _emaVzo;
	var red = SmoothLength < 2 ? vzo < _prevVzo : vzo < _emaVzo;
	
	if (green && vzo > Threshold && Position <= 0)
	{
	BuyMarket();
	}
	else if (red && vzo < -Threshold && Position >= 0)
	{
	SellMarket();
	}
	else if (CloseAllPositions && Position != 0 && !green && !red)
	{
	ClosePosition();
	}
	
	_prevClose = candle.ClosePrice;
	_prevVzo = vzo;
	}
	}
	
