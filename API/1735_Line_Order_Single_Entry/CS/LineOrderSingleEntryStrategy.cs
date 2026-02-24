using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Line order strategy that enters when price crosses a predefined level.
/// Uses SMA as the dynamic entry line.
/// </summary>
public class LineOrderSingleEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LineOrderSingleEntryStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Moving average period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			// Cross above SMA
			if (_prevClose <= _prevSma && close > smaValue && Position <= 0)
			{
				BuyMarket();
			}
			// Cross below SMA
			else if (_prevClose >= _prevSma && close < smaValue && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevClose = close;
		_prevSma = smaValue;
		_hasPrev = true;
	}
}
