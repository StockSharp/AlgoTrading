namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy using Bollinger Bands and a simple neuron to generate signals.
/// </summary>
public class RockTraderNeuroStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _lot;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _band1;
	private decimal _band2;
	private decimal _band3;
	private decimal _band4;
	private decimal _band5;
	private decimal _band6;
	private decimal _band7;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal Lot
	{
		get => _lot.Value;
		set => _lot.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RockTraderNeuroStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_lot = Param(nameof(Lot), 1m)
			.SetDisplay("Lot", "Lots to trade", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = Lot;

		var bb = new BollingerBands
		{
			Length = 20,
			Width = 2m
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate current Bollinger Band width
		var bandWidth = middle == 0m ? 0m : (upper - lower) / middle;

		// Shift previous values to keep last seven widths
		_band7 = _band6;
		_band6 = _band5;
		_band5 = _band4;
		_band4 = _band3;
		_band3 = _band2;
		_band2 = _band1;
		_band1 = bandWidth;

		// Wait until all values are filled
		if (_band7 == 0m)
			return;

		var min = Math.Min(Math.Min(Math.Min(_band1, _band2), Math.Min(_band3, _band4)), Math.Min(_band5, Math.Min(_band6, _band7)));
		var max = Math.Max(Math.Max(Math.Max(_band1, _band2), Math.Max(_band3, _band4)), Math.Max(_band5, Math.Max(_band6, _band7)));

		if (max == min)
			return;

		const decimal d1 = -1m;
		const decimal d2 = 1m;

		decimal Normalize(decimal x) => (x - min) * (d2 - d1) / (max - min) + d1;

		var n1 = Normalize(_band1);
		var n2 = Normalize(_band2);
		var n3 = Normalize(_band3);
		var n4 = Normalize(_band4);
		var n5 = Normalize(_band5);
		var n6 = Normalize(_band6);
		var n7 = Normalize(_band7);

		// Weighted sum of inputs
		var net = n1 * 0.8m + n2 * -0.9m + n3 * 0.7m + n4 * 0.9m + n5 * -1m + n6 * 0.5m + n7 * 0m;

		// Activation using hyperbolic tangent
		var output = Tanh(net * 2m);

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				ClosePosition();
		}
		else
		{
			if (output < 0m)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
				_takePrice = _entryPrice + TakeProfit;
			}
			else if (output > 0m)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
				_takePrice = _entryPrice - TakeProfit;
			}
		}
	}

	private static decimal Tanh(decimal x)
	{
		var d = (double)x;
		var ePos = Math.Exp(d);
		var eNeg = Math.Exp(-d);
		return (decimal)((ePos - eNeg) / (ePos + eNeg));
	}
}
