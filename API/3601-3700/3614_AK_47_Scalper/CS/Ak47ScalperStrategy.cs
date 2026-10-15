using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "AK-47 Scalper" MetaTrader expert.
/// Sells when price breaks below the low of the previous N candles (breakout scalp),
/// buys when price breaks above the high. Uses ATR for stop distance management.
/// </summary>
public class Ak47ScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMultiplier;

	private AverageTrueRange _atr;
	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barsCollected;
	private decimal? _entryPrice;
	private Sides? _entrySide;
	private decimal _stopDistance;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	public Ak47ScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars for high/low channel", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stop distance", "Indicators");

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "ATR multiplier for stop distance", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_highestHigh = 0;
		_lowestLow = decimal.MaxValue;
		_barsCollected = 0;
		_entryPrice = null;
		_entrySide = null;
		_stopDistance = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrValue.IsFinal)
			return;

		var atrDecimal = atrValue.IsEmpty ? 0m : atrValue.GetValue<decimal>();

		// Build lookback channel
		if (_barsCollected < LookbackPeriod)
		{
			if (candle.HighPrice > _highestHigh)
				_highestHigh = candle.HighPrice;
			if (candle.LowPrice < _lowestLow)
				_lowestLow = candle.LowPrice;
			_barsCollected++;
			return;
		}

		if (!_atr.IsFormed)
		{
			// Keep updating channel
			UpdateChannel(candle);
			return;
		}

		var close = candle.ClosePrice;
		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		_stopDistance = atrDecimal * AtrStopMultiplier;

		// Check stop loss on existing position
		if (_entryPrice != null && _entrySide != null)
		{
			if (_entrySide == Sides.Buy && close <= _entryPrice.Value - _stopDistance)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
				_entrySide = null;
			}
			else if (_entrySide == Sides.Sell && close >= _entryPrice.Value + _stopDistance)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_entrySide = null;
			}
			// Take profit at 2x ATR
			else if (_entrySide == Sides.Buy && close >= _entryPrice.Value + _stopDistance * 1.5m)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
				_entrySide = null;
			}
			else if (_entrySide == Sides.Sell && close <= _entryPrice.Value - _stopDistance * 1.5m)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_entrySide = null;
			}
		}

		// Entry signals: breakout
		if (Position == 0)
		{
			if (close > _highestHigh)
			{
				BuyMarket(volume);
				_entryPrice = close;
				_entrySide = Sides.Buy;
			}
			else if (close < _lowestLow)
			{
				SellMarket(volume);
				_entryPrice = close;
				_entrySide = Sides.Sell;
			}
		}

		UpdateChannel(candle);
	}

	private void UpdateChannel(ICandleMessage candle)
	{
		// Simple rolling update - reset and let it rebuild
		// For simplicity, just use last candle's high/low as reference shifted
		if (candle.HighPrice > _highestHigh)
			_highestHigh = candle.HighPrice;
		else
			_highestHigh = _highestHigh * 0.999m + candle.HighPrice * 0.001m; // slow decay

		if (candle.LowPrice < _lowestLow)
			_lowestLow = candle.LowPrice;
		else
			_lowestLow = _lowestLow * 0.999m + candle.LowPrice * 0.001m; // slow decay
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_atr = null;
		_highestHigh = 0;
		_lowestLow = decimal.MaxValue;
		_barsCollected = 0;
		_entryPrice = null;
		_entrySide = null;
		_stopDistance = 0;

		base.OnReseted();
	}
}
