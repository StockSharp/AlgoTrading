using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Executes a market order when price crosses a moving average "line".
/// Manages stop-loss and take-profit via candle monitoring.
/// </summary>
public class LineOrderStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevClose;
	private decimal _prevMa;
	private bool _hasPrev;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LineOrderStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period for line", "Indicators");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_prevClose = 0;
		_prevMa = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevMa = maVal;
			_hasPrev = true;
			return;
		}

		// Check stop-loss / take-profit for existing positions
		if (Position > 0 && _entryPrice > 0)
		{
			if (close <= _entryPrice * (1 - StopLossPct / 100m) ||
				close >= _entryPrice * (1 + TakeProfitPct / 100m))
			{
				SellMarket();
				_entryPrice = 0;
				_prevClose = close;
				_prevMa = maVal;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (close >= _entryPrice * (1 + StopLossPct / 100m) ||
				close <= _entryPrice * (1 - TakeProfitPct / 100m))
			{
				BuyMarket();
				_entryPrice = 0;
				_prevClose = close;
				_prevMa = maVal;
				return;
			}
		}

		// Cross above MA line -> buy
		if (_prevClose <= _prevMa && close > maVal)
		{
			if (Position < 0)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
			}
		}
		// Cross below MA line -> sell
		else if (_prevClose >= _prevMa && close < maVal)
		{
			if (Position > 0)
			{
				SellMarket();
				_entryPrice = 0;
			}
			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		_prevClose = close;
		_prevMa = maVal;
	}
}
