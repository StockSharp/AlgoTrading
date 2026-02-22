using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MesaStochasticMultiLengthStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();
	private decimal _prevStoch1;
	private decimal _prevStoch2;

	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MesaStochasticMultiLengthStrategy()
	{
		_length1 = Param(nameof(Length1), 50);
		_length2 = Param(nameof(Length2), 14);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prices.Clear();
		_prevStoch1 = 0.5m;
		_prevStoch2 = 0.5m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		_prices.Add(price);

		var maxLen = Math.Max(Length1, Length2);
		if (_prices.Count > maxLen + 10)
			_prices.RemoveAt(0);

		if (_prices.Count < maxLen)
			return;

		var stoch1 = CalcStochastic(_prices, Length1);
		var stoch2 = CalcStochastic(_prices, Length2);

		var up = stoch1 > 0.5m && stoch2 > 0.5m && _prevStoch1 <= 0.5m;
		var down = stoch1 < 0.5m && stoch2 < 0.5m && _prevStoch1 >= 0.5m;

		if (up && Position <= 0)
			BuyMarket();
		else if (down && Position >= 0)
			SellMarket();

		_prevStoch1 = stoch1;
		_prevStoch2 = stoch2;
	}

	private static decimal CalcStochastic(List<decimal> prices, int length)
	{
		var count = prices.Count;
		if (count < length) return 0.5m;

		var high = decimal.MinValue;
		var low = decimal.MaxValue;
		for (int i = count - length; i < count; i++)
		{
			if (prices[i] > high) high = prices[i];
			if (prices[i] < low) low = prices[i];
		}

		if (high == low) return 0.5m;
		return (prices[count - 1] - low) / (high - low);
	}
}
