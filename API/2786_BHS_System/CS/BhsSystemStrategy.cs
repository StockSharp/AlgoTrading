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
/// Breakout strategy that places stop orders on rounded price levels guided by a Kaufman adaptive moving average.
/// </summary>
public class BhsSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossBuyPoints;
	private readonly StrategyParam<int> _stopLossSellPoints;
	private readonly StrategyParam<int> _trailingStopBuyPoints;
	private readonly StrategyParam<int> _trailingStopSellPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<int> _roundStepPoints;
	private readonly StrategyParam<decimal> _expirationHours;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousAma;
	private bool _hasPreviousAma;
	private decimal? _buyStopLevel;
	private decimal? _sellStopLevel;
	private DateTimeOffset? _buyOrderTime;
	private DateTimeOffset? _sellOrderTime;
	private decimal _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;

	/// <summary>
	/// Trade volume used for both entry and protective orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in points.
	/// </summary>
	public int StopLossBuyPoints
	{
		get => _stopLossBuyPoints.Value;
		set => _stopLossBuyPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in points.
	/// </summary>
	public int StopLossSellPoints
	{
		get => _stopLossSellPoints.Value;
		set => _stopLossSellPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points for long positions.
	/// </summary>
	public int TrailingStopBuyPoints
	{
		get => _trailingStopBuyPoints.Value;
		set => _trailingStopBuyPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points for short positions.
	/// </summary>
	public int TrailingStopSellPoints
	{
		get => _trailingStopSellPoints.Value;
		set => _trailingStopSellPoints.Value = value;
	}

	/// <summary>
	/// Minimum step in points between trailing stop updates.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Step used to build rounded trigger prices in points.
	/// </summary>
	public int RoundStepPoints
	{
		get => _roundStepPoints.Value;
		set => _roundStepPoints.Value = value;
	}

	/// <summary>
	/// Lifetime of pending entry orders in hours.
	/// </summary>
	public decimal ExpirationHours
	{
		get => _expirationHours.Value;
		set => _expirationHours.Value = value;
	}

	/// <summary>
	/// Main period of the adaptive moving average.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// Fast smoothing constant of the adaptive moving average.
	/// </summary>
	public int AmaFastPeriod
	{
		get => _amaFastPeriod.Value;
		set => _amaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing constant of the adaptive moving average.
	/// </summary>
	public int AmaSlowPeriod
	{
		get => _amaSlowPeriod.Value;
		set => _amaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BhsSystemStrategy"/> class.
	/// </summary>
	public BhsSystemStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Lot size used for entry orders", "Trading");

		_stopLossBuyPoints = Param(nameof(StopLossBuyPoints), 300)
			.SetNotNegative()
			.SetDisplay("Stop Loss Buy (points)", "Distance in points for long stop loss", "Risk");

		_stopLossSellPoints = Param(nameof(StopLossSellPoints), 300)
			.SetNotNegative()
			.SetDisplay("Stop Loss Sell (points)", "Distance in points for short stop loss", "Risk");

		_trailingStopBuyPoints = Param(nameof(TrailingStopBuyPoints), 100)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Buy (points)", "Trailing distance in points for long positions", "Risk");

		_trailingStopSellPoints = Param(nameof(TrailingStopSellPoints), 100)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Sell (points)", "Trailing distance in points for short positions", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10)
			.SetNotNegative()
			.SetDisplay("Trailing Step (points)", "Minimum step in points between trailing updates", "Risk");

		_roundStepPoints = Param(nameof(RoundStepPoints), 500)
			.SetGreaterThanZero()
			.SetDisplay("Round Step (points)", "Number of points used to build round price levels", "Execution");

		_expirationHours = Param(nameof(ExpirationHours), 1m)
			.SetNotNegative()
			.SetDisplay("Order Expiration (hours)", "Lifetime of pending entry orders in hours", "Execution");

		_amaLength = Param(nameof(AmaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "Adaptive moving average period", "Indicators");

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("AMA Fast Period", "Fast smoothing constant for AMA", "Indicators");

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("AMA Slow Period", "Slow smoothing constant for AMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for analysis", "General");
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

		_previousAma = 0m;
		_hasPreviousAma = false;
		_buyStopLevel = null;
		_sellStopLevel = null;
		_buyOrderTime = null;
		_sellOrderTime = null;
		_entryPrice = 0m;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Configure the adaptive moving average with user parameters.
		var ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaLength,
			FastSCPeriod = AmaFastPeriod,
			SlowSCPeriod = AmaSlowPeriod
		};

		// Subscribe to candle data and bind indicator updates.
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ama, ProcessCandle)
			.Start();

		// Draw price, indicator and trades if a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ama);
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, decimal amaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPreviousAma)
		{
			_previousAma = amaValue;
			_hasPreviousAma = true;
			return;
		}

		// Check protective stops
		CheckStopLoss(candle);
		CheckTrailingStop(candle);

		// Expire old pending levels
		CancelExpiredLevels();

		// Check if pending levels are triggered
		CheckPendingTriggers(candle);

		var price = candle.ClosePrice;
		var (_, priceCeil, priceFloor) = CalculateRoundLevels(price);

		// Track extremes for trailing
		if (Position > 0 && price > _highestSinceEntry)
			_highestSinceEntry = price;
		if (Position < 0 && (_lowestSinceEntry == 0 || price < _lowestSinceEntry))
			_lowestSinceEntry = price;

		var hasPendingLevels = _buyStopLevel.HasValue || _sellStopLevel.HasValue;

		if (Position == 0 && !hasPendingLevels)
		{
			if (price > _previousAma)
			{
				_buyStopLevel = priceCeil;
				_buyOrderTime = candle.OpenTime;
			}
			else if (price < _previousAma)
			{
				_sellStopLevel = priceFloor;
				_sellOrderTime = candle.OpenTime;
			}
		}

		_previousAma = amaValue;
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Trade == null) return;
		if (Position != 0m && _entryPrice == 0m)
		{
			_entryPrice = trade.Trade.Price;
			_highestSinceEntry = trade.Trade.Price;
			_lowestSinceEntry = trade.Trade.Price;
		}
		if (Position == 0m)
		{
			_entryPrice = 0m;
			_highestSinceEntry = 0m;
			_lowestSinceEntry = 0m;
		}
	}

	private void CheckStopLoss(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;

		if (Position > 0 && StopLossBuyPoints > 0)
		{
			var stopPrice = _entryPrice - StopLossBuyPoints * step;
			if (candle.LowPrice <= stopPrice)
			{
				SellMarket(Math.Abs(Position));
				return;
			}
		}

		if (Position < 0 && StopLossSellPoints > 0)
		{
			var stopPrice = _entryPrice + StopLossSellPoints * step;
			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void CheckTrailingStop(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;

		if (Position > 0 && TrailingStopBuyPoints > 0)
		{
			var trailingDist = TrailingStopBuyPoints * step;
			var trailingStep = TrailingStepPoints * step;
			var profit = _highestSinceEntry - _entryPrice;
			if (profit > trailingDist + trailingStep)
			{
				var trailStop = _highestSinceEntry - trailingDist;
				if (candle.LowPrice <= trailStop)
				{
					SellMarket(Math.Abs(Position));
					return;
				}
			}
		}

		if (Position < 0 && TrailingStopSellPoints > 0 && _lowestSinceEntry > 0)
		{
			var trailingDist = TrailingStopSellPoints * step;
			var trailingStep = TrailingStepPoints * step;
			var profit = _entryPrice - _lowestSinceEntry;
			if (profit > trailingDist + trailingStep)
			{
				var trailStop = _lowestSinceEntry + trailingDist;
				if (candle.HighPrice >= trailStop)
				{
					BuyMarket(Math.Abs(Position));
				}
			}
		}
	}

	private void CheckPendingTriggers(ICandleMessage candle)
	{
		if (_buyStopLevel.HasValue && Position <= 0 && candle.HighPrice >= _buyStopLevel.Value)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(OrderVolume);
			_buyStopLevel = null;
			_buyOrderTime = null;
			_sellStopLevel = null;
			_sellOrderTime = null;
		}

		if (_sellStopLevel.HasValue && Position >= 0 && candle.LowPrice <= _sellStopLevel.Value)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(OrderVolume);
			_sellStopLevel = null;
			_sellOrderTime = null;
			_buyStopLevel = null;
			_buyOrderTime = null;
		}
	}

	private void CancelExpiredLevels()
	{
		if (ExpirationHours <= 0m)
			return;

		var expiration = TimeSpan.FromHours((double)ExpirationHours);
		var now = CurrentTime;

		if (_buyOrderTime.HasValue && now - _buyOrderTime.Value >= expiration)
		{
			_buyStopLevel = null;
			_buyOrderTime = null;
		}

		if (_sellOrderTime.HasValue && now - _sellOrderTime.Value >= expiration)
		{
			_sellStopLevel = null;
			_sellOrderTime = null;
		}
	}

	private (decimal rounded, decimal ceil, decimal floor) CalculateRoundLevels(decimal price)
	{
		var point = Security?.PriceStep ?? 1m;
		var stepPoints = RoundStepPoints;

		if (point <= 0m || stepPoints <= 0)
			return (price, price, price);

		var step = stepPoints * point;
		if (step <= 0m)
			return (price, price, price);

		var ratio = price / step;
		var roundedIndex = decimal.Round(ratio, 0, MidpointRounding.AwayFromZero);
		var priceRound = roundedIndex * step;

		var ceilIndex = decimal.Ceiling((priceRound + step / 2m) / step);
		var floorIndex = decimal.Floor((priceRound - step / 2m) / step);

		var priceCeil = ceilIndex * step;
		var priceFloor = floorIndex * step;

		return (priceRound, priceCeil, priceFloor);
	}
}