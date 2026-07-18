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
/// Karpenko Channel strategy.
/// Generates signals based on dynamic channel and SMA baseline crossover.
/// Long when price is below channel baseline, short when above.
/// </summary>
public class KarpenkoChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _basicMa;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMa;
	private bool _initialized;
	private int _cooldownRemaining;

	/// <summary>
	/// Period for base moving average.
	/// </summary>
	public int BasicMa { get => _basicMa.Value; set => _basicMa.Value = value; }

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KarpenkoChannelStrategy()
	{
		_basicMa = Param(nameof(BasicMa), 20)
			.SetGreaterThanZero()
			.SetDisplay("Base MA", "Length of base moving average", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		var sma = new SimpleMovingAverage { Length = BasicMa };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevClose = 0m;
		_prevMa = 0m;
		_initialized = false;
		_cooldownRemaining = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = maValue;
			_initialized = true;
			return;
		}

		// Cross above MA -> buy signal
		var crossUp = _prevClose <= _prevMa && candle.ClosePrice > maValue;
		// Cross below MA -> sell signal
		var crossDown = _prevClose >= _prevMa && candle.ClosePrice < maValue;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (crossUp && _cooldownRemaining == 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (crossDown && _cooldownRemaining == 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
		_prevMa = maValue;
	}
}
