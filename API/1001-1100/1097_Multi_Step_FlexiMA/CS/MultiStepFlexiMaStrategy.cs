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
/// Multi-step FlexiMA strategy using fast and slow SMA crossover.
/// </summary>
public class MultiStepFlexiMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalCooldownBars;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public MultiStepFlexiMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "FlexiMA");
		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "FlexiMA");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between crossovers", "FlexiMA");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SMA { Length = FastLength };
		var slowSma = new SMA { Length = SlowLength };

		var prevFast = 0m;
		var prevSlow = 0m;
		var initialized = false;
		var cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(candle =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (cooldownRemaining > 0)
					cooldownRemaining--;

				var fastResult = fastSma.Process(candle);
				var slowResult = slowSma.Process(candle);
				if (!fastSma.IsFormed || !slowSma.IsFormed || fastResult.IsEmpty || slowResult.IsEmpty)
					return;

				var fastVal = fastResult.ToDecimal();
				var slowVal = slowResult.ToDecimal();

				if (!initialized)
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					initialized = true;
					return;
				}

				if (cooldownRemaining == 0 && prevFast <= prevSlow && fastVal > slowVal && Position <= 0)
				{
					BuyMarket();
					cooldownRemaining = SignalCooldownBars;
				}
				else if (cooldownRemaining == 0 && prevFast >= prevSlow && fastVal < slowVal && Position > 0)
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
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}
}
