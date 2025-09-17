using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "555 Scalper" MetaTrader expert advisor using the high level StockSharp API.
/// Combines LWMA trend filters, higher timeframe momentum confirmation and a monthly MACD filter.
/// Includes the original money management rules such as break-even moves, pip trailing and money-based targets.
/// </summary>
public class Scalper555Strategy : Strategy
{
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailingTrigger;
	private readonly StrategyParam<decimal> _moneyTrailingStop;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _totalEquityRisk;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerSteps;
	private readonly StrategyParam<decimal> _breakEvenOffsetSteps;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private ExponentialMovingAverage _emaHigh = null!;
	private ExponentialMovingAverage _emaLow = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private ICandleMessage? _previousCandle;
	private ICandleMessage? _previousCandle2;
	private decimal? _momentumAbs1;
	private decimal? _momentumAbs2;
	private decimal? _momentumAbs3;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal _pipSize;
	private decimal _stepPrice;
	private decimal _initialCapital;
	private decimal _equityPeak;
	private decimal _maxFloatingProfit;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _breakevenPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _previousRealizedPnL;
	private int _longTradeCount;
	private int _shortTradeCount;

	/// <summary>
	/// Initializes default parameters that mirror the MetaTrader template.
	/// </summary>
	public Scalper555Strategy()
	{
		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
		.SetDisplay("Use money take profit", "Close all positions when floating profit reaches MoneyTakeProfit.", "Money management");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
		.SetDisplay("Money take profit", "Floating profit target measured in account currency.", "Money management");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
		.SetDisplay("Use percent take profit", "Close all positions when floating profit reaches PercentTakeProfit of initial capital.", "Money management");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
		.SetDisplay("Percent take profit", "Floating profit target as a percentage of initial capital.", "Money management");

		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
		.SetDisplay("Enable money trailing", "Activate trailing stop on floating profit measured in money.", "Money management");

		_moneyTrailingTrigger = Param(nameof(MoneyTrailingTrigger), 40m)
		.SetDisplay("Money trailing trigger", "Floating profit required before money trailing becomes active.", "Money management");

		_moneyTrailingStop = Param(nameof(MoneyTrailingStop), 10m)
		.SetDisplay("Money trailing stop", "Maximum allowed pullback in floating profit once money trailing is active.", "Money management");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
		.SetDisplay("Base volume", "Initial lot volume for the first trade.", "Position sizing");

		_lotExponent = Param(nameof(LotExponent), 1.44m)
		.SetDisplay("Lot exponent", "Multiplier applied every time an additional position is opened in the same direction.", "Position sizing");

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
		.SetDisplay("Stop loss (points)", "Protective stop distance expressed in MetaTrader pips.", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
		.SetDisplay("Take profit (points)", "Initial take profit distance in MetaTrader pips.", "Risk");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetDisplay("Fast LWMA period", "Length of the fast weighted moving average calculated on typical price.", "Filters");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetDisplay("Slow LWMA period", "Length of the slow weighted moving average calculated on typical price.", "Filters");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetDisplay("Momentum sell threshold", "Minimum absolute deviation from 100 required for bearish momentum.", "Filters");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetDisplay("Momentum buy threshold", "Minimum absolute deviation from 100 required for bullish momentum.", "Filters");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetDisplay("Max trades", "Maximum number of layered entries per direction.", "Position sizing");

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use equity stop", "Abort all positions when floating drawdown exceeds TotalEquityRisk of equity peak.", "Risk");

		_totalEquityRisk = Param(nameof(TotalEquityRisk), 1m)
		.SetDisplay("Equity risk %", "Percentage of highest recorded equity tolerated as drawdown.", "Risk");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
		.SetDisplay("Trailing stop (points)", "Classic pip-based trailing distance.", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use break-even", "Move stop to break-even after BreakEvenTriggerSteps of profit.", "Risk");

		_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 30m)
		.SetDisplay("Break-even trigger", "Profit in MetaTrader pips required to arm the break-even stop.", "Risk");

		_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 30m)
		.SetDisplay("Break-even offset", "Additional pips added beyond the entry price once break-even activates.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Trading candles", "Primary timeframe used for entries.", "General");
	}

	/// <summary>
	/// Use money-based take profit.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit target in account currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Use percent-based take profit.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit target in percent of initial capital.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable money trailing on floating profit.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Trigger level for money trailing.
	/// </summary>
	public decimal MoneyTrailingTrigger
	{
		get => _moneyTrailingTrigger.Value;
		set => _moneyTrailingTrigger.Value = value;
	}

	/// <summary>
	/// Maximum allowed pullback after the trailing trigger.
	/// </summary>
	public decimal MoneyTrailingStop
	{
		get => _moneyTrailingStop.Value;
		set => _moneyTrailingStop.Value = value;
	}

