using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Heikin Ashi Trader" MetaTrader 4 strategy.
/// Combines Heikin-Ashi candles with weighted moving averages, stochastic, momentum, and MACD filters.
/// </summary>
public class HeikinAshiTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticOverbought;
	private readonly StrategyParam<decimal> _stochasticOversold;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerPips;
	private readonly StrategyParam<int> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _forceExit;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private StochasticOscillator _stochastic = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private readonly Queue<decimal> _stochasticHistory = new();
	private readonly Queue<decimal> _momentumHistory = new();

	private decimal _haOpenPrev;
	private decimal _haClosePrev;

	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private bool _longBreakEvenActivated;
	private bool _shortBreakEvenActivated;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeikinAshiTraderStrategy"/> class.
	/// </summary>
	public HeikinAshiTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for all indicators", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Length of the fast weighted moving average", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Length of the slow weighted moving average", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Main period of the stochastic oscillator", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Signal period of the stochastic oscillator", "Indicators");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Smoothing factor of the stochastic oscillator", "Indicators");

		_stochasticOverbought = Param(nameof(StochasticOverbought), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Overbought", "Threshold used for long setups", "Signals");

		_stochasticOversold = Param(nameof(StochasticOversold), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Oversold", "Threshold used for short setups", "Signals");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum indicator", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum Buy", "Minimum |momentum-100| required for longs", "Signals");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum Sell", "Minimum |momentum-100| required for shorts", "Signals");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Indicators");

		_closeOppositePositions = Param(nameof(CloseOppositePositions), true)
			.SetDisplay("Close Opposite", "Close opposite exposure before entering", "Trading");

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Positions", "Maximum net positions per side (0 = unlimited)", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume used for each new order", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Target distance in pips", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop", "Enable stop-loss handling", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Target", "Enable take-profit handling", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop adjustments", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing (pips)", "Trailing stop distance in pips", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Break-Even", "Move stop to break-even after a favorable move", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-Even Trigger", "Distance in pips before stop moves to breakeven", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-Even Offset", "Extra pips added when locking in break-even", "Risk");

		_forceExit = Param(nameof(ForceExit), false)
			.SetDisplay("Force Exit", "Flatten all positions on the next candle", "Trading");
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast weighted moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow weighted moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the stochastic %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Period of the stochastic %D signal line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing factor applied to the stochastic oscillator.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticOverbought
	{
		get => _stochasticOverbought.Value;
		set => _stochasticOverbought.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticOversold
	{
		get => _stochasticOversold.Value;
		set => _stochasticOversold.Value = value;
	}

	/// <summary>
	/// Momentum length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance from 100 required for longs.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance from 100 required for shorts.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA period of the MACD indicator.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the MACD indicator.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period of the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Whether to close opposite positions before opening a trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
	}

	/// <summary>
	/// Maximum number of net positions per side (0 = unlimited).
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Order volume for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable stop-loss management.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Enable take-profit management.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable trailing stop adjustments.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable break-even behaviour.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Number of pips required before moving the stop to break-even.
	/// </summary>
	public int BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional pips added beyond the entry price when activating break-even.
	/// </summary>
	public int BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// When enabled the strategy closes all positions on the next candle.
	/// </summary>
	public bool ForceExit
	{
		get => _forceExit.Value;
		set => _forceExit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stochasticHistory.Clear();
		_momentumHistory.Clear();
		_haOpenPrev = 0m;
		_haClosePrev = 0m;
		_pipSize = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_longBreakEvenActivated = false;
		_shortBreakEvenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new WeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_stochastic = new StochasticOscillator
		{
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod },
			Smooth = StochasticSlowing
		};

		_momentum = new Momentum { Length = MomentumPeriod };

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = MacdFastPeriod },
			LongMa = { Length = MacdSlowPeriod },
			SignalMa = { Length = MacdSignalPeriod }
		};

		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastMa, _slowMa, _stochastic, _momentum, _macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue stochasticValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !stochasticValue.IsFinal || !momentumValue.IsFinal || !macdValue.IsFinal)
			return;

		var fastMa = fastValue.ToDecimal();
		var slowMa = slowValue.ToDecimal();

		var stochastic = (StochasticOscillatorValue)stochasticValue;
		if (stochastic.K is not decimal stochK)
			return;

		var momentumDiff = Math.Abs(momentumValue.ToDecimal() - 100m);

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal macdSignal)
			return;

		UpdateIndicatorBuffers(stochK, momentumDiff);

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpenPrev == 0m && _haClosePrev == 0m ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_haOpenPrev + _haClosePrev) / 2m;
		var isHaBullish = haClose >= haOpen;
		var isHaBearish = haClose <= haOpen;

		var momentumBuyReady = _momentumHistory.Any(v => v >= MomentumBuyThreshold);
		var momentumSellReady = _momentumHistory.Any(v => v >= MomentumSellThreshold);
		var stochasticOverbought = _stochasticHistory.Any(v => v >= StochasticOverbought);
		var stochasticOversold = _stochasticHistory.Any(v => v <= StochasticOversold);

		if (ForceExit)
		{
			CloseAllPositions();
		}
		else
		{
			HandleEntries(candle, fastMa, slowMa, macdLine, macdSignal, isHaBullish, isHaBearish, stochasticOverbought, stochasticOversold, momentumBuyReady, momentumSellReady);
		}

		HandleRiskManagement(candle);

		_haOpenPrev = haOpen;
		_haClosePrev = haClose;
	}

	private void UpdateIndicatorBuffers(decimal stochK, decimal momentumDiff)
	{
		if (_stochasticHistory.Count == 3)
			_stochasticHistory.Dequeue();
		_stochasticHistory.Enqueue(stochK);

		if (_momentumHistory.Count == 3)
			_momentumHistory.Dequeue();
		_momentumHistory.Enqueue(momentumDiff);
	}

	private void HandleEntries(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal macdLine, decimal macdSignal, bool isHaBullish, bool isHaBearish, bool stochasticOverbought, bool stochasticOversold, bool momentumBuyReady, bool momentumSellReady)
	{
		var maxPosition = MaxPositions <= 0 ? decimal.MaxValue : MaxPositions * TradeVolume;

		var canOpenLong = Position < maxPosition;
		var canOpenShort = -Position < maxPosition;

		var shouldBuy = isHaBullish && stochasticOverbought && fastMa > slowMa && momentumBuyReady && macdLine > macdSignal;
		var shouldSell = isHaBearish && stochasticOversold && fastMa < slowMa && momentumSellReady && macdLine < macdSignal;

		if (shouldBuy && canOpenLong)
		{
			if (CloseOppositePositions && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}

			BuyMarket(TradeVolume);
			_longEntryPrice = candle.ClosePrice;
			_longBreakEvenActivated = false;
			_longStopPrice = UseStopLoss && StopLossPips > 0 ? candle.ClosePrice - StopLossPips * _pipSize : null;
			_longTakePrice = UseTakeProfit && TakeProfitPips > 0 ? candle.ClosePrice + TakeProfitPips * _pipSize : null;
		}
		else if (shouldSell && canOpenShort)
		{
			if (CloseOppositePositions && Position > 0)
			{
				SellMarket(Position);
			}

			SellMarket(TradeVolume);
			_shortEntryPrice = candle.ClosePrice;
			_shortBreakEvenActivated = false;
			_shortStopPrice = UseStopLoss && StopLossPips > 0 ? candle.ClosePrice + StopLossPips * _pipSize : null;
			_shortTakePrice = UseTakeProfit && TakeProfitPips > 0 ? candle.ClosePrice - TakeProfitPips * _pipSize : null;
		}
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			UpdateLongStops(candle);
		}
		else if (Position < 0)
		{
			UpdateShortStops(candle);
		}
	}

	private void UpdateLongStops(ICandleMessage candle)
	{
		if (_longEntryPrice.HasValue && UseBreakEven && BreakEvenTriggerPips > 0)
		{
			var triggerPrice = _longEntryPrice.Value + BreakEvenTriggerPips * _pipSize;
			if (candle.ClosePrice >= triggerPrice)
			{
				var newStop = _longEntryPrice.Value + BreakEvenOffsetPips * _pipSize;
				if (!_longBreakEvenActivated || (_longStopPrice.HasValue && newStop > _longStopPrice.Value))
				{
					_longStopPrice = newStop;
					_longBreakEvenActivated = true;
				}
			}
		}

		if (UseTrailingStop && TrailingStopPips > 0 && _longStopPrice.HasValue)
		{
			var trailCandidate = candle.ClosePrice - TrailingStopPips * _pipSize;
			if (trailCandidate > _longStopPrice.Value)
				_longStopPrice = trailCandidate;
		}

		if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
		{
			SellMarket(Position);
			ResetLongState();
			return;
		}

		if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
		{
			SellMarket(Position);
			ResetLongState();
		}
	}

	private void UpdateShortStops(ICandleMessage candle)
	{
		if (_shortEntryPrice.HasValue && UseBreakEven && BreakEvenTriggerPips > 0)
		{
			var triggerPrice = _shortEntryPrice.Value - BreakEvenTriggerPips * _pipSize;
			if (candle.ClosePrice <= triggerPrice)
			{
				var newStop = _shortEntryPrice.Value - BreakEvenOffsetPips * _pipSize;
				if (!_shortBreakEvenActivated || (_shortStopPrice.HasValue && newStop < _shortStopPrice.Value))
				{
					_shortStopPrice = newStop;
					_shortBreakEvenActivated = true;
				}
			}
		}

		if (UseTrailingStop && TrailingStopPips > 0 && _shortStopPrice.HasValue)
		{
			var trailCandidate = candle.ClosePrice + TrailingStopPips * _pipSize;
			if (trailCandidate < _shortStopPrice.Value)
				_shortStopPrice = trailCandidate;
		}

		if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return;
		}

		if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			ResetLongState();
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_longBreakEvenActivated = false;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortBreakEvenActivated = false;
	}
}
