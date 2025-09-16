using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the JK BullP AutoTrader strategy that trades using the Bulls Power indicator.
/// Sells when Bulls Power weakens above zero and buys when it drops below zero with trailing risk control.
/// </summary>
public class JkBullPowerAutoTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _bullsPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<DataType> _candleType;

	private BullsPower _bullsPower = null!;
	private decimal? _prevBulls;
	private decimal? _prevPrevBulls;

	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	private decimal _priceStep;

	/// <summary>
	/// Bulls Power indicator length.
	/// </summary>
	public int BullsPeriod
	{
		get => _bullsPeriod.Value;
		set => _bullsPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop step in price steps.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
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
	/// Initializes a new instance of the <see cref="JkBullPowerAutoTraderStrategy"/> class.
	/// </summary>
	public JkBullPowerAutoTraderStrategy()
	{
		_bullsPeriod = Param(nameof(BullsPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Bulls Power Period", "Length for Bulls Power indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 350m)
			.SetGreaterOrEqualTo(0m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 600m, 50m);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetGreaterOrEqualTo(0m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 300m, 25m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 100m)
			.SetGreaterOrEqualTo(0m)
			.SetDisplay("Trailing Stop (pts)", "Profit distance that activates trailing", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 40m)
			.SetGreaterOrEqualTo(0m)
			.SetDisplay("Trailing Step (pts)", "Minimal trailing increment", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevBulls = null;
		_prevPrevBulls = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPoints > 0m && TrailingStopPoints <= TrailingStepPoints)
		{
			AddErrorLog("Trailing stop must be greater than trailing step.");
			Stop();
			return;
		}

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_bullsPower = new BullsPower
		{
			Length = BullsPeriod
		};

		_prevBulls = null;
		_prevPrevBulls = null;
		_stopPrice = null;
		_takeProfitPrice = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bullsPower, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bullsPower);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(bullsValue);
			return;
		}

		CheckRisk(candle);

		if (!_bullsPower.IsFormed)
		{
			UpdateHistory(bullsValue);
			return;
		}

		if (_prevBulls is not decimal prevBulls || _prevPrevBulls is not decimal prevPrevBulls)
		{
			UpdateHistory(bullsValue);
			return;
		}

		var sellSignal = prevPrevBulls > prevBulls && prevBulls > 0m;
		var buySignal = prevBulls < 0m;

		if (sellSignal && Position >= 0)
		{
			// Close any existing long position and establish a short.
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				SellMarket(volume);
				InitializeTargets(false, candle.ClosePrice);
			}
		}
		else if (buySignal && Position <= 0)
		{
			// Close any existing short position and establish a long.
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			if (volume > 0)
			{
				BuyMarket(volume);
				InitializeTargets(true, candle.ClosePrice);
			}
		}

		UpdateHistory(bullsValue);
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (Position == 0 || TrailingStopPoints <= 0m)
			return;

		var trailingDistance = TrailingStopPoints * _priceStep;
		if (trailingDistance <= 0m)
			return;

		var trailingStep = TrailingStepPoints * _priceStep;

		if (Position > 0)
		{
			// Update trailing stop for long positions when profit exceeds the trigger distance.
			var profit = candle.ClosePrice - PositionAvgPrice;
			if (profit <= trailingDistance)
				return;

			var candidate = candle.ClosePrice - trailingDistance;
			if (!_stopPrice.HasValue || candidate > _stopPrice.Value && (trailingStep <= 0m || candidate - _stopPrice.Value >= trailingStep))
				_stopPrice = candidate;
		}
		else
		{
			// Update trailing stop for short positions when profit exceeds the trigger distance.
			var profit = PositionAvgPrice - candle.ClosePrice;
			if (profit <= trailingDistance)
				return;

			var candidate = candle.ClosePrice + trailingDistance;
			if (!_stopPrice.HasValue || candidate < _stopPrice.Value && (trailingStep <= 0m || _stopPrice.Value - candidate >= trailingStep))
				_stopPrice = candidate;
		}
	}

	private void CheckRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			// Manage long exits by stop loss, trailing stop, or take profit.
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetTargets();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetTargets();
			}
		}
		else if (Position < 0)
		{
			// Manage short exits by stop loss, trailing stop, or take profit.
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(-Position);
				ResetTargets();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(-Position);
				ResetTargets();
			}
		}
		else
		{
			ResetTargets();
		}
	}

	private void InitializeTargets(bool isLong, decimal entryPrice)
	{
		var stopDistance = StopLossPoints * _priceStep;
		var takeDistance = TakeProfitPoints * _priceStep;

		_stopPrice = stopDistance > 0m
			? isLong ? entryPrice - stopDistance : entryPrice + stopDistance
			: null;

		_takeProfitPrice = takeDistance > 0m
			? isLong ? entryPrice + takeDistance : entryPrice - takeDistance
			: null;
	}

	private void ResetTargets()
	{
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private void UpdateHistory(decimal bullsValue)
	{
		_prevPrevBulls = _prevBulls;
		_prevBulls = bullsValue;
	}
}
