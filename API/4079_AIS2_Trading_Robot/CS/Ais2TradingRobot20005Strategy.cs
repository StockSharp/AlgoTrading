using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS2 Trading Robot: range breakout strategy.
/// Enters on close above/below previous candle range midpoint,
/// uses ATR for trailing stop management.
/// </summary>
public class Ais2TradingRobot20005Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _stopFactor;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevMid;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public Ais2TradingRobot20005Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_takeFactor = Param(nameof(TakeFactor), 1.7m)
			.SetDisplay("Take Factor", "ATR multiplier for take profit.", "Risk");

		_stopFactor = Param(nameof(StopFactor), 1.0m)
			.SetDisplay("Stop Factor", "ATR multiplier for stop loss.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	public decimal StopFactor
	{
		get => _stopFactor.Value;
		set => _stopFactor.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0;
		_prevLow = 0;
		_prevMid = 0;
		_entryPrice = 0;
		_stopPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_prevMid = 0;
		_entryPrice = 0;
		_stopPrice = 0;

		var atr = new AverageTrueRange { Length = AtrLength };

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

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHigh == 0 || atrVal <= 0)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevMid = (candle.HighPrice + candle.LowPrice) / 2m;
			return;
		}

		var close = candle.ClosePrice;
		var takeDistance = atrVal * TakeFactor;
		var stopDistance = atrVal * StopFactor;

		// Manage open position
		if (Position > 0)
		{
			// Take profit
			if (close - _entryPrice >= takeDistance)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			// Stop loss
			else if (_stopPrice > 0 && close <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			// Trail stop
			else
			{
				var newStop = close - stopDistance;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - close >= takeDistance)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			else if (_stopPrice > 0 && close >= _stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			else
			{
				var newStop = close + stopDistance;
				if (newStop < _stopPrice || _stopPrice == 0)
					_stopPrice = newStop;
			}
		}

		// New entry: breakout above previous high with close above midpoint
		if (Position == 0)
		{
			if (close > _prevHigh && close > _prevMid)
			{
				_entryPrice = close;
				_stopPrice = close - stopDistance;
				BuyMarket();
			}
			else if (close < _prevLow && close < _prevMid)
			{
				_entryPrice = close;
				_stopPrice = close + stopDistance;
				SellMarket();
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevMid = (candle.HighPrice + candle.LowPrice) / 2m;
	}
}
