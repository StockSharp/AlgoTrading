using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Support and resistance breakout strategy with EMA trend filter and staged trailing stop.
/// Reproduces the behaviour of the original MQL advisor that buys above resistance during bullish trends
/// and sells below support during bearish trends.
/// </summary>
public class SupportResistanceBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian = null!;
	private ExponentialMovingAverage _ema = null!;

	private decimal _support;
	private decimal _resistance;
	private TrendDirection _trend;

	private decimal _point;
	private Order _stopOrder;
	private decimal? _stopPrice;
	private decimal? _entryPrice;
	private int _trailingStage;

	private enum TrendDirection
	{
		None,
		Bullish,
		Bearish
	}

	/// <summary>
	/// Number of candles used to compute support and resistance.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// EMA length used as the trend filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="SupportResistanceBreakoutStrategy"/> class.
	/// </summary>
	public SupportResistanceBreakoutStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 55)
		.SetGreaterThanZero()
		.SetDisplay("Range Length", "Candles used to form support/resistance", "General")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 5);

		_emaPeriod = Param(nameof(EmaPeriod), 500)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of the EMA trend filter", "General")
		.SetCanOptimize(true)
		.SetOptimize(100, 800, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");
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

		_support = 0m;
		_resistance = 0m;
		_trend = TrendDirection.None;
		_point = 0m;
		_stopOrder = null;
		_stopPrice = null;
		_entryPrice = null;
		_trailingStage = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security?.PriceStep ?? 1m;

		_donchian = new DonchianChannels { Length = RangeLength };
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(candle => ProcessCandle(candle, _donchian, _ema))
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _ema);
		}
	}

	private void ProcessCandle(ICandleMessage candle, DonchianChannels donchian, ExponentialMovingAverage ema)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Update support/resistance range and the EMA trend filter.
		var channelValue = donchian.Process(candle);
		var emaValue = ema.Process(new DecimalIndicatorValue(ema, candle.OpenPrice, candle.OpenTime));

		if (!donchian.IsFormed || !ema.IsFormed)
		return;

		var channels = (DonchianChannelsValue)channelValue;

		if (channels.UpperBand is not decimal upper || channels.LowerBand is not decimal lower)
		return;

		_support = lower;
		_resistance = upper;

		var emaDecimal = emaValue.ToDecimal();

		_trend = TrendDirection.None;
		if (candle.OpenPrice > emaDecimal)
		_trend = TrendDirection.Bullish;
		else if (candle.OpenPrice < emaDecimal)
		_trend = TrendDirection.Bearish;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		HandleEntries(candle, canTrade);
		HandleExits(candle, canTrade);
		UpdateTrailing(candle, canTrade);

		if (Position == 0)
		ResetPositionState();
	}

	private void HandleEntries(ICandleMessage candle, bool canTrade)
	{
		if (!canTrade)
		return;

		// Buy the breakout when the market is trending higher.
		if (_trend == TrendDirection.Bullish && Position <= 0 && candle.ClosePrice > _resistance)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				CancelStop();
				BuyMarket(volume);
			}
		}
		// Sell the breakdown when the market is trending lower.
		else if (_trend == TrendDirection.Bearish && Position >= 0 && candle.ClosePrice < _support)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				CancelStop();
				SellMarket(volume);
			}
		}
	}

	private void HandleExits(ICandleMessage candle, bool canTrade)
	{
		if (!canTrade)
		return;

		// Close longs if price falls back below the refreshed support while in profit.
		if (Position > 0 && _entryPrice is decimal entryLong)
		{
			var profit = candle.ClosePrice - entryLong;
			if (profit > 0m && candle.ClosePrice < _support)
			{
				CancelStop();
				SellMarket(Position);
			}
		}
		// Close shorts if price rises back above the refreshed resistance while in profit.
		else if (Position < 0 && _entryPrice is decimal entryShort)
		{
			var profit = entryShort - candle.ClosePrice;
			if (profit > 0m && candle.ClosePrice > _resistance)
			{
				CancelStop();
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void UpdateTrailing(ICandleMessage candle, bool canTrade)
	{
		if (!canTrade)
		return;

		if (_entryPrice is null || _point <= 0m)
		return;

		// Apply the three-step trailing stop that locks in 10/20/30 points of profit.
		if (Position > 0)
		{
			ApplyLongTrailing(candle.ClosePrice);
		}
		else if (Position < 0)
		{
			ApplyShortTrailing(candle.ClosePrice);
		}
	}

	private void ApplyLongTrailing(decimal price)
	{
		if (_entryPrice is not decimal entry)
		return;

		var thresholds = new[] { 20m, 40m, 60m };
		var offsets = new[] { 10m, 20m, 30m };

		for (var i = 0; i < thresholds.Length; i++)
		{
			if (_trailingStage >= i + 1)
			continue;

			var target = entry + thresholds[i] * _point;
			if (price > target)
			{
				var newStop = entry + offsets[i] * _point;
				if (_stopPrice is null || newStop > _stopPrice.Value)
				{
					MoveStop(Sides.Sell, newStop);
					_trailingStage = i + 1;
				}
			}
		}
	}

	private void ApplyShortTrailing(decimal price)
	{
		if (_entryPrice is not decimal entry)
		return;

		var thresholds = new[] { 20m, 40m, 60m };
		var offsets = new[] { 10m, 20m, 30m };

		for (var i = 0; i < thresholds.Length; i++)
		{
			if (_trailingStage >= i + 1)
			continue;

			var target = entry - thresholds[i] * _point;
			if (price < target)
			{
				var newStop = entry - offsets[i] * _point;
				if (_stopPrice is null || newStop < _stopPrice.Value)
				{
					MoveStop(Sides.Buy, newStop);
					_trailingStage = i + 1;
				}
			}
		}
	}

	private void MoveStop(Sides side, decimal price)
	{
		CancelStop();

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_stopPrice = price;
	}

	private void CancelStop()
	{
		if (_stopOrder != null)
		{
			CancelOrder(_stopOrder);
			_stopOrder = null;
		}
	}

	private void PlaceInitialStopForLong()
	{
		if (_entryPrice is not decimal entry || Position <= 0)
		return;

		if (_support <= 0m || _support >= entry)
		return;

		MoveStop(Sides.Sell, _support);
	}

	private void PlaceInitialStopForShort()
	{
		if (_entryPrice is not decimal entry || Position >= 0)
		return;

		if (_resistance <= 0m || _resistance <= entry)
		return;

		MoveStop(Sides.Buy, _resistance);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_trailingStage = 0;
		CancelStop();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
		return;

		var tradeVolume = trade.Trade.Volume;
		if (tradeVolume <= 0m)
		return;

		var delta = trade.Order.Side == Sides.Buy ? tradeVolume : -tradeVolume;
		var previousPosition = Position - delta;

		if (Position > 0)
		{
			if (trade.Order.Side == Sides.Buy)
			{
				if (previousPosition <= 0m)
				{
					_entryPrice = trade.Trade.Price;
				}
				else if (_entryPrice is decimal existing && Math.Abs(previousPosition) > 0m)
				{
					var currentVolume = Math.Abs(Position);
					_entryPrice = (existing * Math.Abs(previousPosition) + trade.Trade.Price * tradeVolume) / currentVolume;
				}
				else
				{
					_entryPrice = trade.Trade.Price;
				}

				_trailingStage = 0;
				_stopPrice = null;
				PlaceInitialStopForLong();
			}
		}
		else if (Position < 0)
		{
			if (trade.Order.Side == Sides.Sell)
			{
				if (previousPosition >= 0m)
				{
					_entryPrice = trade.Trade.Price;
				}
				else if (_entryPrice is decimal existing && Math.Abs(previousPosition) > 0m)
				{
					var currentVolume = Math.Abs(Position);
					_entryPrice = (existing * Math.Abs(previousPosition) + trade.Trade.Price * tradeVolume) / currentVolume;
				}
				else
				{
					_entryPrice = trade.Trade.Price;
				}

				_trailingStage = 0;
				_stopPrice = null;
				PlaceInitialStopForShort();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		ResetPositionState();
	}
}
