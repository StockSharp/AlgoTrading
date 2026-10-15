using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS3 Trading Robot: breakout strategy with ATR-based stops and trailing.
/// Enters on breakout above/below previous candle range with EMA filter.
/// </summary>
public class Ais3TradingRobotTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _takeMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public Ais3TradingRobotTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA period for trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_takeMultiplier = Param(nameof(TakeMultiplier), 2.0m)
			.SetDisplay("Take Multiplier", "ATR multiplier for TP.", "Risk");

		_stopMultiplier = Param(nameof(StopMultiplier), 1.5m)
			.SetDisplay("Stop Multiplier", "ATR multiplier for SL.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal TakeMultiplier
	{
		get => _takeMultiplier.Value;
		set => _takeMultiplier.Value = value;
	}

	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0;
		_prevLow = 0;
		_entryPrice = 0;
		_stopPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_entryPrice = 0;
		_stopPrice = 0;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHigh == 0 || atrVal <= 0)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		var close = candle.ClosePrice;
		var takeDistance = atrVal * TakeMultiplier;
		var stopDistance = atrVal * StopMultiplier;

		// Manage position
		if (Position > 0)
		{
			if (close - _entryPrice >= takeDistance)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			else if (_stopPrice > 0 && close <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			else
			{
				var trail = close - stopDistance;
				if (trail > _stopPrice) _stopPrice = trail;
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
				var trail = close + stopDistance;
				if (trail < _stopPrice || _stopPrice == 0) _stopPrice = trail;
			}
		}

		// Entry on breakout + EMA filter
		if (Position == 0)
		{
			if (close > _prevHigh && close > emaVal)
			{
				_entryPrice = close;
				_stopPrice = close - stopDistance;
				BuyMarket();
			}
			else if (close < _prevLow && close < emaVal)
			{
				_entryPrice = close;
				_stopPrice = close + stopDistance;
				SellMarket();
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
