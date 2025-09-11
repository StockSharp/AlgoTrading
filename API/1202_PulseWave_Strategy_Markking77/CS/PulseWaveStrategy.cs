using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining VWAP, MACD crossover and RSI filter.
/// </summary>
public class PulseWaveStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	// Previous MACD minus signal to detect crossovers
	private decimal _prevMacdDiff;
	private bool _isFirst = true;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public PulseWaveStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 30, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 12, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length for RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetNotNegative()
			.SetDisplay("RSI Overbought", "Upper threshold for RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetNotNegative()
			.SetDisplay("RSI Oversold", "Lower threshold for RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
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
		var vwap = new VolumeWeightedMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, rsi, vwap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, rsi);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue vwapValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var rsi = rsiValue.ToDecimal();
		var vwap = vwapValue.ToDecimal();
		var price = candle.ClosePrice;

		var diff = macd - signal;
		var crossUp = !_isFirst && _prevMacdDiff <= 0 && diff > 0;
		var crossDown = !_isFirst && _prevMacdDiff >= 0 && diff < 0;
		_prevMacdDiff = diff;
		_isFirst = false;

		if (price > vwap && crossUp && rsi < RsiOverbought && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (price < vwap && crossDown && rsi > RsiOversold && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
