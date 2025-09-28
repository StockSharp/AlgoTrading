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

public class CryptoScalperMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macroCandleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiOversold;
	private readonly StrategyParam<decimal> _mfiOverbought;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _momentumReference;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _macroMacdFastPeriod;
	private readonly StrategyParam<int> _macroMacdSlowPeriod;
	private readonly StrategyParam<int> _macroMacdSignalPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _useCandleTrail;
	private readonly StrategyParam<int> _trailTriggerPips;
	private readonly StrategyParam<int> _trailAmountPips;
	private readonly StrategyParam<int> _candleTrailLength;
	private readonly StrategyParam<decimal> _candleTrailBufferPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailTarget;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<bool> _forceExit;

	private MoneyFlowIndex _mfi = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _primaryMacd = null!;
	private MovingAverageConvergenceDivergence _macroMacd = null!;

	private readonly Queue<decimal> _mfiHistory = new();
	private readonly Queue<decimal> _momentumHistory = new();
	private readonly Queue<decimal> _recentLows = new();
	private readonly Queue<decimal> _recentHighs = new();

	private decimal _tickSize;
	private decimal _lastMacdLine;
	private decimal _lastMacdSignal;
	private decimal _macroMacdLine;
	private decimal _macroSignalLine;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;
	private bool _longBreakEvenArmed;
	private bool _shortBreakEvenArmed;
	private decimal _moneyTrailPeak;
	private decimal _equityPeak;
	private decimal _initialEquity;

	public CryptoScalperMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Primary Candle", "Base timeframe used for entries", "Data");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Momentum Candle", "Higher timeframe used for momentum confirmation", "Data");

		_macroCandleType = Param(nameof(MacroCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Macro Candle", "Slow timeframe used for macro MACD filter", "Data");

		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MFI Period", "Money Flow Index period", "Indicators");

		_mfiOversold = Param(nameof(MfiOversold), 30m)
		.SetNotNegative()
		.SetDisplay("MFI Oversold", "Oversold threshold for MFI", "Indicators");

		_mfiOverbought = Param(nameof(MfiOverbought), 70m)
		.SetNotNegative()
		.SetDisplay("MFI Overbought", "Overbought threshold for MFI", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum length on the higher timeframe", "Indicators");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Threshold", "Minimum deviation from the 100 level", "Indicators");

		_momentumReference = Param(nameof(MomentumReference), 100m)
		.SetDisplay("Momentum Reference", "Reference level used in MetaTrader momentum", "Indicators");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length on the primary timeframe", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length on the primary timeframe", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length on the primary timeframe", "Indicators");

		_macroMacdFastPeriod = Param(nameof(MacroMacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Macro MACD Fast", "Fast EMA length on the macro timeframe", "Indicators");

		_macroMacdSlowPeriod = Param(nameof(MacroMacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Macro MACD Slow", "Slow EMA length on the macro timeframe", "Indicators");

		_macroMacdSignalPeriod = Param(nameof(MacroMacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Macro MACD Signal", "Signal EMA length on the macro timeframe", "Indicators");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume used for each market order", "Trading");

		_maxTrades = Param(nameof(MaxTrades), 1)
		.SetNotNegative()
		.SetDisplay("Max Trades", "Maximum simultaneous trades per direction", "Trading");

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop", "Enable the protective stop-loss", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Target", "Enable the protective take-profit", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Distance to the protective stop", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Distance to the protective target", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing", "Enable trailing logic", "Risk");

		_useCandleTrail = Param(nameof(UseCandleTrail), true)
		.SetDisplay("Use Candle Trail", "Trail stops with recent candle extremes", "Risk");

		_trailTriggerPips = Param(nameof(TrailTriggerPips), 40)
		.SetNotNegative()
		.SetDisplay("Trail Trigger", "Profit in pips before trailing activates", "Risk");

		_trailAmountPips = Param(nameof(TrailAmountPips), 40)
		.SetNotNegative()
		.SetDisplay("Trail Amount", "Distance maintained by the trailing stop", "Risk");

		_candleTrailLength = Param(nameof(CandleTrailLength), 3)
		.SetNotNegative()
		.SetDisplay("Candle Trail Length", "Number of candles used for trailing extremes", "Risk");

		_candleTrailBufferPips = Param(nameof(CandleTrailBufferPips), 3m)
		.SetNotNegative()
		.SetDisplay("Trail Buffer", "Extra pips added beyond the extreme", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break-Even", "Move stop once price travels in favor", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetNotNegative()
		.SetDisplay("Break-Even Trigger", "Distance before break-even is armed", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetNotNegative()
		.SetDisplay("Break-Even Offset", "Pips locked in once break-even triggers", "Risk");

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
		.SetDisplay("Use Money TP", "Close all trades at fixed profit in currency", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
		.SetNotNegative()
		.SetDisplay("Money Take Profit", "Currency profit target for the basket", "Risk");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
		.SetDisplay("Use Percent TP", "Close all trades at fixed percent of balance", "Risk");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
		.SetNotNegative()
		.SetDisplay("Percent Take Profit", "Percentage gain that triggers liquidation", "Risk");

		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), false)
		.SetDisplay("Enable Money Trailing", "Trail floating profit in account currency", "Risk");

		_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
		.SetNotNegative()
		.SetDisplay("Money Trail Target", "Profit level that arms the trailing logic", "Risk");

		_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
		.SetNotNegative()
		.SetDisplay("Money Trail Stop", "Maximum allowed pullback in currency", "Risk");

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use Equity Stop", "Close positions on deep equity drawdown", "Risk");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
		.SetNotNegative()
		.SetDisplay("Equity Risk Percent", "Maximum drawdown percentage from peak", "Risk");

		_forceExit = Param(nameof(ForceExit), false)
		.SetDisplay("Force Exit", "Close all positions on the next bar", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	public DataType MacroCandleType
	{
		get => _macroCandleType.Value;
		set => _macroCandleType.Value = value;
	}

	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	public decimal MfiOversold
	{
		get => _mfiOversold.Value;
		set => _mfiOversold.Value = value;
	}

	public decimal MfiOverbought
	{
		get => _mfiOverbought.Value;
		set => _mfiOverbought.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	public decimal MomentumReference
	{
		get => _momentumReference.Value;
		set => _momentumReference.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public int MacroMacdFastPeriod
	{
		get => _macroMacdFastPeriod.Value;
		set => _macroMacdFastPeriod.Value = value;
	}

	public int MacroMacdSlowPeriod
	{
		get => _macroMacdSlowPeriod.Value;
		set => _macroMacdSlowPeriod.Value = value;
	}

	public int MacroMacdSignalPeriod
	{
		get => _macroMacdSignalPeriod.Value;
		set => _macroMacdSignalPeriod.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public bool UseCandleTrail
	{
		get => _useCandleTrail.Value;
		set => _useCandleTrail.Value = value;
	}

	public int TrailTriggerPips
	{
		get => _trailTriggerPips.Value;
		set => _trailTriggerPips.Value = value;
	}

	public int TrailAmountPips
	{
		get => _trailAmountPips.Value;
		set => _trailAmountPips.Value = value;
	}

	public int CandleTrailLength
	{
		get => _candleTrailLength.Value;
		set => _candleTrailLength.Value = value;
	}

	public decimal CandleTrailBufferPips
	{
		get => _candleTrailBufferPips.Value;
		set => _candleTrailBufferPips.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	public decimal MoneyTrailTarget
	{
		get => _moneyTrailTarget.Value;
		set => _moneyTrailTarget.Value = value;
	}

	public decimal MoneyTrailStop
	{
		get => _moneyTrailStop.Value;
		set => _moneyTrailStop.Value = value;
	}

	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	public bool ForceExit
	{
		get => _forceExit.Value;
		set => _forceExit.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(Security, MomentumCandleType),
		(Security, MacroCandleType)
		];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_mfiHistory.Clear();
		_momentumHistory.Clear();
		_recentHighs.Clear();
		_recentLows.Clear();
		_lastMacdLine = 0m;
		_lastMacdSignal = 0m;
		_macroMacdLine = 0m;
		_macroSignalLine = 0m;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_longBreakEvenArmed = false;
		_shortBreakEvenArmed = false;
		_moneyTrailPeak = 0m;
		_equityPeak = 0m;
		_initialEquity = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_primaryMacd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = MacdFastPeriod },
			LongMa = { Length = MacdSlowPeriod },
			SignalMa = { Length = MacdSignalPeriod }
		};

		_macroMacd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = MacroMacdFastPeriod },
			LongMa = { Length = MacroMacdSlowPeriod },
			SignalMa = { Length = MacroMacdSignalPeriod }
		};

		_tickSize = Security?.PriceStep ?? 0m;
		if (_tickSize <= 0m)
		_tickSize = 0.0001m;

		Volume = TradeVolume;

		_initialEquity = GetPortfolioValue();
		_equityPeak = _initialEquity;
		_moneyTrailPeak = 0m;

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
		.BindEx(_mfi, _primaryMacd, ProcessPrimary)
		.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
		.Bind(_momentum, ProcessMomentum)
		.Start();

		var macroSubscription = SubscribeCandles(MacroCandleType);
		macroSubscription
		.BindEx(_macroMacd, ProcessMacro)
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, primarySubscription);
			DrawIndicator(priceArea, _mfi);
			DrawIndicator(priceArea, _primaryMacd);
			DrawOwnTrades(priceArea);
		}
	}

	protected override void OnOwnTradeReceived(MyTrade myTrade)
	{
		base.OnOwnTradeReceived(myTrade);

		if (myTrade.Order.Security != Security)
		return;

		if (Position > 0)
		{
			_longStop = UseStopLoss && StopLossPips > 0 ? myTrade.Trade.Price - GetStepValue(StopLossPips) : null;
			_longTake = UseTakeProfit && TakeProfitPips > 0 ? myTrade.Trade.Price + GetStepValue(TakeProfitPips) : null;
			_longBreakEvenArmed = false;
		}
		else if (Position < 0)
		{
			_shortStop = UseStopLoss && StopLossPips > 0 ? myTrade.Trade.Price + GetStepValue(StopLossPips) : null;
			_shortTake = UseTakeProfit && TakeProfitPips > 0 ? myTrade.Trade.Price - GetStepValue(TakeProfitPips) : null;
			_shortBreakEvenArmed = false;
		}
	}

	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0)
		ResetRiskState();
	}

	private void ProcessPrimary(ICandleMessage candle, IIndicatorValue mfiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateRecentExtremes(candle);
		UpdateMoneyManagement(candle);
		HandleForceExit();

		ManageLongPosition(candle);
		ManageShortPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!mfiValue.IsFinal || !macdValue.IsFinal)
		return;

		var mfi = mfiValue.ToDecimal();
		StoreHistory(_mfiHistory, mfi, 3);

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal macdSignal)
		return;

		_lastMacdLine = macdLine;
		_lastMacdSignal = macdSignal;

		if (ShouldOpenLong() && CanOpenLong())
		{
			BuyMarket(TradeVolume);
		}
		else if (ShouldOpenShort() && CanOpenShort())
		{
			SellMarket(TradeVolume);
		}
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var diff = Math.Abs(momentumValue - MomentumReference);
		StoreHistory(_momentumHistory, diff, 3);
	}

	private void ProcessMacro(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal)
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal macdSignal)
		return;

		_macroMacdLine = macdLine;
		_macroSignalLine = macdSignal;

		if (Position > 0 && macdLine < macdSignal)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && macdLine > macdSignal)
		{
			BuyMarket(-Position);
		}
	}

	private bool ShouldOpenLong()
	{
		if (_mfiHistory.Count == 0 || _momentumHistory.Count == 0)
		return false;

		var mfiOversold = _mfiHistory.Any(v => v <= MfiOversold);
		if (!mfiOversold)
		return false;

		var momentumReady = _momentumHistory.Any(v => v >= MomentumThreshold);
		if (!momentumReady)
		return false;

		if (_lastMacdLine <= _lastMacdSignal)
		return false;

		if (_macroMacdLine <= _macroSignalLine)
		return false;

		return true;
	}

	private bool ShouldOpenShort()
	{
		if (_mfiHistory.Count == 0 || _momentumHistory.Count == 0)
		return false;

		var mfiOverbought = _mfiHistory.Any(v => v >= MfiOverbought);
		if (!mfiOverbought)
		return false;

		var momentumReady = _momentumHistory.Any(v => v >= MomentumThreshold);
		if (!momentumReady)
		return false;

		if (_lastMacdLine >= _lastMacdSignal)
		return false;

		if (_macroMacdLine >= _macroSignalLine)
		return false;

		return true;
	}

	private bool CanOpenLong()
	{
		if (TradeVolume <= 0m)
		return false;

		if (Position < 0)
		return false;

		if (MaxTrades == 0)
		return true;

		var maxVolume = MaxTrades * TradeVolume;
		return Position < maxVolume;
	}

	private bool CanOpenShort()
	{
		if (TradeVolume <= 0m)
		return false;

		if (Position > 0)
		return false;

		if (MaxTrades == 0)
		return true;

		var maxVolume = MaxTrades * TradeVolume;
		return -Position < maxVolume;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0)
		return;

		UpdateLongStops(candle);

		if (_longTake is decimal take && candle.HighPrice >= take)
		{
			SellMarket(Position);
			ResetLongSide();
			return;
		}

		if (_longStop is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetLongSide();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0)
		return;

		UpdateShortStops(candle);

		if (_shortTake is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(-Position);
			ResetShortSide();
			return;
		}

		if (_shortStop is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(-Position);
			ResetShortSide();
		}
	}

	private void UpdateLongStops(ICandleMessage candle)
	{
		if (!UseTrailingStop)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice == 0m)
		return;

		if (UseBreakEven && !_longBreakEvenArmed)
		{
			var trigger = entryPrice + GetStepValue(BreakEvenTriggerPips);
			if (candle.HighPrice >= trigger || candle.ClosePrice >= trigger)
			{
				var candidate = entryPrice + GetStepValue(BreakEvenOffsetPips);
				_longStop = _longStop is null ? candidate : Math.Max(_longStop.Value, candidate);
				_longBreakEvenArmed = true;
			}
		}

		if (UseCandleTrail && CandleTrailLength > 0 && _recentLows.Count >= CandleTrailLength)
		{
			var trailingLow = _recentLows.Min();
			var candidate = trailingLow - GetStepValue(CandleTrailBufferPips);
			_longStop = _longStop is null ? candidate : Math.Max(_longStop.Value, candidate);
		}
		else if (!UseCandleTrail)
		{
			var trigger = entryPrice + GetStepValue(TrailTriggerPips);
			if (candle.ClosePrice >= trigger)
			{
				var candidate = candle.ClosePrice - GetStepValue(TrailAmountPips);
				_longStop = _longStop is null ? candidate : Math.Max(_longStop.Value, candidate);
			}
		}
	}

	private void UpdateShortStops(ICandleMessage candle)
	{
		if (!UseTrailingStop)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice == 0m)
		return;

		if (UseBreakEven && !_shortBreakEvenArmed)
		{
			var trigger = entryPrice - GetStepValue(BreakEvenTriggerPips);
			if (candle.LowPrice <= trigger || candle.ClosePrice <= trigger)
			{
				var candidate = entryPrice - GetStepValue(BreakEvenOffsetPips);
				_shortStop = _shortStop is null ? candidate : Math.Min(_shortStop.Value, candidate);
				_shortBreakEvenArmed = true;
			}
		}

		if (UseCandleTrail && CandleTrailLength > 0 && _recentHighs.Count >= CandleTrailLength)
		{
			var trailingHigh = _recentHighs.Max();
			var candidate = trailingHigh + GetStepValue(CandleTrailBufferPips);
			_shortStop = _shortStop is null ? candidate : Math.Min(_shortStop.Value, candidate);
		}
		else if (!UseCandleTrail)
		{
			var trigger = entryPrice - GetStepValue(TrailTriggerPips);
			if (candle.ClosePrice <= trigger)
			{
				var candidate = candle.ClosePrice + GetStepValue(TrailAmountPips);
				_shortStop = _shortStop is null ? candidate : Math.Min(_shortStop.Value, candidate);
			}
		}
	}

	private void UpdateMoneyManagement(ICandleMessage candle)
	{
		var unrealized = GetUnrealizedPnL(candle);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && unrealized >= MoneyTakeProfit)
		{
			ClosePosition();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialEquity > 0m)
		{
			var target = _initialEquity * PercentTakeProfit / 100m;
			if (unrealized >= target)
			{
				ClosePosition();
				return;
			}
		}

		if (EnableMoneyTrailing && MoneyTrailTarget > 0m && MoneyTrailStop > 0m)
		{
			if (unrealized >= MoneyTrailTarget)
			{
				_moneyTrailPeak = Math.Max(_moneyTrailPeak, unrealized);
				if (_moneyTrailPeak - unrealized >= MoneyTrailStop)
				ClosePosition();
			}
			else
			{
				_moneyTrailPeak = 0m;
			}
		}

		if (UseEquityStop && EquityRiskPercent > 0m)
		{
			var equity = GetPortfolioValue() + unrealized;
			_equityPeak = Math.Max(_equityPeak, equity);

			var drawdown = _equityPeak - equity;
			var limit = _equityPeak * EquityRiskPercent / 100m;

			if (drawdown >= limit && Position != 0)
			ClosePosition();
		}
	}

	private void HandleForceExit()
	{
		if (!ForceExit)
		return;

		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}

	private void UpdateRecentExtremes(ICandleMessage candle)
	{
		if (CandleTrailLength <= 0)
		return;

		_recentLows.Enqueue(candle.LowPrice);
		_recentHighs.Enqueue(candle.HighPrice);

		while (_recentLows.Count > CandleTrailLength)
		_recentLows.Dequeue();

		while (_recentHighs.Count > CandleTrailLength)
		_recentHighs.Dequeue();
	}

	private void ResetRiskState()
	{
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_longBreakEvenArmed = false;
		_shortBreakEvenArmed = false;
		_moneyTrailPeak = 0m;
	}

	private void ResetLongSide()
	{
		_longStop = null;
		_longTake = null;
		_longBreakEvenArmed = false;
	}

	private void ResetShortSide()
	{
		_shortStop = null;
		_shortTake = null;
		_shortBreakEvenArmed = false;
	}

	private static void StoreHistory(Queue<decimal> buffer, decimal value, int maxCount)
	{
		buffer.Enqueue(value);
		while (buffer.Count > maxCount)
		buffer.Dequeue();
	}

	private decimal GetStepValue(decimal pips)
	{
		return pips * _tickSize;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0)
		return 0m;

		var entry = Position.AveragePrice;
		if (entry == 0m)
		return 0m;

		var diff = candle.ClosePrice - entry;
		return diff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
		return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
		return portfolio.BeginValue;

		return 0m;
	}
}

