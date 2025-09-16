using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gordago oscillator-based strategy ported from MetaTrader 5.
/// Combines higher timeframe MACD and stochastic filters with trailing protection.
/// </summary>
public class GordagoEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _stochasticBuyLevel;
	private readonly StrategyParam<decimal> _stochasticSellLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<DataType> _stochasticCandleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSignalPeriod;
	private readonly StrategyParam<int> _stochasticSmoothing;

	private MovingAverageConvergenceDivergence _macd = null!;
	private Stochastic _stochastic = null!;

	private decimal? _macdPrevious;
	private decimal? _macdCurrent;
	private decimal? _stochasticPrevious;
	private decimal? _stochasticCurrent;
	private decimal? _entryPrice;
	private decimal? _stopLossLevel;
	private decimal? _takeProfitLevel;
	private bool _isLongPosition;
	private decimal _pipSize;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades in pips.
	/// </summary>
	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades in pips.
	/// </summary>
	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades in pips.
	/// </summary>
	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades in pips.
	/// </summary>
	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
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
	/// Trailing stop step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Stochastic threshold to trigger long entries.
	/// </summary>
	public decimal StochasticBuyLevel
	{
		get => _stochasticBuyLevel.Value;
		set => _stochasticBuyLevel.Value = value;
	}

	/// <summary>
	/// Stochastic threshold to trigger short entries.
	/// </summary>
	public decimal StochasticSellLevel
	{
		get => _stochasticSellLevel.Value;
		set => _stochasticSellLevel.Value = value;
	}

	/// <summary>
	/// Working candle series for trade logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for MACD calculations.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for stochastic calculations.
	/// </summary>
	public DataType StochasticCandleType
	{
		get => _stochasticCandleType.Value;
		set => _stochasticCandleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Base lookback length for the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D line.
	/// </summary>
	public int StochasticSignalPeriod
	{
		get => _stochasticSignalPeriod.Value;
		set => _stochasticSignalPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to %K.
	/// </summary>
	public int StochasticSmoothing
	{
		get => _stochasticSmoothing.Value;
		set => _stochasticSmoothing.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GordagoEaStrategy"/>.
	/// </summary>
	public GordagoEaStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 40m)
			.SetDisplay("Buy SL", "Stop loss for long trades (pips)", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 70m)
			.SetDisplay("Buy TP", "Take profit for long trades (pips)", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 150m, 10m);

		_stopLossSellPips = Param(nameof(StopLossSellPips), 10m)
			.SetDisplay("Sell SL", "Stop loss for short trades (pips)", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 10m);

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 40m)
			.SetDisplay("Sell TP", "Take profit for short trades (pips)", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop", "Trailing distance (pips)", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetDisplay("Trailing Step", "Trailing step (pips)", "Risk");

		_stochasticBuyLevel = Param(nameof(StochasticBuyLevel), 37m)
			.SetDisplay("Stochastic Buy", "Upper threshold for long entries", "Filters")
			.SetRange(0m, 100m);

		_stochasticSellLevel = Param(nameof(StochasticSellLevel), 96m)
			.SetDisplay("Stochastic Sell", "Lower threshold for short entries", "Filters")
			.SetRange(0m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(3).TimeFrame())
			.SetDisplay("Base Timeframe", "Execution candles", "Timeframes");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromMinutes(12).TimeFrame())
			.SetDisplay("MACD Timeframe", "Timeframe for MACD", "Timeframes");

		_stochasticCandleType = Param(nameof(StochasticCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Stochastic Timeframe", "Timeframe for stochastic", "Timeframes");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period", "Indicators");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Length", "Lookback for %K", "Indicators");

		_stochasticSignalPeriod = Param(nameof(StochasticSignalPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch D", "Smoothing for %D", "Indicators");

		_stochasticSmoothing = Param(nameof(StochasticSmoothing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Smooth", "Smoothing for %K", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (MacdCandleType != CandleType)
			yield return (Security, MacdCandleType);

		if (StochasticCandleType != CandleType && StochasticCandleType != MacdCandleType)
			yield return (Security, StochasticCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macdPrevious = null;
		_macdCurrent = null;
		_stochasticPrevious = null;
		_stochasticCurrent = null;
		_entryPrice = null;
		_stopLossLevel = null;
		_takeProfitLevel = null;
		_isLongPosition = default;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			LogWarning("Trailing stop is enabled but trailing step is not positive. Trailing will be ignored.");
		}

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod,
		};

		_stochastic = new Stochastic
		{
			Length = StochasticLength,
			KPeriod = StochasticSmoothing,
			DPeriod = StochasticSignalPeriod,
		};

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.WhenNew(ProcessBaseCandle).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.Bind(_macd, ProcessMacd).Start();

		var stochasticSubscription = SubscribeCandles(StochasticCandleType);
		stochasticSubscription.BindEx(_stochastic, ProcessStochastic).Start();

		_pipSize = CalculatePipSize();
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histogramValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_macdCurrent is null)
		{
			_macdCurrent = macdValue;
			return;
		}

		_macdPrevious = _macdCurrent;
		_macdCurrent = macdValue;
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (StochasticValue)indicatorValue;

		if (value.K is not decimal kValue)
			return;

		if (_stochasticCurrent is null)
		{
			_stochasticCurrent = kValue;
			return;
		}

		_stochasticPrevious = _stochasticCurrent;
		_stochasticCurrent = kValue;
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle);
		CheckProtectiveLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_macdPrevious is null || _macdCurrent is null)
			return;

		if (_stochasticPrevious is null || _stochasticCurrent is null)
			return;

		var macdPrev = _macdPrevious.Value;
		var macdCurr = _macdCurrent.Value;
		var stochPrev = _stochasticPrevious.Value;
		var stochCurr = _stochasticCurrent.Value;

		var buySignal = macdCurr > macdPrev && macdPrev < 0m && stochCurr < StochasticBuyLevel && stochCurr > stochPrev;
		var sellSignal = macdCurr < macdPrev && macdPrev > 0m && stochCurr > StochasticSellLevel && stochCurr < stochPrev;

		if (buySignal && Position <= 0m)
		{
			TryEnterLong(candle);
		}
		else if (sellSignal && Position >= 0m)
		{
			TryEnterShort(candle);
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var volume = OrderVolume + Math.Max(0m, -Position);

		if (volume <= 0m)
			return;

		var stopLoss = CalculateStopPrice(candle.ClosePrice, StopLossBuyPips, true);

		if (stopLoss is decimal sl && sl >= candle.ClosePrice)
			return;

		var takeProfit = CalculateTakePrice(candle.ClosePrice, TakeProfitBuyPips, true);

		BuyMarket(volume);

		_isLongPosition = true;
		_entryPrice = candle.ClosePrice;
		_stopLossLevel = stopLoss;
		_takeProfitLevel = takeProfit;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var volume = OrderVolume + Math.Max(0m, Position);

		if (volume <= 0m)
			return;

		var stopLoss = CalculateStopPrice(candle.ClosePrice, StopLossSellPips, false);

		if (stopLoss is decimal sl && sl <= candle.ClosePrice)
			return;

		var takeProfit = CalculateTakePrice(candle.ClosePrice, TakeProfitSellPips, false);

		SellMarket(volume);

		_isLongPosition = false;
		_entryPrice = candle.ClosePrice;
		_stopLossLevel = stopLoss;
		_takeProfitLevel = takeProfit;
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (_entryPrice is null)
			return;

		if (_isLongPosition && Position > 0m)
		{
			if (_stopLossLevel is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitLevel is decimal tp && tp > 0m && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (!_isLongPosition && Position < 0m)
		{
			var absPosition = Math.Abs(Position);

			if (_stopLossLevel is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(absPosition);
				ResetPositionState();
				return;
			}

			if (_takeProfitLevel is decimal tp && tp > 0m && candle.LowPrice <= tp)
			{
				BuyMarket(absPosition);
				ResetPositionState();
			}
		}
		else if (Position == 0m)
		{
			ResetPositionState();
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
			return;

		if (_pipSize <= 0m || _entryPrice is null)
			return;

		if (_isLongPosition && Position > 0m)
		{
			var profit = candle.ClosePrice - _entryPrice.Value;
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			if (profit > trailingDistance + trailingStep)
			{
				var trigger = candle.ClosePrice - (trailingDistance + trailingStep);

				if (!_stopLossLevel.HasValue || _stopLossLevel.Value < trigger)
				{
					_stopLossLevel = candle.ClosePrice - trailingDistance;
				}
			}
		}
		else if (!_isLongPosition && Position < 0m)
		{
			var profit = _entryPrice.Value - candle.ClosePrice;
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			if (profit > trailingDistance + trailingStep)
			{
				var trigger = candle.ClosePrice + (trailingDistance + trailingStep);

				if (!_stopLossLevel.HasValue || _stopLossLevel.Value > trigger)
				{
					_stopLossLevel = candle.ClosePrice + trailingDistance;
				}
			}
		}
	}

	private decimal? CalculateStopPrice(decimal closePrice, decimal distanceInPips, bool isLong)
	{
		if (distanceInPips <= 0m || _pipSize <= 0m)
			return null;

		var offset = distanceInPips * _pipSize;
		return isLong ? closePrice - offset : closePrice + offset;
	}

	private decimal? CalculateTakePrice(decimal closePrice, decimal distanceInPips, bool isLong)
	{
		if (distanceInPips <= 0m || _pipSize <= 0m)
			return null;

		var offset = distanceInPips * _pipSize;
		return isLong ? closePrice + offset : closePrice - offset;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossLevel = null;
		_takeProfitLevel = null;
		_isLongPosition = default;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep;

		if (priceStep is null || priceStep.Value <= 0m)
			return 0m;

		var digits = CountDecimalDigits(priceStep.Value);
		var multiplier = (digits == 3 || digits == 5) ? 10m : 1m;
		return priceStep.Value * multiplier;
	}

	private static int CountDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Truncate(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}
}
