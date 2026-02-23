using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Cleaner strategy: opens trades when the MACD main line rises or falls
/// during three consecutive closed candles.
/// </summary>
public class MacdCleanerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public MacdCleanerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for MACD evaluation", "General");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 15)
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
			.SetGreaterThanZero();

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 33)
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
			.SetGreaterThanZero();

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 11)
			.SetDisplay("MACD Signal", "Signal EMA length", "Indicators")
			.SetGreaterThanZero();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_macdPrev1 = null;
		_macdPrev2 = null;
		_macdPrev3 = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = MacdFastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = MacdSlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate MACD manually
		var fastInput = new DecimalIndicatorValue(_fastEma, candle.ClosePrice, candle.ServerTime) { IsFinal = true };
		var slowInput = new DecimalIndicatorValue(_slowEma, candle.ClosePrice, candle.ServerTime) { IsFinal = true };

		var fastOut = _fastEma.Process(fastInput);
		var slowOut = _slowEma.Process(slowInput);

		if (fastOut.IsEmpty || slowOut.IsEmpty || !_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		var macdValue = fastOut.ToDecimal() - slowOut.ToDecimal();

		// Shift history
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdValue;

		if (!_macdPrev3.HasValue || !_macdPrev2.HasValue || !_macdPrev1.HasValue)
			return;

		var older = _macdPrev3.Value;
		var previous = _macdPrev2.Value;
		var current = _macdPrev1.Value;

		// Three consecutive rising MACD -> buy signal
		if (older <= previous && previous <= current)
		{
			if (Position <= 0)
			{
				var vol = Volume + Math.Abs(Position);
				if (vol > 0)
					BuyMarket(vol);
			}
		}
		// Three consecutive falling MACD -> sell signal
		else if (older >= previous && previous >= current)
		{
			if (Position >= 0)
			{
				var vol = Volume + Math.Abs(Position);
				if (vol > 0)
					SellMarket(vol);
			}
		}
	}
}
