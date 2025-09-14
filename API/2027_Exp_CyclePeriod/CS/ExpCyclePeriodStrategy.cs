using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the CyclePeriod indicator.
/// </summary>
public class ExpCyclePeriodStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _alpha;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private readonly Queue<decimal> _values = new();

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Alpha parameter for CyclePeriod indicator.
	/// </summary>
	public decimal Alpha
	{
		get => _alpha.Value;
		set => _alpha.Value = value;
	}

	/// <summary>
	/// Number of bars to shift the indicator for signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Take profit value in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss value in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ExpCyclePeriodStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_alpha = Param(nameof(Alpha), 0.07m)
			.SetDisplay("Alpha", "Alpha for CyclePeriod", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.2m, 0.01m);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Indicator shift", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow long entries", "Logic");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow short entries", "Logic");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing longs", "Logic");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing shorts", "Logic");
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

		// Enable risk management with take profit and stop loss
		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));

		var cyclePeriod = new CyclePeriod { Alpha = Alpha };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cyclePeriod, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_values.Enqueue(value);
		var required = SignalBar + 3;
		if (_values.Count < required)
			return;

		var arr = _values.ToArray();
		var v0 = arr[^ (SignalBar + 1)];
		var v1 = arr[^ (SignalBar + 2)];
		var v2 = arr[^ (SignalBar + 3)];

		while (_values.Count > required)
			_values.Dequeue();

		if (v1 < v2)
		{
			if (BuyPosOpen && v0 > v1 && Position <= 0)
				BuyMarket(Volume);

			if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (v1 > v2)
		{
			if (SellPosOpen && v0 < v1 && Position >= 0)
				SellMarket(Volume);

			if (BuyPosClose && Position > 0)
				SellMarket(Math.Abs(Position));
		}
	}
}
