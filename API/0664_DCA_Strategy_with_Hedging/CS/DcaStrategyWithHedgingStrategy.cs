using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dollar cost averaging strategy that adds positions at price intervals and hedges with short entries.
/// </summary>
public class DcaStrategyWithHedgingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _dcaIntervalPercent;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _initialPosition;

	private ExponentialMovingAverage _ema;

	private int _candlesAboveEma;
	private int _candlesBelowEma;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longAverageEntry;
	private decimal? _shortAverageEntry;
	private decimal _longTotalAmount;
	private decimal _shortTotalAmount;
	private int _longPositions;
	private int _shortPositions;

	/// <summary>
	/// Candle type for calculation.
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
	/// Percentage move from last entry to add a new position.
	/// </summary>
	public decimal DcaIntervalPercent
	{
		get => _dcaIntervalPercent.Value;
		set => _dcaIntervalPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from average entry price.
	/// </summary>
	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	/// <summary>
	/// Initial position size for each entry.
	/// </summary>
	public decimal InitialPosition
	{
		get => _initialPosition.Value;
		set => _initialPosition.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DcaStrategyWithHedgingStrategy"/>.
	/// </summary>
	public DcaStrategyWithHedgingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");

		_emaLength = Param(nameof(EmaLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of the EMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_dcaIntervalPercent = Param(nameof(DcaIntervalPercent), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("DCA Interval %", "Percentage move from last entry", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 5m, 0.05m);

		_tpPercent = Param(nameof(TpPercent), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Percentage gain to exit", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 5m, 0.05m);

		_initialPosition = Param(nameof(InitialPosition), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Position", "Position size for each entry", "General");
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

		_candlesAboveEma = 0;
		_candlesBelowEma = 0;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longAverageEntry = null;
		_shortAverageEntry = null;
		_longTotalAmount = 0;
		_shortTotalAmount = 0;
		_longPositions = 0;
		_shortPositions = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new() { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var close = candle.ClosePrice;

		if (close > ema)
		{
		_candlesAboveEma++;
		_candlesBelowEma = 0;
		}
		else
		{
		_candlesBelowEma++;
		_candlesAboveEma = 0;
		}

		var longCondition = _candlesAboveEma >= 3 && Position >= 0;
		if (longCondition && _longPositions == 0)
		{
		var qty = InitialPosition;
		BuyMarket(qty);
		_longEntryPrice = close;
		_longAverageEntry = close;
		_longTotalAmount = qty;
		_longPositions = 1;
		}
		else if (_longPositions > 0 && _longEntryPrice is decimal le && close < le * (1 - DcaIntervalPercent / 100m))
		{
		var qty = InitialPosition;
		var total = _longTotalAmount + qty;
		_longAverageEntry = (_longAverageEntry!.Value * _longTotalAmount + close * qty) / total;
		BuyMarket(qty);
		_longEntryPrice = close;
		_longTotalAmount = total;
		_longPositions++;
		}

		if (_longPositions > 0 && _longAverageEntry is decimal la && close > la * (1 + TpPercent / 100m))
		{
		SellMarket(Position);
		_longPositions = 0;
		_longTotalAmount = 0;
		_longAverageEntry = null;
		_longEntryPrice = null;
		}

		var shortCondition = _candlesBelowEma >= 3 && Position <= 0;
		if (shortCondition && _shortPositions == 0)
		{
		var qty = InitialPosition;
		SellMarket(qty);
		_shortEntryPrice = close;
		_shortAverageEntry = close;
		_shortTotalAmount = qty;
		_shortPositions = 1;
		}
		else if (_shortPositions > 0 && _shortEntryPrice is decimal se && close > se * (1 + DcaIntervalPercent / 100m))
		{
		var qty = InitialPosition;
		var total = _shortTotalAmount + qty;
		_shortAverageEntry = (_shortAverageEntry!.Value * _shortTotalAmount + close * qty) / total;
		SellMarket(qty);
		_shortEntryPrice = close;
		_shortTotalAmount = total;
		_shortPositions++;
		}

		if (_shortPositions > 0 && _shortAverageEntry is decimal sa && close < sa * (1 - TpPercent / 100m))
		{
		BuyMarket(-Position);
		_shortPositions = 0;
		_shortTotalAmount = 0;
		_shortAverageEntry = null;
		_shortEntryPrice = null;
		}
	}
}
