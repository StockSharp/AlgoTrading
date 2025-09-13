using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA "rainbow" with MACD and ADX filters.
/// Opens long when MACD signal is positive, all EMAs are ascending and ADX is above threshold.
/// Opens short when MACD signal is negative, all EMAs are descending and ADX is above threshold.
/// </summary>
public class MagnaRapaxCopperStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMacd;
	private readonly StrategyParam<int> _slowMacd;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema2 = null!;
	private ExponentialMovingAverage _ema3 = null!;
	private ExponentialMovingAverage _ema5 = null!;
	private ExponentialMovingAverage _ema8 = null!;
	private ExponentialMovingAverage _ema13 = null!;
	private ExponentialMovingAverage _ema21 = null!;
	private ExponentialMovingAverage _ema34 = null!;
	private ExponentialMovingAverage _ema55 = null!;
	private ExponentialMovingAverage _ema89 = null!;
	private ExponentialMovingAverage _ema144 = null!;
	private ExponentialMovingAverage _ema233 = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private AverageDirectionalIndex _adx = null!;

	/// <summary>
	/// Fast period for MACD.
	/// </summary>
	public int FastMacd { get => _fastMacd.Value; set => _fastMacd.Value = value; }

	/// <summary>
	/// Slow period for MACD.
	/// </summary>
	public int SlowMacd { get => _slowMacd.Value; set => _slowMacd.Value = value; }

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// Minimum ADX value required for trading.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public MagnaRapaxCopperStrategy()
	{
		_fastMacd = Param(nameof(FastMacd), 5)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast period for MACD", "Indicators");

		_slowMacd = Param(nameof(SlowMacd), 35)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow period for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal period for MACD", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 50m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_ema2 = new ExponentialMovingAverage { Length = 2 };
		_ema3 = new ExponentialMovingAverage { Length = 3 };
		_ema5 = new ExponentialMovingAverage { Length = 5 };
		_ema8 = new ExponentialMovingAverage { Length = 8 };
		_ema13 = new ExponentialMovingAverage { Length = 13 };
		_ema21 = new ExponentialMovingAverage { Length = 21 };
		_ema34 = new ExponentialMovingAverage { Length = 34 };
		_ema55 = new ExponentialMovingAverage { Length = 55 };
		_ema89 = new ExponentialMovingAverage { Length = 89 };
		_ema144 = new ExponentialMovingAverage { Length = 144 };
		_ema233 = new ExponentialMovingAverage { Length = 233 };

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = FastMacd,
			Slow = SlowMacd,
			Signal = SignalPeriod
		};

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([
				_ema2,
				_ema3,
				_ema5,
				_ema8,
				_ema13,
				_ema21,
				_ema34,
				_ema55,
				_ema89,
				_ema144,
				_ema233,
				_macd,
				_adx
			], ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema2);
			DrawIndicator(area, _ema233);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fma1 = values[0].ToDecimal();
		var fma2 = values[1].ToDecimal();
		var fma3 = values[2].ToDecimal();
		var fma4 = values[3].ToDecimal();
		var fma5 = values[4].ToDecimal();
		var fma6 = values[5].ToDecimal();
		var fma7 = values[6].ToDecimal();
		var fma8 = values[7].ToDecimal();
		var fma9 = values[8].ToDecimal();
		var fma10 = values[9].ToDecimal();
		var fma11 = values[10].ToDecimal();

		var macdVal = (MovingAverageConvergenceDivergenceValue)values[11];
		var signal = macdVal.Signal;
		if (signal is not decimal macdSignal)
			return;

		var adxVal = (AverageDirectionalIndexValue)values[12];
		if (adxVal.MovingAverage is not decimal adx)
			return;

		var ascending = fma1 > fma2 && fma2 > fma3 && fma3 > fma4 && fma4 > fma5 && fma5 > fma6 && fma6 > fma7 && fma7 > fma8 && fma8 > fma9 && fma9 > fma10 && fma10 > fma11;
		var descending = fma1 < fma2 && fma2 < fma3 && fma3 < fma4 && fma4 < fma5 && fma5 < fma6 && fma6 < fma7 && fma7 < fma8 && fma8 < fma9 && fma9 < fma10 && fma10 < fma11;

		if (ascending && macdSignal > 0m && adx > AdxThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (descending && macdSignal < 0m && adx > AdxThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
