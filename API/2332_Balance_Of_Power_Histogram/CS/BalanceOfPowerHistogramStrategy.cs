using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Balance of Power Histogram strategy that reacts on direction changes
/// of the Balance of Power indicator.
/// </summary>
public class BalanceOfPowerHistogramStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev;
	private decimal? _prevPrev;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BalanceOfPowerHistogramStrategy"/>.
	/// </summary>
	public BalanceOfPowerHistogramStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_prev = _prevPrev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev = null;
		_prevPrev = null;

		var sma = new SimpleMovingAverage { Length = 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _unused)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.HighPrice == candle.LowPrice)
			return;

		var bop = (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrev = _prev;
			_prev = bop;
			return;
		}

		if (_prev is decimal prev && _prevPrev is decimal prevPrev)
		{
			var turnedUp = prev < prevPrev && bop > prev;
			var turnedDown = prev > prevPrev && bop < prev;

			if (turnedUp && Position <= 0)
				BuyMarket();
			else if (turnedDown && Position >= 0)
				SellMarket();
		}

		_prevPrev = _prev;
		_prev = bop;
	}
}