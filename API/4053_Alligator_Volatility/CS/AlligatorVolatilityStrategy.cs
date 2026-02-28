using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alligator volatility strategy using three smoothed moving averages (Jaw, Teeth, Lips).
/// Enters long when Lips > Teeth > Jaw (uptrend expansion), short when Lips &lt; Teeth &lt; Jaw.
/// Exits when the lines converge (Alligator sleeps).
/// </summary>
public class AlligatorVolatilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _lipsPeriod;

	private int _candleCount;

	public AlligatorVolatilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetDisplay("Jaw Period", "Alligator jaw smoothing length.", "Indicators");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetDisplay("Teeth Period", "Alligator teeth smoothing length.", "Indicators");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetDisplay("Lips Period", "Alligator lips smoothing length.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candleCount = 0;

		// Use SimpleMovingAverage for Jaw/Teeth/Lips lines
		var jaw = new SimpleMovingAverage { Length = JawPeriod };
		var teeth = new SimpleMovingAverage { Length = TeethPeriod };
		var lips = new SimpleMovingAverage { Length = LipsPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jaw, teeth, lips, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawValue, decimal teethValue, decimal lipsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candleCount++;
		if (_candleCount < JawPeriod + 2)
			return;

		var close = candle.ClosePrice;

		// Alligator expansion: lips > teeth > jaw = uptrend
		var bullish = lipsValue > teethValue && teethValue > jawValue;
		// Alligator expansion: lips < teeth < jaw = downtrend
		var bearish = lipsValue < teethValue && teethValue < jawValue;

		// Exit conditions: lines converge (no longer in order)
		if (Position > 0 && !bullish)
		{
			SellMarket();
		}
		else if (Position < 0 && !bearish)
		{
			BuyMarket();
		}

		// Entry conditions
		if (Position == 0)
		{
			if (bullish && close > lipsValue)
			{
				BuyMarket();
			}
			else if (bearish && close < lipsValue)
			{
				SellMarket();
			}
		}
	}
}
