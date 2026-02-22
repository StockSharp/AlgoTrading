using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LorenzoSuperScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LorenzoSuperScalpStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_bbLength = Param(nameof(BbLength), 20).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var bb = new BollingerBands { Length = BbLength, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiVal, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiVal.IsFinal || !rsiVal.IsFormed || !bbVal.IsFormed)
			return;

		var rsi = rsiVal.ToDecimal();

		// Get BB bands
		decimal upperBand = candle.ClosePrice * 1.01m;
		decimal lowerBand = candle.ClosePrice * 0.99m;
		var complexBb = bbVal as IComplexIndicatorValue;
		if (complexBb != null)
		{
			var vals = complexBb.InnerValues.Select(v => v.Value.ToDecimal()).ToArray();
			if (vals.Length >= 3)
			{
				upperBand = vals[0];
				lowerBand = vals[2];
			}
		}

		// Buy when RSI oversold and price near lower BB
		if (rsi < 35m && candle.ClosePrice <= lowerBand && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		// Sell when RSI overbought and price near upper BB
		else if (rsi > 65m && candle.ClosePrice >= upperBand && Position >= 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}
		// Exit long when RSI > 60
		else if (Position > 0 && rsi > 60m)
		{
			SellMarket(Math.Abs(Position));
		}
		// Exit short when RSI < 40
		else if (Position < 0 && rsi < 40m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
