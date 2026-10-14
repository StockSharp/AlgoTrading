using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Honest Volatility Grid strategy.
/// Keltner levels are built as EMA + level * Multiplier * ATR, with the ATR over the same EmaPeriod. A long opens when the close
/// reaches the LEntry1Level band and a short when it reaches the SEntry1Level band; reaching the opposite entry band closes and
/// reverses the position. A raw stop closes a long below the -RawStopLevel band and a short above the +RawStopLevel band.
/// </summary>
public class HonestVolatilityGridStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _lEntry1Level;
	private readonly StrategyParam<decimal> _sEntry1Level;
	private readonly StrategyParam<decimal> _rawStopLevel;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA and ATR period of the Keltner channel.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of one channel level.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Channel level of the long entry.
	/// </summary>
	public decimal LEntry1Level
	{
		get => _lEntry1Level.Value;
		set => _lEntry1Level.Value = value;
	}

	/// <summary>
	/// Channel level of the short entry.
	/// </summary>
	public decimal SEntry1Level
	{
		get => _sEntry1Level.Value;
		set => _sEntry1Level.Value = value;
	}

	/// <summary>
	/// Channel level of the raw stop (0 disables it).
	/// </summary>
	public decimal RawStopLevel
	{
		get => _rawStopLevel.Value;
		set => _rawStopLevel.Value = value;
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
	public HonestVolatilityGridStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA and ATR period of the Keltner channel", "Indicators");

		_multiplier = Param(nameof(Multiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier of one channel level", "Indicators");

		_lEntry1Level = Param(nameof(LEntry1Level), -2m)
			.SetDisplay("Long Entry Level", "Channel level of the long entry", "Grid");

		_sEntry1Level = Param(nameof(SEntry1Level), 2m)
			.SetDisplay("Short Entry Level", "Channel level of the short entry", "Grid");

		_rawStopLevel = Param(nameof(RawStopLevel), 20m)
			.SetNotNegative()
			.SetDisplay("Raw Stop Level", "Channel level of the raw stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = EmaPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = atr * Multiplier;
		if (step <= 0)
			return;

		var close = candle.ClosePrice;
		var longEntry = ema + LEntry1Level * step;
		var shortEntry = ema + SEntry1Level * step;

		if (RawStopLevel > 0)
		{
			if (Position > 0 && close <= ema - RawStopLevel * step)
			{
				SellMarket(Position);
				return;
			}

			if (Position < 0 && close >= ema + RawStopLevel * step)
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (close <= longEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close >= shortEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
