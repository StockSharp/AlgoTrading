using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy translated from the Jupiter M. 4.1.1 algorithm.
/// </summary>
public class JupiterMStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useAverageTakeProfit;
	private readonly StrategyParam<bool> _dynamicTakeProfit;
	private readonly StrategyParam<int> _tpDynamicStep;
	private readonly StrategyParam<decimal> _tpDecreaseFactor;
	private readonly StrategyParam<decimal> _minTakeProfit;
	private readonly StrategyParam<bool> _breakevenClose;
	private readonly StrategyParam<int> _breakevenStep;
	private readonly StrategyParam<decimal> _firstStep;
	private readonly StrategyParam<bool> _dynamicStep;
	private readonly StrategyParam<int> _stepIncreaseStep;
	private readonly StrategyParam<decimal> _stepIncreaseFactor;
	private readonly StrategyParam<int> _maxStepsBuy;
	private readonly StrategyParam<int> _maxStepsSell;
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _multiplyUseStep;
	private readonly StrategyParam<bool> _cciFilter;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private int _buySteps;
	private int _sellSteps;
	private decimal _avgBuyPrice;
	private decimal _avgSellPrice;

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Use basket level take profit.
	/// </summary>
	public bool UseAverageTakeProfit { get => _useAverageTakeProfit.Value; set => _useAverageTakeProfit.Value = value; }

	/// <summary>
	/// Decrease take profit with each additional step.
	/// </summary>
	public bool DynamicTakeProfit { get => _dynamicTakeProfit.Value; set => _dynamicTakeProfit.Value = value; }

	/// <summary>
	/// Step number starting dynamic take profit.
	/// </summary>
	public int TpDynamicStep { get => _tpDynamicStep.Value; set => _tpDynamicStep.Value = value; }

	/// <summary>
	/// Multiplier for dynamic take profit.
	/// </summary>
	public decimal TpDecreaseFactor { get => _tpDecreaseFactor.Value; set => _tpDecreaseFactor.Value = value; }

	/// <summary>
	/// Minimal allowed take profit.
	/// </summary>
	public decimal MinTakeProfit { get => _minTakeProfit.Value; set => _minTakeProfit.Value = value; }

	/// <summary>
	/// Move take profit to breakeven after some steps.
	/// </summary>
	public bool BreakevenClose { get => _breakevenClose.Value; set => _breakevenClose.Value = value; }

	/// <summary>
	/// Step number to enable breakeven.
	/// </summary>
	public int BreakevenStep { get => _breakevenStep.Value; set => _breakevenStep.Value = value; }

	/// <summary>
	/// Initial grid step.
	/// </summary>
	public decimal FirstStep { get => _firstStep.Value; set => _firstStep.Value = value; }

	/// <summary>
	/// Increase grid step with each order.
	/// </summary>
	public bool DynamicStep { get => _dynamicStep.Value; set => _dynamicStep.Value = value; }

	/// <summary>
	/// Step number starting dynamic grid step.
	/// </summary>
	public int StepIncreaseStep { get => _stepIncreaseStep.Value; set => _stepIncreaseStep.Value = value; }

	/// <summary>
	/// Multiplier for dynamic step increase.
	/// </summary>
	public decimal StepIncreaseFactor { get => _stepIncreaseFactor.Value; set => _stepIncreaseFactor.Value = value; }

	/// <summary>
	/// Maximum amount of buy orders in the grid.
	/// </summary>
	public int MaxStepsBuy { get => _maxStepsBuy.Value; set => _maxStepsBuy.Value = value; }

	/// <summary>
	/// Maximum amount of sell orders in the grid.
	/// </summary>
	public int MaxStepsSell { get => _maxStepsSell.Value; set => _maxStepsSell.Value = value; }

	/// <summary>
	/// Volume for the first order.
	/// </summary>
	public decimal FirstVolume { get => _firstVolume.Value; set => _firstVolume.Value = value; }

	/// <summary>
	/// Multiplier for subsequent order volumes.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// Step number starting volume multiplier application.
	/// </summary>
	public int MultiplyUseStep { get => _multiplyUseStep.Value; set => _multiplyUseStep.Value = value; }

	/// <summary>
	/// Use CCI indicator as entry filter.
	/// </summary>
	public bool CciFilter { get => _cciFilter.Value; set => _cciFilter.Value = value; }

	/// <summary>
	/// Period for CCI indicator.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuy { get => _allowBuy.Value; set => _allowBuy.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSell { get => _allowSell.Value; set => _allowSell.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="JupiterMStrategy"/>.
	/// </summary>
	public JupiterMStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 10m)
		.SetDisplay("Take Profit", "Take profit in price units", "Trading")
		.SetGreaterThanZero();

		_useAverageTakeProfit = Param(nameof(UseAverageTakeProfit), true)
		.SetDisplay("Use Average TP", "Apply basket level take profit", "Trading");

		_dynamicTakeProfit = Param(nameof(DynamicTakeProfit), true)
		.SetDisplay("Dynamic TP", "Decrease take profit with each step", "Trading");

		_tpDynamicStep = Param(nameof(TpDynamicStep), 3)
		.SetDisplay("TP Dynamic Step", "Step to start take profit reduction", "Trading")
		.SetGreaterThanZero();

		_tpDecreaseFactor = Param(nameof(TpDecreaseFactor), 1m)
		.SetDisplay("TP Decrease Factor", "Multiplier for dynamic take profit", "Trading")
		.SetGreaterThanZero();

		_minTakeProfit = Param(nameof(MinTakeProfit), 1m)
		.SetDisplay("Min TP", "Minimal take profit value", "Trading")
		.SetGreaterThanZero();

		_breakevenClose = Param(nameof(BreakevenClose), true)
		.SetDisplay("Breakeven Close", "Move take profit to breakeven", "Trading");

		_breakevenStep = Param(nameof(BreakevenStep), 10)
		.SetDisplay("Breakeven Step", "Step when breakeven is enabled", "Trading")
		.SetGreaterThanZero();

		_firstStep = Param(nameof(FirstStep), 20m)
		.SetDisplay("First Step", "Initial grid step", "Grid")
		.SetGreaterThanZero();

		_dynamicStep = Param(nameof(DynamicStep), true)
		.SetDisplay("Dynamic Step", "Increase step for each new order", "Grid");

		_stepIncreaseStep = Param(nameof(StepIncreaseStep), 1)
		.SetDisplay("Step Increase From", "Step starting dynamic increment", "Grid")
		.SetGreaterThanZero();

		_stepIncreaseFactor = Param(nameof(StepIncreaseFactor), 1m)
		.SetDisplay("Step Multiplier", "Multiplier for dynamic steps", "Grid")
		.SetGreaterThanZero();

		_maxStepsBuy = Param(nameof(MaxStepsBuy), 10)
		.SetDisplay("Max Buy Steps", "Maximum buy orders", "Grid")
		.SetGreaterThanZero();

		_maxStepsSell = Param(nameof(MaxStepsSell), 10)
		.SetDisplay("Max Sell Steps", "Maximum sell orders", "Grid")
		.SetGreaterThanZero();

		_firstVolume = Param(nameof(FirstVolume), 0.01m)
		.SetDisplay("First Volume", "Volume for the first order", "Volume")
		.SetGreaterThanZero();

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
		.SetDisplay("Volume Multiplier", "Multiplier for subsequent orders", "Volume")
		.SetGreaterThanZero();

		_multiplyUseStep = Param(nameof(MultiplyUseStep), 1)
		.SetDisplay("Multiply From Step", "Step to start volume multiplier", "Volume")
		.SetGreaterThanZero();

		_cciFilter = Param(nameof(CciFilter), true)
		.SetDisplay("Use CCI", "Use CCI filter for entry", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 50)
		.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
		.SetGreaterThanZero();

		_allowBuy = Param(nameof(AllowBuy), true)
		.SetDisplay("Allow Buy", "Allow opening long positions", "General");

		_allowSell = Param(nameof(AllowSell), true)
		.SetDisplay("Allow Sell", "Allow opening short positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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

	CommodityChannelIndex cci = null;
	if (CciFilter)
	cci = new CommodityChannelIndex { Length = CciPeriod };

	var subscription = SubscribeCandles(CandleType);

	if (cci != null)
	subscription.Bind(cci, ProcessCandle);
	else
	subscription.Bind(ProcessCandle);

	subscription.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	if (cci != null)
	DrawIndicator(area, cci);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	ProcessCandle(candle, 0m);
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var price = candle.ClosePrice;

	if (Position <= 0 && _buySteps > 0)
	ResetBuy();
	if (Position >= 0 && _sellSteps > 0)
	ResetSell();

	if (Position >= 0)
	HandleBuy(price, cci);
	if (Position <= 0)
	HandleSell(price, cci);
	}

	private void HandleBuy(decimal price, decimal cci)
	{
	// First buy order
	if (_buySteps == 0)
	{
	if (!AllowBuy)
	return;
	if (CciFilter && cci > -100)
	return;

	BuyMarket(FirstVolume);
	_buySteps = 1;
	_lastBuyPrice = price;
	_avgBuyPrice = price;
	return;
	}

	var step = CalcStep(_buySteps);
	if (price <= _lastBuyPrice - step && _buySteps < MaxStepsBuy)
	{
	var volume = CalcVolume(_buySteps);
	BuyMarket(volume);
	_buySteps++;
	_lastBuyPrice = price;
	_avgBuyPrice = (_avgBuyPrice * (_buySteps - 1) + price) / _buySteps;
	}

	var tp = CalcTakeProfit(_buySteps);
	var target = (UseAverageTakeProfit ? _avgBuyPrice : _lastBuyPrice) + tp;

	if (price >= target && _buySteps > 0)
	{
	SellMarket(Position);
	ResetBuy();
	}
	}

	private void HandleSell(decimal price, decimal cci)
	{
	if (_sellSteps == 0)
	{
	if (!AllowSell)
	return;
	if (CciFilter && cci < 100)
	return;

	SellMarket(FirstVolume);
	_sellSteps = 1;
	_lastSellPrice = price;
	_avgSellPrice = price;
	return;
	}

	var step = CalcStep(_sellSteps);
	if (price >= _lastSellPrice + step && _sellSteps < MaxStepsSell)
	{
	var volume = CalcVolume(_sellSteps);
	SellMarket(volume);
	_sellSteps++;
	_lastSellPrice = price;
	_avgSellPrice = (_avgSellPrice * (_sellSteps - 1) + price) / _sellSteps;
	}

	var tp = CalcTakeProfit(_sellSteps);
	var target = (UseAverageTakeProfit ? _avgSellPrice : _lastSellPrice) - tp;

	if (price <= target && _sellSteps > 0)
	{
	BuyMarket(-Position);
	ResetSell();
	}
	}

	private decimal CalcStep(int step)
	{
	var value = FirstStep;
	if (DynamicStep && step >= StepIncreaseStep)
	{
	var pow = step - StepIncreaseStep + 1;
	for (var i = 0; i < pow; i++)
	value *= StepIncreaseFactor;
	}
	return value;
	}

	private decimal CalcVolume(int step)
	{
	var value = FirstVolume;
	if (step >= MultiplyUseStep)
	{
	var pow = step - MultiplyUseStep + 1;
	for (var i = 0; i < pow; i++)
	value *= VolumeMultiplier;
	}
	return value;
	}

	private decimal CalcTakeProfit(int step)
	{
	var tp = TakeProfit;
	if (DynamicTakeProfit && step >= TpDynamicStep)
	{
	var pow = step - TpDynamicStep + 1;
	for (var i = 0; i < pow; i++)
	tp *= TpDecreaseFactor;
	if (tp < MinTakeProfit)
	tp = MinTakeProfit;
	}
	if (BreakevenClose && step >= BreakevenStep)
	tp = 0m;
	return tp;
	}

	private void ResetBuy()
	{
	_buySteps = 0;
	_lastBuyPrice = 0m;
	_avgBuyPrice = 0m;
	}

	private void ResetSell()
	{
	_sellSteps = 0;
	_lastSellPrice = 0m;
	_avgSellPrice = 0m;
	}
}
