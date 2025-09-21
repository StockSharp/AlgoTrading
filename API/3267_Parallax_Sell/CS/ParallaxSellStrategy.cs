using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short-only martingale strategy converted from the MetaTrader expert "parallax_sell".
/// Combines Williams %R, MACD and stochastic filters to scale into shorts and exit on deep pullbacks.
/// </summary>
public class ParallaxSellStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _entryWilliamsLength;
	private readonly StrategyParam<int> _exitWilliamsLength;
	private readonly StrategyParam<int> _entryStochasticLength;
	private readonly StrategyParam<int> _entryStochasticSignal;
	private readonly StrategyParam<int> _entryStochasticSlowing;
	private readonly StrategyParam<int> _exitStochasticLength;
	private readonly StrategyParam<int> _exitStochasticSignal;
	private readonly StrategyParam<int> _exitStochasticSlowing;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _entryWilliamsThreshold;
	private readonly StrategyParam<decimal> _exitWilliamsThreshold;
	private readonly StrategyParam<decimal> _entryStochasticTrigger;
	private readonly StrategyParam<decimal> _exitStochasticTrigger;
	private readonly StrategyParam<decimal> _macdThreshold;
	private readonly StrategyParam<decimal> _singleTradeTargetPips;
	private readonly StrategyParam<decimal> _multiTradeTargetPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<bool> _useMartingale;

	private WilliamsR _entryWilliams = null!;
	private WilliamsR _exitWilliams = null!;
	private StochasticOscillator _entryStochastic = null!;
	private StochasticOscillator _exitStochastic = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _previousEntryK;
	private decimal _pipSize;
	private decimal _currentVolume;
	private decimal _lastRealizedPnL;
	private decimal _sumEntryPrices;
	private int _entryCount;

	/// <summary>
	/// Trading timeframe used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Williams %R lookback used for entry confirmation.
	/// </summary>
	public int EntryWilliamsLength
	{
		get => _entryWilliamsLength.Value;
		set => _entryWilliamsLength.Value = value;
	}

	/// <summary>
	/// Williams %R lookback used for exit confirmation.
	/// </summary>
	public int ExitWilliamsLength
	{
		get => _exitWilliamsLength.Value;
		set => _exitWilliamsLength.Value = value;
	}

	/// <summary>
	/// Stochastic %K length for entry signals.
	/// </summary>
	public int EntryStochasticLength
	{
		get => _entryStochasticLength.Value;
		set => _entryStochasticLength.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing for entry signals.
	/// </summary>
	public int EntryStochasticSignal
	{
		get => _entryStochasticSignal.Value;
		set => _entryStochasticSignal.Value = value;
	}

	/// <summary>
	/// Stochastic slowing factor for entry signals.
	/// </summary>
	public int EntryStochasticSlowing
	{
		get => _entryStochasticSlowing.Value;
		set => _entryStochasticSlowing.Value = value;
	}

	/// <summary>
	/// Stochastic %K length for exit checks.
	/// </summary>
	public int ExitStochasticLength
	{
		get => _exitStochasticLength.Value;
		set => _exitStochasticLength.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing for exit checks.
	/// </summary>
	public int ExitStochasticSignal
	{
		get => _exitStochasticSignal.Value;
		set => _exitStochasticSignal.Value = value;
	}

	/// <summary>
	/// Stochastic slowing factor for exit checks.
	/// </summary>
	public int ExitStochasticSlowing
	{
		get => _exitStochasticSlowing.Value;
		set => _exitStochasticSlowing.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Williams %R threshold required before new shorts are allowed.
	/// </summary>
	public decimal EntryWilliamsThreshold
	{
		get => _entryWilliamsThreshold.Value;
		set => _entryWilliamsThreshold.Value = value;
	}

	/// <summary>
	/// Williams %R threshold used to close the last remaining trade.
	/// </summary>
	public decimal ExitWilliamsThreshold
	{
		get => _exitWilliamsThreshold.Value;
		set => _exitWilliamsThreshold.Value = value;
	}

	/// <summary>
	/// Overbought level that the stochastic must cross downward to trigger entries.
	/// </summary>
	public decimal EntryStochasticTrigger
	{
		get => _entryStochasticTrigger.Value;
		set => _entryStochasticTrigger.Value = value;
	}

	/// <summary>
	/// Oversold level that confirms exits when multiple shorts are open.
	/// </summary>
	public decimal ExitStochasticTrigger
	{
		get => _exitStochasticTrigger.Value;
		set => _exitStochasticTrigger.Value = value;
	}

	/// <summary>
	/// Minimal MACD main line value required for new shorts.
	/// </summary>
	public decimal MacdThreshold
	{
		get => _macdThreshold.Value;
		set => _macdThreshold.Value = value;
	}

	/// <summary>
	/// Floating profit in pips needed to close a single short.
	/// </summary>
	public decimal SingleTradeTargetPips
	{
		get => _singleTradeTargetPips.Value;
		set => _singleTradeTargetPips.Value = value;
	}

	/// <summary>
	/// Floating profit in pips needed to close a stacked short basket.
	/// </summary>
	public decimal MultiTradeTargetPips
	{
		get => _multiTradeTargetPips.Value;
		set => _multiTradeTargetPips.Value = value;
	}

	/// <summary>
	/// MetaTrader-like take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Base trading volume before martingale escalation.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next entry after a losing cycle when martingale is enabled.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Enables martingale-style volume escalation.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ParallaxSellStrategy"/>.
	/// </summary>
	public ParallaxSellStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");

		_entryWilliamsLength = Param(nameof(EntryWilliamsLength), 350)
		.SetGreaterThanZero()
		.SetDisplay("Entry Williams %R", "Lookback for entry Williams %R", "Indicators")
		.SetCanOptimize(true);

		_exitWilliamsLength = Param(nameof(ExitWilliamsLength), 350)
		.SetGreaterThanZero()
		.SetDisplay("Exit Williams %R", "Lookback for exit Williams %R", "Indicators")
		.SetCanOptimize(true);

		_entryStochasticLength = Param(nameof(EntryStochasticLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Entry Stochastic %K", "Lookback for entry stochastic", "Indicators")
		.SetCanOptimize(true);

		_entryStochasticSignal = Param(nameof(EntryStochasticSignal), 1)
		.SetGreaterThanZero()
		.SetDisplay("Entry Stochastic %D", "Smoothing for entry stochastic", "Indicators");

		_entryStochasticSlowing = Param(nameof(EntryStochasticSlowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Entry Stochastic Slowing", "Slowing factor for entry stochastic", "Indicators");

		_exitStochasticLength = Param(nameof(ExitStochasticLength), 90)
		.SetGreaterThanZero()
		.SetDisplay("Exit Stochastic %K", "Lookback for exit stochastic", "Indicators")
		.SetCanOptimize(true);

		_exitStochasticSignal = Param(nameof(ExitStochasticSignal), 7)
		.SetGreaterThanZero()
		.SetDisplay("Exit Stochastic %D", "Smoothing for exit stochastic", "Indicators");

		_exitStochasticSlowing = Param(nameof(ExitStochasticSlowing), 1)
		.SetGreaterThanZero()
		.SetDisplay("Exit Stochastic Slowing", "Slowing factor for exit stochastic", "Indicators");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 120)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length", "Indicators")
		.SetCanOptimize(true);

		_entryWilliamsThreshold = Param(nameof(EntryWilliamsThreshold), -10m)
		.SetDisplay("Entry WPR Threshold", "Minimum Williams %R value before shorting", "Signals");

		_exitWilliamsThreshold = Param(nameof(ExitWilliamsThreshold), -80m)
		.SetDisplay("Exit WPR Threshold", "Williams %R level confirming exits", "Signals");

		_entryStochasticTrigger = Param(nameof(EntryStochasticTrigger), 90m)
		.SetRange(0m, 100m)
		.SetDisplay("Entry Stochastic Level", "Overbought level for entry cross", "Signals");

		_exitStochasticTrigger = Param(nameof(ExitStochasticTrigger), 12m)
		.SetRange(0m, 100m)
		.SetDisplay("Exit Stochastic Level", "Oversold level for exit", "Signals");

		_macdThreshold = Param(nameof(MacdThreshold), 0.178m)
		.SetDisplay("MACD Threshold", "Required MACD main line value", "Signals");

		_singleTradeTargetPips = Param(nameof(SingleTradeTargetPips), 10m)
		.SetNotNegative()
		.SetDisplay("Single Trade Target", "Pips required to exit a lone short", "Risk Management");

		_multiTradeTargetPips = Param(nameof(MultiTradeTargetPips), 15m)
		.SetNotNegative()
		.SetDisplay("Basket Target", "Pips required to exit a stacked basket", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Distance for the protective take profit", "Risk Management");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Base lot size", "Trading");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale Multiplier", "Multiplier applied after losses", "Trading");

		_useMartingale = Param(nameof(UseMartingale), true)
		.SetDisplay("Use Martingale", "Enable volume escalation", "Trading");
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

	_previousEntryK = null;
	_pipSize = 0m;
	_currentVolume = 0m;
	_lastRealizedPnL = 0m;
	_sumEntryPrices = 0m;
	_entryCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_entryWilliams = new WilliamsR { Length = EntryWilliamsLength };
	_exitWilliams = new WilliamsR { Length = ExitWilliamsLength };
	_entryStochastic = new StochasticOscillator
	{
	Length = EntryStochasticLength,
	K = { Length = EntryStochasticLength },
	D = { Length = EntryStochasticSignal },
	Slowing = EntryStochasticSlowing
	};
	_exitStochastic = new StochasticOscillator
	{
	Length = ExitStochasticLength,
	K = { Length = ExitStochasticLength },
	D = { Length = ExitStochasticSignal },
	Slowing = ExitStochasticSlowing
	};
	_macd = new MovingAverageConvergenceDivergenceSignal
	{
	Macd =
	{
	ShortMa = { Length = Math.Max(1, MacdFastLength) },
	LongMa = { Length = Math.Max(1, MacdSlowLength) }
	},
	SignalMa = { Length = Math.Max(1, MacdSignalLength) }
	};

	_pipSize = CalculatePipSize();
	_currentVolume = AlignVolume(InitialVolume);
	Volume = _currentVolume;
	_lastRealizedPnL = PnL;

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_entryWilliams, _macd, _entryStochastic, _exitWilliams, _exitStochastic, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _entryWilliams);
	DrawIndicator(area, _exitWilliams);
	DrawOwnTrades(area);

	var macdArea = CreateChartArea();
	if (macdArea != null)
	DrawIndicator(macdArea, _macd);

	var stochArea = CreateChartArea();
	if (stochArea != null)
	{
	DrawIndicator(stochArea, _entryStochastic);
	DrawIndicator(stochArea, _exitStochastic);
	}
	}

	var takeProfitUnit = TakeProfitPips > 0m && _pipSize > 0m
	? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point)
	: null;

	StartProtection(takeProfit: takeProfitUnit);
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue entryWilliamsValue,
	IIndicatorValue macdValue,
	IIndicatorValue entryStochasticValue,
	IIndicatorValue exitWilliamsValue,
	IIndicatorValue exitStochasticValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	UpdateMartingaleVolume();

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (!entryWilliamsValue.IsFinal || !macdValue.IsFinal || !entryStochasticValue.IsFinal ||
	!exitWilliamsValue.IsFinal || !exitStochasticValue.IsFinal)
	{
	return;
	}

	var entryWilliams = entryWilliamsValue.ToDecimal();
	var exitWilliams = exitWilliamsValue.ToDecimal();

	var macdSignal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macdSignal.Macd is not decimal macdMain)
	return;

	var entryStoch = (StochasticOscillatorValue)entryStochasticValue;
	var exitStoch = (StochasticOscillatorValue)exitStochasticValue;

	var entryK = entryStoch.K;
	var exitK = exitStoch.K;

	var crossDown = _previousEntryK is decimal prev && prev >= EntryStochasticTrigger && entryK < EntryStochasticTrigger;
	_previousEntryK = entryK;

	if (crossDown && entryWilliams > EntryWilliamsThreshold && macdMain > MacdThreshold)
	{
	EnterShort(candle.ClosePrice);
	}

	if (_entryCount == 0)
	return;

	var averageProfit = CalculateAverageProfitPips(candle.ClosePrice);

	var shouldExit = false;

	if (_entryCount == 1)
	{
	if (averageProfit >= SingleTradeTargetPips && exitWilliams < ExitWilliamsThreshold)
	shouldExit = true;
	}
	else
	{
	if (averageProfit >= MultiTradeTargetPips && exitK < ExitStochasticTrigger)
	shouldExit = true;
	}

	if (TakeProfitPips > 0m && averageProfit >= TakeProfitPips)
	shouldExit = true;

	if (shouldExit)
	{
	ExitShorts();
	}
	}

	private void EnterShort(decimal price)
	{
	var volume = AlignVolume(_currentVolume);
	if (volume <= 0m)
	return;

	SellMarket(volume);
	_sumEntryPrices += price;
	_entryCount++;
	}

	private void ExitShorts()
	{
	var position = Math.Abs(Position);
	if (position <= 0m)
	{
	ResetEntries();
	return;
	}

	BuyMarket(position);
	ResetEntries();
	}

	private void ResetEntries()
	{
	_sumEntryPrices = 0m;
	_entryCount = 0;
	}

	private void UpdateMartingaleVolume()
	{
	var realized = PnL;
	if (realized == _lastRealizedPnL)
	return;

	var delta = realized - _lastRealizedPnL;

	if (UseMartingale)
	{
	if (delta > 0m)
	{
	_currentVolume = AlignVolume(InitialVolume);
	}
	else if (delta < 0m)
	{
	_currentVolume = AlignVolume(_currentVolume * MartingaleMultiplier);
	}
	}
	else
	{
	_currentVolume = AlignVolume(InitialVolume);
	}

	_lastRealizedPnL = realized;
	Volume = _currentVolume;
	}

	private decimal CalculateAverageProfitPips(decimal currentPrice)
	{
	if (_entryCount == 0 || _pipSize <= 0m)
	return 0m;

	var averageEntry = _sumEntryPrices / _entryCount;
	var profit = (averageEntry - currentPrice) / _pipSize;
	return profit;
	}

	private decimal AlignVolume(decimal volume)
	{
	if (volume <= 0m)
	return 0m;

	var step = Security?.VolumeStep ?? 0m;
	if (step <= 0m)
	return volume;

	var rounded = Math.Round(volume / step) * step;
	if (rounded <= 0m)
	rounded = step;

	return rounded;
	}

	private decimal CalculatePipSize()
	{
	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	return 0m;

	var step = priceStep;
	var decimals = 0;

	while (step < 1m && decimals < 10)
	{
	step *= 10m;
	decimals++;
	}

	return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}
}
