using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<int> _aoShort;
	private readonly StrategyParam<int> _aoLong;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int StochK { get => _stochK.Value; set => _stochK.Value = value; }
	public int StochD { get => _stochD.Value; set => _stochD.Value = value; }
	public int StochSmooth { get => _stochSmooth.Value; set => _stochSmooth.Value = value; }
	public int AoShort { get => _aoShort.Value; set => _aoShort.Value = value; }
	public int AoLong { get => _aoLong.Value; set => _aoLong.Value = value; }
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
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
		
		_stochSmooth = Param(nameof(StochSmooth), 14)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Smooth", "Smoothing for %K", "Indicators");
		
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
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for strategy", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
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
			KPeriod = StochK,
			DPeriod = StochD,
			Slowing = StochSmooth
		};
		var ao = new AwesomeOscillator
		{
			ShortPeriod = AoShort,
			LongPeriod = AoLong
		};
		var vwma = new VolumeWeightedMovingAverage { Length = VolumeLength };
		var sma = new SimpleMovingAverage { Length = VolumeLength };
		var jaw = new SmoothedMovingAverage { Length = 13 };
		var teeth = new SmoothedMovingAverage { Length = 8 };
		var lips = new SmoothedMovingAverage { Length = 5 };
		
		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(macd, rsi, stoch, ao, vwma, sma, jaw, teeth, lips, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, rsi);
			DrawIndicator(area, stoch);
			DrawIndicator(area, ao);
			DrawIndicator(area, vwma);
			DrawIndicator(area, sma);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue stochValue, IIndicatorValue aoValue, IIndicatorValue vwmaValue, IIndicatorValue smaValue, IIndicatorValue jawValue, IIndicatorValue teethValue, IIndicatorValue lipsValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdHist = (macdTyped.Macd - macdTyped.Signal) * 2m;
		
		var rsi = rsiValue.ToDecimal();
		var rsiHist = (rsi - 50m) / 5m;
		
		var stochTyped = (StochasticOscillatorValue)stochValue;
		var stochHist = ((stochTyped.K - stochTyped.D) / 10m) * 1.5m;
		
		var ao = aoValue.ToDecimal() * 0.6m;
		
		var vwma = vwmaValue.ToDecimal();
		var sma = smaValue.ToDecimal();
		var volumeHist = vwma - sma;
		
		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();
		var gatorHist = (lips - teeth) + (teeth - jaw);
		
		var bass = macdHist + rsiHist + stochHist + ao + gatorHist + volumeHist;
		
		if (bass > 0m && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (bass < 0m && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
