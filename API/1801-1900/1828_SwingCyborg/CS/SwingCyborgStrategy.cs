using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SwingCyborg strategy using RSI overbought/oversold levels.
/// </summary>
public class SwingCyborgStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SwingCyborgStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		SubscribeCandles(CandleType)
			.Bind(rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevRsi = rsiVal;
			_hasPrev = true;
			return;
		}

		// Buy when RSI exits oversold
		if (_prevRsi <= 30m && rsiVal > 30m && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell when RSI exits overbought
		else if (_prevRsi >= 70m && rsiVal < 70m && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevRsi = rsiVal;
	}
}
