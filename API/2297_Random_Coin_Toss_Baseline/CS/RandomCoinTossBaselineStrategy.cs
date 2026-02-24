using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that tosses a virtual coin to decide trade direction.
/// A long position is opened on heads, a short position on tails.
/// Closes after N candles and re-enters.
/// </summary>
public class RandomCoinTossBaselineStrategy : Strategy
{
	private readonly StrategyParam<int> _holdBars;
	private readonly StrategyParam<DataType> _candleType;

	private Random _random;
	private int _barsInPosition;

	public int HoldBars
	{
		get => _holdBars.Value;
		set => _holdBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RandomCoinTossBaselineStrategy()
	{
		_holdBars = Param(nameof(HoldBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Hold Bars", "Number of bars to hold position", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = new Random(42);
		_barsInPosition = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Close position after holding for N bars
		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldBars)
			{
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();

				_barsInPosition = 0;
			}

			return;
		}

		// Flip coin and enter
		var coin = _random.Next(2);

		if (coin == 0)
			BuyMarket();
		else
			SellMarket();

		_barsInPosition = 0;
	}
}
