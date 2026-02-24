using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI trend strategy with ATR trailing stop.
/// </summary>
public class RsiTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousRsi;
	private bool _isRsiInitialized;
	private decimal _stopPrice;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiBuyLevel { get => _rsiBuyLevel.Value; set => _rsiBuyLevel.Value = value; }
	public decimal RsiSellLevel { get => _rsiSellLevel.Value; set => _rsiSellLevel.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiple { get => _atrMultiple.Value; set => _atrMultiple.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiTrendStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 60m)
			.SetDisplay("RSI Buy Level", "Upper RSI barrier for long entries", "RSI Settings");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 40m)
			.SetDisplay("RSI Sell Level", "Lower RSI barrier for short entries", "RSI Settings");

		_atrPeriod = Param(nameof(AtrPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing stop", "ATR Settings");

		_atrMultiple = Param(nameof(AtrMultiple), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiple", "ATR multiplier for trailing stop", "ATR Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RSI { Length = RsiPeriod };
		var atr = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		if (!_isRsiInitialized)
		{
			_previousRsi = rsiValue;
			_isRsiInitialized = true;
			return;
		}

		var bullish = rsiValue > RsiBuyLevel && _previousRsi <= RsiBuyLevel;
		var bearish = rsiValue < RsiSellLevel && _previousRsi >= RsiSellLevel;

		if (bullish && Position <= 0)
		{
			BuyMarket();
			_stopPrice = candle.ClosePrice - atrValue * AtrMultiple;
		}
		else if (bearish && Position >= 0)
		{
			SellMarket();
			_stopPrice = candle.ClosePrice + atrValue * AtrMultiple;
		}

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - atrValue * AtrMultiple;
			if (newStop > _stopPrice)
				_stopPrice = newStop;
			if (candle.ClosePrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + atrValue * AtrMultiple;
			if (newStop < _stopPrice)
				_stopPrice = newStop;
			if (candle.ClosePrice >= _stopPrice)
				BuyMarket();
		}

		_previousRsi = rsiValue;
	}
}
