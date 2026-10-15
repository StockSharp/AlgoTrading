using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy converted from the "ACB1" MetaTrader expert advisor.
/// Enters on breakouts above previous candle high / below previous candle low,
/// with trailing stop based on ATR.
/// </summary>
public class Acb1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _trailFactor;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private decimal _prevMid;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _hasPrev;

	public Acb1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for breakout detection.", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR indicator used in trailing.", "Indicators");

		_takeFactor = Param(nameof(TakeFactor), 2m)
			.SetDisplay("Take Factor", "ATR multiplier for take profit distance.", "Execution");

		_trailFactor = Param(nameof(TrailFactor), 1.5m)
			.SetDisplay("Trail Factor", "ATR multiplier for trailing stop distance.", "Execution");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0;
		_prevLow = 0;
		_prevClose = 0;
		_prevMid = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_hasPrev = false;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_prevClose = 0;
		_prevMid = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_hasPrev = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		// Manage open position
		if (Position != 0)
		{
			if (Position > 0)
			{
				// Trailing stop for long
				var newStop = candle.ClosePrice - atrValue * TrailFactor;
				if (newStop > _stopPrice)
					_stopPrice = newStop;

				// Check stop hit
				if (candle.LowPrice <= _stopPrice)
				{
					SellMarket();
					_entryPrice = 0;
					_stopPrice = 0;
				}
				// Check take profit
				else if (_entryPrice > 0 && candle.HighPrice >= _entryPrice + atrValue * TakeFactor)
				{
					SellMarket();
					_entryPrice = 0;
					_stopPrice = 0;
				}
			}
			else
			{
				// Trailing stop for short
				var newStop = candle.ClosePrice + atrValue * TrailFactor;
				if (_stopPrice == 0 || newStop < _stopPrice)
					_stopPrice = newStop;

				// Check stop hit
				if (candle.HighPrice >= _stopPrice)
				{
					BuyMarket();
					_entryPrice = 0;
					_stopPrice = 0;
				}
				// Check take profit
				else if (_entryPrice > 0 && candle.LowPrice <= _entryPrice - atrValue * TakeFactor)
				{
					BuyMarket();
					_entryPrice = 0;
					_stopPrice = 0;
				}
			}
		}

		// Entry logic after managing position
		if (_hasPrev && Position == 0)
		{
			if (_prevClose > _prevMid && candle.ClosePrice > _prevHigh)
			{
				// Bullish breakout
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _prevLow;
			}
			else if (_prevClose < _prevMid && candle.ClosePrice < _prevLow)
			{
				// Bearish breakout
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _prevHigh;
			}
		}

		// Store for next candle
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
		_prevMid = (candle.HighPrice + candle.LowPrice) / 2m;
		_hasPrev = true;
	}
}
