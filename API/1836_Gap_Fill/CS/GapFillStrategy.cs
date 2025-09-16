using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gap fill strategy.
/// It sells when today's open is above yesterday's high by a predefined gap.
/// It buys when today's open is below yesterday's low by a predefined gap.
/// The strategy expects price to return to the previous candle extreme.
/// </summary>
public class GapFillStrategy : Strategy
{
	private readonly StrategyParam<int> _minGapSize;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;
	private DateTimeOffset _lastOrderTime;

	/// <summary>
	/// Minimum gap size in points (price steps).
	/// </summary>
	public int MinGapSize
	{
		get => _minGapSize.Value;
		set => _minGapSize.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="GapFillStrategy"/>.
	/// </summary>
	public GapFillStrategy()
	{
		_minGapSize = Param(nameof(MinGapSize), 1)
			.SetDisplay("Min Gap Size", "Minimum gap size in points", "Parameters");

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Data");
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
		_prevHigh = 0m;
		_prevLow = 0m;
		_hasPrev = false;
		_lastOrderTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var priceStep = Security.PriceStep ?? 1m;
		var spread = (Security.BestAsk?.Price - Security.BestBid?.Price) ?? 0m;
		var threshold = MinGapSize * priceStep + spread;

		// Check for gap up
		if (candle.OpenTime != _lastOrderTime && candle.OpenPrice > _prevHigh + threshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			BuyLimit(volume, _prevHigh + spread);
			_lastOrderTime = candle.OpenTime;
		}
		// Check for gap down
		else if (candle.OpenTime != _lastOrderTime && candle.OpenPrice < _prevLow - threshold && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			SellLimit(volume, _prevLow - spread);
			_lastOrderTime = candle.OpenTime;
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
