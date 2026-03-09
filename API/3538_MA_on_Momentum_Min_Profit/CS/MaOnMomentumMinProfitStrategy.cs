using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum crossing its own moving average strategy.
/// Converted from MetaTrader 5 (MA on Momentum Min Profit.mq5).
/// </summary>
public class MaOnMomentumMinProfitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _maPeriod;

	private Momentum _momentum;
	private readonly Queue<decimal> _momentumHistory = new();
	private decimal? _prevMomentum;
	private decimal? _prevSignal;

	/// <summary>
	/// Candle type used for signal calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period applied to momentum values.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MaOnMomentumMinProfitStrategy"/>.
	/// </summary>
	public MaOnMomentumMinProfitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for the momentum calculation", "General");

		_momentumPeriod = Param(nameof(MomentumPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback for the momentum indicator", "Momentum");

		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the moving average applied to momentum", "Momentum");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMomentum = null;
		_prevSignal = null;
		_momentumHistory.Clear();

		_momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_momentumHistory.Enqueue(momentumValue);
		while (_momentumHistory.Count > MaPeriod)
			_momentumHistory.Dequeue();

		if (!_momentum.IsFormed)
			return;

		if (_momentumHistory.Count < MaPeriod)
		{
			_prevMomentum = momentumValue;
			return;
		}

		// Calculate SMA of momentum
		var sum = 0m;
		var history = _momentumHistory.ToArray();
		foreach (var v in history)
			sum += v;
		var signalValue = sum / history.Length;

		if (_prevMomentum is null || _prevSignal is null)
		{
			_prevMomentum = momentumValue;
			_prevSignal = signalValue;
			return;
		}

		var crossUp = _prevMomentum < _prevSignal && momentumValue > signalValue;
		var crossDown = _prevMomentum > _prevSignal && momentumValue < signalValue;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var minSpread = 0.5m;

		if (crossUp && Math.Abs(momentumValue - signalValue) >= minSpread)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (crossDown && Math.Abs(momentumValue - signalValue) >= minSpread)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		_prevMomentum = momentumValue;
		_prevSignal = signalValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_prevMomentum = null;
		_prevSignal = null;
		_momentum = null;
		_momentumHistory.Clear();

		base.OnReseted();
	}
}
