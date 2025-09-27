using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

	/// <summary>
	/// Smart Fibonacci strategy using SMA breakout for entries and ATR-based bands for exits.
	/// </summary>
	public class SmartFibStrategy : Strategy
	{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _fibSmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _firstFactor;
	private readonly StrategyParam<decimal> _secondFactor;
	private readonly StrategyParam<string> _strategyType;
	private readonly StrategyParam<string> _exitMethod;

	private decimal _prevClose;
	private decimal _prevEntry;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry SMA length.
	/// </summary>
	public int SmaLength
	{
	get => _smaLength.Value;
	set => _smaLength.Value = value;
	}

	/// <summary>
	/// SMA length for Fibonacci bands.
	/// </summary>
	public int FibSmaLength
	{
	get => _fibSmaLength.Value;
	set => _fibSmaLength.Value = value;
	}

	/// <summary>
	/// ATR length for bands.
	/// </summary>
	public int AtrLength
	{
	get => _atrLength.Value;
	set => _atrLength.Value = value;
	}

	/// <summary>
	/// First Fibonacci factor.
	/// </summary>
	public decimal FirstFactor
	{
	get => _firstFactor.Value;
	set => _firstFactor.Value = value;
	}

	/// <summary>
	/// Second Fibonacci factor.
	/// </summary>
	public decimal SecondFactor
	{
	get => _secondFactor.Value;
	set => _secondFactor.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public string StrategyType
	{
	get => _strategyType.Value;
	set => _strategyType.Value = value;
	}

	/// <summary>
	/// Exit method.
	/// </summary>
	public string ExitMethod
	{
	get => _exitMethod.Value;
	set => _exitMethod.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SmartFibStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");

	_smaLength = Param(nameof(SmaLength), 50)
	.SetDisplay("Entry SMA Length", "Length for entry SMA", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(20, 100, 5);

	_fibSmaLength = Param(nameof(FibSmaLength), 8)
	.SetDisplay("Fibonacci SMA Length", "Length for center SMA", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(5, 20, 1);

	_atrLength = Param(nameof(AtrLength), 6)
	.SetDisplay("ATR Length", "Length for ATR calculation", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(5, 20, 1);

	_firstFactor = Param(nameof(FirstFactor), 1.618m)
	.SetDisplay("First Factor", "Multiplier for first band", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(1m, 2m, 0.1m);

	_secondFactor = Param(nameof(SecondFactor), 2.618m)
	.SetDisplay("Second Factor", "Multiplier for second band", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(2m, 3.5m, 0.1m);

	_strategyType = Param(nameof(StrategyType), "Buy & Sell")
	.SetDisplay("Strategy Type", "Allowed trade directions", "Trading")
	.SetOptions("Buy & Sell", "Long Only", "Short Only");

	_exitMethod = Param(nameof(ExitMethod), "Low Risk")
	.SetDisplay("Exit Method", "Exit bands preference", "Trading")
	.SetOptions("Low Risk", "High Risk");
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
	_prevClose = 0m;
	_prevEntry = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var entrySma = new SimpleMovingAverage { Length = SmaLength };
	var atr = new AverageTrueRange { Length = AtrLength };
	var fibSma = new SimpleMovingAverage { Length = FibSmaLength };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(entrySma, atr, fibSma, ProcessCandle)
	.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal entryValue, decimal atrValue, decimal smaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var middle = smaValue;
	var fib1Upper = middle + atrValue * FirstFactor;
	var fib1Lower = middle - atrValue * FirstFactor;
	var fib2Upper = middle + atrValue * SecondFactor;
	var fib2Lower = middle - atrValue * SecondFactor;

	var longExit = ExitMethod == "Low Risk" ? fib1Upper : fib2Upper;
	var shortExit = ExitMethod == "Low Risk" ? fib1Lower : fib2Lower;

	var longCross = _prevClose <= _prevEntry && candle.ClosePrice > entryValue;
	var shortCross = _prevClose >= _prevEntry && candle.ClosePrice < entryValue;

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_prevClose = candle.ClosePrice;
	_prevEntry = entryValue;
	return;
	}

	if (StrategyType != "Short Only" && longCross && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));

	if (StrategyType != "Long Only" && shortCross && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));

	if (Position > 0 && candle.HighPrice >= longExit)
	SellMarket(Position);

	if (Position < 0 && candle.LowPrice <= shortExit)
	BuyMarket(Math.Abs(Position));

	_prevClose = candle.ClosePrice;
	_prevEntry = entryValue;
	}
	}
