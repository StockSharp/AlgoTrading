using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opens a long trade at the start of the week and exits on Tuesday.
/// </summary>
public class MondayOpenStrategy : Strategy
{
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<int> _endYear;
	private readonly StrategyParam<DataType> _candleType;

	private bool _tradeOpened;

	/// <summary>
	/// First year to trade.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// Last year to trade.
	/// </summary>
	public int EndYear
	{
		get => _endYear.Value;
		set => _endYear.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref=\"MondayOpenStrategy\"/> class.
	/// </summary>
	public MondayOpenStrategy()
	{
		_startYear = Param(nameof(StartYear), 2023)
			.SetDisplay(\"Start Year\", \"First year to trade\", \"General\")
			.SetCanOptimize(true)
			.SetRange(1900, 2100);

		_endYear = Param(nameof(EndYear), 2025)
			.SetDisplay(\"End Year\", \"Last year to trade\", \"General\")
			.SetCanOptimize(true)
			.SetRange(1900, 2100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay(\"Candle Type\", \"Type of candles to use\", \"General\");
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
		_tradeOpened = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var year = candle.OpenTime.Year;
		if (year < StartYear || year > EndYear)
			return;

		var day = candle.OpenTime.DayOfWeek;

		if (day == DayOfWeek.Monday && !_tradeOpened)
		{
			BuyMarket();
			_tradeOpened = true;
		}
		else if (day == DayOfWeek.Tuesday && _tradeOpened && Position > 0)
		{
			ClosePosition();
			_tradeOpened = false;
		}
	}
}
