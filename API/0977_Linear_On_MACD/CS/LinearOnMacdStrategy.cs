using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<bool> _riskHigh;
	private readonly StrategyParam<DataType> _candleType;

	private OnBalanceVolume _obv = null!;
	private MovingAverageConvergenceDivergenceSignal _obvMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _priceMacd = null!;
	private LinearRegression _priceReg = null!;
	private SimpleMovingAverage _riskMa = null!;

	private decimal _prevPredicted;
	private decimal _prevObvMacd;

	/// <summary>
	/// Fast EMA period for OBV MACD.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA period for OBV MACD.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal period for OBV MACD.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Lookback period for price regression.
	/// </summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	/// <summary>
	/// Use moving average of regression as risk level.
	/// </summary>
	public bool RiskHigh { get => _riskHigh.Value; set => _riskHigh.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public LinearOnMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "Fast EMA period for OBV MACD", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Slow EMA period for OBV MACD", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 32, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "Signal period for OBV MACD", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 13, 2);

		_lookback = Param(nameof(Lookback), 21)
			.SetDisplay("Lookback", "Lookback for price regression", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_riskHigh = Param(nameof(RiskHigh), false)
			.SetDisplay("High Risk", "Use moving average of regression as risk", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevPredicted = 0m;
		_prevObvMacd = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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
		_riskMa = new SimpleMovingAverage { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_obv, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _priceReg);
			DrawIndicator(area, _riskMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var obvMacdTyped = (MovingAverageConvergenceDivergenceSignalValue)_obvMacd.Process(obvValue, candle.ServerTime, true);
		var priceMacdTyped = (MovingAverageConvergenceDivergenceSignalValue)_priceMacd.Process(candle.ClosePrice, candle.ServerTime, true);
		var regTyped = (LinearRegressionValue)_priceReg.Process(candle.ClosePrice, candle.ServerTime, true);

		if (obvMacdTyped.Macd is not decimal obvMacd ||
			obvMacdTyped.Signal is not decimal obvSignal ||
			priceMacdTyped.Macd is not decimal priceMacd ||
			priceMacdTyped.Signal is not decimal priceSignal ||
			regTyped.LinearReg is not decimal predicted)
			return;

		var riskLevel = predicted;
		if (RiskHigh)
		{
			var maValue = _riskMa.Process(predicted, candle.ServerTime, true);
			if (maValue.IsFinal)
				riskLevel = maValue.ToDecimal();
		}

		var isBetween = candle.OpenPrice < predicted && predicted < candle.ClosePrice;
		var longCondition = priceMacd > priceSignal && obvMacd > obvSignal && isBetween && predicted > _prevPredicted;
		var macdFall = obvMacd < _prevObvMacd;
		var macdSell = obvMacd < obvSignal;
		var shortCondition = macdFall && macdSell && priceMacd < priceSignal && candle.ClosePrice < riskLevel;

		if (longCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevPredicted = predicted;
		_prevObvMacd = obvMacd;
	}
}
