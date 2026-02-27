using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares the close of the last finished candle with the open of the prior candle.
/// Buys when the latest close is above the previous open, sells when it is below.
/// </summary>
public class CloseVsPreviousOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevPrevOpen;
	private decimal _prevClose;
	private bool _isInitialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CloseVsPreviousOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0; _prevPrevOpen = 0; _prevClose = 0; _isInitialized = false;
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
		var open = candle.OpenPrice;

		if (_isInitialized)
		{
			if (_prevClose > _prevPrevOpen && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (_prevClose < _prevPrevOpen && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPrevOpen = _prevOpen;
		_prevOpen = open;
		_prevClose = close;
		_isInitialized = true;
	}
}
