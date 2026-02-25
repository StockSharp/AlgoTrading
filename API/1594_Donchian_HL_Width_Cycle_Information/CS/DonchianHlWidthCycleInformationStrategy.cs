using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian channel cycle strategy.
/// Tracks Donchian channel breakouts and cycles between upper and lower bands.
/// </summary>
public class DonchianHlWidthCycleInformationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private int _cycleTrend;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DonchianHlWidthCycleInformationStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Donchian Length", "Lookback for Donchian channel", "Donchian");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cycleTrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (upper == lower)
			return;

		// Cycle detection: breakout above upper band or below lower band
		if (candle.ClosePrice >= upper && _cycleTrend != 1)
		{
			_cycleTrend = 1;
			if (Position <= 0)
				BuyMarket();
		}
		else if (candle.ClosePrice <= lower && _cycleTrend != -1)
		{
			_cycleTrend = -1;
			if (Position >= 0)
				SellMarket();
		}
	}
}
