using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 5-minute RSI qualified strategy.
/// Counts consecutive candles in RSI extreme zones.
/// Buys after sustained oversold, sells after sustained overbought.
/// </summary>
public class FiveMinRsiQualifiedStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _qualificationLength;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private int _overboughtCount;
	private int _oversoldCount;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int QualificationLength { get => _qualificationLength.Value; set => _qualificationLength.Value = value; }
	public decimal UpperThreshold { get => _upperThreshold.Value; set => _upperThreshold.Value = value; }
	public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FiveMinRsiQualifiedStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");

		_qualificationLength = Param(nameof(QualificationLength), 3)
			.SetDisplay("Qual Length", "Consecutive candles in extreme zone", "Indicators");

		_upperThreshold = Param(nameof(UpperThreshold), 65m)
			.SetDisplay("Upper", "RSI overbought threshold", "Indicators");

		_lowerThreshold = Param(nameof(LowerThreshold), 35m)
			.SetDisplay("Lower", "RSI oversold threshold", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_overboughtCount = 0;
		_oversoldCount = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track consecutive overbought candles
		if (rsiValue >= UpperThreshold)
			_overboughtCount++;
		else
			_overboughtCount = 0;

		// Track consecutive oversold candles
		if (rsiValue <= LowerThreshold)
			_oversoldCount++;
		else
			_oversoldCount = 0;

		// After qualified oversold period, buy (contrarian)
		if (_oversoldCount >= QualificationLength && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_oversoldCount = 0;
		}
		// After qualified overbought period, sell (contrarian)
		else if (_overboughtCount >= QualificationLength && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_overboughtCount = 0;
		}
	}
}
