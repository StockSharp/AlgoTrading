using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MNQ strategy based on multiple EMA levels and dynamic exits.
/// </summary>
public class MNQEMAStrategy : Strategy
{
	private readonly StrategyParam<int> _ema5Length;
	private readonly StrategyParam<int> _ema13Length;
	private readonly StrategyParam<int> _ema30Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<int> _ema300Length;
	private readonly StrategyParam<decimal> _emaBuyExitThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;

	/// <summary>
	/// Initializes a new instance of <see cref="MNQEMAStrategy"/>.
	/// </summary>
	public MNQEMAStrategy()
	{
		_ema5Length = Param(nameof(Ema5Length), 5)
			.SetDisplay("EMA 5 Length", "Period for EMA 5", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 2);

		_ema13Length = Param(nameof(Ema13Length), 13)
			.SetDisplay("EMA 13 Length", "Period for EMA 13", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 2);

		_ema30Length = Param(nameof(Ema30Length), 30)
			.SetDisplay("EMA 30 Length", "Period for EMA 30", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_ema200Length = Param(nameof(Ema200Length), 200)
			.SetDisplay("EMA 200 Length", "Period for EMA 200", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 20);

		_ema300Length = Param(nameof(Ema300Length), 300)
			.SetDisplay("EMA 300 Length", "Period for EMA 300", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(200, 400, 20);

		_emaBuyExitThreshold = Param(nameof(EmaBuyExitThreshold), 92m)
			.SetDisplay("EMA Buy Exit Threshold", "Tick distance to switch exit rule", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(40m, 120m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
	}

	/// <summary>
	/// Length for EMA 5.
	/// </summary>
	public int Ema5Length { get => _ema5Length.Value; set => _ema5Length.Value = value; }

	/// <summary>
	/// Length for EMA 13.
	/// </summary>
	public int Ema13Length { get => _ema13Length.Value; set => _ema13Length.Value = value; }

	/// <summary>
	/// Length for EMA 30.
	/// </summary>
	public int Ema30Length { get => _ema30Length.Value; set => _ema30Length.Value = value; }

	/// <summary>
	/// Length for EMA 200.
	/// </summary>
	public int Ema200Length { get => _ema200Length.Value; set => _ema200Length.Value = value; }

	/// <summary>
	/// Length for EMA 300.
	/// </summary>
	public int Ema300Length { get => _ema300Length.Value; set => _ema300Length.Value = value; }

	/// <summary>
	/// Tick distance between EMA200 and EMA30 to switch exit logic.
	/// </summary>
	public decimal EmaBuyExitThreshold { get => _emaBuyExitThreshold.Value; set => _emaBuyExitThreshold.Value = value; }

	/// <summary>
	/// Type of candles used for the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema5 = new ExponentialMovingAverage { Length = Ema5Length };
		var ema13 = new ExponentialMovingAverage { Length = Ema13Length };
		var ema30 = new ExponentialMovingAverage { Length = Ema30Length };
		var ema200 = new ExponentialMovingAverage { Length = Ema200Length };
		var ema300 = new ExponentialMovingAverage { Length = Ema300Length };
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema5, ema13, ema30, ema200, ema300, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema5);
			DrawIndicator(area, ema13);
			DrawIndicator(area, ema30);
			DrawIndicator(area, ema200);
			DrawIndicator(area, ema300);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema5, decimal ema13, decimal ema30, decimal ema200, decimal ema300, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var priceStep = Security.PriceStep ?? 1m;
		var priceAboveEMA200 = candle.ClosePrice > ema200;
		var priceAboveEMA30 = candle.ClosePrice > ema30;
		var priceAboveEMA5 = candle.ClosePrice > ema5;
		var priceBelowEMA5 = candle.ClosePrice < ema5;
		var priceBelowEMA30 = candle.ClosePrice < ema30;
		var priceBelowEMA300 = candle.ClosePrice < ema300;
		var blockRsi = rsi >= 41m && rsi <= 75m;
		var candleIsGreen = candle.ClosePrice > candle.OpenPrice;
		var candleIsRed = candle.ClosePrice < candle.OpenPrice;

		if (Position == 0)
		{
		if (!blockRsi && priceAboveEMA200 && priceAboveEMA30 && priceAboveEMA5 && candleIsGreen && !(candle.LowPrice <= ema200 && candle.HighPrice >= ema200))
		{
		BuyMarket();
		}
		else if (!blockRsi && priceBelowEMA300 && priceBelowEMA30 && priceBelowEMA5 && candleIsRed && !(candle.LowPrice <= ema200 && candle.HighPrice >= ema200))
		{
		SellMarket();
		}
		}
		else if (Position > 0)
		{
		var stopPriceLong = PositionAvgPrice - 122m * priceStep;
		if (candle.LowPrice <= stopPriceLong)
		{
		SellMarket(Position);
		_highestSinceEntry = null;
		return;
		}

		var distanceBetween = Math.Abs(ema200 - ema30);
		var exitLongConditionDynamic = (distanceBetween >= EmaBuyExitThreshold * priceStep && candle.ClosePrice < ema30) ||
		(distanceBetween < EmaBuyExitThreshold * priceStep && candle.ClosePrice < ema13);
		if (exitLongConditionDynamic)
		{
		SellMarket(Position);
		_highestSinceEntry = null;
		return;
		}

		_highestSinceEntry ??= PositionAvgPrice;
		_highestSinceEntry = Math.Max(_highestSinceEntry.Value, candle.HighPrice);
		var profitTicksLong = (_highestSinceEntry.Value - PositionAvgPrice) / priceStep;
		if (profitTicksLong >= 400m)
		{
		var pullbackLevelLong = _highestSinceEntry.Value - 120m * priceStep;
		if (candle.ClosePrice <= pullbackLevelLong)
		{
		SellMarket(Position);
		_highestSinceEntry = null;
		return;
		}
		}

		var profitTicks = (candle.ClosePrice - PositionAvgPrice) / priceStep;
		if (profitTicks >= 800m && candle.ClosePrice < ema13)
		{
		SellMarket(Position);
		_highestSinceEntry = null;
		}
		}
		else
		{
		var stopPriceShort = PositionAvgPrice + 108m * priceStep;
		if (candle.HighPrice >= stopPriceShort)
		{
		BuyMarket(Math.Abs(Position));
		_lowestSinceEntry = null;
		return;
		}

		_lowestSinceEntry ??= PositionAvgPrice;
		_lowestSinceEntry = Math.Min(_lowestSinceEntry.Value, candle.LowPrice);
		var pullbackExitPrice = _lowestSinceEntry.Value + 120m * priceStep;
		if (candle.HighPrice >= pullbackExitPrice)
		{
		BuyMarket(Math.Abs(Position));
		_lowestSinceEntry = null;
		return;
		}

		if (ema300 - ema30 > 64m * priceStep)
		{
		if (candle.ClosePrice > ema30)
		{
		BuyMarket(Math.Abs(Position));
		_lowestSinceEntry = null;
		return;
		}
		}
		else
		{
		if (candle.ClosePrice > ema13)
		{
		BuyMarket(Math.Abs(Position));
		_lowestSinceEntry = null;
		return;
		}
		}

		var profitTicks = (PositionAvgPrice - candle.ClosePrice) / priceStep;
		if (profitTicks >= 800m && candle.ClosePrice > ema13)
		{
		BuyMarket(Math.Abs(Position));
		_lowestSinceEntry = null;
		}
		}
	}
}
