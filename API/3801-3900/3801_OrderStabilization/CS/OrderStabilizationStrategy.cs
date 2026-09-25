using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Paired breakout triggers with stabilization-based exits.
/// </summary>
public class OrderStabilizationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _orderDistancePoints;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _absoluteFixation;
	private readonly StrategyParam<decimal> _stabilizationPoints;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _buyStop;
	private decimal? _sellStop;
	private DateTimeOffset? _createdAt;
	private decimal _entryPrice;
	private decimal _previousBody;
	private bool _hasPreviousBody;
	private DateTimeOffset? _entryCandleTime;

	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public decimal OrderDistancePoints { get => _orderDistancePoints.Value; set => _orderDistancePoints.Value = value; }
	public decimal ProfitThreshold { get => _profitThreshold.Value; set => _profitThreshold.Value = value; }
	public decimal AbsoluteFixation { get => _absoluteFixation.Value; set => _absoluteFixation.Value = value; }
	public decimal StabilizationPoints { get => _stabilizationPoints.Value; set => _stabilizationPoints.Value = value; }
	public int ExpirationMinutes { get => _expirationMinutes.Value; set => _expirationMinutes.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrderStabilizationStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero();
		_orderDistancePoints = Param(nameof(OrderDistancePoints), 20m).SetGreaterThanZero();
		_profitThreshold = Param(nameof(ProfitThreshold), -2m);
		_absoluteFixation = Param(nameof(AbsoluteFixation), 30m).SetNotNegative();
		_stabilizationPoints = Param(nameof(StabilizationPoints), 25m).SetGreaterThanZero();
		_expirationMinutes = Param(nameof(ExpirationMinutes), 20).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearPending();
		_entryPrice = 0m;
		_previousBody = 0m;
		_hasPreviousBody = false;
		_entryCandleTime = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var point = Security?.PriceStep ?? 1m;
		if (point <= 0m)
			point = 1m;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyLimit = StabilizationPoints * point;

		if (Position != 0 && (_entryCandleTime is null || candle.OpenTime > _entryCandleTime.Value))
		{
			var pnl = FloatingPnL(candle.ClosePrice, point);
			var oneSmall = body <= bodyLimit;
			var twoSmall = oneSmall && _hasPreviousBody && _previousBody <= bodyLimit;

			if ((oneSmall && pnl > ProfitThreshold) ||
				twoSmall ||
				(AbsoluteFixation > 0m && pnl >= AbsoluteFixation))
			{
				Flatten();
				ClearPending();
				_previousBody = body;
				_hasPreviousBody = true;
				return;
			}
		}

		if (_createdAt is DateTimeOffset created &&
			ExpirationMinutes > 0 &&
			candle.OpenTime - created >= TimeSpan.FromMinutes(ExpirationMinutes))
		{
			ClearPending();
			CreatePending(candle.ClosePrice, candle.OpenTime, point);
			_previousBody = body;
			_hasPreviousBody = true;
			return;
		}

		// Stops created from a finished candle can only be triggered by a later candle.
		if (_buyStop is null && _sellStop is null)
		{
			CreatePending(candle.ClosePrice, candle.OpenTime, point);
			_previousBody = body;
			_hasPreviousBody = true;
			return;
		}

		var hitBuy = _buyStop is decimal buy && candle.HighPrice >= buy;
		var hitSell = _sellStop is decimal sell && candle.LowPrice <= sell;

		if (hitBuy || hitSell)
		{
			var side = hitBuy && hitSell
				? (candle.ClosePrice >= candle.OpenPrice ? Sides.Buy : Sides.Sell)
				: hitBuy ? Sides.Buy : Sides.Sell;

			var trigger = side == Sides.Buy ? _buyStop!.Value : _sellStop!.Value;

			if (side == Sides.Buy)
			{
				BuyMarket(OrderVolume);
				_buyStop = null;
			}
			else
			{
				SellMarket(OrderVolume);
				_sellStop = null;
			}

			_entryPrice = trigger;
			_entryCandleTime = candle.OpenTime;
		}

		_previousBody = body;
		_hasPreviousBody = true;
	}

	private void CreatePending(decimal center, DateTimeOffset time, decimal point)
	{
		var distance = OrderDistancePoints * point;
		_buyStop = center + distance;
		_sellStop = center - distance;
		_createdAt = time;
	}

	private decimal FloatingPnL(decimal price, decimal point)
	{
		if (Position == 0m || _entryPrice == 0m)
			return 0m;

		var direction = Position > 0m ? 1m : -1m;
		var move = (price - _entryPrice) * direction;
		var stepPrice = Security?.StepPrice ?? 0m;

		return stepPrice > 0m
			? move / point * stepPrice * Math.Abs(Position)
			: move * (Security?.Multiplier ?? 1m) * Math.Abs(Position);
	}

	private void Flatten()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		_entryPrice = 0m;
		_entryCandleTime = null;
	}

	private void ClearPending()
	{
		_buyStop = null;
		_sellStop = null;
		_createdAt = null;
	}
}
