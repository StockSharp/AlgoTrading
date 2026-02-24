using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Trickerless RHMP" MetaTrader expert.
/// Uses fast/slow EMA crossover for entries.
/// Fast EMA above slow = buy, below = sell.
/// </summary>
public class TrickerlessRhmpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;

	private ExponentialMovingAverage _fastMa;
	private readonly Queue<decimal> _slowHistory = new();
	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public TrickerlessRhmpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow SMA period (computed manually)", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;
		_slowHistory.Clear();

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute slow SMA manually
		_slowHistory.Enqueue(candle.ClosePrice);
		if (_slowHistory.Count > SlowMaPeriod)
			_slowHistory.Dequeue();

		if (!_fastMa.IsFormed || _slowHistory.Count < SlowMaPeriod)
		{
			_prevFast = fastValue;
			return;
		}

		decimal sum = 0;
		foreach (var v in _slowHistory)
			sum += v;
		var slowValue = sum / SlowMaPeriod;

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var crossUp = _prevFast.Value <= _prevSlow.Value && fastValue > slowValue;
		var crossDown = _prevFast.Value >= _prevSlow.Value && fastValue < slowValue;

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
