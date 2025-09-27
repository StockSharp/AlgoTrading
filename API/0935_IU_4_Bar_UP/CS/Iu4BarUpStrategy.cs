using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy that buys after four consecutive bullish candles when the price is above the SuperTrend.
/// </summary>
public class Iu4BarUpStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevBull1;
	private bool _prevBull2;
	private bool _prevBull3;

	public Iu4BarUpStrategy()
	{
		_supertrendLength = Param(nameof(SupertrendLength), 14)
			.SetDisplay("SuperTrend ATR Period", "ATR period for SuperTrend", "General")
			.SetCanOptimize(true);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 1m)
			.SetDisplay("SuperTrend ATR Factor", "ATR factor for SuperTrend", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}

	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevBull1 = false;
		_prevBull2 = false;
		_prevBull3 = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend
		{
			Length = SupertrendLength,
			Multiplier = SupertrendMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(supertrend, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!stValue.IsFinal)
		return;

		var st = ((SuperTrendIndicatorValue)stValue).Value;

		var bullish = candle.ClosePrice > candle.OpenPrice;
		var fourBull = bullish && _prevBull1 && _prevBull2 && _prevBull3;

		if (Position <= 0 && fourBull && candle.ClosePrice > st)
		BuyMarket();

		if (Position > 0 && candle.ClosePrice < st)
		SellMarket(Position);

		_prevBull3 = _prevBull2;
		_prevBull2 = _prevBull1;
		_prevBull1 = bullish;
	}
}
