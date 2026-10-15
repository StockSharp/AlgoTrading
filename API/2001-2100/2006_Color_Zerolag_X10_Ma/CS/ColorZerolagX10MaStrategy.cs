using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend detection strategy based on the slope of a zero lag moving average.
/// </summary>
public class ColorZerolagX10MaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1;
	private decimal _prev2;
	private int _count;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagX10MaStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "MA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = 0;
		_prev2 = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var zlma = new ZeroLagExponentialMovingAverage { Length = Length };

		SubscribeCandles(CandleType)
			.Bind(zlma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;
		if (_count < 3)
		{
			_prev2 = _prev1;
			_prev1 = ma;
			return;
		}

		// Detect trend turn via slope direction change
		var trendUp = _prev1 < _prev2 && ma > _prev1;
		var trendDown = _prev1 > _prev2 && ma < _prev1;

		if (trendUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (trendDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = ma;
	}
}
