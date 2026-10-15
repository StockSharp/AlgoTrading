using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Well Martin mean reversion strategy using Bollinger Bands.
/// Buys at lower band, sells at upper band.
/// </summary>
public class WellMartinStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public WellMartinStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 84)
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 1.8m)
			.SetDisplay("Bollinger Width", "Bollinger Bands width", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 1200m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1400m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		SubscribeCandles(CandleType)
			.BindEx(bb, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (IBollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			var profit = close - _entryPrice;
			if (close >= upper || (TakeProfit > 0 && profit >= TakeProfit) || (StopLoss > 0 && -profit >= StopLoss))
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - close;
			if (close <= lower || (TakeProfit > 0 && profit >= TakeProfit) || (StopLoss > 0 && -profit >= StopLoss))
			{
				BuyMarket();
				return;
			}
		}

		if (Position != 0)
			return;

		// Mean reversion: buy at lower band, sell at upper band
		if (close < lower)
		{
			BuyMarket();
			_entryPrice = close;
		}
		else if (close > upper)
		{
			SellMarket();
			_entryPrice = close;
		}
	}
}
