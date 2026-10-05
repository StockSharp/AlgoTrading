using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Futures Engulfing Candle Size strategy.
/// Inside the StartHour:StartMinute..EndHour:EndMinute window (candle open time, UTC) the first candle whose high-low range reaches
/// CandleSizeThresholdTicks price steps opens one trade for the day in the direction of its body. The trade exits through a
/// take profit of TakeProfitTicks and a stop loss of StopLossTicks price steps.
/// </summary>
public class FuturesEngulfingCandleSizeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _candleSizeThresholdTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;

	private DateTime _lastTradeDay;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum candle range in price steps.
	/// </summary>
	public int CandleSizeThresholdTicks
	{
		get => _candleSizeThresholdTicks.Value;
		set => _candleSizeThresholdTicks.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Session start hour (UTC).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Session end hour (UTC).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Session end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FuturesEngulfingCandleSizeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_candleSizeThresholdTicks = Param(nameof(CandleSizeThresholdTicks), 25)
			.SetGreaterThanZero()
			.SetDisplay("Candle Size Ticks", "Minimum candle range in price steps", "Signal");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit Ticks", "Take profit in price steps", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 40)
			.SetNotNegative()
			.SetDisplay("Stop Loss Ticks", "Stop loss in price steps", "Risk");

		_startHour = Param(nameof(StartHour), 7)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour (UTC)", "Session");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_endHour = Param(nameof(EndHour), 9)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Session end hour (UTC)", "Session");

		_endMinute = Param(nameof(EndMinute), 15)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Session end minute", "Session");
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
		_lastTradeDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastTradeDay = default;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;

		StartProtection(
			new Unit(TakeProfitTicks * step, UnitTypes.Absolute),
			new Unit(StopLossTicks * step, UnitTypes.Absolute),
			useMarketOrders: true);

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

		var time = candle.OpenTime;
		var day = time.Date;

		if (_lastTradeDay == day || Position != 0)
			return;

		var minutes = time.Hour * 60 + time.Minute;
		if (minutes < StartHour * 60 + StartMinute || minutes > EndHour * 60 + EndMinute)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0)
			step = 1m;

		if ((candle.HighPrice - candle.LowPrice) / step < CandleSizeThresholdTicks)
			return;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			BuyMarket();
			_lastTradeDay = day;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			SellMarket();
			_lastTradeDay = day;
		}
	}
}
