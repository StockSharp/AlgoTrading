namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Vela Superada Strategy.
/// A bearish candle followed by a bullish one that closes above the prior open, with both closes above the EMA, RSI below 65
/// and a rising MACD line, is a long signal. The mirrored pattern below the EMA with RSI above 35 and a falling MACD line is
/// a short signal. ShowLong and ShowShort enable each side; an opposite signal closes or reverses the position. A trailing
/// SlPercent stop and a TpPercent take profit protect the trade.
/// </summary>
public class VelaSuperadaStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal? _prevMacd;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	/// <summary>
	/// Trailing stop loss percentage.
	/// </summary>
	public decimal SlPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
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
	public VelaSuperadaStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long Trades", "Allow long trades", "Trading");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short Trades", "Allow short trades", "Trading");

		_tpPercent = Param(nameof(TpPercent), 1.2m)
			.SetNotNegative()
			.SetDisplay("TP %", "Take profit percentage from entry price", "Risk");

		_slPercent = Param(nameof(SlPercent), 1.8m)
			.SetNotNegative()
			.SetDisplay("SL %", "Trailing stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCandle = null;
		_prevMacd = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_prevMacd = null;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, rsi, macd, ProcessCandle)
			.Start();

		StartProtection(
			TpPercent > 0m ? new Unit(TpPercent, UnitTypes.Percent) : new Unit(),
			SlPercent > 0m ? new Unit(SlPercent, UnitTypes.Percent) : new Unit(),
			isStopTrailing: true,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevCandle = _prevCandle;
		_prevCandle = candle;

		if (!macdValue.IsFormed)
			return;

		var macd = macdValue.GetValue<decimal>();
		var prevMacd = _prevMacd;
		_prevMacd = macd;

		if (prevCandle == null || prevMacd is not decimal previousMacd || !emaValue.IsFormed || !rsiValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var prevClose = prevCandle.ClosePrice;

		var longSignal = prevCandle.ClosePrice < prevCandle.OpenPrice && close > candle.OpenPrice && close > prevCandle.OpenPrice
			&& close > ema && prevClose > ema && rsi < 65m && macd > previousMacd;

		var shortSignal = prevCandle.ClosePrice > prevCandle.OpenPrice && close < candle.OpenPrice && close < prevCandle.OpenPrice
			&& close < ema && prevClose < ema && rsi > 35m && macd < previousMacd;

		if (longSignal)
		{
			if (ShowLong && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(-Position);
		}
		else if (shortSignal)
		{
			if (ShowShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}
}
