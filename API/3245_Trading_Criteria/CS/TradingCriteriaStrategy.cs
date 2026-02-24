using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading Criteria strategy: combines WMA crossover, Momentum filter, and MACD confirmation
/// for trend-following entries on a single timeframe.
/// </summary>
public class TradingCriteriaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;

	private decimal? _prevMacd;
	private decimal? _prevSignal;

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

	public TradingCriteriaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted moving average period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted moving average period", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Threshold", "Minimum momentum deviation for entry", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevSignal = null;

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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastWmaValue, IIndicatorValue slowWmaValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fastMa = fastWmaValue.ToDecimal();
		var slowMa = slowWmaValue.ToDecimal();
		var mom = momentumValue.ToDecimal();

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		var momDeviation = Math.Abs(mom - 100m);
		var longTrend = fastMa > slowMa;
		var shortTrend = fastMa < slowMa;

		// MACD crossover confirmation
		var macdBullCross = _prevMacd.HasValue && _prevSignal.HasValue
			&& _prevMacd.Value <= _prevSignal.Value && macdLine > signalLine;
		var macdBearCross = _prevMacd.HasValue && _prevSignal.HasValue
			&& _prevMacd.Value >= _prevSignal.Value && macdLine < signalLine;

		// Buy: WMA uptrend + momentum strong enough + MACD bullish cross
		if (longTrend && momDeviation >= MomentumThreshold && macdBullCross && Position <= 0)
		{
			BuyMarket();
		}
		// Sell: WMA downtrend + momentum strong enough + MACD bearish cross
		else if (shortTrend && momDeviation >= MomentumThreshold && macdBearCross && Position >= 0)
		{
			SellMarket();
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}
}
