using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner MACD strategy.
/// The Keltner Channel is the EmaPeriod EMA plus and minus Multiplier times the AtrPeriod ATR. A close above the upper band with MACD above
/// its signal line goes long and a close below the lower band with MACD below the signal line goes short, reversing an opposite position.
/// A long closes when MACD crosses below the signal line and a short when it crosses above it. The stop lies AtrMultiplier ATR from the entry
/// close and is checked on candle closes.
/// </summary>
public class KeltnerMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Period of the channel EMA.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the channel width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Period of the channel and stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public KeltnerMacdStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of the channel EMA", "Keltner");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier of the channel width", "Keltner");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the channel and stop ATR", "Keltner");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period of MACD", "MACD");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = default;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
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
			.BindEx(ema, atr, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFormed || !atrValue.IsFormed || !macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var middle = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var upper = middle + Multiplier * atr;
		var lower = middle - Multiplier * atr;
		var close = candle.ClosePrice;

		if (close > upper && macd > signal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (close < lower && macd < signal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && (macd < signal || (AtrMultiplier > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (macd > signal || (AtrMultiplier > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
