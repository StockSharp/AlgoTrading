using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA channel breakout strategy.
/// Buys when price breaks above EMA + offset, sells when below EMA - offset.
/// Uses trailing stop and take profit for exits.
/// </summary>
public class Binario31Strategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _channelOffset;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal ChannelOffset { get => _channelOffset.Value; set => _channelOffset.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLossVal { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Binario31Strategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicator");

		_channelOffset = Param(nameof(ChannelOffset), 50m)
			.SetDisplay("Channel Offset", "Distance from EMA for channel", "Indicator");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLossVal), 1500m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var upperBand = emaValue + ChannelOffset;
		var lowerBand = emaValue - ChannelOffset;

		// Exit management
		if (Position > 0)
		{
			var profit = close - _entryPrice;
			if ((TakeProfit > 0 && profit >= TakeProfit) || (StopLossVal > 0 && -profit >= StopLossVal))
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - close;
			if ((TakeProfit > 0 && profit >= TakeProfit) || (StopLossVal > 0 && -profit >= StopLossVal))
			{
				BuyMarket();
				return;
			}
		}

		// Entry: channel breakout
		if (Position == 0)
		{
			if (close > upperBand)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (close < lowerBand)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
	}
}
