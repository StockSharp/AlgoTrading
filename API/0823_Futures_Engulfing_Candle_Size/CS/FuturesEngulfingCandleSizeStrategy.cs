using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades once per day when candle range exceeds a threshold within a time window.
/// Direction follows candle body, exits via take profit and stop loss.
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

	private bool _hasTradedToday;
	private DateTime _currentDay;
	private decimal _candleSizeThreshold;
	private decimal _takeProfit;
	private decimal _stopLoss;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle range threshold in ticks.
	/// </summary>
	public int CandleSizeThresholdTicks
	{
		get => _candleSizeThresholdTicks.Value;
		set => _candleSizeThresholdTicks.Value = value;
	}

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Trading end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Trading end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FuturesEngulfingCandleSizeStrategy"/>.
	/// </summary>
	public FuturesEngulfingCandleSizeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_candleSizeThresholdTicks = Param(nameof(CandleSizeThresholdTicks), 25)
			.SetGreaterThanZero()
			.SetDisplay("Range Ticks", "Candle range threshold", "General")
			.SetCanOptimize(true);

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Ticks", "Take profit distance", "Risk")
			.SetCanOptimize(true);

		_stopLossTicks = Param(nameof(StopLossTicks), 40)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Ticks", "Stop loss distance", "Risk")
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 7)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Trading start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Trading start minute", "Session");

		_endHour = Param(nameof(EndHour), 9)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Trading end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 15)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Trading end minute", "Session");
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

		_hasTradedToday = false;
		_currentDay = default;
		_candleSizeThreshold = _takeProfit = _stopLoss = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tick = Security?.PriceStep ?? 1m;
		_candleSizeThreshold = CandleSizeThresholdTicks * tick;
		_takeProfit = TakeProfitTicks * tick;
		_stopLoss = StopLossTicks * tick;

		StartProtection(new Unit(_takeProfit), new Unit(_stopLoss));

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var date = candle.OpenTime.Date;
		if (date != _currentDay)
		{
			_currentDay = date;
			_hasTradedToday = false;
		}

		if (_hasTradedToday || Position != 0)
			return;

		var hour = candle.OpenTime.Hour;
		var minute = candle.OpenTime.Minute;

		var afterStart = hour > StartHour || (hour == StartHour && minute >= StartMinute);
		var beforeEnd = hour < EndHour || (hour == EndHour && minute <= EndMinute);

		if (!afterStart || !beforeEnd)
			return;

		var size = candle.HighPrice - candle.LowPrice;
		if (size < _candleSizeThreshold)
			return;

		_hasTradedToday = true;

		if (candle.ClosePrice > candle.OpenPrice)
			BuyMarket();
		else if (candle.ClosePrice < candle.OpenPrice)
			SellMarket();
	}
}
