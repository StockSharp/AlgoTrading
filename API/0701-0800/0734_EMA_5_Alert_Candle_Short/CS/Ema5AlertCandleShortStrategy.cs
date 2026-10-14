using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA 5 alert candle short strategy.
/// After at least three consecutive candles touching the EMA, a candle whose low stays above the EMA becomes the alert candle.
/// When a following candle breaks below the alert candle low a short is opened with the stop at the alert candle high and the
/// take profit at the same distance below the entry. A later candle that stays above the EMA replaces the alert candle.
/// </summary>
public class Ema5AlertCandleShortStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<DataType> _candleType;

	private int _touchCount;
	private decimal? _alertHigh;
	private decimal? _alertLow;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Risk per trade in percent of capital.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Ema5AlertCandleShortStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_riskPerTrade = Param(nameof(RiskPerTrade), 2m)
			.SetNotNegative()
			.SetDisplay("Risk Per Trade %", "Risk per trade in percent of capital", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_touchCount = 0;
		_alertHigh = null;
		_alertLow = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);

			UpdatePattern(candle, ema);
			return;
		}

		if (_alertHigh is decimal alertHigh && _alertLow is decimal alertLow && candle.LowPrice < alertLow)
		{
			var entry = candle.ClosePrice;
			var risk = alertHigh - entry;

			_alertHigh = null;
			_alertLow = null;
			_touchCount = 0;

			if (risk > 0m)
			{
				_stopPrice = alertHigh;
				_takePrice = entry - risk;
				SellMarket(Volume);
				return;
			}
		}

		UpdatePattern(candle, ema);
	}

	private void UpdatePattern(ICandleMessage candle, decimal ema)
	{
		var touches = candle.LowPrice <= ema && candle.HighPrice >= ema;

		if (touches)
		{
			// A touch after an alert candle starts a new sequence of touching candles.
			_touchCount = _alertHigh == null ? _touchCount + 1 : 1;
			_alertHigh = null;
			_alertLow = null;
		}
		else if (candle.LowPrice > ema && (_touchCount >= 3 || _alertHigh != null))
		{
			_alertHigh = candle.HighPrice;
			_alertLow = candle.LowPrice;
		}
		else
		{
			_touchCount = 0;
			_alertHigh = null;
			_alertLow = null;
		}
	}
}
