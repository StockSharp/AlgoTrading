using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bulls Power and Bears Power crossover.
/// Goes long when bulls overtake bears and short when bears dominate.
/// </summary>
public class BullsBearsPowerCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBulls;
	private decimal _prevBears;

	/// <summary>
	/// Indicator period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BullsBearsPowerCrossStrategy"/>.
	/// </summary>
	public BullsBearsPowerCrossStrategy()
	{
		_length = Param(nameof(Length), 13)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Indicator length", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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
		_prevBulls = 0m;
		_prevBears = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bulls = new BullsPower { Length = Length };
		var bears = new BearsPower { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(bulls, bears, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, bulls, "Bulls Power");
			DrawIndicator(area, bears, "Bears Power");
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bulls, decimal bears)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var crossUp = _prevBulls <= _prevBears && bulls > bears;
		var crossDown = _prevBulls >= _prevBears && bulls < bears;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (crossUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossDown && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevBulls = bulls;
		_prevBears = bears;
	}
}
