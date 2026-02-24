using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility straddle strategy with trailing stop.
/// Enters on large candle breakouts and uses trailing stop to protect profits.
/// </summary>
public class StraddleNewsStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingDist;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingDist { get => _trailingDist.Value; set => _trailingDist.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StraddleNewsStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "Range vs ATR for entry", "Indicators");
		_stopLoss = Param(nameof(StopLoss), 400m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
		_trailingDist = Param(nameof(TrailingDist), 200m)
			.SetDisplay("Trailing Distance", "Trailing stop distance", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailingStop = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var price = candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;

		// Trailing stop and SL management
		if (Position > 0)
		{
			var newTrail = price - TrailingDist;
			if (newTrail > _trailingStop)
				_trailingStop = newTrail;

			if (price <= _trailingStop || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				_trailingStop = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			var newTrail = price + TrailingDist;
			if (_trailingStop == 0 || newTrail < _trailingStop)
				_trailingStop = newTrail;

			if (price >= _trailingStop || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				_trailingStop = 0;
				return;
			}
		}

		// Entry: large candle breakout
		if (Position == 0 && range > atrValue * AtrMultiplier)
		{
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
				_entryPrice = price;
				_trailingStop = price - TrailingDist;
			}
			else if (candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket();
				_entryPrice = price;
				_trailingStop = price + TrailingDist;
			}
		}
	}
}
