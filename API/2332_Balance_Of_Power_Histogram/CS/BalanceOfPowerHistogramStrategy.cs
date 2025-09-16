using System;
using System.Collections.Generic;

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

		_prev = _prevPrev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (candle.HighPrice == candle.LowPrice)
			return;

		var bop = (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice);

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
