using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Moving Average crossover on price levels.
/// It enters long when price breaks above the computed level and short when price falls below it.
/// </summary>
public class MhHullMovingAverageBasedTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHma;
	private decimal _prevPrice;

	/// <summary>
	/// Period for Hull Moving Average.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public MhHullMovingAverageBasedTradingStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 210)
			.SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevHma = default;
		_prevPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var hma = new HullMovingAverage { Length = HullPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.OpenPrice;

		if (_prevHma == 0)
		{
			_prevHma = hmaValue;
			_prevPrice = price;
			return;
		}

		var n1 = hmaValue;
		var n2 = _prevHma;
		var hullLine = n2;
		var hullRetracted = n1 > n2 ? hullLine - 2m : hullLine + 2m;
		var c1 = hullRetracted + n1 - price;
		var c2 = hullRetracted - n2 + price;

		if (price < c2 && Position > 0)
		{
			SellMarket(Position);
			LogInfo($"Exit long at {price}");
		}
		else if (price > c2 && Position < 0)
		{
			BuyMarket(-Position);
			LogInfo($"Exit short at {price}");
		}

		if (price > c2 && _prevPrice > c1 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal at {price}");
		}
		else if (price < c1 && _prevPrice < c2 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal at {price}");
		}

		_prevHma = n1;
		_prevPrice = price;
	}
}
