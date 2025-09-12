using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dskyz (DAFE) AI Adaptive Regime - Beginners Version.
/// Strategy uses EMA trend and ADX confirmation with ATR based risk management.
/// </summary>
public class DskyzAiAdaptiveRegimeBeginnersStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trailStop;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="DskyzAiAdaptiveRegimeBeginnersStrategy"/>.
	/// </summary>
	public DskyzAiAdaptiveRegimeBeginnersStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Parameters");

		_slowMaLength = Param(nameof(SlowMaLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "Parameters");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for stop distance", "Parameters");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX indicator period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for subscription", "Common");
	}

	/// <inheritdoc />
	protected override IEnumerable<(Security, DataType)> OnSubscriptionsNeeded()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, atr, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal atr, decimal adx)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trendDir = fastMa > slowMa + atr * 0.5m ? 1 : fastMa < slowMa - atr * 0.5m ? -1 : 0;

		var longCondition = trendDir == 1 && adx > 25;
		var shortCondition = trendDir == -1 && adx > 25;

		if (longCondition && Position <= 0)
		{
			var stop = candle.ClosePrice - atr * AtrMultiplier;
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			SellLimit(candle.ClosePrice + (candle.ClosePrice - stop) * 2m, volume);
			SellStop(stop, volume);
			_trailStop = stop;
		}
		else if (shortCondition && Position >= 0)
		{
			var stop = candle.ClosePrice + atr * AtrMultiplier;
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			BuyLimit(candle.ClosePrice - (stop - candle.ClosePrice) * 2m, volume);
			BuyStop(stop, volume);
			_trailStop = stop;
		}

		if (Position > 0)
		{
			var newStop = Math.Max(_trailStop, candle.ClosePrice - atr * AtrMultiplier);
			if (newStop > _trailStop)
			{
				SellStop(newStop, Position);
				_trailStop = newStop;
			}
		}
		else if (Position < 0)
		{
			var newStop = Math.Min(_trailStop, candle.ClosePrice + atr * AtrMultiplier);
			if (newStop < _trailStop)
			{
				BuyStop(newStop, Math.Abs(Position));
				_trailStop = newStop;
			}
		}
	}
}
