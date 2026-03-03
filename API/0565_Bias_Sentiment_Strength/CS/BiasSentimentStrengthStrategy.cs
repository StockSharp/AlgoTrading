using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bias and Sentiment Strength (BASS) strategy.
/// Combines several momentum and volume indicators.
/// Enters long when the aggregated bias is positive and short when negative.
/// </summary>
public class BiasSentimentStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _aoShort;
	private readonly StrategyParam<int> _aoLong;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _bassThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBass;

	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int StochK { get => _stochK.Value; set => _stochK.Value = value; }
	public int StochD { get => _stochD.Value; set => _stochD.Value = value; }
	public int AoShort { get => _aoShort.Value; set => _aoShort.Value = value; }
	public int AoLong { get => _aoLong.Value; set => _aoLong.Value = value; }
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal BassThreshold { get => _bassThreshold.Value; set => _bassThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BiasSentimentStrengthStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal period for MACD", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

		_stochK = Param(nameof(StochK), 21)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic K", "%K period for Stochastic", "Indicators");

		_stochD = Param(nameof(StochD), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic D", "%D period for Stochastic", "Indicators");

		_aoShort = Param(nameof(AoShort), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Short", "Short period for Awesome Oscillator", "Indicators");

		_aoLong = Param(nameof(AoLong), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Long", "Long period for Awesome Oscillator", "Indicators");

		_volumeLength = Param(nameof(VolumeLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Volume Bias Length", "Length for VWMA/SMA", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_bassThreshold = Param(nameof(BassThreshold), 70m)
			.SetGreaterThanZero()
			.SetDisplay("BASS Threshold", "Minimum BASS value for signal", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for strategy", "General");
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
		_prevBass = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stoch = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};
		var ao = new AwesomeOscillator
		{
			ShortMa = { Length = AoShort },
			LongMa = { Length = AoLong }
		};
		var vwma = new VolumeWeightedMovingAverage { Length = VolumeLength };
		var sma = new SimpleMovingAverage { Length = VolumeLength };
		var jaw = new SmoothedMovingAverage { Length = 13 };
		var teeth = new SmoothedMovingAverage { Length = 8 };
		var lips = new SmoothedMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([macd, rsi, stoch, ao, vwma, sma, jaw, teeth, lips], ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (values.Length < 9)
			return;

		for (var i = 0; i < values.Length; i++)
		{
			if (values[i] is null || !values[i].IsFinal)
				return;
		}

		decimal macdHist, rsiHist, stochHist, aoVal, volumeHist, gatorHist;

		try
		{
			if (values[0] is not IMovingAverageConvergenceDivergenceSignalValue macdTyped)
				return;

			var macdLine = macdTyped.Macd ?? 0m;
			var signalLine = macdTyped.Signal ?? 0m;
			macdHist = (macdLine - signalLine) * 2m;

			var rsi = values[1].ToDecimal();
			rsiHist = (rsi - 50m) / 5m;

			if (values[2] is not IStochasticOscillatorValue stochTyped)
				return;

			if (stochTyped.K is not decimal stochKVal || stochTyped.D is not decimal stochDVal)
				return;

			stochHist = ((stochKVal - stochDVal) / 10m) * 1.5m;

			aoVal = values[3].ToDecimal() * 0.6m;

			var vwmaVal = values[4].ToDecimal();
			var smaVal = values[5].ToDecimal();
			volumeHist = vwmaVal - smaVal;

			var jawVal = values[6].ToDecimal();
			var teethVal = values[7].ToDecimal();
			var lipsVal = values[8].ToDecimal();
			gatorHist = (lipsVal - teethVal) + (teethVal - jawVal);
		}
		catch (Exception)
		{
			return;
		}

		var bass = macdHist + rsiHist + stochHist + aoVal + gatorHist + volumeHist;

		// Trade on zero crossover
		if (bass > 0m && _prevBass <= 0m && Position <= 0)
		{
			BuyMarket();
		}
		else if (bass < 0m && _prevBass >= 0m && Position >= 0)
		{
			SellMarket();
		}

		_prevBass = bass;
	}
}
