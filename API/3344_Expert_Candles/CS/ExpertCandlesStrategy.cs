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
/// Candlestick reversal strategy that replicates the MetaTrader Expert_Candles logic using the StockSharp high level API.
/// </summary>
public class ExpertCandlesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<int> _minimumPoints;
	private readonly StrategyParam<decimal> _shadowBig;
	private readonly StrategyParam<decimal> _shadowSmall;
	private readonly StrategyParam<decimal> _limitFactor;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<int> _expirationBars;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;

	private readonly List<CandleSnapshot> _candles = new();

	private decimal _pipSize;
	private TimeSpan? _timeFrame;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	private DateTimeOffset? _longCooldownUntil;
	private DateTimeOffset? _shortCooldownUntil;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpertCandlesStrategy"/> class.
	/// </summary>
	public ExpertCandlesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for detecting candlestick reversals", "General");

		_range = Param(nameof(Range), 3)
			.SetDisplay("Range", "Maximum number of candles combined into a composite pattern", "Signals")
			.SetGreaterThanZero();

		_minimumPoints = Param(nameof(MinimumPoints), 50)
			.SetDisplay("Minimum Size (points)", "Minimal candle height in points before the pattern is considered valid", "Signals")
			.SetGreaterThanZero();

		_shadowBig = Param(nameof(ShadowBig), 0.5m)
			.SetDisplay("Large Shadow Ratio", "Lower (bullish) or upper (bearish) shadow ratio required for the reversal", "Signals")
			.SetGreaterThanZero();

		_shadowSmall = Param(nameof(ShadowSmall), 0.2m)
			.SetDisplay("Small Shadow Ratio", "Upper (bullish) or lower (bearish) shadow ratio that must stay below this level", "Signals")
			.SetGreaterThanZero();

		_limitFactor = Param(nameof(LimitFactor), 0m)
			.SetDisplay("Limit Factor", "Entry price offset expressed as a fraction of the composite candle size", "Risk");

		_stopLossFactor = Param(nameof(StopLossFactor), 2m)
			.SetDisplay("Stop Loss Factor", "Stop loss distance as a multiple of the composite candle size", "Risk")
			.SetGreaterThanZero();

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 1m)
			.SetDisplay("Take Profit Factor", "Take profit distance as a multiple of the composite candle size", "Risk")
			.SetGreaterThanZero();

		_expirationBars = Param(nameof(ExpirationBars), 4)
			.SetDisplay("Expiration Bars", "Number of bars after which a signal expires", "Risk")
			.SetGreaterThanZero();

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetDisplay("Fixed Volume", "Fallback trade volume used when risk based sizing is unavailable", "Risk")
			.SetGreaterThanZero();

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetDisplay("Risk Percent", "Fraction of account equity risked per trade when a stop loss is present", "Risk")
			.SetRange(0m, 100m);
	}

	/// <summary>
	/// Timeframe used for candle aggregation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum number of candles that can be merged into a composite pattern.
	/// </summary>
	public int Range
	{
		get => _range.Value;
		set => _range.Value = value;
	}

	/// <summary>
	/// Minimal height (in points) that the composite candle must reach.
	/// </summary>
	public int MinimumPoints
	{
		get => _minimumPoints.Value;
		set => _minimumPoints.Value = value;
	}

	/// <summary>
	/// Required ratio for the dominant shadow.
	/// </summary>
	public decimal ShadowBig
	{
		get => _shadowBig.Value;
		set => _shadowBig.Value = value;
	}

	/// <summary>
	/// Threshold for the opposite shadow ratio.
	/// </summary>
	public decimal ShadowSmall
	{
		get => _shadowSmall.Value;
		set => _shadowSmall.Value = value;
	}

	/// <summary>
	/// Entry offset expressed as a fraction of the composite range.
	/// </summary>
	public decimal LimitFactor
	{
		get => _limitFactor.Value;
		set => _limitFactor.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier applied to the composite range.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// Take profit multiplier applied to the composite range.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Number of bars during which the signal remains active.
	/// </summary>
	public int ExpirationBars
	{
		get => _expirationBars.Value;
		set => _expirationBars.Value = value;
	}

	/// <summary>
	/// Minimal volume used when money management cannot compute a value.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Percentage of account equity risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		if (_pipSize <= 0m)
		{
			var security = Security;
			if (security?.MinPriceStep is decimal minStep && minStep > 0m)
			{
				_pipSize = minStep;
			}
			else
			{
				_pipSize = 1m;
			}
		}

		_timeFrame = GetTimeFrame();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			// Only process completed candles to mirror the MetaTrader logic.
			return;
		}

		// Derive a synthetic close time if the connector does not provide one.
		var closeTime = candle.CloseTime;
		if (closeTime == default)
		{
			closeTime = _timeFrame.HasValue ? candle.OpenTime + _timeFrame.Value : candle.OpenTime;
		}

		var snapshot = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.OpenTime, closeTime);
		_candles.Add(snapshot);

		while (_candles.Count > 500)
		{
			_candles.RemoveAt(0);
		}

		// Keep track of expired signals.
		UpdateCooldowns(closeTime);
		// Check if the active position needs to be closed by stop-loss or take-profit.
		ManageOpenPosition(snapshot);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (_candles.Count < 2)
		{
			return;
		}

		var index = _candles.Count - 1;
		// Evaluate the composite candle built from the latest bars.
		var signal = EvaluateComposite(index);
		if (!signal.HasSignal)
		{
			return;
		}

		if (signal.IsBullish)
		{
			HandleBullishSignal(snapshot, signal.RangeSize);
		}

		if (signal.IsBearish)
		{
			HandleBearishSignal(snapshot, signal.RangeSize);
		}
	}

	private void HandleBullishSignal(CandleSnapshot candle, decimal rangeSize)
	{
		var position = Position;
		// Close opposite trades before opening a new direction.
		if (position < 0m)
		{
			BuyMarket(-position);
			ResetProtection();
			position = 0m;
		}

		if (position > 0m)
		{
			return;
		}

		if (_longCooldownUntil.HasValue && candle.CloseTime < _longCooldownUntil.Value)
		{
			return;
		}

		if (rangeSize <= 0m)
		{
			return;
		}

		var entryPrice = candle.Close - LimitFactor * rangeSize;
		var stopLoss = StopLossFactor > 0m ? entryPrice - StopLossFactor * rangeSize : (decimal?)null;
		var takeProfit = TakeProfitFactor > 0m ? entryPrice + TakeProfitFactor * rangeSize : (decimal?)null;

		var volume = CalculateVolume(entryPrice, stopLoss, rangeSize);
		if (volume <= 0m)
		{
			return;
		}

		_entryPrice = entryPrice;
		_stopLossPrice = stopLoss;
		_takeProfitPrice = takeProfit;

		BuyMarket(volume);
		_longCooldownUntil = ComputeExpiration(candle.CloseTime);
	}

	private void HandleBearishSignal(CandleSnapshot candle, decimal rangeSize)
	{
		var position = Position;
		// Close opposite trades before opening a new direction.
		if (position > 0m)
		{
			SellMarket(position);
			ResetProtection();
			position = 0m;
		}

		if (position < 0m)
		{
			return;
		}

		if (_shortCooldownUntil.HasValue && candle.CloseTime < _shortCooldownUntil.Value)
		{
			return;
		}

		if (rangeSize <= 0m)
		{
			return;
		}

		var entryPrice = candle.Close + LimitFactor * rangeSize;
		var stopLoss = StopLossFactor > 0m ? entryPrice + StopLossFactor * rangeSize : (decimal?)null;
		var takeProfit = TakeProfitFactor > 0m ? entryPrice - TakeProfitFactor * rangeSize : (decimal?)null;

		var volume = CalculateVolume(entryPrice, stopLoss, rangeSize);
		if (volume <= 0m)
		{
			return;
		}

		_entryPrice = entryPrice;
		_stopLossPrice = stopLoss;
		_takeProfitPrice = takeProfit;

		SellMarket(volume);
		_shortCooldownUntil = ComputeExpiration(candle.CloseTime);
	}

	private void ManageOpenPosition(CandleSnapshot candle)
	{
		var position = Position;
		// Monitor the running trade for protective exits.
		if (position == 0m)
		{
			return;
		}

		if (position > 0m)
		{
			if (_stopLossPrice is decimal sl && candle.Low <= sl)
			{
				SellMarket(position);
				ResetProtection();
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.High >= tp)
			{
				SellMarket(position);
				ResetProtection();
			}
		}
		else
		{
			if (_stopLossPrice is decimal sl && candle.High >= sl)
			{
				BuyMarket(-position);
				ResetProtection();
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.Low <= tp)
			{
				BuyMarket(-position);
				ResetProtection();
			}
		}
	}

	private void UpdateCooldowns(DateTimeOffset closeTime)
	{
		if (_longCooldownUntil.HasValue && closeTime >= _longCooldownUntil.Value)
		{
			_longCooldownUntil = null;
		}

		if (_shortCooldownUntil.HasValue && closeTime >= _shortCooldownUntil.Value)
		{
			_shortCooldownUntil = null;
		}
	}

	private CompositeSignal EvaluateComposite(int index)
	{
		// Build a composite candle that merges neighbouring bars similar to the MQL implementation.
		if (index < 0 || index >= _candles.Count)
		{
			return CompositeSignal.None;
		}

		var threshold = MinimumPoints * _pipSize;
		if (threshold <= 0m)
		{
			threshold = MinimumPoints;
		}

		var candle = _candles[index];
		var open = candle.Open;
		var high = candle.High;
		var low = candle.Low;
		var close = candle.Close;
		var height = high - low;
		if (height < threshold)
		{
			for (var i = 1; i < Range && index - i >= 0; i++)
			{
				var older = _candles[index - i];
				open = older.Open;
				if (older.High > high)
				{
					high = older.High;
				}
				if (older.Low < low)
				{
					low = older.Low;
				}
				height = high - low;
				if (height > threshold)
				{
					break;
				}
			}
		}

		if (height <= threshold)
		{
			return CompositeSignal.None;
		}

		var isBullish = IsBullishComposite(open, high, low, close, height);
		var isBearish = IsBearishComposite(open, high, low, close, height);

		if (!isBullish && !isBearish)
		{
			return CompositeSignal.None;
		}

		return CompositeSignal.Create(isBullish, isBearish, height);
	}

	private bool IsBullishComposite(decimal open, decimal high, decimal low, decimal close, decimal size)
	{
		var shadowHigh = high - Math.Max(open, close);
		var shadowLow = close - low;

		return shadowHigh < ShadowSmall * size && shadowLow > ShadowBig * size;
	}

	private bool IsBearishComposite(decimal open, decimal high, decimal low, decimal close, decimal size)
	{
		var shadowHigh = high - close;
		var shadowLow = Math.Min(open, close) - low;

		return shadowLow < ShadowSmall * size && shadowHigh > ShadowBig * size;
	}

	private decimal CalculateVolume(decimal entryPrice, decimal? stopPrice, decimal rangeSize)
	{
		// Start from the fixed lot size and optionally apply risk-based position sizing.
		var volume = FixedVolume;

		if (!stopPrice.HasValue || RiskPercent <= 0m)
		{
			return volume;
		}

		var portfolio = Portfolio;
		if (portfolio == null)
		{
			return volume;
		}

		var equity = portfolio.CurrentValue ?? portfolio.CurrentBalance ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
		{
			return volume;
		}

		var stopDistance = Math.Abs(entryPrice - stopPrice.Value);
		if (stopDistance <= 0m)
		{
			stopDistance = rangeSize;
		}

		if (stopDistance <= 0m)
		{
			return volume;
		}

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		var moneyPerUnit = priceStep > 0m && stepPrice > 0m ? stopDistance / priceStep * stepPrice : stopDistance;

		if (moneyPerUnit <= 0m)
		{
			return volume;
		}

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
		{
			return volume;
		}

		var calculated = riskAmount / moneyPerUnit;
		if (calculated > 0m)
		{
			volume = Math.Max(calculated, volume);
		}

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		return volume;
	}

	private void ResetProtection()
	{
		// Forget all cached protective levels.
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal GetPipSize()
	{
		// Recreate the MetaTrader point-to-price conversion rules.
		var security = Security;
		if (security == null)
		{
			return 0m;
		}

		if (security.PriceStep is decimal step && step > 0m)
		{
			var decimals = security.Decimals;
			if (decimals == 3 || decimals == 5)
			{
				return step * 10m;
			}

			return step;
		}

		if (security.MinPriceStep is decimal minStep && minStep > 0m)
		{
			return minStep;
		}

		return 0m;
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan span ? span : null;
	}

	private DateTimeOffset? ComputeExpiration(DateTimeOffset closeTime)
	{
		if (!_timeFrame.HasValue || ExpirationBars <= 0)
		{
			return null;
		}

		return closeTime + TimeSpan.FromTicks(_timeFrame.Value.Ticks * ExpirationBars);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetProtection();
		}
	}

	private sealed class CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low, decimal close, DateTimeOffset openTime, DateTimeOffset closeTime)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
			OpenTime = openTime;
			CloseTime = closeTime;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
		public DateTimeOffset OpenTime { get; }
		public DateTimeOffset CloseTime { get; }
	}

	private readonly struct CompositeSignal
	{
		private CompositeSignal(bool hasSignal, bool isBullish, bool isBearish, decimal rangeSize)
		{
			HasSignal = hasSignal;
			IsBullish = isBullish;
			IsBearish = isBearish;
			RangeSize = rangeSize;
		}

		public bool HasSignal { get; }
		public bool IsBullish { get; }
		public bool IsBearish { get; }
		public decimal RangeSize { get; }

		public static CompositeSignal None => default;

		public static CompositeSignal Create(bool isBullish, bool isBearish, decimal rangeSize)
		{
			return new CompositeSignal(true, isBullish, isBearish, rangeSize);
		}
	}
}