using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MeanReversionVFStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _deviation1;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _ma;
	private decimal _entryPrice;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal Deviation1 { get => _deviation1.Value; set => _deviation1.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MeanReversionVFStrategy()
	{
		_maLength = Param(nameof(MaLength), 20);
		_deviation1 = Param(nameof(Deviation1), 1.3m);
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.67m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new WeightedMovingAverage { Length = MaLength };
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma.IsFormed)
			return;

		var l1 = maValue * (1 - Deviation1 / 100m);

		if (Position > 0 && _entryPrice > 0)
		{
			var tpPrice = _entryPrice * (1 + TakeProfitPercent / 100m);
			if (candle.HighPrice >= tpPrice)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}

		if (candle.ClosePrice < l1 && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (candle.ClosePrice > maValue && Position > 0)
		{
			SellMarket();
			_entryPrice = 0;
		}
	}
}
