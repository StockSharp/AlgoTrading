using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA trend + Williams %R entry strategy.
/// Buys in uptrend when WPR oversold, sells in downtrend when WPR overbought.
/// </summary>
public class EmaPlusWprV2Strategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _wprLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private bool _hasPrev;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int WprLength { get => _wprLength.Value; set => _wprLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaPlusWprV2Strategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_wprLength = Param(nameof(WprLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Length", "Williams %R period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var wpr = new WilliamsR { Length = WprLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, wpr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal wprVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevEma = emaVal;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;
		var uptrend = emaVal > _prevEma && close > emaVal;
		var downtrend = emaVal < _prevEma && close < emaVal;

		// WPR range is -100 to 0
		// Oversold: WPR < -80, Overbought: WPR > -20
		if (uptrend && wprVal < -80 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (downtrend && wprVal > -20 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		// Exit on WPR reversal
		if (Position > 0 && wprVal > -20)
			SellMarket();
		else if (Position < 0 && wprVal < -80)
			BuyMarket();

		_prevEma = emaVal;
	}
}
