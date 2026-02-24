using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Andrews Pitchfork strategy: WMA crossover with MACD filter and momentum confirmation.
/// Enters long when fast WMA crosses above slow WMA with bullish MACD and momentum.
/// Enters short on the opposite conditions.
/// </summary>
public class AndrewsPitchforkStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;

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

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	public AndrewsPitchforkStrategy()
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

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Min deviation from 100", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastWma = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowMaPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastWma, slowWma, momentum, macd, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var mom = momentumValue.ToDecimal();

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		var momDeviation = Math.Abs(mom - 100m);
		var hasMomentum = momDeviation >= MomentumThreshold;

		if (_prevFast.HasValue && _prevSlow.HasValue && hasMomentum)
		{
			// Buy: WMA bullish cross + MACD bullish + momentum
			if (_prevFast.Value <= _prevSlow.Value && fast > slow && macdLine > signalLine && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: WMA bearish cross + MACD bearish + momentum
			else if (_prevFast.Value >= _prevSlow.Value && fast < slow && macdLine < signalLine && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
