
using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive port of the CyberiaTrader expert advisor that reconstructs its probability based decision tree.
/// Combines the original statistical core with optional indicator based filters.
/// </summary>
public class CyberiaTraderAdaptiveStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _autoSelectPeriod;
	private readonly StrategyParam<int> _initialPeriod;
	private readonly StrategyParam<int> _maxPeriod;
	private readonly StrategyParam<int> _historyMultiplier;
	private readonly StrategyParam<decimal> _spreadFilter;
	private readonly StrategyParam<bool> _enableCyberiaLogic;
	private readonly StrategyParam<bool> _enableMa;
	private readonly StrategyParam<bool> _enableMacd;
	private readonly StrategyParam<bool> _enableCci;
	private readonly StrategyParam<bool> _enableAdx;
	private readonly StrategyParam<bool> _enableFractals;
	private readonly StrategyParam<bool> _enableReversalDetector;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _fractalDepth;
	private readonly StrategyParam<decimal> _reversalIndex;
	private readonly StrategyParam<bool> _blockBuy;
	private readonly StrategyParam<bool> _blockSell;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private ExponentialMovingAverage _ema = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private CommodityChannelIndex _cci = null!;
	private AverageDirectionalIndex _adx = null!;

	private readonly List<CandleSnapshot> _history = new();

	private int _currentValuePeriod;
	private int _previousValuePeriod;
	private int _currentValuesPeriodCount;
	private decimal _lastSuitablePeriodQuality;

	private decimal? _previousEmaValue;
	private decimal? _lastEmaValue;
	private decimal? _lastMacdValue;
	private decimal? _lastMacdSignal;
	private decimal? _lastCciValue;
	private decimal? _lastPlusDi;
	private decimal? _lastMinusDi;
	private FractalDirection _fractalDirection = FractalDirection.None;

	private bool _disableBuy;
	private bool _disableSell;
	private bool _blockBuyFlag;
	private bool _blockSellFlag;

	private DecisionType _currentDecision = DecisionType.Unknown;
	private decimal _buyPossibility;
	private decimal _sellPossibility;
	private decimal _undefinedPossibility;
	private decimal _decisionValue;
	private decimal _previousDecisionValue;

	private decimal _buyPossibilityMid;
	private decimal _sellPossibilityMid;
	private decimal _undefinedPossibilityMid;
	private decimal _buySucPossibilityMid;
	private decimal _sellSucPossibilityMid;
	private decimal _undefinedSucPossibilityMid;

	private decimal _buyPossibilityQuality;
	private decimal _sellPossibilityQuality;
	private decimal _undefinedPossibilityQuality;
	private decimal _buySucPossibilityQuality;
	private decimal _sellSucPossibilityQuality;
	private decimal _undefinedSucPossibilityQuality;
	private decimal _possibilityQuality;
	private decimal _possibilitySuccessQuality;

	/// <summary>
	/// Creates a new instance of the adaptive CyberiaTrader strategy.
	/// </summary>
	public CyberiaTraderAdaptiveStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for calculations", "General");

		_autoSelectPeriod = Param(nameof(AutoSelectPeriod), true)
		.SetDisplay("Auto Period", "Automatically scan for the best probability window", "General");

		_initialPeriod = Param(nameof(InitialPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Initial Period", "Fallback period for probability sampling", "General");

		_maxPeriod = Param(nameof(MaxPeriod), 23)
		.SetGreaterThanZero()
		.SetDisplay("Max Period", "Upper bound for adaptive period search", "General");

		_historyMultiplier = Param(nameof(HistoryMultiplier), 5)
		.SetGreaterThanZero()
		.SetDisplay("History Multiplier", "Number of samples per period used for statistics", "General");

		_spreadFilter = Param(nameof(SpreadFilter), 0m)
		.SetDisplay("Spread Filter", "Minimum move treated as actionable", "General");

		_enableCyberiaLogic = Param(nameof(EnableCyberiaLogic), true)
		.SetDisplay("Enable Cyberia Logic", "Use original probability based decision rules", "Logic");

		_enableMa = Param(nameof(EnableMa), false)
		.SetDisplay("Enable EMA", "Use EMA slope filter", "Logic");

		_enableMacd = Param(nameof(EnableMacd), false)
		.SetDisplay("Enable MACD", "Use MACD trend filter", "Logic");

		_enableCci = Param(nameof(EnableCci), false)
		.SetDisplay("Enable CCI", "Use CCI overbought/oversold filter", "Logic");

		_enableAdx = Param(nameof(EnableAdx), false)
		.SetDisplay("Enable ADX", "Use ADX directional filter", "Logic");

		_enableFractals = Param(nameof(EnableFractals), false)
		.SetDisplay("Enable Fractals", "Block trades opposite to the latest fractal", "Logic");

		_enableReversalDetector = Param(nameof(EnableReversalDetector), false)
		.SetDisplay("Enable Reversal Detector", "Toggle direction when probabilities spike", "Logic");

		_maPeriod = Param(nameof(MaPeriod), 23)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of the EMA used by the filter", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Commodity Channel Index length", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Average Directional Index length", "Indicators");

		_fractalDepth = Param(nameof(FractalDepth), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fractal Depth", "Number of candles used to detect fractals", "Indicators");

		_reversalIndex = Param(nameof(ReversalIndex), 3m)
		.SetDisplay("Reversal Index", "Multiplier for spike based reversal detection", "Logic");

		_blockBuy = Param(nameof(BlockBuy), false)
		.SetDisplay("Block Buy", "Prevent buy orders regardless of signals", "Risk");

		_blockSell = Param(nameof(BlockSell), false)
		.SetDisplay("Block Sell", "Prevent sell orders regardless of signals", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetDisplay("Take Profit", "Absolute take profit distance", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetDisplay("Stop Loss", "Absolute stop loss distance", "Risk");

		Volume = 1;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable adaptive period selection.
	/// </summary>
	public bool AutoSelectPeriod
	{
		get => _autoSelectPeriod.Value;
		set => _autoSelectPeriod.Value = value;
	}

	/// <summary>
	/// Fallback probability period when auto selection is disabled.
	/// </summary>
	public int InitialPeriod
	{
		get => _initialPeriod.Value;
		set => _initialPeriod.Value = value;
	}

	/// <summary>
	/// Maximum period evaluated during adaptive search.
	/// </summary>
	public int MaxPeriod
	{
		get => _maxPeriod.Value;
		set => _maxPeriod.Value = value;
	}

	/// <summary>
	/// Number of historical samples analysed per period.
	/// </summary>
	public int HistoryMultiplier
	{
		get => _historyMultiplier.Value;
		set => _historyMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum move required to consider a probability successful.
	/// </summary>
	public decimal SpreadFilter
	{
		get => _spreadFilter.Value;
		set => _spreadFilter.Value = value;
	}

	/// <summary>
	/// Toggle the original Cyberia logic module.
	/// </summary>
	public bool EnableCyberiaLogic
	{
		get => _enableCyberiaLogic.Value;
		set => _enableCyberiaLogic.Value = value;
	}

	/// <summary>
	/// Toggle EMA filter.
	/// </summary>
	public bool EnableMa
	{
		get => _enableMa.Value;
		set => _enableMa.Value = value;
	}

	/// <summary>
	/// Toggle MACD filter.
	/// </summary>
	public bool EnableMacd
	{
		get => _enableMacd.Value;
		set => _enableMacd.Value = value;
	}

	/// <summary>
	/// Toggle CCI filter.
	/// </summary>
	public bool EnableCci
	{
		get => _enableCci.Value;
		set => _enableCci.Value = value;
	}

	/// <summary>
	/// Toggle ADX filter.
	/// </summary>
	public bool EnableAdx
	{
		get => _enableAdx.Value;
		set => _enableAdx.Value = value;
	}

	/// <summary>
	/// Toggle fractal filter.
	/// </summary>
	public bool EnableFractals
	{
		get => _enableFractals.Value;
		set => _enableFractals.Value = value;
	}

	/// <summary>
	/// Toggle probability spike based reversal detector.
	/// </summary>
	public bool EnableReversalDetector
	{
		get => _enableReversalDetector.Value;
		set => _enableReversalDetector.Value = value;
	}

	/// <summary>
	/// EMA period used in the moving average filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Fast MACD period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow MACD period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Depth used to confirm fractal swings.
	/// </summary>
	public int FractalDepth
	{
		get => _fractalDepth.Value;
		set => _fractalDepth.Value = value;
	}

	/// <summary>
	/// Multiplier for reversal detection.
	/// </summary>
	public decimal ReversalIndex
	{
		get => _reversalIndex.Value;
		set => _reversalIndex.Value = value;
	}

	/// <summary>
	/// Hard block for buy orders.
	/// </summary>
	public bool BlockBuy
	{
		get => _blockBuy.Value;
		set => _blockBuy.Value = value;
	}

	/// <summary>
	/// Hard block for sell orders.
	/// </summary>
	public bool BlockSell
	{
		get => _blockSell.Value;
		set => _blockSell.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
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

		_currentValuePeriod = Math.Max(1, InitialPeriod);
		_previousValuePeriod = _currentValuePeriod;
		_currentValuesPeriodCount = Math.Max(1, _currentValuePeriod * HistoryMultiplier);

		_ema = new ExponentialMovingAverage { Length = MaPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_ema, _macd, _cci, _adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);

			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _macd);
				DrawIndicator(indicatorArea, _cci);
				DrawIndicator(indicatorArea, _adx);
			}
		}

		Unit takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		Unit stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;

		if (takeProfit != null || stopLoss != null)
		{
			StartProtection(takeProfit, stopLoss);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue macdValue, IIndicatorValue cciValue, IIndicatorValue adxValue)
	{
		// Ignore updates for unfinished candles.
		if (candle.State != CandleStates.Finished)
		return;

		// Ensure every indicator reports a final value before using it.
		if (!emaValue.IsFinal || !macdValue.IsFinal || !cciValue.IsFinal || !adxValue.IsFinal)
		return;

		var ema = emaValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var cci = cciValue.ToDecimal();
		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		_previousEmaValue = _lastEmaValue;
		_lastEmaValue = ema;
		_lastMacdValue = macdTyped.Macd;
		_lastMacdSignal = macdTyped.Signal;
		_lastCciValue = cci;
		_lastPlusDi = adxTyped.Dx.Plus;
		_lastMinusDi = adxTyped.Dx.Minus;

		// Store the latest bar snapshot for probability calculations.
		AddCandle(candle);
		UpdateFractalState();

		// Skip trading until the probability model is ready.
		if (!UpdateAdaptivePeriod())
		return;

		CalculateDirection();
		ExecuteTradingLogic();
	}

	private void CalculateDirection()
	{
		// Reset direction flags before applying the filter chain.
		_disableBuy = false;
		_disableSell = false;
		_blockBuyFlag = BlockBuy;
		_blockSellFlag = BlockSell;

		if (EnableCyberiaLogic)
		ApplyCyberiaLogic();

		if (EnableMacd)
		ApplyMacdFilter();

		if (EnableMa)
		ApplyMaFilter();

		if (EnableCci)
		ApplyCciFilter();

		if (EnableAdx)
		ApplyAdxFilter();

		if (EnableFractals)
		ApplyFractalFilter();

		if (EnableReversalDetector)
		ApplyReversalDetector();
	}

	private void ExecuteTradingLogic()
	{
		// Combine internal logic and user defined blocks.
		var allowBuy = !_disableBuy && !_blockBuyFlag;
		var allowSell = !_disableSell && !_blockSellFlag;

		if (_currentDecision == DecisionType.Buy && allowBuy)
		{
			if (Position < 0)
			BuyMarket(Math.Abs(Position));

			if (Position <= 0)
			BuyMarket(Volume);
		}
		else if (_currentDecision == DecisionType.Sell && allowSell)
		{
			if (Position > 0)
			SellMarket(Position);

			if (Position >= 0)
			SellMarket(Volume);
		}
		else if (_currentDecision == DecisionType.Unknown)
		{
			if (_possibilityQuality < 0.5m)
			ClosePosition();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void ApplyCyberiaLogic()
	{
		var leftScore = _sellPossibilityMid * _sellPossibilityQuality;
		var rightScore = _buyPossibilityMid * _buyPossibilityQuality;
		var leftSuccess = _sellSucPossibilityMid * _sellSucPossibilityQuality;
		var rightSuccess = _buySucPossibilityMid * _buySucPossibilityQuality;

		if (_currentValuePeriod > _previousValuePeriod)
		{
			if (leftScore > rightScore)
			{
				_disableSell = false;
				_disableBuy = true;

				if (leftSuccess > rightSuccess)
				_disableSell = true;
			}
			else if (leftScore < rightScore)
			{
				_disableSell = true;
				_disableBuy = false;

				if (leftSuccess < rightSuccess)
				_disableBuy = true;
			}
		}
		else if (_currentValuePeriod < _previousValuePeriod)
		{
			_disableSell = true;
			_disableBuy = true;
		}

		if (leftScore == rightScore)
		{
			_disableSell = true;
			_disableBuy = true;
		}

		if (_sellPossibilityMid > 0m && _sellSucPossibilityMid > 0m &&
		_sellPossibility > _sellSucPossibilityMid * 2m)
		{
			_disableSell = true;
		}

		if (_buyPossibilityMid > 0m && _buySucPossibilityMid > 0m &&
		_buyPossibility > _buySucPossibilityMid * 2m)
		{
			_disableBuy = true;
		}
	}

	private void ApplyMacdFilter()
	{
		if (_lastMacdValue is not decimal macd || _lastMacdSignal is not decimal signal)
		return;

		if (macd > signal)
		{
			_disableSell = true;
		}
		else if (macd < signal)
		{
			_disableBuy = true;
		}
	}

	private void ApplyMaFilter()
	{
		if (_previousEmaValue is not decimal prev || _lastEmaValue is not decimal current)
		return;

		if (current > prev)
		{
			_disableSell = true;
		}
		else if (current < prev)
		{
			_disableBuy = true;
		}
	}

	private void ApplyCciFilter()
	{
		if (_lastCciValue is not decimal cci)
		return;

		if (cci < -100m)
		{
			_disableSell = true;
		}
		else if (cci > 100m)
		{
			_disableBuy = true;
		}
	}

	private void ApplyAdxFilter()
	{
		if (_lastPlusDi is not decimal plus || _lastMinusDi is not decimal minus)
		return;

		if (plus > minus)
		{
			_disableSell = true;
		}
		else if (minus > plus)
		{
			_disableBuy = true;
		}
	}

	private void ApplyFractalFilter()
	{
		if (_fractalDirection == FractalDirection.Up)
		{
			_blockBuyFlag = true;
			_blockSellFlag = false;
		}
		else if (_fractalDirection == FractalDirection.Down)
		{
			_blockSellFlag = true;
			_blockBuyFlag = false;
		}
	}

	private void ApplyReversalDetector()
	{
		var trigger = false;

		if (_buyPossibility != 0m && _buyPossibilityMid != 0m &&
		_buyPossibility > _buyPossibilityMid * ReversalIndex)
		{
			trigger = true;
		}

		if (_sellPossibility != 0m && _sellPossibilityMid != 0m &&
		_sellPossibility > _sellPossibilityMid * ReversalIndex)
		{
			trigger = true;
		}

		if (!trigger)
		return;

		_disableSell = !_disableSell;
		_disableBuy = !_disableBuy;
	}

	private void AddCandle(ICandleMessage candle)
	{
		var snapshot = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_history.Add(snapshot);

		var maxHistory = Math.Max(MaxPeriod, _currentValuePeriod) * (HistoryMultiplier + 2) + 2;
		while (_history.Count > maxHistory)
		{
			_history.RemoveAt(0);
		}
	}

	private void UpdateFractalState()
	{
		var depth = Math.Max(5, FractalDepth);
		if (depth % 2 == 0)
		depth += 1;

		if (_history.Count < depth)
		return;

		var start = _history.Count - depth;
		var middle = start + depth / 2;
		var center = _history[middle];

		var isUpper = true;
		var isLower = true;

		for (var i = start; i < start + depth; i++)
		{
			if (i == middle)
			continue;

			var sample = _history[i];
			if (sample.High >= center.High)
			isUpper = false;
			if (sample.Low <= center.Low)
			isLower = false;
		}

		if (isUpper)
		{
			_fractalDirection = FractalDirection.Up;
		}
		else if (isLower)
		{
			_fractalDirection = FractalDirection.Down;
		}
	}

	private bool UpdateAdaptivePeriod()
	{
		// Evaluate possible sampling windows and keep the most reliable one.
		var basePeriod = Math.Max(1, InitialPeriod);
		var maxPeriod = AutoSelectPeriod ? Math.Max(1, MaxPeriod) : basePeriod;

		PossibilityStats? bestStats = null;
		var bestQuality = decimal.MinValue;
		var selectedPeriod = basePeriod;

		for (var period = 1; period <= maxPeriod; period++)
		{
			if (!AutoSelectPeriod && period != basePeriod)
			continue;

			var stats = CalculateStatistics(period);
			if (!stats.IsValid)
			continue;

			if (!AutoSelectPeriod)
			{
				bestStats = stats;
				bestQuality = stats.PossibilitySuccessQuality;
				selectedPeriod = period;
				break;
			}

			if (stats.PossibilitySuccessQuality > bestQuality)
			{
				bestQuality = stats.PossibilitySuccessQuality;
				selectedPeriod = period;
				bestStats = stats;
			}
		}

		if (bestStats == null)
		return false;

		_previousValuePeriod = _currentValuePeriod;
		_currentValuePeriod = selectedPeriod;
		_currentValuesPeriodCount = Math.Max(1, _currentValuePeriod * HistoryMultiplier);
		_lastSuitablePeriodQuality = bestQuality;

		ApplyStatistics(bestStats.Value);
		return true;
	}

	private PossibilityStats CalculateStatistics(int period)
	{
		// Compute averages and hit rates for the specified sampling period.
		var modelingBars = Math.Max(1, period * HistoryMultiplier);
		var required = period * (modelingBars + 1);
		if (_history.Count < required)
		return PossibilityStats.Invalid;

		var spread = SpreadFilter;

		decimal buySum = 0m;
		decimal sellSum = 0m;
		decimal undefinedSum = 0m;
		decimal buySuccessSum = 0m;
		decimal sellSuccessSum = 0m;
		decimal undefinedSuccessSum = 0m;

		var buyCount = 0;
		var sellCount = 0;
		var undefinedCount = 0;
		var buySuccessCount = 0;
		var sellSuccessCount = 0;
		var undefinedSuccessCount = 0;

		var buyQuality = 0m;
		var sellQuality = 0m;
		var undefinedQuality = 0m;
		var buySuccessQuality = 0m;
		var sellSuccessQuality = 0m;
		var undefinedSuccessQuality = 0m;

		DecisionType currentDecision = DecisionType.Unknown;
		decimal currentBuy = 0m;
		decimal currentSell = 0m;
		decimal currentUndefined = 0m;
		decimal currentDecisionValue = 0m;
		decimal previousDecisionValue = 0m;

		var shifts = Math.Min(modelingBars, (_history.Count / period) - 1);

		for (var i = 0; i <= shifts; i++)
		{
			var result = CalculatePossibility(period, i);
			if (i == 0)
			{
				currentDecision = result.Decision;
				currentBuy = result.BuyPossibility;
				currentSell = result.SellPossibility;
				currentUndefined = result.UndefinedPossibility;
				currentDecisionValue = result.DecisionValue;
				previousDecisionValue = result.PreviousDecisionValue;
			}

			if (result.Decision == DecisionType.Buy)
			buyQuality += 1m;
			else if (result.Decision == DecisionType.Sell)
			sellQuality += 1m;
			else
			undefinedQuality += 1m;

			if (result.BuyPossibility > spread)
			{
				buySuccessQuality += 1m;
				buySuccessSum += result.BuyPossibility;
				buySuccessCount += 1;
			}

			if (result.SellPossibility > spread)
			{
				sellSuccessQuality += 1m;
				sellSuccessSum += result.SellPossibility;
				sellSuccessCount += 1;
			}

			if (result.UndefinedPossibility > spread)
			{
				undefinedSuccessQuality += 1m;
				undefinedSuccessSum += result.UndefinedPossibility;
				undefinedSuccessCount += 1;
			}

			buySum += result.BuyPossibility;
			sellSum += result.SellPossibility;
			undefinedSum += result.UndefinedPossibility;

			buyCount += 1;
			sellCount += 1;
			undefinedCount += 1;
		}

		var totalQuality = buyQuality + sellQuality + undefinedQuality;
		var totalSuccessQuality = buySuccessQuality + sellSuccessQuality + undefinedSuccessQuality;

		var stats = new PossibilityStats
		(
		currentDecision,
		currentBuy,
		currentSell,
		currentUndefined,
		currentDecisionValue,
		previousDecisionValue,
		buyCount > 0 ? buySum / buyCount : 0m,
		sellCount > 0 ? sellSum / sellCount : 0m,
		undefinedCount > 0 ? undefinedSum / undefinedCount : 0m,
		buySuccessCount > 0 ? buySuccessSum / buySuccessCount : 0m,
		sellSuccessCount > 0 ? sellSuccessSum / sellSuccessCount : 0m,
		undefinedSuccessCount > 0 ? undefinedSuccessSum / undefinedSuccessCount : 0m,
		buyQuality,
		sellQuality,
		undefinedQuality,
		buySuccessQuality,
		sellSuccessQuality,
		undefinedSuccessQuality,
		totalQuality > 0m ? (sellQuality + buyQuality) / totalQuality : 0m,
		totalSuccessQuality > 0m ? (sellSuccessQuality + buySuccessQuality) / totalSuccessQuality : 0m,
		true
		);

		return stats;
	}

	private void ApplyStatistics(PossibilityStats stats)
	{
		_currentDecision = stats.Decision;
		_buyPossibility = stats.BuyPossibility;
		_sellPossibility = stats.SellPossibility;
		_undefinedPossibility = stats.UndefinedPossibility;
		_decisionValue = stats.DecisionValue;
		_previousDecisionValue = stats.PreviousDecisionValue;
		_buyPossibilityMid = stats.BuyPossibilityMid;
		_sellPossibilityMid = stats.SellPossibilityMid;
		_undefinedPossibilityMid = stats.UndefinedPossibilityMid;
		_buySucPossibilityMid = stats.BuySucPossibilityMid;
		_sellSucPossibilityMid = stats.SellSucPossibilityMid;
		_undefinedSucPossibilityMid = stats.UndefinedSucPossibilityMid;
		_buyPossibilityQuality = stats.BuyPossibilityQuality;
		_sellPossibilityQuality = stats.SellPossibilityQuality;
		_undefinedPossibilityQuality = stats.UndefinedPossibilityQuality;
		_buySucPossibilityQuality = stats.BuySucPossibilityQuality;
		_sellSucPossibilityQuality = stats.SellSucPossibilityQuality;
		_undefinedSucPossibilityQuality = stats.UndefinedSucPossibilityQuality;
		_possibilityQuality = stats.PossibilityQuality;
		_possibilitySuccessQuality = stats.PossibilitySuccessQuality;
	}

	private PossibilityResult CalculatePossibility(int period, int shift)
	{
		var currentIndex = period * shift;
		var previousIndex = period * (shift + 1);

		var current = GetCandle(currentIndex);
		var previous = GetCandle(previousIndex);

		var decisionValue = current.Close - current.Open;
		var previousDecisionValue = previous.Close - previous.Open;

		decimal buyPossibility = 0m;
		decimal sellPossibility = 0m;
		decimal undefinedPossibility = 0m;
		var decision = DecisionType.Unknown;

		if (decisionValue > 0m)
		{
			if (previousDecisionValue < 0m)
			{
				decision = DecisionType.Sell;
				sellPossibility = decisionValue;
			}
			else
			{
				decision = DecisionType.Unknown;
				undefinedPossibility = decisionValue;
			}
		}
		else if (decisionValue < 0m)
		{
			if (previousDecisionValue > 0m)
			{
				decision = DecisionType.Buy;
				buyPossibility = -decisionValue;
			}
			else
			{
				decision = DecisionType.Unknown;
				undefinedPossibility = -decisionValue;
			}
		}

		return new PossibilityResult(decision, buyPossibility, sellPossibility, undefinedPossibility, decisionValue, previousDecisionValue);
	}

	private CandleSnapshot GetCandle(int shift)
	{
		var index = _history.Count - 1 - shift;
		if (index < 0)
		return default;

		return _history[index];
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close);

	private enum DecisionType
	{
		Sell,
		Buy,
		Unknown,
	}

	private enum FractalDirection
	{
		None,
		Up,
		Down,
	}

	private readonly record struct PossibilityResult(DecisionType Decision, decimal BuyPossibility, decimal SellPossibility, decimal UndefinedPossibility, decimal DecisionValue, decimal PreviousDecisionValue);

	private readonly record struct PossibilityStats(
	DecisionType Decision,
	decimal BuyPossibility,
	decimal SellPossibility,
	decimal UndefinedPossibility,
	decimal DecisionValue,
	decimal PreviousDecisionValue,
	decimal BuyPossibilityMid,
	decimal SellPossibilityMid,
	decimal UndefinedPossibilityMid,
	decimal BuySucPossibilityMid,
	decimal SellSucPossibilityMid,
	decimal UndefinedSucPossibilityMid,
	decimal BuyPossibilityQuality,
	decimal SellPossibilityQuality,
	decimal UndefinedPossibilityQuality,
	decimal BuySucPossibilityQuality,
	decimal SellSucPossibilityQuality,
	decimal UndefinedSucPossibilityQuality,
	decimal PossibilityQuality,
	decimal PossibilitySuccessQuality,
	bool HasValue)
	{
		public static PossibilityStats Invalid { get; } = new PossibilityStats(DecisionType.Unknown, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, false);

		public bool IsValid => HasValue;
	}
}
