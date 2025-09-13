using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens positions at a specific time with fixed stops.
/// </summary>
public class TimeTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _tradeMinute;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private bool _buyExecuted;
	private bool _sellExecuted;

	/// <summary>
	/// Hour for order placement (0-23).
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	/// <summary>
	/// Minute for order placement (0-59).
	/// </summary>
	public int TradeMinute
	{
		get => _tradeMinute.Value;
		set => _tradeMinute.Value = value;
	}

	/// <summary>
	/// Allow long entry.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow short entry.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Candle type used to check time.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public TimeTraderStrategy()
	{
		_tradeHour = Param(nameof(TradeHour), 0)
			.SetDisplay("Trade Hour", "Hour of day to trade", "General")
			.SetCanOptimize(true, 0, 23, 1);
		_tradeMinute = Param(nameof(TradeMinute), 0)
			.SetDisplay("Trade Minute", "Minute of hour to trade", "General")
			.SetCanOptimize(true, 0, 59, 1);
		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long order", "General");
		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short order", "General");
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 20)
			.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Protection")
			.SetCanOptimize(true, 1, 200, 1);
		_stopLossTicks = Param(nameof(StopLossTicks), 20)
			.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Protection")
			.SetCanOptimize(true, 1, 200, 1);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_buyExecuted = false;
		_sellExecuted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			new Unit(TakeProfitTicks * step, UnitTypes.Point),
			new Unit(StopLossTicks * step, UnitTypes.Point));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime.Hour != TradeHour || candle.OpenTime.Minute != TradeMinute)
			return;

		if (AllowBuy && !_buyExecuted && Position <= 0)
		{
			// Open long position at the scheduled time
			BuyMarket(Volume + Math.Abs(Position));
			_buyExecuted = true;
		}

		if (AllowSell && !_sellExecuted && Position >= 0)
		{
			// Open short position at the scheduled time
			SellMarket(Volume + Math.Abs(Position));
			_sellExecuted = true;
		}
	}
}
