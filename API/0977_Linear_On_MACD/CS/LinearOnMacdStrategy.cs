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

	private decimal _prevPredicted;
	private decimal _prevObvMacd;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LinearOnMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12);
		_slowLength = Param(nameof(SlowLength), 26);
		_signalLength = Param(nameof(SignalLength), 9);
		_lookback = Param(nameof(Lookback), 21);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPredicted = 0m;
		_prevObvMacd = 0m;

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
		_priceMacd = new MovingAverageConvergenceDivergenceSignal();
		_priceReg = new LinearRegression { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_obv, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var obvMacdResult = _obvMacd.Process(CreateFinalValue(_obvMacd, obvValue, candle.ServerTime));
		var priceMacdResult = _priceMacd.Process(CreateFinalValue(_priceMacd, candle.ClosePrice, candle.ServerTime));
		var regResult = _priceReg.Process(CreateFinalValue(_priceReg, candle.ClosePrice, candle.ServerTime));

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
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevPredicted = predicted;
		_prevObvMacd = obvMacd;
	}
}
