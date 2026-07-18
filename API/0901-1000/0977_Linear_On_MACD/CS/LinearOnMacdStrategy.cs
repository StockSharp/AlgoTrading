using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining MACD on price and volume based data with linear regression.
/// </summary>
public class LinearOnMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private OnBalanceVolume _obv = null!;
	private MovingAverageConvergenceDivergenceSignal _obvMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _priceMacd = null!;
	private LinearRegression _priceReg = null!;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LinearOnMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 70);
		_slowLength = Param(nameof(SlowLength), 200);
		_signalLength = Param(nameof(SignalLength), 50);
		_lookback = Param(nameof(Lookback), 140);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_obv = new OnBalanceVolume();
		_obvMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};
		_priceMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};
		_priceReg = new LinearRegression { Length = Lookback };

		var dummyEma = new ExponentialMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_obv, dummyEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private static DecimalIndicatorValue CreateFinalValue(IIndicator ind, decimal value, DateTime time)
	{
		var v = new DecimalIndicatorValue(ind, value, time);
		v.IsFinal = true;
		return v;
	}

	private void ProcessCandle(ICandleMessage candle, decimal obvValue, decimal dummyValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var obvMacdResult = _obvMacd.Process(CreateFinalValue(_obvMacd, obvValue, candle.ServerTime));
		var priceMacdResult = _priceMacd.Process(CreateFinalValue(_priceMacd, candle.ClosePrice, candle.ServerTime));
		var regResult = _priceReg.Process(CreateFinalValue(_priceReg, candle.ClosePrice, candle.ServerTime));

		if (!_obvMacd.IsFormed || !_priceMacd.IsFormed || !_priceReg.IsFormed)
			return;

		if (obvMacdResult is not IMovingAverageConvergenceDivergenceSignalValue obvMacdTyped)
			return;
		if (priceMacdResult is not IMovingAverageConvergenceDivergenceSignalValue priceMacdTyped)
			return;
		if (regResult is not ILinearRegressionValue regTyped)
			return;

		if (obvMacdTyped.Macd is not decimal obvMacd ||
			obvMacdTyped.Signal is not decimal obvSignal ||
			priceMacdTyped.Macd is not decimal priceMacd ||
			priceMacdTyped.Signal is not decimal priceSignal ||
			regTyped.LinearReg is not decimal predicted)
			return;

		var longCondition = priceMacd > priceSignal && obvMacd > obvSignal && candle.ClosePrice > predicted;
		var shortCondition = obvMacd < obvSignal && priceMacd < priceSignal && candle.ClosePrice < predicted;

		if (longCondition && Position <= 0)
			BuyMarket();
		else if (shortCondition && Position >= 0)
			SellMarket();
	}
}
