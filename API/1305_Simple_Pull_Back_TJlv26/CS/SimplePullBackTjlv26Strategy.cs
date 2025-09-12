using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple pull back strategy using two SMAs and RSI filter.
/// </summary>
public class SimplePullBackTjlv26Strategy : Strategy
{
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _previousLow;

	/// <summary>
	/// Long SMA period.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
	}

	/// <summary>
	/// Short SMA period.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percent from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percent from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SimplePullBackTjlv26Strategy"/>.
	/// </summary>
	public SimplePullBackTjlv26Strategy()
	{
		_longMaPeriod = Param(nameof(LongMaPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("Long MA Period", "Period of the long SMA", "Parameters");

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Short MA Period", "Period of the short SMA", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Start Date", "Start trading date", "Date Range");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("End Date", "End trading date", "Date Range");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_entryPrice = 0m;
		_previousLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var longSma = new SMA { Length = LongMaPeriod };
		var shortSma = new SMA { Length = ShortMaPeriod };
		var rsi = new RSI { Length = 3 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(longSma, shortSma, rsi, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal longSma, decimal shortSma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var candleTime = candle.OpenTime;

		if (candleTime < StartDate || candleTime > EndDate)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var prevLow = _previousLow;
		_previousLow = candle.LowPrice;

		if (Position == 0 && candle.ClosePrice > longSma && candle.ClosePrice < shortSma && rsi < 30)
		{
		_entryPrice = candle.ClosePrice;
		BuyMarket();
		}
		else if (Position > 0)
		{
		var stopPrice = _entryPrice * (1m - StopLossPercent / 100m);
		var takePrice = _entryPrice * (1m + TakeProfitPercent / 100m);

		if (candle.ClosePrice <= stopPrice || candle.ClosePrice >= takePrice)
		{
		SellMarket(Math.Abs(Position));
		}
		else if (prevLow != 0m && candle.ClosePrice > shortSma && candle.ClosePrice < prevLow)
		{
		SellMarket(Math.Abs(Position));
		}
		}
	}
}
