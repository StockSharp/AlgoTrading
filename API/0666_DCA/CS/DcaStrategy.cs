using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dollar cost averaging strategy.
/// </summary>
public class DcaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _toInvestQuote;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<bool> _closeAllOnLastCandle;
	private readonly StrategyParam<bool> _basedOnDayOfWeek;
	private readonly StrategyParam<int> _buyDayOfWeek;
	private readonly StrategyParam<int> _basedOnXDays;

	private int _barIndex;
	private int _lastBuyBarIndex = -1;
	private decimal _investedCapital;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Capital to invest per trade in quote asset.
	/// </summary>
	public decimal ToInvestQuote
	{
		get => _toInvestQuote.Value;
		set => _toInvestQuote.Value = value;
	}

	/// <summary>
	/// Start date.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Close all positions on last candle.
	/// </summary>
	public bool CloseAllOnLastCandle
	{
		get => _closeAllOnLastCandle.Value;
		set => _closeAllOnLastCandle.Value = value;
	}

	/// <summary>
	/// Use day of week mode.
	/// </summary>
	public bool BasedOnDayOfWeek
	{
		get => _basedOnDayOfWeek.Value;
		set => _basedOnDayOfWeek.Value = value;
	}

	/// <summary>
	/// Day of week to buy (1=Monday ... 7=Sunday).
	/// </summary>
	public int BuyDayOfWeek
	{
		get => _buyDayOfWeek.Value;
		set => _buyDayOfWeek.Value = value;
	}

	/// <summary>
	/// Buy every X candles when not using day of week mode.
	/// </summary>
	public int BasedOnXDays
	{
		get => _basedOnXDays.Value;
		set => _basedOnXDays.Value = value;
	}

	public DcaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame()).SetCanOptimize(false);
		_toInvestQuote = Param(nameof(ToInvestQuote), 100m);
		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetCanOptimize(false);
		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 0, 0, 0, TimeSpan.Zero)).SetCanOptimize(false);
		_closeAllOnLastCandle = Param(nameof(CloseAllOnLastCandle), true);
		_basedOnDayOfWeek = Param(nameof(BasedOnDayOfWeek), true);
		_buyDayOfWeek = Param(nameof(BuyDayOfWeek), 1);
		_basedOnXDays = Param(nameof(BasedOnXDays), 7);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openTime = candle.OpenTime;

		if (openTime >= EndDate)
		{
			if (CloseAllOnLastCandle && Position > 0)
				SellMarket(Position);

			_barIndex++;
			return;
		}

		if (openTime < StartDate)
		{
			_barIndex++;
			return;
		}

		var longCondition = false;

		if (BasedOnDayOfWeek)
		{
			var target = (DayOfWeek)(BuyDayOfWeek % 7);
			longCondition = openTime.DayOfWeek == target;
		}
		else
		{
			longCondition = _lastBuyBarIndex == -1 || _lastBuyBarIndex + BasedOnXDays <= _barIndex;
		}

		if (longCondition)
		{
			var qty = ToInvestQuote / candle.ClosePrice;
			BuyMarket(qty);

			_lastBuyBarIndex = _barIndex;
			_investedCapital += ToInvestQuote;
		}

		_barIndex++;
	}
}