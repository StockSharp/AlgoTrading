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
/// Demonstrates CAGR calculation and trades in the direction of the growth rate.
/// </summary>
public class TaLibraryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _prevTime;
	private decimal _prevClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TaLibraryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
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
		_prevTime = null;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevTime is null)
		{
			_prevTime = candle.OpenTime;
			_prevClose = candle.ClosePrice;
			return;
		}

		var hours = (candle.OpenTime - _prevTime.Value).TotalHours;
		if (hours >= 1)
		{
			var rate = candle.ClosePrice / _prevClose;
			var years = (decimal)hours / (365m * 24m);
			var cagr = years > 0 ? (decimal)(Math.Pow((double)rate, 1.0 / (double)years) - 1) * 100m : 0m;

			if (cagr > 0m && Position <= 0)
				BuyMarket();
			else if (cagr < 0m && Position >= 0)
				SellMarket();

			_prevTime = candle.OpenTime;
			_prevClose = candle.ClosePrice;
		}
	}
}
