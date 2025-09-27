using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monthly breakout strategy using selectable months and holding period.
/// </summary>
public class MonthlyBreakoutStrategy : Strategy
{
	public enum EntryOptions
	{
		LongAtHigh,
		ShortAtHigh,
		LongAtLow,
		ShortAtLow
	}
	
	private readonly StrategyParam<EntryOptions> _entryOption;
	private readonly StrategyParam<int> _holdingPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _january;
	private readonly StrategyParam<bool> _february;
	private readonly StrategyParam<bool> _march;
	private readonly StrategyParam<bool> _april;
	private readonly StrategyParam<bool> _may;
	private readonly StrategyParam<bool> _june;
	private readonly StrategyParam<bool> _july;
	private readonly StrategyParam<bool> _august;
	private readonly StrategyParam<bool> _september;
	private readonly StrategyParam<bool> _october;
	private readonly StrategyParam<bool> _november;
	private readonly StrategyParam<bool> _december;
	
	private decimal _monthlyHigh;
	private decimal _monthlyLow;
	private decimal _prevMonthlyHigh;
	private decimal _prevMonthlyLow;
	private decimal _prevClose;
	private int _currentMonth;
	private int _barIndex;
	private int? _entryBar;
	
	public EntryOptions EntryOption
	{
		get => _entryOption.Value;
		set => _entryOption.Value = value;
	}
	
	public int HoldingPeriod
	{
		get => _holdingPeriod.Value;
		set => _holdingPeriod.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public bool January { get => _january.Value; set => _january.Value = value; }
	public bool February { get => _february.Value; set => _february.Value = value; }
	public bool March { get => _march.Value; set => _march.Value = value; }
	public bool April { get => _april.Value; set => _april.Value = value; }
	public bool May { get => _may.Value; set => _may.Value = value; }
	public bool June { get => _june.Value; set => _june.Value = value; }
	public bool July { get => _july.Value; set => _july.Value = value; }
	public bool August { get => _august.Value; set => _august.Value = value; }
	public bool September { get => _september.Value; set => _september.Value = value; }
	public bool October { get => _october.Value; set => _october.Value = value; }
	public bool November { get => _november.Value; set => _november.Value = value; }
	public bool December { get => _december.Value; set => _december.Value = value; }
	
	public MonthlyBreakoutStrategy()
	{
		_entryOption = Param(nameof(EntryOption), EntryOptions.LongAtHigh)
		.SetDisplay("Entry Option", "Breakout direction", "General");
		
		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Holding Period", "Bars to hold position", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Working candle timeframe", "General");
		
		_january = Param(nameof(January), false).SetDisplay("January", "Enable trading in January", "Months");
		_february = Param(nameof(February), false).SetDisplay("February", "Enable trading in February", "Months");
		_march = Param(nameof(March), false).SetDisplay("March", "Enable trading in March", "Months");
		_april = Param(nameof(April), false).SetDisplay("April", "Enable trading in April", "Months");
		_may = Param(nameof(May), false).SetDisplay("May", "Enable trading in May", "Months");
		_june = Param(nameof(June), false).SetDisplay("June", "Enable trading in June", "Months");
		_july = Param(nameof(July), false).SetDisplay("July", "Enable trading in July", "Months");
		_august = Param(nameof(August), false).SetDisplay("August", "Enable trading in August", "Months");
		_september = Param(nameof(September), false).SetDisplay("September", "Enable trading in September", "Months");
		_october = Param(nameof(October), false).SetDisplay("October", "Enable trading in October", "Months");
		_november = Param(nameof(November), false).SetDisplay("November", "Enable trading in November", "Months");
		_december = Param(nameof(December), false).SetDisplay("December", "Enable trading in December", "Months");
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
		_monthlyHigh = 0m;
		_monthlyLow = 0m;
		_prevMonthlyHigh = 0m;
		_prevMonthlyLow = 0m;
		_prevClose = 0m;
		_currentMonth = 0;
		_barIndex = 0;
		_entryBar = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
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
		
		_barIndex++;
		
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var month = candle.OpenTime.Month;
		
		if (month != _currentMonth)
		{
			_monthlyHigh = high;
			_monthlyLow = low;
			_prevMonthlyHigh = _monthlyHigh;
			_prevMonthlyLow = _monthlyLow;
			_currentMonth = month;
		}
		else
		{
			_prevMonthlyHigh = _monthlyHigh;
			_prevMonthlyLow = _monthlyLow;
			_monthlyHigh = Math.Max(_monthlyHigh, high);
			_monthlyLow = Math.Min(_monthlyLow, low);
		}
		
		var crossAboveHigh = _prevClose <= _prevMonthlyHigh && close > _monthlyHigh;
		var crossBelowHigh = _prevClose >= _prevMonthlyHigh && close < _monthlyHigh;
		var crossAboveLow = _prevClose <= _prevMonthlyLow && close > _monthlyLow;
		var crossBelowLow = _prevClose >= _prevMonthlyLow && close < _monthlyLow;
		
		if (Position != 0 && _entryBar.HasValue && _barIndex >= _entryBar.Value + HoldingPeriod)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));
			else
			BuyMarket(Math.Abs(Position));
			_entryBar = null;
		}
		
		if (IsMonthSelected(month))
		{
			switch (EntryOption)
			{
				case EntryOptions.LongAtHigh when crossAboveHigh && Position <= 0:
				BuyMarket(Volume + Math.Abs(Position));
				_entryBar = _barIndex;
				break;
				case EntryOptions.ShortAtHigh when crossBelowHigh && Position >= 0:
				SellMarket(Volume + Math.Abs(Position));
				_entryBar = _barIndex;
				break;
				case EntryOptions.LongAtLow when crossAboveLow && Position <= 0:
				BuyMarket(Volume + Math.Abs(Position));
				_entryBar = _barIndex;
				break;
				case EntryOptions.ShortAtLow when crossBelowLow && Position >= 0:
				SellMarket(Volume + Math.Abs(Position));
				_entryBar = _barIndex;
				break;
			}
		}
		
		_prevClose = close;
	}
	
	private bool IsMonthSelected(int month)
	{
		return month switch
		{
			1 => January,
			2 => February,
			3 => March,
			4 => April,
			5 => May,
			6 => June,
			7 => July,
			8 => August,
			9 => September,
			10 => October,
			11 => November,
			12 => December,
			_ => false
		};
	}
}
