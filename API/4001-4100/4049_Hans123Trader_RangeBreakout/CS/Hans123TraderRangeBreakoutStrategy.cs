using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hans123 breakout strategy. Builds a range from recent highest/lowest prices
/// and enters on breakout above range high or below range low.
/// </summary>
public class Hans123TraderRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevHighest;
	private decimal _prevLowest;

	public Hans123TraderRangeBreakoutStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 20)
			.SetDisplay("Range Length", "Number of candles used to compute the breakout range.", "Breakout");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for range detection.", "General");
	}

	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_prevHighest = 0;
		_prevLowest = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevHighest = 0;
		_prevLowest = 0;

		var highest = new Highest { Length = RangeLength };
		var lowest = new Lowest { Length = RangeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (highestValue <= 0 || lowestValue <= 0)
		{
			_prevHighest = highestValue;
			_prevLowest = lowestValue;
			return;
		}

		// Entry on breakout using previous levels
		if (Position == 0 && _prevHighest > 0 && _prevLowest > 0)
		{
			if (candle.ClosePrice > _prevHighest)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (candle.ClosePrice < _prevLowest)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
	}
}
