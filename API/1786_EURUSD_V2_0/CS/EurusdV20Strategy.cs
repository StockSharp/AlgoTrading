using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA mean-reversion strategy with ATR filter.
/// Buys below SMA, sells above SMA when ATR confirms range conditions.
/// </summary>
public class EurusdV20Strategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EurusdV20Strategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA period", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var sma = new SimpleMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(sma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (close - _entryPrice >= TakeProfit || _entryPrice - close >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - close >= TakeProfit || close - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		if (Position != 0)
			return;

		// Mean reversion: buy below SMA, sell above SMA
		// Use ATR as distance filter: only enter when price is at least 0.5*ATR away
		var dist = Math.Abs(close - sma);
		if (atr <= 0 || dist < atr * 0.5m)
			return;

		if (close < sma)
		{
			BuyMarket();
			_entryPrice = close;
		}
		else if (close > sma)
		{
			SellMarket();
			_entryPrice = close;
		}
	}
}
