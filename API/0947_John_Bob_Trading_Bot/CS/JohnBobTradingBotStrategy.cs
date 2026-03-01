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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy with fair value gap detection and ATR-based stop/target.
/// </summary>
public class JohnBobTradingBotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prev2High;
	private decimal _prev2Low;
	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barCount;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// ATR multiplier for stop-loss calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="JohnBobTradingBotStrategy"/>.
	/// </summary>
	public JohnBobTradingBotStrategy()
	{
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Mult", "ATR stop multiplier.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles.", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prev2High = 0m;
		_prev2Low = 0m;
		_highestHigh = 0m;
		_lowestLow = decimal.MaxValue;
		_barCount = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;

		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;
		if (candle.HighPrice > _highestHigh) _highestHigh = candle.HighPrice;
		if (candle.LowPrice < _lowestLow) _lowestLow = candle.LowPrice;

		if (_barCount < 50)
		{
			_prev2High = _prevHigh;
			_prev2Low = _prevLow;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		var close = candle.ClosePrice;

		// Fair value gap detection
		var fvgUp = _prev2Low > candle.HighPrice;
		var fvgDown = _prev2High < candle.LowPrice;

		// Breakout detection
		var crossUp = _prevClose <= _lowestLow && close > _lowestLow;
		var crossDown = _prevClose >= _highestHigh && close < _highestHigh;

		var buySignal = crossUp || fvgUp;
		var sellSignal = crossDown || fvgDown;

		// Exit logic
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				SellMarket();
				_stopPrice = 0m;
				_targetPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				BuyMarket();
				_stopPrice = 0m;
				_targetPrice = 0m;
			}
		}

		// Entry logic
		if (Position == 0)
		{
			if (buySignal)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - atrValue * AtrMultiplier;
				_targetPrice = close + atrValue * AtrMultiplier * 2m;
			}
			else if (sellSignal)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + atrValue * AtrMultiplier;
				_targetPrice = close - atrValue * AtrMultiplier * 2m;
			}
		}

		_prev2High = _prevHigh;
		_prev2Low = _prevLow;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = close;
	}
}
