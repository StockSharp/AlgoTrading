using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on candle direction patterns.
/// Opens long or short positions depending on the directions of the last four candles.
/// </summary>
public class CandleTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfitTicks;
	private readonly StrategyParam<decimal> _stopLossTicks;
	private readonly StrategyParam<bool> _continuation;
	private readonly StrategyParam<bool> _reverseClose;
	private readonly StrategyParam<DataType> _candleType;

	private int _bar1Dir;
	private int _bar2Dir;
	private int _bar3Dir;
	private int _bar4Dir;

	/// <summary>
	/// Order volume.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Enable continuation pattern.
	/// </summary>
	public bool Continuation
	{
		get => _continuation.Value;
		set => _continuation.Value = value;
	}

	/// <summary>
	/// Close opposite position before opening a new one.
	/// </summary>
	public bool ReverseClose
	{
		get => _reverseClose.Value;
		set => _reverseClose.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CandleTraderStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Ticks", "Take profit in price steps", "Risk Management");

		_stopLossTicks = Param(nameof(StopLossTicks), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Ticks", "Stop loss in price steps", "Risk Management");

		_continuation = Param(nameof(Continuation), true)
			.SetDisplay("Use Continuation", "Allow continuation pattern", "Trading Logic");

		_reverseClose = Param(nameof(ReverseClose), true)
			.SetDisplay("Reverse Close", "Close opposite position on signal", "Trading Logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_bar1Dir = _bar2Dir = _bar3Dir = _bar4Dir = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Shift stored directions
		_bar4Dir = _bar3Dir;
		_bar3Dir = _bar2Dir;
		_bar2Dir = _bar1Dir;

		// Determine direction of current candle
		_bar1Dir = candle.ClosePrice > candle.OpenPrice ? 1 : candle.ClosePrice < candle.OpenPrice ? -1 : 0;

		// Ensure sufficient history
		if (_bar4Dir == 0)
			return;

		var buyDirect = _bar1Dir == 1 && _bar2Dir == -1 && _bar3Dir == -1;
		var buyCont = _bar1Dir == 1 && _bar2Dir == -1 && _bar3Dir == 1 && _bar4Dir == 1 && Continuation;

		var sellDirect = _bar1Dir == -1 && _bar2Dir == 1 && _bar3Dir == 1;
		var sellCont = _bar1Dir == -1 && _bar2Dir == 1 && _bar3Dir == -1 && _bar4Dir == -1 && Continuation;

		if ((buyDirect || buyCont) && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + (ReverseClose && Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
		}
		else if ((sellDirect || sellCont) && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + (ReverseClose && Position > 0 ? Math.Abs(Position) : 0m);
			SellMarket(volume);
		}
	}
}

