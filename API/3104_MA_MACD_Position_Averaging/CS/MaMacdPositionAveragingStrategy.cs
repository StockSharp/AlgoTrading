using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average with MACD filter strategy that averages down losing positions.
/// Converted from the MetaTrader expert "MA MACD Position averaging".
/// </summary>
public class MaMacdPositionAveragingStrategy : Strategy
{
	private const int BufferCapacity = 512;

	private sealed class PositionLeg
	{
		public bool IsLong;
		public decimal Volume;
		public decimal EntryPrice;
		public decimal? StopPrice;
		public decimal? TakePrice;
	}

	private readonly List<PositionLeg> _legs = new();

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _stepLossingPips;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _maAppliedPrice;
	private readonly StrategyParam<int> _indentPips;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<AppliedPriceType> _macdAppliedPrice;
	private readonly StrategyParam<decimal> _macdRatio;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _ma = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private readonly List<decimal> _maValues = new();
	private readonly List<(decimal Macd, decimal Signal)> _macdValues = new();

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;
	private decimal _stepLossOffset;
	private decimal _indentOffset;

	/// <summary>
	/// Base order volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Increment in pips required before the trailing stop advances.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum adverse excursion in pips that triggers averaging.
	/// </summary>
	public int StepLossingPips
	{
		get => _stepLossingPips.Value;
		set => _stepLossingPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous leg volume when averaging down.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Number of completed bars to shift the indicator sampling.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Moving average lookback period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift of the moving average in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source supplied to the moving average.
	/// </summary>
	public AppliedPriceType MaAppliedPrice
	{
		get => _maAppliedPrice.Value;
		set => _maAppliedPrice.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and the moving average in pips.
	/// </summary>
	public int IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Fast EMA period of the MACD filter.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the MACD filter.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period of the MACD filter.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Price source used by the MACD filter.
	/// </summary>
	public AppliedPriceType MacdAppliedPrice
	{
		get => _macdAppliedPrice.Value;
		set => _macdAppliedPrice.Value = value;
	}

	/// <summary>
	/// Minimum MACD main-to-signal ratio required before trading.
	/// </summary>
	public decimal MacdRatio
	{
		get => _macdRatio.Value;
		set => _macdRatio.Value = value;
	}

	/// <summary>
	/// Candle type driving the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MaMacdPositionAveragingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Order Volume", "Base trade size", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop offset", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Trailing Step (pips)", "Additional move before trailing stop updates", "Risk");

		_stepLossingPips = Param(nameof(StepLossingPips), 30)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Averaging Step (pips)", "Loss in pips that triggers averaging", "Risk");

		_lotCoefficient = Param(nameof(LotCoefficient), 2m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Lot Coefficient", "Multiplier for averaging trades", "Risk");

		_signalBar = Param(nameof(SignalBar), 0)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Number of completed bars to look back", "Signals");

		_maPeriod = Param(nameof(MaPeriod), 15)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("MA Period", "Moving average length", "Indicators");

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Horizontal shift of the moving average", "Indicators");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Weighted)
			.SetDisplay("MA Method", "Moving average smoothing type", "Indicators");

		_maAppliedPrice = Param(nameof(MaAppliedPrice), AppliedPriceType.Weighted)
			.SetDisplay("MA Price", "Applied price for the moving average", "Indicators");

		_indentPips = Param(nameof(IndentPips), 4)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("MA Indent (pips)", "Minimum gap between price and MA", "Signals");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("MACD Signal", "Signal line length", "Indicators");

		_macdAppliedPrice = Param(nameof(MacdAppliedPrice), AppliedPriceType.Weighted)
			.SetDisplay("MACD Price", "Applied price for MACD", "Indicators");

		_macdRatio = Param(nameof(MacdRatio), 0.9m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("MACD Ratio", "Required MACD main/signal ratio", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_legs.Clear();
		_maValues.Clear();
		_macdValues.Clear();

		_pipSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
		_stepLossOffset = 0m;
		_indentOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_pipSize = CalculatePipSize();
		_stopLossOffset = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		_takeProfitOffset = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		_trailingStopOffset = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
		_trailingStepOffset = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
		_stepLossOffset = StepLossingPips > 0 ? StepLossingPips * _pipSize : 0m;
		_indentOffset = IndentPips > 0 ? IndentPips * _pipSize : 0m;

		_ma = CreateMovingAverage(MaMethod, MaPeriod);
		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod,
		};

		_ma.Reset();
		_macd.Reset();
		_maValues.Clear();
		_macdValues.Clear();
		_legs.Clear();

		Volume = AdjustVolume(OrderVolume);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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
		// React only to fully formed candles to avoid double processing.
		if (candle.State != CandleStates.Finished)
			return;

		// Execute virtual stop-loss and take-profit handling before new decisions.
		ProcessStopsAndTargets(candle);

		// Update trailing stops for surviving legs.
		ApplyTrailing(candle);

		var maInput = GetAppliedPrice(candle, MaAppliedPrice);
		var maValue = _ma.Process(maInput, candle.OpenTime, true).ToDecimal();

		if (!_ma.IsFormed)
			return;

		PushValue(_maValues, maValue);

		var macdInput = GetAppliedPrice(candle, MacdAppliedPrice);
		var macdValue = (MovingAverageConvergenceDivergenceValue)_macd.Process(macdInput, candle.OpenTime, true);

		if (!_macd.IsFormed)
			return;

		PushValue(_macdValues, (macdValue.Macd, macdValue.Signal));

		if (!TryGetValue(_maValues, SignalBar + MaShift, out var sampledMa))
			return;

		if (!TryGetValue(_macdValues, SignalBar, out var sampledMacd))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hasLong = false;
		var hasShort = false;

		for (var i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];

			if (leg.IsLong)
				hasLong = true;
			else
				hasShort = true;
		}

		if (hasLong && hasShort)
		{
			CloseAllLegs();
			return;
		}

		var currentPrice = candle.ClosePrice;

		if (hasLong)
		{
			if (_stepLossOffset <= 0m)
				return;

			var worstLong = GetWorstLong(currentPrice);

			if (worstLong != null)
			{
				var volume = AdjustVolume(worstLong.Volume * LotCoefficient);

				if (volume > 0m && OpenLong(volume, currentPrice))
					return;
			}

			return;
		}

		if (hasShort)
		{
			if (_stepLossOffset <= 0m)
				return;

			var worstShort = GetWorstShort(currentPrice);

			if (worstShort != null)
			{
				var volume = AdjustVolume(worstShort.Volume * LotCoefficient);

				if (volume > 0m && OpenShort(volume, currentPrice))
					return;
			}

			return;
		}

		var (macdMain, macdSignal) = sampledMacd;

		if (macdSignal == 0m)
			return;

		var ratio = macdMain / macdSignal;

		// Long entries require both MACD lines below zero and price above the moving average.
		if (macdMain < 0m && macdSignal < 0m && currentPrice > sampledMa)
		{
			if (ratio >= MacdRatio && currentPrice - sampledMa >= _indentOffset)
			{
				var volume = AdjustVolume(OrderVolume);

				if (volume > 0m)
					OpenLong(volume, currentPrice);
			}

			return;
		}

		// Short entries require both MACD lines above zero and price below the moving average.
		if (macdMain > 0m && macdSignal > 0m && currentPrice < sampledMa)
		{
			if (ratio >= MacdRatio && sampledMa - currentPrice >= _indentOffset)
			{
				var volume = AdjustVolume(OrderVolume);

				if (volume > 0m)
					OpenShort(volume, currentPrice);
			}
		}
	}

	private void ProcessStopsAndTargets(ICandleMessage candle)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (leg.IsLong)
			{
				if (leg.StopPrice is decimal stop && low <= stop)
				{
					SellMarket(leg.Volume);
					_legs.RemoveAt(i);
					continue;
				}

				if (leg.TakePrice is decimal take && high >= take)
				{
					SellMarket(leg.Volume);
					_legs.RemoveAt(i);
				}
			}
			else
			{
				if (leg.StopPrice is decimal stop && high >= stop)
				{
					BuyMarket(leg.Volume);
					_legs.RemoveAt(i);
					continue;
				}

				if (leg.TakePrice is decimal take && low <= take)
				{
					BuyMarket(leg.Volume);
					_legs.RemoveAt(i);
				}
			}
		}
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
			return;

		var currentPrice = candle.ClosePrice;

		for (var i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];

			if (leg.IsLong)
			{
				var profit = currentPrice - leg.EntryPrice;

				if (profit > _trailingStopOffset + _trailingStepOffset)
				{
					var threshold = currentPrice - (_trailingStopOffset + _trailingStepOffset);

					if (leg.StopPrice == null || leg.StopPrice.Value < threshold)
					{
						leg.StopPrice = AlignPrice(currentPrice - _trailingStopOffset);
					}
				}
			}
			else
			{
				var profit = leg.EntryPrice - currentPrice;

				if (profit > _trailingStopOffset + _trailingStepOffset)
				{
					var threshold = currentPrice + (_trailingStopOffset + _trailingStepOffset);

					if (leg.StopPrice == null || leg.StopPrice.Value > threshold)
					{
						leg.StopPrice = AlignPrice(currentPrice + _trailingStopOffset);
					}
				}
			}
		}
	}

	private PositionLeg? GetWorstLong(decimal currentPrice)
	{
		PositionLeg? candidate = null;

		for (var i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];

			if (!leg.IsLong)
				continue;

			var loss = leg.EntryPrice - currentPrice;

			if (loss < _stepLossOffset)
				continue;

			if (candidate == null || leg.EntryPrice < candidate.EntryPrice)
				candidate = leg;
		}

		return candidate;
	}

	private PositionLeg? GetWorstShort(decimal currentPrice)
	{
		PositionLeg? candidate = null;

		for (var i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];

			if (leg.IsLong)
				continue;

			var loss = currentPrice - leg.EntryPrice;

			if (loss < _stepLossOffset)
				continue;

			if (candidate == null || leg.EntryPrice > candidate.EntryPrice)
				candidate = leg;
		}

		return candidate;
	}

	private bool OpenLong(decimal volume, decimal price)
	{
		var order = BuyMarket(volume);

		if (order is null)
			return false;

		var stop = _stopLossOffset > 0m ? AlignPrice(price - _stopLossOffset) : (decimal?)null;
		var take = _takeProfitOffset > 0m ? AlignPrice(price + _takeProfitOffset) : (decimal?)null;

		_legs.Add(new PositionLeg
		{
			IsLong = true,
			Volume = volume,
			EntryPrice = price,
			StopPrice = stop,
			TakePrice = take,
		});

		return true;
	}

	private bool OpenShort(decimal volume, decimal price)
	{
		var order = SellMarket(volume);

		if (order is null)
			return false;

		var stop = _stopLossOffset > 0m ? AlignPrice(price + _stopLossOffset) : (decimal?)null;
		var take = _takeProfitOffset > 0m ? AlignPrice(price - _takeProfitOffset) : (decimal?)null;

		_legs.Add(new PositionLeg
		{
			IsLong = false,
			Volume = volume,
			EntryPrice = price,
			StopPrice = stop,
			TakePrice = take,
		});

		return true;
	}

	private void CloseAllLegs()
	{
		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (leg.IsLong)
				SellMarket(leg.Volume);
			else
				BuyMarket(leg.Volume);
		}

		_legs.Clear();
	}

	private static void PushValue<T>(List<T> buffer, T value)
	{
		buffer.Add(value);

		if (buffer.Count > BufferCapacity)
			buffer.RemoveAt(0);
	}

	private static bool TryGetValue(List<decimal> buffer, int offset, out decimal value)
	{
		value = 0m;

		if (offset < 0)
			return false;

		var index = buffer.Count - 1 - offset;

		if (index < 0 || index >= buffer.Count)
			return false;

		value = buffer[index];
		return true;
	}

	private static bool TryGetValue(List<(decimal Macd, decimal Signal)> buffer, int offset, out (decimal Macd, decimal Signal) value)
	{
		value = default;

		if (offset < 0)
			return false;

		var index = buffer.Count - 1 - offset;

		if (index < 0 || index >= buffer.Count)
			return false;

		value = buffer[index];
		return true;
	}

	private LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int period)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = period },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			_ => new WeightedMovingAverage { Length = period },
		};
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? GetDecimalsFromStep(step);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return step * factor;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;

		if (step > 0m)
			volume = step * Math.Floor(volume / step);

		var minVolume = Security.MinVolume ?? 0m;

		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = Security.MaxVolume;

		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal AlignPrice(decimal price)
	{
		if (Security is null)
			return price;

		var step = Security.PriceStep ?? 0m;

		if (step <= 0m)
			return price;

		var steps = Math.Round(price / step, MidpointRounding.AwayFromZero);
		return steps * step;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
			return 0;

		var value = Math.Abs(Math.Log10((double)step));
		return (int)Math.Round(value);
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Moving average calculation options mirroring MetaTrader modes.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		Weighted,
	}

	/// <summary>
	/// Price selection modes compatible with MetaTrader applied prices.
	/// </summary>
	public enum AppliedPriceType
	{
		/// <summary>Use the candle close price.</summary>
		Close,
		/// <summary>Use the candle open price.</summary>
		Open,
		/// <summary>Use the candle high price.</summary>
		High,
		/// <summary>Use the candle low price.</summary>
		Low,
		/// <summary>Use the median price (high + low) / 2.</summary>
		Median,
		/// <summary>Use the typical price (close + high + low) / 3.</summary>
		Typical,
		/// <summary>Use the weighted price (high + low + 2 * close) / 4.</summary>
		Weighted,
	}
}
