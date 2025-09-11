using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OvernightPositioningEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _entry;
	private readonly StrategyParam<int> _exit;
	private readonly StrategyParam<int> _emaLen;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<DataType> _candle;
	private readonly StrategyParam<Market> _market;

	private TimeZoneInfo _tz = TimeZoneInfo.Utc;
	private TimeSpan _open;
	private TimeSpan _close;

	public int EntryMinutesBeforeClose { get => _entry.Value; set => _entry.Value = value; }
	public int ExitMinutesAfterOpen { get => _exit.Value; set => _exit.Value = value; }
	public int EmaLength { get => _emaLen.Value; set => _emaLen.Value = value; }
	public bool UseEma { get => _useEma.Value; set => _useEma.Value = value; }
	public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }
	public Market MarketSelection { get => _market.Value; set => _market.Value = value; }

	public enum Market { US, Asia, Europe }

	public OvernightPositioningEmaStrategy()
	{
		_entry = Param(nameof(EntryMinutesBeforeClose), 20).SetGreaterThanZero();
		_exit = Param(nameof(ExitMinutesAfterOpen), 20).SetGreaterThanZero();
		_emaLen = Param(nameof(EmaLength), 100).SetGreaterThanZero();
		_useEma = Param(nameof(UseEma), true);
		_candle = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
		_market = Param(nameof(MarketSelection), Market.US);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		SetupMarket();
		var ema = new EMA { Length = EmaLength };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(ema, Process).Start();
		StartProtection();
	}

	private void SetupMarket()
	{
		switch (MarketSelection)
		{
			case Market.US:
				_tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
				_open = new(9, 30, 0);
				_close = new(16, 0, 0);
				break;
			case Market.Asia:
				_tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
				_open = new(9, 0, 0);
				_close = new(15, 0, 0);
				break;
			case Market.Europe:
				_tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
				_open = new(8, 0, 0);
				_close = new(16, 30, 0);
				break;
		}
	}

	private void Process(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;
		var mt = TimeZoneInfo.ConvertTime(candle.CloseTime, _tz);
		if (mt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
			return;
		var date = mt.Date;
		var open = new DateTimeOffset(date + _open, mt.Offset);
		var close = new DateTimeOffset(date + _close, mt.Offset);
		var entry = close - TimeSpan.FromMinutes(EntryMinutesBeforeClose);
		var exit = open + TimeSpan.FromMinutes(ExitMinutesAfterOpen);
		var longOk = !UseEma || candle.ClosePrice > emaVal;
		if (mt.DayOfWeek != DayOfWeek.Friday && mt >= entry && mt < close && Position == 0 && longOk)
			BuyMarket();
		if (mt >= exit && mt < close && Position > 0)
			SellMarket(Position);
		if (mt.DayOfWeek == DayOfWeek.Friday && mt >= close - TimeSpan.FromMinutes(5) && Position > 0)
			SellMarket(Position);
	}
}
