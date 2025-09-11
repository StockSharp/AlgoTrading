namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-timeframe Parabolic SAR strategy.
/// </summary>
public class MultiTimeframeParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _lowerCandleType;
	private readonly StrategyParam<bool> _useHigherTf;
	private readonly StrategyParam<bool> _useLowerTf;
	private readonly StrategyParam<string> _longSource;
	private readonly StrategyParam<string> _shortSource;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private ParabolicSar _currentSar;
	private ParabolicSar _higherSar;
	private ParabolicSar _lowerSar;

	private decimal? _higherSarValue;
	private decimal? _lowerSarValue;

	/// <summary>
	/// Initialize <see cref="MultiTimeframeParabolicSarStrategy"/>.
	/// </summary>
	public MultiTimeframeParabolicSarStrategy()
	{
	_acceleration = Param(nameof(Acceleration), 0.02m)
		.SetDisplay("Acceleration", "Parabolic SAR acceleration", "SAR")
		.SetCanOptimize(true);

	_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
		.SetDisplay("Max Acceleration", "Parabolic SAR maximum acceleration", "SAR")
		.SetCanOptimize(true);

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Base timeframe", "General");

	_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Higher TF", "Higher timeframe", "General");

	_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Lower TF", "Lower timeframe", "General");

	_useHigherTf = Param(nameof(UseHigherTf), true)
		.SetDisplay("Use Higher TF", "Enable higher timeframe SAR", "General");

	_useLowerTf = Param(nameof(UseLowerTf), false)
		.SetDisplay("Use Lower TF", "Enable lower timeframe SAR", "General");

	_longSource = Param(nameof(LongSource), "Higher")
		.SetDisplay("Long Source", "Source for long entries", "Logic")
		.SetOptions("Higher", "Current", "Both");

	_shortSource = Param(nameof(ShortSource), "Higher")
		.SetDisplay("Short Source", "Source for short entries", "Logic")
		.SetOptions("Higher", "Current", "Both");

	_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable stop loss", "Protection");

	_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Protection");

	_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing", "Enable trailing stop", "Protection");

	_trailingPercent = Param(nameof(TrailingPercent), 0.5m)
		.SetDisplay("Trailing %", "Trailing stop percentage", "Protection");

	_useTakeProfit = Param(nameof(UseTakeProfit), false)
		.SetDisplay("Use Take Profit", "Enable take profit", "Protection");

	_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Protection");
	}

	/// <summary>
	/// Parabolic SAR acceleration.
	/// </summary>
	public decimal Acceleration
	{
	get => _acceleration.Value;
	set => _acceleration.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal MaxAcceleration
	{
	get => _maxAcceleration.Value;
	set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Main candle type.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType
	{
	get => _higherCandleType.Value;
	set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type.
	/// </summary>
	public DataType LowerCandleType
	{
	get => _lowerCandleType.Value;
	set => _lowerCandleType.Value = value;
	}

	/// <summary>
	/// Use higher timeframe SAR.
	/// </summary>
	public bool UseHigherTf
	{
	get => _useHigherTf.Value;
	set => _useHigherTf.Value = value;
	}

	/// <summary>
	/// Use lower timeframe SAR.
	/// </summary>
	public bool UseLowerTf
	{
	get => _useLowerTf.Value;
	set => _useLowerTf.Value = value;
	}

	/// <summary>
	/// Source for long entries.
	/// </summary>
	public string LongSource
	{
	get => _longSource.Value;
	set => _longSource.Value = value;
	}

	/// <summary>
	/// Source for short entries.
	/// </summary>
	public string ShortSource
	{
	get => _shortSource.Value;
	set => _shortSource.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss
	{
	get => _useStopLoss.Value;
	set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
	get => _stopLossPercent.Value;
	set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
	get => _useTrailingStop.Value;
	set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailingPercent
	{
	get => _trailingPercent.Value;
	set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit
	{
	get => _useTakeProfit.Value;
	set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
	get => _takeProfitPercent.Value;
	set => _takeProfitPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, CandleType);
	if (UseHigherTf)
		yield return (Security, HigherCandleType);
	if (UseLowerTf)
		yield return (Security, LowerCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();
	_currentSar = null;
	_higherSar = null;
	_lowerSar = null;
	_higherSarValue = null;
	_lowerSarValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_currentSar = new ParabolicSar
	{
		Acceleration = Acceleration,
		AccelerationMax = MaxAcceleration
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(_currentSar, ProcessMain)
		.Start();

	if (UseHigherTf)
	{
		_higherSar = new ParabolicSar
		{
		Acceleration = Acceleration,
		AccelerationMax = MaxAcceleration
		};

		var higherSub = SubscribeCandles(HigherCandleType);
		higherSub
		.Bind(_higherSar, ProcessHigher)
		.Start();
	}

	if (UseLowerTf)
	{
		_lowerSar = new ParabolicSar
		{
		Acceleration = Acceleration,
		AccelerationMax = MaxAcceleration
		};

		var lowerSub = SubscribeCandles(LowerCandleType);
		lowerSub
		.Bind(_lowerSar, ProcessLower)
		.Start();
	}

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, _currentSar);
		if (UseHigherTf && _higherSar != null)
		DrawIndicator(area, _higherSar);
		if (UseLowerTf && _lowerSar != null)
		DrawIndicator(area, _lowerSar);
		DrawOwnTrades(area);
	}

	if (UseStopLoss || UseTakeProfit || UseTrailingStop)
	{
		StartProtection(
		takeProfit: UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null,
		stopLoss: UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : null,
		isStopTrailing: UseTrailingStop
		);
	}
	}

	private void ProcessHigher(ICandleMessage candle, decimal sar)
	{
	if (candle.State != CandleStates.Finished)
		return;

	_higherSarValue = sar;
	}

	private void ProcessLower(ICandleMessage candle, decimal sar)
	{
	if (candle.State != CandleStates.Finished)
		return;

	_lowerSarValue = sar;
	}

	private void ProcessMain(ICandleMessage candle, decimal currentSar)
	{
	if (candle.State != CandleStates.Finished)
		return;

	if (!_currentSar.IsFormed)
		return;

	if (UseHigherTf && (!_higherSar.IsFormed || _higherSarValue == null))
		return;

	if (UseLowerTf && (!_lowerSar.IsFormed || _lowerSarValue == null))
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	var price = candle.ClosePrice;

	var longCondition = EvaluateLong(price, currentSar);
	var shortCondition = EvaluateShort(price, currentSar);

	if (longCondition && Position <= 0)
	{
		BuyMarket(Volume + Math.Abs(Position));
	}
	else if (shortCondition && Position >= 0)
	{
		SellMarket(Volume + Math.Abs(Position));
	}

	if (Position > 0 && price < currentSar)
	{
		ClosePosition();
	}
	else if (Position < 0 && price > currentSar)
	{
		ClosePosition();
	}
	}

	private bool EvaluateLong(decimal price, decimal currentSar)
	{
	var lowerOk = !UseLowerTf || (price > _lowerSarValue);
	var higherOk = !UseHigherTf || (price > _higherSarValue);

	return LongSource switch
	{
		"Higher" => higherOk && lowerOk,
		"Current" => price > currentSar && lowerOk,
		"Both" => price > currentSar && higherOk && lowerOk,
		_ => false
	};
	}

	private bool EvaluateShort(decimal price, decimal currentSar)
	{
	var lowerOk = !UseLowerTf || (price < _lowerSarValue);
	var higherOk = !UseHigherTf || (price < _higherSarValue);

	return ShortSource switch
	{
		"Higher" => higherOk && lowerOk,
		"Current" => price < currentSar && lowerOk,
		"Both" => price < currentSar && higherOk && lowerOk,
		_ => false
	};
	}
}
