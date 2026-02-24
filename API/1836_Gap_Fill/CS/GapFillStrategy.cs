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
/// Gap fill strategy.
/// It sells when today's open is above yesterday's high by a predefined gap.
/// It buys when today's open is below yesterday's low by a predefined gap.
/// The strategy expects price to return to the previous candle extreme.
/// </summary>
public class GapFillStrategy : Strategy
{
	private readonly StrategyParam<int> _minGapSize;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;
	private decimal _targetPrice;

	/// <summary>
	/// Minimum gap size in points (price steps).
	/// </summary>
	public int MinGapSize
	{
		get => _minGapSize.Value;
		set => _minGapSize.Value = value;
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

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var priceStep = Security.PriceStep ?? 1m;
		var threshold = MinGapSize * priceStep;

		// Check if position target reached
		if (Position > 0 && _targetPrice > 0 && candle.ClosePrice >= _targetPrice)
		{
			SellMarket();
			_targetPrice = 0;
		}
		else if (Position < 0 && _targetPrice > 0 && candle.ClosePrice <= _targetPrice)
		{
			BuyMarket();
			_targetPrice = 0;
		}

		// Check for gap up - sell expecting fill back to previous high
		if (Position == 0 && candle.OpenPrice > _prevHigh + threshold)
		{
			SellMarket();
			_targetPrice = _prevHigh;
		}
		// Check for gap down - buy expecting fill back to previous low
		else if (Position == 0 && candle.OpenPrice < _prevLow - threshold)
		{
			BuyMarket();
			_targetPrice = _prevLow;
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
