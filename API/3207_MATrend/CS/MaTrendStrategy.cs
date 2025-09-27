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

/// <summary>
/// Trend-following strategy based on a single moving average.
/// Opens long positions when price closes above the shifted moving average and shorts when it closes below.
/// Includes optional stop-loss, take-profit, and trailing stop management mimicking the original MQL expert.
/// </summary>
public class MaTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageKinds> _maMethod;
	private readonly StrategyParam<AppliedPriceModes> _appliedPrice;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _movingAverage;
	private readonly List<decimal> _maHistory = new();
	private decimal _pipSize;
	private int _historyCapacity;

	private decimal? _longEntryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortEntryPrice;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaTrendStrategy"/> class.
	/// </summary>
	public MaTrendStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Position size in lots or contracts.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance to the protective stop in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 140)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Base trailing-stop distance in pips.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Extra pip move required before tightening the trailing stop.", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the moving average.", "Indicator");

		_maShift = Param(nameof(MaShift), 3)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Forward shift (in bars) applied to the moving average.", "Indicator");

		_maMethod = Param(nameof(MaMethod), MovingAverageKinds.Weighted)
			.SetDisplay("MA Method", "Moving average calculation mode.", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceModes.Weighted)
			.SetDisplay("Applied Price", "Candle price source fed into the moving average.", "Indicator");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only One Position", "Allow just a single open position at a time.", "Behaviour");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Swap long and short entry conditions.", "Behaviour");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close Opposite", "Close the opposite exposure before opening a new trade.", "Behaviour");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the strategy.", "General");
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
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
	/// Additional pip move required before the trailing stop advances.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed bars used to shift the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MovingAverageKinds MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Candle price source passed into the moving average.
	/// </summary>
	public AppliedPriceModes AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Whether the strategy keeps only a single open position.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Whether to invert the trading signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Whether to close the opposite exposure before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_movingAverage = null;
		_maHistory.Clear();
		_pipSize = 0m;
		_historyCapacity = 0;

		ResetLongProtection();
		ResetShortProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		Volume = OrderVolume;

		_movingAverage = CreateMovingAverage(MaMethod, MaPeriod);
		_maHistory.Clear();
		_historyCapacity = Math.Max(MaShift, 0) + 5;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_movingAverage != null)
				DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_movingAverage == null)
			return;

		var price = GetAppliedPrice(candle, AppliedPrice);
		var maValue = _movingAverage.Process(price, candle.OpenTime, true).ToDecimal();

		if (!_movingAverage.IsFormed)
			return;

		_maHistory.Add(maValue);
		TrimHistory(_maHistory, _historyCapacity);

		UpdateTrailing(candle);
		CheckExitByStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_maHistory.Count <= MaShift)
			return;

		var maIndex = _maHistory.Count - 1 - MaShift;
		var shiftedMa = _maHistory[maIndex];

		var closePrice = candle.ClosePrice;
		var buySignal = !ReverseSignals ? closePrice > shiftedMa : closePrice < shiftedMa;
		var sellSignal = !ReverseSignals ? closePrice < shiftedMa : closePrice > shiftedMa;

		if (buySignal && sellSignal)
			return;

		if (buySignal)
		{
			TryEnterLong(candle);
		}
		else if (sellSignal)
		{
			TryEnterShort(candle);
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (OrderVolume <= 0m)
			return;

		if (CloseOpposite && Position < 0m)
		{
			var volumeToClose = Math.Abs(Position);
			if (volumeToClose > 0)
			{
				BuyMarket(volumeToClose);
				ResetShortProtection();
			}
		}

		if (OnlyOnePosition && Position != 0m)
			return;

		BuyMarket(OrderVolume);

		_longEntryPrice = candle.ClosePrice;
		_longStop = StopLossPips > 0 && _pipSize > 0m ? candle.ClosePrice - StopLossPips * _pipSize : null;
		_longTake = TakeProfitPips > 0 && _pipSize > 0m ? candle.ClosePrice + TakeProfitPips * _pipSize : null;

		ResetShortProtection();
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (OrderVolume <= 0m)
			return;

		if (CloseOpposite && Position > 0m)
		{
			var volumeToClose = Math.Abs(Position);
			if (volumeToClose > 0)
			{
				SellMarket(volumeToClose);
				ResetLongProtection();
			}
		}

		if (OnlyOnePosition && Position != 0m)
			return;

		SellMarket(OrderVolume);

		_shortEntryPrice = candle.ClosePrice;
		_shortStop = StopLossPips > 0 && _pipSize > 0m ? candle.ClosePrice + StopLossPips * _pipSize : null;
		_shortTake = TakeProfitPips > 0 && _pipSize > 0m ? candle.ClosePrice - TakeProfitPips * _pipSize : null;

		ResetLongProtection();
	}

	private void CheckExitByStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
					SellMarket(volume);
				ResetLongProtection();
				return;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
					SellMarket(volume);
				ResetLongProtection();
				return;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
					BuyMarket(volume);
				ResetShortProtection();
				return;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
					BuyMarket(volume);
				ResetShortProtection();
				return;
			}
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0 || _pipSize <= 0m)
			return;

		var offset = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (Position > 0 && _longEntryPrice.HasValue)
		{
			var threshold = candle.ClosePrice - (offset + step);
			if (!_longStop.HasValue || _longStop.Value < threshold)
				_longStop = candle.ClosePrice - offset;
		}
		else if (Position < 0 && _shortEntryPrice.HasValue)
		{
			var threshold = candle.ClosePrice + (offset + step);
			if (!_shortStop.HasValue || _shortStop.Value > threshold)
				_shortStop = candle.ClosePrice + offset;
		}
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	private static void TrimHistory(IList<decimal> list, int limit)
	{
		if (limit <= 0)
		{
			list.Clear();
			return;
		}

		while (list.Count > limit)
		{
			list.RemoveAt(0);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var digits = GetDecimalDigits(step);
		if (digits == 3 || digits == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Floor(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceModes mode)
	{
		return mode switch
		{
			AppliedPriceModes.Open => candle.OpenPrice,
			AppliedPriceModes.High => candle.HighPrice,
			AppliedPriceModes.Low => candle.LowPrice,
			AppliedPriceModes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceModes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceModes.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageKinds kind, int length)
	{
		return kind switch
		{
			MovingAverageKinds.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageKinds.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageKinds.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageKinds.Weighted => new WeightedMovingAverage { Length = length },
			_ => new WeightedMovingAverage { Length = length },
		};
	}

	public enum MovingAverageKinds
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,
		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,
		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Weighted
	}

	/// <summary>
	/// Candle price modes compatible with <see cref="MaTrendStrategy"/>.
	/// </summary>
	public enum AppliedPriceModes
	{
		/// <summary>
		/// Close price of the candle.
		/// </summary>
		Close,
		/// <summary>
		/// Open price of the candle.
		/// </summary>
		Open,
		/// <summary>
		/// High price of the candle.
		/// </summary>
		High,
		/// <summary>
		/// Low price of the candle.
		/// </summary>
		Low,
		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,
		/// <summary>
		/// Typical price (high + low + close) / 3.
		/// </summary>
		Typical,
		/// <summary>
		/// Weighted price (2 * close + high + low) / 4.
		/// </summary>
		Weighted
	}
}
