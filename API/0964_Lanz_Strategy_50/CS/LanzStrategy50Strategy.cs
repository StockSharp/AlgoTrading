
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LANZ Strategy 5.0 - trades with EMA filter and consecutive candles.
/// </summary>
public class LanzStrategy50Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _minDistance;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;

	private decimal? _lastEntryPrice;
	private int _dailyCounter;
	private DateTime _lastDay;
	private decimal _pipSize;
	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private bool _hadPosition;
	private readonly TimeZoneInfo _nyZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	public decimal MinDistancePips { get => _minDistance.Value; set => _minDistance.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public int EndMinute { get => _endMinute.Value; set => _endMinute.Value = value; }
	public bool EnableBuy { get => _enableBuy.Value; set => _enableBuy.Value = value; }
	public bool EnableSell { get => _enableSell.Value; set => _enableSell.Value = value; }

	public LanzStrategy50Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA filter period", "Indicators")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 99)
			.SetDisplay("Max Trades", "Maximum trades per day", "Risk")
			.SetRange(1, 99);

		_minDistance = Param(nameof(MinDistancePips), 25m)
			.SetDisplay("Min Distance", "Minimum distance between entries in pips", "Risk")
			.SetNotNegative();

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 120m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetNotNegative();

		_startHour = Param(nameof(StartHour), 19)
			.SetDisplay("Start Hour", "Operational start hour NY time", "Time");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Operational start minute NY time", "Time");

		_endHour = Param(nameof(EndHour), 15)
			.SetDisplay("End Hour", "Operational end hour NY time", "Time");

		_endMinute = Param(nameof(EndMinute), 0)
			.SetDisplay("End Minute", "Operational end minute NY time", "Time");

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long trades", "Mode");

		_enableSell = Param(nameof(EnableSell), false)
			.SetDisplay("Enable Sell", "Allow short trades", "Mode");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastEntryPrice = null;
		_dailyCounter = 0;
		_lastDay = default;
		_prev1 = null;
		_prev2 = null;
		_hadPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = (Security?.PriceStep ?? 1m) * 10m;

		StartProtection(
			takeProfit: new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * _pipSize, UnitTypes.Absolute),
			useMarketOrders: true);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _nyZone);
		var today = nyTime.Date;

		if (today != _lastDay)
		{
			_dailyCounter = 0;
			_lastDay = today;
		}

		if (_hadPosition && Position == 0)
		{
			_lastEntryPrice = null;
			_hadPosition = false;
		}

		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var cur = nyTime.TimeOfDay;
		var sameDay = end > start;
		var isWithinHours = sameDay ? cur >= start && cur <= end : cur >= start || cur <= end;
		var isCloseTime = cur.Hours == EndHour && cur.Minutes == EndMinute;

		if (isCloseTime)
		{
			CloseAll();
			_lastEntryPrice = null;
			_hadPosition = false;
		}

		var distanceOk = _lastEntryPrice == null || Math.Abs(candle.ClosePrice - _lastEntryPrice.Value) >= MinDistancePips * _pipSize;
		var canOpen = _dailyCounter < MaxTrades && isWithinHours && distanceOk;

		var bullish1 = candle.ClosePrice > candle.OpenPrice;
		var bullish2 = _prev1?.ClosePrice > _prev1?.OpenPrice;
		var bullish3 = _prev2?.ClosePrice > _prev2?.OpenPrice;
		var bearish1 = candle.ClosePrice < candle.OpenPrice;
		var bearish2 = _prev1?.ClosePrice < _prev1?.OpenPrice;
		var bearish3 = _prev2?.ClosePrice < _prev2?.OpenPrice;

		var buySignal = EnableBuy && candle.ClosePrice > emaValue && bullish1 && bullish2 == true && bullish3 == true;
		var sellSignal = EnableSell && candle.ClosePrice < emaValue && bearish1 && bearish2 == true && bearish3 == true;

		if (buySignal && canOpen && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_dailyCounter++;
			_lastEntryPrice = candle.ClosePrice;
			_hadPosition = true;
		}
		else if (sellSignal && canOpen && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_dailyCounter++;
			_lastEntryPrice = candle.ClosePrice;
			_hadPosition = true;
		}

		_prev2 = _prev1;
		_prev1 = candle;
	}

	private void CloseAll()
	{
		CancelActiveOrders();
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
