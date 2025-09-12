using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy that buys when SuperTrend direction flips downward and exits when it flips upward.
/// </summary>
public class TugaSupertrendStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevIsUpTrend;

	/// <summary>
	/// Start of trading window.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End of trading window.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend factor.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public TugaSupertrendStrategy()
	{
		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Start Date", "Date Window");
		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "End Date", "Date Window");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Length", "ATR length for SuperTrend", "Indicators")
			.SetCanOptimize(true);

		_factor = Param(nameof(Factor), 3m)
			.SetDisplay("Factor", "Multiplier for SuperTrend", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevIsUpTrend = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend
		{
			Length = AtrPeriod,
			Multiplier = Factor
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stValue.IsFinal)
			return;

		var st = (SuperTrendIndicatorValue)stValue;
		var isUpTrend = st.IsUpTrend;

		var inWindow = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;
		if (!inWindow)
		{
			_prevIsUpTrend = isUpTrend;
			return;
		}

		if (_prevIsUpTrend is bool prev)
		{
			var change = (isUpTrend ? 1 : -1) - (prev ? 1 : -1);

			if (change < 0 && Position <= 0)
				BuyMarket();

			if (change > 0 && Position > 0)
				SellMarket(Position);
		}

		_prevIsUpTrend = isUpTrend;
	}
}
