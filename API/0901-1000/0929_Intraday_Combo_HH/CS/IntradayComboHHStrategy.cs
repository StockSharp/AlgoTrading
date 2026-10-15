namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Intraday combo strategy combining Stochastic, MACD, and Bollinger Bands signals.
/// </summary>
public class IntradayComboHHStrategy : Strategy
{
	private readonly StrategyParam<int> _minSignals;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacdSignal;
	private bool _macdInitialized;

	public int MinSignals { get => _minSignals.Value; set => _minSignals.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IntradayComboHHStrategy()
	{
		_minSignals = Param(nameof(MinSignals), 2)
			.SetDisplay("Min Signals", "Minimum required conditions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_prevMacdSignal = 0;
		_macdInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacdSignal = 0;
		_macdInitialized = false;

		var stoch = new StochasticOscillator
		{
			K = { Length = 3 },
			D = { Length = 3 }
		};

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};

		var bollinger = new BollingerBands { Length = 20, Width = 2m };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stoch, macd, bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue macdValue, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var buyCount = 0;
		var sellCount = 0;

		// Stochastic signal
		if (stochValue is StochasticOscillatorValue stochTyped && stochTyped.K is decimal k)
		{
			if (k < 20)
				buyCount++;
			if (k > 80)
				sellCount++;
		}

		// MACD signal
		if (macdValue is MovingAverageConvergenceDivergenceSignalValue macdTyped && macdTyped.Signal is decimal signal)
		{
			if (_macdInitialized)
			{
				if (signal > _prevMacdSignal)
					buyCount++;
				else if (signal < _prevMacdSignal)
					sellCount++;
			}
			else
			{
				_macdInitialized = true;
			}
			_prevMacdSignal = signal;
		}

		// Bollinger Bands signal
		if (bollingerValue is BollingerBandsValue bb && bb.LowBand is decimal lower && bb.UpBand is decimal upper)
		{
			if (candle.ClosePrice < lower)
				buyCount++;
			if (candle.ClosePrice > upper)
				sellCount++;
		}

		if (buyCount >= MinSignals && Position <= 0)
			BuyMarket();
		else if (sellCount >= MinSignals && Position >= 0)
			SellMarket();
	}
}
