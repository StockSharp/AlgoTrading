using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZMFX Stolid strategy - trades pullbacks within the main trend.
/// Uses RSI for oversold/overbought, EMA crossover for trend direction.
/// </summary>
public class ZmfxStolid5aEaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrevRsi;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZmfxStolid5aEaStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 11)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_hasPrevRsi = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, fastEma, slowEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal fastEma, decimal slowEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrevRsi)
		{
			_prevRsi = rsi;
			_hasPrevRsi = true;
			return;
		}

		var upTrend = fastEma > slowEma;
		var downTrend = fastEma < slowEma;

		// Buy pullback: uptrend, RSI was oversold, now crossing up
		if (upTrend && _prevRsi < 35 && rsi >= 35 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell pullback: downtrend, RSI was overbought, now crossing down
		else if (downTrend && _prevRsi > 65 && rsi <= 65 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		// Exit long on RSI overbought or trend reversal
		if (Position > 0 && (rsi > 75 || fastEma < slowEma))
		{
			SellMarket();
		}
		// Exit short on RSI oversold or trend reversal
		else if (Position < 0 && (rsi < 25 || fastEma > slowEma))
		{
			BuyMarket();
		}

		_prevRsi = rsi;
	}
}
