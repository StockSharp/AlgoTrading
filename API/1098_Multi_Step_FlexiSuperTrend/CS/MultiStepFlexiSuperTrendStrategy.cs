using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-Step FlexiSuperTrend strategy - SMA deviation crossover.
/// </summary>
public class MultiStepFlexiSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiHigh;
	private readonly StrategyParam<decimal> _rsiLow;
	private readonly StrategyParam<int> _signalCooldownBars;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }
	public decimal RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public MultiStepFlexiSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Indicators");
		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_rsiHigh = Param(nameof(RsiHigh), 55m)
			.SetDisplay("RSI High", "RSI overbought", "Indicators");
		_rsiLow = Param(nameof(RsiLow), 45m)
			.SetDisplay("RSI Low", "RSI oversold", "Indicators");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var prevFast = 0m;
		var prevSlow = 0m;
		var initialized = false;
		var cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fast, slow, rsi, (candle, fastVal, slowVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (cooldownRemaining > 0)
					cooldownRemaining--;

				if (!initialized)
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					initialized = true;
					return;
				}

				if (cooldownRemaining == 0 && prevFast <= prevSlow && fastVal > slowVal && rsiVal > RsiHigh && Position <= 0)
				{
					BuyMarket();
					cooldownRemaining = SignalCooldownBars;
				}
				else if (cooldownRemaining == 0 && prevFast >= prevSlow && fastVal < slowVal && rsiVal < RsiLow && Position > 0)
				{
					SellMarket();
					cooldownRemaining = SignalCooldownBars;
				}

				prevFast = fastVal;
				prevSlow = slowVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
