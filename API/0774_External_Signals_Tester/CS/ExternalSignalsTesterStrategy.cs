using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// External signals tester strategy.
/// </summary>
public class ExternalSignalsTesterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _closeOnReverse;
	private readonly StrategyParam<bool> _reversePosition;
	private readonly StrategyParam<bool> _useTp;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<bool> _useSl;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<bool> _useBe;
	private readonly StrategyParam<decimal> _breakevenPerc;
	private readonly StrategyParam<bool> _longCrossEnable;
	private readonly StrategyParam<int> _longCrossPeriod;
	private readonly StrategyParam<string> _longCrossDir;
	private readonly StrategyParam<bool> _shortCrossEnable;
	private readonly StrategyParam<int> _shortCrossPeriod;
	private readonly StrategyParam<string> _shortCrossDir;

	private EMA _fast;
	private EMA _slow;
	private EMA _longCross;
	private EMA _shortCross;
	private decimal _prevSignal;
	private decimal _prevClose;
	private decimal _prevLongCross;
	private decimal _prevShortCross;
	private decimal _entryPrice;
	private bool _beActivated;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}
	public bool CloseOnReverse
	{
		get => _closeOnReverse.Value;
		set => _closeOnReverse.Value = value;
	}
	public bool ReversePosition
	{
		get => _reversePosition.Value;
		set => _reversePosition.Value = value;
	}
	public bool UseTp
	{
		get => _useTp.Value;
		set => _useTp.Value = value;
	}
	public decimal TakeProfitPerc
	{
		get => _takeProfitPerc.Value;
		set => _takeProfitPerc.Value = value;
	}
	public bool UseSl
	{
		get => _useSl.Value;
		set => _useSl.Value = value;
	}
	public decimal StopLossPerc
	{
		get => _stopLossPerc.Value;
		set => _stopLossPerc.Value = value;
	}
	public bool UseBe
	{
		get => _useBe.Value;
		set => _useBe.Value = value;
	}
	public decimal BreakevenPerc
	{
		get => _breakevenPerc.Value;
		set => _breakevenPerc.Value = value;
	}
	public bool LongCrossEnable
	{
		get => _longCrossEnable.Value;
		set => _longCrossEnable.Value = value;
	}
	public int LongCrossPeriod
	{
		get => _longCrossPeriod.Value;
		set => _longCrossPeriod.Value = value;
	}
	public string LongCrossDir
	{
		get => _longCrossDir.Value;
		set => _longCrossDir.Value = value;
	}
	public bool ShortCrossEnable
	{
		get => _shortCrossEnable.Value;
		set => _shortCrossEnable.Value = value;
	}
	public int ShortCrossPeriod
	{
		get => _shortCrossPeriod.Value;
		set => _shortCrossPeriod.Value = value;
	}
	public string ShortCrossDir
	{
		get => _shortCrossDir.Value;
		set => _shortCrossDir.Value = value;
	}

	public ExternalSignalsTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Candles", "General");
		_startDate = Param(nameof(StartDate), DateTimeOffset.MinValue).SetDisplay("Start Date", "Start", "Date Range");
		_endDate = Param(nameof(EndDate), DateTimeOffset.MaxValue).SetDisplay("End Date", "End", "Date Range");
		_enableLong = Param(nameof(EnableLong), true).SetDisplay("Enable Long", "Long", "Trade Settings");
		_enableShort = Param(nameof(EnableShort), true).SetDisplay("Enable Short", "Short", "Trade Settings");
		_closeOnReverse =
			Param(nameof(CloseOnReverse), true).SetDisplay("Close On Opposite", "Reverse", "Trade Settings");
		_reversePosition =
			Param(nameof(ReversePosition), false).SetDisplay("Reverse On Opposite", "Reverse", "Trade Settings");
		_useTp = Param(nameof(UseTp), true).SetDisplay("Use TP%", "Take Profit", "Risk");
		_takeProfitPerc =
			Param(nameof(TakeProfitPerc), 2m).SetGreaterThanZero().SetDisplay("TP %", "Take Profit", "Risk");
		_useSl = Param(nameof(UseSl), true).SetDisplay("Use SL%", "Stop Loss", "Risk");
		_stopLossPerc = Param(nameof(StopLossPerc), 1m).SetGreaterThanZero().SetDisplay("SL %", "Stop Loss", "Risk");
		_useBe = Param(nameof(UseBe), true).SetDisplay("Use BE%", "Breakeven", "Risk");
		_breakevenPerc = Param(nameof(BreakevenPerc), 1m).SetGreaterThanZero().SetDisplay("BE %", "Breakeven", "Risk");
		_longCrossEnable = Param(nameof(LongCrossEnable), false).SetDisplay("Long Line Cross", "Long", "Signals");
		_longCrossPeriod =
			Param(nameof(LongCrossPeriod), 50).SetGreaterThanZero().SetDisplay("Long Line Period", "Long", "Signals");
		_longCrossDir = Param(nameof(LongCrossDir), "BelowAbove").SetDisplay("Long Dir", "Long", "Signals");
		_shortCrossEnable = Param(nameof(ShortCrossEnable), false).SetDisplay("Short Line Cross", "Short", "Signals");
		_shortCrossPeriod = Param(nameof(ShortCrossPeriod), 50)
								.SetGreaterThanZero()
								.SetDisplay("Short Line Period", "Short", "Signals");
		_shortCrossDir = Param(nameof(ShortCrossDir), "AboveBelow").SetDisplay("Short Dir", "Short", "Signals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fast?.Reset();
		_slow?.Reset();
		_longCross?.Reset();
		_shortCross?.Reset();
		_prevSignal = 0m;
		_prevClose = 0m;
		_prevLongCross = 0m;
		_prevShortCross = 0m;
		_entryPrice = 0m;
		_beActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fast = new EMA { Length = 10 };
		_slow = new EMA { Length = 30 };
		_longCross = new EMA { Length = LongCrossPeriod };
		_shortCross = new EMA { Length = ShortCrossPeriod };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_fast, _slow, _longCross, _shortCross, Process).Start();

		StartProtection(takeProfit: UseTp ? new Unit(TakeProfitPerc, UnitTypes.Percent) : null,
						stopLoss: UseSl ? new Unit(StopLossPerc, UnitTypes.Percent) : null);
	}

	private void Process(ICandleMessage c, decimal fast, decimal slow, decimal lCrossVal, decimal sCrossVal)
	{
		if (c.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var signal = fast - slow;

		var inDate = c.OpenTime >= StartDate && c.OpenTime <= EndDate;
		var longCond = signal > 0m && _prevSignal <= 0m;
		var shortCond = signal < 0m && _prevSignal >= 0m;

		if (LongCrossEnable)
		{
			if (LongCrossDir == "BelowAbove")
				longCond |= c.ClosePrice > lCrossVal && _prevClose <= _prevLongCross;
			else
				longCond |= c.ClosePrice < lCrossVal && _prevClose >= _prevLongCross;
		}

		if (ShortCrossEnable)
		{
			if (ShortCrossDir == "BelowAbove")
				shortCond |= c.ClosePrice > sCrossVal && _prevClose <= _prevShortCross;
			else
				shortCond |= c.ClosePrice < sCrossVal && _prevClose >= _prevShortCross;
		}

		if (inDate)
		{
			var reversed = false;
			if (ReversePosition && Position < 0 && longCond && EnableLong)
			{
				CancelActiveOrders();
				var vol = Volume + Math.Abs(Position);
				BuyMarket(vol);
				reversed = true;
				_entryPrice = c.ClosePrice;
				_beActivated = false;
			}
			else if (ReversePosition && Position > 0 && shortCond && EnableShort)
			{
				CancelActiveOrders();
				var vol = Volume + Math.Abs(Position);
				SellMarket(vol);
				reversed = true;
				_entryPrice = c.ClosePrice;
				_beActivated = false;
			}
			else if (!ReversePosition && CloseOnReverse)
			{
				if (Position > 0 && shortCond)
				{
					CancelActiveOrders();
					SellMarket(Position);
				}
				if (Position < 0 && longCond)
				{
					CancelActiveOrders();
					BuyMarket(-Position);
				}
			}

			if (Position == 0 && !reversed)
			{
				if (EnableLong && longCond)
				{
					BuyMarket();
					_entryPrice = c.ClosePrice;
					_beActivated = false;
				}
				if (EnableShort && shortCond)
				{
					SellMarket();
					_entryPrice = c.ClosePrice;
					_beActivated = false;
				}
			}
		}

		if (Position != 0)
		{
			var profit = (c.ClosePrice - _entryPrice) / _entryPrice * 100m * Math.Sign(Position);

			if (UseBe && !_beActivated && profit >= BreakevenPerc)
			{
				if (Position > 0)
					SellStop(_entryPrice, Position);
				else
					BuyStop(_entryPrice, -Position);
				_beActivated = true;
			}
		}
		else
		{
			_entryPrice = 0m;
			_beActivated = false;
		}

		_prevSignal = signal;
		_prevClose = c.ClosePrice;
		_prevLongCross = lCrossVal;
		_prevShortCross = sCrossVal;
	}
}
