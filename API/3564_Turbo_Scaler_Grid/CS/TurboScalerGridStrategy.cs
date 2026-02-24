using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Turbo Scaler Grid" MetaTrader expert.
/// Uses EMA crossover with RSI confirmation for scalping entries.
/// </summary>
public class TurboScalerGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;

	private ExponentialMovingAverage _fastEma;
	private RelativeStrengthIndex _rsi;
	private readonly Queue<decimal> _closeHistory = new();
	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	public TurboScalerGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for scalping", "General");

		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period (computed manually)", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for confirmation", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 55m)
			.SetDisplay("RSI Upper", "RSI level for buy confirmation", "Signals");

		_rsiLower = Param(nameof(RsiLower), 45m)
			.SetDisplay("RSI Lower", "RSI level for sell confirmation", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;
		_closeHistory.Clear();

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute slow SMA manually
		_closeHistory.Enqueue(candle.ClosePrice);
		if (_closeHistory.Count > SlowPeriod)
			_closeHistory.Dequeue();

		if (!_fastEma.IsFormed || !_rsi.IsFormed || _closeHistory.Count < SlowPeriod)
		{
			_prevFast = fastValue;
			return;
		}

		decimal sum = 0;
		foreach (var v in _closeHistory)
			sum += v;
		var slowValue = sum / SlowPeriod;

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

		if (crossUp && rsiValue > RsiUpper)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown && rsiValue < RsiLower)
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
