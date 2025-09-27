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
/// Port of the MetaTrader expert advisor DonchianScalperEA.
/// The strategy prepares stop orders at Donchian channel boundaries after a pullback below the EMA.
/// Orders rely on Donchian middle/outer bands for context and can manage exits through fixed targets or trailing stops.
/// </summary>
public class DonchianScalperStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _crossAnchorPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ProfitTargetMode> _profitTargetMode;
	private readonly StrategyParam<TrailingProfitMode> _trailingMode;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _priceToleranceMultiplier;

	private DonchianChannels _donchian = null!;
	private ExponentialMovingAverage _ema = null!;
	private AverageTrueRange _atr = null!;

	private readonly List<Snapshot> _history = new(8);

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingShortStop;
	private decimal? _stopPrice;
	private bool _stopForLong;

	private decimal _pointSize;
	private decimal _lastAtr;
	private int _barsSinceExit = int.MaxValue;
	private decimal _previousPosition;

	/// <summary>
	/// Determines how the strategy manages profitable positions.
	/// </summary>
	public enum ProfitTargetMode
	{
		CloseAtProfit,
		Trailing,
	}

	/// <summary>
	/// Selects the trailing stop engine when <see cref="ProfitTargetMode.Trailing"/> is active.
	/// </summary>
	public enum TrailingProfitMode
	{
		DonchianBoundary,
		MovingAverage,
		AverageTrueRange,
	}

	/// <summary>
	/// Trading volume used for stop orders and market exits.
	/// </summary>
	public decimal VolumeParam
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Donchian channel length used for upper/lower boundaries.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Pullback distance (in price points) required before scheduling a breakout order.
	/// </summary>
	public decimal CrossAnchorPoints
	{
		get => _crossAnchorPoints.Value;
		set => _crossAnchorPoints.Value = value;
	}

	/// <summary>
	/// Additional distance added to the opposite Donchian band when computing stop-loss prices.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in points. Only used when <see cref="ProfitTargetMode.CloseAtProfit"/> is selected.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type feeding the indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Selected profit management style.
	/// </summary>
	public ProfitTargetMode ProfitMode
	{
		get => _profitTargetMode.Value;
		set => _profitTargetMode.Value = value;
	}

	/// <summary>
	/// Selected trailing engine when profit mode is set to <see cref="ProfitTargetMode.Trailing"/>.
	/// </summary>
	public TrailingProfitMode TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Minimum number of finished candles required after a flat position before new stop orders may be submitted.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// ATR period used by the trailing stop when <see cref="TrailingProfitMode.AverageTrueRange"/> is active.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR when trailing by volatility.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the instrument point size to derive price tolerance when adjusting orders.
	/// </summary>
	public decimal PriceToleranceMultiplier
	{
		get => _priceToleranceMultiplier.Value;
		set => _priceToleranceMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes the Donchian scalper strategy.
	/// </summary>
	public DonchianScalperStrategy()
	{
		_volume = Param(nameof(VolumeParam), 0.01m)
		.SetDisplay("Volume", "Order volume used for stop entries", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.5m, 0.01m);

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
		.SetDisplay("Channel Period", "Lookback length for the Donchian channel and EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_crossAnchorPoints = Param(nameof(CrossAnchorPoints), 0m)
		.SetDisplay("Cross Anchor", "Additional pullback depth required before a breakout order is armed", "Logic")
		.SetCanOptimize(true)
		.SetOptimize(0m, 100m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 80m)
		.SetDisplay("Stop Loss (points)", "Distance added to the opposite Donchian band for the initial protective stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 200m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 380m)
		.SetDisplay("Take Profit (points)", "Fixed profit distance used by the close-at-profit mode", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 600m, 20m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_profitTargetMode = Param(nameof(ProfitMode), ProfitTargetMode.CloseAtProfit)
		.SetDisplay("Profit Mode", "Close at a fixed profit or trail a stop", "Risk");

		_trailingMode = Param(nameof(TrailingMode), TrailingProfitMode.AverageTrueRange)
		.SetDisplay("Trailing Mode", "Trailing engine used when profit mode equals Trailing", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 3)
		.SetDisplay("Cooldown Bars", "Bars that must elapse after exiting before arming new orders", "Logic")
		.SetCanOptimize(true)
		.SetOptimize(0, 6, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetDisplay("ATR Period", "ATR lookback for volatility trailing", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.0m)
		.SetDisplay("ATR Multiplier", "Multiplier applied to the ATR based trailing stop", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_priceToleranceMultiplier = Param(nameof(PriceToleranceMultiplier), 0.5m)
			.SetDisplay("Price Tolerance Multiplier", "Multiplier applied to point size when reconciling order prices", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);
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

		_history.Clear();
		_buyStopOrder = null;
		_sellStopOrder = null;
		ResetStop();
		_pendingLongStop = null;
		_pendingShortStop = null;
		_stopPrice = null;
		_stopForLong = default;
		_lastAtr = 0m;
		_barsSinceExit = int.MaxValue;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security.Step ?? 0.0001m;
		Volume = VolumeParam;

		_donchian = new DonchianChannels
		{
			Length = ChannelPeriod,
		};

		_ema = new ExponentialMovingAverage
		{
			Length = ChannelPeriod,
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_donchian, _ema, _atr, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (donchianValue is not DonchianChannelsValue dcValue)
		return;

		if (dcValue.UpperBand is not decimal upper ||
		dcValue.LowerBand is not decimal lower ||
		dcValue.Middle is not decimal middle)
		{
			return;
		}

		if (!emaValue.IsFinal || !atrValue.IsFinal)
		return;

		var ema = emaValue.GetValue<decimal>();
		_lastAtr = atrValue.GetValue<decimal>();

		var snapshot = new Snapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, upper, lower, middle, ema);
		_history.Add(snapshot);

		if (_history.Count > 6)
		_history.RemoveAt(0);

		UpdateCooldown();
		MaintainPendingOrders(upper, lower);
		HandleEntries(upper, lower, middle);
		ManageProfits(candle, upper, lower, middle, ema);
	}

	private void UpdateCooldown()
	{
		if (_barsSinceExit < int.MaxValue)
		_barsSinceExit++;

		var currentPosition = Position;
		if (_previousPosition != 0m && currentPosition == 0m)
		{
			_barsSinceExit = 0;
		}

		_previousPosition = currentPosition;
	}

	private void MaintainPendingOrders(decimal currentUpper, decimal currentLower)
	{
		var tolerance = _pointSize * PriceToleranceMultiplier;

		if (_buyStopOrder != null && _history.Count >= 3)
		{
			var prev1 = _history[^2];
			var prev2 = _history[^3];
			var upperStable = Math.Abs(prev1.Upper - prev2.Upper) <= tolerance && Math.Abs(currentUpper - prev1.Upper) <= tolerance;

			if (upperStable && _buyStopOrder.Price.HasValue && Math.Abs(_buyStopOrder.Price.Value - currentUpper) > tolerance)
			{
				PlaceBuyStopOrder(currentUpper, currentLower);
			}
		}

		if (_sellStopOrder != null && _history.Count >= 3)
		{
			var prev1 = _history[^2];
			var prev2 = _history[^3];
			var lowerStable = Math.Abs(prev1.Lower - prev2.Lower) <= tolerance && Math.Abs(currentLower - prev1.Lower) <= tolerance;

			if (lowerStable && _sellStopOrder.Price.HasValue && Math.Abs(_sellStopOrder.Price.Value - currentLower) > tolerance)
			{
				PlaceSellStopOrder(currentLower, currentUpper);
			}
		}
	}

	private void HandleEntries(decimal upper, decimal lower, decimal middle)
	{
		if (_history.Count < 3)
		return;

		var anchor = CrossAnchorPoints * _pointSize;
		var allowEntry = _barsSinceExit >= CooldownBars || _barsSinceExit == int.MaxValue;

		if (allowEntry && Position <= 0m && _buyStopOrder == null && HasPullback(true, anchor))
		{
			PlaceBuyStopOrder(upper, lower);
		}

		if (allowEntry && Position >= 0m && _sellStopOrder == null && HasPullback(false, anchor))
		{
			PlaceSellStopOrder(lower, upper);
		}
	}

	private bool HasPullback(bool forLong, decimal anchor)
	{
		var tolerance = _pointSize * PriceToleranceMultiplier;
		var relevant = new[] { _history[^2], _history[^3] };

		foreach (var snapshot in relevant)
		{
			if (forLong)
			{
				var level1 = snapshot.Middle - anchor;
				var level2 = snapshot.Lower - anchor;

				if (CrossedFromAbove(snapshot, level1, tolerance) || CrossedFromAbove(snapshot, level2, tolerance))
				return true;
			}
			else
			{
				var level1 = snapshot.Middle + anchor;
				var level2 = snapshot.Upper + anchor;

				if (CrossedFromBelow(snapshot, level1, tolerance) || CrossedFromBelow(snapshot, level2, tolerance))
				return true;
			}
		}

		return false;
	}

	private void PlaceBuyStopOrder(decimal entryPrice, decimal lowerBand)
	{
		CancelOrderIfActive(_buyStopOrder);

		var alignedPrice = AlignPrice(entryPrice, true);
		if (alignedPrice <= 0m || VolumeParam <= 0m)
		return;

		var stopPrice = CalculateLongStop(lowerBand);
		_buyStopOrder = BuyStop(VolumeParam, alignedPrice);
		_pendingLongStop = stopPrice;

		LogInfo("Arm long breakout at {0} with stop {1}", alignedPrice, stopPrice);
	}

	private void PlaceSellStopOrder(decimal entryPrice, decimal upperBand)
	{
		CancelOrderIfActive(_sellStopOrder);

		var alignedPrice = AlignPrice(entryPrice, false);
		if (alignedPrice <= 0m || VolumeParam <= 0m)
		return;

		var stopPrice = CalculateShortStop(upperBand);
		_sellStopOrder = SellStop(VolumeParam, alignedPrice);
		_pendingShortStop = stopPrice;

		LogInfo("Arm short breakout at {0} with stop {1}", alignedPrice, stopPrice);
	}

	private decimal? CalculateLongStop(decimal lowerBand)
	{
		var basePrice = StopLossPoints <= 0m ? lowerBand : lowerBand + StopLossPoints * _pointSize;
		var aligned = AlignPrice(basePrice, false);
		return aligned > 0m ? aligned : null;
	}

	private decimal? CalculateShortStop(decimal upperBand)
	{
		var basePrice = StopLossPoints <= 0m ? upperBand : upperBand - StopLossPoints * _pointSize;
		var aligned = AlignPrice(basePrice, true);
		return aligned > 0m ? aligned : null;
	}

	private void ManageProfits(ICandleMessage candle, decimal upper, decimal lower, decimal middle, decimal ema)
	{
		EnsureProtectiveStop();

		if (Position == 0m)
		{
			ResetStop();
			return;
		}

		if (ProfitMode == ProfitTargetMode.CloseAtProfit)
		{
			var distance = TakeProfitPoints * _pointSize;
			if (distance <= 0m)
			return;

			if (Position > 0m && candle.ClosePrice - PositionPrice >= distance)
			{
				SellMarket(Position);
				LogInfo("Close long at profit distance {0}", distance);
			}
			else if (Position < 0m && PositionPrice - candle.ClosePrice >= distance)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo("Close short at profit distance {0}", distance);
			}
		}
		else
		{
			ApplyTrailing(candle, upper, lower, middle, ema);
		}
	}

	private void EnsureProtectiveStop()
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		{
			_pendingLongStop = null;
			_pendingShortStop = null;
			return;
		}

		if (_stopOrder != null)
		return;

		if (Position > 0m && _pendingLongStop is decimal longStop)
		{
			var stop = AlignPrice(longStop, false);
			PlaceProtectiveStop(Sides.Sell, stop, positionVolume, true);
			_pendingLongStop = null;
		}
		else if (Position < 0m && _pendingShortStop is decimal shortStop)
		{
			var stop = AlignPrice(shortStop, true);
			PlaceProtectiveStop(Sides.Buy, stop, positionVolume, false);
			_pendingShortStop = null;
		}
	}

	private void ApplyTrailing(ICandleMessage candle, decimal upper, decimal lower, decimal middle, decimal ema)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		{
			ResetStop();
			return;
		}

		var tolerance = _pointSize * PriceToleranceMultiplier;

		if (Position > 0m)
		{
			var target = TrailingMode switch
			{
				TrailingProfitMode.DonchianBoundary => CalculateLongStop(lower),
				TrailingProfitMode.MovingAverage => AlignPrice(ema, false),
				TrailingProfitMode.AverageTrueRange => AlignPrice(candle.ClosePrice - _lastAtr * AtrMultiplier, false),
				_ => null,
			};

			if (target is decimal price && price > 0m && candle.ClosePrice - price > tolerance)
			{
				MoveStop(price, positionVolume, true);
			}
		}
		else
		{
			var target = TrailingMode switch
			{
				TrailingProfitMode.DonchianBoundary => CalculateShortStop(upper),
				TrailingProfitMode.MovingAverage => AlignPrice(ema, true),
				TrailingProfitMode.AverageTrueRange => AlignPrice(candle.ClosePrice + _lastAtr * AtrMultiplier, true),
				_ => null,
			};

			if (target is decimal price && price > 0m && price - candle.ClosePrice > tolerance)
			{
				MoveStop(price, positionVolume, false);
			}
		}
	}

	private void MoveStop(decimal targetPrice, decimal volume, bool forLong)
	{
		if (_stopOrder != null && _stopPrice is decimal currentPrice)
		{
			if (forLong && targetPrice <= currentPrice + _pointSize * PriceToleranceMultiplier)
			return;

			if (!forLong && targetPrice >= currentPrice - _pointSize * PriceToleranceMultiplier)
			return;
		}

		PlaceProtectiveStop(forLong ? Sides.Sell : Sides.Buy, targetPrice, volume, forLong);
	}

	private void PlaceProtectiveStop(Sides side, decimal price, decimal volume, bool forLong)
	{
		if (price <= 0m || volume <= 0m)
		return;

		CancelOrderIfActive(_stopOrder);

		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_stopPrice = price;
		_stopForLong = forLong;
	}

	private void ResetStop()
	{
		if (_stopOrder != null)
		{
			CancelOrderIfActive(_stopOrder);
			_stopOrder = null;
		}

		_stopPrice = null;
	}

	private static bool CrossedFromAbove(in Snapshot snapshot, decimal level, decimal tolerance)
	{
		var upperCondition = snapshot.Open >= level - tolerance || snapshot.Low >= level - tolerance;
		var lowerCondition = snapshot.Close <= level + tolerance || snapshot.High <= level + tolerance;
		return upperCondition && lowerCondition;
	}

	private static bool CrossedFromBelow(in Snapshot snapshot, decimal level, decimal tolerance)
	{
		var lowerCondition = snapshot.Open <= level + tolerance || snapshot.High <= level + tolerance;
		var upperCondition = snapshot.Close >= level - tolerance || snapshot.Low <= level - tolerance;
		return lowerCondition && upperCondition;
	}

	private decimal AlignPrice(decimal price, bool up)
	{
		if (price <= 0m || _pointSize <= 0m)
		return price;

		var steps = price / _pointSize;
		var alignedSteps = up ? Math.Ceiling(steps) : Math.Floor(steps);
		return alignedSteps * _pointSize;
	}

	private void CancelOrderIfActive(Order order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active or OrderStates.Suspended)
		{
			CancelOrder(order);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == _buyStopOrder && IsFinal(order.State))
		{
			_buyStopOrder = null;
		}
		else if (order == _sellStopOrder && IsFinal(order.State))
		{
			_sellStopOrder = null;
		}
		else if (order == _stopOrder && IsFinal(order.State))
		{
			_stopOrder = null;
			_stopPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order == null)
		return;

		if (trade.Order == _buyStopOrder)
		{
			_buyStopOrder = null;
			CancelOrderIfActive(_sellStopOrder);
			_sellStopOrder = null;
		}
		else if (trade.Order == _sellStopOrder)
		{
			_sellStopOrder = null;
			CancelOrderIfActive(_buyStopOrder);
			_buyStopOrder = null;
		}

		EnsureProtectiveStop();
	}

	private static bool IsFinal(OrderStates state)
	{
		return state is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled or OrderStates.Stopped or OrderStates.Rejected;
	}

	private readonly record struct Snapshot(decimal Open, decimal High, decimal Low, decimal Close, decimal Upper, decimal Lower, decimal Middle, decimal Ema);
}

