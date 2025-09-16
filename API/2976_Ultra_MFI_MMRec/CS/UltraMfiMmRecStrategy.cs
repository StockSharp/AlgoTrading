using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ultra Money Flow Index strategy with multi-step smoothing and adaptive money management.
/// The logic mirrors the MT5 Exp_UltraMFI_MMRec expert advisor and uses crossovers of smoothed
/// bullish and bearish pressure counters to generate trades, while reducing size after losing streaks.
/// </summary>
public class UltraMfiMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<SmoothingMethod> _stepSmoothingMethod;
	private readonly StrategyParam<int> _startLength;
	private readonly StrategyParam<int> _stepSize;
	private readonly StrategyParam<int> _stepsTotal;
	private readonly StrategyParam<SmoothingMethod> _finalSmoothingMethod;
	private readonly StrategyParam<int> _finalSmoothingLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<int> _buyTotalTrigger;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellTotalTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private MoneyFlowIndex _mfi = null!;
	private readonly List<IndicatorBase<decimal>> _stepSmoothers = new();
	private readonly List<decimal?> _previousStepValues = new();
	private IndicatorBase<decimal> _bullsSmoother = null!;
	private IndicatorBase<decimal> _bearsSmoother = null!;
	private readonly List<(decimal bulls, decimal bears)> _lineHistory = new();
	private readonly List<bool> _recentBuyResults = new();
	private readonly List<bool> _recentSellResults = new();
	private decimal _lastRealizedPnL;
	private int _currentPositionDirection;

	/// <summary>
	/// Supported smoothing methods for the Ultra MFI ladder.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		LinearWeighted,
		/// <summary>Jurik moving average.</summary>
		Jurik
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UltraMfiMmRecStrategy"/>.
	/// </summary>
	public UltraMfiMmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for signal calculation", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MFI Period", "Length of the base Money Flow Index", "Indicator");

		_stepSmoothingMethod = Param(nameof(StepSmoothing), SmoothingMethod.Jurik)
		.SetDisplay("Step Smoothing", "Method used for ladder smoothing", "Indicator");

		_startLength = Param(nameof(StartLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Start Length", "Length of the first smoothing step", "Indicator");

		_stepSize = Param(nameof(StepSize), 5)
		.SetGreaterThanZero()
		.SetDisplay("Step Size", "Increment between smoothing lengths", "Indicator");

		_stepsTotal = Param(nameof(StepsTotal), 10)
		.SetGreaterThanZero()
		.SetDisplay("Steps Total", "Number of smoothing steps", "Indicator");

		_finalSmoothingMethod = Param(nameof(FinalSmoothing), SmoothingMethod.Jurik)
		.SetDisplay("Final Smoothing", "Method used for bullish/bearish counters", "Indicator");

		_finalSmoothingLength = Param(nameof(FinalSmoothingLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Final Length", "Length for the counter smoothers", "Indicator");

		_signalShift = Param(nameof(SignalShift), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Shift", "Bars back to evaluate crossover", "Signal");

		_normalVolume = Param(nameof(NormalVolume), 0.1m)
		.SetDisplay("Normal Volume", "Default trade volume", "Money Management");

		_reducedVolume = Param(nameof(ReducedVolume), 0.01m)
		.SetDisplay("Reduced Volume", "Volume after a losing streak", "Money Management");

		_buyTotalTrigger = Param(nameof(BuyTotalTrigger), 5)
		.SetGreaterThanZero()
		.SetDisplay("Buy Trigger Count", "Number of recent buy trades to inspect", "Money Management");

		_buyLossTrigger = Param(nameof(BuyLossTrigger), 3)
		.SetGreaterThanZero()
		.SetDisplay("Buy Loss Trigger", "Losing buys to switch to reduced volume", "Money Management");

		_sellTotalTrigger = Param(nameof(SellTotalTrigger), 5)
		.SetGreaterThanZero()
		.SetDisplay("Sell Trigger Count", "Number of recent sell trades to inspect", "Money Management");

		_sellLossTrigger = Param(nameof(SellLossTrigger), 3)
		.SetGreaterThanZero()
		.SetDisplay("Sell Loss Trigger", "Losing sells to switch to reduced volume", "Money Management");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
		.SetDisplay("Allow Long Entries", "Enable buy entries", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
		.SetDisplay("Allow Short Entries", "Enable sell entries", "Trading");

		_allowLongExits = Param(nameof(AllowLongExits), true)
		.SetDisplay("Allow Long Exits", "Allow closing long positions", "Trading");

		_allowShortExits = Param(nameof(AllowShortExits), true)
		.SetDisplay("Allow Short Exits", "Allow closing short positions", "Trading");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
		.SetRange(0m, 100m)
		.SetDisplay("Take Profit %", "Optional profit target in percent", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
		.SetRange(0m, 100m)
		.SetDisplay("Stop Loss %", "Optional stop loss in percent", "Risk");
	}

	/// <summary>Candle type used by the strategy.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Length of the base Money Flow Index indicator.</summary>
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }

	/// <summary>Smoothing method for intermediate ladder averages.</summary>
	public SmoothingMethod StepSmoothing { get => _stepSmoothingMethod.Value; set => _stepSmoothingMethod.Value = value; }

	/// <summary>Length of the first smoothing step.</summary>
	public int StartLength { get => _startLength.Value; set => _startLength.Value = value; }

	/// <summary>Increment between consecutive smoothing lengths.</summary>
	public int StepSize { get => _stepSize.Value; set => _stepSize.Value = value; }

	/// <summary>Total number of smoothing steps.</summary>
	public int StepsTotal { get => _stepsTotal.Value; set => _stepsTotal.Value = value; }

	/// <summary>Smoothing method used for bullish and bearish counters.</summary>
	public SmoothingMethod FinalSmoothing { get => _finalSmoothingMethod.Value; set => _finalSmoothingMethod.Value = value; }

	/// <summary>Length for the bullish and bearish counter smoothers.</summary>
	public int FinalSmoothingLength { get => _finalSmoothingLength.Value; set => _finalSmoothingLength.Value = value; }

	/// <summary>Number of finished bars to look back when generating signals.</summary>
	public int SignalShift { get => _signalShift.Value; set => _signalShift.Value = value; }

	/// <summary>Default trade volume.</summary>
	public decimal NormalVolume { get => _normalVolume.Value; set => _normalVolume.Value = value; }

	/// <summary>Reduced volume applied after losing streaks.</summary>
	public decimal ReducedVolume { get => _reducedVolume.Value; set => _reducedVolume.Value = value; }

	/// <summary>Number of recent buy trades that participate in loss counting.</summary>
	public int BuyTotalTrigger { get => _buyTotalTrigger.Value; set => _buyTotalTrigger.Value = value; }

	/// <summary>Number of losing buy trades required to switch to reduced volume.</summary>
	public int BuyLossTrigger { get => _buyLossTrigger.Value; set => _buyLossTrigger.Value = value; }

	/// <summary>Number of recent sell trades that participate in loss counting.</summary>
	public int SellTotalTrigger { get => _sellTotalTrigger.Value; set => _sellTotalTrigger.Value = value; }

	/// <summary>Number of losing sell trades required to switch to reduced volume.</summary>
	public int SellLossTrigger { get => _sellLossTrigger.Value; set => _sellLossTrigger.Value = value; }

	/// <summary>Enable or disable new long entries.</summary>
	public bool AllowLongEntries { get => _allowLongEntries.Value; set => _allowLongEntries.Value = value; }

	/// <summary>Enable or disable new short entries.</summary>
	public bool AllowShortEntries { get => _allowShortEntries.Value; set => _allowShortEntries.Value = value; }

	/// <summary>Allow the strategy to close existing long positions.</summary>
	public bool AllowLongExits { get => _allowLongExits.Value; set => _allowLongExits.Value = value; }

	/// <summary>Allow the strategy to close existing short positions.</summary>
	public bool AllowShortExits { get => _allowShortExits.Value; set => _allowShortExits.Value = value; }

	/// <summary>Optional take-profit expressed in percent of the entry price.</summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>Optional stop-loss expressed in percent of the entry price.</summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stepSmoothers.Clear();
		_previousStepValues.Clear();
		_lineHistory.Clear();
		_recentBuyResults.Clear();
		_recentSellResults.Clear();
		_lastRealizedPnL = 0m;
		_currentPositionDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mfi = new MoneyFlowIndex { Length = MfiPeriod };

		_stepSmoothers.Clear();
		_previousStepValues.Clear();
		for (var i = 0; i <= StepsTotal; i++)
		{
			var length = Math.Max(1, StartLength + i * StepSize);
			var smoother = CreateSmoother(StepSmoothing, length);
			_stepSmoothers.Add(smoother);
			_previousStepValues.Add(null);
		}

		var finalLength = Math.Max(1, FinalSmoothingLength);
		_bullsSmoother = CreateSmoother(FinalSmoothing, finalLength);
		_bearsSmoother = CreateSmoother(FinalSmoothing, finalLength);

		_lineHistory.Clear();

		var takeProfitUnit = new Unit(TakeProfitPercent, UnitTypes.Percent);
		var stopLossUnit = new Unit(StopLossPercent, UnitTypes.Percent);
		StartProtection(takeProfitUnit, stopLossUnit);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_mfi, ProcessCandle)
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);

			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _bullsSmoother);
				DrawIndicator(indicatorArea, _bearsSmoother);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_mfi.IsFormed)
		return;

		var upCount = 0m;
		var downCount = 0m;

		for (var i = 0; i < _stepSmoothers.Count; i++)
		{
			var indicatorValue = _stepSmoothers[i].Process(mfiValue);

			if (!indicatorValue.IsFinal)
			return;

			var current = indicatorValue.GetValue<decimal>();

			if (!_previousStepValues[i].HasValue)
			{
				_previousStepValues[i] = current;
				return;
			}

			if (current > _previousStepValues[i]!.Value)
			{
				upCount += 1m;
			}
			else
			{
				downCount += 1m;
			}

			_previousStepValues[i] = current;
		}

		var bullsValue = _bullsSmoother.Process(upCount);
		var bearsValue = _bearsSmoother.Process(downCount);

		if (!bullsValue.IsFinal || !bearsValue.IsFinal)
		return;

		var bulls = bullsValue.GetValue<decimal>();
		var bears = bearsValue.GetValue<decimal>();

		_lineHistory.Add((bulls, bears));

		var maxHistory = Math.Max(4, SignalShift + 2);
		if (_lineHistory.Count > maxHistory)
		_lineHistory.RemoveRange(0, _lineHistory.Count - maxHistory);

		if (_lineHistory.Count <= SignalShift + 1)
		return;

		var currentIndex = _lineHistory.Count - 1 - SignalShift;
		if (currentIndex <= 0)
		return;

		var currentPair = _lineHistory[currentIndex];
		var previousPair = _lineHistory[currentIndex - 1];

		var closeShortSignal = previousPair.bulls > previousPair.bears;
		var closeLongSignal = previousPair.bulls < previousPair.bears;
		var openLongSignal = closeShortSignal && currentPair.bulls < currentPair.bears;
		var openShortSignal = closeLongSignal && currentPair.bulls > currentPair.bears;

		var canTrade = IsFormedAndOnlineAndAllowTrading();
		if (!canTrade)
		return;

		if (closeLongSignal && AllowLongExits && Position > 0)
		{
			SellMarket(Position);
		}

		if (closeShortSignal && AllowShortExits && Position < 0)
		{
			BuyMarket(-Position);
		}

		if (openLongSignal && AllowLongEntries && Position == 0)
		{
			var volume = GetBuyVolume();
			if (volume > 0m)
			BuyMarket(volume);
		}

		if (openShortSignal && AllowShortEntries && Position == 0)
		{
			var volume = GetSellVolume();
			if (volume > 0m)
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			_currentPositionDirection = 1;
		}
		else if (Position < 0)
		{
			_currentPositionDirection = -1;
		}
		else
		{
			if (_currentPositionDirection == 0)
			return;

			var realized = PnL;
			var tradePnL = realized - _lastRealizedPnL;
			var isLoss = tradePnL < 0m;

			if (_currentPositionDirection > 0)
			{
				RegisterBuyResult(isLoss);
			}
			else
			{
				RegisterSellResult(isLoss);
			}

			_lastRealizedPnL = realized;
			_currentPositionDirection = 0;
		}
	}

	private decimal GetBuyVolume()
	{
		var normal = NormalVolume;
		if (normal <= 0m)
		return 0m;

		if (BuyLossTrigger <= 0 || BuyTotalTrigger <= 0)
		return normal;

		var losses = 0;
		for (var i = 0; i < _recentBuyResults.Count; i++)
		{
			if (_recentBuyResults[i])
			losses++;
		}

		return losses >= BuyLossTrigger ? Math.Max(0m, ReducedVolume) : normal;
	}

	private decimal GetSellVolume()
	{
		var normal = NormalVolume;
		if (normal <= 0m)
		return 0m;

		if (SellLossTrigger <= 0 || SellTotalTrigger <= 0)
		return normal;

		var losses = 0;
		for (var i = 0; i < _recentSellResults.Count; i++)
		{
			if (_recentSellResults[i])
			losses++;
		}

		return losses >= SellLossTrigger ? Math.Max(0m, ReducedVolume) : normal;
	}

	private void RegisterBuyResult(bool isLoss)
	{
		if (BuyTotalTrigger <= 0)
		return;

		_recentBuyResults.Add(isLoss);
		while (_recentBuyResults.Count > BuyTotalTrigger)
		_recentBuyResults.RemoveAt(0);
	}

	private void RegisterSellResult(bool isLoss)
	{
		if (SellTotalTrigger <= 0)
		return;

		_recentSellResults.Add(isLoss);
		while (_recentSellResults.Count > SellTotalTrigger)
		_recentSellResults.RemoveAt(0);
	}

	private IndicatorBase<decimal> CreateSmoother(SmoothingMethod method, int length)
	{
		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new JurikMovingAverage { Length = length }
		};
	}
}
