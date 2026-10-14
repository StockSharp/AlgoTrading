using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR slope breakout.
/// Enters when the ATR slope exceeds its average by a standard deviation multiplier,
/// in the direction of the breakout candle. Exits when the slope returns to its average
/// or the ATR-based stop is hit.
/// </summary>
public class AtrSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevAtr;
	private decimal _stopPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period for slope statistics.
	/// </summary>
	public int SlopePeriod
	{
		get => _slopePeriod.Value;
		set => _slopePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
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
	/// Initialize <see cref="AtrSlopeBreakoutStrategy"/>.
	/// </summary>
	public AtrSlopeBreakoutStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR", "Indicators");

		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Period for slope statistics", "Strategy");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Strategy");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop ATR Multiplier", "Stop-loss distance in ATR multiples", "Risk Management");

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
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevAtr = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = SlopePeriod };
		_slopeStdDev = new StandardDeviation { Length = SlopePeriod };

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

		if (_prevAtr is not decimal prev)
		{
			_prevAtr = atrValue;
			return;
		}

		_prevAtr = atrValue;

		var slope = atrValue - prev;
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStop(candle))
			return;

		var close = candle.ClosePrice;
		var stopDistance = StopLossAtrMultiplier * atrValue;

		// ATR has no direction, so the breakout candle decides the side.
		if (slope > avgSlope + BreakoutMultiplier * stdSlope)
		{
			if (close > candle.OpenPrice && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = stopDistance > 0 ? close - stopDistance : 0m;
				return;
			}

			if (close < candle.OpenPrice && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = stopDistance > 0 ? close + stopDistance : 0m;
				return;
			}
		}

		if (Position != 0 && slope < avgSlope)
			ExitPosition();
	}

	private bool CheckStop(ICandleMessage candle)
	{
		if (_stopPrice == 0m)
			return false;

		if ((Position > 0 && candle.LowPrice <= _stopPrice) || (Position < 0 && candle.HighPrice >= _stopPrice))
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
