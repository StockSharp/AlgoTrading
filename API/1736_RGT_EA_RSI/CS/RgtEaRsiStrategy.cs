using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI and Bollinger Bands breakout with trailing stop.
/// </summary>
public class RgtEaRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiHigh;
	private readonly StrategyParam<int> _rsiLow;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;

	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }
	public int RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal MinProfit { get => _minProfit.Value; set => _minProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RgtEaRsiStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_rsiPeriod = Param(nameof(RsiPeriod), 8)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicator")
			.SetCanOptimize(true);

		_rsiHigh = Param(nameof(RsiHigh), 90)
			.SetDisplay("RSI High", "Overbought threshold", "Indicator")
			.SetCanOptimize(true);

		_rsiLow = Param(nameof(RsiLow), 10)
			.SetDisplay("RSI Low", "Oversold threshold", "Indicator")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 70m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss size in price units", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_minProfit = Param(nameof(MinProfit), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Min Profit", "Minimum profit before trailing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RSI { Length = RsiPeriod };
		var bb = new BollingerBands { Length = 20, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, bb, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			if (rsi < RsiLow && candle.ClosePrice < lower && Position <= 0)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
				return;
			}
			if (rsi > RsiHigh && candle.ClosePrice > upper && Position >= 0)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
				return;
			}
		}

		if (Position > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			var newStop = candle.ClosePrice - TrailingStop;
			if (profit > MinProfit && newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - candle.ClosePrice;
			var newStop = candle.ClosePrice + TrailingStop;
			if (profit > MinProfit && newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice >= _stopPrice)
				BuyMarket();
		}
	}
}