	/// <summary>
	/// Base volume for the first trade.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Lot exponent applied when stacking positions.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Fast LWMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum bullish momentum distance from the neutral 100 level.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum bearish momentum distance from the neutral 100 level.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of trades per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Use equity based emergency stop.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum allowed drawdown from equity peak.
	/// </summary>
	public decimal TotalEquityRisk
	{
		get => _totalEquityRisk.Value;
		set => _totalEquityRisk.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in MetaTrader pips.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Enable break-even stop move.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit required to trigger the break-even stop in MetaTrader pips.
	/// </summary>
	public decimal BreakEvenTriggerSteps
	{
		get => _breakEvenTriggerSteps.Value;
		set => _breakEvenTriggerSteps.Value = value;
	}

	/// <summary>
	/// Offset applied when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetSteps
	{
		get => _breakEvenOffsetSteps.Value;
		set => _breakEvenOffsetSteps.Value = value;
	}

	/// <summary>
	/// Primary trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private DataType MomentumCandleType => GetHigherTimeFrame(CandleType);

	private static readonly DataType MonthlyCandleType = TimeSpan.FromDays(30).TimeFrame();

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, CandleType);

		var momentumType = MomentumCandleType;
		if (!momentumType.Equals(CandleType))
		yield return (Security, momentumType);

		if (!MonthlyCandleType.Equals(CandleType) && !MonthlyCandleType.Equals(momentumType))
		yield return (Security, MonthlyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousCandle = null;
		_previousCandle2 = null;
		_momentumAbs1 = null;
		_momentumAbs2 = null;
		_momentumAbs3 = null;
		_macdMain = null;
		_macdSignal = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_maxFloatingProfit = 0m;
		_longTradeCount = 0;
		_shortTradeCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_emaHigh = new ExponentialMovingAverage { Length = 5 };
		_emaLow = new ExponentialMovingAverage { Length = 5 };
		_momentum = new Momentum { Length = 14 };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = 12,
			SlowLength = 26,
			SignalLength = 9
		};

		_pipSize = GetPipSize();
		_stepPrice = Security?.StepPrice ?? 0m;
		_initialCapital = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;
		_equityPeak = _initialCapital;
		_previousRealizedPnL = PnL;
		_maxFloatingProfit = 0m;
		_longTradeCount = Position > 0m ? 1 : 0;
		_shortTradeCount = Position < 0m ? 1 : 0;

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(ProcessMainCandle).Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription.Bind(_momentum, ProcessMomentum).Start();

