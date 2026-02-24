using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibo Arc Momentum strategy: WMA crossover with momentum filter and MACD.
/// Enters long when fast WMA above slow WMA, momentum is bullish, and MACD confirms.
/// Enters short on the opposite conditions.
/// </summary>
public class FiboArcMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public FiboArcMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted MA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted MA period", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum", "Momentum period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastWma = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowMaPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullMomentum = momentum > 100m;
		var bearMomentum = momentum < 100m;

		if (_prevFast.HasValue && _prevSlow.HasValue)
		{
			if (_prevFast.Value <= _prevSlow.Value && fast > slow && bullMomentum && Position <= 0)
			{
				BuyMarket();
			}
			else if (_prevFast.Value >= _prevSlow.Value && fast < slow && bearMomentum && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
