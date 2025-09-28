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
/// DreamBot strategy converted from the MetaTrader 4 expert advisor.
/// Uses Force Index momentum thresholds to detect bullish and bearish impulses.
/// Supports optional trailing stop management that mirrors the original EA behaviour.
/// </summary>
public class DreamBotStrategy : Strategy
{
	private readonly StrategyParam<int> _forcePeriod;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _bullsThreshold;
	private readonly StrategyParam<decimal> _bearsThreshold;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousForce;
	private decimal? _twoBarsBackForce;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingLevel;
	private decimal? _shortTrailingLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="DreamBotStrategy"/> class.
	/// </summary>
	public DreamBotStrategy()
	{
		_forcePeriod = Param(nameof(ForcePeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Force Period", "Smoothing length for the Force Index indicator.", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (points)", "Take profit distance expressed in MetaTrader points.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200, 700, 20);

		_stopLossPoints = Param(nameof(StopLossPoints), 520)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (points)", "Stop loss distance expressed in MetaTrader points.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200, 700, 20);

		_bullsThreshold = Param(nameof(BullsThreshold), 1.056m)
		.SetDisplay("Bulls Threshold", "Force Index level that indicates bullish pressure.", "Signals");

		_bearsThreshold = Param(nameof(BearsThreshold), -0.078m)
		.SetDisplay("Bears Threshold", "Force Index level that indicates bearish pressure.", "Signals");

		_enableTrailing = Param(nameof(EnableTrailing), false)
		.SetDisplay("Enable Trailing", "Enable DreamBot style trailing stop management.", "Trailing");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 290)
		.SetNotNegative()
		.SetDisplay("Trailing Distance (points)", "Distance maintained between price and trailing stop.", "Trailing")
		.SetCanOptimize(true)
		.SetOptimize(100, 400, 10);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 120)
		.SetNotNegative()
		.SetDisplay("Trailing Trigger (points)", "Minimum profit before the trailing stop activates.", "Trailing")
		.SetCanOptimize(true)
		.SetOptimize(60, 300, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for Force Index calculations.", "General");
	}

	/// <summary>
	/// Force Index smoothing length.
	/// </summary>
	public int ForcePeriod
	{
		get => _forcePeriod.Value;
		set => _forcePeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Force Index threshold that marks bullish pressure.
	/// </summary>
	public decimal BullsThreshold
	{
		get => _bullsThreshold.Value;
		set => _bullsThreshold.Value = value;
	}

	/// <summary>
	/// Force Index threshold that marks bearish pressure.
	/// </summary>
	public decimal BearsThreshold
	{
		get => _bearsThreshold.Value;
		set => _bearsThreshold.Value = value;
	}

	/// <summary>
	/// Enables DreamBot style trailing stop handling.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Distance kept between price and trailing stop once activated.
	/// </summary>
	public int TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Profit threshold required before the trailing stop activates.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
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

		_previousForce = null;
		_twoBarsBackForce = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingLevel = null;
		_shortTrailingLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStepPoints > TrailingStartPoints)
		{
			LogWarning("Trailing trigger distance should not exceed the trailing distance. Trailing will be disabled for safety.");
			EnableTrailing = false;
		}

		var force = new ForceIndex { Length = ForcePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(force, OnProcessCandle)
		.Start();

		var takeProfitDistance = PointsToDistance(TakeProfitPoints);
		var stopLossDistance = PointsToDistance(StopLossPoints);

		if (takeProfitDistance > 0m || stopLossDistance > 0m)
		{
			var takeProfitUnit = takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Absolute) : default;
			var stopLossUnit = stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Absolute) : default;
			StartProtection(takeProfitUnit, stopLossUnit, useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, force);
			DrawOwnTrades(area);
		}
	}

	private void OnProcessCandle(ICandleMessage candle, decimal forceValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StoreForceValue(forceValue);
			return;
		}

		if (!TryGetForceHistory(out var previousForce, out var olderForce))
		{
			StoreForceValue(forceValue);
			return;
		}

		if (previousForce > BullsThreshold && olderForce < BullsThreshold && Position == 0m)
		{
			BuyMarket(Volume);
		}
		else if (previousForce < BearsThreshold && olderForce > BearsThreshold && Position == 0m)
		{
			SellMarket(Volume);
		}

		StoreForceValue(forceValue);
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (!EnableTrailing || Position == 0m)
		return;

		var trailingDistance = PointsToDistance(TrailingStartPoints);
		var trailingTrigger = PointsToDistance(TrailingStepPoints);

		if (trailingDistance <= 0m || trailingTrigger <= 0m)
		return;

		if (Position > 0m && _longEntryPrice.HasValue)
		{
			var profit = candle.ClosePrice - _longEntryPrice.Value;
			if (profit > trailingTrigger)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (!_longTrailingLevel.HasValue || candidate > _longTrailingLevel.Value)
				_longTrailingLevel = candidate;
			}

			if (_longTrailingLevel.HasValue && candle.LowPrice <= _longTrailingLevel.Value)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0m && _shortEntryPrice.HasValue)
		{
			var profit = _shortEntryPrice.Value - candle.ClosePrice;
			if (profit > trailingTrigger)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (!_shortTrailingLevel.HasValue || candidate < _shortTrailingLevel.Value)
				_shortTrailingLevel = candidate;
			}

			if (_shortTrailingLevel.HasValue && candle.HighPrice >= _shortTrailingLevel.Value)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void StoreForceValue(decimal value)
	{
		_twoBarsBackForce = _previousForce;
		_previousForce = value;
	}

	private bool TryGetForceHistory(out decimal previousForce, out decimal olderForce)
	{
		if (_previousForce.HasValue && _twoBarsBackForce.HasValue)
		{
			previousForce = _previousForce.Value;
			olderForce = _twoBarsBackForce.Value;
			return true;
		}

		previousForce = default;
		olderForce = default;
		return false;
	}

	private decimal PointsToDistance(int points)
	{
		if (points <= 0)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		return points * step;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order is null)
		return;

		var tradePrice = trade.Trade.Price;
		var tradeVolume = trade.Trade.Volume;

		if (trade.Order.Side == Sides.Buy)
		{
			if (Position > 0m)
			{
				var totalVolume = Position;
				var previousVolume = Math.Max(0m, totalVolume - tradeVolume);
				var previousPrice = _longEntryPrice ?? tradePrice;
				_longEntryPrice = totalVolume > 0m
				? (previousPrice * previousVolume + tradePrice * tradeVolume) / totalVolume
				: tradePrice;
			}
			else if (Position <= 0m)
			{
				_shortEntryPrice = null;
				_shortTrailingLevel = null;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (Position < 0m)
			{
				var totalVolume = Math.Abs(Position);
				var previousVolume = Math.Max(0m, totalVolume - tradeVolume);
				var previousPrice = _shortEntryPrice ?? tradePrice;
				_shortEntryPrice = totalVolume > 0m
				? (previousPrice * previousVolume + tradePrice * tradeVolume) / totalVolume
				: tradePrice;
			}
			else if (Position >= 0m)
			{
				_longEntryPrice = null;
				_longTrailingLevel = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longTrailingLevel = null;
			_shortTrailingLevel = null;
		}
	}
}

