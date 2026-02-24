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
/// Ultimate Balance Strategy combines RSI and momentum into a weighted oscillator.
/// Opens long when the smoothed oscillator crosses above the oversold level and exits on overbought.
/// </summary>
public class UltimateBalanceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _oscillators = new();
	private decimal _prevSmoothed;
	private bool _initialized;

	public decimal OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }
	public decimal OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateBalanceStrategy()
	{
		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "Overbought threshold", "General");

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "Oversold threshold", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");

		_smoothLength = Param(nameof(SmoothLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Smoothing period for oscillator", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_oscillators.Clear();
		_prevSmoothed = 0m;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		_closes.Clear();
		_oscillators.Clear();
		_prevSmoothed = 0m;
		_initialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		// Calculate momentum component (rate of change)
		var rocLen = 20;
		decimal normRoc = 0.5m;
		if (_closes.Count > rocLen)
		{
			var prevPrice = _closes[_closes.Count - 1 - rocLen];
			if (prevPrice > 0)
			{
				var roc = (candle.ClosePrice - prevPrice) / prevPrice * 100m;
				// Normalize ROC to 0-100 range (roughly)
				normRoc = Math.Max(0, Math.Min(100, 50m + roc * 5m));
			}
		}

		// Weighted oscillator: RSI (60%) + normalized ROC (40%)
		var oscillator = rsiValue * 0.6m + normRoc * 0.4m;
		_oscillators.Add(oscillator);

		// Keep buffer
		while (_closes.Count > 200)
			_closes.RemoveAt(0);
		while (_oscillators.Count > SmoothLength + 5)
			_oscillators.RemoveAt(0);

		if (_oscillators.Count < SmoothLength)
			return;

		// Smooth the oscillator with SMA
		decimal sum = 0;
		for (int i = _oscillators.Count - SmoothLength; i < _oscillators.Count; i++)
			sum += _oscillators[i];
		var smoothed = sum / SmoothLength;

		if (!_initialized)
		{
			_prevSmoothed = smoothed;
			_initialized = true;
			return;
		}

		// Crossover signals
		var buySignal = _prevSmoothed <= OversoldLevel && smoothed > OversoldLevel;
		var sellSignal = _prevSmoothed >= OverboughtLevel && smoothed < OverboughtLevel;

		if (buySignal && Position <= 0)
			BuyMarket();

		if (sellSignal && Position >= 0)
		{
			if (EnableShort)
				SellMarket();
			else if (Position > 0)
				SellMarket();
		}

		_prevSmoothed = smoothed;
	}
}
