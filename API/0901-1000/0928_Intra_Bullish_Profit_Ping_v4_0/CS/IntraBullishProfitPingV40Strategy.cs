using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intra Bullish Strategy - Profit Ping v4.0.
/// Long only. A long opens when the short EMA crosses above the long EMA on a candle where the MACD histogram is positive, RSI is above 50
/// and the candle closes above its open. The position closes when the short EMA crosses below the long EMA with a negative histogram,
/// RSI below 50 and a candle closing below its open. There are no stops.
/// </summary>
public class IntraBullishProfitPingV40Strategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
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
	public IntraBullishProfitPingV40Strategy()
	{
		_shortEmaLength = Param(nameof(ShortEmaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA length", "EMA");

		_longEmaLength = Param(nameof(LongEmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA length", "EMA");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_macdFast = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast period", "MACD");

		_macdSlow = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow period", "MACD");

		_macdSignal = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal period", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevShort = null;
		_prevLong = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = null;
		_prevLong = null;

		var emaShort = new ExponentialMovingAverage { Length = ShortEmaLength };
		var emaLong = new ExponentialMovingAverage { Length = LongEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFastPeriod }, LongMa = { Length = MacdSlowPeriod } },
			SignalMa = { Length = MacdSignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(emaShort, emaLong, rsi, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaShortValue, IIndicatorValue emaLongValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaShortValue.IsFormed || !emaLongValue.IsFormed)
			return;

		var emaShort = emaShortValue.GetValue<decimal>();
		var emaLong = emaLongValue.GetValue<decimal>();

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = emaShort;
		_prevLong = emaLong;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!rsiValue.IsFormed || !macdValue.IsFormed || macdValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var histogram = macd - signal;

		var crossUp = ps <= pl && emaShort > emaLong;
		var crossDown = ps >= pl && emaShort < emaLong;

		if (Position <= 0 && crossUp && histogram > 0 && rsi > 50m && candle.ClosePrice > candle.OpenPrice)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && crossDown && histogram < 0 && rsi < 50m && candle.ClosePrice < candle.OpenPrice)
			SellMarket(Position);
	}
}
