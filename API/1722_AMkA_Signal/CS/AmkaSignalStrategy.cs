using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AMkA based strategy using KAMA derivative and standard deviation filter.
/// Buys when KAMA rises above volatility threshold and sells when it falls below.
/// Includes optional take-profit and stop-loss protection.
/// </summary>
public class AmkaSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _fast;
	private readonly StrategyParam<int> _slow;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevKama;
	private bool _isFirst;
	private StandardDeviation _stdDev = null!;

	/// <summary>
	/// KAMA lookback period.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Fast EMA period for smoothing constant.
	/// </summary>
	public int Fast { get => _fast.Value; set => _fast.Value = value; }

	/// <summary>
	/// Slow EMA period for smoothing constant.
	/// </summary>
	public int Slow { get => _slow.Value; set => _slow.Value = value; }

	/// <summary>
	/// Multiplier for standard deviation threshold.
	/// </summary>
	public decimal DeviationMultiplier { get => _deviationMultiplier.Value; set => _deviationMultiplier.Value = value; }

	/// <summary>
	/// Take-profit value.
	/// </summary>
	public Unit TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop-loss value.
	/// </summary>
	public Unit StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Candle type for indicator calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AmkaSignalStrategy"/> class.
	/// </summary>
	public AmkaSignalStrategy()
	{
		_length = Param(nameof(Length), 9)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Lookback period for the adaptive moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_fast = Param(nameof(Fast), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast smoothing constant period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slow = Param(nameof(Slow), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow smoothing constant period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation filter", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2.0m, 0.5m);

		_takeProfit = Param(nameof(TakeProfit), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Take Profit", "Take-profit percentage", "Risk");

		_stopLoss = Param(nameof(StopLoss), new Unit(1, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop-loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevKama = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var kama = new KaufmanAdaptiveMovingAverage
		{
			Length = Length,
			FastSCPeriod = Fast,
			SlowSCPeriod = Slow
		};

		_stdDev = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(kama, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kama);
			DrawOwnTrades(area);
		}

		StartProtection(TakeProfit, StopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevKama = kamaValue;
			_isFirst = false;
			return;
		}

		var delta = kamaValue - _prevKama;
		_prevKama = kamaValue;

		var stdValue = _stdDev.Process(delta, candle.OpenTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_stdDev.IsFormed)
			return;

		var threshold = stdValue * DeviationMultiplier;

		if (delta > threshold)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position == 0)
				BuyMarket(Volume);
		}
		else if (delta < -threshold)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			if (Position == 0)
				SellMarket(Volume);
		}
	}
}
