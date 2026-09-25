using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R reversal strategy.
/// </summary>
public class VladoStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<ICandleMessage> _candles = new();

	public int WilliamsPeriod { get => _williamsPeriod.Value; set => _williamsPeriod.Value = value; }
	public decimal OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }
	public decimal OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VladoStrategy()
	{
		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams Period", "Williams %R lookback.", "Indicator");
		_overboughtLevel = Param(nameof(OverboughtLevel), -25m)
			.SetDisplay("Overbought Level", "Williams %R short threshold.", "Signal");
		_oversoldLevel = Param(nameof(OversoldLevel), -75m)
			.SetDisplay("Oversold Level", "Williams %R long threshold.", "Signal");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Enqueue(candle);
		while (_candles.Count > WilliamsPeriod)
			_candles.Dequeue();

		if (_candles.Count < WilliamsPeriod)
			return;

		var highest = _candles.Max(c => c.HighPrice);
		var lowest = _candles.Min(c => c.LowPrice);
		var value = highest == lowest
			? -50m
			: -100m * (highest - candle.ClosePrice) / (highest - lowest);

		var signal = GetSignal(value, OversoldLevel, OverboughtLevel);

		if (signal > 0 && Position <= 0m)
		{
			LogInfo("Williams %R {0}: LONG", value);
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (signal < 0 && Position >= 0m)
		{
			LogInfo("Williams %R {0}: SHORT", value);
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	internal static int GetSignal(decimal williamsR, decimal oversoldLevel, decimal overboughtLevel)
	{
		if (williamsR < oversoldLevel)
			return 1;

		if (williamsR > overboughtLevel)
			return -1;

		return 0;
	}
}
