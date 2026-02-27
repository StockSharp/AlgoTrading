using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on candle direction changes, inspired by Renko-style logic.
/// Buys when candle direction flips from down to up, sells when it flips from up to down.
/// Uses ATR-based filter to only trade on significant candles.
/// </summary>
public class RenkoChartFromTicksStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;

	private bool? _prevUp;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public RenkoChartFromTicksStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period for significance filter", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		// Only consider candles with meaningful body (at least 0.3 * ATR)
		if (body < atrValue * 0.3m)
			return;

		var isUp = candle.ClosePrice > candle.OpenPrice;

		if (_prevUp.HasValue && _prevUp.Value != isUp)
		{
			if (isUp && Position <= 0)
				BuyMarket();
			else if (!isUp && Position >= 0)
				SellMarket();
		}

		_prevUp = isUp;
	}
}
