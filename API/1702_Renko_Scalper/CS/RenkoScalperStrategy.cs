using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko scalper strategy.
/// Opens long when close is higher than previous close, short when lower.
/// </summary>
public class RenkoScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RenkoScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousClose = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (_previousClose is null)
		{
			_previousClose = close;
			return;
		}

		if (close > _previousClose && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (close < _previousClose && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_previousClose = close;
	}
}
