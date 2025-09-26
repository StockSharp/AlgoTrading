using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades bounces from a price channel with ATR-based protection and optional trailing stops.
/// The system opens a short position after failed breakouts at the channel high and goes long after failures at the channel low.
/// </summary>
public class TradeChannelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousChannelHigh;
	private decimal? _previousChannelLow;
	private decimal? _previousClose;
	private decimal? _stopPrice;
	private Order _stopOrder;
	private decimal _point;

	/// <summary>
	/// Trade volume for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Lookback period for calculating the price channel.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// ATR lookback used for stop calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Trailing distance in price steps.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Candle type to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TradeChannelStrategy"/>.
	/// </summary>
	public TradeChannelStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Trade volume", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetDisplay("Channel Period", "Lookback for highest/lowest channel levels", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 4)
			.SetDisplay("ATR Period", "Average True Range period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_trailingDistance = Param(nameof(TrailingDistance), 30m)
			.SetDisplay("Trailing Distance", "Trailing stop distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		_previousChannelHigh = null;
		_previousChannelLow = null;
		_previousClose = null;
		CancelStop();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security?.PriceStep ?? 1m;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(highest, lowest, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highestValue, IIndicatorValue lowestValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!highestValue.IsFinal || !lowestValue.IsFinal || !atrValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var channelHigh = highestValue.ToDecimal();
		var channelLow = lowestValue.ToDecimal();
		var atr = atrValue.ToDecimal();

		var prevHigh = _previousChannelHigh;
		var prevLow = _previousChannelLow;
		var prevClose = _previousClose;

		if (prevHigh is null || prevLow is null || prevClose is null)
		{
			_previousChannelHigh = channelHigh;
			_previousChannelLow = channelLow;
			_previousClose = candle.ClosePrice;
			return;
		}

		ManagePosition(candle, channelHigh, channelLow, prevHigh.Value, prevLow.Value);
		TryOpenPosition(candle, channelHigh, channelLow, atr, prevHigh.Value, prevLow.Value, prevClose.Value);

		_previousChannelHigh = channelHigh;
		_previousChannelLow = channelLow;
		_previousClose = candle.ClosePrice;
	}

	private void TryOpenPosition(ICandleMessage candle, decimal channelHigh, decimal channelLow, decimal atr, decimal prevHigh, decimal prevLow, decimal prevClose)
	{
		if (Position != 0m)
			return;

		var pivot = (channelHigh + channelLow + prevClose) / 3m;

		var shortSignal = channelHigh == prevHigh &&
			(candle.HighPrice >= channelHigh || (candle.ClosePrice < channelHigh && candle.ClosePrice > pivot));

		if (shortSignal)
		{
			var volume = Volume;
			if (volume > 0m)
			{
				SellMarket(volume);
				PlaceStop(Sides.Buy, channelHigh + atr, volume);
			}

			return;
		}

		var longSignal = channelLow == prevLow &&
			(candle.LowPrice <= channelLow || (candle.ClosePrice > channelLow && candle.ClosePrice < pivot));

		if (longSignal)
		{
			var volume = Volume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				PlaceStop(Sides.Sell, channelLow - atr, volume);
			}
		}
	}

	private void ManagePosition(ICandleMessage candle, decimal channelHigh, decimal channelLow, decimal prevHigh, decimal prevLow)
	{
		if (Position > 0m)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return;

			var exitLong = channelHigh == prevHigh && candle.HighPrice >= channelHigh;
			if (exitLong)
			{
				SellMarket(volume);
				CancelStop();
				return;
			}

			UpdateTrailingStop(true, candle.ClosePrice, volume);
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return;

			var exitShort = channelLow == prevLow && candle.LowPrice <= channelLow;
			if (exitShort)
			{
				BuyMarket(volume);
				CancelStop();
				return;
			}

			UpdateTrailingStop(false, candle.ClosePrice, volume);
		}
		else
		{
			CancelStop();
		}
	}

	private void UpdateTrailingStop(bool isLong, decimal closePrice, decimal volume)
	{
		if (TrailingDistance <= 0m || _point <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var offset = TrailingDistance * _point;

		if (isLong)
		{
			if (closePrice - entryPrice <= offset)
				return;

			var newStop = closePrice - offset;
			if (_stopPrice is decimal current && newStop <= current)
				return;

			PlaceStop(Sides.Sell, newStop, volume);
		}
		else
		{
			if (entryPrice - closePrice <= offset)
				return;

			var newStop = closePrice + offset;
			if (_stopPrice is decimal current && newStop >= current)
				return;

			PlaceStop(Sides.Buy, newStop, volume);
		}
	}

	private void PlaceStop(Sides side, decimal price, decimal volume)
	{
		CancelStop();

		if (volume <= 0m || price <= 0m)
			return;

		_stopOrder = side == Sides.Buy
			? BuyStop(volume, price)
			: SellStop(volume, price);

		_stopPrice = price;
	}

	private void CancelStop()
	{
		if (_stopOrder != null)
		{
			CancelOrder(_stopOrder);
			_stopOrder = null;
		}

		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			CancelStop();
	}
}