		var macdSubscription = SubscribeCandles(MonthlyCandleType);
		macdSubscription.BindEx(_macd, ProcessMacd).Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_previousRealizedPnL = PnL;
			_stopPrice = null;
			_takeProfitPrice = null;
			_breakevenPrice = null;
			_highestPrice = 0m;
			_lowestPrice = 0m;
			_maxFloatingProfit = 0m;
			_longTradeCount = 0;
			_shortTradeCount = 0;
			return;
		}

		if (Position > 0m)
		{
			_shortTradeCount = 0;
			if (PositionPrice is decimal entry)
			{
				_stopPrice = entry - StepsToPrice(StopLossSteps);
				_takeProfitPrice = entry + StepsToPrice(TakeProfitSteps);
				_highestPrice = entry;
			}
		}
		else if (Position < 0m)
		{
			_longTradeCount = 0;
			if (PositionPrice is decimal entry)
			{
				_stopPrice = entry + StepsToPrice(StopLossSteps);
				_takeProfitPrice = entry - StepsToPrice(TakeProfitSteps);
				_lowestPrice = entry;
			}
		}
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, typical, candle.OpenTime)).ToDecimal();
		var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, typical, candle.OpenTime)).ToDecimal();
		var emaHigh = _emaHigh.Process(new DecimalIndicatorValue(_emaHigh, candle.HighPrice, candle.OpenTime)).ToDecimal();
		var emaLow = _emaLow.Process(new DecimalIndicatorValue(_emaLow, candle.LowPrice, candle.OpenTime)).ToDecimal();

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_emaHigh.IsFormed || !_emaLow.IsFormed)
		{
			StorePreviousCandles(candle);
			return;
		}

		UpdateExtremes(candle);

		if (CheckPriceStops(candle))
		{
			StorePreviousCandles(candle);
			return;
		}

		if (TryApplyEquityStop(candle.ClosePrice))
		{
			StorePreviousCandles(candle);
			return;
		}

		TryActivateBreakeven(candle.ClosePrice);

		if (TryApplyBreakEvenExit(candle.ClosePrice))
		{
			StorePreviousCandles(candle);
			return;
		}

		ApplyTrailingStop(candle);

		if (TryApplyMoneyTargets(candle.ClosePrice))
		{
			StorePreviousCandles(candle);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StorePreviousCandles(candle);
			return;
		}

		if (_previousCandle is not ICandleMessage prev1 ||
		_previousCandle2 is not ICandleMessage prev2 ||
		_momentumAbs1 is not decimal momentum1 ||
		_momentumAbs2 is not decimal momentum2 ||
		_momentumAbs3 is not decimal momentum3 ||
		_macdMain is not decimal macdMain ||
		_macdSignal is not decimal macdSignal)
		{
			StorePreviousCandles(candle);
			return;
		}

		var buyMomentumOk = momentum1 >= MomentumBuyThreshold || momentum2 >= MomentumBuyThreshold || momentum3 >= MomentumBuyThreshold;
		var sellMomentumOk = momentum1 >= MomentumSellThreshold || momentum2 >= MomentumSellThreshold || momentum3 >= MomentumSellThreshold;

		var canBuy = candle.OpenPrice > emaLow &&
		fastValue > slowValue &&
		prev2.LowPrice < prev1.HighPrice &&
		buyMomentumOk &&
		macdMain > macdSignal;

		var canSell = candle.OpenPrice < emaHigh &&
		fastValue < slowValue &&
		prev1.LowPrice < prev2.HighPrice &&
		sellMomentumOk &&
		macdMain < macdSignal;

		if (canBuy && Position <= 0m && _longTradeCount < MaxTrades)
		{
			if (Position < 0m)
			CloseShort(candle.ClosePrice);

			EnterLong(candle.ClosePrice);
		}
		else if (canSell && Position >= 0m && _shortTradeCount < MaxTrades)
		{
			if (Position > 0m)
			CloseLong(candle.ClosePrice);

			EnterShort(candle.ClosePrice);
		}

		StorePreviousCandles(candle);
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var distance = Math.Abs(momentum - 100m);
		_momentumAbs3 = _momentumAbs2;
		_momentumAbs2 = _momentumAbs1;
		_momentumAbs1 = distance;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
		return;

		if (value is MovingAverageConvergenceDivergenceSignalValue macdSignalValue)
		{
			_macdMain = macdSignalValue.Macd;
			_macdSignal = macdSignalValue.Signal;
		}
		else if (value is MovingAverageConvergenceDivergenceValue macdValue)
		{
			_macdMain = macdValue.Macd;
			_macdSignal = macdValue.Signal;
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = CalculateNextVolume(Sides.Buy);
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		_longTradeCount++;
		_maxFloatingProfit = 0m;
		_highestPrice = price;
	}

	private void EnterShort(decimal price)
	{
		var volume = CalculateNextVolume(Sides.Sell);
		if (volume <= 0m)
		return;

		SellMarket(volume);
		_shortTradeCount++;
		_maxFloatingProfit = 0m;
		_lowestPrice = price;
	}

	private void CloseLong(decimal price)
	{
		if (Position <= 0m)
		return;

		SellMarket(Position);
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
	}

	private void CloseShort(decimal price)
	{
		if (Position >= 0m)
		return;

		BuyMarket(-Position);
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
	}

	private bool CheckPriceStops(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				CloseLong(stop);
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				CloseLong(take);
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				CloseShort(stop);
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				CloseShort(take);
				return true;
			}
		}

		return false;
	}

	private void TryActivateBreakeven(decimal closePrice)
	{
		if (!UseBreakEven || _breakevenPrice is decimal)
		return;

		if (Position == 0m || PositionPrice is not decimal entry)
		return;

		var triggerDistance = StepsToPrice(BreakEvenTriggerSteps);
		var offset = StepsToPrice(BreakEvenOffsetSteps);

		if (triggerDistance <= 0m)
		return;

		if (Position > 0m && closePrice - entry >= triggerDistance)
		{
			_breakevenPrice = entry + offset;
			_stopPrice = _breakevenPrice;
		}
		else if (Position < 0m && entry - closePrice >= triggerDistance)
		{
			_breakevenPrice = entry - offset;
			_stopPrice = _breakevenPrice;
		}
	}

	private bool TryApplyBreakEvenExit(decimal closePrice)
	{
		if (_breakevenPrice is not decimal breakEven || Position == 0m)
		return false;

		if (Position > 0m && closePrice <= breakEven)
		{
			CloseLong(breakEven);
			return true;
		}

		if (Position < 0m && closePrice >= breakEven)
		{
			CloseShort(breakEven);
			return true;
		}

		return false;
	}

	private void ApplyTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopSteps <= 0m || Position == 0m || PositionPrice is not decimal entry)
		return;

		var distance = StepsToPrice(TrailingStopSteps);
		if (distance <= 0m)
		return;

		if (Position > 0m)
		{
			var newStop = candle.ClosePrice - distance;
			if (newStop > entry - distance && (_stopPrice is not decimal current || newStop > current))
			_stopPrice = newStop;
		}
		else if (Position < 0m)
		{
			var newStop = candle.ClosePrice + distance;
			if (newStop < entry + distance && (_stopPrice is not decimal current || newStop < current))
			_stopPrice = newStop;
		}
	}

	private bool TryApplyMoneyTargets(decimal closePrice)
	{
		if (Position == 0m)
		return false;

		var profit = GetFloatingProfit(closePrice);
		if (profit <= 0m && !EnableMoneyTrailing)
		return false;

		if (UseMoneyTakeProfit && profit >= MoneyTakeProfit && profit > 0m)
		{
			ClosePosition();
			return true;
		}

		if (UsePercentTakeProfit && _initialCapital > 0m)
		{
			var target = _initialCapital * PercentTakeProfit / 100m;
			if (profit >= target && profit > 0m)
			{
				ClosePosition();
				return true;
			}
		}

		if (EnableMoneyTrailing && profit > 0m)
		{
			if (profit >= MoneyTrailingTrigger)
			_maxFloatingProfit = Math.Max(_maxFloatingProfit, profit);

			if (_maxFloatingProfit > 0m && _maxFloatingProfit - profit >= MoneyTrailingStop)
			{
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private bool TryApplyEquityStop(decimal closePrice)
	{
		if (!UseEquityStop)
		return false;

		var profit = GetFloatingProfit(closePrice);
		var realized = PnL;
		var equity = _initialCapital + realized + profit;
		_equityPeak = Math.Max(_equityPeak, equity);

		if (profit >= 0m || _equityPeak <= 0m)
		return false;

		var threshold = _equityPeak * TotalEquityRisk / 100m;
		if (Math.Abs(profit) >= threshold)
		{
			ClosePosition();
			return true;
		}

		return false;
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}

		_stopPrice = null;
		_takeProfitPrice = null;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
	}

	private void UpdateExtremes(ICandleMessage candle)
	{
		if (Position > 0m)
		_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
		else if (Position < 0m)
		_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
	}

	private void StorePreviousCandles(ICandleMessage candle)
	{
		_previousCandle2 = _previousCandle;
		_previousCandle = candle;
	}

	private decimal CalculateNextVolume(Sides side)
	{
		var count = side == Sides.Buy ? _longTradeCount : _shortTradeCount;
		var volume = BaseVolume * (decimal)Math.Pow((double)LotExponent, count);
		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var min = Security?.MinVolume ?? 0m;
		var max = Security?.MaxVolume ?? decimal.MaxValue;
		var step = Security?.VolumeStep ?? 0m;

		if (step > 0m)
		{
			var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = ratio * step;
		}

		if (min > 0m && volume < min)
		volume = min;

		if (volume > max)
		volume = max;

		return volume;
	}

	private decimal StepsToPrice(decimal steps)
	{
		if (_pipSize <= 0m)
		return 0m;

		return steps * _pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		if (step < 1m)
		return step * 10m;

		return step;
	}

	private decimal GetFloatingProfit(decimal price)
	{
		if (Position == 0m || PositionPrice is not decimal entry)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m || _stepPrice <= 0m)
		return 0m;

		var direction = Position > 0m ? 1m : -1m;
		var priceDiff = (price - entry) * direction;
		var steps = priceDiff / priceStep;
		return steps * _stepPrice * Math.Abs(Position);
	}

	private static DataType GetHigherTimeFrame(DataType type)
	{
		if (type.MessageType != typeof(TimeFrameCandleMessage))
		return type;

		var tf = ((TimeFrameCandleMessage)type.Message).TimeFrame;
		var minutes = tf.TotalMinutes;

		return minutes switch
		{
			1 => TimeSpan.FromMinutes(15).TimeFrame(),
			5 => TimeSpan.FromMinutes(30).TimeFrame(),
			15 => TimeSpan.FromHours(1).TimeFrame(),
			30 => TimeSpan.FromHours(4).TimeFrame(),
			60 => TimeSpan.FromDays(1).TimeFrame(),
			240 => TimeSpan.FromDays(7).TimeFrame(),
			1440 => TimeSpan.FromDays(30).TimeFrame(),
			10080 => TimeSpan.FromDays(30).TimeFrame(),
			43200 => TimeSpan.FromDays(30).TimeFrame(),
			_ => TimeSpan.FromMinutes(Math.Max(1, minutes * 4)).TimeFrame()
		};
	}
}
