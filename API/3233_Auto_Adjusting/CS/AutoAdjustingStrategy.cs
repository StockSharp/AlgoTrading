using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto Adjusting strategy converted from the MQL4 expert "Aouto Adjusting1".
/// Combines a three-EMA stack with momentum filter to trade pullbacks in trend.
/// </summary>
public class AutoAdjustingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _middleEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMiddle;
	private decimal _prevSlow;
	private bool _hasPrev;

	/// <summary>
	/// Constructor.
	/// </summary>
	public AutoAdjustingStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA", "Signals");

		_middleEmaLength = Param(nameof(MiddleEmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Middle EMA", "Length of the middle EMA", "Signals");

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow EMA", "Signals");

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Lookback for the momentum indicator", "Signals");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.1m)
			.SetNotNegative()
			.SetDisplay("Momentum Threshold", "Minimum deviation from 100 for signals", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int MiddleEmaLength
	{
		get => _middleEmaLength.Value;
		set => _middleEmaLength.Value = value;
	}

	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var emaFast = new EMA { Length = FastEmaLength };
		var emaMiddle = new EMA { Length = MiddleEmaLength };
		var emaSlow = new EMA { Length = SlowEmaLength };
		var momentum = new Momentum { Length = MomentumLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaMiddle, emaSlow, momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaMiddle);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow, decimal mom)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var momentumDeviation = Math.Abs(mom - 100m);
		var hasMomentum = momentumDeviation >= MomentumThreshold;

		// EMA stack: bullish = fast > middle > slow
		var bullishStack = fast > middle && middle > slow;
		// EMA stack: bearish = fast < middle < slow
		var bearishStack = fast < middle && middle < slow;

		if (_hasPrev)
		{
			// Detect crossover: previous was not bullish, now bullish
			var prevBullish = _prevFast > _prevMiddle && _prevMiddle > _prevSlow;
			var prevBearish = _prevFast < _prevMiddle && _prevMiddle < _prevSlow;

			if (bullishStack && !prevBullish && hasMomentum && Position <= 0)
			{
				BuyMarket();
			}
			else if (bearishStack && !prevBearish && hasMomentum && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevMiddle = middle;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
