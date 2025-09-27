namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Strategy inspired by the "Equidistant Channel" expert advisor.
/// It opens trades on MACD line crossovers and manages exits with Bollinger Bands and money management rules.
/// </summary>
public class EquidistantChannelStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTakeProfitMoney;
	private readonly StrategyParam<decimal> _takeProfitMoney;
	private readonly StrategyParam<bool> _useTakeProfitPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingTakeProfitMoney;
	private readonly StrategyParam<decimal> _trailingStopMoney;
	private readonly StrategyParam<bool> _useBollingerStop;
	private readonly StrategyParam<bool> _useMoveToBreakeven;
	private readonly StrategyParam<decimal> _breakevenTrigger;
	private readonly StrategyParam<decimal> _breakevenOffset;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private BollingerBands _bollinger = null!;

	private decimal? _previousMacd;
	private decimal? _previousSignal;
	private decimal _entryPrice;
	private decimal _maxFloatingProfit;
	private decimal? _breakevenPrice;
	private decimal _initialCapital;

	/// <summary>
	/// Enables closing positions when floating profit reaches a money based target.
	/// </summary>
	public bool UseTakeProfitMoney
	{
		get => _useTakeProfitMoney.Value;
		set => _useTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Take profit target expressed in account currency.
	/// </summary>
	public decimal TakeProfitMoney
	{
		get => _takeProfitMoney.Value;
		set => _takeProfitMoney.Value = value;
	}

	/// <summary>
	/// Enables closing positions when floating profit reaches a percentage of the starting capital.
	/// </summary>
	public bool UseTakeProfitPercent
	{
		get => _useTakeProfitPercent.Value;
		set => _useTakeProfitPercent.Value = value;
	}

	/// <summary>
	/// Take profit threshold expressed as percent of the initial capital.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enables trailing logic on floating profit measured in money.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Floating profit level that activates trailing logic (account currency).
	/// </summary>
	public decimal TrailingTakeProfitMoney
	{
		get => _trailingTakeProfitMoney.Value;
		set => _trailingTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Maximum allowed pullback from the tracked floating profit peak (account currency).
	/// </summary>
	public decimal TrailingStopMoney
	{
		get => _trailingStopMoney.Value;
		set => _trailingStopMoney.Value = value;
	}

	/// <summary>
	/// Enables exits when price touches Bollinger Bands similarly to the expert advisor.
	/// </summary>
	public bool UseBollingerStop
	{
		get => _useBollingerStop.Value;
		set => _useBollingerStop.Value = value;
	}

	/// <summary>
	/// Enables moving the stop level to breakeven once profit threshold is reached.
	/// </summary>
	public bool UseMoveToBreakeven
	{
		get => _useMoveToBreakeven.Value;
		set => _useMoveToBreakeven.Value = value;
	}

	/// <summary>
	/// Profit (in price steps) required to arm the breakeven stop.
	/// </summary>
	public decimal BreakevenTrigger
	{
		get => _breakevenTrigger.Value;
		set => _breakevenTrigger.Value = value;
	}

	/// <summary>
	/// Additional offset (in price steps) added to the breakeven price once armed.
	/// </summary>
	public decimal BreakevenOffset
	{
		get => _breakevenOffset.Value;
		set => _breakevenOffset.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width expressed in standard deviations.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line length for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Volume used for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EquidistantChannelStrategy"/>.
	/// </summary>
	public EquidistantChannelStrategy()
	{
		_useTakeProfitMoney = Param(nameof(UseTakeProfitMoney), false)
			.SetDisplay("Use TP (Money)", "Close by absolute profit", "Risk");

		_takeProfitMoney = Param(nameof(TakeProfitMoney), 10m)
			.SetDisplay("TP Money", "Profit target in currency", "Risk")
			.SetCanOptimize(true);

		_useTakeProfitPercent = Param(nameof(UseTakeProfitPercent), false)
			.SetDisplay("Use TP (%)", "Close by percent of balance", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m)
			.SetDisplay("TP Percent", "Percent of initial capital", "Risk")
			.SetCanOptimize(true);

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Activate trailing profit", "Risk");

		_trailingTakeProfitMoney = Param(nameof(TrailingTakeProfitMoney), 40m)
			.SetDisplay("Trail Activate", "Profit level that arms trailing", "Risk")
			.SetCanOptimize(true);

		_trailingStopMoney = Param(nameof(TrailingStopMoney), 10m)
			.SetDisplay("Trail Step", "Allowed pullback of floating profit", "Risk")
			.SetCanOptimize(true);

		_useBollingerStop = Param(nameof(UseBollingerStop), true)
			.SetDisplay("Use BB Stop", "Exit when price touches band", "Risk");

		_useMoveToBreakeven = Param(nameof(UseMoveToBreakeven), true)
			.SetDisplay("Use Breakeven", "Move stop to breakeven", "Risk");

		_breakevenTrigger = Param(nameof(BreakevenTrigger), 10m)
			.SetDisplay("Breakeven Trigger", "Profit (points) to arm breakeven", "Risk")
			.SetCanOptimize(true);

		_breakevenOffset = Param(nameof(BreakevenOffset), 5m)
			.SetDisplay("Breakeven Offset", "Extra distance (points) from entry", "Risk")
			.SetCanOptimize(true);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
			.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands width", "Indicators")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "MACD fast EMA", "Indicators")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "MACD slow EMA", "Indicators")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "MACD signal EMA", "Indicators")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 20)
			.SetDisplay("Stop Loss", "Stop distance in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetDisplay("Take Profit", "Take distance in points", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 40)
			.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "General");
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

	_previousMacd = null;
	_previousSignal = null;
	_entryPrice = 0m;
	_maxFloatingProfit = 0m;
	_breakevenPrice = null;
	_initialCapital = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_initialCapital = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;

	_macd = new MovingAverageConvergenceDivergenceSignal
	{
	Macd =
	{
	ShortMa = { Length = MacdFastPeriod },
	LongMa = { Length = MacdSlowPeriod },
	},
	SignalMa = { Length = MacdSignalPeriod }
	};

	_bollinger = new BollingerBands
	{
	Length = BollingerPeriod,
	Width = BollingerDeviation
	};

	var subscription = SubscribeCandles(CandleType);

	subscription
	.BindEx(_macd, _bollinger, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
	}

	Unit tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Point) : null;
	Unit sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Point) : null;

	if (TrailingStopPoints > 0 && (sl is null || TrailingStopPoints < StopLossPoints))
	sl = new Unit(TrailingStopPoints, UnitTypes.Point);

	if (tp is not null || sl is not null)
	StartProtection(tp, sl, isStopTrailing: TrailingStopPoints > 0);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue bollingerValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macdData.Macd is not decimal macdLine || macdData.Signal is not decimal signalLine)
	return;

	var bb = (BollingerBandsValue)bollingerValue;
	if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
	return;

	var close = candle.ClosePrice;

	if (Position != 0m)
	{
	TryActivateBreakeven(close);

	if (TryApplyBreakeven(close))
	{
	_previousMacd = macdLine;
	_previousSignal = signalLine;
	return;
	}

	if (UseBollingerStop)
	{
	if (Position > 0m && close >= upper)
	{
	ExitPosition(close);
	_previousMacd = macdLine;
	_previousSignal = signalLine;
	return;
	}

	if (Position < 0m && close <= lower)
	{
	ExitPosition(close);
	_previousMacd = macdLine;
	_previousSignal = signalLine;
	return;
	}
	}

	if (TryApplyMoneyTargets(close))
	{
	_previousMacd = macdLine;
	_previousSignal = signalLine;
	return;
	}
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousMacd = macdLine;
	_previousSignal = signalLine;
	return;
	}

	if (_previousMacd is decimal prevMacd && _previousSignal is decimal prevSignal)
	{
	var crossUp = prevMacd < prevSignal && macdLine > signalLine;
	var crossDown = prevMacd > prevSignal && macdLine < signalLine;

	if (crossUp && Position <= 0m)
	EnterPosition(Sides.Buy, close);
	else if (crossDown && Position >= 0m)
	EnterPosition(Sides.Sell, close);
	}

	_previousMacd = macdLine;
	_previousSignal = signalLine;
	}

	private void EnterPosition(Sides side, decimal price)
	{
	var volume = TradeVolume;
	if (volume <= 0m)
	return;

	if (side == Sides.Buy)
	{
	var cover = Position < 0m ? Math.Abs(Position) : 0m;
	if (cover > 0m)
	BuyMarket(cover);

	BuyMarket(volume);
	}
	else
	{
	var cover = Position > 0m ? Position : 0m;
	if (cover > 0m)
	SellMarket(cover);

	SellMarket(volume);
	}

	_entryPrice = price;
	_maxFloatingProfit = 0m;
	_breakevenPrice = null;
	}

	private void ExitPosition(decimal price)
	{
	if (Position > 0m)
	SellMarket(Position);
	else if (Position < 0m)
	BuyMarket(Math.Abs(Position));

	ResetPositionState();
	}

	private void ResetPositionState()
	{
	_entryPrice = 0m;
	_maxFloatingProfit = 0m;
	_breakevenPrice = null;
	}

	private bool TryApplyMoneyTargets(decimal closePrice)
	{
	if (Position == 0m)
	return false;

	var profit = CalculateFloatingProfit(closePrice);

	if (UseTakeProfitMoney && profit >= TakeProfitMoney && TakeProfitMoney > 0m)
	{
	ExitPosition(closePrice);
	return true;
	}

	if (UseTakeProfitPercent && TakeProfitPercent > 0m && _initialCapital > 0m)
	{
	var target = _initialCapital * TakeProfitPercent / 100m;
	if (profit >= target)
	{
	ExitPosition(closePrice);
	return true;
	}
	}

	if (EnableTrailing && TrailingTakeProfitMoney > 0m && TrailingStopMoney > 0m)
	{
	if (profit >= TrailingTakeProfitMoney)
	_maxFloatingProfit = Math.Max(_maxFloatingProfit, profit);

	if (_maxFloatingProfit > 0m && _maxFloatingProfit - profit >= TrailingStopMoney)
	{
	ExitPosition(closePrice);
	return true;
	}
	}

	return false;
	}

	private void TryActivateBreakeven(decimal closePrice)
	{
	if (!UseMoveToBreakeven || _breakevenPrice.HasValue || Position == 0m)
	return;

	var trigger = StepsToPrice(BreakevenTrigger);
	if (trigger <= 0m)
	return;

	var offset = StepsToPrice(BreakevenOffset);

	if (Position > 0m && closePrice >= _entryPrice + trigger)
	_breakevenPrice = _entryPrice + offset;
	else if (Position < 0m && closePrice <= _entryPrice - trigger)
	_breakevenPrice = _entryPrice - offset;
	}

	private bool TryApplyBreakeven(decimal closePrice)
	{
	if (!UseMoveToBreakeven || !_breakevenPrice.HasValue || Position == 0m)
	return false;

	var breakeven = _breakevenPrice.Value;

	if (Position > 0m && closePrice <= breakeven)
	{
	ExitPosition(closePrice);
	return true;
	}

	if (Position < 0m && closePrice >= breakeven)
	{
	ExitPosition(closePrice);
	return true;
	}

	return false;
	}

	private decimal CalculateFloatingProfit(decimal currentPrice)
	{
	if (Position == 0m || _entryPrice == 0m)
	return 0m;

	var priceStep = Security?.PriceStep ?? 0m;
	var stepPrice = Security?.StepPrice ?? 0m;

	if (priceStep <= 0m || stepPrice <= 0m)
	return 0m;

	var direction = Position > 0m ? 1m : -1m;
	var priceDiff = (currentPrice - _entryPrice) * direction;
	var steps = priceDiff / priceStep;
	return steps * stepPrice * Math.Abs(Position);
	}

	private decimal StepsToPrice(decimal steps)
	{
	var priceStep = Security?.PriceStep ?? 0m;
	return steps * priceStep;
	}
}

