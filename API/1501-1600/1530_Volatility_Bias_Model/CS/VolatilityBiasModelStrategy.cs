using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Measures directional bias over a window and trades when bias conditions align.
/// Uses EMA crossover with bias confirmation.
/// </summary>
public class VolatilityBiasModelStrategy : Strategy
{
	private readonly StrategyParam<int> _biasWindow;
	private readonly StrategyParam<decimal> _biasThreshold;
	private readonly StrategyParam<int> _maxBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private readonly Queue<bool> _biasQueue = new();
	private int _barsInPosition;
	private int _cooldown;

	public int BiasWindow { get => _biasWindow.Value; set => _biasWindow.Value = value; }
	public decimal BiasThreshold { get => _biasThreshold.Value; set => _biasThreshold.Value = value; }
	public int MaxBars { get => _maxBars.Value; set => _maxBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolatilityBiasModelStrategy()
	{
		_biasWindow = Param(nameof(BiasWindow), 10)
			.SetGreaterThanZero()
			.SetDisplay("Bias Window", "Bars for bias calculation", "Parameters");

		_biasThreshold = Param(nameof(BiasThreshold), 0.6m)
			.SetRange(0m, 1m)
			.SetDisplay("Bias Threshold", "Directional bias threshold", "Parameters");

		_maxBars = Param(nameof(MaxBars), 100)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars", "Maximum bars to hold", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_prevFast = 0;
		_prevSlow = 0;
		_biasQueue.Clear();
		_barsInPosition = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = 10 };
		var slowEma = new ExponentialMovingAverage { Length = 30 };

		_prevFast = 0;
		_prevSlow = 0;
		_biasQueue.Clear();
		_barsInPosition = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track bias in rolling window
		_biasQueue.Enqueue(candle.ClosePrice > candle.OpenPrice);
		while (_biasQueue.Count > BiasWindow)
			_biasQueue.Dequeue();

		if (_cooldown > 0)
		{
			_cooldown--;
			if (Position != 0)
				_barsInPosition++;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast == 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// Time-based exit
		if (Position != 0)
		{
			_barsInPosition++;
			if (_barsInPosition >= MaxBars)
			{
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();

				_barsInPosition = 0;
				_cooldown = 50;
				_prevFast = fast;
				_prevSlow = slow;
				return;
			}
		}

		if (_biasQueue.Count < BiasWindow)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var bullCount = 0;
		foreach (var b in _biasQueue)
			if (b) bullCount++;
		var biasRatio = (decimal)bullCount / _biasQueue.Count;

		var longCross = _prevFast <= _prevSlow && fast > slow;
		var shortCross = _prevFast >= _prevSlow && fast < slow;

		if (longCross && biasRatio >= BiasThreshold && Position <= 0)
		{
			BuyMarket();
			_barsInPosition = 0;
			_cooldown = 50;
		}
		else if (shortCross && biasRatio <= (1 - BiasThreshold) && Position >= 0)
		{
			SellMarket();
			_barsInPosition = 0;
			_cooldown = 50;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
