using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA crossover prediction indicator.
/// </summary>
public class EmaPredictionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfitTicks;
	private readonly StrategyParam<decimal> _stopLossTicks;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	/// <summary>
	/// Candle type for indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public decimal StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public EmaPredictionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast EMA Period", "Period of fast EMA", "Indicator")
			.SetGreaterThanZero();

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetDisplay("Slow EMA Period", "Period of slow EMA", "Indicator")
			.SetGreaterThanZero();

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000m)
			.SetDisplay("Take Profit Ticks", "Take profit in ticks", "Risk Management")
			.SetNotNegative();

		_stopLossTicks = Param(nameof(StopLossTicks), 1000m)
			.SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk Management")
			.SetNotNegative();
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
		_prevFast = default;
		_prevSlow = default;
		_initialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			new Unit(StopLossTicks * step, UnitTypes.Absolute),
			new Unit(TakeProfitTicks * step, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var bullish = _prevFast < _prevSlow && fast > slow && candle.OpenPrice < candle.ClosePrice;
		var bearish = _prevFast > _prevSlow && fast < slow && candle.OpenPrice > candle.ClosePrice;

		if (bullish && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (bearish && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
