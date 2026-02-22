using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe RSI strategy.
/// </summary>
public class MtfOscillatorFrameworkStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	public MtfOscillatorFrameworkStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_rsiLength = Param(nameof(RsiLength), 14);
		_overbought = Param(nameof(Overbought), 70m);
		_oversold = Param(nameof(Oversold), 30m);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new EMA { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (rsi < Oversold && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket();
		}
		else if (rsi > Overbought && Position >= 0)
		{
			if (Position > 0) SellMarket(Position);
			SellMarket();
		}

		// Exit on RSI normalization
		if (Position > 0 && rsi > 60)
			SellMarket();
		else if (Position < 0 && rsi < 40)
			BuyMarket();
	}
}
