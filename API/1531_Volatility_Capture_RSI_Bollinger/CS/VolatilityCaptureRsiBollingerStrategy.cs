using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility capture strategy using RSI and Bollinger band logic.
/// Buys when price crosses above lower band with RSI confirmation.
/// Sells when price crosses below upper band.
/// </summary>
public class VolatilityCaptureRsiBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuy;
	private readonly StrategyParam<decimal> _rsiSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPrice;
	private decimal _prevBand;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public decimal BbWidth { get => _bbWidth.Value; set => _bbWidth.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiBuy { get => _rsiBuy.Value; set => _rsiBuy.Value = value; }
	public decimal RsiSell { get => _rsiSell.Value; set => _rsiSell.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolatilityCaptureRsiBollingerStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Bollinger SMA period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 2.7m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger band width", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiBuy = Param(nameof(RsiBuy), 55m)
			.SetDisplay("RSI Buy", "RSI above for buy signal", "Levels");

		_rsiSell = Param(nameof(RsiSell), 50m)
			.SetDisplay("RSI Sell", "RSI below for sell signal", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPrice = 0;
		_prevBand = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaLength };
		var stdDev = new StandardDeviation { Length = SmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		_prevPrice = 0;
		_prevBand = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper = smaVal + BbWidth * stdVal;
		var lower = smaVal - BbWidth * stdVal;
		var price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		// Trailing band logic
		decimal band;
		if (candle.ClosePrice > _prevBand)
			band = Math.Max(_prevBand, lower);
		else if (candle.ClosePrice < _prevBand)
			band = Math.Min(_prevBand, upper);
		else
			band = _prevBand;

		if (_prevPrice == 0)
		{
			_prevPrice = price;
			_prevBand = lower;
			return;
		}

		// Crossover signals
		var longSignal = _prevPrice <= _prevBand && price > _prevBand && rsiVal > RsiBuy;
		var shortSignal = _prevPrice >= _prevBand && price < _prevBand && rsiVal < RsiSell;

		if (longSignal && Position <= 0)
		{
			BuyMarket();
			band = lower;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket();
			band = upper;
		}

		// Exit on band cross
		if (Position > 0 && price < band)
			SellMarket();
		else if (Position < 0 && price > band)
			BuyMarket();

		_prevPrice = price;
		_prevBand = band;
	}
}
