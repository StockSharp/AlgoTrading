using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Direction Index re-entry strategy.
/// Trades based on crossings between the TDI momentum line and the TDI index line.
/// </summary>
public class Tdi2ReOpenStrategy : Strategy
{
	private readonly StrategyParam<int> _tdiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDirectional;
	private decimal? _prevIndex;

	public int TdiPeriod { get => _tdiPeriod.Value; set => _tdiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Tdi2ReOpenStrategy()
	{
		_tdiPeriod = Param(nameof(TdiPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("TDI Period", "Momentum lookback period", "Indicator")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Data series", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDirectional = null;
		_prevIndex = null;

		var momentum = new Momentum { Length = TdiPeriod };
		var momSmoother = new SimpleMovingAverage { Length = TdiPeriod };
		var absSmoother = new SimpleMovingAverage { Length = TdiPeriod };
		var absDoubleSmoother = new SimpleMovingAverage { Length = TdiPeriod * 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var t = candle.OpenTime;
			var close = candle.ClosePrice;

			var momVal = momentum.Process(new DecimalIndicatorValue(momentum, close, t) { IsFinal = true });
			if (!momentum.IsFormed)
				return;

			var mom = momVal.ToDecimal();
			var absMom = Math.Abs(mom);

			var momSmooth = momSmoother.Process(new DecimalIndicatorValue(momSmoother, mom, t) { IsFinal = true });
			var absSmooth = absSmoother.Process(new DecimalIndicatorValue(absSmoother, absMom, t) { IsFinal = true });
			var absDoubleSmooth = absDoubleSmoother.Process(new DecimalIndicatorValue(absDoubleSmoother, absMom, t) { IsFinal = true });

			if (!momSmoother.IsFormed || !absSmoother.IsFormed || !absDoubleSmoother.IsFormed)
				return;

			var momSum = TdiPeriod * momSmooth.ToDecimal();
			var momAbsSum = TdiPeriod * absSmooth.ToDecimal();
			var momAbsSum2 = 2 * TdiPeriod * absDoubleSmooth.ToDecimal();

			var directional = momSum;
			var index = Math.Abs(momSum) - (momAbsSum2 - absMom);

			if (_prevDirectional is not decimal prevDir || _prevIndex is not decimal prevIdx)
			{
				_prevDirectional = directional;
				_prevIndex = index;
				return;
			}

			// Buy on cross: directional crosses above index
			var crossUp = prevDir <= prevIdx && directional > index;
			// Sell on cross: directional crosses below index
			var crossDown = prevDir >= prevIdx && directional < index;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();

			_prevDirectional = directional;
			_prevIndex = index;
		}
	}
}
