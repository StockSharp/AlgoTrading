namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Rectangle breakout strategy: detects tight consolidation ranges and trades breakouts
/// using EMA/SMA trend direction as a filter.
/// </summary>
public class RectangleTestStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rangeCandles;
	private readonly StrategyParam<decimal> _rectangleSizePercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public int RangeCandles
	{
		get => _rangeCandles.Value;
		set => _rangeCandles.Value = value;
	}

	public decimal RectangleSizePercent
	{
		get => _rectangleSizePercent.Value;
		set => _rectangleSizePercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RectangleTestStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetDisplay("Slow SMA Period", "Length of the slow SMA", "Indicators");

		_rangeCandles = Param(nameof(RangeCandles), 10)
			.SetDisplay("Rectangle Candles", "Number of candles for range detection", "Logic");

		_rectangleSizePercent = Param(nameof(RectangleSizePercent), 10m)
			.SetDisplay("Rectangle Size (%)", "Maximum range height in percent", "Logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle source", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, sma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_highs.Count >= RangeCandles)
		{
			// Use PREVIOUS window (excluding current candle) for rectangle detection
			var highestValue = decimal.MinValue;
			var lowestValue = decimal.MaxValue;
			var startIdx = _highs.Count - RangeCandles;
			for (var i = startIdx; i < _highs.Count; i++)
			{
				if (_highs[i] > highestValue) highestValue = _highs[i];
				if (_lows[i] < lowestValue) lowestValue = _lows[i];
			}

			if (highestValue > 0m && lowestValue > 0m)
			{
				var rangePercent = (highestValue - lowestValue) / highestValue * 100m;
				if (rangePercent > 0 && rangePercent < RectangleSizePercent)
				{
					var close = candle.ClosePrice;

					// Breakout above rectangle with bullish trend (EMA > SMA)
					if (close > highestValue && emaValue > smaValue && Position == 0)
					{
						BuyMarket();
					}
					// Breakout below rectangle with bearish trend (EMA < SMA)
					else if (close < lowestValue && emaValue < smaValue && Position == 0)
					{
						SellMarket();
					}
				}
			}
		}

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > RangeCandles + 1)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_highs.Clear();
		_lows.Clear();

		base.OnReseted();
	}
}
