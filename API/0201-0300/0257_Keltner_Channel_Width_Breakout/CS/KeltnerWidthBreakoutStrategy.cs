using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel width breakout.
/// Enters when the channel width exceeds its average by a standard deviation multiplier,
/// in the direction of the close relative to the channel middle (EMA).
/// Exits when the width falls back below its average or the ATR stop is hit.
/// </summary>
public class KeltnerWidthBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopMultiplier;

	private SimpleMovingAverage _widthAverage;
	private StandardDeviation _widthStdDev;
	private decimal _stopPrice;

	/// <summary>
	/// EMA period for the channel middle line.
	/// </summary>
	public int EMAPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for the channel bands.
	/// </summary>
	public int ATRPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the channel bands.
	/// </summary>
	public decimal ATRMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period for the width average and standard deviation.
	/// </summary>
	public int AvgPeriod
	{
		get => _avgPeriod.Value;
		set => _avgPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for the width breakout.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public int StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="KeltnerWidthBreakoutStrategy"/>.
	/// </summary>
	public KeltnerWidthBreakoutStrategy()
	{
		_emaPeriod = Param(nameof(EMAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators");

		_atrPeriod = Param(nameof(ATRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators");

		_atrMultiplier = Param(nameof(ATRMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators");

		_avgPeriod = Param(nameof(AvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period for width average and deviation", "Strategy");

		_multiplier = Param(nameof(Multiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Standard deviation multiplier for breakout", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopMultiplier = Param(nameof(StopMultiplier), 2)
			.SetNotNegative()
			.SetDisplay("Stop Multiplier", "Stop-loss distance in ATR multiples", "Risk Management");
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
		_widthAverage = null;
		_widthStdDev = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EMAPeriod };
		var atr = new AverageTrueRange { Length = ATRPeriod };
		_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
		_widthStdDev = new StandardDeviation { Length = AvgPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Width of the channel: (EMA + k*ATR) - (EMA - k*ATR).
		var width = 2m * ATRMultiplier * atrValue;
		var avgWidth = _widthAverage.Process(width, candle.ServerTime, true).ToDecimal();
		var stdWidth = _widthStdDev.Process(width, candle.ServerTime, true).ToDecimal();

		if (!_widthAverage.IsFormed || !_widthStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStop(candle))
			return;

		var close = candle.ClosePrice;
		var stopDistance = StopMultiplier * atrValue;

		if (width > avgWidth + Multiplier * stdWidth)
		{
			if (close > emaValue && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = StopMultiplier > 0 ? close - stopDistance : 0m;
				return;
			}

			if (close < emaValue && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = StopMultiplier > 0 ? close + stopDistance : 0m;
				return;
			}
		}

		if (Position != 0 && width < avgWidth)
			ExitPosition();
	}

	private bool CheckStop(ICandleMessage candle)
	{
		if (_stopPrice == 0m)
			return false;

		if (Position > 0 && candle.LowPrice <= _stopPrice)
		{
			ExitPosition();
			return true;
		}

		if (Position < 0 && candle.HighPrice >= _stopPrice)
		{
			ExitPosition();
			return true;
		}

		return false;
	}

	private void ExitPosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		_stopPrice = 0m;
	}
}
