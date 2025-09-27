using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 5 EMA Strategy - stores a signal when price closes beyond the EMA and enters on breakout within the next few candles.
/// </summary>
public class FiveEmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<bool> _filterBuy;
	private readonly StrategyParam<bool> _filterSell;
	private readonly StrategyParam<decimal> _targetRR;
	private readonly StrategyParam<bool> _entryOnCloseOnly;
	private readonly StrategyParam<bool> _enableCustomExitTime;
	private readonly StrategyParam<bool> _enableBlockTradeTime;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<int> _exitMinute;
	private readonly StrategyParam<int> _blockStartHour;
	private readonly StrategyParam<int> _blockStartMinute;
	private readonly StrategyParam<int> _blockEndHour;
	private readonly StrategyParam<int> _blockEndMinute;

	private ExponentialMovingAverage _ema;

	private decimal? _signalHigh;
	private decimal? _signalLow;
	private int? _signalIndex;
	private bool _isBuySignal;
	private bool _isSellSignal;

	private int _barIndex;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	private static readonly TimeSpan _istOffset = TimeSpan.FromHours(5.5);

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Enable buy trades.
	/// </summary>
	public bool FilterBuy
	{
		get => _filterBuy.Value;
		set => _filterBuy.Value = value;
	}

	/// <summary>
	/// Enable sell trades.
	/// </summary>
	public bool FilterSell
	{
		get => _filterSell.Value;
		set => _filterSell.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal TargetRR
	{
		get => _targetRR.Value;
		set => _targetRR.Value = value;
	}

	/// <summary>
	/// Enter only on candle close.
	/// </summary>
	public bool EntryOnCloseOnly
	{
		get => _entryOnCloseOnly.Value;
		set => _entryOnCloseOnly.Value = value;
	}

	/// <summary>
	/// Enable custom exit time.
	/// </summary>
	public bool EnableCustomExitTime
	{
		get => _enableCustomExitTime.Value;
		set => _enableCustomExitTime.Value = value;
	}

	/// <summary>
	/// Enable trade block window.
	/// </summary>
	public bool EnableBlockTradeTime
	{
		get => _enableBlockTradeTime.Value;
		set => _enableBlockTradeTime.Value = value;
	}

	/// <summary>
	/// Exit hour in IST.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Exit minute in IST.
	/// </summary>
	public int ExitMinute
	{
		get => _exitMinute.Value;
		set => _exitMinute.Value = value;
	}

	/// <summary>
	/// Block start hour in IST.
	/// </summary>
	public int BlockStartHour
	{
		get => _blockStartHour.Value;
		set => _blockStartHour.Value = value;
	}

	/// <summary>
	/// Block start minute in IST.
	/// </summary>
	public int BlockStartMinute
	{
		get => _blockStartMinute.Value;
		set => _blockStartMinute.Value = value;
	}

	/// <summary>
	/// Block end hour in IST.
	/// </summary>
	public int BlockEndHour
	{
		get => _blockEndHour.Value;
		set => _blockEndHour.Value = value;
	}

	/// <summary>
	/// Block end minute in IST.
	/// </summary>
	public int BlockEndMinute
	{
		get => _blockEndMinute.Value;
		set => _blockEndMinute.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FiveEmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of EMA", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_filterBuy = Param(nameof(FilterBuy), true)
			.SetDisplay("Enable Buy", "Allow long trades", "General");

		_filterSell = Param(nameof(FilterSell), true)
			.SetDisplay("Enable Sell", "Allow short trades", "General");

		_targetRR = Param(nameof(TargetRR), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Target R:R", "Reward to risk ratio", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_entryOnCloseOnly = Param(nameof(EntryOnCloseOnly), false)
			.SetDisplay("Entry On Close", "Enter only on candle close", "General");

		_enableCustomExitTime = Param(nameof(EnableCustomExitTime), true)
			.SetDisplay("Custom Exit Time", "Enable custom exit time", "Time");

		_enableBlockTradeTime = Param(nameof(EnableBlockTradeTime), true)
			.SetDisplay("Block Trade Window", "Enable trade blocking window", "Time");

		_exitHour = Param(nameof(ExitHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "Hour to exit positions (IST)", "Time");

		_exitMinute = Param(nameof(ExitMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Exit Minute", "Minute to exit positions (IST)", "Time");

		_blockStartHour = Param(nameof(BlockStartHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Block Start Hour", "Start hour for trade block (IST)", "Time");

		_blockStartMinute = Param(nameof(BlockStartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Block Start Minute", "Start minute for trade block (IST)", "Time");

		_blockEndHour = Param(nameof(BlockEndHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Block End Hour", "End hour for trade block (IST)", "Time");

		_blockEndMinute = Param(nameof(BlockEndMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Block End Minute", "End minute for trade block (IST)", "Time");
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

		_signalHigh = null;
		_signalLow = null;
		_signalIndex = null;
		_isBuySignal = false;
		_isSellSignal = false;
		_barIndex = 0;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || !_ema.IsFormed)
			return;

		_barIndex++;

		var istTime = candle.CloseTime.ToOffset(_istOffset);
		var hour = istTime.Hour;
		var minute = istTime.Minute;

		var exitNow = EnableCustomExitTime && hour == ExitHour && minute == ExitMinute;
		var afterBlockStart = hour > BlockStartHour || (hour == BlockStartHour && minute >= BlockStartMinute);
		var beforeBlockEnd = hour < BlockEndHour || (hour == BlockEndHour && minute < BlockEndMinute);
		var inBlockZone = EnableBlockTradeTime && afterBlockStart && beforeBlockEnd;

		if (exitNow && Position != 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));
			_longStop = _longTarget = null;
			_shortStop = _shortTarget = null;
		}

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		var newBuySignal = close < emaValue && high < emaValue;
		var newSellSignal = close > emaValue && low > emaValue;

		if (newBuySignal)
		{
			_signalHigh = high;
			_signalLow = low;
			_signalIndex = _barIndex;
			_isBuySignal = true;
			_isSellSignal = false;
		}
		else if (newSellSignal)
		{
			_signalHigh = high;
			_signalLow = low;
			_signalIndex = _barIndex;
			_isBuySignal = false;
			_isSellSignal = true;
		}

		var withinWindow = _signalIndex is int idx && _barIndex > idx && _barIndex <= idx + 3;

		if (_isBuySignal && withinWindow && _signalHigh is decimal sh && high > sh && !inBlockZone)
		{
			if (FilterBuy && (!EntryOnCloseOnly || close > sh))
			{
				var entry = sh;
				var sl = _signalLow ?? low;
				var risk = entry - sl;
				_longStop = sl;
				_longTarget = entry + risk * TargetRR;
				BuyMarket(Volume + Math.Abs(Position));
				_isBuySignal = false;
				_signalHigh = _signalLow = null;
				_signalIndex = null;
			}
		}
		else if (_isSellSignal && withinWindow && _signalLow is decimal slw && low < slw && !inBlockZone)
		{
			if (FilterSell && (!EntryOnCloseOnly || close < slw))
			{
				var entry = slw;
				var sl = _signalHigh ?? high;
				var risk = sl - entry;
				_shortStop = sl;
				_shortTarget = entry - risk * TargetRR;
				SellMarket(Volume + Math.Abs(Position));
				_isSellSignal = false;
				_signalHigh = _signalLow = null;
				_signalIndex = null;
			}
		}

		if (Position > 0 && _longStop is decimal ls && _longTarget is decimal lt)
		{
			if (low <= ls || high >= lt)
			{
				SellMarket(Math.Abs(Position));
				_longStop = null;
				_longTarget = null;
			}
		}
		else if (Position < 0 && _shortStop is decimal ss && _shortTarget is decimal st)
		{
			if (high >= ss || low <= st)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
				_shortTarget = null;
			}
		}
	}
}
