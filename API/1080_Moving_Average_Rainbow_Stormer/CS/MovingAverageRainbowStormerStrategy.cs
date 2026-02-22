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
/// Moving Average Rainbow (Stormer) strategy.
/// Uses multiple EMA rainbow for trend confirmation.
/// Enters long when price is above all MAs (bullish alignment), short when below.
/// </summary>
public class MovingAverageRainbowStormerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _targetFactor;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _ma3;
	private EMA _ma8;
	private EMA _ma20;
	private EMA _ma50;
	private decimal _entryPrice;

	public decimal TargetFactor { get => _targetFactor.Value; set => _targetFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageRainbowStormerStrategy()
	{
		_targetFactor = Param(nameof(TargetFactor), 2m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		_ma3 = new EMA { Length = 3 };
		_ma8 = new EMA { Length = 8 };
		_ma20 = new EMA { Length = 20 };
		_ma50 = new EMA { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma3, _ma8, _ma20, _ma50, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma3, decimal ma8, decimal ma20, decimal ma50)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Bullish: all MAs aligned (ma3 > ma8 > ma20 > ma50) and price touches ma8 zone
		var bullishAlignment = ma3 > ma8 && ma8 > ma20 && ma20 > ma50;
		var priceTouchMa8 = candle.LowPrice <= ma8;

		// Bearish: all MAs aligned downward
		var bearishAlignment = ma3 < ma8 && ma8 < ma20 && ma20 < ma50;
		var priceTouchMa8Short = candle.HighPrice >= ma8;

		if (bullishAlignment && priceTouchMa8 && close > ma3 && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket();
			_entryPrice = close;
		}
		else if (bearishAlignment && priceTouchMa8Short && close < ma3 && Position >= 0)
		{
			if (Position > 0) SellMarket(Position);
			SellMarket();
			_entryPrice = close;
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var risk = _entryPrice - ma20;
			if (risk > 0)
			{
				var target = _entryPrice + risk * TargetFactor;
				if (close >= target || close < ma20)
					SellMarket();
			}
			else if (close < ma8)
				SellMarket();
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var risk = ma20 - _entryPrice;
			if (risk > 0)
			{
				var target = _entryPrice - risk * TargetFactor;
				if (close <= target || close > ma20)
					BuyMarket();
			}
			else if (close > ma8)
				BuyMarket();
		}
	}
}
