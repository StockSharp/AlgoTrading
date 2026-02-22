using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA crossover, RSI and Stochastic oscillator.
/// </summary>
public class MultiConditionsCurveFittingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiConditionsCurveFittingStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10);
		_slowEmaLength = Param(nameof(SlowEmaLength), 25);
		_rsiLength = Param(nameof(RsiLength), 14);
		_rsiOverbought = Param(nameof(RsiOverbought), 70m);
		_rsiOversold = Param(nameof(RsiOversold), 30m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new EMA { Length = FastEmaLength };
		var slowEma = new EMA { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longCondition = fastEma > slowEma && rsi < 50;
		var shortCondition = fastEma < slowEma && rsi > 50;

		if (longCondition && Position <= 0)
			BuyMarket();
		if (shortCondition && Position >= 0)
			SellMarket();

		// Exit
		if (Position > 0 && (fastEma < slowEma || rsi > RsiOverbought))
			SellMarket();
		if (Position < 0 && (fastEma > slowEma || rsi < RsiOversold))
			BuyMarket();
	}
}
