using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Averaged Stochastic with ZigZag-style pivot confirmation.
/// Uses RSI as simplified oscillator and highest/lowest for pivots.
/// </summary>
public class Aver4StochPostZigZagStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Aver4StochPostZigZagStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_pivotLength = Param(nameof(PivotLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Highest/Lowest period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var highest = new Highest { Length = PivotLength };
		var lowest = new Lowest { Length = PivotLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevRsi = rsi;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Near pivot low + RSI oversold -> buy
		if (close <= low * 1.001m && rsi < 30 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Near pivot high + RSI overbought -> sell
		else if (close >= high * 0.999m && rsi > 70 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long on RSI overbought
		else if (Position > 0 && rsi > 70)
		{
			SellMarket();
		}
		// Exit short on RSI oversold
		else if (Position < 0 && rsi < 30)
		{
			BuyMarket();
		}

		_prevRsi = rsi;
	}
}
