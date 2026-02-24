using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI with Bollinger Bands strategy.
/// </summary>
public class RgtRsiBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiHigh;
	private readonly StrategyParam<int> _rsiLow;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isLong;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }
	public int RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal MinProfit { get => _minProfit.Value; set => _minProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RgtRsiBollingerStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 8)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicator");

		_rsiHigh = Param(nameof(RsiHigh), 55)
			.SetDisplay("RSI High", "Overbought RSI level", "Indicator");

		_rsiLow = Param(nameof(RsiLow), 45)
			.SetDisplay("RSI Low", "Oversold RSI level", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_minProfit = Param(nameof(MinProfit), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Min Profit", "Minimum profit before trailing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RSI { Length = RsiPeriod };
		var bb = new BollingerBands { Length = 20, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { rsi, bb }, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (values[0].IsEmpty || values[1].IsEmpty)
			return;

		var rsiValue = values[0].GetValue<decimal>();
		var bbVal = (BollingerBandsValue)values[1];

		if (bbVal.UpBand is not decimal upper ||
			bbVal.LowBand is not decimal lower)
			return;

		if (Position == 0)
		{
			if (rsiValue < RsiLow && candle.ClosePrice < lower)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
				_isLong = true;
			}
			else if (rsiValue > RsiHigh && candle.ClosePrice > upper)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
				_isLong = false;
			}
		}
		else if (_isLong && Position > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			if (profit > MinProfit)
			{
				var newStop = candle.ClosePrice - TrailingStop;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}

			if (candle.ClosePrice <= _stopPrice)
				SellMarket();
		}
		else if (!_isLong && Position < 0)
		{
			var profit = _entryPrice - candle.ClosePrice;
			if (profit > MinProfit)
			{
				var newStop = candle.ClosePrice + TrailingStop;
				if (newStop < _stopPrice)
					_stopPrice = newStop;
			}

			if (candle.ClosePrice >= _stopPrice)
				BuyMarket();
		}
	}
}
