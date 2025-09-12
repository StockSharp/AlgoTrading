using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Molly ETF EMA crossover strategy.
/// Goes long when fast EMA crosses above slow EMA and exits on opposite cross.
/// Supports optional date range filter.
/// </summary>
public class MollyEtfEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useDateFilter;
	private readonly StrategyParam<DateTime> _startDate;
	private readonly StrategyParam<DateTime> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast EMA period length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Enable trading only within specified date range.
	/// </summary>
	public bool UseDateFilter
	{
		get => _useDateFilter.Value;
		set => _useDateFilter.Value = value;
	}

	/// <summary>
	/// Start date of allowed trading period.
	/// </summary>
	public DateTime StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date of allowed trading period.
	/// </summary>
	public DateTime EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MollyEtfEmaCrossoverStrategy"/> class.
	/// </summary>
	public MollyEtfEmaCrossoverStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA", "Parameters");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow EMA", "Parameters");

		_useDateFilter = Param(nameof(UseDateFilter), true)
			.SetDisplay("Use Date Filter", "Enable date range filtering", "Date Range");

		_startDate = Param(nameof(StartDate), new DateTime(2018, 1, 1))
			.SetDisplay("Start Date", "Beginning of trading period", "Date Range");

		_endDate = Param(nameof(EndDate), new DateTime(2023, 9, 7))
			.SetDisplay("End Date", "End of trading period", "Date Range");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new EMA { Length = FastLength };
		var slowEma = new EMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		var wasFastAboveSlow = false;
		var initialized = false;
		var wasInTradeWindow = false;

		subscription
			.Bind(fastEma, slowEma, (candle, fastValue, slowValue) =>
			{
			if (candle.State != CandleStates.Finished)
			return;

			var candleTime = candle.OpenTime;
			var inTradeWindow = !UseDateFilter || (candleTime >= StartDate && candleTime < EndDate);

			if (!inTradeWindow && wasInTradeWindow)
			{
			CancelActiveOrders();
			ClosePosition();
			}

			wasInTradeWindow = inTradeWindow;

			if (!inTradeWindow)
			return;

			if (!initialized)
			{
			if (fastEma.IsFormed && slowEma.IsFormed)
			{
			wasFastAboveSlow = fastValue > slowValue;
			initialized = true;
			}

			return;
			}

			var isFastAboveSlow = fastValue > slowValue;
			var crossOver = !wasFastAboveSlow && isFastAboveSlow;
			var crossUnder = wasFastAboveSlow && !isFastAboveSlow;

			if (crossOver && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

			if (crossUnder && Position > 0)
			SellMarket(Math.Abs(Position));

			wasFastAboveSlow = isFastAboveSlow;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, fastEma);
		DrawIndicator(area, slowEma);
		DrawOwnTrades(area);
		}
	}
}
