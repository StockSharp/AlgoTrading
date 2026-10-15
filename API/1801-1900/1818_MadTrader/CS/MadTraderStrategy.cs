using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-based trading strategy.
/// </summary>
public class MadTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiUpper { get => _rsiUpper.Value; set => _rsiUpper.Value = value; }
	public decimal RsiLower { get => _rsiLower.Value; set => _rsiLower.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MadTraderStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_rsiUpper = Param(nameof(RsiUpper), 70m)
			.SetDisplay("RSI Upper", "Overbought level", "Indicators");
		_rsiLower = Param(nameof(RsiLower), 30m)
			.SetDisplay("RSI Lower", "Oversold level", "Indicators");
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

		// Buy when RSI crosses above lower level (oversold exit)
		if (_prevRsi <= RsiLower && rsiVal > RsiLower && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell when RSI crosses below upper level (overbought exit)
		else if (_prevRsi >= RsiUpper && rsiVal < RsiUpper && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevRsi = rsiVal;
	}
}
