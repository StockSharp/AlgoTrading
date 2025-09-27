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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual-stream adaptive Renko strategy converted from the Exp_AdaptiveRenko_Duplex MQL5 expert advisor.
/// Generates independent long and short signals by projecting the Adaptive Renko indicator onto configurable candle series.
/// </summary>
public class AdaptiveRenkoDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<AdaptiveRenkoVolatilityModes> _longVolatilityMode;
	private readonly StrategyParam<AdaptiveRenkoVolatilityModes> _shortVolatilityMode;
	private readonly StrategyParam<int> _longVolatilityPeriod;
	private readonly StrategyParam<int> _shortVolatilityPeriod;
	private readonly StrategyParam<decimal> _longSensitivity;
	private readonly StrategyParam<decimal> _shortSensitivity;
	private readonly StrategyParam<AdaptiveRenkoPriceModes> _longPriceMode;
	private readonly StrategyParam<AdaptiveRenkoPriceModes> _shortPriceMode;
	private readonly StrategyParam<decimal> _longMinimumBrickPoints;
	private readonly StrategyParam<decimal> _shortMinimumBrickPoints;
	private readonly StrategyParam<int> _longSignalBarOffset;
	private readonly StrategyParam<int> _shortSignalBarOffset;
	private readonly StrategyParam<bool> _longEntriesEnabled;
	private readonly StrategyParam<bool> _longExitsEnabled;
	private readonly StrategyParam<bool> _shortEntriesEnabled;
	private readonly StrategyParam<bool> _shortExitsEnabled;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;

	private readonly AdaptiveRenkoProcessor _longProcessor = new();
	private readonly AdaptiveRenkoProcessor _shortProcessor = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public AdaptiveRenkoDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Timeframe used to derive long-side signals", "Long Side");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Timeframe used to derive short-side signals", "Short Side");

		_longVolatilityMode = Param(nameof(LongVolatilityMode), AdaptiveRenkoVolatilityModes.AverageTrueRange)
			.SetDisplay("Long Volatility Source", "Volatility measure controlling long Renko brick size", "Long Side");

		_shortVolatilityMode = Param(nameof(ShortVolatilityMode), AdaptiveRenkoVolatilityModes.AverageTrueRange)
			.SetDisplay("Short Volatility Source", "Volatility measure controlling short Renko brick size", "Short Side");

		_longVolatilityPeriod = Param(nameof(LongVolatilityPeriod), 10)
			.SetRange(1, 500)
			.SetDisplay("Long Volatility Period", "Lookback period for the volatility calculation", "Long Side")
			.SetCanOptimize(true);

		_shortVolatilityPeriod = Param(nameof(ShortVolatilityPeriod), 10)
			.SetRange(1, 500)
			.SetDisplay("Short Volatility Period", "Lookback period for the volatility calculation", "Short Side")
			.SetCanOptimize(true);

		_longSensitivity = Param(nameof(LongSensitivity), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Long Sensitivity", "Multiplier applied to volatility for long bricks", "Long Side")
			.SetCanOptimize(true);

		_shortSensitivity = Param(nameof(ShortSensitivity), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Short Sensitivity", "Multiplier applied to volatility for short bricks", "Short Side")
			.SetCanOptimize(true);

		_longPriceMode = Param(nameof(LongPriceMode), AdaptiveRenkoPriceModes.Close)
			.SetDisplay("Long Price Mode", "Price source used when building long bricks", "Long Side");

		_shortPriceMode = Param(nameof(ShortPriceMode), AdaptiveRenkoPriceModes.Close)
			.SetDisplay("Short Price Mode", "Price source used when building short bricks", "Short Side");

		_longMinimumBrickPoints = Param(nameof(LongMinimumBrickPoints), 2m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Long Minimum Brick", "Minimal brick height in points for long bricks", "Long Side");

		_shortMinimumBrickPoints = Param(nameof(ShortMinimumBrickPoints), 2m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Short Minimum Brick", "Minimal brick height in points for short bricks", "Short Side");

		_longSignalBarOffset = Param(nameof(LongSignalBarOffset), 1)
			.SetRange(0, 10)
			.SetDisplay("Long Signal Offset", "Number of closed bars to delay long signals", "Long Side");

		_shortSignalBarOffset = Param(nameof(ShortSignalBarOffset), 1)
			.SetRange(0, 10)
			.SetDisplay("Short Signal Offset", "Number of closed bars to delay short signals", "Short Side");

		_longEntriesEnabled = Param(nameof(LongEntriesEnabled), true)
			.SetDisplay("Enable Long Entries", "Allow long-side market entries", "Long Side");

		_longExitsEnabled = Param(nameof(LongExitsEnabled), true)
			.SetDisplay("Enable Long Exits", "Allow long-side exits triggered by Renko", "Long Side");

		_shortEntriesEnabled = Param(nameof(ShortEntriesEnabled), true)
			.SetDisplay("Enable Short Entries", "Allow short-side market entries", "Short Side");

		_shortExitsEnabled = Param(nameof(ShortExitsEnabled), true)
			.SetDisplay("Enable Short Exits", "Allow short-side exits triggered by Renko", "Short Side");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Long Stop Loss", "Protective stop distance in points for long trades", "Risk");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Long Take Profit", "Profit target distance in points for long trades", "Risk");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Short Stop Loss", "Protective stop distance in points for short trades", "Risk");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Short Take Profit", "Profit target distance in points for short trades", "Risk");
	}

	/// <summary>
	/// Candle stream used to compute long-side Renko structures.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Candle stream used to compute short-side Renko structures.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Volatility mode for the long Renko stream.
	/// </summary>
	public AdaptiveRenkoVolatilityModes LongVolatilityMode
	{
		get => _longVolatilityMode.Value;
		set => _longVolatilityMode.Value = value;
	}

	/// <summary>
	/// Volatility mode for the short Renko stream.
	/// </summary>
	public AdaptiveRenkoVolatilityModes ShortVolatilityMode
	{
		get => _shortVolatilityMode.Value;
		set => _shortVolatilityMode.Value = value;
	}

	/// <summary>
	/// Lookback period for the long-side volatility indicator.
	/// </summary>
	public int LongVolatilityPeriod
	{
		get => _longVolatilityPeriod.Value;
		set => _longVolatilityPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for the short-side volatility indicator.
	/// </summary>
	public int ShortVolatilityPeriod
	{
		get => _shortVolatilityPeriod.Value;
		set => _shortVolatilityPeriod.Value = value;
	}

	/// <summary>
	/// Volatility multiplier that scales long-side bricks.
	/// </summary>
	public decimal LongSensitivity
	{
		get => _longSensitivity.Value;
		set => _longSensitivity.Value = value;
	}

	/// <summary>
	/// Volatility multiplier that scales short-side bricks.
	/// </summary>
	public decimal ShortSensitivity
	{
		get => _shortSensitivity.Value;
		set => _shortSensitivity.Value = value;
	}

	/// <summary>
	/// Price source used while building long bricks.
	/// </summary>
	public AdaptiveRenkoPriceModes LongPriceMode
	{
		get => _longPriceMode.Value;
		set => _longPriceMode.Value = value;
	}

	/// <summary>
	/// Price source used while building short bricks.
	/// </summary>
	public AdaptiveRenkoPriceModes ShortPriceMode
	{
		get => _shortPriceMode.Value;
		set => _shortPriceMode.Value = value;
	}

	/// <summary>
	/// Minimal brick height for the long Renko stream (expressed in points).
	/// </summary>
	public decimal LongMinimumBrickPoints
	{
		get => _longMinimumBrickPoints.Value;
		set => _longMinimumBrickPoints.Value = value;
	}

	/// <summary>
	/// Minimal brick height for the short Renko stream (expressed in points).
	/// </summary>
	public decimal ShortMinimumBrickPoints
	{
		get => _shortMinimumBrickPoints.Value;
		set => _shortMinimumBrickPoints.Value = value;
	}

	/// <summary>
	/// Number of closed bars to wait before using a long-side signal.
	/// </summary>
	public int LongSignalBarOffset
	{
		get => _longSignalBarOffset.Value;
		set => _longSignalBarOffset.Value = value;
	}

	/// <summary>
	/// Number of closed bars to wait before using a short-side signal.
	/// </summary>
	public int ShortSignalBarOffset
	{
		get => _shortSignalBarOffset.Value;
		set => _shortSignalBarOffset.Value = value;
	}

	/// <summary>
	/// Enables long-side entries.
	/// </summary>
	public bool LongEntriesEnabled
	{
		get => _longEntriesEnabled.Value;
		set => _longEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enables Renko-driven exits for long positions.
	/// </summary>
	public bool LongExitsEnabled
	{
		get => _longExitsEnabled.Value;
		set => _longExitsEnabled.Value = value;
	}

	/// <summary>
	/// Enables short-side entries.
	/// </summary>
	public bool ShortEntriesEnabled
	{
		get => _shortEntriesEnabled.Value;
		set => _shortEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enables Renko-driven exits for short positions.
	/// </summary>
	public bool ShortExitsEnabled
	{
		get => _shortExitsEnabled.Value;
		set => _shortExitsEnabled.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions expressed in indicator points.
	/// </summary>
	public decimal LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long positions expressed in indicator points.
	/// </summary>
	public decimal LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions expressed in indicator points.
	/// </summary>
	public decimal ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short positions expressed in indicator points.
	/// </summary>
	public decimal ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, LongCandleType);

		if (ShortCandleType != LongCandleType)
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longProcessor.Reset();
		_shortProcessor.Reset();
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longProcessor.Reset();
		_shortProcessor.Reset();
		_longEntryPrice = null;
		_shortEntryPrice = null;

		var longIndicator = CreateVolatilityIndicator(LongVolatilityMode, LongVolatilityPeriod);
		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription.BindEx(longIndicator, ProcessLongCandle);

		var shortIndicator = CreateVolatilityIndicator(ShortVolatilityMode, ShortVolatilityPeriod);

		if (ShortCandleType == LongCandleType)
		{
			longSubscription.BindEx(shortIndicator, ProcessShortCandle);
			longSubscription.Start();
		}
		else
		{
			longSubscription.Start();
			var shortSubscription = SubscribeCandles(ShortCandleType);
			shortSubscription.BindEx(shortIndicator, ProcessShortCandle);
			shortSubscription.Start();
		}
	}

	private void ProcessLongCandle(ICandleMessage candle, IIndicatorValue volatilityValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageLongRisk(candle);

		if (!volatilityValue.IsFinal)
			return;

		var step = GetPriceStep();
		var volatility = volatilityValue.ToDecimal();
		var snapshot = _longProcessor.Process(candle, volatility, LongSensitivity, LongMinimumBrickPoints, LongPriceMode, LongSignalBarOffset, step);

		if (snapshot == null)
			return;

		var signal = _longProcessor.GetSnapshot(LongSignalBarOffset);
		if (signal == null)
			return;

		if (LongExitsEnabled && Position > 0 && signal.Value.Trend == RenkoTrends.Down)
		{
			TryCloseLong("Adaptive Renko bearish reversal", candle);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (LongEntriesEnabled && signal.Value.Trend == RenkoTrends.Up)
		{
			TryOpenLong(candle, signal.Value);
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, IIndicatorValue volatilityValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageShortRisk(candle);

		if (!volatilityValue.IsFinal)
			return;

		var step = GetPriceStep();
		var volatility = volatilityValue.ToDecimal();
		var snapshot = _shortProcessor.Process(candle, volatility, ShortSensitivity, ShortMinimumBrickPoints, ShortPriceMode, ShortSignalBarOffset, step);

		if (snapshot == null)
			return;

		var signal = _shortProcessor.GetSnapshot(ShortSignalBarOffset);
		if (signal == null)
			return;

		if (ShortExitsEnabled && Position < 0 && signal.Value.Trend == RenkoTrends.Up)
		{
			TryCloseShort("Adaptive Renko bullish reversal", candle);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ShortEntriesEnabled && signal.Value.Trend == RenkoTrends.Down)
		{
			TryOpenShort(candle, signal.Value);
		}
	}

	private void ManageLongRisk(ICandleMessage candle)
	{
		if (Position <= 0)
		{
			_longEntryPrice = null;
			return;
		}

		if (_longEntryPrice == null)
			_longEntryPrice = candle.ClosePrice;

		var step = GetPriceStep();

		if (LongStopLossPoints > 0m)
		{
			var stopDistance = LongStopLossPoints * step;
			if (stopDistance > 0m && candle.LowPrice <= _longEntryPrice.Value - stopDistance)
			{
				TryCloseLong("Long stop loss reached", candle);
				return;
			}
		}

		if (LongTakeProfitPoints > 0m)
		{
			var targetDistance = LongTakeProfitPoints * step;
			if (targetDistance > 0m && candle.HighPrice >= _longEntryPrice.Value + targetDistance)
			{
				TryCloseLong("Long take profit reached", candle);
			}
		}
	}

	private void ManageShortRisk(ICandleMessage candle)
	{
		if (Position >= 0)
		{
			_shortEntryPrice = null;
			return;
		}

		if (_shortEntryPrice == null)
			_shortEntryPrice = candle.ClosePrice;

		var step = GetPriceStep();

		if (ShortStopLossPoints > 0m)
		{
			var stopDistance = ShortStopLossPoints * step;
			if (stopDistance > 0m && candle.HighPrice >= _shortEntryPrice.Value + stopDistance)
			{
				TryCloseShort("Short stop loss reached", candle);
				return;
			}
		}

		if (ShortTakeProfitPoints > 0m)
		{
			var targetDistance = ShortTakeProfitPoints * step;
			if (targetDistance > 0m && candle.LowPrice <= _shortEntryPrice.Value - targetDistance)
			{
				TryCloseShort("Short take profit reached", candle);
			}
		}
	}

	private void TryOpenLong(ICandleMessage candle, RenkoSnapshot signal)
	{
		if (Position > 0)
			return;

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
		{
			LogWarning("Volume must be positive to open a long position.");
			return;
		}

		BuyMarket(volume);
		_longEntryPrice = candle.ClosePrice;
		_shortEntryPrice = null;
		LogInfo($"Long entry triggered. Trend level: {signal.Support?.ToString("F5") ?? "n/a"}.");
	}

	private void TryOpenShort(ICandleMessage candle, RenkoSnapshot signal)
	{
		if (Position < 0)
			return;

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
		{
			LogWarning("Volume must be positive to open a short position.");
			return;
		}

		SellMarket(volume);
		_shortEntryPrice = candle.ClosePrice;
		_longEntryPrice = null;
		LogInfo($"Short entry triggered. Trend level: {signal.Resistance?.ToString("F5") ?? "n/a"}.");
	}

	private void TryCloseLong(string reason, ICandleMessage candle)
	{
		if (Position <= 0)
		{
			_longEntryPrice = null;
			return;
		}

		SellMarket(Math.Abs(Position));
		_longEntryPrice = null;
		LogInfo($"Long exit: {reason} at {candle.ClosePrice:F5}.");
	}

	private void TryCloseShort(string reason, ICandleMessage candle)
	{
		if (Position >= 0)
		{
			_shortEntryPrice = null;
			return;
		}

		BuyMarket(Math.Abs(Position));
		_shortEntryPrice = null;
		LogInfo($"Short exit: {reason} at {candle.ClosePrice:F5}.");
	}

	private static IIndicator CreateVolatilityIndicator(AdaptiveRenkoVolatilityModes mode, int period)
	{
		return mode switch
		{
			AdaptiveRenkoVolatilityModes.AverageTrueRange => new AverageTrueRange { Length = period },
			AdaptiveRenkoVolatilityModes.StandardDeviation => new StandardDeviation { Length = period },
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported volatility mode"),
		};
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 1m;

		if (security.PriceStep != null && security.PriceStep.Value > 0m)
			return security.PriceStep.Value;

		if (security.MinStep != null && security.MinStep.Value > 0m)
			return security.MinStep.Value;

		return 1m;
	}

	private enum RenkoTrends
	{
		None = 0,
		Up = 1,
		Down = -1
	}

	private readonly struct RenkoSnapshot
	{
		public RenkoSnapshot(DateTimeOffset time, RenkoTrends trend, decimal? support, decimal? resistance)
		{
			Time = time;
			Trend = trend;
			Support = support;
			Resistance = resistance;
		}

		public DateTimeOffset Time { get; }

		public RenkoTrends Trend { get; }

		public decimal? Support { get; }

		public decimal? Resistance { get; }
	}

	private sealed class AdaptiveRenkoProcessor
	{
		private readonly List<RenkoSnapshot> _history = new();
		private bool _initialized;
		private decimal _up;
		private decimal _down;
		private decimal _brick;
		private RenkoTrends _trend;

		public RenkoSnapshot? Process(ICandleMessage candle, decimal volatility, decimal sensitivity, decimal minimumBrickPoints, AdaptiveRenkoPriceModes priceMode, int signalOffset, decimal step)
		{
			var (high, low) = priceMode == AdaptiveRenkoPriceModes.Close
				? (candle.ClosePrice, candle.ClosePrice)
				: (candle.HighPrice, candle.LowPrice);

			var minBrick = Math.Max(minimumBrickPoints * step, 0m);

			if (!_initialized)
			{
				var range = Math.Max(high - low, 0m);
				var initialBrick = Math.Max(sensitivity * range, minBrick);

				_up = high;
				_down = low;
				_brick = initialBrick > 0m ? initialBrick : minBrick;
				_trend = RenkoTrends.None;
				_initialized = true;

				var initialSnapshot = new RenkoSnapshot(GetCandleTime(candle), RenkoTrends.None, null, null);
				AppendSnapshot(initialSnapshot, signalOffset);
				return initialSnapshot;
			}

			var up = _up;
			var down = _down;
			var brick = _brick > 0m ? _brick : minBrick;
			var trend = _trend;

			var adjustedBrick = Math.Max(sensitivity * Math.Abs(volatility), minBrick);
			if (adjustedBrick <= 0m)
				adjustedBrick = minBrick;

			if (brick <= 0m)
				brick = adjustedBrick > 0m ? adjustedBrick : minBrick;

			if (high > up + brick)
			{
				if (brick > 0m)
				{
					var diff = high - up;
					var bricks = Math.Floor(diff / brick);
					if (bricks < 1m)
						bricks = 1m;
					up += bricks * brick;
				}
				else
				{
					up = high;
				}

				brick = adjustedBrick;
				down = up - brick;
			}

			if (low < down - brick)
			{
				if (brick > 0m)
				{
					var diff = down - low;
					var bricks = Math.Floor(diff / brick);
					if (bricks < 1m)
						bricks = 1m;
					down -= bricks * brick;
				}
				else
				{
					down = low;
				}

				brick = adjustedBrick;
				up = down + brick;
			}

			if (_up < up)
				trend = RenkoTrends.Up;

			if (_down > down)
				trend = RenkoTrends.Down;

			_up = up;
			_down = down;
			_brick = brick;
			_trend = trend;

			var support = trend == RenkoTrends.Up ? down - brick : (decimal?)null;
			var resistance = trend == RenkoTrends.Down ? up + brick : (decimal?)null;

			var snapshot = new RenkoSnapshot(GetCandleTime(candle), trend, support, resistance);
			AppendSnapshot(snapshot, signalOffset);
			return snapshot;
		}

		public RenkoSnapshot? GetSnapshot(int shift)
		{
			if (shift < 0)
				shift = 0;

			var index = _history.Count - 1 - shift;
			if (index < 0)
				return null;

			return _history[index];
		}

		public void Reset()
		{
			_history.Clear();
			_initialized = false;
			_up = 0m;
			_down = 0m;
			_brick = 0m;
			_trend = RenkoTrends.None;
		}

		private void AppendSnapshot(RenkoSnapshot snapshot, int signalOffset)
		{
			_history.Add(snapshot);
			var maxHistory = Math.Max(signalOffset + 3, 8);
			var overflow = _history.Count - maxHistory;
			if (overflow > 0)
				_history.RemoveRange(0, overflow);
		}

		private static DateTimeOffset GetCandleTime(ICandleMessage candle)
		{
			if (candle.CloseTime != default)
				return candle.CloseTime;

			return candle.Time;
		}
	}

	public enum AdaptiveRenkoVolatilityModes
	{
		AverageTrueRange,
		StandardDeviation
	}

	public enum AdaptiveRenkoPriceModes
	{
		HighLow,
		Close
	}
}

