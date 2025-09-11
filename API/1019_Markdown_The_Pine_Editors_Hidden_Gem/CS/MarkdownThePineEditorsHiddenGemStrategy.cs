namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger Bands breakout strategy based on a simple moving average.
/// Opens long when price closes above the upper band and short when price closes below the lower band.
/// </summary>
public class MarkdownThePineEditorsHiddenGemStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Band width multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MarkdownThePineEditorsHiddenGemStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Period of the moving average", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Band width multiplier", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var bollinger = new BollingerBands
		{
			Length = Length,
			Width = Multiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && candle.ClosePrice > upper)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && candle.ClosePrice < lower)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
