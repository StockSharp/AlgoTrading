using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum ArrowMode
{
	HideArrows,
	SimpleArrows,
	OpenCloseMedian,
	HighLowOpenClose
}

public class FollowLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _barsCount;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviations;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<ArrowMode> _arrowMode;
	private readonly StrategyParam<int> _indicatorShift;
	private readonly StrategyParam<bool> _closeInSignal;
	private readonly StrategyParam<bool> _useBasketClose;
	private readonly StrategyParam<bool> _closeInProfit;
	private readonly StrategyParam<decimal> _pipsCloseProfit;
	private readonly StrategyParam<bool> _closeInLoss;
	private readonly StrategyParam<decimal> _pipsCloseLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEven;
	private readonly StrategyParam<decimal> _breakEvenAfter;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _timeStartTrade;
	private readonly StrategyParam<int> _timeEndTrade;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<int> _maxOrders;

	private BollingerBands? _bollinger;
	private AverageTrueRange? _atr;
	private SimpleMovingAverage? _highSma;
	private SimpleMovingAverage? _openSma;
	private SimpleMovingAverage? _closeSma;
	private SimpleMovingAverage? _lowSma;
	private SimpleMovingAverage? _medianSma;

	private decimal? _previousTrendLine;
	private int _previousTrendDirection;
	private bool _allowBuyArrow = true;
	private bool _allowSellArrow = true;
	private bool _initialized;
	private readonly Queue<SignalDirection> _pendingSignals = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public FollowLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Execution timeframe", "General");

		_barsCount = Param(nameof(BarsCount), 10)
			.SetGreaterThanZero()
			.SetDisplay("Bars Count", "Amount of bars analysed", "Indicator");

		_bbPeriod = Param(nameof(BollingerPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger period", "Indicator");

		_bbDeviations = Param(nameof(BollingerDeviations), 1m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviations", "Band width multiplier", "Indicator");

		_maPeriod = Param(nameof(MovingAveragePeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length", "Indicator");

		_useAtrFilter = Param(nameof(UseAtrFilter), false)
			.SetDisplay("Use ATR Filter", "Extend trend line by ATR", "Indicator");

		_arrowMode = Param(nameof(TypeOfArrows), ArrowMode.SimpleArrows)
			.SetDisplay("Arrow Mode", "Signal confirmation type", "Indicator");

		_indicatorShift = Param(nameof(IndicatorsShift), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Indicator Shift", "Bars delay before execution", "Indicator");

		_closeInSignal = Param(nameof(CloseInSignal), false)
			.SetDisplay("Close On Signal", "Exit when opposite arrow appears", "Risk");

		_useBasketClose = Param(nameof(UseBasketClose), false)
			.SetDisplay("Use Basket Close", "Close both sides together", "Risk");

		_closeInProfit = Param(nameof(CloseInProfit), false)
			.SetDisplay("Close In Profit", "Lock basket when pips profit reached", "Risk");

		_pipsCloseProfit = Param(nameof(PipsCloseProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Pips", "Pips target for manual close", "Risk");

		_closeInLoss = Param(nameof(CloseInLoss), false)
			.SetDisplay("Close In Loss", "Exit basket when loss reached", "Risk");

		_pipsCloseLoss = Param(nameof(PipsCloseLoss), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Loss Pips", "Pips threshold for basket loss", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target in steps", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop in steps", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing distance in steps", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Minimum move to shift trailing", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), false)
			.SetDisplay("Use BreakEven", "Protect trade once in profit", "Risk");

		_breakEven = Param(nameof(BreakEven), 4m)
			.SetGreaterThanZero()
			.SetDisplay("BreakEven", "Final stop distance", "Risk");

		_breakEvenAfter = Param(nameof(BreakEvenAfter), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BreakEven After", "Profit required before break even", "Risk");

		_autoLotSize = Param(nameof(AutoLotSize), true)
			.SetDisplay("Auto Lot", "Use balance based volume", "Money Management");

		_riskFactor = Param(nameof(RiskFactor), 1m)
			.SetDisplay("Risk Factor", "Balance percentage per trade", "Money Management");

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
			.SetDisplay("Manual Lot", "Fixed volume when auto lot disabled", "Money Management");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Limit trading hours", "Time");

		_timeStartTrade = Param(nameof(TimeStartTrade), 0)
			.SetDisplay("Start Hour", "Trading window start hour", "Time");

		_timeEndTrade = Param(nameof(TimeEndTrade), 0)
			.SetDisplay("End Hour", "Trading window end hour", "Time");

		_maxSpread = Param(nameof(MaxSpread), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Spread", "Maximum allowed spread in steps", "Filters");

		_maxOrders = Param(nameof(MaxOrders), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Orders", "Maximum simultaneous volume", "Filters");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BarsCount
	{
		get => _barsCount.Value;
		set => _barsCount.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal BollingerDeviations
	{
		get => _bbDeviations.Value;
		set => _bbDeviations.Value = value;
	}

	public int MovingAveragePeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	public ArrowMode TypeOfArrows
	{
		get => _arrowMode.Value;
		set => _arrowMode.Value = value;
	}

	public int IndicatorsShift
	{
		get => _indicatorShift.Value;
		set => _indicatorShift.Value = value;
	}

	public bool CloseInSignal
	{
		get => _closeInSignal.Value;
		set => _closeInSignal.Value = value;
	}

	public bool UseBasketClose
	{
		get => _useBasketClose.Value;
		set => _useBasketClose.Value = value;
	}

	public bool CloseInProfit
	{
		get => _closeInProfit.Value;
		set => _closeInProfit.Value = value;
	}

	public decimal PipsCloseProfit
	{
		get => _pipsCloseProfit.Value;
		set => _pipsCloseProfit.Value = value;
	}

	public bool CloseInLoss
	{
		get => _closeInLoss.Value;
		set => _closeInLoss.Value = value;
	}

	public decimal PipsCloseLoss
	{
		get => _pipsCloseLoss.Value;
		set => _pipsCloseLoss.Value = value;
	}

	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public decimal BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	public decimal BreakEvenAfter
	{
		get => _breakEvenAfter.Value;
		set => _breakEvenAfter.Value = value;
	}

	public bool AutoLotSize
	{
		get => _autoLotSize.Value;
		set => _autoLotSize.Value = value;
	}

	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	public decimal ManualLotSize
	{
		get => _manualLotSize.Value;
		set => _manualLotSize.Value = value;
	}

	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	public int TimeStartTrade
	{
		get => _timeStartTrade.Value;
		set => _timeStartTrade.Value = value;
	}

	public int TimeEndTrade
	{
		get => _timeEndTrade.Value;
		set => _timeEndTrade.Value = value;
	}

	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}

	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviations
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_highSma = new SimpleMovingAverage { Length = MovingAveragePeriod };
		_openSma = new SimpleMovingAverage { Length = MovingAveragePeriod };
		_closeSma = new SimpleMovingAverage { Length = MovingAveragePeriod };
		_lowSma = new SimpleMovingAverage { Length = MovingAveragePeriod };
		_medianSma = new SimpleMovingAverage { Length = MovingAveragePeriod };

		_pendingSignals.Clear();
		_previousTrendDirection = 0;
		_previousTrendLine = null;
		_allowBuyArrow = true;
		_allowSellArrow = true;
		_initialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_bollinger == null || _atr == null || _highSma == null || _openSma == null || _closeSma == null || _lowSma == null || _medianSma == null)
			return;

		var bollingerValue = _bollinger.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var atrValue = _atr.Process(new CandleIndicatorValue(_atr, candle));
		var highValue = _highSma.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var openValue = _openSma.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
		var closeValue = _closeSma.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var lowValue = _lowSma.Process(new CandleIndicatorValue(candle, candle.LowPrice));
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var medianValue = _medianSma.Process(new CandleIndicatorValue(candle, medianPrice));

		if (!bollingerValue.IsFinal)
			return;

		if (TypeOfArrows != ArrowMode.SimpleArrows && TypeOfArrows != ArrowMode.HideArrows)
		{
			if (!openValue.IsFinal || !closeValue.IsFinal || !highValue.IsFinal || !lowValue.IsFinal || !medianValue.IsFinal)
				return;
		}

		if (UseAtrFilter && !atrValue.IsFinal)
			return;

		if (CloseInProfit || CloseInLoss)
			CheckBasketManagement(candle);

		var signal = CalculateSignal(
			candle,
			bollingerValue,
			atrValue,
			openValue,
			closeValue,
			highValue,
			lowValue,
			medianValue);

		_pendingSignals.Enqueue(signal);

		while (_pendingSignals.Count > IndicatorsShift)
		{
			var pending = _pendingSignals.Dequeue();
			ExecuteSignal(candle, pending);
		}

		UpdateTrailing(candle);
	}

	private SignalDirection CalculateSignal(
		ICandleMessage candle,
		IIndicatorValue bollingerValue,
		IIndicatorValue atrValue,
		IIndicatorValue openValue,
		IIndicatorValue closeValue,
		IIndicatorValue highValue,
		IIndicatorValue lowValue,
		IIndicatorValue medianValue)
	{
		if (bollingerValue is not BollingerBandsValue bb)
			return SignalDirection.None;

		if (bb.UpBand is not decimal upperBand || bb.LowBand is not decimal lowerBand)
			return SignalDirection.None;

		decimal atr = 0m;
		if (UseAtrFilter)
		{
			if (!atrValue.TryGetValue(out atr))
				return SignalDirection.None;
		}

		int maSignal = 0;
		if (TypeOfArrows != ArrowMode.SimpleArrows && TypeOfArrows != ArrowMode.HideArrows)
		{
			if (!openValue.TryGetValue(out decimal openMa) ||
				!closeValue.TryGetValue(out decimal closeMa) ||
				!highValue.TryGetValue(out decimal highMa) ||
				!lowValue.TryGetValue(out decimal lowMa) ||
				!medianValue.TryGetValue(out decimal medianMa))
			{
				return SignalDirection.None;
			}

			if (TypeOfArrows == ArrowMode.OpenCloseMedian)
			{
				if (closeMa > openMa && medianMa < closeMa && medianMa > openMa)
					maSignal = 1;
				else if (closeMa < openMa && medianMa > closeMa && medianMa < openMa)
					maSignal = -1;
			}
			else if (TypeOfArrows == ArrowMode.HighLowOpenClose)
			{
				var diffHoc = (highMa - openMa) + (highMa - closeMa);
				var diffLoc = (openMa - lowMa) + (closeMa - lowMa);

				if (closeMa > openMa && diffHoc > diffLoc)
					maSignal = 1;
				else if (closeMa < openMa && diffHoc < diffLoc)
					maSignal = -1;
			}
		}

		var closePrice = candle.ClosePrice;
		var highPrice = candle.HighPrice;
		var lowPrice = candle.LowPrice;

		var bollingerSignal = 0;
		if (closePrice > upperBand)
			bollingerSignal = 1;
		else if (closePrice < lowerBand)
			bollingerSignal = -1;

		var trendLine = _previousTrendLine ?? closePrice;

		if (bollingerSignal > 0)
		{
			var candidate = UseAtrFilter ? lowPrice - atr : lowPrice;
			trendLine = _previousTrendLine.HasValue && candidate < _previousTrendLine.Value
				? _previousTrendLine.Value
				: candidate;
		}
		else if (bollingerSignal < 0)
		{
			var candidate = UseAtrFilter ? highPrice + atr : highPrice;
			trendLine = _previousTrendLine.HasValue && candidate > _previousTrendLine.Value
				? _previousTrendLine.Value
				: candidate;
		}

		var trendDirection = _previousTrendDirection;
		if (_previousTrendLine.HasValue)
		{
			if (trendLine > _previousTrendLine.Value)
				trendDirection = 1;
			else if (trendLine < _previousTrendLine.Value)
				trendDirection = -1;
		}

		SignalDirection signal = SignalDirection.None;

		if (!_initialized)
		{
			_initialized = true;
		}
		else
		{
			if (trendDirection > 0)
			{
				_allowSellArrow = true;
				var shouldTrigger = TypeOfArrows == ArrowMode.SimpleArrows
					? _previousTrendDirection < 0
					: TypeOfArrows != ArrowMode.HideArrows;
				var maConfirmed = TypeOfArrows == ArrowMode.SimpleArrows || maSignal > 0;

				if (_allowBuyArrow && TypeOfArrows != ArrowMode.HideArrows && shouldTrigger && maConfirmed)
				{
					signal = SignalDirection.Buy;
					_allowBuyArrow = false;
					_allowSellArrow = true;
				}
			}
			else if (trendDirection < 0)
			{
				_allowBuyArrow = true;
				var shouldTrigger = TypeOfArrows == ArrowMode.SimpleArrows
					? _previousTrendDirection > 0
					: TypeOfArrows != ArrowMode.HideArrows;
				var maConfirmed = TypeOfArrows == ArrowMode.SimpleArrows || maSignal < 0;

				if (_allowSellArrow && TypeOfArrows != ArrowMode.HideArrows && shouldTrigger && maConfirmed)
				{
					signal = SignalDirection.Sell;
					_allowSellArrow = false;
					_allowBuyArrow = true;
				}
			}
		}

		_previousTrendLine = trendLine;
		_previousTrendDirection = trendDirection;

		return signal;
	}

	private void ExecuteSignal(ICandleMessage candle, SignalDirection signal)
	{
		if (signal == SignalDirection.None)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
			return;

		if (!IsSpreadAccepted())
			return;

		if (signal == SignalDirection.Buy)
		{
			if (CloseInSignal && Position < 0)
				ClosePosition();

if (Position >= 0 && IsOrderSlotAvailable())
			{
				var volume = GetOrderVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);
					_longEntryPrice = candle.ClosePrice;
				}
			}
		}
		else if (signal == SignalDirection.Sell)
		{
			if (CloseInSignal && Position > 0)
				ClosePosition();

if (Position <= 0 && IsOrderSlotAvailable())
			{
				var volume = GetOrderVolume();
				if (volume > 0m)
				{
					SellMarket(volume);
					_shortEntryPrice = candle.ClosePrice;
				}
			}
		}
	}

	private void CheckBasketManagement(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		var step = Security.PriceStep ?? 1m;
		if (step <= 0m)
			return;

		var entryPrice = Position > 0 ? _longEntryPrice ?? candle.ClosePrice : _shortEntryPrice ?? candle.ClosePrice;
		var profitPips = Position > 0
			? (candle.ClosePrice - entryPrice) / step
			: (entryPrice - candle.ClosePrice) / step;

		if (CloseInProfit)
		{
			if ((UseBasketClose && profitPips >= PipsCloseProfit) || (!UseBasketClose && profitPips >= PipsCloseProfit))
			{
				ClosePosition();
				return;
			}
		}

		if (CloseInLoss)
		{
			if ((UseBasketClose && profitPips <= -PipsCloseLoss) || (!UseBasketClose && profitPips <= -PipsCloseLoss))
				ClosePosition();
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		var step = Security.PriceStep ?? 1m;
		if (step <= 0m)
			return;

		if (UseTrailingStop && TrailingStop > 0m && TrailingStep > 0m && Position != 0)
		{
			SetStopLoss(TrailingStop, candle.ClosePrice, Position);
		}

		if (UseBreakEven && BreakEven > 0m && BreakEvenAfter > 0m && Position != 0)
		{
			var entry = Position > 0 ? _longEntryPrice : _shortEntryPrice;
			if (entry.HasValue)
			{
				var stepDistance = BreakEvenAfter * step;
				var stopDistance = BreakEven * step;

				if (Position > 0 && candle.ClosePrice - entry.Value >= stepDistance)
				{
					var stop = entry.Value + stopDistance;
					SetStopLoss(BreakEven, stop, Position);
				}
				else if (Position < 0 && entry.Value - candle.ClosePrice >= stepDistance)
				{
					var stop = entry.Value - stopDistance;
					SetStopLoss(BreakEven, stop, Position);
				}
			}
		}

		if (UseTakeProfit && TakeProfit > 0m && Position != 0)
		{
			SetTakeProfit(TakeProfit, candle.ClosePrice, Position);
		}

		if (UseStopLoss && StopLoss > 0m && Position != 0)
		{
			SetStopLoss(StopLoss, candle.ClosePrice, Position);
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var start = TimeStartTrade;
		var end = TimeEndTrade;
		var hour = time.Hour;

		if (start == end)
			return true;

		if (start < end)
			return hour >= start && hour < end;

		return hour >= start || hour < end;
	}

	private bool IsSpreadAccepted()
	{
		if (MaxSpread <= 0m)
			return true;

		var bestBid = Security.BestBid?.Price;
		var bestAsk = Security.BestAsk?.Price;
		var step = Security.PriceStep ?? 0m;

		if (bestBid is null || bestAsk is null || step <= 0m)
			return true;

		var spread = (bestAsk.Value - bestBid.Value) / step;
		return spread <= MaxSpread;
	}

	private decimal GetOrderVolume()
	{
		if (!AutoLotSize)
			return NormalizeVolume(ManualLotSize);

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
			return NormalizeVolume(ManualLotSize);

		var amount = balance * (RiskFactor / 100m);
		return NormalizeVolume(amount);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var step = Security.VolumeStep ?? 1m;
		var min = Security.VolumeMin ?? step;
		var max = Security.VolumeMax ?? volume;

		if (step <= 0m)
			return volume;

		var normalized = Math.Floor(volume / step) * step;
		normalized = Math.Max(normalized, min);
		normalized = max > 0m ? Math.Min(normalized, max) : normalized;
		return normalized;
	}

	private bool IsOrderSlotAvailable()
	{
		if (MaxOrders <= 0)
			return true;

		var totalPosition = Math.Abs(Position);
		return totalPosition < MaxOrders;
	}

	private enum SignalDirection
	{
		None,
		Buy,
		Sell
	}
}
