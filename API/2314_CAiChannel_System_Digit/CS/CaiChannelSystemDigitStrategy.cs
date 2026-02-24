using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CAiChannel System Digit strategy using Bollinger Bands.
/// Buys when price returns inside from above the upper band.
/// Sells when price returns inside from below the lower band.
/// </summary>
public class CaiChannelSystemDigitStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _width;
	private readonly StrategyParam<DataType> _candle;
	private bool _prevUp;
	private bool _prevDown;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Width { get => _width.Value; set => _width.Value = value; }
	public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }

	public CaiChannelSystemDigitStrategy()
	{
		_length = Param(nameof(Length), 12).SetGreaterThanZero();
		_width = Param(nameof(Width), 2m).SetGreaterThanZero();
		_candle = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUp = false;
		_prevDown = false;

		var bb = new BollingerBands { Length = Length, Width = Width };

		var sub = SubscribeCandles(CandleType);
		sub.BindEx(bb, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bbVal is not IBollingerBandsValue bb || bb.UpBand is not decimal up || bb.LowBand is not decimal down)
			return;

		if (_prevUp && candle.ClosePrice <= up && Position <= 0)
			BuyMarket();
		else if (_prevDown && candle.ClosePrice >= down && Position >= 0)
			SellMarket();

		_prevUp = candle.ClosePrice > up;
		_prevDown = candle.ClosePrice < down;
	}
}
