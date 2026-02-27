using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gap trading strategy. Detects price gaps between candles and trades the gap fill.
/// </summary>
public class GapsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _gapPercent;

	private decimal? _prevClose;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal GapPercent
	{
		get => _gapPercent.Value;
		set => _gapPercent.Value = value;
	}

	public GapsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_gapPercent = Param(nameof(GapPercent), 0.1m)
			.SetDisplay("Gap Percent", "Minimum gap size as percentage", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (_prevClose == null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var prevClose = _prevClose.Value;
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		_prevClose = close;

		if (prevClose == 0)
			return;

		var gapPct = (open - prevClose) / prevClose * 100;

		// Gap up detected - sell expecting gap fill
		if (gapPct > GapPercent && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Gap down detected - buy expecting gap fill
		else if (gapPct < -GapPercent && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Exit at mid when gap fills
		else if (Position > 0 && close > prevClose)
		{
			SellMarket();
		}
		else if (Position < 0 && close < prevClose)
		{
			BuyMarket();
		}
	}
}
