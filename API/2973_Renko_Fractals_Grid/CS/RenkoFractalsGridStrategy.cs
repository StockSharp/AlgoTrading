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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko Fractals Grid strategy translated from the "RENKO FRACTALS GRID" MQL4 expert advisor.
/// Combines fractal breakouts with a Renko-style volatility filter, weighted moving averages,
/// rate of change momentum confirmation, and MACD based exits.
/// Includes money management features such as martingale-style position sizing, trailing stop,
/// break-even logic, equity protection, and optional floating profit trailing in currency units.
/// </summary>
public class RenkoFractalsGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<decimal> _boxSizePips;
	private readonly StrategyParam<int> _candlesToRetrace;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useMoneyTarget;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyStopLoss;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private RateOfChange _rateOfChange = null!;
	private AverageTrueRange _atr = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _h5;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private decimal _l5;
	private decimal _close1;
	private decimal _close2;
	private decimal _close3;
	private decimal _close4;
	private decimal _close5;
	private decimal _high1;
	private decimal _high2;
	private decimal _low1;
	private decimal _low2;
	private decimal? _upFractal;
	private decimal? _downFractal;
	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private decimal[] _recentHighs = Array.Empty<decimal>();
	private decimal[] _recentLows = Array.Empty<decimal>();
	private int _recentIndex;
	private int _recentCount;

	private int _activeTrades;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal _moneyTrailPeak;
	private decimal _realizedPnL;
	private decimal _equityPeak;
	private decimal _initialEquity;
	private decimal _openVolume;
	private decimal _averageEntryPrice;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast weighted moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow weighted moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Rate of change lookback used for momentum calculation.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation (abs(100 - momentum)) required for long entries.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation (abs(100 - momentum)) required for short entries.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Use ATR based Renko box size instead of a fixed pip distance.
	/// </summary>
	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	/// <summary>
	/// Renko box size in pips when ATR filter is disabled.
	/// </summary>
	public decimal BoxSizePips
	{
		get => _boxSizePips.Value;
		set => _boxSizePips.Value = value;
	}

	/// <summary>
	/// Number of candles inspected for the Renko filter.
	/// </summary>
	public int CandlesToRetrace
	{
		get => _candlesToRetrace.Value;
		set => _candlesToRetrace.Value = value;
	}

	/// <summary>
	/// Base order volume used before martingale scaling.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplicative factor applied to the volume for each additional grid trade.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous grid trades.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable break-even stop adjustment.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance in pips required before moving the stop to break even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset in pips added when shifting the stop to break even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enable floating profit trailing expressed in currency units.
	/// </summary>
	public bool UseMoneyTarget
	{
		get => _useMoneyTarget.Value;
		set => _useMoneyTarget.Value = value;
	}

	/// <summary>
	/// Profit threshold in currency units that activates money trailing.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Maximum allowed pullback in currency units once money trailing is active.
	/// </summary>
	public decimal MoneyStopLoss
	{
		get => _moneyStopLoss.Value;
		set => _moneyStopLoss.Value = value;
	}

	/// <summary>
	/// Enable equity based stop-out that closes all trades on excessive drawdown.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum percentage drawdown from the equity peak tolerated before closing all trades.
	/// </summary>
	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RenkoFractalsGridStrategy"/>.
	/// </summary>
	public RenkoFractalsGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle type", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast WMA", "Length of the fast weighted moving average", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 2);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow WMA", "Length of the slow weighted moving average", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(50, 120, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Rate of change lookback used for momentum", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 2);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Buy Threshold", "Minimum deviation from 100 for long entries", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.0m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Sell Threshold", "Minimum deviation from 100 for short entries", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.0m, 0.1m);

		_useAtrFilter = Param(nameof(UseAtrFilter), false)
		.SetDisplay("Use ATR Box", "Use ATR instead of fixed pip size for Renko filter", "Renko");

		_boxSizePips = Param(nameof(BoxSizePips), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Box Size (pips)", "Fixed Renko box size in pips when ATR is disabled", "Renko")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_candlesToRetrace = Param(nameof(CandlesToRetrace), 10)
		.SetGreaterThanZero()
		.SetDisplay("Retrace Candles", "Number of candles inspected by the Renko filter", "Renko")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Initial trade volume before martingale scaling", "Money Management");

		_lotExponent = Param(nameof(LotExponent), 1.44m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Exponent", "Multiplier applied for each additional grid order", "Money Management");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of grid entries per direction", "Money Management");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(30m, 80m, 5m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break Even", "Move the stop to break even after reaching a profit threshold", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetNotNegative()
		.SetDisplay("Break Even Trigger", "Profit in pips required before shifting the stop", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetNotNegative()
		.SetDisplay("Break Even Offset", "Additional pips added beyond entry when break even triggers", "Risk");

		_useMoneyTarget = Param(nameof(UseMoneyTarget), false)
		.SetDisplay("Use Money Trailing", "Enable floating profit trailing in currency units", "Money Management");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
		.SetNotNegative()
		.SetDisplay("Money Take Profit", "Floating profit required before activating money trailing", "Money Management");

		_moneyStopLoss = Param(nameof(MoneyStopLoss), 10m)
		.SetNotNegative()
		.SetDisplay("Money Stop Loss", "Allowed pullback in floating profit before closing all trades", "Money Management");

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use Equity Stop", "Enable global equity based stop-out", "Risk");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 1.0m)
		.SetNotNegative()
		.SetDisplay("Equity Risk %", "Maximum drawdown from equity peak before closing all trades", "Risk");
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

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_close1 = _close2 = _close3 = _close4 = _close5 = 0m;
		_high1 = _high2 = _low1 = _low2 = 0m;
		_upFractal = null;
		_downFractal = null;
		_momentum1 = _momentum2 = _momentum3 = null;
		_recentHighs = Array.Empty<decimal>();
		_recentLows = Array.Empty<decimal>();
		_recentIndex = 0;
		_recentCount = 0;
		_activeTrades = 0;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_moneyTrailPeak = 0m;
		_realizedPnL = 0m;
		_equityPeak = 0m;
		_initialEquity = 0m;
		_openVolume = 0m;
		_averageEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_rateOfChange = new RateOfChange { Length = MomentumLength };
		_atr = new AverageTrueRange { Length = 14 };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};

		_recentHighs = new decimal[Math.Max(1, CandlesToRetrace)];
		_recentLows = new decimal[Math.Max(1, CandlesToRetrace)];
		_recentIndex = 0;
		_recentCount = 0;

		_initialEquity = GetPortfolioValue();
		_equityPeak = _initialEquity;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _rateOfChange, _atr, _macd, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rocValue, IIndicatorValue atrValue, IIndicatorValue macdValue)
	{
		// Update fractal buffers and OHLC history using every tick to match MQL logic.
		UpdateFractalBuffers(candle);

		if (candle.State != CandleStates.Finished)
		return;

		UpdateRecentExtremes(candle);
		UpdateMomentumHistory(rocValue);
		UpdateEquityPeak(candle);
		ApplyMoneyTrailing(candle);

		if (!IndicatorsReady())
		return;

		var fastMa = fastValue.ToDecimal();
		var slowMa = slowValue.ToDecimal();
		var atr = atrValue.ToDecimal();

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
		return;

		if (macdTyped.Macd is not decimal macdMain || macdTyped.Signal is not decimal macdSignal)
		return;

		CheckRiskManagement(candle, macdMain, macdSignal);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var renkoDirection = EvaluateRenkoDirection(candle.ClosePrice, atr);

		TryEnterLong(candle, fastMa, slowMa, renkoDirection, macdMain, macdSignal);
		TryEnterShort(candle, fastMa, slowMa, renkoDirection, macdMain, macdSignal);

		ManageLongPosition(candle, macdMain, macdSignal);
		ManageShortPosition(candle, macdMain, macdSignal);
	}

	private void UpdateFractalBuffers(ICandleMessage candle)
	{
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		_close5 = _close4;
		_close4 = _close3;
		_close3 = _close2;
		_close2 = _close1;
		_close1 = candle.ClosePrice;

		_high2 = _high1;
		_high1 = candle.HighPrice;

		_low2 = _low1;
		_low1 = candle.LowPrice;

		if (_h3 > 0m && _h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
		_upFractal = _h3;

		if (_l3 > 0m && _l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
		_downFractal = _l3;
	}

	private void UpdateRecentExtremes(ICandleMessage candle)
	{
		if (_recentHighs.Length == 0)
		return;

		_recentHighs[_recentIndex] = candle.HighPrice;
		_recentLows[_recentIndex] = candle.LowPrice;
		_recentIndex = (_recentIndex + 1) % _recentHighs.Length;
		_recentCount = Math.Min(_recentCount + 1, _recentHighs.Length);

		if (Position > 0)
		_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
		else if (Position < 0)
		_lowestPrice = Math.Min(_lowestPrice == 0m ? candle.LowPrice : _lowestPrice, candle.LowPrice);
	}

	private void UpdateMomentumHistory(IIndicatorValue rocValue)
	{
		if (!rocValue.IsFinal)
		return;

		if (!_rateOfChange.IsFormed)
		return;

		var momentum = 100m + rocValue.ToDecimal();
		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = momentum;
	}

	private void UpdateEquityPeak(ICandleMessage candle)
	{
		var equity = GetPortfolioValue() + GetUnrealizedPnL(candle);
		_equityPeak = Math.Max(_equityPeak, equity);
	}

	private bool IndicatorsReady()
	{
		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		return false;

		if (!_rateOfChange.IsFormed)
		return false;

		if (UseAtrFilter && !_atr.IsFormed)
		return false;

		return _macd.IsFormed;
	}

	private void TryEnterLong(ICandleMessage candle, decimal fastMa, decimal slowMa, RenkoDirections renkoDirection, decimal macdMain, decimal macdSignal)
	{
		if (_upFractal is not decimal upFractal)
		return;

		if (_momentum1 is not decimal mom1 || _momentum2 is not decimal mom2 || _momentum3 is not decimal mom3)
		return;

		var momCheck = Math.Abs(100m - mom1) > MomentumBuyThreshold || Math.Abs(100m - mom2) > MomentumBuyThreshold || Math.Abs(100m - mom3) > MomentumBuyThreshold;
		if (!momCheck)
		return;

		if (fastMa <= slowMa)
		return;

		if (renkoDirection != RenkoDirections.Up)
		return;

		if (!(_close2 <= upFractal || _close3 <= upFractal || _close4 <= upFractal))
		return;

		if (_close1 <= upFractal)
		return;

		if (!(_low2 < _high1))
		return;

		if (macdMain <= macdSignal)
		return;

		if (Position < 0)
		return;

		if (_activeTrades >= MaxTrades)
		return;

		var volume = GetNextTradeVolume();
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		_activeTrades++;
	}

	private void TryEnterShort(ICandleMessage candle, decimal fastMa, decimal slowMa, RenkoDirections renkoDirection, decimal macdMain, decimal macdSignal)
	{
		if (_downFractal is not decimal downFractal)
		return;

		if (_momentum1 is not decimal mom1 || _momentum2 is not decimal mom2 || _momentum3 is not decimal mom3)
		return;

		var momCheck = Math.Abs(100m - mom1) > MomentumSellThreshold || Math.Abs(100m - mom2) > MomentumSellThreshold || Math.Abs(100m - mom3) > MomentumSellThreshold;
		if (!momCheck)
		return;

		if (fastMa >= slowMa)
		return;

		if (renkoDirection != RenkoDirections.Down)
		return;

		if (!(_close2 >= downFractal || _close3 >= downFractal || _close4 >= downFractal))
		return;

		if (_close1 >= downFractal)
		return;

		if (!(_low1 < _high2))
		return;

		if (macdMain >= macdSignal)
		return;

		if (Position > 0)
		return;

		if (_activeTrades >= MaxTrades)
		return;

		var volume = GetNextTradeVolume();
		if (volume <= 0m)
		return;

		SellMarket(volume);
		_activeTrades++;
	}

	private void ManageLongPosition(ICandleMessage candle, decimal macdMain, decimal macdSignal)
	{
		if (Position <= 0)
		return;

		var entry = PositionAvgPrice;
		var stopDistance = ToPrice(StopLossPips);
		var takeDistance = ToPrice(TakeProfitPips);

		_longStop ??= entry - stopDistance;
		_longTake ??= entry + takeDistance;

		if (UseBreakEven)
		{
			var trigger = ToPrice(BreakEvenTriggerPips);
			var offset = ToPrice(BreakEvenOffsetPips);
			if (candle.HighPrice >= entry + trigger)
			{
				var newStop = entry + offset;
				if (_longStop < newStop)
				_longStop = newStop;
			}
		}

		if (TrailingStopPips > 0m)
		{
			var trailingDistance = ToPrice(TrailingStopPips);
			if (_highestPrice > 0m)
			{
				var candidate = _highestPrice - trailingDistance;
				if (_longStop < candidate)
				_longStop = candidate;
			}
		}

		if (_longStop is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Math.Abs(Position));
			return;
		}

		if (_longTake is decimal take && candle.HighPrice >= take)
		{
			SellMarket(Math.Abs(Position));
			return;
		}

		if (macdMain < macdSignal)
		{
			SellMarket(Math.Abs(Position));
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal macdMain, decimal macdSignal)
	{
		if (Position >= 0)
		return;

		var entry = PositionAvgPrice;
		var stopDistance = ToPrice(StopLossPips);
		var takeDistance = ToPrice(TakeProfitPips);

		_shortStop ??= entry + stopDistance;
		_shortTake ??= entry - takeDistance;

		if (UseBreakEven)
		{
			var trigger = ToPrice(BreakEvenTriggerPips);
			var offset = ToPrice(BreakEvenOffsetPips);
			if (candle.LowPrice <= entry - trigger)
			{
				var newStop = entry - offset;
				if (_shortStop > newStop)
				_shortStop = newStop;
			}
		}

		if (TrailingStopPips > 0m)
		{
			var trailingDistance = ToPrice(TrailingStopPips);
			if (_lowestPrice != 0m && _lowestPrice < entry)
			{
				var candidate = _lowestPrice + trailingDistance;
				if (_shortStop > candidate)
				_shortStop = candidate;
			}
		}

		if (_shortStop is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		if (_shortTake is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		if (macdMain > macdSignal)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void CheckRiskManagement(ICandleMessage candle, decimal macdMain, decimal macdSignal)
	{
		if (UseEquityStop)
		{
			var equity = GetPortfolioValue() + GetUnrealizedPnL(candle);
			var drawdown = _equityPeak - equity;
			var limit = _equityPeak * EquityRiskPercent / 100m;
			if (drawdown > limit)
			CloseAllPositions();
		}

		if (Position > 0 && macdMain < macdSignal)
		{
			SellMarket(Math.Abs(Position));
		}

		if (Position < 0 && macdMain > macdSignal)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private RenkoDirections EvaluateRenkoDirection(decimal closePrice, decimal atr)
	{
		var threshold = UseAtrFilter ? atr : ToPrice(BoxSizePips);
		if (threshold <= 0m || _recentCount == 0)
		return RenkoDirections.None;

		for (var i = 0; i < _recentCount; i++)
		{
			if (closePrice - _recentLows[i] >= threshold)
			return RenkoDirections.Up;

			if (_recentHighs[i] - closePrice >= threshold)
			return RenkoDirections.Down;
		}

		return RenkoDirections.None;
	}

	private decimal GetNextTradeVolume()
	{
		var exponent = (decimal)Math.Pow((double)LotExponent, _activeTrades);
		var volume = BaseVolume * exponent;
		return AlignVolume(volume);
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.VolumeMin ?? 0m;
		var max = security.VolumeMax ?? decimal.MaxValue;

		if (min > 0m && volume < min)
		volume = min;

		if (max > 0m && volume > max)
		volume = max;

		if (step > 0m)
		{
			var steps = Math.Round(volume / step);
			volume = steps * step;
		}

		return volume;
	}

	private decimal ToPrice(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return pips;

		var step = security.Step ?? 0m;
		if (step == 0m)
		return pips;

		var pip = (step == 0.00001m || step == 0.001m) ? step * 10m : step;
		return pips * pip;
	}

	private void ApplyMoneyTrailing(ICandleMessage candle)
	{
		if (!UseMoneyTarget || MoneyTakeProfit <= 0m || MoneyStopLoss <= 0m)
		return;

		var floating = GetUnrealizedPnL(candle);
		if (floating >= MoneyTakeProfit)
		{
			_moneyTrailPeak = Math.Max(_moneyTrailPeak, floating);
			if (_moneyTrailPeak - floating >= MoneyStopLoss)
			CloseAllPositions();
		}
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0)
		return 0m;

		var priceDiff = candle.ClosePrice - PositionAvgPrice;
		return priceDiff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		var value = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (value == 0m && _initialEquity != 0m)
		value = _initialEquity + _realizedPnL;
		return value;
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		SellMarket(Math.Abs(Position));
		else if (Position < 0)
		BuyMarket(Math.Abs(Position));
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_openVolume >= 0m)
			{
				var total = _openVolume + volume;
				_averageEntryPrice = total == 0m ? 0m : (_averageEntryPrice * _openVolume + price * volume) / total;
				_openVolume = total;
			}
			else
			{
				var closingVolume = Math.Min(volume, Math.Abs(_openVolume));
				_realizedPnL += (_averageEntryPrice - price) * closingVolume;
				_openVolume += volume;
				if (_openVolume >= 0m)
				_averageEntryPrice = _openVolume == 0m ? 0m : price;
			}
		}
		else
		{
			if (_openVolume <= 0m)
			{
				var total = Math.Abs(_openVolume) + volume;
				_averageEntryPrice = total == 0m ? 0m : (_averageEntryPrice * Math.Abs(_openVolume) + price * volume) / total;
				_openVolume -= volume;
			}
			else
			{
				var closingVolume = Math.Min(volume, _openVolume);
				_realizedPnL += (price - _averageEntryPrice) * closingVolume;
				_openVolume -= volume;
				if (_openVolume <= 0m)
				_averageEntryPrice = _openVolume == 0m ? 0m : price;
			}
		}

		if (_openVolume == 0m)
		{
			_activeTrades = 0;
			_highestPrice = 0m;
			_lowestPrice = 0m;
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
			_moneyTrailPeak = 0m;
		}
	}

	private enum RenkoDirections
	{
		None,
		Up,
		Down
	}
}