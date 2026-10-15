using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian strategy based on moving average slope.
/// Buys when SMA decreases for two consecutive periods, sells when it increases.
/// </summary>
public class MultikSmaExpStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _ma0;
	private decimal? _ma1;
	private decimal? _ma2;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultikSmaExpStrategy()
	{
		_period = Param(nameof(Period), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the moving average", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_ma0 = _ma1 = _ma2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma0 = _ma1 = _ma2 = null;

		var sma = new ExponentialMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_ma2 = _ma1;
		_ma1 = _ma0;
		_ma0 = smaValue;

		if (_ma2 is null || _ma1 is null)
			return;

		var dsma1 = _ma0.Value - _ma1.Value;
		var dsma2 = _ma1.Value - _ma2.Value;

		// Two consecutive decreases -> contrarian buy
		if (dsma2 < 0 && dsma1 < 0 && Position <= 0)
			BuyMarket();
		// Two consecutive increases -> contrarian sell
		else if (dsma2 > 0 && dsma1 > 0 && Position >= 0)
			SellMarket();
	}
}
