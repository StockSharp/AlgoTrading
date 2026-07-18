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
/// EMA crossover with RSI confirmation trend following strategy.
/// </summary>
public class MultiIndicatorTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _longRsiLevel;
	private readonly StrategyParam<decimal> _shortRsiLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal LongRsiLevel { get => _longRsiLevel.Value; set => _longRsiLevel.Value = value; }
	public decimal ShortRsiLevel { get => _shortRsiLevel.Value; set => _shortRsiLevel.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiIndicatorTrendFollowingStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicators");
		_slowMaLength = Param(nameof(SlowMaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_longRsiLevel = Param(nameof(LongRsiLevel), 55m)
			.SetDisplay("Long RSI", "Minimum RSI for long entries", "Indicators");
		_shortRsiLevel = Param(nameof(ShortRsiLevel), 45m)
			.SetDisplay("Short RSI", "Maximum RSI for short entries", "Indicators");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after each crossover trade", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastMaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var prevFast = 0m;
		var prevSlow = 0m;
		var initialized = false;
		var cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, rsi, (candle, fastVal, slowVal, rsiVal) =>
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

				var crossUp = prevFast <= prevSlow && fastVal > slowVal;
				var crossDown = prevFast >= prevSlow && fastVal < slowVal;

				if (cooldownRemaining == 0 && crossUp && rsiVal > LongRsiLevel && Position <= 0)
				{
					BuyMarket();
					cooldownRemaining = SignalCooldownBars;
				}
				else if (cooldownRemaining == 0 && crossDown && rsiVal < ShortRsiLevel && Position >= 0)
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
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}
}
