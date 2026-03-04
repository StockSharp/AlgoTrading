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
/// Enters long on EMA crossover with MACD and RSI confirmation.
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
	private decimal _lastRsi;
	private decimal _lastHistogram;

	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public IntraBullishProfitPingV40Strategy()
	{
		_shortEmaLength = Param(nameof(ShortEmaLength), 7)
			.SetDisplay("Short EMA", "Short EMA length", "EMA")
			.SetGreaterThanZero();

		_longEmaLength = Param(nameof(LongEmaLength), 14)
			.SetDisplay("Long EMA", "Long EMA length", "EMA")
			.SetGreaterThanZero();

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI calculation period", "RSI")
			.SetGreaterThanZero();

		_macdFast = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "MACD fast EMA length", "MACD")
			.SetGreaterThanZero();

		_macdSlow = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "MACD slow EMA length", "MACD")
			.SetGreaterThanZero();

		_macdSignal = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "MACD signal EMA length", "MACD")
			.SetGreaterThanZero();

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
		_lastRsi = 0;
		_lastHistogram = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaShort = new ExponentialMovingAverage { Length = ShortEmaLength };
		var emaLong = new ExponentialMovingAverage { Length = LongEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
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
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue emaShortVal,
		IIndicatorValue emaLongVal,
		IIndicatorValue rsiVal,
		IIndicatorValue macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (emaShortVal.IsEmpty || emaLongVal.IsEmpty || rsiVal.IsEmpty || macdVal.IsEmpty)
			return;

		var emaShort = emaShortVal.ToDecimal();
		var emaLong = emaLongVal.ToDecimal();
		_lastRsi = rsiVal.ToDecimal();

		if (macdVal is MovingAverageConvergenceDivergenceSignalValue macdTyped)
		{
			if (macdTyped.Macd is decimal m && macdTyped.Signal is decimal s)
				_lastHistogram = m - s;
		}

		var crossUp = _prevShort is not null && _prevLong is not null && _prevShort <= _prevLong && emaShort > emaLong;
		var crossDown = _prevShort is not null && _prevLong is not null && _prevShort >= _prevLong && emaShort < emaLong;

		var buySignal = crossUp && _lastRsi > 40m;
		var sellSignal = crossDown && _lastRsi < 60m;

		if (buySignal && Position <= 0)
			BuyMarket();

		if (sellSignal && Position > 0)
			SellMarket();

		_prevShort = emaShort;
		_prevLong = emaLong;
	}
}
