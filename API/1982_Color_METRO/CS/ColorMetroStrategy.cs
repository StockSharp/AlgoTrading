
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ColorMETRO strategy based on RSI step channels.
/// Opens long when fast line crosses above slow line and vice versa.
/// </summary>
public class ColorMetroStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastStep;
	private readonly StrategyParam<int> _slowStep;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _buyOpenAllowed;
	private readonly StrategyParam<bool> _sellOpenAllowed;
	private readonly StrategyParam<bool> _buyCloseAllowed;
	private readonly StrategyParam<bool> _sellCloseAllowed;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMPlus;
	private decimal? _prevMMinus;

	public ColorMetroStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicator");

		_fastStep = Param(nameof(FastStep), 5)
			.SetDisplay("Fast Step", "Step size for fast line", "Indicator");

		_slowStep = Param(nameof(SlowStep), 15)
			.SetDisplay("Slow Step", "Step size for slow line", "Indicator");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_buyOpenAllowed = Param(nameof(BuyOpenAllowed), true)
			.SetDisplay("Allow Buy", "Permission to open long positions", "Trading");

		_sellOpenAllowed = Param(nameof(SellOpenAllowed), true)
			.SetDisplay("Allow Sell", "Permission to open short positions", "Trading");

		_buyCloseAllowed = Param(nameof(BuyCloseAllowed), true)
			.SetDisplay("Close Short", "Permission to close short positions", "Trading");

		_sellCloseAllowed = Param(nameof(SellCloseAllowed), true)
			.SetDisplay("Close Long", "Permission to close long positions", "Trading");
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Fast step size.
	/// </summary>
	public int FastStep
	{
		get => _fastStep.Value;
		set => _fastStep.Value = value;
	}

	/// <summary>
	/// Slow step size.
	/// </summary>
	public int SlowStep
	{
		get => _slowStep.Value;
		set => _slowStep.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpenAllowed
	{
		get => _buyOpenAllowed.Value;
		set => _buyOpenAllowed.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpenAllowed
	{
		get => _sellOpenAllowed.Value;
		set => _sellOpenAllowed.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool SellCloseAllowed
	{
		get => _sellCloseAllowed.Value;
		set => _sellCloseAllowed.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool BuyCloseAllowed
	{
		get => _buyCloseAllowed.Value;
		set => _buyCloseAllowed.Value = value;
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

	_prevMPlus = null;
	_prevMMinus = null;

	var indicator = new ColorMetroIndicator
	{
	RsiPeriod = RsiPeriod,
	FastStep = FastStep,
	SlowStep = SlowStep,
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(indicator, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, indicator);
	DrawOwnTrades(area);
	}

	var step = Security.PriceStep ?? 1m;
	StartProtection(
	takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Point),
	stopLoss: new Unit(StopLossPoints * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
	if (candle.State != CandleStates.Finished || !value.IsFinal)
	return;

	var cm = (ColorMetroValue)value;
	var mPlus = cm.MPlus;
	var mMinus = cm.MMinus;

	if (_prevMPlus is null || _prevMMinus is null)
	{
	_prevMPlus = mPlus;
	_prevMMinus = mMinus;
	return;
	}

	var crossUp = _prevMPlus <= _prevMMinus && mPlus > mMinus;
	var crossDown = _prevMPlus >= _prevMMinus && mPlus < mMinus;

	if (crossUp)
	{
	if (SellCloseAllowed && Position < 0)
	BuyMarket(Math.Abs(Position));

	if (BuyOpenAllowed && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	}
	else if (crossDown)
	{
	if (BuyCloseAllowed && Position > 0)
	SellMarket(Position);

	if (SellOpenAllowed && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));
	}

	_prevMPlus = mPlus;
	_prevMMinus = mMinus;
	}
}

/// <summary>
/// ColorMETRO indicator producing fast and slow lines based on RSI.
/// </summary>
public class ColorMetroIndicator : BaseIndicator<decimal>
{
	public int RsiPeriod { get; set; } = 7;
	public int FastStep { get; set; } = 5;
	public int SlowStep { get; set; } = 15;

	private RelativeStrengthIndex _rsi;
	private bool _initialized;
	private decimal _fmin1;
	private decimal _fmax1;
	private decimal _smin1;
	private decimal _smax1;
	private int _ftrend;
	private int _strend;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
	_rsi ??= new RelativeStrengthIndex { Length = RsiPeriod };

	var rsiVal = _rsi.Process(input);
	if (!_rsi.IsFormed)
	return new ColorMetroValue(this, input, default, default);

	var rsi = rsiVal.GetValue<decimal>();

	var fmax0 = rsi + 2m * FastStep;
	var fmin0 = rsi - 2m * FastStep;
	var smax0 = rsi + 2m * SlowStep;
	var smin0 = rsi - 2m * SlowStep;

	if (!_initialized)
	{
	_fmin1 = fmin0;
	_fmax1 = fmax0;
	_smin1 = smin0;
	_smax1 = smax0;
	_initialized = true;
	}

	if (rsi > _fmax1) _ftrend = +1;
	if (rsi < _fmin1) _ftrend = -1;

	if (_ftrend > 0 && fmin0 < _fmin1) fmin0 = _fmin1;
	if (_ftrend < 0 && fmax0 > _fmax1) fmax0 = _fmax1;

	if (rsi > _smax1) _strend = +1;
	if (rsi < _smin1) _strend = -1;

	if (_strend > 0 && smin0 < _smin1) smin0 = _smin1;
	if (_strend < 0 && smax0 > _smax1) smax0 = _smax1;

	var mPlus = _ftrend > 0 ? fmin0 + FastStep : fmax0 - FastStep;
	var mMinus = _strend > 0 ? smin0 + SlowStep : smax0 - SlowStep;

	_fmin1 = fmin0;
	_fmax1 = fmax0;
	_smin1 = smin0;
	_smax1 = smax0;

	return new ColorMetroValue(this, input, mPlus, mMinus);
	}
}

/// <summary>
/// Indicator value for <see cref="ColorMetroIndicator"/>.
/// </summary>
public class ColorMetroValue : ComplexIndicatorValue
{
	public ColorMetroValue(IIndicator indicator, IIndicatorValue input, decimal mPlus, decimal mMinus)
	: base(indicator, input, (nameof(MPlus), mPlus), (nameof(MMinus), mMinus))
	{
	}

	/// <summary>
	/// Fast line value.
	/// </summary>
	public decimal MPlus => (decimal)GetValue(nameof(MPlus));

	/// <summary>
	/// Slow line value.
	/// </summary>
	public decimal MMinus => (decimal)GetValue(nameof(MMinus));
}
