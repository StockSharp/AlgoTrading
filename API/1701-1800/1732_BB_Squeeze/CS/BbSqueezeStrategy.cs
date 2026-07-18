using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout strategy.
/// Enters on breakout above/below bands, exits at middle band.
/// </summary>
public class BbSqueezeStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _hasPrev;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BbSqueezeStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of Bollinger Bands", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BollingerPeriod, Width = 2m };
		SubscribeCandles(CandleType).BindEx(bb, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished) return;

		var bbVal = (BollingerBandsValue)value;
		if (bbVal.UpBand is not decimal upper ||
			bbVal.LowBand is not decimal lower ||
			bbVal.MovingAverage is not decimal middle)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevUpper = upper;
			_prevLower = lower;
			_hasPrev = true;
			return;
		}

		// Cross above upper band => buy
		if (_prevClose <= _prevUpper && close > upper && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Cross below lower band => sell
		else if (_prevClose >= _prevLower && close < lower && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		// Exit long at middle
		else if (Position > 0 && close < middle)
		{
			SellMarket();
		}
		// Exit short at middle
		else if (Position < 0 && close > middle)
		{
			BuyMarket();
		}

		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
