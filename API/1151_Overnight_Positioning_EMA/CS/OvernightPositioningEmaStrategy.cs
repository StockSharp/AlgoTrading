using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

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

	private TimeSpan _open;
	private TimeSpan _close;

	public int EntryMinutesBeforeClose { get => _entry.Value; set => _entry.Value = value; }
	public int ExitMinutesAfterOpen { get => _exit.Value; set => _exit.Value = value; }
	public int EmaLength { get => _emaLen.Value; set => _emaLen.Value = value; }
	public bool UseEma { get => _useEma.Value; set => _useEma.Value = value; }
	public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }

	public OvernightPositioningEmaStrategy()
	{
		_entry = Param(nameof(EntryMinutesBeforeClose), 20);
		_exit = Param(nameof(ExitMinutesAfterOpen), 20);
		_emaLen = Param(nameof(EmaLength), 100);
		_useEma = Param(nameof(UseEma), true);
		_candle = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_open = new(9, 30, 0);
		_close = new(16, 0, 0);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var ema = new EMA { Length = EmaLength };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(ema, Process).Start();
	}

	private void Process(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var ct = candle.CloseTime;
		var hour = ct.Hour;
		var minute = ct.Minute;
		var tod = ct.TimeOfDay;

		var closeTime = _close;
		var openTime = _open;
		var entryTime = closeTime - TimeSpan.FromMinutes(EntryMinutesBeforeClose);
		var exitTime = openTime + TimeSpan.FromMinutes(ExitMinutesAfterOpen);

		var longOk = !UseEma || candle.ClosePrice > emaVal;

		if (tod >= entryTime && tod < closeTime && Position == 0 && longOk)
			BuyMarket();

		if (tod >= exitTime && tod < closeTime && Position > 0)
			SellMarket(Position);
	}
}
