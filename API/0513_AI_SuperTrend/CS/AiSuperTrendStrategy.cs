using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI SuperTrend Strategy - trades SuperTrend signals combined with weighted moving averages.
/// </summary>
public class AiSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _priceWmaLength;
	private readonly StrategyParam<int> _superWmaLength;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private SuperTrend _superTrend;
	private AverageTrueRange _atr;
	private WeightedMovingAverage _priceWma;
	private WeightedMovingAverage _superWma;
	private bool _prevIsBull;
	private int _prevDirection;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR factor for SuperTrend calculation.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Weighted moving average length for price.
	/// </summary>
	public int PriceWmaLength
	{
		get => _priceWmaLength.Value;
		set => _priceWmaLength.Value = value;
	}

	/// <summary>
	/// Weighted moving average length for SuperTrend values.
	/// </summary>
	public int SuperWmaLength
	{
		get => _superWmaLength.Value;
		set => _superWmaLength.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AiSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(7, 15, 2);

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("ATR Factor", "ATR factor for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_priceWmaLength = Param(nameof(PriceWmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Price WMA Length", "WMA length for price", "AI")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);

		_superWmaLength = Param(nameof(SuperWmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend WMA Length", "WMA length for SuperTrend", "AI")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 10);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Long Trades", "Enable long entries", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Short Trades", "Enable short entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new() { Length = AtrPeriod, Multiplier = AtrFactor };
		_atr = new() { Length = AtrPeriod };
		_priceWma = new() { Length = PriceWmaLength };
		_superWma = new() { Length = SuperWmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_superTrend, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawIndicator(area, _priceWma);
			DrawIndicator(area, _superWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal superTrendValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var priceWma = _priceWma.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var superWma = _superWma.Process(superTrendValue, candle.ServerTime, true).ToDecimal();

		var isBull = priceWma > superWma;
		var direction = candle.ClosePrice > superTrendValue ? -1 : 1;
		var directionChanged = _prevDirection != 0 && direction != _prevDirection;

		var startTrendUp = isBull && !_prevIsBull;
		var startTrendDown = !isBull && _prevIsBull;
		var trendUp = directionChanged && direction < 0 && isBull;
		var trendDown = directionChanged && direction > 0 && !isBull;

		var longCondition = startTrendUp || trendUp;
		var shortCondition = startTrendDown || trendDown;

		var longExit = !(direction == -1 && isBull);
		var shortExit = !(direction == 1 && !isBull);

		var longStop = superTrendValue - atrValue * AtrFactor;
		var shortStop = superTrendValue + atrValue * AtrFactor;

		if (EnableLong && longCondition && Position <= 0)
		RegisterBuy();

		if (EnableShort && shortCondition && Position >= 0)
		RegisterSell();

		if (Position > 0 && (longExit || candle.LowPrice <= longStop))
		RegisterSell();

		if (Position < 0 && (shortExit || candle.HighPrice >= shortStop))
		RegisterBuy();

		_prevIsBull = isBull;
		_prevDirection = direction;
	}
}
