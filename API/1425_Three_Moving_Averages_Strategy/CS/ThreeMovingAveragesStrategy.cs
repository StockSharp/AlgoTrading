using System;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossover of three simple moving averages.
/// </summary>
public class ThreeMovingAveragesStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMa;
	private readonly StrategyParam<int> _mediumMa;
	private readonly StrategyParam<int> _longMa;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevMedium;

	public int ShortMa { get => _shortMa.Value; set => _shortMa.Value = value; }
	public int MediumMa { get => _mediumMa.Value; set => _mediumMa.Value = value; }
	public int LongMa { get => _longMa.Value; set => _longMa.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThreeMovingAveragesStrategy()
	{
		_shortMa = Param(nameof(ShortMa), 20)
		.SetDisplay("Short MA", "Short moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);

		_mediumMa = Param(nameof(MediumMa), 50)
		.SetDisplay("Medium MA", "Medium moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 10);

		_longMa = Param(nameof(LongMa), 200)
		.SetDisplay("Long MA", "Long moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(100, 300, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe of working candles", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevShort = 0;
		_prevMedium = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortMa = new SimpleMovingAverage { Length = ShortMa };
		var mediumMa = new SimpleMovingAverage { Length = MediumMa };
		var longMa = new SimpleMovingAverage { Length = LongMa };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortMa, mediumMa, longMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, mediumMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal mediumValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var crossover = _prevShort <= _prevMedium && shortValue > mediumValue;
		var crossunder = _prevShort >= _prevMedium && shortValue < mediumValue;

		if (crossover && mediumValue > longValue && Position <= 0)
		BuyMarket();
		else if (crossunder && mediumValue < longValue && Position >= 0)
		SellMarket();

		_prevShort = shortValue;
		_prevMedium = mediumValue;
	}
}
