using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MawreezRsiDivergenceDetectorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _minDivLength;
	private readonly StrategyParam<int> _maxDivLength;

	private RelativeStrengthIndex _rsi;
	private decimal[] _priceHistory;
	private decimal[] _rsiHistory;
	private int _index;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int MinDivLength { get => _minDivLength.Value; set => _minDivLength.Value = value; }
	public int MaxDivLength { get => _maxDivLength.Value; set => _maxDivLength.Value = value; }

	public MawreezRsiDivergenceDetectorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_rsiLength = Param(nameof(RsiLength), 14);
		_minDivLength = Param(nameof(MinDivLength), 3);
		_maxDivLength = Param(nameof(MaxDivLength), 28);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_priceHistory = new decimal[MaxDivLength + 1];
		_rsiHistory = new decimal[MaxDivLength + 1];
		_index = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
			return;

		var price = candle.ClosePrice;

		var pos = _index % _priceHistory.Length;
		_priceHistory[pos] = price;
		_rsiHistory[pos] = rsi;
		_index++;

		if (_index <= MaxDivLength)
			return;

		int winner = 0;

		for (var l = MinDivLength; l <= MaxDivLength; l++)
		{
			var idx = (_index - l - 1) % _priceHistory.Length;
			if (idx < 0) idx += _priceHistory.Length;
			var pastPrice = _priceHistory[idx];
			var pastRsi = _rsiHistory[idx];

			var dsrc = price - pastPrice;
			var dosc = rsi - pastRsi;

			if (Math.Sign(dsrc) == Math.Sign(dosc))
				continue;

			if (winner == 0)
			{
				if (dsrc < 0 && dosc > 0)
					winner = 1;
				else if (dsrc > 0 && dosc < 0)
					winner = -1;
			}
		}

		if (winner > 0 && Position <= 0)
			BuyMarket();
		else if (winner < 0 && Position >= 0)
			SellMarket();
	}
}
