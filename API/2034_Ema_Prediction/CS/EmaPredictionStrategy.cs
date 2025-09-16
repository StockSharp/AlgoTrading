using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private ExponentialMovingAverage _fast;
	private ExponentialMovingAverage _slow;
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
	/// Allow long entries.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on sell signals.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signals.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public EmaPredictionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 1)
			.SetDisplay("Fast EMA Period", "Period of fast EMA", "Indicator")
			.SetGreaterThanZero();

		_slowPeriod = Param(nameof(SlowPeriod), 2)
			.SetDisplay("Slow EMA Period", "Period of slow EMA", "Indicator")
			.SetGreaterThanZero();

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000m)
			.SetDisplay("Take Profit Ticks", "Take profit in ticks", "Risk Management")
			.SetGreaterThanOrEqual(0m);

		_stopLossTicks = Param(nameof(StopLossTicks), 1000m)
			.SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk Management")
			.SetGreaterThanOrEqual(0m);

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Open Long", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Open Short", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long", "Close long positions on sell signal", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short", "Close short positions on buy signal", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fast = new ExponentialMovingAverage { Length = FastPeriod };
		_slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fast, _slow, ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fast);
			DrawIndicator(area, _slow);
			DrawOwnTrades(area);
		}
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

		if (bullish)
		{
			var volume = 0m;
			if (SellClose && Position < 0)
				volume += Math.Abs(Position);
			if (BuyOpen && Position <= 0)
				volume += Volume;
			if (volume > 0)
				BuyMarket(volume);
		}
		else if (bearish)
		{
			var volume = 0m;
			if (BuyClose && Position > 0)
				volume += Math.Abs(Position);
			if (SellOpen && Position >= 0)
				volume += Volume;
			if (volume > 0)
				SellMarket(volume);
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
