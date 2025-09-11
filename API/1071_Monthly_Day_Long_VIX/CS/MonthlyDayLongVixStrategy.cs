using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monthly day long strategy with VIX filter and risk management.
/// </summary>
public class MonthlyDayLongVixStrategy : Strategy
{
	private readonly StrategyParam<int> _entryDay;
	private readonly StrategyParam<int> _holdDuration;
	private readonly StrategyParam<decimal> _vixThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _vixSecurity;

	private decimal _vix;
	private int _currentMonth;
	private DateTime _entryDate;
	private int _barsInPosition;

	/// <summary>
	/// Entry day of the month.
	/// </summary>
	public int EntryDay
	{
		get => _entryDay.Value;
		set => _entryDay.Value = value;
	}

	/// <summary>
	/// Holding period in bars.
	/// </summary>
	public int HoldDuration
	{
		get => _holdDuration.Value;
		set => _holdDuration.Value = value;
	}

	/// <summary>
	/// VIX threshold.
	/// </summary>
	public decimal VixThreshold
	{
		get => _vixThreshold.Value;
		set => _vixThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// VIX security.
	/// </summary>
	public Security VixSecurity
	{
		get => _vixSecurity.Value;
		set => _vixSecurity.Value = value;
	}

	public MonthlyDayLongVixStrategy()
	{
		_entryDay = Param(nameof(EntryDay), 27)
			.SetDisplay("Entry Day", "Day of month to enter", "General");

		_holdDuration = Param(nameof(HoldDuration), 4)
			.SetGreaterThanZero()
			.SetDisplay("Hold Duration", "Bars to hold position", "General");

		_vixThreshold = Param(nameof(VixThreshold), 20m)
			.SetDisplay("VIX Threshold", "Maximum VIX value to allow entry", "VIX");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_vixSecurity = Param(nameof(VixSecurity), new Security { Id = "CBOE:VIX" })
			.SetDisplay("VIX Security", "Security representing VIX index", "VIX");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (VixSecurity, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_vix = 0m;
		_currentMonth = 0;
		_entryDate = default;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessMainCandle).Start();

		var vixSub = SubscribeCandles(CandleType, security: VixSecurity);
		vixSub.Bind(ProcessVixCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessVixCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_vix = candle.ClosePrice;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var date = candle.OpenTime.Date;

		if (_currentMonth != date.Month)
		{
			_currentMonth = date.Month;
			_entryDate = GetAdjustedDate(date);
		}

		if (Position == 0)
		{
			_barsInPosition = 0;

			if (date == _entryDate && _vix < VixThreshold)
				BuyMarket();
		}
		else
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldDuration)
				SellMarket();
		}
	}

	private DateTime GetAdjustedDate(DateTime date)
	{
		var d = new DateTime(date.Year, date.Month, EntryDay);
		return d.DayOfWeek switch
		{
			DayOfWeek.Saturday => d.AddDays(2),
			DayOfWeek.Sunday => d.AddDays(1),
			_ => d
		};
	}
}
