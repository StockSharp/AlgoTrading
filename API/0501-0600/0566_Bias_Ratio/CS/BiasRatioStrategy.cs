using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bias Ratio Strategy - trades when price deviates from moving averages by a threshold.
/// </summary>
public class BiasRatioStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _biasThreshold;

	private decimal _prevBiasEma;
	private decimal _prevBiasSma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal BiasThreshold { get => _biasThreshold.Value; set => _biasThreshold.Value = value; }

	public BiasRatioStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");

		_biasThreshold = Param(nameof(BiasThreshold), 0.015m)
			.SetDisplay("Bias Threshold", "Price deviation ratio from MA", "Trading");
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
		_prevBiasEma = 0m;
		_prevBiasSma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (emaValue <= 0 || smaValue <= 0)
			return;

		var biasEma = candle.ClosePrice / emaValue - 1m;
		var biasSma = candle.ClosePrice / smaValue - 1m;

		// Long: price crosses above threshold from EMA
		var longSignal = _prevBiasEma <= BiasThreshold && biasEma > BiasThreshold;
		// Short: price crosses below negative threshold from SMA
		var shortSignal = _prevBiasSma >= -BiasThreshold && biasSma < -BiasThreshold;

		if (longSignal && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket();
		}

		_prevBiasEma = biasEma;
		_prevBiasSma = biasSma;
	}
}
