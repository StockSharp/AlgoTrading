using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters when price moves away from a moving average by
/// a configurable distance, expecting mean reversion.
/// </summary>
public class LastPriceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _distancePct;

	private decimal _entryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal DistancePct
	{
		get => _distancePct.Value;
		set => _distancePct.Value = value;
	}

	public LastPriceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Moving average period", "Parameters");

		_distancePct = Param(nameof(DistancePct), 0.5m)
			.SetDisplay("Distance %", "Percent distance from MA to trigger entry", "Parameters");
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
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var ma = new ExponentialMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var threshold = maValue * DistancePct / 100m;

		// Exit: price returned to MA
		if (Position > 0 && price >= maValue)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && price <= maValue)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// Entry: price moved away from MA
		if (Position == 0)
		{
			if (price < maValue - threshold)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (price > maValue + threshold)
			{
				SellMarket();
				_entryPrice = price;
			}
		}
	}
}
