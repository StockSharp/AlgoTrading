using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MarkdownThePineEditorsHiddenGemStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bollinger;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MarkdownThePineEditorsHiddenGemStrategy()
	{
		_length = Param(nameof(Length), 50);
		_multiplier = Param(nameof(Multiplier), 2m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = Length,
			Width = Multiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (Position <= 0 && candle.ClosePrice > upper)
		{
			BuyMarket();
		}
		else if (Position >= 0 && candle.ClosePrice < lower)
		{
			SellMarket();
		}
	}
}
