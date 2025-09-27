using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only Supertrend strategy for QQQ with date range filter.
/// </summary>
public class SupertrendLongOnlyForQqqStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private bool _isAbove;
	private bool _hasPrev;
	private decimal _prevSupertrend;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start date filter.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// End date filter.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public SupertrendLongOnlyForQqqStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 32)
			.SetDisplay("ATR Period", "ATR period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 2);

		_multiplier = Param(nameof(Multiplier), 4.35m)
			.SetDisplay("Multiplier", "ATR multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(1995, 1, 1), TimeSpan.Zero))
			.SetDisplay("Start Date", "Trading start", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2050, 1, 1), TimeSpan.Zero))
			.SetDisplay("End Date", "Trading end", "General");
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
		_isAbove = false;
		_hasPrev = false;
		_prevSupertrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper = median + Multiplier * atrValue;
		var lower = median - Multiplier * atrValue;

		if (!_hasPrev)
		{
			_prevSupertrend = candle.ClosePrice > median ? lower : upper;
			_isAbove = candle.ClosePrice > _prevSupertrend;
			_hasPrev = true;
			return;
		}

		var supertrend = _isAbove ? Math.Max(lower, _prevSupertrend) : Math.Min(upper, _prevSupertrend);
		var isAbove = candle.ClosePrice > supertrend;
		var crossedUp = isAbove && !_isAbove;
		var crossedDown = !isAbove && _isAbove;

		if (crossedUp && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (crossedDown && Position > 0)
		{
			SellMarket(Position);
		}

		_prevSupertrend = supertrend;
		_isAbove = isAbove;
	}
}
