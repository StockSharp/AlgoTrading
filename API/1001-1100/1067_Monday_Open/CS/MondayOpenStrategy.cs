using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monday open strategy.
/// Buys at the beginning of the week on Monday and closes the long position at Tuesday's close,
/// trading only in years from StartYear to EndYear.
/// </summary>
public class MondayOpenStrategy : Strategy
{
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<int> _endYear;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _lastEntryDate;

	/// <summary>
	/// First year in which trading is allowed.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// Last year in which trading is allowed.
	/// </summary>
	public int EndYear
	{
		get => _endYear.Value;
		set => _endYear.Value = value;
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
	/// Constructor.
	/// </summary>
	public MondayOpenStrategy()
	{
		_startYear = Param(nameof(StartYear), 2023)
			.SetDisplay("Start Year", "First year in which trading is allowed", "General");

		_endYear = Param(nameof(EndYear), 2025)
			.SetDisplay("End Year", "Last year in which trading is allowed", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_lastEntryDate = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastEntryDate = null;

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

		var openTime = candle.OpenTime;
		var closeTime = CandleType.Arg is TimeSpan frame ? openTime + frame : openTime;
		var day = openTime.DayOfWeek;

		if (Position > 0)
		{
			// Exit on the candle that completes Tuesday, or on the first later candle if that one was missing.
			var tuesdayClosed = day == DayOfWeek.Tuesday && closeTime.Date > openTime.Date;
			if (tuesdayClosed || (day != DayOfWeek.Monday && day != DayOfWeek.Tuesday))
				SellMarket(Position);

			return;
		}

		var year = openTime.Year;
		if (year < StartYear || year > EndYear)
			return;

		if (day == DayOfWeek.Monday && _lastEntryDate != openTime.Date && Position == 0)
		{
			BuyMarket(Volume);
			_lastEntryDate = openTime.Date;
		}
	}
}
